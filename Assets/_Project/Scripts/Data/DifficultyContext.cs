namespace MathGame.Data
{
    // Готовый набор параметров для генератора примеров: диапазон чисел и
    // разрешённые операции. Отдельная структура нужна, чтобы генератор не
    // зависел напрямую от настроек игрока и номера уровня — он просто
    // получает уже посчитанный DifficultyContext (см. DifficultyScaler).
    public readonly struct DifficultyContext
    {
        public readonly int MinValue;
        public readonly int MaxValue;
        public readonly MathOperation AllowedOperations;

        public DifficultyContext(int minValue, int maxValue, MathOperation allowedOperations)
        {
            MinValue = minValue;
            MaxValue = maxValue;
            AllowedOperations = allowedOperations;
        }
    }
}
