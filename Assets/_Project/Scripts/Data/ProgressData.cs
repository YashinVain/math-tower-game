using System;
using System.Collections.Generic;

namespace MathGame.Data
{
    [Serializable]
    public class ProgressData
    {
        public List<string> completedLevelIds = new List<string>();

        public bool IsCompleted(string levelId) => completedLevelIds.Contains(levelId);

        public void MarkCompleted(string levelId)
        {
            if (!completedLevelIds.Contains(levelId))
                completedLevelIds.Add(levelId);
        }
    }
}
