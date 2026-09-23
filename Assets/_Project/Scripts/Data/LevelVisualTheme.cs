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
    }
}
