using System.Collections.Generic;
using UnityEngine;

namespace MathGame.Data
{
    [CreateAssetMenu(menuName = "MathGame/Level Definition", fileName = "Level_")]
    public class LevelDefinition : ScriptableObject
    {
        // levelId — стабильный идентификатор для сохранений: прогресс хранит
        // именно его, а не позицию в списке, чтобы порядок уровней в
        // LevelCatalog можно было менять, не сбрасывая прогресс игроков.
        public string levelId;
        public string displayName;

        public List<MinigameDefinition> sequence = new List<MinigameDefinition>();

        public LevelVisualTheme visualTheme;

        public bool overrideTimeLimit;
        public float timeLimitSecondsOverride = 90f;

        public float GetTimeLimitSeconds(GameSettingsData settings)
        {
            return overrideTimeLimit ? timeLimitSecondsOverride : settings.levelTimeLimitSeconds;
        }
    }
}
