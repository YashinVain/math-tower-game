using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MathGame.UI.Gameplay
{
    public class GameplayHud : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI timerLabel;
        [SerializeField] private Image timerFillBar;

        // Золотая полоска оставшегося времени краснеет, когда времени мало.
        private static readonly Color BarColor = new Color(0.96f, 0.76f, 0.24f);
        private static readonly Color BarLowColor = new Color(0.92f, 0.25f, 0.2f);
        private const float LowTimeFraction = 0.2f;

        public void UpdateTimer(float remaining, float total)
        {
            int seconds = Mathf.CeilToInt(Mathf.Max(0f, remaining));
            timerLabel.text = $"{seconds / 60:00}:{seconds % 60:00}";

            if (timerFillBar != null && total > 0f)
            {
                float fraction = Mathf.Clamp01(remaining / total);

                // Ширина заливки меняется якорем (а не fillAmount: у картинки
                // без спрайта заполнение не работает).
                var rect = timerFillBar.rectTransform;
                rect.anchorMax = new Vector2(fraction, 1f);

                timerFillBar.color = fraction < LowTimeFraction ? BarLowColor : BarColor;
            }
        }
    }
}
