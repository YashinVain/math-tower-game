using System;
using UnityEngine;

namespace MathGame.Core
{
    // Считает время через Time.deltaTime, поэтому автоматически "замирает"
    // вместе со всем остальным при Time.timeScale = 0 (пауза) — отдельный
    // код для остановки таймера на паузе не нужен.
    public class CountdownTimer : MonoBehaviour
    {
        public event Action Expired;

        public float TimeRemaining { get; private set; }
        public float TotalTime { get; private set; }
        public bool IsRunning { get; private set; }

        public void StartTimer(float seconds)
        {
            TotalTime = seconds;
            TimeRemaining = seconds;
            IsRunning = true;
        }

        public void Stop()
        {
            IsRunning = false;
        }

        private void Update()
        {
            if (!IsRunning) return;

            TimeRemaining -= Time.deltaTime;
            if (TimeRemaining <= 0f)
            {
                TimeRemaining = 0f;
                IsRunning = false;
                Expired?.Invoke();
            }
        }
    }
}
