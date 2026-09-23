namespace MathGame.Data
{
    // Один сгенерированный пример: то, что видит игрок ("3 + 2"), и
    // правильный ответ, с которым сравнивается выбор игрока (выбранный
    // голем/дверь).
    public readonly struct MathProblem
    {
        public readonly string Expression;
        public readonly int Answer;

        public MathProblem(string expression, int answer)
        {
            Expression = expression;
            Answer = answer;
        }
    }
}
