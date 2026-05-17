using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to the Handle_up GameObject (which already has a Button component).
///
/// On click:
///   1. Hides Handle_up, shows Handle_Down.
///   2. After 0.5 s, hides Handle_Down and shows Handle_up again.
///   3. Deducts GamblingMoney from SlotGameConfig and updates the Money TMP label.
///   4. Resumes movement on every ReelMover in the scene and restarts the spin timer.
/// </summary>
[RequireComponent(typeof(Button))]
public class HandleController : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    private const float HandleDownDuration = 0.5f;

    // -------------------------------------------------------------------------
    // Inspector fields
    // -------------------------------------------------------------------------

    [Header("References")]
    [Tooltip("The Handle_Down GameObject to show briefly on click.")]
    [SerializeField] private GameObject _handleDown;

    [Tooltip("The Money TextMeshProUGUI label that displays the current balance.")]
    [SerializeField] private TextMeshProUGUI _moneyText;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private Button _button;
    private Image _handleUpImage;
    private SlotGameConfig _config;
    private bool _isAnimating;
    private float _currentBalance;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        _button = GetComponent<Button>();
        _handleUpImage = GetComponent<Image>();
        _config = FindFirstObjectByType<SlotGameConfig>();

        if (_config == null)
        {
            Debug.LogError("[HandleController]: No SlotGameConfig found in the scene.", this);
        }

        // Auto-find Handle_Down if not set in inspector
        if (_handleDown == null)
        {
            _handleDown = GameObject.Find("Handle_Down");

            if (_handleDown == null)
            {
                Debug.LogError("[HandleController]: Handle_Down GameObject not found.", this);
            }
        }

        // Auto-find Money label if not set in inspector
        if (_moneyText == null)
        {
            GameObject moneyObj = GameObject.Find("Money");

            if (moneyObj != null)
            {
                _moneyText = moneyObj.GetComponent<TextMeshProUGUI>();
            }

            if (_moneyText == null)
            {
                Debug.LogError("[HandleController]: Money TextMeshProUGUI not found.", this);
            }
        }

        _button.onClick.AddListener(OnHandleClicked);

        // Seed the running balance from whatever the Money text currently shows
        if (_moneyText != null && float.TryParse(_moneyText.text.Trim(), out float parsed))
        {
            _currentBalance = parsed;
        }
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

        StartCoroutine(HandleSequence());
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

        // Deduct gambling money and update label
        DeductMoney();

        // Restart spin timer and resume all ReelMovers
        RestartReels();

        _isAnimating = false;
    }

    /// <summary>
    /// Subtracts GamblingMoney from the running balance and refreshes the Money label.
    /// </summary>
    private void DeductMoney()
    {
        if (_config == null)
        {
            return;
        }

        _currentBalance = Mathf.Max(0f, _currentBalance - _config.GamblingMoney);

        if (_moneyText != null)
        {
            _moneyText.text = _currentBalance.ToString("0");
        }
    }

    /// <summary>
    /// Restarts the SlotGameConfig spin timer and resumes every ReelMover in the scene.
    /// - Paused items (mid-tween siblings or stopped winners) are resumed via ResumeFromPause().
    /// - Waiting items (spawned at start, never started) are kicked off via StartMovement().
    /// </summary>
    private void RestartReels()
    {
        if (_config != null)
        {
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
