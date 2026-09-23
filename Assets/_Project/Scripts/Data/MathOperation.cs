using System;

namespace MathGame.Data
{
    // [Flags] позволяет хранить сразу несколько операций в одном значении
    // и проверять "включена ли операция X" через побитовое И.
    [Flags]
    public enum MathOperation
    {
        None = 0,
        Addition = 1 << 0,
        Subtraction = 1 << 1,
        Multiplication = 1 << 2,
        Division = 1 << 3
    }
}
