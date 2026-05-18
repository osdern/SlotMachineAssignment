using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages two independent upgrade slots in the upgrade menu:
///
/// [Gambling Money]
///   4 tiers — multiplies GamblingMoney by 10 each tier.
///   Cost starts at 1 000 and multiplies by 10 each tier.
///   Tier costs: 1 000 → 10 000 → 100 000 → 1 000 000
///
/// [Spin Duration]
///   Reduces SpinDuration by 1 second per tier until it reaches 1 s.
///   Cost starts at 2 000 and multiplies by 3 each tier.
///   Continues as long as (currentDuration - 1) > 0.
///   Example at default 3 s: 3 s→2 s (2 000) → 1 s (6 000) → MAX
/// </summary>
public class TimeAndMoneyUpgradeController : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Tier constants
    // -------------------------------------------------------------------------

    private const int   MoneyMaxTiers          = 4;
    private const float MoneyBaseCost           = 1000f;
    private const float MoneyCostMultiplier     = 10f;
    private const float MoneyGambleMultiplier   = 10f;

    private const float TimeBaseCost            = 2000f;
    private const float TimeCostMultiplier      = 3f;
    private const float TimeReductionPerTier    = 1f;
    private const float TimeMinDuration         = 1f;

    // -------------------------------------------------------------------------
    // Shake constants
    // -------------------------------------------------------------------------

    private const float ShakeDuration   = 0.4f;
    private const float ShakeStrength   = 12f;
    private const int   ShakeVibrato    = 20;
    private const float ShakeRandomness = 45f;

    // -------------------------------------------------------------------------
    // Serializable slot structs
    // -------------------------------------------------------------------------

    [System.Serializable]
    public class UpgradeSlot
    {
        [Tooltip("Button that triggers this upgrade.")]
        public Button button;

        [Tooltip("Cost label on the button.")]
        public TextMeshProUGUI costText;

        [Tooltip("Text showing the current value.")]
        public TextMeshProUGUI currentText;

        [Tooltip("Arrow GameObject — hidden when maxed.")]
        public GameObject arrowObject;

        [Tooltip("Text previewing the next value after upgrade.")]
        public TextMeshProUGUI upgradedText;

        /// <summary>How many times this slot has been upgraded.</summary>
        [System.NonSerialized] public int CurrentTier;
    }

    // -------------------------------------------------------------------------
    // Inspector fields
    // -------------------------------------------------------------------------

    [Header("References")]
    [Tooltip("SlotGameConfig that owns GamblingMoney and SpinDuration. Auto-found if left empty.")]
    [SerializeField] private SlotGameConfig _config;

    [Header("Gambling Money Upgrade Slot")]
    [SerializeField] private UpgradeSlot _moneySlot;

    [Header("Spin Duration Upgrade Slot")]
    [SerializeField] private UpgradeSlot _timeSlot;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    /// <summary>Cached initial spin duration read from config on Awake.</summary>
    private float _initialSpinDuration;

    /// <summary>Total tiers available for time, computed from initial spin duration.</summary>
    private int _timeMaxTiers;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (_config == null)
        {
            _config = FindFirstObjectByType<SlotGameConfig>();
        }

        if (_config == null)
        {
            Debug.LogError("[TimeAndMoneyUpgradeController]: No SlotGameConfig found in the scene.", this);
            return;
        }

        RegisterSlot(_moneySlot, OnMoneyUpgradeClicked);
        RegisterSlot(_timeSlot,  OnTimeUpgradeClicked);

        if (SaveManager.Instance != null)
        {
            _moneySlot.CurrentTier = SaveManager.Instance.Data.moneyTier;
            _timeSlot.CurrentTier  = SaveManager.Instance.Data.timeTier;
        }
    }

    private void Start()
    {
        // Guard against a failed Awake (config not found).
        if (_config == null) return;

        _initialSpinDuration = _config.SpinDuration + (_timeSlot.CurrentTier * TimeReductionPerTier);
        _timeMaxTiers = Mathf.Max(0, Mathf.FloorToInt(_initialSpinDuration - TimeMinDuration));

        RefreshMoneyUI();
        RefreshTimeUI();
    }

    private void OnDestroy()
    {
        _moneySlot?.button?.onClick.RemoveAllListeners();
        _timeSlot?.button?.onClick.RemoveAllListeners();
    }

    // -------------------------------------------------------------------------
    // Setup helpers
    // -------------------------------------------------------------------------

    /// <summary>Wires up the button listener and caches the RectTransform.</summary>
    private static void RegisterSlot(UpgradeSlot slot, UnityEngine.Events.UnityAction callback)
    {
        if (slot?.button == null)
        {
            return;
        }

        slot.button.onClick.AddListener(callback);
    }

    // -------------------------------------------------------------------------
    // Button handlers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Attempts a gambling-money upgrade.
    /// Deducts cost and multiplies GamblingMoney by 10 on success;
    /// shakes the button when funds are insufficient.
    /// </summary>
    private void OnMoneyUpgradeClicked()
    {
        if (_moneySlot.CurrentTier >= MoneyMaxTiers)
        {
            return;
        }

        float cost = GetMoneyCost(_moneySlot.CurrentTier);

        if (MoneyManager.Instance == null || MoneyManager.Instance.GetBalance() < cost)
        {
            ShakeButton(_moneySlot);
            return;
        }

        MoneyManager.Instance.Deduct(cost);
        _config.SetGamblingMoney(_config.GamblingMoney * MoneyGambleMultiplier);
        _moneySlot.CurrentTier++;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Data.moneyTier = _moneySlot.CurrentTier;
            SaveManager.Instance.Save();
        }

        RefreshMoneyUI();
    }

    /// <summary>
    /// Attempts a spin-duration upgrade.
    /// Deducts cost and reduces SpinDuration by 1 second on success;
    /// shakes the button when funds are insufficient.
    /// </summary>
    private void OnTimeUpgradeClicked()
    {
        if (_timeSlot.CurrentTier >= _timeMaxTiers)
        {
            return;
        }

        float cost = GetTimeCost(_timeSlot.CurrentTier);

        if (MoneyManager.Instance == null || MoneyManager.Instance.GetBalance() < cost)
        {
            ShakeButton(_timeSlot);
            return;
        }

        MoneyManager.Instance.Deduct(cost);
        _config.SetSpinDuration(_config.SpinDuration - TimeReductionPerTier);
        _timeSlot.CurrentTier++;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Data.timeTier = _timeSlot.CurrentTier;
            SaveManager.Instance.Save();
        }

        RefreshTimeUI();
    }

    // -------------------------------------------------------------------------
    // UI refresh
    // -------------------------------------------------------------------------

    /// <summary>
    /// Updates all labels in the gambling-money slot to reflect the current tier.
    /// </summary>
    private void RefreshMoneyUI()
    {
        bool isMaxed = _moneySlot.CurrentTier >= MoneyMaxTiers;

        if (_moneySlot.currentText != null)
        {
            _moneySlot.currentText.text = isMaxed
                ? "MAX"
                : FormatMoney(_config.GamblingMoney);
        }

        if (_moneySlot.upgradedText != null)
        {
            _moneySlot.upgradedText.gameObject.SetActive(!isMaxed);

            if (!isMaxed)
            {
                _moneySlot.upgradedText.text = FormatMoney(_config.GamblingMoney * MoneyGambleMultiplier);
            }
        }

        SetArrow(_moneySlot, !isMaxed);
        SetCostText(_moneySlot, isMaxed, GetMoneyCost(_moneySlot.CurrentTier));
        SetButtonInteractable(_moneySlot, !isMaxed);
    }

    /// <summary>
    /// Updates all labels in the spin-duration slot to reflect the current tier.
    /// </summary>
    private void RefreshTimeUI()
    {
        bool isMaxed = _timeSlot.CurrentTier >= _timeMaxTiers;

        if (_timeSlot.currentText != null)
        {
            _timeSlot.currentText.text = isMaxed
                ? "MAX"
                : FormatTime(_config.SpinDuration);
        }

        if (_timeSlot.upgradedText != null)
        {
            _timeSlot.upgradedText.gameObject.SetActive(!isMaxed);

            if (!isMaxed)
            {
                _timeSlot.upgradedText.text = FormatTime(_config.SpinDuration - TimeReductionPerTier);
            }
        }

        SetArrow(_timeSlot, !isMaxed);
        SetCostText(_timeSlot, isMaxed, GetTimeCost(_timeSlot.CurrentTier));
        SetButtonInteractable(_timeSlot, !isMaxed);
    }

    // -------------------------------------------------------------------------
    // Cost helpers
    // -------------------------------------------------------------------------

    /// <summary>Returns money upgrade cost at the given tier: 1000 × 10^tier.</summary>
    private static float GetMoneyCost(int tier)
    {
        return MoneyBaseCost * Mathf.Pow(MoneyCostMultiplier, tier);
    }

    /// <summary>Returns time upgrade cost at the given tier: 2000 × 3^tier.</summary>
    private static float GetTimeCost(int tier)
    {
        return TimeBaseCost * Mathf.Pow(TimeCostMultiplier, tier);
    }

    // -------------------------------------------------------------------------
    // Formatting helpers
    // -------------------------------------------------------------------------

    private static string FormatMoney(float value) => $"{value:0}";

    private static string FormatTime(float seconds) => $"{seconds:0.#}s";

    // -------------------------------------------------------------------------
    // Shared UI helpers
    // -------------------------------------------------------------------------

    private static void SetArrow(UpgradeSlot slot, bool active)
    {
        slot.arrowObject?.SetActive(active);
    }

    private static void SetCostText(UpgradeSlot slot, bool isMaxed, float cost)
    {
        if (slot.costText == null)
        {
            return;
        }

        slot.costText.text = isMaxed ? "MAX" : $"{cost:0}";
    }

    private static void SetButtonInteractable(UpgradeSlot slot, bool interactable)
    {
        if (slot.button != null)
        {
            slot.button.interactable = interactable;
        }
    }

    private static void ShakeButton(UpgradeSlot slot)
    {
        slot.button?.GetComponent<RectTransform>()
            ?.DOShakeAnchorPos(ShakeDuration, ShakeStrength, ShakeVibrato, ShakeRandomness);
    }
}
