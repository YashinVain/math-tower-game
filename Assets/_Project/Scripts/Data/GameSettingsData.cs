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
        // Было 10 — подняли до 16. Нижняя причина: в башнях сила героя
        // растёт с каждой победой и должна уместиться в то, что вообще
        // можно показать суммой двух чисел из диапазона (2×numberRangeMax).
        // При слишком узком диапазоне сила героя быстро упирается в свой
        // потолок (см. EffectivePowerCap в TowerMinigameController), и
        // тогда ВСЕ примеры на этаже разом становятся "сразу верными" —
        // баг, который игрок явно видел и просил исключить настройками.
        // 16 даёт достаточный запас для текущего набора башен (2→3→4 этажа,
        // +1 к силе за победу); см. также MinNumberRangeWidth ниже.
        public int numberRangeMax = 16;

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

        // Минимальная ширина диапазона чисел (numberRangeMax − numberRangeMin).
        // Слишком узкий диапазон не оставляет силе героя в башнях места
        // расти, прежде чем она упрётся в свой потолок (EffectivePowerCap в
        // TowerMinigameController, рассчитанный от numberRangeMax) — а как
        // только герой застревает РОВНО на потолке, вообще ВСЕ примеры
        // следующей башни разом становятся "сразу верными" (см.
        // ClampNumberRange). 14 — с запасом под текущий набор башен (этажи
        // 2→3→4, сила растёт на +1 за победу, герой стартует с силой 5).
        public const int MinNumberRangeWidth = 14;

        // Гарантирует минимальную ширину диапазона — вызывается и здесь
        // (SettingsService.Load, при каждом запуске игры — иначе старое
        // сохранение с узким диапазоном так и оставалось бы узким, пока
        // игрок сам не откроет экран настроек), и из SettingsPanel при
        // live-редактировании слайдеров.
        public void ClampNumberRange()
        {
            if (numberRangeMax - numberRangeMin < MinNumberRangeWidth)
                numberRangeMax = numberRangeMin + MinNumberRangeWidth;
        }

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
