using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles luck upgrades for the slot machine's upgrade menu.
///
/// Tier table (cumulative luck boost → cost to reach it):
///   0%  →  5%  :  300 coins
///   5%  → 10%  :  1 200 coins
///  10%  → 20%  :  2 800 coins
///  20%  → 40%  :  5 600 coins
///  40%  → 80%  : 12 000 coins
///  80%  → MAX  : 30 000 coins
///
/// Per upgrade step the following deltas are applied to each symbol's
/// current luck value (delta = difference between tier percentages):
///   Seven  : +delta%  of current value
///   Bar    : +delta/2% of current value
///   Bell   : -delta/2% of current value
///   Cherry : -delta%  of current value
/// </summary>
public class LuckUpgradeController : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Tier data
    // -------------------------------------------------------------------------

    /// <summary>Cumulative luck-boost label for each tier (index = tier level).</summary>
    private static readonly float[] TierLuckPercent = { 0f, 5f, 10f, 20f, 40f, 80f, 100f };

    /// <summary>Cost in coins to upgrade FROM tier[i] TO tier[i+1].</summary>
    private static readonly float[] TierCost = { 300f, 1200f, 2800f, 5600f, 12000f, 30000f };

    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    private const float ShakeDuration   = 0.4f;
    private const float ShakeStrength   = 12f;
    private const int   ShakeVibrato    = 20;
    private const float ShakeRandomness = 45f;

    // -------------------------------------------------------------------------
    // Inspector fields
    // -------------------------------------------------------------------------

    [Header("References")]
    [Tooltip("SlotGameConfig that holds the per-symbol luck values.")]
    [SerializeField] private SlotGameConfig _config;

    [Tooltip("The Button that triggers the upgrade.")]
    [SerializeField] private Button _upgradeButton;

    [Header("UI Labels")]
    [Tooltip("Text on the upgrade button showing the cost of the next upgrade.")]
    [SerializeField] private TextMeshProUGUI _costText;

    [Tooltip("Text showing the player's current luck tier percentage.")]
    [SerializeField] private TextMeshProUGUI _currentLuckText;

    [Tooltip("Text previewing the luck tier percentage after upgrading.")]
    [SerializeField] private TextMeshProUGUI _upgradedLuckText;

    [Tooltip("Arrow GameObject between the current and upgraded luck texts. Hidden when maxed.")]
    [SerializeField] private GameObject _arrowObject;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private int _currentTier;
    private RectTransform _buttonRect;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (_config == null)
        {
            _config = FindFirstObjectByType<SlotGameConfig>();
        }

        if (_upgradeButton == null)
        {
            _upgradeButton = GetComponent<Button>();
        }

        if (_upgradeButton != null)
        {
            _buttonRect = _upgradeButton.GetComponent<RectTransform>();
            _upgradeButton.onClick.AddListener(OnUpgradeClicked);
        }
        else
        {
            Debug.LogError("[LuckUpgradeController]: No Button component found.", this);
        }
    }

    private void Start()
    {
        // Read tier in Start so SlotGameConfig.Awake has already
        // restored all saved luck values before we display them.
        if (SaveManager.Instance != null)
        {
            _currentTier = SaveManager.Instance.Data.luckTier;
        }

        RefreshUI();
    }

    private void OnDestroy()
    {
        if (_upgradeButton != null)
        {
            _upgradeButton.onClick.RemoveListener(OnUpgradeClicked);
        }
    }

    // -------------------------------------------------------------------------
    // Button handler
    // -------------------------------------------------------------------------

    /// <summary>
    /// Checks whether the player can afford the next luck upgrade.
    /// Applies the upgrade and deducts money on success; shakes the button on failure.
    /// </summary>
    private void OnUpgradeClicked()
    {
        if (_currentTier >= TierCost.Length)
        {
            // Already at max tier — nothing to do.
            return;
        }

        float cost = TierCost[_currentTier];

        if (MoneyManager.Instance == null || MoneyManager.Instance.GetBalance() < cost)
        {
            ShakeButton();
            return;
        }

        MoneyManager.Instance.Deduct(cost);
        ApplyLuckUpgrade();
        _currentTier++;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Data.luckTier = _currentTier;
            SaveManager.Instance.Save();
        }

        RefreshUI();
    }

    // -------------------------------------------------------------------------
    // Core luck logic
    // -------------------------------------------------------------------------

    /// <summary>
    /// Applies the luck delta for the current upgrade step to all four symbols.
    ///
    /// The delta is the percentage-point difference between the next tier and
    /// the current tier (e.g. tier 0→1 has delta = 5%).
    ///
    /// Multipliers applied to each symbol's current luck value:
    ///   Seven  +delta%, Bar  +delta/2%, Bell  -delta/2%, Cherry  -delta%
    /// </summary>
    private void ApplyLuckUpgrade()
    {
        float delta = (TierLuckPercent[_currentTier + 1] - TierLuckPercent[_currentTier]) / 100f;

        AdjustLuck(ReelItemType.Seven,  delta);
        AdjustLuck(ReelItemType.Bar,    delta * 0.5f);
        AdjustLuck(ReelItemType.Bell,  -delta * 0.5f);
        AdjustLuck(ReelItemType.Cherry, -delta);
    }

    /// <summary>
    /// Multiplies a symbol's current luck value by (1 + <paramref name="delta"/>)
    /// and writes it back via SlotGameConfig.SetLuck.
    /// </summary>
    private void AdjustLuck(ReelItemType type, float delta)
    {
        float current = _config.GetLuck(type);
        _config.SetLuck(type, current + current * delta);
    }

    // -------------------------------------------------------------------------
    // UI helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Refreshes all three UI labels to reflect the current tier state.
    /// When maxed: current label shows "MAX", upgraded label and arrow are hidden.
    /// </summary>
    private void RefreshUI()
    {
        bool isMaxed = _currentTier >= TierCost.Length;

        if (_currentLuckText != null)
        {
            _currentLuckText.text = isMaxed
                ? "MAX"
                : $"{TierLuckPercent[_currentTier]:0}%";
        }

        if (_upgradedLuckText != null)
        {
            _upgradedLuckText.gameObject.SetActive(!isMaxed);

            if (!isMaxed)
            {
                _upgradedLuckText.text = $"{TierLuckPercent[_currentTier + 1]:0}%";
            }
        }

        if (_arrowObject != null)
        {
            _arrowObject.SetActive(!isMaxed);
        }

        if (_costText != null)
        {
            _costText.text = isMaxed
                ? "MAX"
                : $"{TierCost[_currentTier]:0}";
        }

        if (_upgradeButton != null)
        {
            _upgradeButton.interactable = !isMaxed;
        }
    }

    /// <summary>
    /// Shakes the upgrade button to indicate insufficient funds.
    /// </summary>
    private void ShakeButton()
    {
        if (_buttonRect == null)
        {
            return;
        }

        _buttonRect.DOShakeAnchorPos(ShakeDuration, ShakeStrength, ShakeVibrato, ShakeRandomness);
    }
}
