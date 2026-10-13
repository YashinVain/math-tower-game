using UnityEngine;

namespace MathGame.Data
{
    // Визуальное "настроение" уровня: чем дальше в игре, тем темнее фон и
    // тем выше darknessAmount. Это только данные — применяет их отдельный
    // компонент на сцене (LevelThemeApplier), сам ассет ничего не рисует.
    [CreateAssetMenu(menuName = "MathGame/Level Visual Theme", fileName = "Theme_")]
    public class LevelVisualTheme : ScriptableObject
    {
        public Sprite backgroundSprite;
        public Color ambientTint = Color.white;

        [Range(0f, 1f)]
        public float darknessAmount = 0f;

        // Для фонов-стен (уровни с дверями): на какой высоте картинки (доля
        // от её высоты, снизу) должны стоять "ноги" героя и дверей — там
        // стена встречается с полом. 0 — не привязывать (обычный фон, как у
        // уровней с башнями). См. BackgroundFitter.
        [Range(0f, 0.5f)]
        public float floorLine = 0f;
    }
}
