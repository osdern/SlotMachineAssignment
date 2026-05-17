using UnityEngine;

/// <summary>
/// Defines the four reel symbol types.
/// </summary>
public enum ReelItemType
{
    Seven,
    Cherry,
    Bell,
    Bar
}

/// <summary>
/// Central configuration for the slot machine.
/// Holds per-symbol luck values, the spin timer, and the gambling money.
/// Other scripts (e.g. ReelMover) read from this component.
///
/// Call StartSpin() to begin the countdown. Once the timer reaches zero,
/// IsLuckActive becomes true and ReelMovers will start evaluating luck.
/// </summary>
public class SlotGameConfig : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector fields
    // -------------------------------------------------------------------------

    [Header("Per-Symbol Luck [0 = never stops, 1 = always stops]")]
    [Range(0f, 1f)]
    [SerializeField] private float _sevenLuck = 0.5f;

    [Range(0f, 1f)]
    [SerializeField] private float _cherryLuck = 0.5f;

    [Range(0f, 1f)]
    [SerializeField] private float _bellLuck = 0.5f;

    [Range(0f, 1f)]
    [SerializeField] private float _barLuck = 0.5f;

    [Header("Spin Timer")]
    [Tooltip("Seconds the reels must spin before luck evaluation begins.")]
    [SerializeField] private float _spinDuration = 3f;

    [Tooltip("Read-only runtime view of the remaining timer. Visible in Play Mode.")]
    [SerializeField] private float _remainingTime;

    [Header("Gambling")]
    [Tooltip("Amount of money deducted from the balance on each spin.")]
    [SerializeField] private float _gamblingMoney = 10f;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private bool _isSpinning;

    // -------------------------------------------------------------------------
    // Public read-only properties
    // -------------------------------------------------------------------------

    /// <summary>
    /// True when a spin is in progress AND the minimum timer has expired.
    /// ReelMovers should only evaluate luck while this is true.
    /// </summary>
    public bool IsLuckActive => _isSpinning && _remainingTime <= 0f;

    /// <summary>Seconds remaining on the current spin timer.</summary>
    public float RemainingTime => _remainingTime;

    /// <summary>Whether a spin is currently in progress.</summary>
    public bool IsSpinning => _isSpinning;

    /// <summary>Amount deducted from the player's balance on each spin.</summary>
    public float GamblingMoney => _gamblingMoney;

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Starts the spin countdown. Call this when the reels begin moving.
    /// If a spin is already in progress the timer is not reset.
    /// </summary>
    public void StartSpin()
    {
        if (_isSpinning)
        {
            return;
        }

        _remainingTime = _spinDuration;
        _isSpinning = true;
    }

    /// <summary>
    /// Stops the spin and resets the timer.
    /// Call this when all reels have come to rest.
    /// </summary>
    public void StopSpin()
    {
        _isSpinning = false;
        _remainingTime = 0f;
    }

    /// <summary>
    /// Returns the luck value [0–1] for the given reel symbol type.
    /// </summary>
    public float GetLuck(ReelItemType type)
    {
        return type switch
        {
            ReelItemType.Seven  => _sevenLuck,
            ReelItemType.Cherry => _cherryLuck,
            ReelItemType.Bell   => _bellLuck,
            ReelItemType.Bar    => _barLuck,
            _                   => 0f
        };
    }

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Update()
    {
        if (_isSpinning && _remainingTime > 0f)
        {
            _remainingTime -= Time.deltaTime;

            if (_remainingTime < 0f)
            {
                _remainingTime = 0f;
            }
        }
    }
}
