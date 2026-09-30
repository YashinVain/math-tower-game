using System.Collections.Generic;
using UnityEngine;

namespace MathGame.Data
{
    [CreateAssetMenu(menuName = "MathGame/Minigames/Tower Minigame", fileName = "TowerMinigame_")]
    public class TowerMinigameDefinition : MinigameDefinition
    {
        // Одно число в списке = одна башня внутри мини-игры, значение —
        // сколько големов в этой башне. По умолчанию 2 -> 3 -> 4, как в
        // задании, но можно добавить/убрать/изменить прямо в инспекторе,
        // без единой строчки кода.
        public List<int> towerSizes = new List<int> { 2, 3, 4 };

        public int heroStartingPower = 5;

        // 0 = без ограничения. Если сила героя не должна расти бесконечно,
        // здесь задаётся потолок — после победы над големом сила героя не
        // поднимется выше этого значения.
        public int powerCap = 0;

        // Сколько големов в только что появившейся башне заведомо
        // "проходные" (их сила ≤ силы героя) — остальные заведомо сильнее
        // героя на этот момент. Без этого ограничения при большом запасе
        // силы героя относительно диапазона чисел все големы в башне могут
        // оказаться проходными одновременно, и выбор перестаёт быть
        // выбором.
        public int maxBeatableAtOnce = 2;
    }
}
