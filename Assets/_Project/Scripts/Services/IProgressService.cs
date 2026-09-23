using MathGame.Data;

namespace MathGame.Services
{
    public interface IProgressService
    {
        bool IsLevelCompleted(string levelId);
        bool IsLevelUnlocked(LevelCatalog catalog, string levelId);
        void MarkLevelCompleted(string levelId);
        void ResetProgress();
    }
}
