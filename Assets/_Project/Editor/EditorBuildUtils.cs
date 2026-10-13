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

        // Для массивов ссылок (например, список внешних видов гоблина).
        public static void SetObjectArrayField(Object target, string fieldName, Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"Поле '{fieldName}' не найдено на {target.GetType().Name} — проверьте, не переименовалось ли оно в скрипте.");
                return;
            }
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedProperties();
        }

        // Загружает нарисованную картинку как спрайт. Если файла нет или он
        // ещё не импортирован как Sprite — возвращает null (вызывающий код
        // сам решает, что показать вместо неё), и пишет понятное
        // предупреждение, а не молча оставляет невидимый объект.
        public static Sprite LoadArtSprite(string assetPath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
                Debug.LogWarning($"Картинка '{assetPath}' не найдена или не импортирована как Sprite (попробуйте MathGame → Reimport Art). Вместо неё будет квадрат-заглушка.");
            return sprite;
        }

        // Картинка или (если её нет) белый квадрат-заглушка — чтобы сборка
        // префабов не падала и объект всё равно был виден.
        public static Sprite LoadArtSpriteOrPlaceholder(string assetPath)
        {
            var sprite = LoadArtSprite(assetPath);
            return sprite != null ? sprite : GetPlaceholderSprite();
        }

        private const string LabelMaterialPath = "Assets/_Project/Materials/WorldLabelOutline.mat";

        // Материал для подписей над героем/гоблинами/дверьми: тот же шрифт,
        // но с чёрной обводкой. Без неё белые цифры терялись на светлом небе
        // и лугу (теперь у уровней нарисованные фоны, а не однотонная
        // заливка). Это отдельный файл-материал в проекте, а не "копия на
        // лету": копии материалов не сохраняются в префабе, а ссылка на
        // файл — сохраняется.
        public static Material GetWorldLabelMaterial(TMP_FontAsset font)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(LabelMaterialPath);
            if (existing != null) return existing;

            if (!AssetDatabase.IsValidFolder("Assets/_Project/Materials"))
                AssetDatabase.CreateFolder("Assets/_Project", "Materials");

            var material = new Material(font.material) { name = "WorldLabelOutline" };
            material.EnableKeyword("OUTLINE_ON");
            material.SetColor("_OutlineColor", Color.black);
            material.SetFloat("_OutlineWidth", 0.22f);
            AssetDatabase.CreateAsset(material, LabelMaterialPath);
            return material;
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
            // Жирный шрифт с русскими буквами (Russo One), если он собран.
            var uiFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LabelFontBuilder.UiFontAssetPath);
            if (uiFont != null) tmp.font = uiFont;
            rect.sizeDelta = new Vector2(width, height);
            return tmp;
        }

        public static Button CreateButton(Transform parent, string name, string label, float width = 260, float height = 60, int fontSize = 28)
        {
            var rect = CreateUIObject(name, parent);
            rect.sizeDelta = new Vector2(width, height);

            // Нарисованная кнопка: дерево, железные полосы и уголки с
            // заклёпками (собрана из рамки уровня, см. Tools/MakeUiArt.ps1).
            // Картинка растягивается "по краям" (9-slice): уголки остаются
            // как нарисованы. pixelsPerUnitMultiplier уменьшает уголки так,
            // чтобы они занимали около 30% высоты кнопки (а не 76 px при
            // кнопке высотой 52–80) — иначе они съедали бы всю кнопку.
            var buttonSprite = LoadArtSprite(ArtSpecs.ButtonWide);
            var image = rect.gameObject.AddComponent<Image>();
            if (buttonSprite != null)
            {
                image.sprite = buttonSprite;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
                image.pixelsPerUnitMultiplier = Mathf.Max(1f, ArtSpecs.SlicedCornerPx / (0.3f * height));
            }
            else
            {
                image.color = new Color(1f, 1f, 1f, 0.9f);
            }

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            // Наведение осветляет кнопку, нажатие затемняет.
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, 0.94f, 0.78f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f);
            colors.selectedColor = Color.white;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var labelRect = CreateUIObject("Label", rect);
            StretchFull(labelRect);
            var tmp = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            tmp.fontSize = fontSize;

            // Жирный шрифт (Russo One — с русскими буквами) и светлый
            // кремовый цвет на тёмном дереве; если шрифт ещё не собран —
            // остаётся стандартный.
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LabelFontBuilder.UiFontAssetPath);
            var uiMaterial = AssetDatabase.LoadAssetAtPath<Material>(LabelFontBuilder.UiMaterialPath);
            if (font != null)
            {
                tmp.font = font;
                if (uiMaterial != null) tmp.fontSharedMaterial = uiMaterial;
                tmp.color = new Color(1f, 0.93f, 0.75f);
            }
            else
            {
                tmp.color = Color.black;
            }

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

            // Дорожка тёмного дерева, заливка золотая, ползунок — золотой
            // кружок (встроенный круглый спрайт Unity, закрашенный в золото).
            var bg = rect.gameObject.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.06f, 0.03f);

            var fillAreaRect = CreateUIObject("FillArea", rect);
            StretchFull(fillAreaRect);

            var fillRect = CreateUIObject("Fill", fillAreaRect);
            fillRect.anchorMin = new Vector2(0, 0);
            fillRect.anchorMax = new Vector2(0, 1);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImage = fillRect.gameObject.AddComponent<Image>();
            fillImage.color = new Color(0.96f, 0.76f, 0.24f);

            var handleRect = CreateUIObject("Handle", rect);
            handleRect.sizeDelta = new Vector2(height + 14, height + 14);
            var handleImage = handleRect.gameObject.AddComponent<Image>();
            handleImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            handleImage.color = new Color(1f, 0.88f, 0.55f);

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

            // Коробочка — та же рамка, что у кнопок уровней, а "включено" —
            // золотая медаль с зелёной галочкой (картинки из набора значков).
            var frameSprite = LoadArtSprite(ArtSpecs.LevelButtonFrame);
            var checkSprite = LoadArtSprite(ArtSpecs.IconCheck);

            var bg = rect.gameObject.AddComponent<Image>();
            bg.sprite = frameSprite;
            bg.preserveAspect = true;
            bg.color = frameSprite != null ? Color.white : new Color(0.25f, 0.25f, 0.25f);

            var checkRect = CreateUIObject("Checkmark", rect);
            StretchFull(checkRect);
            checkRect.offsetMin = new Vector2(-4, -4);
            checkRect.offsetMax = new Vector2(4, 4);
            var check = checkRect.gameObject.AddComponent<Image>();
            check.sprite = checkSprite;
            check.preserveAspect = true;
            check.color = checkSprite != null ? Color.white : new Color(0.3f, 0.8f, 0.4f);

            var toggle = rect.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = bg;
            toggle.graphic = check;
            toggle.isOn = isOn;

            return toggle;
        }

        // ---- Раскладка экрана настроек ----
        // Все позиции — в пикселях от ЦЕНТРА экрана (холст 1920×1080, y вверх).
        // Единая левая колонка: и подписи слайдеров, и галочки стоят у одного
        // левого края, а подпись галочки идёт сразу за ней (раньше галочка и
        // текст были далеко друг от друга).
        public const float SettingsLeftEdge = -610f;
        public static readonly Color SettingsTextColor = new Color(1f, 0.93f, 0.75f);
        public static readonly Color SettingsValueColor = new Color(1f, 0.82f, 0.3f);

        // Ряд "подпись — слайдер — значение".
        public static (Slider slider, TextMeshProUGUI valueLabel) CreateSliderRow(Transform parent, string rowName, string labelText, float min, float max, float value, float y)
        {
            var label = CreateLabel(parent, rowName + "_Label", labelText, 32, SettingsTextColor, 400, 50, TextAlignmentOptions.MidlineLeft);
            AnchorAt(label.rectTransform, 0.5f, 0.5f, 400, 50, SettingsLeftEdge + 200f, y);

            var slider = CreateSlider(parent, rowName + "_Slider", min, max, value, 400, 26);
            AnchorAt(slider.GetComponent<RectTransform>(), 0.5f, 0.5f, 400, 26, 30f, y);

            var valueLabel = CreateLabel(parent, rowName + "_Value", value.ToString("0.##"), 34, SettingsValueColor, 330, 50, TextAlignmentOptions.MidlineLeft);
            valueLabel.textWrappingMode = TextWrappingModes.NoWrap;
            AnchorAt(valueLabel.rectTransform, 0.5f, 0.5f, 330, 50, 290f + 165f, y);

            return (slider, valueLabel);
        }

        // Галочка и сразу за ней подпись. x — центр галочки (в пикселях от центра).
        public static (Toggle toggle, TextMeshProUGUI label) CreateToggleRow(Transform parent, string rowName, string labelText, bool isOn, float x, float y, float labelWidth = 260f)
        {
            const float box = 60f;
            var toggle = CreateToggle(parent, rowName + "_Toggle", isOn, box);
            AnchorAt(toggle.GetComponent<RectTransform>(), 0.5f, 0.5f, box, box, x, y);

            var label = CreateLabel(parent, rowName + "_Label", labelText, 32, SettingsTextColor, labelWidth, 50, TextAlignmentOptions.MidlineLeft);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            AnchorAt(label.rectTransform, 0.5f, 0.5f, labelWidth, 50, x + box * 0.5f + 16f + labelWidth * 0.5f, y);

            return (toggle, label);
        }

    }
}
