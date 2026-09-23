using MathGame.Data;
using MathGame.MathGen;

namespace MathGame.Minigames.Framework
{
    // Всё, что мини-игре нужно извне, но что сама она не должна создавать:
    // текущие настройки, номер уровня (для расчёта сложности) и общий
    // генератор примеров. Собирается GameplayController-ом и передаётся в
    // Begin() при запуске мини-игры.
    public readonly struct MinigameRuntimeContext
    {
        public readonly GameSettingsData Settings;
        public readonly int LevelIndex;
        public readonly IMathProblemGenerator ProblemGenerator;

        public MinigameRuntimeContext(GameSettingsData settings, int levelIndex, IMathProblemGenerator problemGenerator)
        {
            Settings = settings;
            LevelIndex = levelIndex;
            ProblemGenerator = problemGenerator;
        }

        public DifficultyContext BuildDifficulty() => DifficultyScaler.Build(Settings, LevelIndex);
    }
}
