using MathGame.Data;

namespace MathGame.MathGen
{
    public interface IMathProblemGenerator
    {
        // Обычный случайный пример в заданном диапазоне/операциях.
        MathProblem Generate(DifficultyContext context);

        // Пример, ответ которого гарантированно равен targetAnswer. Нужен для
        // "Дверей": среди нескольких дверей ровно одна должна совпадать с
        // числом героя, и составляется она именно через этот метод.
        MathProblem GenerateWithAnswer(DifficultyContext context, int targetAnswer);
    }
}
