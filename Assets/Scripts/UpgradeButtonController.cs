using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attached to the Upgrade Button GameObject.
///
/// Responsibilities:
///   1. Toggle the Animator's 'speed' parameter between 1 and -1 on each click.
///   2. Expose OnTransitionMidpoint() as an animation event target that swaps
///      the button label between "Upgrade" and "Slot".
/// </summary>
[RequireComponent(typeof(Button))]
public class UpgradeButtonController : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    private const string SpeedParam = "speed";
    private const string StartTrigger = "Start";
    private const string LabelUpgrade = "Upgrade";
    private const string LabelSlot = "Slot";
    private const float SpeedForward = 1f;
    private const float SpeedReverse = -1f;

    // -------------------------------------------------------------------------
    // Inspector fields
    // -------------------------------------------------------------------------

    [Header("References")]
    [Tooltip("The TMP label on this button.")]
    [SerializeField] private TextMeshProUGUI _label;

    [Tooltip("The Animator that owns the Canvas controller with the 'speed' parameter.")]
    [SerializeField] private Animator _animator;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private Button _button;
    private bool _isUpgradeState = true;
    private bool _isAnimating;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnButtonClicked);

        if (_label == null)
        {
            _label = GetComponentInChildren<TextMeshProUGUI>();
        }

        if (_animator == null)
        {
            _animator = GetComponentInParent<Animator>();
        }

        if (_label == null)
        {
            Debug.LogError("[UpgradeButtonController]: No TextMeshProUGUI found.", this);
        }

        if (_animator == null)
        {
            Debug.LogError("[UpgradeButtonController]: No Animator found in parent.", this);
        }
    }

    private void OnDestroy()
    {
        _button.onClick.RemoveListener(OnButtonClicked);
    }

    // -------------------------------------------------------------------------
    // Button click handler
    // -------------------------------------------------------------------------

    private void OnButtonClicked()
    {
        if (_animator == null || _isAnimating)
        {
            return;
        }

        _animator.enabled = true;

        float speed = _isUpgradeState ? SpeedForward : SpeedReverse;
        _animator.SetFloat(SpeedParam, speed);
        _animator.SetTrigger(StartTrigger);

        _isUpgradeState = !_isUpgradeState;

        StopAllCoroutines();
        StartCoroutine(WaitForAnimationComplete());
    }

    /// <summary>
    /// Waits two frames for the animator to enter the transition, then waits
    /// for the clip to finish one full cycle before disabling the animator.
    /// This prevents the loop from continuously re-driving the UI layout.
    /// </summary>
    private IEnumerator WaitForAnimationComplete()
    {
        _isAnimating = true;

        // Wait for the animator to start transitioning into the new state.
        yield return null;
        yield return null;

        // Wait out the transition blend.
        while (_animator.IsInTransition(0))
        {
            yield return null;
        }

        // Read the clip length from the state now that we're fully inside it.
        float clipLength = _animator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(clipLength);

        _animator.enabled = false;
        _isAnimating = false;
    }

    // -------------------------------------------------------------------------
    // Animation event target
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called by the animation event at frame 15 of Upgrade_transition.
    /// Swaps the button label between "Upgrade" and "Slot" based on playback direction.
    /// </summary>
    public void OnTransitionMidpoint()
    {
        if (_label == null)
        {
            return;
        }

        // When going forward (speed = 1), we just pressed Upgrade → show "Slot".
        // When going backward (speed = -1), we just pressed Slot → show "Upgrade".
        _label.text = _isUpgradeState ? LabelUpgrade : LabelSlot;
    }
}
