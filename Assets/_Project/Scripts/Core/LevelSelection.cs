using MathGame.Data;

namespace MathGame.Core
{
    // Простой "почтовый ящик" для передачи выбранного уровня из сцены Menu
    // в сцену Gameplay. Полноценная шина событий тут избыточна — сцены
    // переключаются последовательно, и в любой момент есть только один
    // "текущий выбор".
    public static class LevelSelection
    {
        public static LevelDefinition Current { get; set; }
        public static int CurrentIndex { get; set; }
    }
}
