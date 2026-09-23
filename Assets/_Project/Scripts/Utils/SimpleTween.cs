using System;
using System.Collections;
using UnityEngine;

namespace MathGame.Utils
{
    // Простые корутины-твины без внешних зависимостей (без DOTween) — этого
    // достаточно, пока нет финального арта с покадровой анимацией; когда он
    // появится, вызовы этих методов легко заменить на Animator, не трогая
    // логику мини-игр. Все используют Time.deltaTime, поэтому сами
    // останавливаются на паузе (Time.timeScale = 0).
    public static class SimpleTween
    {
        public static IEnumerator MoveTo(Transform target, Vector3 destination, float duration, Action onComplete = null)
        {
            Vector3 start = target.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                target.position = Vector3.Lerp(start, destination, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            target.position = destination;
            onComplete?.Invoke();
        }

        public static IEnumerator ScaleTo(Transform target, Vector3 destinationScale, float duration, Action onComplete = null)
        {
            Vector3 start = target.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                target.localScale = Vector3.Lerp(start, destinationScale, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            target.localScale = destinationScale;
            onComplete?.Invoke();
        }

        public static IEnumerator ColorFlash(SpriteRenderer renderer, Color flashColor, float duration, Action onComplete = null)
        {
            Color baseColor = renderer.color;
            float half = duration * 0.5f;
            float elapsed = 0f;

            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                renderer.color = Color.Lerp(baseColor, flashColor, elapsed / half);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                renderer.color = Color.Lerp(flashColor, baseColor, elapsed / half);
                yield return null;
            }

            renderer.color = baseColor;
            onComplete?.Invoke();
        }
    }
}
