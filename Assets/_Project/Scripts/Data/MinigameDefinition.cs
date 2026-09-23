using UnityEngine;

namespace MathGame.Data
{
    // Базовый класс "описания" одной мини-игры внутри уровня. Конкретные типы
    // (TowerMinigameDefinition, DoorMinigameDefinition, ...) наследуются от
    // него и добавляют свои поля баланса. LevelDefinition хранит список именно
    // этого базового типа — так в один уровень можно положить мини-игры
    // разных типов, не меняя LevelDefinition.
    //
    // controllerPrefab специально имеет тип GameObject, а не тип конкретного
    // класса-контроллера: класс контроллера живёт в другом слое кода
    // (Minigames/Framework), который сам ссылается на MinigameDefinition. Если
    // бы Definition ссылался на класс контроллера напрямую, слои зависели бы
    // друг от друга по кругу. GameObject разрывает этот цикл; нужный
    // компонент достаётся через GetComponent там, где префаб создаётся
    // (см. GameplayController).
    public abstract class MinigameDefinition : ScriptableObject
    {
        [SerializeField] private string minigameId;
        [SerializeField] private GameObject controllerPrefab;

        public string MinigameId => minigameId;
        public GameObject ControllerPrefab => controllerPrefab;
    }
}
