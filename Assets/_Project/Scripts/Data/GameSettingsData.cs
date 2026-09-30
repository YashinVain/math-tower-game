using System;
using UnityEngine;

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

        // Верхний предел для numberRangeMin. Найденный в игре баг: сумма
        // двух чисел из диапазона (сложение) не может быть меньше
        // 2×numberRangeMin — а "сразу проходимому" примеру в первой башне
        // уровня иногда нужна цель меньше этого (герой стартует с силой 5,
        // а цель для него может быть от 0 до 5). Если numberRangeMin
        // слишком большой (например, 4 — тогда 2×4=8 > 5), такую цель
        // построить сложением вообще нельзя, и генератор молча подставлял
        // ближайшее возможное число — БОЛЬШЕ, чем сила героя, из-за чего
        // этаж становился непроходимым (и из-за одинаковой "ближайшей
        // возможной" пары могли появиться два одинаковых примера сразу —
        // ровно то, что увидел игрок: "4 + 4" дважды при силе героя 5). 2 —
        // с запасом (2×2=4 < 5).
        public const int MaxNumberRangeMin = 2;

        // Гарантирует допустимый диапазон — вызывается и здесь
        // (SettingsService.Load, при каждом запуске игры — иначе старое
        // сохранение с узким или слишком высоким numberRangeMin так и
        // оставалось бы таким, пока игрок сам не откроет экран настроек), и
        // из SettingsPanel при live-редактировании слайдеров.
        public void ClampNumberRange()
        {
            numberRangeMin = Mathf.Clamp(numberRangeMin, 0, MaxNumberRangeMin);
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
