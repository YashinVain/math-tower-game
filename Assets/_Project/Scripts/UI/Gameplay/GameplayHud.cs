using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MathGame.UI.Gameplay
{
    public class GameplayHud : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI timerLabel;
        [SerializeField] private Image timerFillBar;

        public void UpdateTimer(float remaining, float total)
        {
            int seconds = Mathf.CeilToInt(Mathf.Max(0f, remaining));
            timerLabel.text = $"{seconds / 60:00}:{seconds % 60:00}";

            if (timerFillBar != null && total > 0f)
                timerFillBar.fillAmount = Mathf.Clamp01(remaining / total);
        }
    }
}
