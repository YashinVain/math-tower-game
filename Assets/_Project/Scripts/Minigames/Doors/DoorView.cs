using System;
using System.Collections;
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
    //
    // У двери три картинки: закрытая (по умолчанию), открытая (выбрана
    // правильно — внутри золотой свет) и запертая на замок и цепи
    // (упрощённый режим: неверная дверь просто не открывается). Если в
    // обычном режиме выбрана неверная дверь — дверь открывается и из неё
    // выскакивает гоблин (ambusherRenderer).
    [RequireComponent(typeof(SpriteRenderer))]
    public class DoorView : MonoBehaviour
    {
        [SerializeField] private TextMeshPro expressionLabel;
        [SerializeField] private SpriteRenderer doorRenderer;
        [SerializeField] private Sprite openSprite;
        [SerializeField] private Sprite lockedSprite;
        [SerializeField] private SpriteRenderer ambusherRenderer;

        // Размер текста примера над дверью. Чуть меньше, чем у гоблинов
        // (6.5): четыре подписи стоят в ряд с шагом до 2.7, и самый длинный
        // пример (например "144 : 12 = x") шрифтом Lilita One при 6.2
        // занимает около 2.4 — ещё помещается, не наезжая на соседнюю дверь.
        // Можно менять в Inspector (префаб Door), но не слишком сильно по
        // этой причине.
        [SerializeField] private float doorNumberSize = 8f;
        // Если пример шире этого значения (в единицах двери), шрифт уменьшается:
        // четыре подписи стоят в ряд, и соседние не должны наезжать друг на друга.
        [SerializeField] private float doorNumberMaxWidth = 2.35f;
        // Зазор между верхом двери и нижним краем текста над ней.
        [SerializeField] private float labelGap = 0.12f;
        // Во сколько раз гоблин-засада крупнее своего обычного размера:
        // дверь стала высокой (2.8), и гоблин высотой 1.7 терялся бы в
        // проёме.
        [SerializeField] private float ambusherScale = 1.25f;

        private Action<DoorView> _onChosen;

        public int Answer { get; private set; }

        // Размер подписи после подгонки по ширине. Контроллер берёт самый
        // маленький из четырёх и ставит его всем дверям ряда, чтобы числа на
        // соседних дверях были одного размера.
        public float LabelFontSize => expressionLabel.fontSize;

        public void SetLabelFontSize(float size)
        {
            expressionLabel.fontSize = size;
            FitToSprite(); // заново поставить подпись над дверью
        }

        public void Init(MathProblem problem, Action<DoorView> onChosen)
        {
            Answer = problem.Answer;
            LabelStyle.Apply(expressionLabel, LabelStyle.ProblemColor, doorNumberSize);
            expressionLabel.text = problem.Expression;
            LabelStyle.FitWidth(expressionLabel, doorNumberMaxWidth);
            _onChosen = onChosen;

            FitToSprite();
        }

        // Кликабельная область и подпись подгоняются под РЕАЛЬНЫЙ размер
        // картинки двери, а не под число, запомненное при сборке префаба.
        // Тогда размер двери можно менять (ArtSpecs.DoorHeight), не
        // пересобирая префаб: коллайдер и подпись всегда совпадут с картинкой.
        private void FitToSprite()
        {
            if (doorRenderer == null || doorRenderer.sprite == null) return;

            Bounds bounds = doorRenderer.sprite.bounds;

            var box = GetComponent<BoxCollider2D>();
            if (box != null)
            {
                box.size = bounds.size;
                box.offset = bounds.center;
            }

            // Подпись над дверью, нижний край текста на labelGap выше верха
            // двери (белая обводка делает её читаемой и на светлой стене
            // замка, и на тёмной).
            LabelLayout.PlaceAbove(expressionLabel, doorRenderer, labelGap);
        }

        private void OnMouseDown()
        {
            _onChosen?.Invoke(this);
        }

        // Правильная дверь: открывается (золотой свет), слегка "подпрыгивает",
        // через долю секунды мини-игра идёт дальше.
        public void PlayOpenCorrect(Action onComplete)
        {
            _onChosen = null;
            if (openSprite != null) doorRenderer.sprite = openSprite;
            StartCoroutine(OpenRoutine(onComplete));
        }

        private IEnumerator OpenRoutine(Action onComplete)
        {
            Vector3 baseScale = transform.localScale;
            yield return SimpleTween.ScaleTo(transform, baseScale * 1.06f, 0.12f);
            yield return SimpleTween.ScaleTo(transform, baseScale, 0.12f);
            yield return SimpleTween.Wait(0.35f);
            onComplete?.Invoke();
        }

        // Упрощённый режим: дверь запирается на замок и цепи и дрожит, герой
        // не погибает, можно выбрать другую дверь.
        public void PlayLocked()
        {
            _onChosen = null;
            if (lockedSprite != null) doorRenderer.sprite = lockedSprite;
            StartCoroutine(ShakeRoutine());
        }

        private IEnumerator ShakeRoutine()
        {
            Vector3 start = transform.localPosition;
            const float duration = 0.35f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float strength = 1f - elapsed / duration;
                transform.localPosition = start + new Vector3(Mathf.Sin(elapsed * 70f) * 0.09f * strength, 0f, 0f);
                yield return null;
            }
            transform.localPosition = start;
        }

        // Обычный режим: дверь открывается, из неё выскакивает гоблин и
        // побеждает героя, после этого мини-игра сообщает о поражении.
        public void PlayGolemAmbush(Action onComplete)
        {
            _onChosen = null;
            if (openSprite != null) doorRenderer.sprite = openSprite;
            StartCoroutine(AmbushRoutine(onComplete));
        }

        private IEnumerator AmbushRoutine(Action onComplete)
        {
            if (ambusherRenderer != null)
            {
                var ambusher = ambusherRenderer.transform;
                ambusher.localScale = Vector3.zero;
                ambusherRenderer.gameObject.SetActive(true);
                yield return SimpleTween.ScaleTo(ambusher, Vector3.one * ambusherScale, 0.25f);
            }
            yield return SimpleTween.Wait(0.6f);
            onComplete?.Invoke();
        }
    }
}
