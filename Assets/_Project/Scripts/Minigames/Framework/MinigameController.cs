using System;
using UnityEngine;
using MathGame.Data;

namespace MathGame.Minigames.Framework
{
    // Общий контракт, которому подчиняется любая мини-игра (башни, двери и
    // любой будущий тип). GameplayController работает только с этим базовым
    // классом и не знает, что происходит внутри конкретной мини-игры — это
    // и даёт возможность добавлять новые типы мини-игр, не трогая игровой
    // поток.
    public abstract class MinigameController : MonoBehaviour
    {
        public event Action Completed;
        public event Action Failed;

        public abstract void Begin(MinigameDefinition definition, MinigameRuntimeContext context);

        protected void RaiseCompleted() => Completed?.Invoke();
        protected void RaiseFailed() => Failed?.Invoke();
    }
}
