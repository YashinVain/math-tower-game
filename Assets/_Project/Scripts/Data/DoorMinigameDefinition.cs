using UnityEngine;

namespace MathGame.Data
{
    [CreateAssetMenu(menuName = "MathGame/Minigames/Door Minigame", fileName = "DoorMinigame_")]
    public class DoorMinigameDefinition : MinigameDefinition
    {
        public int doorsPerRound = 4;
        public int correctDoorsRequired = 3;
    }
}
