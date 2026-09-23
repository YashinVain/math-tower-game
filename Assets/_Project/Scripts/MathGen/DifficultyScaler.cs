using UnityEngine;
using MathGame.Data;

namespace MathGame.MathGen
{
    // Единственное место, где "настройки игрока + номер уровня" превращаются
    // в конкретный диапазон чисел для генератора примеров. Понадобится другая
    // формула роста сложности (например, по просьбе преподавателя) — меняется
    // только эта функция, а не код мини-игр.
    public static class DifficultyScaler
    {
        public static DifficultyContext Build(GameSettingsData settings, int levelIndex)
        {
            int min = settings.numberRangeMin;
            int max = settings.numberRangeMax;

            if (settings.difficultyGrowthEnabled && levelIndex > 0)
            {
                float growth = 1f + settings.difficultyGrowthRate * levelIndex;
                max = Mathf.RoundToInt(max * growth);
            }

            max = Mathf.Max(min, max);
            return new DifficultyContext(min, max, settings.ToAllowedOperations());
        }
    }
}
