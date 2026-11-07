using UnityEngine;

/// Passt ein RectTransform an Screen.safeArea an (Notch, Kamera-Cutout,
/// abgerundete Ecken, Gesten-Leiste). Reagiert auf Rotation und Auflösungswechsel.
/// Einfach auf ein Container-Objekt unterhalb des Canvas legen, das auf
/// "Stretch/Stretch" verankert ist. Alle UI-Elemente, die nicht verdeckt werden
/// dürfen, werden Kinder dieses Containers.
[RequireComponent(typeof(RectTransform))]
[ExecuteAlways]
public class SafeAreaFitter : MonoBehaviour
{
    RectTransform rt;
    Rect lastSafeArea;
    Vector2Int lastResolution;
    ScreenOrientation lastOrientation;

    void OnEnable()
    {
        rt = GetComponent<RectTransform>();
        Apply();
    }

    void Update()
    {
        // Vergleich pro Frame; nur bei Änderung wird neu berechnet.
        if (Screen.safeArea != lastSafeArea ||
            Screen.width != lastResolution.x ||
            Screen.height != lastResolution.y ||
            Screen.orientation != lastOrientation)
        {
            Apply();
        }
    }

    void Apply()
    {
        if (rt == null) rt = GetComponent<RectTransform>();
        if (Screen.width <= 0 || Screen.height <= 0) return;

        Rect safe = Screen.safeArea;

        lastSafeArea = safe;
        lastResolution = new Vector2Int(Screen.width, Screen.height);
        lastOrientation = Screen.orientation;

        // Pixel -> normalisierte Anchors (0..1)
        Vector2 anchorMin = safe.position;
        Vector2 anchorMax = safe.position + safe.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
