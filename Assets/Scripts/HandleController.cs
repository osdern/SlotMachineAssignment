using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attached to the Handle_up GameObject (which has a Button component).
///
/// Responsibilities:
///   1. Toggle Handle_up / Handle_Down visuals on click.
///   2. Shake the handle when the reels are still spinning.
///   3. Trigger a deduction via MoneyManager and restart the reels.
/// </summary>
[RequireComponent(typeof(Button))]
public class HandleController : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    private const float HandleDownDuration = 0.5f;
    private const float ShakeDuration = 0.4f;
    private const float ShakeStrength = 12f;
    private const int ShakeVibrato = 20;
    private const float ShakeRandomness = 45f;

    // -------------------------------------------------------------------------
    // Inspector fields
    // -------------------------------------------------------------------------

    [Header("References")]
    [Tooltip("The Handle_Down GameObject to show briefly on click.")]
    [SerializeField] private GameObject _handleDown;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private Button _button;
    private Image _handleUpImage;
    private RectTransform _rectTransform;
    private SlotGameConfig _config;
    private bool _isAnimating;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        _button = GetComponent<Button>();
        _handleUpImage = GetComponent<Image>();
        _rectTransform = GetComponent<RectTransform>();
        _config = FindFirstObjectByType<SlotGameConfig>();

        if (_config == null)
        {
            Debug.LogError("[HandleController]: No SlotGameConfig found in the scene.", this);
        }

        // Auto-find Handle_Down if not assigned in the Inspector
        if (_handleDown == null)
        {
            _handleDown = GameObject.Find("Handle_Down");

            if (_handleDown == null)
            {
                Debug.LogError("[HandleController]: Handle_Down GameObject not found.", this);
            }
        }

        _button.onClick.AddListener(OnHandleClicked);
    }

    private void OnDestroy()
    {
        _button.onClick.RemoveListener(OnHandleClicked);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private void OnHandleClicked()
    {
        if (_isAnimating)
        {
            return;
        }

        if (_config != null && _config.IsSpinning)
        {
            ShakeHandle();
            return;
        }

        StartCoroutine(HandleSequence());
    }

    /// <summary>
    /// Shakes the handle's RectTransform to indicate the reels are still spinning.
    /// </summary>
    private void ShakeHandle()
    {
        _rectTransform.DOShakeAnchorPos(ShakeDuration, ShakeStrength, ShakeVibrato, ShakeRandomness);
    }

    private IEnumerator HandleSequence()
    {
        _isAnimating = true;
        _button.interactable = false;

        // Hide only the Image on Handle_up — keeps the GameObject and script alive
        if (_handleUpImage != null)
        {
            _handleUpImage.enabled = false;
        }

        if (_handleDown != null)
        {
            _handleDown.SetActive(true);
        }

        yield return new WaitForSeconds(HandleDownDuration);

        // Restore Handle_up image, hide Handle_Down
        if (_handleDown != null)
        {
            _handleDown.SetActive(false);
        }

        if (_handleUpImage != null)
        {
            _handleUpImage.enabled = true;
        }

        _button.interactable = true;

        // Delegate money deduction to MoneyManager
        if (_config != null && MoneyManager.Instance != null)
        {
            MoneyManager.Instance.Deduct(_config.GamblingMoney);
        }

        // Restart spin timer and resume all ReelMovers
        RestartReels();

        _isAnimating = false;
    }

    /// <summary>
    /// Restarts the SlotGameConfig spin timer and resumes every ReelMover in the scene.
    /// Waiting movers are kicked off via StartMovement(); paused ones via ResumeFromPause().
    /// </summary>
    private void RestartReels()
    {
        if (_config != null)
        {
            _config.ResetLoopComplete();
            _config.StopSpin();
            _config.StartSpin();
        }

        List<ReelMover> reelMovers = new List<ReelMover>(
            FindObjectsByType<ReelMover>(FindObjectsSortMode.None)
        );

        foreach (ReelMover mover in reelMovers)
        {
            if (mover.IsWaiting)
            {
                mover.StartMovement();
            }
            else
            {
                mover.ResumeFromPause();
            }
        }
    }
}
