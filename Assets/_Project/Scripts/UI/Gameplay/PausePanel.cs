using System;
using UnityEngine;
using UnityEngine.UI;

namespace MathGame.UI.Gameplay
{
    // Открывшееся окно паузы само по себе служит подтверждением выхода: Esc
    // не выходит из уровня мгновенно, а открывает это меню, где уже
    // осознанно выбирается "Выйти" или "Продолжить".
    public class PausePanel : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button exitButton;

        public event Action ResumeRequested;
        public event Action ExitRequested;

        public bool IsVisible => root.activeSelf;

        private void Awake()
        {
            resumeButton.onClick.AddListener(() => ResumeRequested?.Invoke());
            exitButton.onClick.AddListener(() => ExitRequested?.Invoke());
        }

        public void Show() => root.SetActive(true);
        public void Hide() => root.SetActive(false);
    }
}
