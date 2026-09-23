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
    }
}
