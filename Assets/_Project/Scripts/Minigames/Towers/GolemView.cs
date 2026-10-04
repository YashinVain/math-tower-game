using System;
using UnityEngine;
using TMPro;
using MathGame.Data;
using MathGame.Utils;

namespace MathGame.Minigames.Towers
{
    // Один гоблин (в коде по-прежнему "голем" — так называлась сущность в
    // самом начале проекта) в башне: показывает пример в формате "x = ...",
    // хранит ответ и сообщает наружу о выборе игрока. Сам не решает,
    // победил герой или нет — это знает только TowerMinigameController (у
    // него есть текущая сила героя).
    [RequireComponent(typeof(SpriteRenderer))]
    public class GolemView : MonoBehaviour
    {
        [SerializeField] private TextMeshPro expressionLabel;

        // Несколько внешних видов гоблина (зелёный, тёмный в красном,
        // вожак в фиолетовом плаще): каждому новому гоблину в башне
        // выбирается случайный, чтобы башни не были однообразными. Высота у
        // всех одинаковая (задаётся при импорте картинок, см.
        // ArtImportSettings), поэтому подпись над головой стоит ровно.
        [SerializeField] private Sprite[] variants;

        // Размер текста примера над головой (в "мировых" единицах TextMeshPro:
        // 1.0 ≈ 0.1 единицы высоты в игровом мире). Задаётся здесь, а не
        // только при сборке префаба, чтобы размер можно было поменять прямо в
        // Inspector (выберите префаб Golem) без пересборки всего проекта.
        [SerializeField] private float labelFontSize = 4.5f;

        private Action<GolemView> _onSelected;

        public int Answer { get; private set; }

        public void Init(MathProblem problem, Action<GolemView> onSelected)
        {
            Answer = problem.Answer;
            expressionLabel.fontSize = labelFontSize;
            expressionLabel.text = $"x = {problem.Expression}";
            _onSelected = onSelected;

            PickVariant();
        }

        private void PickVariant()
        {
            if (variants == null || variants.Length == 0) return;

            var spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = variants[UnityEngine.Random.Range(0, variants.Length)];

            // Размер кликабельной области — по реальной картинке (у разных
            // вариантов она чуть разной ширины).
            var box = GetComponent<BoxCollider2D>();
            if (box != null)
            {
                Bounds bounds = spriteRenderer.sprite.bounds;
                box.size = bounds.size;
                box.offset = bounds.center;
            }
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
