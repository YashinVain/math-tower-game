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
                    int b = Random.Range(0, context.MaxValue + 1);
                    int a = targetAnswer + b;
                    return new MathProblem($"{a} - {b}", targetAnswer);
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
                    int aMin = Mathf.Max(0, targetAnswer - context.MaxValue);
                    int aMax = Mathf.Max(aMin, Mathf.Min(targetAnswer, context.MaxValue));
                    int a = Random.Range(aMin, aMax + 1);
                    int b = targetAnswer - a;
                    return new MathProblem($"{a} + {b}", targetAnswer);
                }
            }
        }

        private static int FindFactorWithinRange(int target, DifficultyContext context)
        {
            if (target <= 0) return 0;
            int lo = Mathf.Max(1, context.MinValue);
            int hi = Mathf.Max(lo, context.MaxValue);
            for (int factor = lo; factor <= hi; factor++)
            {
                if (target % factor == 0) return factor;
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
