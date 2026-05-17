using UnityEngine;

/// <summary>
/// Evaluates the win condition after each spin ends.
/// Reads the ReelItemType of the 2nd child (index 1) in each slot and
/// awards coins based on the payout table scaled to the current GamblingMoney.
///
/// Payout multipliers (relative to a base cost of 10):
///   Symbol   2-match   3-match
///   Cherry     1.5×      3.0×
///   Bell       2.5×      6.0×
///   Bar        4.0×     10.0×
///   Seven      7.5×     25.0×
///
/// Call Evaluate() once all reels have come to rest.
/// </summary>
public class WinningCondition : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    private const int SlotSymbolIndex = 1;
    private const float BaseCost = 10f;

    // -------------------------------------------------------------------------
    // Inspector fields
    // -------------------------------------------------------------------------

    [Header("Slot Parents")]
    [Tooltip("Assign the three slot parent GameObjects in order (Slot 1, Slot 1(1), Slot 1(2)).")]
    [SerializeField] private Transform[] _slots = new Transform[3];

    [Header("Coin Spawn")]
    [Tooltip("The RectTransform from which winning coins will be spawned.")]
    [SerializeField] private RectTransform _coinSpawnOrigin;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private SlotGameConfig _config;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        _config = FindFirstObjectByType<SlotGameConfig>();

        if (_config == null)
        {
            Debug.LogError("[WinningCondition]: No SlotGameConfig found in the scene.", this);
        }
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Reads the symbol at position index 1 of each slot and evaluates the
    /// win condition. Spawns coins and adds reward to the balance if won.
    /// Call this once all reels have stopped.
    /// </summary>
    public void Evaluate()
    {
        if (_slots == null || _slots.Length < 3)
        {
            Debug.LogError("[WinningCondition]: Slots array must have exactly 3 entries.", this);
            return;
        }

        ReelItemType?[] symbols = new ReelItemType?[3];

        for (int i = 0; i < 3; i++)
        {
            symbols[i] = GetSlotSymbol(_slots[i]);
        }

        for (int i = 0; i < 3; i++)
        {
            if (symbols[i] == null)
            {
                Debug.LogWarning($"[WinningCondition]: Could not read symbol from slot {i}. Aborting evaluation.");
                return;
            }
        }

        int matchCount = CountMatches(symbols[0].Value, symbols[1].Value, symbols[2].Value);
        ReelItemType dominantSymbol = GetDominantSymbol(symbols[0].Value, symbols[1].Value, symbols[2].Value);

        if (matchCount < 2)
        {
            Debug.Log("[WinningCondition]: No win.");
            return;
        }

        float gamblingMoney = _config != null ? _config.GamblingMoney : BaseCost;
        int coins = CalculateCoins(dominantSymbol, matchCount, gamblingMoney);

        Debug.Log($"[WinningCondition]: {matchCount}-match on {dominantSymbol} — awarding {coins} coins.");

        SpawnWinCoins(coins);
        MoneyManager.Instance?.AddMoney(coins);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private ReelItemType? GetSlotSymbol(Transform slot)
    {
        if (slot == null)
        {
            return null;
        }

        if (slot.childCount <= SlotSymbolIndex)
        {
            Debug.LogWarning($"[WinningCondition]: Slot '{slot.name}' has fewer than {SlotSymbolIndex + 1} children.");
            return null;
        }

        Transform child = slot.GetChild(SlotSymbolIndex);
        ReelMover mover = child.GetComponent<ReelMover>();

        if (mover == null)
        {
            Debug.LogWarning($"[WinningCondition]: Child at index {SlotSymbolIndex} in '{slot.name}' has no ReelMover.");
            return null;
        }

        return mover.ItemType;
    }

    private int CountMatches(ReelItemType a, ReelItemType b, ReelItemType c)
    {
        if (a == b && b == c)
        {
            return 3;
        }

        if (a == b || b == c || a == c)
        {
            return 2;
        }

        return 1;
    }

    private ReelItemType GetDominantSymbol(ReelItemType a, ReelItemType b, ReelItemType c)
    {
        if (a == b || a == c)
        {
            return a;
        }

        return b;
    }

    private int CalculateCoins(ReelItemType symbol, int matchCount, float gamblingMoney)
    {
        float multiplier = GetMultiplier(symbol, matchCount);
        float scaled = (gamblingMoney / BaseCost) * multiplier * BaseCost;
        return Mathf.RoundToInt(scaled);
    }

    private float GetMultiplier(ReelItemType symbol, int matchCount)
    {
        return symbol switch
        {
            ReelItemType.Cherry => matchCount == 3 ? 3.0f  : 1.5f,
            ReelItemType.Bell   => matchCount == 3 ? 6.0f  : 2.5f,
            ReelItemType.Bar    => matchCount == 3 ? 10.0f : 4.0f,
            ReelItemType.Seven  => matchCount == 3 ? 25.0f : 7.5f,
            _                   => 0f
        };
    }

    private void SpawnWinCoins(int coins)
    {
        if (CoinAnimation.Instance == null)
        {
            Debug.LogError("[WinningCondition]: CoinAnimation instance not found.");
            return;
        }

        Vector2 spawnPosition = Vector2.zero;

        if (_coinSpawnOrigin != null)
        {
            spawnPosition = _coinSpawnOrigin.anchoredPosition;
        }
        else
        {
            // Fallback to middle slot child if no origin assigned
            Transform middleSlot = _slots.Length > 1 ? _slots[1] : _slots[0];
            if (middleSlot != null && middleSlot.childCount > SlotSymbolIndex)
            {
                RectTransform rt = middleSlot.GetChild(SlotSymbolIndex).GetComponent<RectTransform>();
                if (rt != null)
                {
                    spawnPosition = rt.anchoredPosition;
                }
            }
        }

        CoinAnimation.Instance.SpawnCoins(spawnPosition, coins);
    }
}
