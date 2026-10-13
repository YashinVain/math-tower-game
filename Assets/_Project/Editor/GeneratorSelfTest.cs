using UnityEditor;
using UnityEngine;
using MathGame.Data;
using MathGame.MathGen;

namespace MathGame.EditorTools
{
    // Самопроверка генератора примеров: для множества настроек (диапазоны и
    // наборы действий) просит у генератора пример с заданным ответом и
    // проверяет, что ответ действительно тот самый. Эта проверка ловит баг,
    // из-за которого на уровне с дверями не было ни одной правильной двери
    // (цель 1, диапазон 1..15, сложение: "1 + 1" вместо верного примера).
    // Запуск: меню MathGame/Run Generator Self-Test или из командной строки
    //   Unity.exe -batchmode -nographics -quit -projectPath ... -executeMethod MathGame.EditorTools.GeneratorSelfTest.Run
    public static class GeneratorSelfTest
    {
        [MenuItem("MathGame/Run Generator Self-Test")]
        public static void Run()
        {
            var generator = new MathProblemGenerator();
            int checkedCount = 0;
            int failures = 0;

            // Все непустые наборы действий.
            for (int mask = 1; mask < 16; mask++)
            {
                var operations = (MathOperation)0;
                if ((mask & 1) != 0) operations |= MathOperation.Addition;
                if ((mask & 2) != 0) operations |= MathOperation.Subtraction;
                if ((mask & 4) != 0) operations |= MathOperation.Multiplication;
                if ((mask & 8) != 0) operations |= MathOperation.Division;

                foreach (var (min, max) in new[] { (0, 5), (1, 5), (1, 10), (1, 15), (2, 20), (5, 40), (10, 100) })
                {
                    var context = new DifficultyContext(min, max, operations);

                    // Цели строим так же, как игра: ответом обычного примера.
                    for (int i = 0; i < 400; i++)
                    {
                        int target = generator.Generate(context).Answer;
                        var problem = generator.GenerateWithAnswer(context, target);
                        checkedCount++;
                        if (problem.Answer != target)
                        {
                            failures++;
                            if (failures <= 10)
                                Debug.LogError($"Самотест: цель {target}, пример \"{problem.Expression}\" даёт {problem.Answer} (диапазон {min}..{max}, действия {operations})");
                        }
                    }
                }
            }

            if (failures == 0) Debug.Log($"Самотест генератора пройден: {checkedCount} проверок, ошибок нет.");
            else Debug.LogError($"Самотест генератора: ошибок {failures} из {checkedCount}.");

            if (Application.isBatchMode) EditorApplication.Exit(failures == 0 ? 0 : 1);
        }
    }
}
