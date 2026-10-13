using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace MathGame.EditorTools
{
    // Собирает шрифты и материалы для текста в игре. Оба шрифта бесплатные
    // (лицензия OFL — можно свободно использовать в проектах; тексты лицензий
    // лежат рядом со шрифтами в Assets/_Project/Fonts).
    //
    // 1) Числа и примеры над героем, гоблинами и дверьми — стиль "как в
    //    казуальных играх жанра Hero Tower Wars": очень жирные округлые
    //    цифры (Lilita One), цветная заливка, толстая БЕЛАЯ обводка
    //    ("стикер") и мягкая тень. Заливка (синяя у героя, красная у
    //    примеров) задаётся в коде (LabelStyle), а обводка, жирность и тень —
    //    в материале.
    // 2) Текст на кнопках — Russo One (жирный, и ГЛАВНОЕ — с русскими
    //    буквами: у Lilita One кириллицы нет) с мягкой тёмной тенью.
    //
    // Результат кладётся в Assets/_Project/Resources/Fonts — папка
    // Resources нужна, чтобы код мог загрузить шрифт сам (Resources.Load) и
    // не требовалось пересобирать префабы.
    public static class LabelFontBuilder
    {
        public const string LilitaTtfPath = "Assets/_Project/Fonts/LilitaOne-Regular.ttf";
        public const string RussoTtfPath = "Assets/_Project/Fonts/RussoOne-Regular.ttf";

        public const string FontAssetPath = "Assets/_Project/Resources/Fonts/LilitaOne SDF.asset";
        public const string MaterialPath = "Assets/_Project/Resources/Fonts/LilitaOne Label.mat";
        public const string UiFontAssetPath = "Assets/_Project/Resources/Fonts/RussoOne SDF.asset";
        public const string UiMaterialPath = "Assets/_Project/Resources/Fonts/RussoOne UI.mat";

        // Символы, которые сразу кладём в шрифты (остальные подгружаются на
        // лету, шрифты "динамические"): цифры и знаки действий / русский алфавит.
        private const string NumberCharacters = "0123456789xX=+-−–×÷:*/ ";
        private const string UiCharacters = "0123456789 .,:;!?()–-—«»" +
            "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя" +
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

        [MenuItem("MathGame/Build Label Font")]
        public static void Build()
        {
            Directory.CreateDirectory("Assets/_Project/Resources/Fonts");

            // ---- 1) числа над героем/гоблинами/дверьми ----
            var numbers = CreateFontAsset(LilitaTtfPath, FontAssetPath, "LilitaOne SDF", NumberCharacters);
            if (numbers != null)
            {
                var material = new Material(numbers.material) { name = "LilitaOne Label" };
                material.EnableKeyword("OUTLINE_ON");
                material.SetColor("_OutlineColor", Color.white);
                material.SetFloat("_OutlineWidth", 0.36f);
                material.SetFloat("_OutlineSoftness", 0.02f);
                material.SetFloat("_FaceDilate", 0.12f);     // чуть жирнее букв
                material.EnableKeyword("UNDERLAY_ON");
                material.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.55f));
                material.SetFloat("_UnderlayOffsetX", 0.5f);
                material.SetFloat("_UnderlayOffsetY", -0.5f);
                material.SetFloat("_UnderlayDilate", 0.3f);
                material.SetFloat("_UnderlaySoftness", 0.25f);
                AssetDatabase.DeleteAsset(MaterialPath);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            // ---- 2) текст на кнопках ----
            var ui = CreateFontAsset(RussoTtfPath, UiFontAssetPath, "RussoOne SDF", UiCharacters);
            if (ui != null)
            {
                var uiMaterial = new Material(ui.material) { name = "RussoOne UI" };
                uiMaterial.EnableKeyword("UNDERLAY_ON");
                uiMaterial.SetColor("_UnderlayColor", new Color(0.1f, 0.05f, 0f, 0.75f));
                uiMaterial.SetFloat("_UnderlayOffsetX", 0.4f);
                uiMaterial.SetFloat("_UnderlayOffsetY", -0.4f);
                uiMaterial.SetFloat("_UnderlayDilate", 0.2f);
                uiMaterial.SetFloat("_UnderlaySoftness", 0.2f);
                AssetDatabase.DeleteAsset(UiMaterialPath);
                AssetDatabase.CreateAsset(uiMaterial, UiMaterialPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Готово: шрифты Lilita One (числа) и Russo One (кнопки) и их материалы собраны в Assets/_Project/Resources/Fonts.");
        }

        private static TMP_FontAsset CreateFontAsset(string ttfPath, string assetPath, string assetName, string characters)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (font == null)
            {
                Debug.LogError($"Шрифт не найден: {ttfPath}");
                return null;
            }

            // Пересоздаём с нуля, чтобы повторный запуск давал тот же результат.
            AssetDatabase.DeleteAsset(assetPath);

            var fontAsset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            fontAsset.name = assetName;
            fontAsset.TryAddCharacters(characters, out string missing);
            if (!string.IsNullOrEmpty(missing))
                Debug.LogWarning($"В шрифте {assetName} нет символов: {missing}");

            AssetDatabase.CreateAsset(fontAsset, assetPath);
            fontAsset.material.name = assetName + " Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
            {
                fontAsset.atlasTextures[i].name = assetName + " Atlas " + i;
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[i], fontAsset);
            }
            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            return fontAsset;
        }
    }
}
