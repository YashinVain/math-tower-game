using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using MathGame.Core;
using MathGame.Data;
using MathGame.Services;

namespace MathGame.UI.Menu
{
    // Список уровней строится заново каждый раз при открытии экрана —
    // из LevelCatalog. Новый уровень, добавленный в каталог, появится тут
    // сам, без изменений в этом классе (см. docs/ARCHITECTURE.md).
    public class LevelSelectPanel : MonoBehaviour
    {
        [SerializeField] private Transform buttonsContainer;
        [SerializeField] private LevelButton levelButtonPrefab;

        private readonly List<LevelButton> _spawned = new List<LevelButton>();

        public void Populate()
        {
            foreach (var button in _spawned)
                if (button != null) Destroy(button.gameObject);
            _spawned.Clear();

            var catalog = GameServices.Instance.LevelCatalog;
            var progress = GameServices.Instance.Progress;

            for (int i = 0; i < catalog.levels.Count; i++)
            {
                var level = catalog.levels[i];
                var state = ResolveState(progress, catalog, level.levelId);

                var button = Instantiate(levelButtonPrefab, buttonsContainer);
                int index = i;
                button.Setup(level, i + 1, state, () => OpenLevel(level, index));
                _spawned.Add(button);
            }
        }

        private static LevelState ResolveState(IProgressService progress, LevelCatalog catalog, string levelId)
        {
            if (progress.IsLevelCompleted(levelId)) return LevelState.Completed;
            if (progress.IsLevelUnlocked(catalog, levelId)) return LevelState.Unlocked;
            return LevelState.Locked;
        }

        private static void OpenLevel(LevelDefinition level, int index)
        {
            LevelSelection.Current = level;
            LevelSelection.CurrentIndex = index;
            SceneManager.LoadScene(SceneNames.Gameplay);
        }
    }
}
