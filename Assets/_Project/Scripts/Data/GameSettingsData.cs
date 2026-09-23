using System;

namespace MathGame.Data
{
    // Обычный сериализуемый класс, не ScriptableObject: это не ассет проекта,
    // а сохранение пользователя, которое живёт в JSON-файле на диске игрока
    // (см. SettingsService). [Serializable] нужен для JsonUtility.
    [Serializable]
    public class GameSettingsData
    {
        public int numberRangeMin = 1;
        public int numberRangeMax = 10;

        public bool additionEnabled = true;
        public bool subtractionEnabled = false;
        public bool multiplicationEnabled = false;
        public bool divisionEnabled = false;

        public bool difficultyGrowthEnabled = true;

        // Доля, на которую верхняя граница диапазона чисел растёт за один
        // уровень. 0.15 значит "+15% к numberRangeMax на каждый следующий
        // уровень" — см. DifficultyScaler.Build.
        public float difficultyGrowthRate = 0.15f;

        public float levelTimeLimitSeconds = 90f;

        // Для младшей аудитории: при неверной двери герой не погибает,
        // просто дверь не открывается.
        public bool doorsEasyModeEnabled = false;

        public MathOperation ToAllowedOperations()
        {
            var ops = MathOperation.None;
            if (additionEnabled) ops |= MathOperation.Addition;
            if (subtractionEnabled) ops |= MathOperation.Subtraction;
            if (multiplicationEnabled) ops |= MathOperation.Multiplication;
            if (divisionEnabled) ops |= MathOperation.Division;
            return ops;
        }

        public GameSettingsData Clone()
        {
            return (GameSettingsData)MemberwiseClone();
        }
    }
}
