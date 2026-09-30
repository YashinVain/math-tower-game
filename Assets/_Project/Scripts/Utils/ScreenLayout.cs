namespace MathGame.Utils
{
    // Раньше позиции героя/башни/дверей были зашитыми числами в мировых
    // координатах (например "герой на -2, башня на +5"). Это ломалось при
    // смене соотношения сторон окна: на широком окне всё сжималось к центру,
    // на узком — уезжало за край камеры (ровно то, с чем боролись несколько
    // раз подряд в сценах Tower/Door). AnchorAt в EditorBuildUtils решает
    // эту же проблему для UI-кнопок через якоря в долях экрана; этот класс —
    // то же самое, но для мировых координат игровой камеры: герой и цель
    // (башня или двери) всегда встают на одну и ту же долю ширины экрана от
    // левого края, какой бы ширины окно ни было — потому что доля считается
    // заново от РЕАЛЬНОГО текущего aspect камеры при каждом ConfigureCamera().
    public static class ScreenLayout
    {
        // Половина ширины видимой области камеры в мировых единицах.
        public static float HalfWidth(float orthographicSize, float cameraAspect)
            => orthographicSize * cameraAspect;

        // Насколько сдвинуть центр камеры вправо от героя (значение для
        // CameraFollowX.SetOffsetX), чтобы герой оказался на доле
        // heroScreenFraction ширины экрана от левого края (0 = у левого
        // края, 0.5 = по центру).
        public static float OffsetXForHeroFraction(float halfWidth, float heroScreenFraction)
            => halfWidth * (1f - 2f * heroScreenFraction);

        // На каком расстоянии от героя по X (в мировых единицах) поставить
        // цель (башню/центр группы дверей), чтобы она оказалась на доле
        // targetScreenFraction ширины экрана от левого края.
        public static float DistanceForTargetFraction(float halfWidth, float heroScreenFraction, float targetScreenFraction)
            => 2f * halfWidth * (targetScreenFraction - heroScreenFraction);
    }
}
