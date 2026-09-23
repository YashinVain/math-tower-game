using System;
using UnityEngine;
using TMPro;
using MathGame.Data;
using MathGame.Utils;

namespace MathGame.Minigames.Towers
{
    // Один голем в башне: показывает пример в формате "x = ...", хранит
    // ответ и сообщает наружу о выборе игрока. Сам не решает, победил герой
    // или нет — это знает только TowerMinigameController (у него есть
    // текущая сила героя).
    [RequireComponent(typeof(SpriteRenderer))]
    public class GolemView : MonoBehaviour
    {
        [SerializeField] private TextMeshPro expressionLabel;

        private Action<GolemView> _onSelected;

        public int Answer { get; private set; }

        public void Init(MathProblem problem, Action<GolemView> onSelected)
        {
            Answer = problem.Answer;
            expressionLabel.text = $"x = {problem.Expression}";
            _onSelected = onSelected;
        }

        private void OnMouseDown()
        {
            _onSelected?.Invoke(this);
        }

        public void PlayDefeatedByHero(Action onComplete)
        {
            _onSelected = null;
            StartCoroutine(SimpleTween.ScaleTo(transform, Vector3.zero, 0.3f, () =>
            {
                gameObject.SetActive(false);
                onComplete?.Invoke();
            }));
        }
    }
}
