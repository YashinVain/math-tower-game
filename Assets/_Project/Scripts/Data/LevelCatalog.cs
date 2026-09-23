using System.Collections.Generic;
using UnityEngine;

namespace MathGame.Data
{
    // Единственный ассет, который перечисляет все уровни игры по порядку.
    // Порядок в списке = порядок на экране выбора уровня = порядок
    // разблокировки. Добавить уровень — добавить элемент в список; убрать —
    // удалить элемент; поменять местами — перетащить в инспекторе.
    [CreateAssetMenu(menuName = "MathGame/Level Catalog", fileName = "LevelCatalog")]
    public class LevelCatalog : ScriptableObject
    {
        public List<LevelDefinition> levels = new List<LevelDefinition>();
    }
}
