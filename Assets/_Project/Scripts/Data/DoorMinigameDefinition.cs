using UnityEngine;

namespace MathGame.Data
{
    [CreateAssetMenu(menuName = "MathGame/Minigames/Door Minigame", fileName = "DoorMinigame_")]
    public class DoorMinigameDefinition : MinigameDefinition
    {
        // 4 — проверено, что при такой ширине ряд дверей с подписями
        // помещается на экране при любой форме окна (см. cameraOrthographicSize
        // и doorsScreenFraction в DoorMinigameController — они подобраны
        // именно под это число).
        public int doorsPerRound = 4;
        public int correctDoorsRequired = 3;
    }
}
