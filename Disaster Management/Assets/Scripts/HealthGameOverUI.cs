using UnityEngine;
using TMPro;

// Creates missing game-over controls at runtime; existing Inspector references are preserved.
public static class HealthGameOverUI
{
    public static void Ensure(PlayerHealth health)
    {
        if (health.retryScreen == null)
        {
            var obj = new GameObject("GameOverCanvas", typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            var canvas = obj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = obj.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            var panel = new GameObject("GameOverPanel", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            panel.transform.SetParent(obj.transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            panel.GetComponent<UnityEngine.UI.Image>().color = new Color(0.025f, 0.035f, 0.05f, 0.92f);
            health.retryScreen = panel;
        }
        if (health.retryText == null)
            health.retryText = MakeText(health.retryScreen.transform, "GameOverTitle", "Simulation ended", 44,
                new Vector2(0, 100), new Vector2(700, 70));
        MakeText(health.retryScreen.transform, "RestartHint", "You died from fire or smoke.\nPress R or Space, or select Restart.", 24,
            new Vector2(0, 15), new Vector2(700, 90));
        var button = health.retryScreen.GetComponentInChildren<UnityEngine.UI.Button>(true);
        if (button == null)
        {
            var obj = new GameObject("RestartButton", typeof(RectTransform),
                typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            obj.transform.SetParent(health.retryScreen.transform, false);
            Center(obj.GetComponent<RectTransform>(), new Vector2(0, -95), new Vector2(260, 64));
            obj.GetComponent<UnityEngine.UI.Image>().color = new Color(0.12f, 0.5f, 0.36f);
            button = obj.GetComponent<UnityEngine.UI.Button>();
            MakeText(obj.transform, "Label", "Restart", 28, Vector2.zero, new Vector2(250, 60));
        }
        button.onClick.AddListener(health.RetrySimulation);
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            var events = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
#if ENABLE_INPUT_SYSTEM
            events.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            events.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
        }
    }

    private static TMP_Text MakeText(Transform parent, string name, string content, int size, Vector2 position, Vector2 dimensions)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        Center(obj.GetComponent<RectTransform>(), position, dimensions);
        var text = obj.GetComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static void Center(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }
}
