using System;
using UnityEngine;
using MathGame.Utils;

namespace MathGame.Minigames.Towers
{
    // Визуальное представление героя в мини-игре "Башни": движение и простые
    // цветовые реакции на победу/поражение. Игровой логики тут нет — ею
    // управляет TowerMinigameController, этот класс только показывает.
    [RequireComponent(typeof(SpriteRenderer))]
    public class HeroView : MonoBehaviour
    {
        [SerializeField] private Color victoryFlashColor = new Color(1f, 0.92f, 0.4f);
        [SerializeField] private Color defeatFlashColor = new Color(0.8f, 0.15f, 0.15f);

        private SpriteRenderer _renderer;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            CameraFollowX.Target = transform;
        }

        public void MoveTo(Vector3 destination, float duration, Action onComplete = null)
        {
            StartCoroutine(SimpleTween.MoveTo(transform, destination, duration, onComplete));
        }

        public void PlayVictoryPulse(Action onComplete = null)
        {
            StartCoroutine(SimpleTween.ColorFlash(_renderer, victoryFlashColor, 0.35f, onComplete));
        }

        public void PlayDefeat(Action onComplete = null)
        {
            StartCoroutine(SimpleTween.ColorFlash(_renderer, defeatFlashColor, 0.6f, onComplete));
        }
    }
}
