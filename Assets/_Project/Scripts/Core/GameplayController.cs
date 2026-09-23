using UnityEngine;
using UnityEngine.SceneManagement;
using MathGame.Data;
using MathGame.Minigames.Framework;
using MathGame.UI.Gameplay;

namespace MathGame.Core
{
    // "Дирижёр" сцены Gameplay: по очереди запускает мини-игры из
    // sequence текущего уровня, следит за таймером и обрабатывает
    // победу/поражение/паузу. Сам не содержит логики ни одной конкретной
    // мини-игры — только общается с ними через контракт MinigameController
    // (Completed/Failed/Begin). Это единственное место в проекте, которое
    // должно знать про HUD/паузу/экран результата игровой сцены — в отличие
    // от экранов меню, они не самостоятельные "страницы", а часть одной
    // игровой сцены, которой управляет именно этот класс.
    [RequireComponent(typeof(CountdownTimer))]
    public class GameplayController : MonoBehaviour
    {
        [SerializeField] private Transform minigameHost;
        [SerializeField] private GameplayHud hud;
        [SerializeField] private ResultPanel resultPanel;
        [SerializeField] private PausePanel pausePanel;

        private CountdownTimer _timer;
        private LevelDefinition _level;
        private int _minigameIndex;
        private MinigameController _activeController;
        private bool _levelEnded;

        private void Awake()
        {
            _timer = GetComponent<CountdownTimer>();
        }

        private void Start()
        {
            _level = LevelSelection.Current;
            if (_level == null || _level.sequence.Count == 0)
            {
                Debug.LogError("Gameplay-сцена открыта без выбранного уровня — возвращаюсь в меню.");
                SceneManager.LoadScene(SceneNames.Menu);
                return;
            }

            resultPanel.Hide();
            pausePanel.Hide();
            pausePanel.ResumeRequested += OnResumeRequested;
            pausePanel.ExitRequested += OnExitRequested;
            _timer.Expired += OnTimeExpired;

            float timeLimit = _level.GetTimeLimitSeconds(GameServices.Instance.Settings.Current);
            _timer.StartTimer(timeLimit);

            _minigameIndex = 0;
            StartCurrentMinigame();
        }

        private void Update()
        {
            if (_levelEnded) return;

            hud.UpdateTimer(_timer.TimeRemaining, _timer.TotalTime);

            if (Input.GetKeyDown(KeyCode.Escape))
                TogglePause();
        }

        private void TogglePause()
        {
            if (pausePanel.IsVisible) OnResumeRequested();
            else
            {
                Time.timeScale = 0f;
                pausePanel.Show();
            }
        }

        private void OnResumeRequested()
        {
            Time.timeScale = 1f;
            pausePanel.Hide();
        }

        private void OnExitRequested()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneNames.Menu);
        }

        private void StartCurrentMinigame()
        {
            var definition = _level.sequence[_minigameIndex];
            var instance = Instantiate(definition.ControllerPrefab, minigameHost);
            _activeController = instance.GetComponent<MinigameController>();

            if (_activeController == null)
            {
                Debug.LogError($"Префаб {definition.ControllerPrefab.name} не содержит компонент-наследник MinigameController.");
                return;
            }

            _activeController.Completed += OnMinigameCompleted;
            _activeController.Failed += OnMinigameFailed;

            var context = new MinigameRuntimeContext(
                GameServices.Instance.Settings.Current,
                LevelSelection.CurrentIndex,
                GameServices.Instance.ProblemGenerator);

            _activeController.Begin(definition, context);
        }

        private void OnMinigameCompleted()
        {
            DestroyActiveController();
            _minigameIndex++;

            if (_minigameIndex >= _level.sequence.Count) LevelWon();
            else StartCurrentMinigame();
        }

        private void OnMinigameFailed()
        {
            DestroyActiveController();
            LevelLost();
        }

        private void OnTimeExpired()
        {
            if (_levelEnded) return;
            DestroyActiveController();
            LevelLost();
        }

        private void DestroyActiveController()
        {
            if (_activeController == null) return;

            _activeController.Completed -= OnMinigameCompleted;
            _activeController.Failed -= OnMinigameFailed;
            Destroy(_activeController.gameObject);
            _activeController = null;
        }

        private void LevelWon()
        {
            _levelEnded = true;
            _timer.Stop();
            GameServices.Instance.Progress.MarkLevelCompleted(_level.levelId);

            var catalog = GameServices.Instance.LevelCatalog;
            int nextIndex = LevelSelection.CurrentIndex + 1;
            bool hasNext = catalog != null && nextIndex < catalog.levels.Count;

            resultPanel.ShowWin(hasNext, () => LoadLevelByIndex(nextIndex), GoToMenu);
        }

        private void LevelLost()
        {
            _levelEnded = true;
            _timer.Stop();
            resultPanel.ShowLose(RetryLevel, GoToMenu);
        }

        private void RetryLevel() => SceneManager.LoadScene(SceneNames.Gameplay);

        private void GoToMenu() => SceneManager.LoadScene(SceneNames.Menu);

        private void LoadLevelByIndex(int index)
        {
            var catalog = GameServices.Instance.LevelCatalog;
            LevelSelection.Current = catalog.levels[index];
            LevelSelection.CurrentIndex = index;
            SceneManager.LoadScene(SceneNames.Gameplay);
        }
    }
}
