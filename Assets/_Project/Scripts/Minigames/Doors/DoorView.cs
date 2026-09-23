using System;
using UnityEngine;
using TMPro;
using MathGame.Data;
using MathGame.Utils;

namespace MathGame.Minigames.Doors
{
    // Одна дверь: показывает пример в формате "... = x", хранит ответ и
    // сообщает наружу о выборе игрока. Не решает сама, правильная ли она, —
    // это знает только DoorMinigameController (у него есть текущее целевое
    // число героя).
    [RequireComponent(typeof(SpriteRenderer))]
    public class DoorView : MonoBehaviour
    {
        [SerializeField] private TextMeshPro expressionLabel;
        [SerializeField] private SpriteRenderer doorRenderer;
        [SerializeField] private Color lockedFlashColor = new Color(0.45f, 0.45f, 0.45f);
        [SerializeField] private Color ambushFlashColor = new Color(0.8f, 0.15f, 0.15f);

        private Action<DoorView> _onChosen;

        public int Answer { get; private set; }

        public void Init(MathProblem problem, Action<DoorView> onChosen)
        {
            Answer = problem.Answer;
            expressionLabel.text = $"{problem.Expression} = x";
            _onChosen = onChosen;
        }

        private void OnMouseDown()
        {
            _onChosen?.Invoke(this);
        }

        public void PlayOpenCorrect(Action onComplete)
        {
            _onChosen = null;
            var targetScale = new Vector3(transform.localScale.x, transform.localScale.y * 0.05f, transform.localScale.z);
            StartCoroutine(SimpleTween.ScaleTo(transform, targetScale, 0.3f, onComplete));
        }

        // Упрощённый режим: дверь просто не открывается, герой не погибает.
        public void PlayLocked()
        {
            _onChosen = null;
            StartCoroutine(SimpleTween.ColorFlash(doorRenderer, lockedFlashColor, 0.3f));
        }

        // Обычный режим: из двери выходит голем и побеждает героя. Пока
        // это просто цветовая вспышка — заменится на спрайт голема и его
        // анимацию, когда появится финальный арт (см. чат про арт-ассеты).
        public void PlayGolemAmbush(Action onComplete)
        {
            _onChosen = null;
            StartCoroutine(SimpleTween.ColorFlash(doorRenderer, ambushFlashColor, 0.5f, onComplete));
        }
    }
}
