using UnityEngine;

namespace MathGame.Core
{
    // Применяет визуальную тему текущего уровня к сцене (фон) — тот самый
    // механизм, которым уровни "мрачнеют" по мере прохождения.
    // Отдельный маленький компонент, а не часть GameplayController, чтобы
    // тему можно было расширять (туман, частицы, музыка), не трогая
    // игровой поток.
    //
    // Раньше "страшнее" достигалось затемнением цвета фона. Теперь у
    // каждой темы есть своя нарисованная картинка (луг → пустошь → руины →
    // крепость гоблинов), и атмосфера растёт за счёт появляющихся на ней
    // деталей, а не темноты — иначе светлые персонажи выглядели бы
    // "вырезанными" на тёмном фоне. Поэтому, если у темы есть картинка,
    // она показывается как есть (белый цвет = без тонировки).
    public class LevelThemeApplier : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer backgroundRenderer;

        private void Start()
        {
            var theme = LevelSelection.Current != null ? LevelSelection.Current.visualTheme : null;
            if (theme == null || backgroundRenderer == null) return;

            if (theme.backgroundSprite != null)
            {
                backgroundRenderer.sprite = theme.backgroundSprite;
                backgroundRenderer.color = Color.white;
                return;
            }

            // Запасной вариант без картинки: прежнее затемнение цветом.
            backgroundRenderer.color = Color.Lerp(theme.ambientTint, Color.black, theme.darknessAmount * 0.5f);
        }
    }
}
