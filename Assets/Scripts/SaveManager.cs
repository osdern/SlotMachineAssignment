using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Flat data container for everything that must survive a scene reload.
/// Serialized to JSON and stored in PlayerPrefs under SaveManager.SaveKey.
/// </summary>
[System.Serializable]
public class GameSaveData
{
    // MoneyManager
    public int balance = 0;

    // SlotGameConfig — luck values
    public float sevenLuck  = 0.08f;
    public float cherryLuck = 0.45f;
    public float bellLuck   = 0.22f;
    public float barLuck    = 0.15f;

    // SlotGameConfig — timing & gambling
    public float spinDuration  = 5f;
    public float gamblingMoney = 10f;

    // Upgrade tiers
    public int luckTier        = 0;
    public int moneyTier       = 0;
    public int timeTier        = 0;

    // Per-symbol multiplier tiers (Cherry=0, Bell=1, Bar=2, Seven=3)
    public int[] multiplierTiers = { 0, 0, 0, 0 };

    // Per-symbol multiplier values (2-match then 3-match, interleaved per symbol)
    // Layout: [cherry2, cherry3, bell2, bell3, bar2, bar3, seven2, seven3]
    public float[] multiplierValues = { 1.5f, 3f, 2.5f, 6f, 4f, 10f, 7.5f, 25f };

    // Audio
    public float masterVolume = 1f;

    // Story
    public bool storyShown = false;
}

/// <summary>
/// Singleton that owns the single GameSaveData instance.
/// Call SaveManager.Instance.Save() whenever state changes.
/// Call SaveManager.Instance.Load() once when a scene starts.
/// All systems read from and write to SaveManager.Instance.Data directly.
/// </summary>
[DefaultExecutionOrder(-100)]
public class SaveManager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    public const string SaveKey = "SlotMachineSave";

    // -------------------------------------------------------------------------
    // Singleton
    // -------------------------------------------------------------------------

    public static SaveManager Instance { get; private set; }

    // -------------------------------------------------------------------------
    // Public state
    // -------------------------------------------------------------------------

    /// <summary>The active save data. Populated by Load() on Awake.</summary>
    public GameSaveData Data { get; private set; }

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Must be a root object before DontDestroyOnLoad,
            // otherwise Unity promotes the entire parent hierarchy.
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        Load();
    }

    // Wipe the save when the browser tab is closed or the page is reloaded.
    // OnApplicationQuit is called reliably in WebGL on page unload.
    private void OnApplicationQuit()
    {
        DeleteSave();
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Serializes the current Data to PlayerPrefs.</summary>
    public void Save()
    {
        string json = JsonUtility.ToJson(Data);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Deserializes Data from PlayerPrefs.
    /// If no save exists, Data is initialized with default values.
    /// </summary>
    public void Load()
    {
        if (PlayerPrefs.HasKey(SaveKey))
        {
            string json = PlayerPrefs.GetString(SaveKey);
            Data = JsonUtility.FromJson<GameSaveData>(json);
        }
        else
        {
            Data = new GameSaveData();
        }
    }

    /// <summary>Wipes the saved data and resets to defaults.</summary>
    public void DeleteSave()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        Data = new GameSaveData();
    }

    /// <summary>
    /// Wipes all saved data and reloads the main menu scene from scratch.
    /// Wire this to a Reset button in the pause menu.
    /// </summary>
    public void ResetGame(string mainMenuSceneName = "MainMenu")
    {
        DeleteSave();
        Time.timeScale = 1f;
        AudioManager.Instance?.ResumeAudio();
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
