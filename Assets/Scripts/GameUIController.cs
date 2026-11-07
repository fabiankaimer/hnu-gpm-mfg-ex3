using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

/// Verknüpft Startmenü, HUD, Pause-Menü und Game-Over-Screen.
/// Es gibt KEIN echtes Spiel: Der Punktestand läuft nur zur Demo hoch,
/// Leben werden über einen Test-Button abgezogen.
public class GameUIController : MonoBehaviour
{
    enum State { MainMenu, Playing, Paused, GameOver }

    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject hudPanel;
    public GameObject pausePanel;
    public GameObject gameOverPanel;

    [Header("HUD")]
    public Text scoreText;
    public Text finalScoreText;
    public Image[] hearts;

    [Header("Demo-Einstellungen")]
    public int maxLives = 3;
    public float tickInterval = 0.2f;   // alle 0,2 s +1 Punkt, solange gespielt wird

    State state;
    int score;
    int lives;
    float timer;

    static readonly Color HeartFull = new Color(0.95f, 0.25f, 0.30f, 1f);
    static readonly Color HeartEmpty = new Color(0.25f, 0.25f, 0.30f, 1f);

    void Start()
    {
        ShowMainMenu();
    }

    void Update()
    {
        // Android-Zurück-Taste (entspricht Escape)
        if (BackPressed())
        {
            if (state == State.Playing) PauseGame();
            else if (state == State.Paused) ResumeGame();
        }

        if (state == State.Playing)
        {
            timer += Time.deltaTime; // timeScale = 0 im Pausemenü -> Zähler steht
            while (timer >= tickInterval)
            {
                timer -= tickInterval;
                AddScore(1);
            }
        }
    }

    bool BackPressed()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Escape);
#elif ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return false;
#endif
    }

    // ---------- Zustandswechsel (werden von Buttons aufgerufen) ----------

    public void ShowMainMenu()
    {
        Time.timeScale = 1f;
        state = State.MainMenu;
        SetPanels(menu: true, hud: false, pause: false, over: false);
    }

    public void StartGame()
    {
        score = 0;
        lives = maxLives;
        timer = 0f;
        Time.timeScale = 1f;
        state = State.Playing;
        RefreshHud();
        SetPanels(menu: false, hud: true, pause: false, over: false);
    }

    public void PauseGame()
    {
        if (state != State.Playing) return;
        Time.timeScale = 0f;
        state = State.Paused;
        SetPanels(menu: false, hud: true, pause: true, over: false); // HUD bleibt hinter dem Dim sichtbar
    }

    public void ResumeGame()
    {
        if (state != State.Paused) return;
        Time.timeScale = 1f;
        state = State.Playing;
        SetPanels(menu: false, hud: true, pause: false, over: false);
    }

    public void RestartGame() => StartGame();

    public void GameOver()
    {
        Time.timeScale = 0f;
        state = State.GameOver;
        if (finalScoreText != null) finalScoreText.text = "Punkte: " + score;
        SetPanels(menu: false, hud: false, pause: false, over: true);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------- Demo-Buttons im HUD ----------

    public void AddPoints() => AddScore(10);

    public void TakeHit()
    {
        if (state != State.Playing) return;
        lives = Mathf.Max(0, lives - 1);
        RefreshHud();
        if (lives <= 0) GameOver();
    }

    // ---------- intern ----------

    void AddScore(int amount)
    {
        score += amount;
        RefreshHud();
    }

    void RefreshHud()
    {
        if (scoreText != null) scoreText.text = "Punkte: " + score;
        if (hearts == null) return;
        for (int i = 0; i < hearts.Length; i++)
            hearts[i].color = i < lives ? HeartFull : HeartEmpty;
    }

    void SetPanels(bool menu, bool hud, bool pause, bool over)
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(menu);
        if (hudPanel) hudPanel.SetActive(hud);
        if (pausePanel) pausePanel.SetActive(pause);
        if (gameOverPanel) gameOverPanel.SetActive(over);
    }

    void OnDestroy()
    {
        Time.timeScale = 1f; // nie mit eingefrorener Zeit zurücklassen
    }
}
