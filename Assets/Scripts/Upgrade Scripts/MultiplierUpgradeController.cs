using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles reward-multiplier upgrades for all four reel symbols.
/// Each symbol has its own independent upgrade slot in the upgrade menu.
///
/// There are 4 upgrade tiers per symbol. At each tier both the 2-match
/// and 3-match multipliers are doubled relative to their base values.
///
/// Base multipliers (from WinningCondition defaults):
///   Cherry : 1.5× / 3.0×
///   Bell   : 2.5× / 6.0×
///   Bar    : 4.0× / 10.0×
///   Seven  : 7.5× / 25.0×
///
/// Cost tables (multiplied by 3 at each successive tier):
///   Cherry :   500 → 1 500 → 4 500 → 13 500
///   Bell   : 1 000 → 3 000 → 9 000 → 27 000
///   Bar    : 2 000 → 6 000 → 18 000 → 54 000
///   Seven  : 4 000 → 12 000 → 36 000 → 108 000
/// </summary>
public class MultiplierUpgradeController : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Tier data
    // -------------------------------------------------------------------------

    private const int MaxTiers = 4;
    private const float CostMultiplier = 3f;

    private static readonly float[] BaseCosts =
    {
        500f,   // Cherry
        1000f,  // Bell
        2000f,  // Bar
        4000f,  // Seven
    };

    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    private const float ShakeDuration   = 0.4f;
    private const float ShakeStrength   = 12f;
    private const int   ShakeVibrato    = 20;
    private const float ShakeRandomness = 45f;

    // -------------------------------------------------------------------------
    // Nested data — one per symbol slot
    // -------------------------------------------------------------------------

    [System.Serializable]
    public class SymbolUpgradeSlot
    {
        [Tooltip("Which reel symbol this slot upgrades.")]
        public ReelItemType symbol;

        [Header("UI")]
        [Tooltip("Button that triggers this symbol's upgrade.")]
        public Button button;

        [Tooltip("Cost label on the button.")]
        public TextMeshProUGUI costText;

        [Tooltip("Text showing the current multiplier tier.")]
        public TextMeshProUGUI currentText;

        [Tooltip("Arrow GameObject hidden when maxed.")]
        public GameObject arrowObject;

        [Tooltip("Text previewing the next multiplier tier.")]
        public TextMeshProUGUI upgradedText;

        /// <summary>Current upgrade tier (0 = base, MaxTiers = fully upgraded).</summary>
        [System.NonSerialized] public int CurrentTier;
    }

    // -------------------------------------------------------------------------
    // Inspector fields
    // -------------------------------------------------------------------------

    [Header("References")]
    [Tooltip("WinningCondition that owns the runtime multiplier table.")]
    [SerializeField] private WinningCondition _winningCondition;

    [Header("Upgrade Slots")]
    [Tooltip("One entry per symbol — Cherry, Bell, Bar, Seven in any order.")]
    [SerializeField] private SymbolUpgradeSlot[] _slots;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (_winningCondition == null)
        {
            _winningCondition = FindFirstObjectByType<WinningCondition>();
        }

        if (_winningCondition == null)
        {
            Debug.LogError("[MultiplierUpgradeController]: No WinningCondition found in the scene.", this);
            return;
        }

        for (int i = 0; i < _slots.Length; i++)
        {
            // Capture index for the lambda closure.
            int captured = i;
            SymbolUpgradeSlot slot = _slots[i];

            if (slot.button != null)
            {
                slot.button.onClick.AddListener(() => OnUpgradeClicked(captured));
            }
            else
            {
                Debug.LogWarning($"[MultiplierUpgradeController]: Slot {i} ({slot.symbol}) has no Button assigned.", this);
            }

            RestoreSlot(slot, i);
            RefreshSlotUI(slot);
        }
    }

    private void OnDestroy()
    {
        if (_slots == null)
        {
            return;
        }

        foreach (SymbolUpgradeSlot slot in _slots)
        {
            slot.button?.onClick.RemoveAllListeners();
        }
    }

    // -------------------------------------------------------------------------
    // Button handler
    // -------------------------------------------------------------------------

    /// <summary>
    /// Checks whether the player can afford the upgrade for the given slot index.
    /// Applies the upgrade and deducts money on success; shakes the button on failure.
    /// </summary>
    private void OnUpgradeClicked(int slotIndex)
    {
        SymbolUpgradeSlot slot = _slots[slotIndex];

        if (slot.CurrentTier >= MaxTiers)
        {
            return;
        }

        float cost = GetCostForTier(slot.symbol, slot.CurrentTier);

        if (MoneyManager.Instance == null || MoneyManager.Instance.GetBalance() < cost)
        {
            ShakeButton(slot);
            return;
        }

        MoneyManager.Instance.Deduct(cost);
        ApplyMultiplierUpgrade(slot);
        slot.CurrentTier++;

        PersistSlot(slot, slotIndex);

        RefreshSlotUI(slot);
    }

    // -------------------------------------------------------------------------
    // Core multiplier logic
    // -------------------------------------------------------------------------

    /// <summary>
    /// Doubles both the 2-match and 3-match multipliers for the slot's symbol.
    /// </summary>
    private void ApplyMultiplierUpgrade(SymbolUpgradeSlot slot)
    {
        (float twoMatch, float threeMatch) = _winningCondition.GetMultipliers(slot.symbol);
        _winningCondition.SetMultipliers(slot.symbol, twoMatch * 2f, threeMatch * 2f);
    }

    // -------------------------------------------------------------------------
    // Save / load helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Maps a ReelItemType to its canonical index used in the save arrays.
    /// Cherry=0, Bell=1, Bar=2, Seven=3 — matches WinningCondition.SymbolIndex.
    /// </summary>
    private static int SymbolSaveIndex(ReelItemType symbol) => symbol switch
    {
        ReelItemType.Cherry => 0,
        ReelItemType.Bell   => 1,
        ReelItemType.Bar    => 2,
        ReelItemType.Seven  => 3,
        _                   => 0
    };

    /// <summary>
    /// Restores tier and multiplier values for a slot from save data.
    /// Keyed by symbol so Inspector slot order does not affect correctness.
    /// </summary>
    private void RestoreSlot(SymbolUpgradeSlot slot, int index)
    {
        if (SaveManager.Instance == null) return;

        GameSaveData data      = SaveManager.Instance.Data;
        int          saveIndex = SymbolSaveIndex(slot.symbol);

        if (saveIndex < data.multiplierTiers.Length)
        {
            slot.CurrentTier = data.multiplierTiers[saveIndex];
        }

        int valueBase = saveIndex * 2;
        if (valueBase + 1 < data.multiplierValues.Length)
        {
            _winningCondition.SetMultipliers(
                slot.symbol,
                data.multiplierValues[valueBase],
                data.multiplierValues[valueBase + 1]);
        }
    }

    /// <summary>Writes a slot's tier and current multiplier values to save data.</summary>
    private void PersistSlot(SymbolUpgradeSlot slot, int index)
    {
        if (SaveManager.Instance == null) return;

        GameSaveData data      = SaveManager.Instance.Data;
        int          saveIndex = SymbolSaveIndex(slot.symbol);

        if (saveIndex < data.multiplierTiers.Length)
        {
            data.multiplierTiers[saveIndex] = slot.CurrentTier;
        }

        (float twoMatch, float threeMatch) = _winningCondition.GetMultipliers(slot.symbol);
        int valueBase = saveIndex * 2;
        if (valueBase + 1 < data.multiplierValues.Length)
        {
            data.multiplierValues[valueBase]     = twoMatch;
            data.multiplierValues[valueBase + 1] = threeMatch;
        }

        SaveManager.Instance.Save();
    }

    // -------------------------------------------------------------------------
    // UI helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Refreshes the cost, current, arrow, and upgraded labels for one slot.
    /// When maxed: current shows "MAX", upgraded text and arrow are hidden.
    /// </summary>
    private void RefreshSlotUI(SymbolUpgradeSlot slot)
    {
        bool isMaxed = slot.CurrentTier >= MaxTiers;

        (float twoMatch, float threeMatch) = _winningCondition.GetMultipliers(slot.symbol);

        if (slot.currentText != null)
        {
            slot.currentText.text = isMaxed
                ? "MAX"
                : FormatMultiplier(twoMatch, threeMatch);
        }

        if (slot.upgradedText != null)
        {
            slot.upgradedText.gameObject.SetActive(!isMaxed);

            if (!isMaxed)
            {
                slot.upgradedText.text = FormatMultiplier(twoMatch * 2f, threeMatch * 2f);
            }
        }

        if (slot.arrowObject != null)
        {
            slot.arrowObject.SetActive(!isMaxed);
        }

        if (slot.costText != null)
        {
            slot.costText.text = isMaxed
                ? "MAX"
                : $"{GetCostForTier(slot.symbol, slot.CurrentTier):0}";
        }

        if (slot.button != null)
        {
            slot.button.interactable = !isMaxed;
        }
    }

    /// <summary>
    /// Returns the upgrade cost for a given symbol at a given tier index.
    /// Cost = baseCost × 3^tier.
    /// </summary>
    private static float GetCostForTier(ReelItemType symbol, int tier)
    {
        float baseCost = symbol switch
        {
            ReelItemType.Cherry => BaseCosts[0],
            ReelItemType.Bell   => BaseCosts[1],
            ReelItemType.Bar    => BaseCosts[2],
            ReelItemType.Seven  => BaseCosts[3],
            _                   => 0f
        };

        return baseCost * Mathf.Pow(CostMultiplier, tier);
    }

    /// <summary>Formats two multiplier values into a compact display string.</summary>
    private static string FormatMultiplier(float twoMatch, float threeMatch)
    {
        return $"{twoMatch:0.#}x   {threeMatch:0.#}x";
    }

    /// <summary>
    /// Shakes the slot's button RectTransform to indicate insufficient funds.
    /// </summary>
    private static void ShakeButton(SymbolUpgradeSlot slot)
    {
        if (slot.button == null)
        {
            return;
        }

        slot.button.GetComponent<RectTransform>()
            ?.DOShakeAnchorPos(ShakeDuration, ShakeStrength, ShakeVibrato, ShakeRandomness);
    }
}
