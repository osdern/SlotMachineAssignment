using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Moves a UI RectTransform (anchoredPosition) through the path:
///   start → slot → end → destroy self
///
/// Behaviour at the slot position each pass:
///   - Always spawns the configured prefab at the start position (skipped when resuming).
///   - If SlotGameConfig.IsLuckActive is false  → always continues (luck ignored).
///   - If SlotGameConfig.IsLuckActive is true   → luck is evaluated using the
///     per-symbol value from SlotGameConfig; if it wins, the item stops and all
///     sibling ReelMovers are paused at their current positions.
///
/// ResumeFromPause() handles two cases:
///   - Item stopped at slot (winner)   → builds a fresh slot → end → destroy sequence.
///   - Item paused mid-tween (sibling) → resumes its sequence; when it reaches the
///     slot position it skips the spawn and luck and goes straight to end → destroy.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ReelMover : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector fields
    // -------------------------------------------------------------------------

    [Header("Symbol Type")]
    [Tooltip("Which symbol this reel item represents. Determines which luck value is read from SlotGameConfig.")]
    [SerializeField] private ReelItemType _itemType;

    [Header("Positions (anchoredPosition)")]
    [Tooltip("Local anchored position where movement begins.")]
    [SerializeField] private Vector2 _startPosition;

    [Tooltip("Local anchored position where the object may stop (slot stop).")]
    [SerializeField] private Vector2 _slotPosition;

    [Tooltip("Local anchored position at the very end of the movement.")]
    [SerializeField] private Vector2 _endPosition;

    [Header("Timing")]
    [Tooltip("Duration (seconds) for the start → slot leg.")]
    [SerializeField] private float _durationToSlot = 1f;

    [Tooltip("Duration (seconds) for the slot → end leg.")]
    [SerializeField] private float _durationSlotToEnd = 0.5f;

    [Header("Easing")]
    [SerializeField] private Ease _easeToSlot = Ease.Linear;
    [SerializeField] private Ease _easeSlotToEnd = Ease.OutQuad;

    [Header("Spawn")]
    [Tooltip("Prefab to instantiate at the start position each time this element passes through the slot position.")]
    [SerializeField] private RectTransform _spawnPrefab;

    [Header("Auto Play")]
    [Tooltip("If true, StartMovement() is called automatically on Awake.")]
    [SerializeField] private bool _autoPlay = false;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private RectTransform _rectTransform;
    private SlotGameConfig _config;
    private WinningCondition _winningCondition;
    private Sequence _sequence;
    private bool _isPaused;

    // True for the item whose luck won — its sequence is dead, needs a fresh one on resume.
    private bool _isStopped;

    // True for siblings that are mid-tween when paused — skip spawn and luck on next slot pass.
    private bool _isResuming;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();

        _config = FindFirstObjectByType<SlotGameConfig>();
        _winningCondition = FindFirstObjectByType<WinningCondition>();

        if (_config == null)
        {
            Debug.LogError($"[ReelMover] '{name}': No SlotGameConfig found in the scene.", this);
        }

        if (_autoPlay)
        {
            StartMovement();
        }
    }

    private void OnDestroy()
    {
        KillTween();
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>The symbol type this reel item represents.</summary>
    public ReelItemType ItemType => _itemType;

    /// <summary>Returns true while the tween is alive but paused, or the item has stopped at slot.</summary>
    public bool IsPaused => _isPaused;

    /// <summary>
    /// Returns true when this item has no active sequence and is not paused —
    /// i.e. it was spawned and is sitting at the start position waiting for the next spin.
    /// </summary>
    public bool IsWaiting => !_isPaused && (_sequence == null || !_sequence.IsActive());

    /// <summary>
    /// Starts the movement from the start position.
    /// Kills any running tween before beginning.
    /// Also notifies SlotGameConfig to begin the spin timer if not already running.
    /// </summary>
    public void StartMovement()
    {
        KillTween();
        _isPaused = false;
        _isStopped = false;
        _isResuming = false;

        _config?.StartSpin();

        _rectTransform.anchoredPosition = _startPosition;

        MoveToSlot();
    }

    /// <summary>
    /// Pauses the tween wherever the object currently is (for mid-tween items).
    /// Safe to call even if no tween is running.
    /// </summary>
    public void Pause()
    {
        if (_sequence != null && _sequence.IsActive() && _sequence.IsPlaying())
        {
            _sequence.Pause();
            _isPaused = true;
        }
    }

    /// <summary>
    /// Resumes movement after a full spin cycle has ended.
    ///
    /// - If this item stopped at the slot position (winner): builds a fresh
    ///   slot → end → destroy tween so it slides off without spawning.
    /// - If this item was paused mid-tween (sibling): resumes its existing
    ///   tween; the next slot-position pass will skip spawning and luck,
    ///   going directly to end → destroy.
    /// </summary>
    public void ResumeFromPause()
    {
        if (!_isPaused)
        {
            return;
        }

        _isPaused = false;

        if (_isStopped)
        {
            // Winner: sequence is dead — build a fresh one from slot to end
            _isStopped = false;
            MoveToEnd();
        }
        else
        {
            // Mid-tween sibling: resume sequence; flag skips spawn + luck at slot
            _isResuming = true;

            if (_sequence != null && _sequence.IsActive())
            {
                _sequence.Play();
            }
        }
    }

    /// <summary>Kills the active tween immediately without completing it.</summary>
    public void Stop()
    {
        KillTween();
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds and plays the start → slot leg.
    /// The slot-position callback decides what happens next.
    /// </summary>
    private void MoveToSlot()
    {
        _sequence = DOTween.Sequence();

        _sequence.Append(
            _rectTransform.DOAnchorPos(_slotPosition, _durationToSlot)
                          .SetEase(_easeToSlot)
        );

        _sequence.AppendCallback(OnPassedSlotPosition);
        _sequence.SetAutoKill(true);
        _sequence.Play();
    }

    /// <summary>
    /// Builds and plays the slot → end leg, then destroys the object.
    /// Used when resuming a stopped winner — no spawn, no luck check.
    /// </summary>
    private void MoveToEnd()
    {
        _sequence = DOTween.Sequence();

        _sequence.Append(
            _rectTransform.DOAnchorPos(_endPosition, _durationSlotToEnd)
                          .SetEase(_easeSlotToEnd)
        );

        _sequence.AppendCallback(() => Destroy(gameObject));
        _sequence.SetAutoKill(true);
        _sequence.Play();
    }

    /// <summary>
    /// Fires every time the element passes through the slot position.
    /// Skips spawn and luck when _isResuming is true (cleanup pass after handle press).
    /// </summary>
    private void OnPassedSlotPosition()
    {
        // Always spawn — pausing and resuming state has no effect on this.
        SpawnPrefabAtStart();

        if (_isResuming)
        {
            // Cleanup pass — skip luck, go straight to end.
            _isResuming = false;
            MoveToEnd();
            return;
        }

        bool luckActive = _config != null && _config.IsLuckActive;
        float luck = _config != null ? _config.GetLuck(_itemType) : 0f;
        bool willStop = luckActive && (Random.value <= luck);

        if (willStop)
        {
            OnReachedSlotPosition();
            return;
        }

        MoveToEnd();
    }

    /// <summary>
    /// Called when this item wins the luck roll at the slot position.
    /// Marks itself as stopped and pauses all sibling ReelMovers mid-tween.
    /// </summary>
    private void OnReachedSlotPosition()
    {
        _isStopped = true;
        _isPaused = true;

        _config?.StopSpin();

        if (transform.parent == null)
        {
            return;
        }

        List<ReelMover> siblings = new List<ReelMover>(
            transform.parent.GetComponentsInChildren<ReelMover>()
        );

        foreach (ReelMover mover in siblings)
        {
            if (mover != this)
            {
                mover.Pause();
            }
        }

        if (_config != null)
        {
            _config.IncrementLoopComplete();

            if (_config.LoopComplete >= 3)
            {
                _winningCondition?.Evaluate();
            }
        }
    }

    /// <summary>
    /// Instantiates <see cref="_spawnPrefab"/> under the same parent at the
    /// start anchored position. Does nothing if no prefab is assigned.
    /// </summary>
    private void SpawnPrefabAtStart()
    {
        if (_spawnPrefab == null)
        {
            Debug.LogWarning($"[ReelMover] '{name}': No spawn prefab assigned.", this);
            return;
        }

        RectTransform spawned = Instantiate(_spawnPrefab, transform.parent);
        spawned.anchoredPosition = _startPosition;
    }

    private void KillTween()
    {
        if (_sequence != null && _sequence.IsActive())
        {
            _sequence.Kill();
        }

        _sequence = null;
        _isPaused = false;
        _isStopped = false;
        _isResuming = false;
    }
}
