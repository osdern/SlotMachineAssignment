using UnityEngine;
using System.Collections;

/// <summary>
/// Handles only the visual coin-flight animation.
/// Money balance is managed exclusively by MoneyManager.
/// </summary>
public class CoinAnimation : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    private const float DefaultArcHeight = 100f;
    private const float ScaleInThreshold = 0.3f;
    private const float ScaleOutThreshold = 0.7f;
    private const float MinScale = 0.5f;

    /// <summary>
    /// Max fraction of flightDuration that the staggered spawning window may occupy.
    /// Keeps each coin's individual flight time meaningful even with many coins.
    /// </summary>
    private const float MaxSpawnWindowFraction = 0.4f;

    // -------------------------------------------------------------------------
    // Inspector fields
    // -------------------------------------------------------------------------

    [Header("References")]
    [Tooltip("UI Image prefab used for the flying coin.")]
    public GameObject coinPrefab;

    [Tooltip("RectTransform of the coin counter UI element (animation destination).")]
    public RectTransform coinDestination;

    [Header("Animation Settings")]
    [Tooltip("Total time in seconds by which every coin in the batch must reach the destination.")]
    public float flightDuration = 1f;
    public float rotationSpeed = 360f;
    public float spawnOffset = 50f;
    public float coinSize = 1f;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    public RectTransform canvasRect;
    private int _activeCoinAnimations;

    // -------------------------------------------------------------------------
    // Singleton
    // -------------------------------------------------------------------------

    public static CoinAnimation Instance { get; private set; }

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

        canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Spawns <paramref name="coinAmount"/> animated coins flying from
    /// <paramref name="spawnPosition"/> to the coin destination.
    /// </summary>
    public void SpawnCoins(Vector2 spawnPosition, int coinAmount = 1)
    {
        StartCoroutine(SpawnCoinAnimation(spawnPosition, coinAmount));
    }

    /// <summary>Returns true while any coin flight animation is still running.</summary>
    public bool AreCoinsAnimating()
    {
        return _activeCoinAnimations > 0;
    }

    /// <summary>Yields until all in-flight coin animations have finished.</summary>
    public IEnumerator WaitForCoinAnimations()
    {
        while (_activeCoinAnimations > 0)
        {
            yield return null;
        }
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private IEnumerator SpawnCoinAnimation(Vector2 spawnPosition, int coinAmount)
    {
        // Spread spawning over at most MaxSpawnWindowFraction of flightDuration so
        // every coin still has enough flight time to animate meaningfully.
        float spawnDelay = coinAmount > 1
            ? (flightDuration * MaxSpawnWindowFraction) / (coinAmount - 1)
            : 0f;

        for (int i = 0; i < coinAmount; i++)
        {
            if (i > 0)
            {
                yield return new WaitForSecondsRealtime(spawnDelay);
            }

            // Each coin's individual flight time shrinks by how long we waited,
            // so all coins arrive at the destination at t = flightDuration.
            float coinFlightDuration = flightDuration - i * spawnDelay;
            StartCoroutine(AnimateSingleCoin(spawnPosition, coinFlightDuration));
        }
    }

    private IEnumerator AnimateSingleCoin(Vector2 spawnPosition, float duration)
    {
        _activeCoinAnimations++;

        GameObject coin = Instantiate(coinPrefab, canvasRect.transform);
        RectTransform coinRect = coin.GetComponent<RectTransform>();
        coinRect.localScale = Vector3.one * coinSize;

        Vector2 startPos = spawnPosition + Random.insideUnitCircle * spawnOffset;
        coinRect.anchoredPosition = startPos;

        Vector2 endPos = coinDestination.anchoredPosition;

        float elapsedTime = 0f;

        while (elapsedTime < duration && coin != null)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = elapsedTime / duration;
            float curvedProgress = Mathf.SmoothStep(0f, 1f, progress);

            Vector2 currentPos = Vector2.Lerp(startPos, endPos, curvedProgress);
            currentPos.y += Mathf.Sin(curvedProgress * Mathf.PI) * DefaultArcHeight;
            coinRect.anchoredPosition = currentPos;

            coinRect.Rotate(0f, 0f, rotationSpeed * Time.unscaledDeltaTime);

            float scale = 1f;
            if (progress < ScaleInThreshold)
            {
                scale = Mathf.Lerp(MinScale, 1f, progress / ScaleInThreshold);
            }
            else if (progress > ScaleOutThreshold)
            {
                scale = Mathf.Lerp(1f, MinScale, (progress - ScaleOutThreshold) / (1f - ScaleOutThreshold));
            }
            coinRect.localScale = Vector3.one * (scale * coinSize);

            yield return null;
        }

        if (coin != null)
        {
            Destroy(coin);
        }

        _activeCoinAnimations--;
    }
}
