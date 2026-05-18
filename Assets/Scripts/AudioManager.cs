using UnityEngine;

/// <summary>
/// Singleton audio manager for the slot machine.
/// Owns three dedicated AudioSources so sounds never interrupt each other.
///
/// Assign the three clips in the Inspector, then call:
///   AudioManager.Instance.PlayHandle()
///   AudioManager.Instance.PlayReel()
///   AudioManager.Instance.StopReel()
///   AudioManager.Instance.PlayReward()
/// </summary>
public class AudioManager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Singleton
    // -------------------------------------------------------------------------

    public static AudioManager Instance { get; private set; }

    // -------------------------------------------------------------------------
    // Inspector fields
    // -------------------------------------------------------------------------

    [Header("Clips")]
    [Tooltip("Played once when the handle is pressed.")]
    [SerializeField] private AudioClip _handleClip;

    [Tooltip("Looped while the reels are rolling.")]
    [SerializeField] private AudioClip _reelClip;

    [Tooltip("Played once when the player wins a reward.")]
    [SerializeField] private AudioClip _rewardClip;

    [Header("Volumes")]
    [SerializeField] [Range(0f, 1f)] private float _handleVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float _reelVolume   = 0.7f;
    [SerializeField] [Range(0f, 1f)] private float _rewardVolume = 1f;

    // Master volume scalar applied on top of per-channel volumes.
    private float _masterVolume = 1f;

    // -------------------------------------------------------------------------
    // Private state — one AudioSource per channel
    // -------------------------------------------------------------------------

    private AudioSource _handleSource;
    private AudioSource _reelSource;
    private AudioSource _rewardSource;

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

        if (SaveManager.Instance != null)
        {
            _masterVolume = SaveManager.Instance.Data.masterVolume;
        }

        _handleSource = AddSource(loop: false);
        _reelSource   = AddSource(loop: true);
        _rewardSource = AddSource(loop: false);
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Plays the handle click sound once.</summary>
    public void PlayHandle()
    {
        PlayOneShot(_handleSource, _handleClip, _handleVolume * _masterVolume);
    }

    /// <summary>Starts looping the reel rolling sound.</summary>
    public void PlayReel()
    {
        if (_reelSource == null || _reelClip == null) return;

        if (_reelSource.isPlaying) return;

        _reelSource.clip   = _reelClip;
        _reelSource.volume = _reelVolume * _masterVolume;
        _reelSource.Play();
    }

    /// <summary>Stops the reel rolling loop.</summary>
    public void StopReel()
    {
        if (_reelSource != null && _reelSource.isPlaying)
        {
            _reelSource.Stop();
        }
    }

    /// <summary>Plays the reward fanfare sound once.</summary>
    public void PlayReward()
    {
        PlayOneShot(_rewardSource, _rewardClip, _rewardVolume * _masterVolume);
    }

    /// <summary>
    /// Sets the master volume scalar (0–1) and immediately applies it to all active sources.
    /// Wire this to a UI Slider's OnValueChanged event.
    /// </summary>
    public void SetVolume(float volume)
    {
        _masterVolume = Mathf.Clamp01(volume);

        if (_handleSource != null) _handleSource.volume = _handleVolume * _masterVolume;
        if (_reelSource   != null) _reelSource.volume   = _reelVolume   * _masterVolume;
        if (_rewardSource != null) _rewardSource.volume = _rewardVolume  * _masterVolume;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Data.masterVolume = _masterVolume;
            SaveManager.Instance.Save();
        }
    }

    /// <summary>Pauses all audio output globally.</summary>
    public void PauseAudio()
    {
        AudioListener.pause = true;
    }

    /// <summary>Resumes all audio output globally.</summary>
    public void ResumeAudio()
    {
        AudioListener.pause = false;
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>Creates and configures a new AudioSource component on this GameObject.</summary>
    private AudioSource AddSource(bool loop)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.loop        = loop;
        source.playOnAwake = false;
        return source;
    }

    /// <summary>Plays a clip once through the given source, replacing any current one-shot.</summary>
    private static void PlayOneShot(AudioSource source, AudioClip clip, float volume)
    {
        if (source == null || clip == null) return;

        source.Stop();
        source.clip   = clip;
        source.volume = volume;
        source.Play();
    }
}
