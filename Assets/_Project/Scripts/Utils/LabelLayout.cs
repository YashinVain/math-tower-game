using TMPro;
using UnityEngine;

namespace MathGame.Utils
{
    // Расстановка подписей: сколько места занимает текст и как поставить
    // подпись над объектом (героем, дверью) с учётом реальной высоты его
    // картинки — а не числа, запомненного при сборке префаба (герой стал
    // крупнее, дверь тоже, и подписи должны подстраиваться сами).
    public static class LabelLayout
    {
        // Небольшой запас на толщину обводки и тени вокруг букв.
        private const float Pad = 0.12f;

        // Размер текста в единицах мира (в масштабе самой подписи).
        public static Vector2 Measure(TextMeshPro label)
        {
            label.ForceMeshUpdate();
            return label.GetRenderedValues(false) + new Vector2(Pad, Pad);
        }

        // Ставит подпись над объектом: нижний край текста — на gap выше верха
        // картинки объекта (в локальных координатах родителя подписи).
        public static void PlaceAbove(TextMeshPro label, SpriteRenderer target, float gap)
        {
            if (target == null || target.sprite == null) return;
            Vector2 size = Measure(label);
            float top = target.sprite.bounds.max.y * target.transform.localScale.y;
            label.transform.localPosition = new Vector3(0f, top + gap + size.y * 0.5f, 0f);
        }
    }
}
