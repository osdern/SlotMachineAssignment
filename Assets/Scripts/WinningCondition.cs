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
    // Nested types
    // -------------------------------------------------------------------------

    /// <summary>
    /// Serializable payout entry for a single symbol.
    /// Visible and editable in the Inspector under "Payout Multipliers".
    /// </summary>
    [System.Serializable]
    public struct SymbolMultiplier
    {
        [Tooltip("The reel symbol this entry applies to.")]
        public ReelItemType symbol;

        [Tooltip("Reward multiplier when exactly 2 of this symbol appear.")]
        public float twoMatch;

        [Tooltip("Reward multiplier when all 3 symbols match.")]
        public float threeMatch;
    }

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

    [Header("Payout Multipliers")]
    [Tooltip("Per-symbol payout multipliers. Order must match SymbolIndex: Cherry, Bell, Bar, Seven.")]
    [SerializeField] private SymbolMultiplier[] _multipliers = new SymbolMultiplier[]
    {
        new SymbolMultiplier { symbol = ReelItemType.Cherry, twoMatch = 1.5f,  threeMatch = 3.0f  },
        new SymbolMultiplier { symbol = ReelItemType.Bell,   twoMatch = 2.5f,  threeMatch = 6.0f  },
        new SymbolMultiplier { symbol = ReelItemType.Bar,    twoMatch = 4.0f,  threeMatch = 10.0f },
        new SymbolMultiplier { symbol = ReelItemType.Seven,  twoMatch = 7.5f,  threeMatch = 25.0f },
    };

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

        AudioManager.Instance?.PlayReward();
        SpawnWinCoins(coins);
        MoneyManager.Instance?.AddMoney(coins);
    }

    /// <summary>
    /// Returns the current 2-match and 3-match multipliers for the given symbol.
    /// </summary>
    public (float twoMatch, float threeMatch) GetMultipliers(ReelItemType symbol)
    {
        int idx = SymbolIndex(symbol);
        return (_multipliers[idx].twoMatch, _multipliers[idx].threeMatch);
    }

    /// <summary>
    /// Overwrites the multipliers for the given symbol at runtime (e.g. from MultiplierUpgradeController).
    /// </summary>
    public void SetMultipliers(ReelItemType symbol, float twoMatch, float threeMatch)
    {
        int idx = SymbolIndex(symbol);
        _multipliers[idx].twoMatch   = twoMatch;
        _multipliers[idx].threeMatch = threeMatch;
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static int SymbolIndex(ReelItemType symbol)
    {
        return symbol switch
        {
            ReelItemType.Cherry => 0,
            ReelItemType.Bell   => 1,
            ReelItemType.Bar    => 2,
            ReelItemType.Seven  => 3,
            _                   => 0
        };
    }

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
        int idx = SymbolIndex(symbol);
        return matchCount == 3 ? _multipliers[idx].threeMatch : _multipliers[idx].twoMatch;
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
