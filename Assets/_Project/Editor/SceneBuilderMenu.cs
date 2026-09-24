using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using MathGame.Core;
using MathGame.Data;
using MathGame.UI.Menu;
using MathGame.UI.Gameplay;
using MathGame.Utils;
using static MathGame.EditorTools.EditorBuildUtils;

namespace MathGame.EditorTools
{
    // Собирает сцены Boot / Menu / Gameplay кодом — то же самое, что
    // описано в docs/EDITOR_SETUP.md (разделы 4-6), но одной кнопкой.
    // Как и PrefabBuilderMenu, каждая сцена строится, только если файла с
    // таким именем ещё нет — если вы уже что-то собрали и сохранили
    // руками, повторный запуск это не тронет.
    public static class SceneBuilderMenu
    {
        private const string ScenesFolder = "Assets/_Project/Scenes";
        private const string CatalogPath = "Assets/_Project/ScriptableObjects/LevelCatalog.asset";

        [MenuItem("MathGame/3. Build Scenes (Boot, Menu, Gameplay)")]
        public static void BuildAllScenes()
        {
            if (!ConfirmDiscardUnsavedChanges()) return;

            BuildBootScene();
            BuildMenuScene();
            BuildGameplayScene();

            AddScenesToBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var bootPath = $"{ScenesFolder}/Boot.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(bootPath) != null)
                EditorSceneManager.OpenScene(bootPath);

            Debug.Log("Готово: Boot.unity, Menu.unity, Gameplay.unity собраны и добавлены в Build Settings.");
        }

        // Сборка сцены полностью заменяет то, что сейчас открыто в редакторе
        // (EditorSceneManager.NewScene). Если там есть несохранённые правки —
        // предлагаем сохранить, а не тихо их терять.
        private static bool ConfirmDiscardUnsavedChanges()
        {
            if (!SceneManager.GetActiveScene().isDirty) return true;

            bool save = EditorUtility.DisplayDialog(
                "Несохранённые изменения",
                "В открытой сейчас сцене есть несохранённые изменения. Продолжать сборку сцен без сохранения нельзя — они потеряются.",
                "Сохранить и продолжить",
                "Отмена");

            if (!save)
            {
                Debug.LogWarning("Сборка сцен отменена — сначала сохраните открытую сцену (Ctrl+S) и запустите снова.");
                return false;
            }

            return EditorSceneManager.SaveOpenScenes();
        }

        private static bool SceneExists(string path) => AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null;

        private static void BuildBootScene()
        {
            var path = $"{ScenesFolder}/Boot.unity";
            if (SceneExists(path))
            {
                Debug.Log($"{path} уже существует — пропускаю (если в нём ещё нет GameServices/Boot Loader, доделайте это вручную).");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var go = new GameObject("GameServices");
            var services = go.AddComponent<GameServices>();
            go.AddComponent<BootLoader>();

            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath);
            if (catalog != null) SetField(services, "levelCatalog", catalog);

            EditorSceneManager.SaveScene(scene, path);
        }

        private static void BuildMenuScene()
        {
            var path = $"{ScenesFolder}/Menu.unity";
            if (SceneExists(path))
            {
                Debug.Log($"{path} уже существует — пропускаю.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateEventSystem();
            var canvasGo = CreateCanvas(null, "Canvas");

            var (mainPanelGo, playButton, settingsButton, quitButton) = BuildMainPanel(canvasGo.transform);
            var (levelSelectGo, levelSelectBack) = BuildLevelSelectPanel(canvasGo.transform);
            var (settingsGo, settingsBack) = BuildSettingsPanel(canvasGo.transform);

            levelSelectGo.SetActive(false);
            settingsGo.SetActive(false);

            var menuController = canvasGo.AddComponent<MenuUIController>();
            SetField(menuController, "mainPanel", mainPanelGo);
            SetField(menuController, "levelSelectPanel", levelSelectGo.GetComponent<LevelSelectPanel>());
            SetField(menuController, "settingsPanel", settingsGo.GetComponent<SettingsPanel>());
            SetField(menuController, "playButton", playButton);
            SetField(menuController, "settingsButton", settingsButton);
            SetField(menuController, "quitButton", quitButton);
            SetField(menuController, "levelSelectBackButton", levelSelectBack);
            SetField(menuController, "settingsBackButton", settingsBack);

            EditorSceneManager.SaveScene(scene, path);
        }

        private static void BuildGameplayScene()
        {
            var path = $"{ScenesFolder}/Gameplay.unity";
            if (SceneExists(path))
            {
                Debug.Log($"{path} уже существует — пропускаю.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraGo = new GameObject("Main Camera");
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.backgroundColor = new Color(0.55f, 0.75f, 0.9f);
            cameraGo.transform.position = new Vector3(-6f, -1f, -10f);
            cameraGo.tag = "MainCamera";
            cameraGo.AddComponent<AudioListener>();
            cameraGo.AddComponent<CameraFollowX>();

            var background = new GameObject("Background");
            background.transform.position = new Vector3(-6f, -1f, 10f);
            background.transform.localScale = new Vector3(40f, 20f, 1f);
            var bgRenderer = background.AddComponent<SpriteRenderer>();
            bgRenderer.sprite = CreatePlaceholderSprite(new Color(0.7f, 0.85f, 0.95f));
            bgRenderer.sortingOrder = -10;
            var themeApplier = background.AddComponent<LevelThemeApplier>();
            SetField(themeApplier, "backgroundRenderer", bgRenderer);

            var minigameHost = new GameObject("MinigameHost");
            minigameHost.transform.position = new Vector3(-4f, -1f, 0f);

            var gameFlow = new GameObject("GameFlow");
            var gameplayController = gameFlow.AddComponent<GameplayController>();

            CreateEventSystem();
            var canvasGo = CreateCanvas(null, "Canvas");

            var hudGo = BuildHud(canvasGo.transform);
            var resultGo = BuildResultPanel(canvasGo.transform);
            var pauseGo = BuildPausePanel(canvasGo.transform);

            SetField(gameplayController, "minigameHost", minigameHost.transform);
            SetField(gameplayController, "hud", hudGo.GetComponent<GameplayHud>());
            SetField(gameplayController, "resultPanel", resultGo.GetComponent<ResultPanel>());
            SetField(gameplayController, "pausePanel", pauseGo.GetComponent<PausePanel>());

            EditorSceneManager.SaveScene(scene, path);
        }

        private static (GameObject panelGo, Button play, Button settings, Button quit) BuildMainPanel(Transform canvasTransform)
        {
            var panel = CreateFullScreenPanel(canvasTransform, "MainPanel");
            AddVerticalLayout(panel, 24, TextAnchor.MiddleCenter);

            var play = CreateButton(panel, "PlayButton", "Играть", 300, 70, 32);
            var settings = CreateButton(panel, "SettingsButton", "Настройки", 300, 70, 32);
            var quit = CreateButton(panel, "QuitButton", "Выход", 300, 70, 32);

            return (panel.gameObject, play, settings, quit);
        }

        private static (GameObject panelGo, Button back) BuildLevelSelectPanel(Transform canvasTransform)
        {
            var panel = CreateFullScreenPanel(canvasTransform, "LevelSelectPanel");
            AddVerticalLayout(panel, 20, TextAnchor.UpperCenter);

            var back = CreateButton(panel, "BackButton", "Назад", 200, 60, 24);

            var container = CreateUIObject("ButtonsContainer", panel);
            SetPreferredSize(container.gameObject, 1600, 800);
            var grid = container.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(160, 160);
            grid.spacing = new Vector2(20, 20);
            grid.childAlignment = TextAnchor.UpperCenter;

            var levelSelectPanel = panel.gameObject.AddComponent<LevelSelectPanel>();
            SetField(levelSelectPanel, "buttonsContainer", container);

            var levelButtonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/LevelButton.prefab");
            if (levelButtonPrefab != null)
                SetField(levelSelectPanel, "levelButtonPrefab", levelButtonPrefab.GetComponent<LevelButton>());
            else
                Debug.LogWarning("LevelButton.prefab не найден — сначала запустите 'MathGame/1. Build Prefabs'.");

            return (panel.gameObject, back);
        }

        private static (GameObject panelGo, Button back) BuildSettingsPanel(Transform canvasTransform)
        {
            var panel = CreateFullScreenPanel(canvasTransform, "SettingsPanel");
            AddVerticalLayout(panel, 10, TextAnchor.UpperCenter);

            var (numberMinSlider, numberMinLabel) = CreateSliderRow(panel, "NumberMinRow", "Мин. число", 0, 20, 1);
            var (numberMaxSlider, numberMaxLabel) = CreateSliderRow(panel, "NumberMaxRow", "Макс. число", 1, 50, 10);

            var (additionToggle, _) = CreateToggleRow(panel, "AdditionRow", "Сложение", true);
            var (subtractionToggle, _) = CreateToggleRow(panel, "SubtractionRow", "Вычитание", false);
            var (multiplicationToggle, _) = CreateToggleRow(panel, "MultiplicationRow", "Умножение", false);
            var (divisionToggle, _) = CreateToggleRow(panel, "DivisionRow", "Деление", false);

            var (difficultyGrowthToggle, _) = CreateToggleRow(panel, "DifficultyGrowthRow", "Рост сложности", true);
            var (difficultyGrowthRateSlider, difficultyGrowthRateLabel) = CreateSliderRow(panel, "DifficultyGrowthRateRow", "Скорость роста", 0f, 1f, 0.15f);

            var (timeLimitSlider, timeLimitLabel) = CreateSliderRow(panel, "TimeLimitRow", "Лимит времени, сек", 15, 300, 90);

            var (doorsEasyModeToggle, _) = CreateToggleRow(panel, "DoorsEasyModeRow", "Упрощённые двери", false);

            var resetProgressButton = CreateButton(panel, "ResetProgressButton", "Сбросить прогресс", 340, 56, 22);
            var back = CreateButton(panel, "BackButton", "Назад", 200, 56, 22);

            var confirmRoot = CreateUIObject("ResetConfirmRoot", panel);
            StretchFull(confirmRoot);
            var confirmBg = confirmRoot.gameObject.AddComponent<Image>();
            confirmBg.color = new Color(0f, 0f, 0f, 0.75f);
            AddVerticalLayout(confirmRoot, 16, TextAnchor.MiddleCenter);
            CreateLabel(confirmRoot, "WarningText", "Точно сбросить весь прогресс?", 28, Color.white, 560, 60, TextAlignmentOptions.Center);
            var yesButton = CreateButton(confirmRoot, "ResetConfirmYesButton", "Да, сбросить", 280, 60, 24);
            var noButton = CreateButton(confirmRoot, "ResetConfirmNoButton", "Отмена", 280, 60, 24);
            confirmRoot.gameObject.SetActive(false);

            var settingsPanel = panel.gameObject.AddComponent<SettingsPanel>();
            SetField(settingsPanel, "numberMinSlider", numberMinSlider);
            SetField(settingsPanel, "numberMinLabel", numberMinLabel);
            SetField(settingsPanel, "numberMaxSlider", numberMaxSlider);
            SetField(settingsPanel, "numberMaxLabel", numberMaxLabel);
            SetField(settingsPanel, "additionToggle", additionToggle);
            SetField(settingsPanel, "subtractionToggle", subtractionToggle);
            SetField(settingsPanel, "multiplicationToggle", multiplicationToggle);
            SetField(settingsPanel, "divisionToggle", divisionToggle);
            SetField(settingsPanel, "difficultyGrowthToggle", difficultyGrowthToggle);
            SetField(settingsPanel, "difficultyGrowthRateSlider", difficultyGrowthRateSlider);
            SetField(settingsPanel, "difficultyGrowthRateLabel", difficultyGrowthRateLabel);
            SetField(settingsPanel, "timeLimitSlider", timeLimitSlider);
            SetField(settingsPanel, "timeLimitLabel", timeLimitLabel);
            SetField(settingsPanel, "doorsEasyModeToggle", doorsEasyModeToggle);
            SetField(settingsPanel, "resetProgressButton", resetProgressButton);
            SetField(settingsPanel, "resetConfirmRoot", confirmRoot.gameObject);
            SetField(settingsPanel, "resetConfirmYesButton", yesButton);
            SetField(settingsPanel, "resetConfirmNoButton", noButton);

            return (panel.gameObject, back);
        }

        private static GameObject BuildHud(Transform canvasTransform)
        {
            var hud = CreateUIObject("HUD", canvasTransform);
            hud.anchorMin = new Vector2(0.5f, 1f);
            hud.anchorMax = new Vector2(0.5f, 1f);
            hud.pivot = new Vector2(0.5f, 1f);
            hud.anchoredPosition = new Vector2(0, -30);
            hud.sizeDelta = new Vector2(320, 120);
            AddVerticalLayout(hud, 8, TextAnchor.UpperCenter);

            var timerLabel = CreateLabel(hud, "TimerLabel", "01:30", 40, Color.white, 260, 60, TextAlignmentOptions.Center);

            var fillBarRect = CreateUIObject("TimerFillBar", hud);
            SetPreferredSize(fillBarRect.gameObject, 280, 18);
            var fillImage = fillBarRect.gameObject.AddComponent<Image>();
            fillImage.color = new Color(0.3f, 0.8f, 0.4f);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillAmount = 1f;

            var hudComp = hud.gameObject.AddComponent<GameplayHud>();
            SetField(hudComp, "timerLabel", timerLabel);
            SetField(hudComp, "timerFillBar", fillImage);

            return hud.gameObject;
        }

        private static GameObject BuildResultPanel(Transform canvasTransform)
        {
            var panel = CreateFullScreenPanel(canvasTransform, "ResultPanel");
            var bg = panel.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.75f);
            AddVerticalLayout(panel, 20, TextAnchor.MiddleCenter);

            var title = CreateLabel(panel, "TitleLabel", "Уровень пройден!", 48, Color.white, 700, 80, TextAlignmentOptions.Center);
            var primary = CreateButton(panel, "PrimaryButton", "Следующий уровень", 380, 70, 26);
            var primaryLabel = primary.GetComponentInChildren<TextMeshProUGUI>();
            var secondary = CreateButton(panel, "SecondaryButton", "В меню", 380, 70, 26);

            var resultPanel = panel.gameObject.AddComponent<ResultPanel>();
            SetField(resultPanel, "root", panel.gameObject);
            SetField(resultPanel, "titleLabel", title);
            SetField(resultPanel, "primaryButton", primary);
            SetField(resultPanel, "primaryButtonLabel", primaryLabel);
            SetField(resultPanel, "secondaryButton", secondary);

            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }

        private static GameObject BuildPausePanel(Transform canvasTransform)
        {
            var panel = CreateFullScreenPanel(canvasTransform, "PausePanel");
            var bg = panel.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.75f);
            AddVerticalLayout(panel, 20, TextAnchor.MiddleCenter);

            CreateLabel(panel, "TitleLabel", "Пауза", 44, Color.white, 400, 70, TextAlignmentOptions.Center);
            var resume = CreateButton(panel, "ResumeButton", "Продолжить", 320, 70, 26);
            var exit = CreateButton(panel, "ExitButton", "Выйти", 320, 70, 26);

            var pausePanel = panel.gameObject.AddComponent<PausePanel>();
            SetField(pausePanel, "root", panel.gameObject);
            SetField(pausePanel, "resumeButton", resume);
            SetField(pausePanel, "exitButton", exit);

            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }

        private static void AddScenesToBuildSettings()
        {
            string[] orderedPaths =
            {
                $"{ScenesFolder}/Boot.unity",
                $"{ScenesFolder}/Menu.unity",
                $"{ScenesFolder}/Gameplay.unity"
            };

            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            foreach (var path in orderedPaths)
            {
                if (scenes.Exists(s => s.path == path)) continue;
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) continue;
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }

            scenes.Sort((a, b) =>
            {
                int ia = System.Array.IndexOf(orderedPaths, a.path);
                int ib = System.Array.IndexOf(orderedPaths, b.path);
                if (ia == -1) ia = int.MaxValue;
                if (ib == -1) ib = int.MaxValue;
                return ia.CompareTo(ib);
            });

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
