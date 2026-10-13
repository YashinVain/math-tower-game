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
        // Размер кнопок главного меню и кнопки "Назад" на выборе уровней.
        private const float MenuButtonWidth = 400f;
        private const float MenuButtonHeight = 100f;
        private const int MenuButtonFont = 40;

        private const string ScenesFolder = "Assets/_Project/Scenes";
        private const string CatalogPath = "Assets/_Project/ScriptableObjects/LevelCatalog.asset";

        [MenuItem("MathGame/3. Build Scenes (Boot, Menu, Gameplay)")]
        public static void BuildAllScenes()
        {
            if (!ConfirmDiscardUnsavedChanges()) return;

            BuildBootScene();
            BuildMenuScene();
            BuildGameplayScene();

            // Важно: сохранить и обновить AssetDatabase ДО того, как
            // добавлять сцены в Build Settings — иначе LoadAssetAtPath
            // может ещё не видеть только что созданный .unity-файл, и
            // сцена молча не попадёт в список (при этом лог всё равно
            // покажет "успех", потому что сама сборка сцены не упала).
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            AddScenesToBuildSettings();

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

            var cameraGo = new GameObject("Main Camera");
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.12f, 0.16f);
            cameraGo.tag = "MainCamera";
            cameraGo.AddComponent<AudioListener>();

            CreateEventSystem();
            var canvasGo = CreateCanvas(null, "Canvas");

            // Нарисованный фон меню — первым ребёнком холста, чтобы все
            // панели рисовались поверх него.
            AddMenuBackground(canvasGo.transform);

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
            // Начальные значения на момент открытия сцены — как только
            // запустится первая мини-игра, её контроллер (TowerMinigame-
            // Controller / DoorMinigameController) сам подгонит
            // orthographicSize и Y под свой тип раскладки (башня растёт
            // вверх, двери — в стороны, общих значений на оба не хватает).
            camera.orthographicSize = 5.7f;
            camera.backgroundColor = new Color(0.55f, 0.75f, 0.9f);
            cameraGo.transform.position = new Vector3(-6f, 4.3f, -10f);
            cameraGo.tag = "MainCamera";
            cameraGo.AddComponent<AudioListener>();
            var cameraFollow = cameraGo.AddComponent<CameraFollowX>();
            // Герой стоит левее центра кадра, не прямо перед башней/дверями
            // — камера "смотрит" немного правее героя.
            SetFloatField(cameraFollow, "offsetX", 3f);

            var background = new GameObject("Background");
            // Раньше фон стоял на фиксированном месте в мире (-6,-1) — это
            // работало, пока камера была маленькой и почти не двигалась.
            // Теперь камера по X следует за героем на всю длину уровня, а
            // по Y её высота (cameraOrthographicSize) разная у башен и
            // дверей — при более широком обзора неподвижный фон переставал
            // доставать до краёв экрана, и по бокам была видна "родная"
            // синяя заливка камеры вместо картинки фона (ровно то, что
            // увидел игрок). Сделал фон ребёнком камеры с большим запасом
            // по размеру — теперь он всегда едет вместе с камерой и всегда
            // перекрывает весь её обзор, при любом окне и любом типе
            // мини-игры.
            background.transform.SetParent(cameraGo.transform, false);
            background.transform.localPosition = new Vector3(0f, 0f, 20f);
            // Раньше тут был фиксированный масштаб 80×40 для однотонного
            // квадрата. Теперь у фона нарисованная картинка, и её размер и
            // положение каждый кадр подгоняет BackgroundFitter под реальный
            // обзор камеры (без искажений, низ картинки — на нижнем краю
            // экрана). Какая именно картинка — выбирает LevelThemeApplier
            // по теме уровня; луг стоит здесь по умолчанию, чтобы сцена в
            // редакторе не была пустой.
            var bgRenderer = background.AddComponent<SpriteRenderer>();
            var defaultBackground = LoadArtSprite(ArtSpecs.Backgrounds[0]);
            bgRenderer.sprite = defaultBackground != null ? defaultBackground : GetPlaceholderSprite();
            bgRenderer.color = Color.white;
            bgRenderer.sortingOrder = -10;
            background.AddComponent<BackgroundFitter>();
            var themeApplier = background.AddComponent<LevelThemeApplier>();
            SetField(themeApplier, "backgroundRenderer", bgRenderer);

            var minigameHost = new GameObject("MinigameHost");
            // Раньше тут стояло (-4,-1,0) — ещё с тех пор, когда камеру
            // настраивали фиксированными числами. Теперь и Tower-, и
            // DoorMinigameController сами считают, куда встать камере,
            // ОТНОСИТЕЛЬНО реальной позиции героя (см. ScreenLayout и
            // cameraY в обоих контроллерах) — а этот сдвиг молча прибавлялся
            // ко всем позициям внутри (включая героя) и не был учтён в
            // расчётах камеры по Y, из-за чего герой в игре стоял на 1
            // юнит ниже, чем камера считала, и вылезал за нижний край
            // экрана. Ноль — нейтральная точка, ничего не сдвигает.
            minigameHost.transform.position = Vector3.zero;

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

        // Фон меню: картинка 16:9 на весь экран. AspectRatioFitter в режиме
        // EnvelopeParent увеличивает её ровно настолько, чтобы закрыть весь
        // экран при любой форме окна, не искажая (лишнее обрезается). Сверху
        // совсем лёгкое затемнение (6%): фон теперь светлый и яркий, а кнопки
        // и подписи и так тёмные и хорошо читаются.
        private static void AddMenuBackground(Transform canvasTransform)
        {
            var sprite = LoadArtSprite(ArtSpecs.MenuBackground);
            if (sprite == null) return;

            var bgRect = CreateUIObject("MenuBackground", canvasTransform);
            var bgImage = bgRect.gameObject.AddComponent<Image>();
            bgImage.sprite = sprite;
            bgImage.raycastTarget = false;
            var fitter = bgRect.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = sprite.rect.width / sprite.rect.height;

            var dimRect = CreateFullScreenPanel(canvasTransform, "MenuDimmer");
            var dimImage = dimRect.gameObject.AddComponent<Image>();
            dimImage.color = new Color(0f, 0f, 0f, 0.06f);
            dimImage.raycastTarget = false;
        }

        private static (GameObject panelGo, Button play, Button settings, Button quit) BuildMainPanel(Transform canvasTransform)
        {
            var panel = CreateFullScreenPanel(canvasTransform, "MainPanel");

            // Кнопки главного меню (400×100). Такого же размера кнопка
            // "Назад" на экране выбора уровней — все кнопки меню одинаковые.
            var play = CreateButton(panel, "PlayButton", "Играть", MenuButtonWidth, MenuButtonHeight, MenuButtonFont);
            AnchorAt(play.GetComponent<RectTransform>(), 0.5f, 0.58f, MenuButtonWidth, MenuButtonHeight);

            var settings = CreateButton(panel, "SettingsButton", "Настройки", MenuButtonWidth, MenuButtonHeight, MenuButtonFont);
            AnchorAt(settings.GetComponent<RectTransform>(), 0.5f, 0.42f, MenuButtonWidth, MenuButtonHeight);

            var quit = CreateButton(panel, "QuitButton", "Выход", MenuButtonWidth, MenuButtonHeight, MenuButtonFont);
            AnchorAt(quit.GetComponent<RectTransform>(), 0.5f, 0.26f, MenuButtonWidth, MenuButtonHeight);

            return (panel.gameObject, play, settings, quit);
        }

        private static (GameObject panelGo, Button back) BuildLevelSelectPanel(Transform canvasTransform)
        {
            var panel = CreateFullScreenPanel(canvasTransform, "LevelSelectPanel");

            // Кнопка "Назад" — того же размера, что кнопки главного меню, по
            // центру под сеткой уровней.
            var back = CreateButton(panel, "BackButton", "Назад", MenuButtonWidth, MenuButtonHeight, MenuButtonFont);
            AnchorAt(back.GetComponent<RectTransform>(), 0.5f, 0.17f, MenuButtonWidth, MenuButtonHeight);

            var container = CreateUIObject("ButtonsContainer", panel);
            AnchorAt(container, 0.5f, 0.57f, 900, 160);
            var grid = container.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(160, 160);
            grid.spacing = new Vector2(20, 20);
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;

            // Автоподгонка высоты под реальное число уровней (растёт вниз
            // и вверх от центра, т.к. у контейнера pivot по центру) —
            // иначе с запасом "на будущее" при всего 1-2 уровнях остаётся
            // пустой хвост.
            var fitter = container.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

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

            // Затемнение на весь экран: слайдеры и белые подписи настроек
            // плохо читались бы прямо на нарисованном фоне меню. Поверх него —
            // нарисованная деревянная доска с железной окантовкой (собрана из
            // рамки уровня, 9-slice), на которой стоят все настройки.
            var panelBackground = panel.gameObject.AddComponent<Image>();
            panelBackground.color = new Color(0f, 0f, 0f, 0.6f);

            var boardSprite = LoadArtSprite(ArtSpecs.PanelBoard);
            if (boardSprite != null)
            {
                var boardRect = CreateUIObject("Board", panel);
                boardRect.anchorMin = new Vector2(0.03f, 0.025f);
                boardRect.anchorMax = new Vector2(0.97f, 0.995f);
                boardRect.offsetMin = Vector2.zero;
                boardRect.offsetMax = Vector2.zero;
                var boardImage = boardRect.gameObject.AddComponent<Image>();
                boardImage.sprite = boardSprite;
                boardImage.type = Image.Type.Sliced;
                boardImage.raycastTarget = false;
                // Уголки доски 76 px — для окна во весь экран это много, берём
                // вдвое меньше, чтобы рамка не съедала место под настройки.
                boardImage.pixelsPerUnitMultiplier = 2f;
                boardRect.SetAsFirstSibling(); // позади всех настроек
            }

            // Заголовок.
            var title = CreateLabel(panel, "TitleLabel", "Настройки", 60, SettingsValueColor, 700, 80, TextAlignmentOptions.Center);
            AnchorAt(title.rectTransform, 0.5f, 0.5f, 700, 80, 0f, 392f);

            // Быстрая автонастройка под возраст (4 кнопки, см. AgePresets).
            var agePresetsLabel = CreateLabel(panel, "AgePresetsLabel", "Возраст игрока (быстрая настройка)", 30, SettingsTextColor, 900, 44, TextAlignmentOptions.Center);
            AnchorAt(agePresetsLabel.rectTransform, 0.5f, 0.5f, 900, 44, 0f, 328f);

            const float ageW = 260f, ageH = 80f, agePitch = 290f;
            Button MakeAge(string name, string text, float x)
            {
                var button = CreateButton(panel, name, text, ageW, ageH, 32);
                AnchorAt(button.GetComponent<RectTransform>(), 0.5f, 0.5f, ageW, ageH, x, 258f);
                return button;
            }
            var age6to8Button = MakeAge("Age6to8Button", "6–8 лет", -1.5f * agePitch);
            var age9to11Button = MakeAge("Age9to11Button", "9–11 лет", -0.5f * agePitch);
            var age12to14Button = MakeAge("Age12to14Button", "12–14 лет", 0.5f * agePitch);
            var age15to16Button = MakeAge("Age15to16Button", "15–16 лет", 1.5f * agePitch);

            // Верхняя граница слайдера "Мин. число" — ровно
            // GameSettingsData.MaxNumberRangeMin (2): выше поднять нельзя,
            // иначе сумму двух чисел из диапазона нельзя сделать маленькой
            // (см. подробности в GameSettingsData). Слайдер физически не
            // даёт задрать значение выше — не приходится "откатывать" его
            // обратно после того, как игрок уже подвинул ползунок.
            var (numberMinSlider, numberMinLabel) = CreateSliderRow(panel, "NumberMinRow", "Мин. число", 0, GameSettingsData.MaxNumberRangeMin, 1, 162f);
            var (numberMaxSlider, numberMaxLabel) = CreateSliderRow(panel, "NumberMaxRow", "Макс. число", 1, 50, 16, 90f);

            // Действия — одним рядом, галочка вплотную к подписи.
            const float opX0 = SettingsLeftEdge + 30f, opPitch = 310f;
            var (additionToggle, _) = CreateToggleRow(panel, "AdditionRow", "Сложение", true, opX0, 12f);
            var (subtractionToggle, _) = CreateToggleRow(panel, "SubtractionRow", "Вычитание", false, opX0 + opPitch, 12f);
            var (multiplicationToggle, _) = CreateToggleRow(panel, "MultiplicationRow", "Умножение", false, opX0 + 2f * opPitch, 12f);
            var (divisionToggle, _) = CreateToggleRow(panel, "DivisionRow", "Деление", false, opX0 + 3f * opPitch, 12f);

            var (difficultyGrowthToggle, _) = CreateToggleRow(panel, "DifficultyGrowthRow", "Рост сложности", true, opX0, -60f, 330f);
            var (doorsEasyModeToggle, _) = CreateToggleRow(panel, "DoorsEasyModeRow", "Упрощённые двери", false, opX0 + 2f * opPitch - 30f, -60f, 340f);

            var (difficultyGrowthRateSlider, difficultyGrowthRateLabel) = CreateSliderRow(panel, "DifficultyGrowthRateRow", "Скорость роста", 0f, 1f, 0.15f, -140f);
            var (timeLimitSlider, timeLimitLabel) = CreateSliderRow(panel, "TimeLimitRow", "Лимит времени, сек", 15, 300, 90, -208f);

            var resetProgressButton = CreateButton(panel, "ResetProgressButton", "Сбросить прогресс", 440, 90, 32);
            AnchorAt(resetProgressButton.GetComponent<RectTransform>(), 0.5f, 0.5f, 440, 90, -260f, -335f);

            var back = CreateButton(panel, "BackButton", "Назад", 300, 90, 32);
            AnchorAt(back.GetComponent<RectTransform>(), 0.5f, 0.5f, 300, 90, 190f, -335f);

            var confirmRoot = CreateUIObject("ResetConfirmRoot", panel);
            StretchFull(confirmRoot);
            var confirmBg = confirmRoot.gameObject.AddComponent<Image>();
            confirmBg.color = new Color(0f, 0f, 0f, 0.85f);

            var warning = CreateLabel(confirmRoot, "WarningText", "Точно сбросить весь прогресс?", 44, SettingsTextColor, 900, 80, TextAlignmentOptions.Center);
            AnchorAt(warning.rectTransform, 0.5f, 0.5f, 900, 80, 0f, 70f);

            var yesButton = CreateButton(confirmRoot, "ResetConfirmYesButton", "Да, сбросить", 380, 90, 32);
            AnchorAt(yesButton.GetComponent<RectTransform>(), 0.5f, 0.5f, 380, 90, -210f, -60f);

            var noButton = CreateButton(confirmRoot, "ResetConfirmNoButton", "Отмена", 380, 90, 32);
            AnchorAt(noButton.GetComponent<RectTransform>(), 0.5f, 0.5f, 380, 90, 210f, -60f);

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
            SetField(settingsPanel, "age6to8Button", age6to8Button);
            SetField(settingsPanel, "age9to11Button", age9to11Button);
            SetField(settingsPanel, "age12to14Button", age12to14Button);
            SetField(settingsPanel, "age15to16Button", age15to16Button);
            SetField(settingsPanel, "resetProgressButton", resetProgressButton);
            SetField(settingsPanel, "resetConfirmRoot", confirmRoot.gameObject);
            SetField(settingsPanel, "resetConfirmYesButton", yesButton);
            SetField(settingsPanel, "resetConfirmNoButton", noButton);

            return (panel.gameObject, back);
        }

        // Таймер: деревянная табличка (та же, что у кнопок) в ВЕРХНЕМ ЛЕВОМ углу
        // — справа в эти моменты башня с примерами, и таймер по центру на неё
        // наезжал. Внутри время крупным шрифтом и тонкая золотая полоска
        // оставшегося времени (раньше была прозрачная серо-синяя плашка и
        // плоская зелёная полоса, которые выбивались из стиля игры).
        private static GameObject BuildHud(Transform canvasTransform)
        {
            var hud = CreateUIObject("HUD", canvasTransform);
            AnchorAt(hud, 0.1f, 1f, 300, 100, 0f, -62f);

            var plaqueSprite = LoadArtSprite(ArtSpecs.ButtonWide);
            var plaque = hud.gameObject.AddComponent<Image>();
            plaque.raycastTarget = false;
            if (plaqueSprite != null)
            {
                plaque.sprite = plaqueSprite;
                plaque.type = Image.Type.Sliced;
                plaque.pixelsPerUnitMultiplier = Mathf.Max(1f, ArtSpecs.SlicedCornerPx / (0.3f * 100f));
                plaque.color = Color.white;
            }
            else
            {
                plaque.color = new Color(0.25f, 0.15f, 0.08f, 0.9f);
            }

            var timerLabel = CreateLabel(hud, "TimerLabel", "01:30", 50, new Color(1f, 0.93f, 0.75f), 260, 60, TextAlignmentOptions.Center);
            timerLabel.raycastTarget = false;
            timerLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            timerLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            timerLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            timerLabel.rectTransform.anchoredPosition = new Vector2(0f, -12f);

            // Полоска: тёмная дорожка и золотая заливка; GameplayHud меняет
            // ширину заливки (а не fillAmount: он работает только со спрайтом).
            var track = CreateUIObject("TimerTrack", hud);
            track.anchorMin = new Vector2(0.5f, 0f);
            track.anchorMax = new Vector2(0.5f, 0f);
            track.pivot = new Vector2(0.5f, 0f);
            track.anchoredPosition = new Vector2(0f, 16f);
            track.sizeDelta = new Vector2(220, 12);
            var trackImage = track.gameObject.AddComponent<Image>();
            trackImage.color = new Color(0.1f, 0.06f, 0.03f);
            trackImage.raycastTarget = false;

            var fillBarRect = CreateUIObject("TimerFillBar", track);
            fillBarRect.anchorMin = Vector2.zero;
            fillBarRect.anchorMax = Vector2.one;
            fillBarRect.offsetMin = new Vector2(2, 2);
            fillBarRect.offsetMax = new Vector2(-2, -2);
            var fillImage = fillBarRect.gameObject.AddComponent<Image>();
            fillImage.color = new Color(0.96f, 0.76f, 0.24f);
            fillImage.raycastTarget = false;

            var hudComp = hud.gameObject.AddComponent<GameplayHud>();
            SetField(hudComp, "timerLabel", timerLabel);
            SetField(hudComp, "timerFillBar", fillImage);

            return hud.gameObject;
        }

        // Деревянная доска по центру панели (окна паузы и результата):
        // та же рамка, что у экрана настроек, — окна в одном стиле с меню.
        private static void AddCenteredBoard(Transform panel, float width, float height)
        {
            var boardSprite = LoadArtSprite(ArtSpecs.PanelBoard);
            if (boardSprite == null) return;

            var boardRect = CreateUIObject("Board", panel);
            AnchorAt(boardRect, 0.5f, 0.5f, width, height);
            var boardImage = boardRect.gameObject.AddComponent<Image>();
            boardImage.sprite = boardSprite;
            boardImage.type = Image.Type.Sliced;
            boardImage.raycastTarget = false;
            boardImage.pixelsPerUnitMultiplier = 2f;
            boardRect.SetAsFirstSibling(); // позади надписей и кнопок
        }

        private static GameObject BuildResultPanel(Transform canvasTransform)
        {
            var panel = CreateFullScreenPanel(canvasTransform, "ResultPanel");
            var bg = panel.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.75f);

            AddCenteredBoard(panel, 960, 620);

            var title = CreateLabel(panel, "TitleLabel", "Уровень пройден!", 60, SettingsValueColor, 860, 90, TextAlignmentOptions.Center);
            AnchorAt(title.rectTransform, 0.5f, 0.5f, 860, 90, 0f, 170f);

            var primary = CreateButton(panel, "PrimaryButton", "Следующий уровень", 560, 100, 38);
            AnchorAt(primary.GetComponent<RectTransform>(), 0.5f, 0.5f, 560, 100, 0f, 10f);
            var primaryLabel = primary.GetComponentInChildren<TextMeshProUGUI>();

            var secondary = CreateButton(panel, "SecondaryButton", "В меню", 560, 100, 38);
            AnchorAt(secondary.GetComponent<RectTransform>(), 0.5f, 0.5f, 560, 100, 0f, -135f);

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

            AddCenteredBoard(panel, 760, 560);

            var title = CreateLabel(panel, "TitleLabel", "Пауза", 64, SettingsValueColor, 600, 90, TextAlignmentOptions.Center);
            AnchorAt(title.rectTransform, 0.5f, 0.5f, 600, 90, 0f, 150f);

            var resume = CreateButton(panel, "ResumeButton", "Продолжить", 480, 100, 38);
            AnchorAt(resume.GetComponent<RectTransform>(), 0.5f, 0.5f, 480, 100, 0f, 0f);

            var exit = CreateButton(panel, "ExitButton", "Выйти", 480, 100, 38);
            AnchorAt(exit.GetComponent<RectTransform>(), 0.5f, 0.5f, 480, 100, 0f, -135f);

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
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                {
                    Debug.LogWarning($"{path} не найден через AssetDatabase — не добавлен в Build Settings. Попробуйте запустить 'MathGame/3. Build Scenes' ещё раз.");
                    continue;
                }
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
