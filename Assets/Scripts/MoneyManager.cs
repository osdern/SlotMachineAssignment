using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Single source of truth for the player's money balance.
/// Handles reading, writing, and animating the Money label.
/// </summary>
public class MoneyManager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    private const float MoneyAnimDuration = 1f;

    // -------------------------------------------------------------------------
    // Singleton
    // -------------------------------------------------------------------------

    public static MoneyManager Instance { get; private set; }

    // -------------------------------------------------------------------------
    // Inspector fields
    // -------------------------------------------------------------------------

    [Header("References")]
    [Tooltip("The TextMeshProUGUI label that displays the current balance.")]
    [SerializeField] private TextMeshProUGUI _moneyText;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private int _currentBalance;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Auto-find Money label if not assigned in the Inspector
        if (_moneyText == null)
        {
            GameObject moneyObj = GameObject.Find("Money");

            if (moneyObj != null)
            {
                _moneyText = moneyObj.GetComponent<TextMeshProUGUI>();
            }

            if (_moneyText == null)
            {
                Debug.LogError("[MoneyManager]: Money TextMeshProUGUI not found.", this);
            }
        }

        // Seed balance from whatever the label currently shows
        if (_moneyText != null && int.TryParse(_moneyText.text.Trim(), out int parsed))
        {
            _currentBalance = parsed;
        }
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Returns the current balance.</summary>
    public float GetBalance() => _currentBalance;

    /// <summary>
    /// Subtracts <paramref name="amount"/> from the balance (floor 0)
    /// and counts the label down over MoneyAnimDuration seconds.
    /// </summary>
    public void Deduct(float amount)
    {
        int from = _currentBalance;
        _currentBalance = Mathf.Max(0, _currentBalance - Mathf.RoundToInt(amount));
        int to = _currentBalance;

        StartCoroutine(AnimateMoney(from, to));
    }

    /// <summary>
    /// Adds <paramref name="amount"/> to the balance and counts the label
    /// up over MoneyAnimDuration seconds.
    /// </summary>
    public void AddMoney(int amount)
    {
        int from = _currentBalance;
        _currentBalance += amount;
        int to = _currentBalance;

        StartCoroutine(AnimateMoney(from, to));
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Counts the Money label from <paramref name="from"/> to <paramref name="to"/>
    /// one unit at a time, spread evenly over MoneyAnimDuration seconds.
    /// </summary>
    private IEnumerator AnimateMoney(int from, int to)
    {
        int steps = Mathf.Abs(to - from);

        if (steps == 0)
        {
            yield break;
        }

        int direction = to > from ? 1 : -1;
        float delay = MoneyAnimDuration / steps;
        int current = from;

        while (current != to)
        {
            current += direction;

            if (_moneyText != null)
            {
                _moneyText.text = current.ToString("0");
            }

            yield return new WaitForSeconds(delay);
        }
    }
}
