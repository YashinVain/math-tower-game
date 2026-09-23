using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MathGame.Data;

namespace MathGame.UI.Menu
{
    public class LevelButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI numberLabel;
        [SerializeField] private GameObject lockIcon;
        [SerializeField] private GameObject completedIcon;

        public void Setup(LevelDefinition level, int displayNumber, LevelState state, Action onClick)
        {
            numberLabel.text = displayNumber.ToString();

            lockIcon.SetActive(state == LevelState.Locked);
            completedIcon.SetActive(state == LevelState.Completed);

            button.interactable = state != LevelState.Locked;
            button.onClick.RemoveAllListeners();
            if (state != LevelState.Locked)
                button.onClick.AddListener(() => onClick());
        }
    }
}
