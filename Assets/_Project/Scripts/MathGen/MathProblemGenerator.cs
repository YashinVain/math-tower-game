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
        // Множители в умножении и делитель/частное в делении не больше 12 —
        // это "таблица умножения", которую реально учат в школе. Без этого
        // предела при диапазоне, например, 1..40 умножение выдавало бы
        // "37 × 29", а деление — "1160 : 40", что для игры на скорость
        // нечитаемо. Сложение и вычитание этим пределом не ограничены —
        // для них диапазон чисел из настроек работает как раньше.
        private const int MaxFactor = 12;

        private static int FactorMax(DifficultyContext context) =>
            Mathf.Max(context.MinValue, Mathf.Min(context.MaxValue, MaxFactor));

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
                    int factorMax = FactorMax(context);
                    int a = Random.Range(context.MinValue, factorMax + 1);
                    int b = Random.Range(context.MinValue, factorMax + 1);
                    return new MathProblem($"{a} × {b}", a * b);
                }
                case MathOperation.Division:
                {
                    // Строим "от ответа": делитель и частное сначала, делимое — их произведение.
                    // Так деление гарантированно без остатка.
                    int factorMax = FactorMax(context);
                    int divisor = Random.Range(Mathf.Max(1, context.MinValue), Mathf.Max(1, factorMax) + 1);
                    int quotient = Random.Range(context.MinValue, factorMax + 1);
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

        // Всегда возвращает пример, ответ которого РОВНО targetAnswer. Раньше,
        // если выбранное действие не могло дать нужный ответ в заданном
        // диапазоне (например, 1 нельзя получить сложением двух чисел от 1
        // до 15), метод молча возвращал "1 + 1" = 2 — и на уровне с дверями
        // не оставалось ни одной правильной двери. Теперь действия
        // перебираются в случайном порядке, пока одно из них не подойдёт.
        public MathProblem GenerateWithAnswer(DifficultyContext context, int targetAnswer)
        {
            var operations = AllowedOperationList(context.AllowedOperations);

            // Случайный порядок (тасование), чтобы примеры по-прежнему были разнообразными.
            for (int i = operations.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (operations[i], operations[j]) = (operations[j], operations[i]);
            }

            foreach (var operation in operations)
            {
                if (TryBuildWithAnswer(operation, context, targetAnswer, out var problem)) return problem;
            }

            // Ни одно действие не уложилось в диапазон (например, цель 1 при
            // разрешённом только сложении и числах от 1). Верный ответ важнее
            // красивого диапазона: ноль не ломает пример.
            return new MathProblem($"{targetAnswer} + 0", targetAnswer);
        }

        private static bool TryBuildWithAnswer(MathOperation operation, DifficultyContext context, int targetAnswer, out MathProblem problem)
        {
            switch (operation)
            {
                case MathOperation.Subtraction:
                {
                    // a - b = targetAnswer, и a, и b должны остаться в
                    // [MinValue, MaxValue]. a = targetAnswer + b, поэтому
                    // a ≤ MaxValue ⇔ b ≤ MaxValue − targetAnswer.
                    int bLo = context.MinValue;
                    int bHi = context.MaxValue - targetAnswer;
                    if (bLo <= bHi)
                    {
                        int b = Random.Range(bLo, bHi + 1);
                        int a = targetAnswer + b;
                        problem = new MathProblem($"{a} - {b}", targetAnswer);
                        return true;
                    }
                    break;
                }
                case MathOperation.Multiplication:
                {
                    int factor = FindFactorWithinRange(targetAnswer, context);
                    if (factor > 0)
                    {
                        problem = new MathProblem($"{factor} × {targetAnswer / factor}", targetAnswer);
                        return true;
                    }
                    break;
                }
                case MathOperation.Division:
                {
                    // Ответ (частное) тоже не больше MaxFactor — иначе
                    // делимое вырастало бы до сотен ("480 : 16").
                    int factorMax = FactorMax(context);
                    if (targetAnswer >= context.MinValue && targetAnswer <= factorMax)
                    {
                        int divisor = Random.Range(Mathf.Max(1, context.MinValue), Mathf.Max(1, factorMax) + 1);
                        problem = new MathProblem($"{divisor * targetAnswer} : {divisor}", targetAnswer);
                        return true;
                    }
                    break;
                }
                default:
                {
                    // a + b = targetAnswer, и a, и b должны остаться в
                    // [MinValue, MaxValue] (TryBoundsForSum считает
                    // пересечение обоих условий).
                    if (TryBoundsForSum(context, targetAnswer, out int aLo, out int aHi))
                    {
                        int a = Random.Range(aLo, aHi + 1);
                        problem = new MathProblem($"{a} + {targetAnswer - a}", targetAnswer);
                        return true;
                    }
                    break;
                }
            }

            problem = default;
            return false;
        }

        private static List<MathOperation> AllowedOperationList(MathOperation allowed)
        {
            var list = new List<MathOperation>(4);
            if (allowed == MathOperation.None) { list.Add(MathOperation.Addition); return list; }
            if ((allowed & MathOperation.Addition) != 0) list.Add(MathOperation.Addition);
            if ((allowed & MathOperation.Subtraction) != 0) list.Add(MathOperation.Subtraction);
            if ((allowed & MathOperation.Multiplication) != 0) list.Add(MathOperation.Multiplication);
            if ((allowed & MathOperation.Division) != 0) list.Add(MathOperation.Division);
            return list;
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
            int hi = Mathf.Max(lo, FactorMax(context)); // не больше MaxFactor — таблица умножения

            // Собираем ВСЕ подходящие множители и берём случайный — раньше
            // брался наименьший, из-за чего почти любая цель превращалась
            // в скучное "1 × цель". Множители, отличные от 1, предпочитаем
            // (если они есть).
            var any = new List<int>();
            var nonTrivial = new List<int>();
            for (int factor = lo; factor <= hi; factor++)
            {
                // Проверяем и сам множитель, и то, что получится во втором
                // числе (target/factor) — второе раньше не проверялось
                // вообще и тоже могло вылезти за диапазон.
                if (target % factor != 0 || target / factor > hi) continue;
                any.Add(factor);
                if (factor > 1 && target / factor > 1) nonTrivial.Add(factor);
            }

            var pool = nonTrivial.Count > 0 ? nonTrivial : any;
            return pool.Count > 0 ? pool[Random.Range(0, pool.Count)] : 0;
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
