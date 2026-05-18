using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Binds the sound Slider to AudioManager.SetVolume so the master volume
/// updates in real-time as the player drags the handle.
/// Attach this component to the Sound Slider GameObject.
/// </summary>
[RequireComponent(typeof(Slider))]
public class SoundSliderController : MonoBehaviour
{
    private Slider _slider;

    private void Awake()
    {
        _slider = GetComponent<Slider>();
    }

    private void Start()
    {
        // Restore slider position from save, then sync AudioManager.
        if (SaveManager.Instance != null)
        {
            _slider.SetValueWithoutNotify(SaveManager.Instance.Data.masterVolume);
        }

        OnSliderChanged(_slider.value);

        _slider.onValueChanged.AddListener(OnSliderChanged);
    }

    private void OnDestroy()
    {
        _slider.onValueChanged.RemoveListener(OnSliderChanged);
    }

    /// <summary>Forwards the slider value to AudioManager as the master volume.</summary>
    private void OnSliderChanged(float value)
    {
        AudioManager.Instance?.SetVolume(value);
    }
}
