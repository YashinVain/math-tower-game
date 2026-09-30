using System.Collections.Generic;
using UnityEngine;
using MathGame.Data;

namespace MathGame.MathGen
{
    // Дроби и отрицательные числа для этой аудитории (6-16 лет, младшие в
    // том числе) сбивают с толку, поэтому генератор всегда строит примеры
    // так, чтобы результат был целым неотрицательным числом: вычитание
    // никогда не уходит в минус, деление всегда подбирается без остатка.
    public class MathProblemGenerator : IMathProblemGenerator
    {
        public MathProblem Generate(DifficultyContext context)
        {
            switch (PickOperation(context.AllowedOperations))
            {
                case MathOperation.Subtraction:
                {
                    int a = Random.Range(context.MinValue, context.MaxValue + 1);
                    int b = Random.Range(context.MinValue, a + 1); // b <= a: без отрицательного результата
                    return new MathProblem($"{a} - {b}", a - b);
                }
                case MathOperation.Multiplication:
                {
                    int a = Random.Range(context.MinValue, context.MaxValue + 1);
                    int b = Random.Range(context.MinValue, context.MaxValue + 1);
                    return new MathProblem($"{a} × {b}", a * b);
                }
                case MathOperation.Division:
                {
                    // Строим "от ответа": делитель и частное сначала, делимое — их произведение.
                    // Так деление гарантированно без остатка.
                    int divisor = Random.Range(Mathf.Max(1, context.MinValue), context.MaxValue + 1);
                    int quotient = Random.Range(context.MinValue, context.MaxValue + 1);
                    int dividend = divisor * quotient;
                    return new MathProblem($"{dividend} : {divisor}", quotient);
                }
                default:
                {
                    int a = Random.Range(context.MinValue, context.MaxValue + 1);
                    int b = Random.Range(context.MinValue, context.MaxValue + 1);
                    return new MathProblem($"{a} + {b}", a + b);
                }
            }
        }

        public MathProblem GenerateWithAnswer(DifficultyContext context, int targetAnswer)
        {
            switch (PickOperation(context.AllowedOperations))
            {
                case MathOperation.Subtraction:
                {
                    // a - b = targetAnswer, и a, и b должны остаться в
                    // [MinValue, MaxValue]. Раньше b подбирался в диапазоне,
                    // а a = targetAnswer + b считался "как получится" — при
                    // большом targetAnswer a мог вылезти далеко за MaxValue.
                    // Теперь границы b считаются так, чтобы a гарантированно
                    // остался в диапазоне тоже.
                    if (TryBoundsForSum(context, targetAnswer, out int bLo, out int bHi))
                    {
                        int b = Random.Range(bLo, bHi + 1);
                        int a = targetAnswer + b;
                        return new MathProblem($"{a} - {b}", targetAnswer);
                    }
                    goto default;
                }
                case MathOperation.Multiplication:
                {
                    int factor = FindFactorWithinRange(targetAnswer, context);
                    if (factor > 0)
                        return new MathProblem($"{factor} × {targetAnswer / factor}", targetAnswer);
                    goto default; // не нашли красивый множитель — не страшно, покажем как сложение
                }
                case MathOperation.Division:
                {
                    int divisor = Random.Range(Mathf.Max(1, context.MinValue), context.MaxValue + 1);
                    int dividend = divisor * targetAnswer;
                    return new MathProblem($"{dividend} : {divisor}", targetAnswer);
                }
                default:
                {
                    // a + b = targetAnswer, и a, и b должны остаться в
                    // [MinValue, MaxValue]. Раньше проверялось только a
                    // (через Mathf.Max(aMin, ...), который на самом деле
                    // просто ИГНОРИРОВАЛ верхнюю границу, когда targetAnswer
                    // был большим) — а всё "лишнее" молча утекало в b без
                    // всякой проверки. Отсюда примеры вроде "11 + 6" при
                    // настройках "числа от 0 до 6": 11 просто никто не
                    // проверял. TryBoundsForSum считает ПЕРЕСЕЧЕНИЕ условий
                    // "a в диапазоне" и "b = target-a тоже в диапазоне" —
                    // если такого a не существует вообще (targetAnswer
                    // больше, чем могут дать два числа из диапазона), это
                    // равносильно тому, что герой стал сильнее, чем
                    // "видимые" числа в настройках вообще способны выразить
                    // сложением двух штук — такого быть не должно (см.
                    // ограничение силы героя в TowerMinigameController), но
                    // на всякий случай не ломаем диапазон, а берём ближайшее
                    // корректное число.
                    if (TryBoundsForSum(context, targetAnswer, out int aLo, out int aHi))
                    {
                        int a = Random.Range(aLo, aHi + 1);
                        int b = targetAnswer - a;
                        return new MathProblem($"{a} + {b}", targetAnswer);
                    }
                    else
                    {
                        int aSafe = Mathf.Clamp(targetAnswer / 2, context.MinValue, context.MaxValue);
                        int bSafe = Mathf.Clamp(targetAnswer - aSafe, context.MinValue, context.MaxValue);
                        return new MathProblem($"{aSafe} + {bSafe}", aSafe + bSafe);
                    }
                }
            }
        }

        // Границы для первого слагаемого/вычитаемого a, при которых ВТОРОЕ
        // число (targetAnswer - a) тоже гарантированно остаётся в
        // [MinValue, MaxValue]. Возвращает false, если такого a не
        // существует совсем (сумма/разность двух чисел из диапазона не
        // может дать targetAnswer).
        private static bool TryBoundsForSum(DifficultyContext context, int targetAnswer, out int lo, out int hi)
        {
            lo = Mathf.Max(context.MinValue, targetAnswer - context.MaxValue);
            hi = Mathf.Min(context.MaxValue, targetAnswer - context.MinValue);
            return lo <= hi;
        }

        private static int FindFactorWithinRange(int target, DifficultyContext context)
        {
            if (target <= 0) return 0;
            int lo = Mathf.Max(1, context.MinValue);
            int hi = Mathf.Max(lo, context.MaxValue);
            for (int factor = lo; factor <= hi; factor++)
            {
                // Проверяем и сам множитель, и то, что получится во втором
                // числе (target/factor) — второе раньше не проверялось
                // вообще и тоже могло вылезти за диапазон.
                if (target % factor == 0 && target / factor <= context.MaxValue) return factor;
            }
            return 0;
        }

        private static MathOperation PickOperation(MathOperation allowed)
        {
            if (allowed == MathOperation.None) return MathOperation.Addition;

            var candidates = new List<MathOperation>(4);
            if ((allowed & MathOperation.Addition) != 0) candidates.Add(MathOperation.Addition);
            if ((allowed & MathOperation.Subtraction) != 0) candidates.Add(MathOperation.Subtraction);
            if ((allowed & MathOperation.Multiplication) != 0) candidates.Add(MathOperation.Multiplication);
            if ((allowed & MathOperation.Division) != 0) candidates.Add(MathOperation.Division);
            return candidates[Random.Range(0, candidates.Count)];
        }
    }
}
