using TMPro;
using UnityEngine;

namespace MathGame.Utils
{
    // Единый вид чисел и примеров в мире (над героем, гоблинами и дверьми).
    //
    // Что смотрели: похожие игры жанра "башня с героем и врагами" (Hero Tower
    // Wars и др.) показывают числа очень крупными, жирными и круглыми,
    // ЦВЕТНЫМИ (синее — число героя, красное — врагов) и с толстой БЕЛОЙ
    // обводкой, как у наклейки; никаких плашек и тонких чёрных контуров. Так
    // числа читаются на любом фоне — и на небе, и на тёмной стене.
    //
    // Сам шрифт и материал (обводка, жирность, тень) собирает редакторский
    // инструмент LabelFontBuilder и кладёт в Resources/Fonts. Здесь они только
    // загружаются и применяются; цвет заливки и размер задаёт вызывающий код.
    public static class LabelStyle
    {
        // Синий — число героя (его сила / цель), красный — примеры, которые
        // нужно решить. Оттенки подобраны яркими, как в казуальных играх.
        public static readonly Color HeroColor = new Color(0.12f, 0.38f, 1f);
        public static readonly Color ProblemColor = new Color(0.93f, 0.13f, 0.17f);

        private const string FontPath = "Fonts/LilitaOne SDF";
        private const string MaterialPath = "Fonts/LilitaOne Label";

        private static TMP_FontAsset _font;
        private static Material _material;

        public static void Apply(TextMeshPro label, Color fill, float fontSize)
        {
            if (_font == null) _font = Resources.Load<TMP_FontAsset>(FontPath);
            if (_material == null) _material = Resources.Load<Material>(MaterialPath);

            // Если шрифт ещё не собран (запасной вариант) — остаётся прежний.
            if (_font != null)
            {
                label.font = _font;
                if (_material != null) label.fontSharedMaterial = _material;
            }

            label.color = fill;
            label.fontSize = fontSize;
            label.fontStyle = FontStyles.Normal; // жирность даёт сам шрифт и материал
            label.alignment = TextAlignmentOptions.Center;
            label.sortingOrder = 10;
        }

        // Уменьшает шрифт так, чтобы текст не был шире maxWidth (в единицах
        // мира в масштабе подписи). Нужен для длинных примеров ("100 : 10"),
        // когда крупный шрифт не помещается между дверьми или рядом с башней.
        // Вызывать ПОСЛЕ присвоения текста.
        public static void FitWidth(TextMeshPro label, float maxWidth)
        {
            label.ForceMeshUpdate();
            float width = label.GetRenderedValues(false).x;
            if (width > maxWidth && width > 0f)
            {
                label.fontSize *= maxWidth / width;
                label.ForceMeshUpdate();
            }
        }
    }
}
