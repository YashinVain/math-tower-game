using MathGame.Data;

namespace MathGame.Services
{
    public interface ISettingsService
    {
        GameSettingsData Current { get; }
        void Save();
        void ResetToDefaults();
    }
}
