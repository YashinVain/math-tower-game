using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MathGame.UI.Gameplay
{
    // Экран конца уровня: победа или поражение. Сам не решает, что делать
    // дальше — только показывает нужные кнопки и вызывает переданные снаружи
    // действия по клику (их назначает GameplayController).
    public class ResultPanel : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private Button primaryButton;
        [SerializeField] private TextMeshProUGUI primaryButtonLabel;
        [SerializeField] private Button secondaryButton;

        public void ShowWin(bool hasNextLevel, Action onNext, Action onMenu)
        {
            root.SetActive(true);
            titleLabel.text = "Уровень пройден!";

            primaryButtonLabel.text = hasNextLevel ? "Следующий уровень" : "В меню";
            BindButton(primaryButton, hasNextLevel ? onNext : onMenu);
            BindButton(secondaryButton, onMenu);
            secondaryButton.gameObject.SetActive(hasNextLevel);
        }

        public void ShowLose(Action onRetry, Action onMenu)
        {
            root.SetActive(true);
            titleLabel.text = "Поражение";

            primaryButtonLabel.text = "Попробовать снова";
            BindButton(primaryButton, onRetry);
            BindButton(secondaryButton, onMenu);
            secondaryButton.gameObject.SetActive(true);
        }

        public void Hide() => root.SetActive(false);

        private static void BindButton(Button button, Action action)
        {
            button.onClick.RemoveAllListeners();
            if (action != null) button.onClick.AddListener(() => action());
        }
    }
}
