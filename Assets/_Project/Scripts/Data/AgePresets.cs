using UnityEngine;

namespace MathGame.Data
{
    public enum AgePreset
    {
        Age6to8,
        Age9to11,
        Age12to14,
        Age15to16
    }

    // Быстрая автонастройка игры под возраст игрока: одна кнопка в
    // настройках выставляет сразу всё, что влияет на сложность — диапазон
    // чисел, какие действия включены, скорость роста сложности по уровням,
    // лимит времени и упрощённые двери. Все значения — в одной таблице
    // ниже (метод Apply), чтобы их было легко пересмотреть, не трогая
    // экран настроек. После нажатия игрок всё равно может подвинуть любой
    // ползунок руками — пресет это просто удобная отправная точка, а не
    // "заморозка" настроек.
    //
    // Ограничения самих настроек (минимальная ширина диапазона, потолок
    // для "Мин. числа") применяются в самом конце через ClampNumberRange,
    // так что даже ошибка в таблице не сможет выставить игру в состояние,
    // в котором уровни ломаются (см. GameSettingsData).
    public static class AgePresets
    {
        public static void Apply(GameSettingsData s, AgePreset preset)
        {
            switch (preset)
            {
                case AgePreset.Age6to8:
                    // Самые маленькие числа, которые позволяют настройки
                    // (ширина диапазона не меньше MinNumberRangeWidth),
                    // только сложение и вычитание, много времени, а
                    // двери прощают ошибку.
                    Set(s, min: 1, max: 15, add: true, sub: true, mul: false, div: false,
                        growth: 0.08f, timeSeconds: 240f, doorsEasy: true);
                    break;

                case AgePreset.Age9to11:
                    Set(s, min: 1, max: 20, add: true, sub: true, mul: true, div: false,
                        growth: 0.12f, timeSeconds: 180f, doorsEasy: false);
                    break;

                case AgePreset.Age12to14:
                    Set(s, min: 1, max: 30, add: true, sub: true, mul: true, div: true,
                        growth: 0.15f, timeSeconds: 150f, doorsEasy: false);
                    break;

                case AgePreset.Age15to16:
                    Set(s, min: 2, max: 40, add: true, sub: true, mul: true, div: true,
                        growth: 0.15f, timeSeconds: 120f, doorsEasy: false);
                    break;
            }

            s.ClampNumberRange();
        }

        // Совпадают ли текущие настройки с пресетом — экран настроек по
        // этому подсвечивает выбранную кнопку (если игрок после выбора
        // пресета что-то подвинул руками, подсветка пропадает: это уже
        // "свои" настройки).
        public static bool Matches(GameSettingsData s, AgePreset preset)
        {
            var expected = s.Clone();
            Apply(expected, preset);

            return s.numberRangeMin == expected.numberRangeMin
                && s.numberRangeMax == expected.numberRangeMax
                && s.additionEnabled == expected.additionEnabled
                && s.subtractionEnabled == expected.subtractionEnabled
                && s.multiplicationEnabled == expected.multiplicationEnabled
                && s.divisionEnabled == expected.divisionEnabled
                && s.difficultyGrowthEnabled == expected.difficultyGrowthEnabled
                && Mathf.Approximately(s.difficultyGrowthRate, expected.difficultyGrowthRate)
                && Mathf.Approximately(s.levelTimeLimitSeconds, expected.levelTimeLimitSeconds)
                && s.doorsEasyModeEnabled == expected.doorsEasyModeEnabled;
        }

        private static void Set(GameSettingsData s, int min, int max,
            bool add, bool sub, bool mul, bool div,
            float growth, float timeSeconds, bool doorsEasy)
        {
            s.numberRangeMin = min;
            s.numberRangeMax = max;
            s.additionEnabled = add;
            s.subtractionEnabled = sub;
            s.multiplicationEnabled = mul;
            s.divisionEnabled = div;
            s.difficultyGrowthEnabled = true;
            s.difficultyGrowthRate = growth;
            s.levelTimeLimitSeconds = timeSeconds;
            s.doorsEasyModeEnabled = doorsEasy;
        }
    }
}
