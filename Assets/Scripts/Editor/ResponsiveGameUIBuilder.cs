using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// Menü: Tools > Mobile UI Demo > Build UI in current scene
/// Erzeugt Canvas (Canvas Scaler), vier Screens mit Safe Area und verdrahtet alle Buttons.
/// Muss im Ordner "Editor" liegen.
public static class ResponsiveGameUIBuilder
{
    static readonly Color BgColor = new Color(0.07f, 0.09f, 0.16f, 1f);
    static readonly Color DimColor = new Color(0f, 0f, 0f, 0.65f);
    static readonly Color WindowColor = new Color(0.12f, 0.15f, 0.25f, 1f);
    static readonly Color Blue = new Color(0.20f, 0.55f, 0.95f, 1f);
    static readonly Color Green = new Color(0.20f, 0.70f, 0.40f, 1f);
    static readonly Color Grey = new Color(0.35f, 0.38f, 0.48f, 1f);
    static readonly Color Red = new Color(0.85f, 0.25f, 0.30f, 1f);

    [MenuItem("Tools/Mobile UI Demo/Build UI in current scene")]
    public static void Build()
    {
        // ---------- Canvas + Canvas Scaler ----------
        var canvasGO = new GameObject("GameCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Build Mobile UI");

        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920); // Hochformat als Referenz
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f; // Kompromiss: funktioniert in Hoch- UND Querformat

        var ctrl = canvasGO.AddComponent<GameUIController>();
        EnsureEventSystem();

        // ---------- 1) Startmenü ----------
        var menu = CreateScreen(canvasGO.transform, "MainMenuPanel", BgColor);

        var title = CreateText(menu.safe, "Title", "SPACE DASH", 120);
        Anchor(title.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 220));

        var subtitle = CreateText(menu.safe, "Subtitle", "Responsive Mobile UI Demo", 44, FontStyle.Normal);
        subtitle.color = new Color(1, 1, 1, 0.6f);
        Anchor(subtitle.rectTransform, new Vector2(0.5f, 0.64f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 80));

        var menuColumn = CreateColumn(menu.safe, "ButtonColumn", new Vector2(700, 330), 40);
        Anchor(menuColumn, new Vector2(0.5f, 0.38f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700, 330));
        var startBtn = CreateButton(menuColumn, "StartButton", "SPIELEN", Green, new Vector2(640, 140));
        var quitBtn = CreateButton(menuColumn, "QuitButton", "BEENDEN", Grey, new Vector2(640, 140));

        // ---------- 2) HUD ----------
        var hud = CreateScreen(canvasGO.transform, "HUDPanel", null);

        var score = CreateText(hud.safe, "ScoreText", "Punkte: 0", 56, FontStyle.Bold);
        score.alignment = TextAnchor.MiddleLeft;
        Anchor(score.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -40), new Vector2(600, 80));

        // Lebensanzeige (3 Herz)
        var heartsRow = NewUI("Hearts", hud.safe);
        var hrt = heartsRow.GetComponent<RectTransform>();
        Anchor(hrt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -130), new Vector2(240, 70));
        var hl = heartsRow.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 15;
        hl.childAlignment = TextAnchor.MiddleLeft;
        hl.childControlWidth = hl.childControlHeight = true;
        hl.childForceExpandWidth = hl.childForceExpandHeight = false;
        var hearts = new Image[3];
        for (int i = 0; i < hearts.Length; i++)
        {
            var h = NewUI("Heart" + (i + 1), heartsRow.transform);
            hearts[i] = h.AddComponent<Image>();
            hearts[i].color = Red;
            hearts[i].raycastTarget = false;
            AddLE(h, 70, 70);
        }

        var pauseBtn = CreateButton(hud.safe, "PauseButton", "II", Grey, new Vector2(120, 120));
        Anchor(pauseBtn.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -40), new Vector2(120, 120));

        // Demo-Buttons unten (simulieren Spielereignisse)
        var demoRow = NewUI("DemoButtons", hud.safe);
        Anchor(demoRow.GetComponent<RectTransform>(), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(900, 120));
        var dl = demoRow.AddComponent<HorizontalLayoutGroup>();
        dl.spacing = 30;
        dl.childAlignment = TextAnchor.MiddleCenter;
        dl.childControlWidth = dl.childControlHeight = true;
        dl.childForceExpandWidth = dl.childForceExpandHeight = false;
        var pointsBtn = CreateButton(demoRow.transform, "AddPointsButton", "+10 Punkte", Blue, new Vector2(420, 110));
        var hitBtn = CreateButton(demoRow.transform, "HitButton", "Treffer (-1 Leben)", Red, new Vector2(420, 110));

        // ---------- 3) Pause-Menü ----------
        var pause = CreateScreen(canvasGO.transform, "PausePanel", DimColor);
        var pauseWin = CreateWindow(pause.safe, new Vector2(800, 740));
        var pauseTitle = CreateText(pauseWin, "PauseTitle", "PAUSE", 90);
        AddLE(pauseTitle.gameObject, 640, 120);
        var resumeBtn = CreateButton(pauseWin, "ResumeButton", "WEITER", Green, new Vector2(640, 130));
        var pauseRestartBtn = CreateButton(pauseWin, "RestartButton", "NEUSTART", Blue, new Vector2(640, 130));
        var pauseMenuBtn = CreateButton(pauseWin, "MenuButton", "HAUPTMENÜ", Grey, new Vector2(640, 130));

        // ---------- 4) Game Over ----------
        var over = CreateScreen(canvasGO.transform, "GameOverPanel", DimColor);
        var overWin = CreateWindow(over.safe, new Vector2(800, 700));
        var overTitle = CreateText(overWin, "GameOverTitle", "GAME OVER", 90);
        overTitle.color = Red;
        AddLE(overTitle.gameObject, 640, 120);
        var finalScore = CreateText(overWin, "FinalScoreText", "Punkte: 0", 60, FontStyle.Normal);
        AddLE(finalScore.gameObject, 640, 90);
        var overRestartBtn = CreateButton(overWin, "RestartButton", "NOCHMAL", Green, new Vector2(640, 130));
        var overMenuBtn = CreateButton(overWin, "MenuButton", "HAUPTMENÜ", Grey, new Vector2(640, 130));

        // ---------- Referenzen im Controller ----------
        ctrl.mainMenuPanel = menu.root;
        ctrl.hudPanel = hud.root;
        ctrl.pausePanel = pause.root;
        ctrl.gameOverPanel = over.root;
        ctrl.scoreText = score;
        ctrl.finalScoreText = finalScore;
        ctrl.hearts = hearts;

        // ---------- Buttons verdrahten (persistent, bleibt in der Szene gespeichert) ----------
        UnityEventTools.AddPersistentListener(startBtn.onClick, ctrl.StartGame);
        UnityEventTools.AddPersistentListener(quitBtn.onClick, ctrl.QuitGame);
        UnityEventTools.AddPersistentListener(pauseBtn.onClick, ctrl.PauseGame);
        UnityEventTools.AddPersistentListener(pointsBtn.onClick, ctrl.AddPoints);
        UnityEventTools.AddPersistentListener(hitBtn.onClick, ctrl.TakeHit);
        UnityEventTools.AddPersistentListener(resumeBtn.onClick, ctrl.ResumeGame);
        UnityEventTools.AddPersistentListener(pauseRestartBtn.onClick, ctrl.RestartGame);
        UnityEventTools.AddPersistentListener(pauseMenuBtn.onClick, ctrl.ShowMainMenu);
        UnityEventTools.AddPersistentListener(overRestartBtn.onClick, ctrl.RestartGame);
        UnityEventTools.AddPersistentListener(overMenuBtn.onClick, ctrl.ShowMainMenu);

        // Ausgangszustand im Editor: nur Startmenü sichtbar
        menu.root.SetActive(true);
        hud.root.SetActive(false);
        pause.root.SetActive(false);
        over.root.SetActive(false);

        Selection.activeGameObject = canvasGO;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Mobile UI Demo erstellt. Tipp: Game View -> Simulator, Gerät wählen, Safe Area Overlay einschalten.");
    }

    // ===================== Helfer =====================

    struct Screen2 { public GameObject root; public RectTransform safe; }

    /// Screen = Vollbild-Panel (Hintergrund läuft bis zum Rand) + Kind "SafeArea" (Inhalt innerhalb der Safe Area)
    static Screen2 CreateScreen(Transform parent, string name, Color? bg)
    {
        var root = NewUI(name, parent);
        Stretch(root.GetComponent<RectTransform>());
        if (bg.HasValue)
        {
            var img = root.AddComponent<Image>();
            img.color = bg.Value;
        }

        var safe = NewUI("SafeArea", root.transform);
        var srt = safe.GetComponent<RectTransform>();
        Stretch(srt);
        safe.AddComponent<SafeAreaFitter>();

        return new Screen2 { root = root, safe = srt };
    }

    static GameObject NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void AddLE(GameObject go, float w, float h)
    {
        var le = go.AddComponent<LayoutElement>();
        if (w >= 0) le.preferredWidth = w;
        if (h >= 0) le.preferredHeight = h;
    }

    static Font GetFont()
    {
#if UNITY_2022_2_OR_NEWER
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
        return Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
    }

    static Text CreateText(Transform parent, string name, string content, int size, FontStyle style = FontStyle.Bold)
    {
        var go = NewUI(name, parent);
        var t = go.AddComponent<Text>();
        t.font = GetFont();
        t.text = content;
        t.fontSize = size;
        t.fontStyle = style;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }

    static Button CreateButton(Transform parent, string name, string label, Color color, Vector2 size)
    {
        var go = NewUI(name, parent);
        var img = go.AddComponent<Image>();
        img.color = color;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        go.GetComponent<RectTransform>().sizeDelta = size;
        AddLE(go, size.x, size.y);

        var t = CreateText(go.transform, "Label", label, 46);
        Stretch(t.rectTransform);
        return btn;
    }

    static RectTransform CreateColumn(Transform parent, string name, Vector2 size, float spacing)
    {
        var go = NewUI(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        var v = go.AddComponent<VerticalLayoutGroup>();
        v.spacing = spacing;
        v.childAlignment = TextAnchor.MiddleCenter;
        v.childControlWidth = v.childControlHeight = true;
        v.childForceExpandWidth = v.childForceExpandHeight = false;
        return rt;
    }

    static RectTransform CreateWindow(Transform parent, Vector2 size)
    {
        var go = NewUI("Window", parent);
        var img = go.AddComponent<Image>();
        img.color = WindowColor;
        var rt = go.GetComponent<RectTransform>();
        Anchor(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);

        var v = go.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(40, 40, 40, 40);
        v.spacing = 30;
        v.childAlignment = TextAnchor.MiddleCenter;
        v.childControlWidth = v.childControlHeight = true;
        v.childForceExpandWidth = v.childForceExpandHeight = false;
        return rt;
    }

    static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;

        var es = new GameObject("EventSystem", typeof(EventSystem));
        Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        // Neues Input System: passendes UI-Modul per Reflection hinzufügen
        var t = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (t != null) es.AddComponent(t);
        else es.AddComponent<StandaloneInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }
}
