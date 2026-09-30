using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MathGame.EditorTools
{
    // Общие маленькие помощники для редакторских инструментов сборки
    // (PrefabBuilderMenu, SceneBuilderMenu) — чтобы не дублировать один и
    // тот же код создания UI-элементов и назначения ссылок в двух местах.
    //
    // Позиционирование — через AnchorAt: у каждого элемента anchorMin =
    // anchorMax = точка в ДОЛЯХ экрана (0..1 по X и Y), плюс фиксированный
    // размер в пикселях. Это специально не через LayoutGroup (Vertical/
    // HorizontalLayoutGroup) — с ними реальный размер/позиция зависели от
    // настроек childControl и легко "уезжали" за экран при непривычном
    // соотношении сторон окна. Точка-в-долях-экрана всегда остаётся на
    // экране по построению, независимо от размера окна.
    public static class EditorBuildUtils
    {
        // Для ссылочных полей (объекты, компоненты, префабы). Для чисел —
        // SetFloatField ниже: objectReferenceValue работает только со
        // ссылками, для float/int нужно отдельное свойство SerializedProperty.
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

        public static void SetFloatField(Object target, string fieldName, float value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"Поле '{fieldName}' не найдено на {target.GetType().Name} — проверьте, не переименовалось ли оно в скрипте.");
                return;
            }
            prop.floatValue = value;
            so.ApplyModifiedProperties();
        }

        private const string PlaceholderSpritePath = "Assets/_Project/Art/PlaceholderSquare.png";

        // ВАЖНО: раньше здесь был Sprite.Create() из текстуры, созданной
        // прямо в памяти — это работает, пока редактор открыт, но у такого
        // спрайта нет файла на диске (GUID), поэтому Unity не может
        // сослаться на него из СОХРАНЁННОГО префаба/сцены: при сохранении
        // ссылка превращалась в "None (Sprite)", и объект становился
        // невидимым (ровно это и произошло с героем — позиция была верной,
        // просто рисовать было нечего). Правильный способ — сохранить один
        // настоящий файл-спрайт на диске один раз и переиспользовать его
        // everywhere, а нужный цвет каждого объекта задавать отдельно через
        // SpriteRenderer.color (он умножается на цвет спрайта — белый
        // спрайт + любой цвет тонировки = сплошной цвет, без отдельной
        // текстуры на каждый оттенок).
        public static Sprite GetPlaceholderSprite()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
            if (existing != null) return existing;

            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color32[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
            texture.SetPixels32(pixels);
            texture.Apply();
            byte[] pngData = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);

            var directory = Path.GetDirectoryName(PlaceholderSpritePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllBytes(PlaceholderSpritePath, pngData);
            AssetDatabase.ImportAsset(PlaceholderSpritePath, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(PlaceholderSpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 4f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
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

        // anchorX/anchorY — точка в долях экрана (0 = левый/нижний край,
        // 1 = правый/верхний, 0.5 = центр). width/height — фиксированный
        // размер в пикселях. offsetX/offsetY — дополнительный сдвиг в
        // пикселях от этой точки (обычно 0, кроме мелких поправок).
        public static RectTransform AnchorAt(RectTransform rect, float anchorX, float anchorY, float width, float height, float offsetX = 0f, float offsetY = 0f)
        {
            rect.anchorMin = new Vector2(anchorX, anchorY);
            rect.anchorMax = new Vector2(anchorX, anchorY);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(offsetX, offsetY);
            return rect;
        }

        public static GameObject CreateCanvas(Transform parent, string name)
        {
            var rect = CreateUIObject(name, parent);
            var canvas = rect.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = rect.gameObject.AddComponent<CanvasScaler>();
            // ВАЖНО: позиция каждого элемента задаётся в долях экрана
            // (см. AnchorAt) — это само по себе гарантирует, что ничего не
            // уедет за край, независимо от режима масштабирования ниже.
            // ScaleWithScreenSize здесь отвечает только за РАЗМЕР: без
            // него (ConstantPixelSize) элементы фиксированного пиксельного
            // размера выглядят гигантскими на маленьком окне и крошечными
            // на большом — именно это и было настоящей причиной "всё
            // растянуто"/"текст наезжает", а не позиционирование.
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
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

        public static LayoutElement SetPreferredSize(GameObject go, float width, float height)
        {
            var rect = go.GetComponent<RectTransform>();
            if (rect != null) rect.sizeDelta = new Vector2(width, height);

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
            rect.sizeDelta = new Vector2(width, height);
            return tmp;
        }

        public static Button CreateButton(Transform parent, string name, string label, float width = 260, float height = 60, int fontSize = 28)
        {
            var rect = CreateUIObject(name, parent);
            rect.sizeDelta = new Vector2(width, height);

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
            rect.sizeDelta = new Vector2(width, height);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static Slider CreateSlider(Transform parent, string name, float min, float max, float value, float width = 300, float height = 24)
        {
            var rect = CreateUIObject(name, parent);
            rect.sizeDelta = new Vector2(width, height);

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
            rect.sizeDelta = new Vector2(boxSize, boxSize);

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

        // Одна строка "подпись + слайдер + число" в настройках — все три
        // элемента независимо закреплены по вертикальной доле экрана
        // (yAnchor), с разными фиксированными горизонтальными долями, а не
        // вложены в общий контейнер со своей раскладкой.
        public static (Slider slider, TextMeshProUGUI valueLabel) CreateSliderRow(Transform parent, string rowName, string labelText, float min, float max, float value, float yAnchor)
        {
            var label = CreateLabel(parent, rowName + "_Label", labelText, 20, Color.white, 340, 34, TextAlignmentOptions.MidlineRight);
            AnchorAt(label.rectTransform, 0.32f, yAnchor, 340, 34);

            var slider = CreateSlider(parent, rowName + "_Slider", min, max, value, 260, 20);
            AnchorAt(slider.GetComponent<RectTransform>(), 0.55f, yAnchor, 260, 20);

            var valueLabel = CreateLabel(parent, rowName + "_Value", value.ToString("0.##"), 20, Color.white, 90, 34, TextAlignmentOptions.MidlineLeft);
            AnchorAt(valueLabel.rectTransform, 0.74f, yAnchor, 90, 34);

            return (slider, valueLabel);
        }

        public static (Toggle toggle, TextMeshProUGUI label) CreateToggleRow(Transform parent, string rowName, string labelText, bool isOn, float yAnchor)
        {
            var toggle = CreateToggle(parent, rowName + "_Toggle", isOn);
            AnchorAt(toggle.GetComponent<RectTransform>(), 0.30f, yAnchor, 26, 26);

            var label = CreateLabel(parent, rowName + "_Label", labelText, 20, Color.white, 300, 30, TextAlignmentOptions.MidlineLeft);
            AnchorAt(label.rectTransform, 0.47f, yAnchor, 300, 30);

            return (toggle, label);
        }
    }
}
