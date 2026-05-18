using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attached to the Upgrade Button GameObject.
/// Toggles the Animator's 'speed' parameter between 1 and -1 on each click
/// to play the transition forward or in reverse.
/// Label swapping is handled separately by AnimationEventRelay on the Canvas.
/// </summary>
[RequireComponent(typeof(Button))]
public class UpgradeButtonController : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    private const string SpeedParam   = "speed";
    private const string StartTrigger = "Start";
    private const float  SpeedForward = 1f;
    private const float  SpeedReverse = -1f;

    // -------------------------------------------------------------------------
    // Inspector fields
    // -------------------------------------------------------------------------

    [Header("References")]
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

        if (_animator == null)
        {
            _animator = GetComponentInParent<Animator>();
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

        yield return null;
        yield return null;

        while (_animator.IsInTransition(0))
        {
            yield return null;
        }

        float clipLength = _animator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(clipLength);

        _animator.enabled = false;
        _isAnimating = false;
    }
}
