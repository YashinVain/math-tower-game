using UnityEngine;
using MathGame.Data;
using MathGame.MathGen;
using MathGame.Services;

namespace MathGame.Core
{
    // Единая "точка входа" ко всем сервисам игры. Живёт на объекте в сцене
    // Boot и переживает переход между сценами (DontDestroyOnLoad), поэтому
    // доступна из Menu и Gameplay через статическое свойство Instance. Это
    // осознанно простой вариант Service Locator — полноценный DI-контейнер
    // для проекта такого размера был бы лишним усложнением.
    public class GameServices : MonoBehaviour
    {
        public static GameServices Instance { get; private set; }

        [SerializeField] private LevelCatalog levelCatalog;

        public LevelCatalog LevelCatalog => levelCatalog;
        public ISettingsService Settings { get; private set; }
        public IProgressService Progress { get; private set; }
        public IMathProblemGenerator ProblemGenerator { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Settings = new SettingsService();
            Progress = new ProgressService();
            ProblemGenerator = new MathProblemGenerator();
        }
    }
}
