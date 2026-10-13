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

        // Размер текста примера (в "мировых" единицах TextMeshPro: 1.0 ≈ 0.1
        // единицы высоты в игровом мире). Крупный и жирный, как числа в играх
        // этого жанра. Можно поменять прямо в Inspector (префаб Golem) без
        // пересборки проекта.
        [SerializeField] private float numberFontSize = 10f;
        // Самый длинный пример не должен уходить далеко влево (к герою): если
        // текст шире этого значения (в единицах мира), шрифт уменьшается.
        [SerializeField] private float maxNumberWidth = 2.6f;

        // Подпись стоит СЛЕВА от башни, на чистом фоне рядом со своим
        // гоблином, а не над головой: над головой текст ложился на камень и
        // площадки башни и выглядел неряшливо. labelRightEdge — где кончается
        // правый край текста (относительно центра башни; стена башни ±0.87,
        // площадка ±1.14, поэтому -1.3 — вплотную слева от башни),
        // labelCenterY — высота центра текста над "ногами" гоблина
        // (примерно на уровне груди).
        [SerializeField] private float labelRightEdge = -1.3f;
        [SerializeField] private float labelCenterY = 0.95f;

        private Action<GolemView> _onSelected;

        public int Answer { get; private set; }

        // Размер подписи после подгонки по ширине: контроллер ставит самый
        // маленький из всех гоблинов башни каждому, чтобы числа были одного размера.
        public float LabelFontSize => expressionLabel.fontSize;

        public void SetLabelFontSize(float size)
        {
            expressionLabel.fontSize = size;
            Vector2 textSize = LabelLayout.Measure(expressionLabel);
            expressionLabel.transform.localPosition = new Vector3(labelRightEdge - textSize.x * 0.5f, labelCenterY, 0f);
        }

        public void Init(MathProblem problem, Action<GolemView> onSelected)
        {
            Answer = problem.Answer;
            // Красный цвет — "пример, который нужно решить" (синий у героя).
            LabelStyle.Apply(expressionLabel, LabelStyle.ProblemColor, numberFontSize);
            // Только пример, без "x =": как в играх этого жанра, где над
            // врагом просто крупное число, — так оно может быть заметно крупнее.
            expressionLabel.text = problem.Expression;
            LabelStyle.FitWidth(expressionLabel, maxNumberWidth);
            _onSelected = onSelected;

            PickVariant();

            // Положение слева от башни: центр подписи сдвигается так, чтобы
            // правый край текста стоял ровно на labelRightEdge при любой
            // длине примера.
            Vector2 textSize = LabelLayout.Measure(expressionLabel);
            expressionLabel.transform.localPosition = new Vector3(labelRightEdge - textSize.x * 0.5f, labelCenterY, 0f);
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
