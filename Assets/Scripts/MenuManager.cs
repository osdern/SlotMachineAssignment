using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central manager for all menu state transitions.
///
/// Panels are assigned in the Inspector. Each public method can be wired
/// directly to a UI Button's OnClick event without any extra glue code.
///
/// Panel visibility rules:
///   Pause()         → show Pause Menu panel, freeze time
///   Resume()        → hide Pause Menu panel, restore time
///   Settings()      → hide Pause Menu panel, show Settings panel
///   SettingsClose() → hide Settings panel, show Pause Menu panel
///   MainMenu()      → load the main menu scene (restores time first)
///   Play()          → load the gameplay scene (or unpause if already loaded)
/// </summary>
public class MenuManager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Singleton
    // -------------------------------------------------------------------------

    public static MenuManager Instance { get; private set; }

    // -------------------------------------------------------------------------
    // Inspector fields
    // -------------------------------------------------------------------------

    [Header("Panels")]
    [Tooltip("The panel shown when the game is paused.")]
    [SerializeField] private GameObject _pauseMenuPanel;

    [Tooltip("The settings panel shown when the player opens settings from pause.")]
    [SerializeField] private GameObject _settingsPanel;

    [Header("Scene Names")]
    [Tooltip("Exact name of the main menu scene as it appears in Build Settings.")]
    [SerializeField] private string _mainMenuSceneName = "MainMenu";

    [Tooltip("Exact name of the gameplay scene as it appears in Build Settings.")]
    [SerializeField] private string _gameplaySceneName = "MainScene";

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

        // Ensure both panels start hidden and time is running.
        SetPanel(_pauseMenuPanel, false);
        SetPanel(_settingsPanel, false);
        Time.timeScale = 1f;
    }

    // -------------------------------------------------------------------------
    // Public API — wire these to Button OnClick events in the Inspector
    // -------------------------------------------------------------------------

    /// <summary>
    /// Pauses the game and shows the Pause Menu panel.
    /// </summary>
    public void Pause()
    {
        SetPanel(_pauseMenuPanel, true);
        SetPanel(_settingsPanel, false);
        Time.timeScale = 0f;
    }

    /// <summary>
    /// Resumes the game and hides the Pause Menu panel.
    /// </summary>
    public void Resume()
    {
        SetPanel(_pauseMenuPanel, false);
        Time.timeScale = 1f;
    }

    /// <summary>
    /// Opens the Settings panel and hides the Pause Menu panel.
    /// </summary>
    public void Settings()
    {
        SetPanel(_pauseMenuPanel, false);
        SetPanel(_settingsPanel, true);
    }

    /// <summary>
    /// Closes the Settings panel and returns to the Pause Menu panel.
    /// </summary>
    public void SettingsClose()
    {
        SetPanel(_settingsPanel, false);
        SetPanel(_pauseMenuPanel, true);
    }

    /// <summary>
    /// Restores time and loads the main menu scene.
    /// </summary>
    public void MainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(_mainMenuSceneName);
    }

    /// <summary>
    /// Restores time and loads (or reloads) the gameplay scene.
    /// </summary>
    public void Play()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(_gameplaySceneName);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Sets a panel's active state. Logs a warning if the panel reference is missing.
    /// </summary>
    private void SetPanel(GameObject panel, bool active)
    {
        if (panel == null)
        {
            Debug.LogWarning("[MenuManager]: A panel reference is not assigned in the Inspector.", this);
            return;
        }

        panel.SetActive(active);
    }
}
