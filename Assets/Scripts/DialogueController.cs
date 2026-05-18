using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Drives the intro story sequence between the Thug and the Kid.
///
/// Flow:
///   1. Words are revealed one at a time with a configurable per-word delay.
///   2. Space (first press while typing) → dumps the full sentence instantly.
///   3. Space (second press after sentence is complete) → advances to the next line.
///   4. After the last line a configurable pause plays, then 50 coins are added
///      via MoneyManager and the Story Panel hides.
/// </summary>
public class DialogueController : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    private const float WordDelay      = 0.1f;
    private const float SentencePause  = 0.5f;
    private const int   EndBonusCoins  = 50;

    // -------------------------------------------------------------------------
    // Inspector fields
    // -------------------------------------------------------------------------

    [Header("Panels & Characters")]
    [Tooltip("The root Story Panel GameObject that contains everything.")]
    [SerializeField] private GameObject _storyPanel;

    [Tooltip("The Thug character GameObject (image + text box).")]
    [SerializeField] private GameObject _thugObject;

    [Tooltip("The Kid character GameObject (image + text box).")]
    [SerializeField] private GameObject _kidObject;

    [Header("Text Labels")]
    [Tooltip("The TMP label inside Thug > Text Box.")]
    [SerializeField] private TextMeshProUGUI _thugText;

    [Tooltip("The TMP label inside Kid > Text Box.")]
    [SerializeField] private TextMeshProUGUI _kidText;

    [Header("UI")]
    [Tooltip("The hint label shown at the bottom ('SPACE to skip / advance').")]
    [SerializeField] private TextMeshProUGUI _skipHintText;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    /// <summary>All dialogue lines in order.</summary>
    private readonly DialogueLine[] _lines = new DialogueLine[]
    {
        new DialogueLine(Speaker.Thug,
            "I want my money back kid, i dont care if your father died or not. " +
            "I want my 1500000 back each and every penny of it."),

        new DialogueLine(Speaker.Kid,
            "Sorry but i dont have a dime on me right now..."),

        new DialogueLine(Speaker.Thug,
            "today i m feeling generous, take this 50 coins and give me back everything with 30% intreset u peasant."),

        new DialogueLine(Speaker.Kid,
            "i need to win lottery to pay all of this money....wait i know what to do...."),
    };

    private int  _lineIndex;
    private bool _isTyping;
    private bool _skipRequested;
    private bool _waitingForAdvance;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Start()
    {
        // Skip the story entirely if it has already been seen.
        if (SaveManager.Instance != null && SaveManager.Instance.Data.storyShown)
        {
            if (_storyPanel != null) _storyPanel.SetActive(false);
            return;
        }

        // Hide both characters; the panel itself should already be active.
        SetSpeaker(Speaker.None);
        ClearTexts();
        StartCoroutine(PlaySequence());
    }

    private void Update()
    {
        if (!(Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)) return;

        if (_isTyping)
        {
            // First space press: skip word-by-word and reveal full sentence.
            _skipRequested = true;
        }
        else if (_waitingForAdvance)
        {
            // Second space press: move to next sentence.
            _waitingForAdvance = false;
        }
    }

    // -------------------------------------------------------------------------
    // Sequence coroutine
    // -------------------------------------------------------------------------

    private IEnumerator PlaySequence()
    {
        for (_lineIndex = 0; _lineIndex < _lines.Length; _lineIndex++)
        {
            DialogueLine line = _lines[_lineIndex];

            SetSpeaker(line.Speaker);
            TextMeshProUGUI label = line.Speaker == Speaker.Thug ? _thugText : _kidText;

            yield return StartCoroutine(TypeLine(label, line.Text));

            // Pause between sentences.
            yield return new WaitForSeconds(SentencePause);

            // Wait until the player presses Space to advance.
            _waitingForAdvance = true;
            UpdateHint(advancing: true);
            yield return new WaitUntil(() => !_waitingForAdvance);
        }

        // All lines done — grant bonus coins, mark story as seen, then hide the panel.
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.AddMoney(EndBonusCoins);
        }

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Data.storyShown = true;
            SaveManager.Instance.Save();
        }

        if (_skipHintText != null) _skipHintText.gameObject.SetActive(false);

        yield return new WaitForSeconds(SentencePause);

        if (_storyPanel != null) _storyPanel.SetActive(false);
    }

    // -------------------------------------------------------------------------
    // Typing coroutine
    // -------------------------------------------------------------------------

    /// <summary>
    /// Reveals <paramref name="fullText"/> word by word into <paramref name="label"/>.
    /// Respects the skip flag set by the Space key.
    /// </summary>
    private IEnumerator TypeLine(TextMeshProUGUI label, string fullText)
    {
        _isTyping      = true;
        _skipRequested = false;

        UpdateHint(advancing: false);
        label.text = string.Empty;

        string[] words = fullText.Split(' ');

        for (int i = 0; i < words.Length; i++)
        {
            if (_skipRequested)
            {
                label.text = fullText;
                break;
            }

            // Append the next word (add a leading space after the first word).
            label.text += (i == 0 ? string.Empty : " ") + words[i];
            yield return new WaitForSeconds(WordDelay);
        }

        _isTyping      = false;
        _skipRequested = false;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>Shows the active speaker's GameObject, hides the other.</summary>
    private void SetSpeaker(Speaker speaker)
    {
        if (_thugObject != null) _thugObject.SetActive(speaker == Speaker.Thug);
        if (_kidObject  != null) _kidObject .SetActive(speaker == Speaker.Kid);
    }

    /// <summary>Clears both dialogue text labels.</summary>
    private void ClearTexts()
    {
        if (_thugText != null) _thugText.text = string.Empty;
        if (_kidText  != null) _kidText .text = string.Empty;
    }

    /// <summary>Updates the hint label depending on current state.</summary>
    private void UpdateHint(bool advancing)
    {
        if (_skipHintText == null) return;

        _skipHintText.text = advancing
            ? "[SPACE] to continue"
            : "[SPACE] to skip";
    }

    // -------------------------------------------------------------------------
    // Inner types
    // -------------------------------------------------------------------------

    private enum Speaker { None, Thug, Kid }

    private readonly struct DialogueLine
    {
        public readonly Speaker Speaker;
        public readonly string  Text;

        public DialogueLine(Speaker speaker, string text)
        {
            Speaker = speaker;
            Text    = text;
        }
    }
}
