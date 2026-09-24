using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MathGame.EditorTools
{
    // Общие маленькие помощники для редакторских инструментов сборки
    // (PrefabBuilderMenu, SceneBuilderMenu) — чтобы не дублировать один и
    // тот же код создания UI-элементов и назначения ссылок в двух местах.
    // Всё построено на UnityEngine.UI.LayoutGroup/LayoutElement, а не на
    // ручном расчёте координат, — так собранные экраны не разваливаются
    // при разных размерах текста/окна, и код не нужно подгонять руками.
    public static class EditorBuildUtils
    {
        public static void SetField(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"Поле '{fieldName}' не найдено на {target.GetType().Name} — проверьте, не переименовалось ли оно в скрипте.");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }

        public static Sprite CreatePlaceholderSprite(Color color)
        {
            var texture = new Texture2D(4, 4) { name = "PlaceholderSquare" };
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }

        public static RectTransform CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        public static RectTransform StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static GameObject CreateCanvas(Transform parent, string name)
        {
            var rect = CreateUIObject(name, parent);
            var canvas = rect.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = rect.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            rect.gameObject.AddComponent<GraphicRaycaster>();
            return rect.gameObject;
        }

        public static void CreateEventSystem()
        {
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<UnityEngine.EventSystems.EventSystem>();
            go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        public static RectTransform CreateFullScreenPanel(Transform canvasParent, string name)
        {
            var rect = CreateUIObject(name, canvasParent);
            StretchFull(rect);
            return rect;
        }

        // childAlignment/spacing описаны один раз тут, чтобы все панели
        // выглядели единообразно без ручной подгонки координат.
        public static VerticalLayoutGroup AddVerticalLayout(RectTransform rect, int spacing = 16, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(40, 40, 40, 40);
            return layout;
        }

        public static HorizontalLayoutGroup AddHorizontalLayout(RectTransform rect, int spacing = 12, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static LayoutElement SetPreferredSize(GameObject go, float width, float height)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = height;
            return le;
        }

        public static TextMeshProUGUI CreateLabel(Transform parent, string name, string text, int fontSize, Color color, float width = 260, float height = 34, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
        {
            var rect = CreateUIObject(name, parent);
            var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            SetPreferredSize(rect.gameObject, width, height);
            return tmp;
        }

        public static Button CreateButton(Transform parent, string name, string label, float width = 260, float height = 60, int fontSize = 28)
        {
            var rect = CreateUIObject(name, parent);
            SetPreferredSize(rect.gameObject, width, height);

            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.9f);

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var labelRect = CreateUIObject("Label", rect);
            StretchFull(labelRect);
            var tmp = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.black;
            tmp.fontSize = fontSize;

            return button;
        }

        public static Image CreateIcon(Transform parent, string name, Color color, float width, float height)
        {
            var rect = CreateUIObject(name, parent);
            SetPreferredSize(rect.gameObject, width, height);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static Slider CreateSlider(Transform parent, string name, float min, float max, float value, float width = 300, float height = 24)
        {
            var rect = CreateUIObject(name, parent);
            SetPreferredSize(rect.gameObject, width, height);

            var bg = rect.gameObject.AddComponent<Image>();
            bg.color = new Color(0.25f, 0.25f, 0.25f);

            var fillAreaRect = CreateUIObject("FillArea", rect);
            StretchFull(fillAreaRect);

            var fillRect = CreateUIObject("Fill", fillAreaRect);
            fillRect.anchorMin = new Vector2(0, 0);
            fillRect.anchorMax = new Vector2(0, 1);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImage = fillRect.gameObject.AddComponent<Image>();
            fillImage.color = new Color(0.3f, 0.6f, 1f);

            var handleRect = CreateUIObject("Handle", rect);
            handleRect.sizeDelta = new Vector2(20, height + 10);
            var handleImage = handleRect.gameObject.AddComponent<Image>();
            handleImage.color = Color.white;

            var slider = rect.gameObject.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;

            return slider;
        }

        public static Toggle CreateToggle(Transform parent, string name, bool isOn, float boxSize = 28)
        {
            var rect = CreateUIObject(name, parent);
            SetPreferredSize(rect.gameObject, boxSize, boxSize);

            var bg = rect.gameObject.AddComponent<Image>();
            bg.color = new Color(0.25f, 0.25f, 0.25f);

            var checkRect = CreateUIObject("Checkmark", rect);
            StretchFull(checkRect);
            var check = checkRect.gameObject.AddComponent<Image>();
            check.color = new Color(0.3f, 0.8f, 0.4f);

            var toggle = rect.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = bg;
            toggle.graphic = check;
            toggle.isOn = isOn;

            return toggle;
        }

        // Одна строка "подпись + слайдер + число" в настройках, чтобы не
        // повторять эту связку кода 4 раза.
        public static (Slider slider, TextMeshProUGUI valueLabel) CreateSliderRow(Transform parent, string rowName, string labelText, float min, float max, float value)
        {
            var row = CreateUIObject(rowName, parent);
            SetPreferredSize(row.gameObject, 820, 40);
            AddHorizontalLayout(row, 12, TextAnchor.MiddleLeft);

            CreateLabel(row, "Label", labelText, 22, Color.white, 320, 34);
            var slider = CreateSlider(row, "Slider", min, max, value, 320, 24);
            var valueLabel = CreateLabel(row, "Value", value.ToString("0.##"), 22, Color.white, 100, 34, TextAlignmentOptions.MidlineRight);

            return (slider, valueLabel);
        }

        public static (Toggle toggle, TextMeshProUGUI label) CreateToggleRow(Transform parent, string rowName, string labelText, bool isOn)
        {
            var row = CreateUIObject(rowName, parent);
            SetPreferredSize(row.gameObject, 820, 34);
            AddHorizontalLayout(row, 12, TextAnchor.MiddleLeft);

            var toggle = CreateToggle(row, "Toggle", isOn);
            var label = CreateLabel(row, "Label", labelText, 22, Color.white, 400, 34);

            return (toggle, label);
        }
    }
}
