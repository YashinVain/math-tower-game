using UnityEngine;

namespace MathGame.Core
{
    // Применяет визуальную тему текущего уровня к сцене (фон и общий тон) —
    // тот самый механизм, которым уровни "мрачнеют" по мере прохождения.
    // Отдельный маленький компонент, а не часть GameplayController, чтобы
    // тему можно было расширять (туман, частицы, музыка), не трогая
    // игровой поток.
    public class LevelThemeApplier : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer backgroundRenderer;

        private void Start()
        {
            var theme = LevelSelection.Current != null ? LevelSelection.Current.visualTheme : null;
            if (theme == null || backgroundRenderer == null) return;

            if (theme.backgroundSprite != null)
                backgroundRenderer.sprite = theme.backgroundSprite;

            backgroundRenderer.color = Color.Lerp(theme.ambientTint, Color.black, theme.darknessAmount * 0.5f);
        }
    }
}
