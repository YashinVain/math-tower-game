using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using MathGame.Data;

namespace MathGame.EditorTools
{
    // Инструмент только для редактора. Строит ВСЕ 10 уровней игры (раньше
    // здесь было только 2 — "пример", чтобы проверить архитектуру; теперь
    // это уже настоящий, полный список уровней). Уровни чередуются:
    // нечётные (1,3,5,7,9) — "Башни", чётные (2,4,6,8,10) — "Двери", как и
    // было задано в самом начале работы.
    //
    // Сложность растёт только у дверей от уровня к уровню (больше раундов
    // подряд нужно пройти правильно). У башен прогрессию специально убрали
    // по просьбе игрока — на всех 5 уровнях-башнях одна и та же
    // последовательность 2→3→4, без повторов размера внутри уровня.
    // Диапазон чисел у обоих типов растёт сам — за это отвечают настройки
    // игрока (difficultyGrowthRate) и DifficultyScaler, эта утилита их не
    // трогает.
    //
    // controllerPrefab у мини-игр всё равно нужно назначить вручную после
    // того, как собраны префабы TowerMinigame/DoorMinigame — этого код
    // сделать не может, только перетаскивание в инспекторе (хотя обычно за
    // вас это уже делает LinkControllerPrefab ниже, если префабы к этому
    // моменту уже собраны — см. "MathGame → 0. Build Absolutely Everything",
    // которая и вызывает всё по порядку).
    public static class LevelCatalogMenu
    {
        private const string LevelsFolder = "Assets/_Project/ScriptableObjects/Levels";
        private const string MinigamesFolder = "Assets/_Project/ScriptableObjects/Minigames";
        private const string ThemesFolder = "Assets/_Project/ScriptableObjects/Themes";
        private const string CatalogPath = "Assets/_Project/ScriptableObjects/LevelCatalog.asset";
        private const string TowerMinigamePrefabPath = "Assets/_Project/Prefabs/Minigames/Towers/TowerMinigame.prefab";
        private const string DoorMinigamePrefabPath = "Assets/_Project/Prefabs/Minigames/Doors/DoorMinigame.prefab";

        private const int LevelCount = 10;

        // Одна и та же последовательность башен на всех 5 уровнях-башнях
        // (1, 3, 5, 7, 9) — 2→3→4 этажа, без прогрессии и без повторов
        // внутри уровня (было {3,4,4,4} и похожее — игрок справедливо не
        // хочет видеть один и тот же размер башни дважды подряд на одном
        // уровне). heroStartingPower/maxBeatableAtOnce/powerPerWin тоже
        // одинаковы везде — это настройки "формата игры", а не
        // "сложности" (см. TowerMinigameDefinition, там же объяснено,
        // почему именно такие значения).
        private static readonly List<int> TowerSizes = new List<int> { 2, 3, 4 };

        // Сколько дверей показывать в раунде на КАЖДОМ уровне-двери — 4
        // (было 3). Сколько раундов подряд нужно пройти правильно — растёт
        // от уровня к уровню (уровни 2,4,6,8,10): это единственная
        // прогрессия сложности, которую оставили у дверей.
        private const int DoorsPerRound = 4;
        private static readonly int[] DoorRequiredProgression = { 3, 4, 5, 6, 8 };

        [MenuItem("MathGame/2. Build Level Catalog (10 Levels)")]
        public static void BuildLevelCatalog()
        {
            var catalog = LoadOrCreate<LevelCatalog>(CatalogPath);

            int towerLevelsSoFar = 0;
            int doorLevelsSoFar = 0;

            for (int levelIndex = 0; levelIndex < LevelCount; levelIndex++)
            {
                int levelNumber = levelIndex + 1; // 1..10, для человекочитаемых имён
                bool isTowerLevel = levelIndex % 2 == 0; // 0,2,4,6,8 → нечётные номера уровней (1,3,5,7,9)

                var theme = BuildTheme(levelIndex);

                MinigameDefinition minigame;
                string typeSuffix;
                if (isTowerLevel)
                {
                    minigame = BuildTowerDefinition(towerLevelsSoFar);
                    typeSuffix = "Towers";
                    towerLevelsSoFar++;
                }
                else
                {
                    minigame = BuildDoorDefinition(doorLevelsSoFar);
                    typeSuffix = "Doors";
                    doorLevelsSoFar++;
                }

                var level = LoadOrCreate<LevelDefinition>($"{LevelsFolder}/Level_{levelNumber:00}_{typeSuffix}.asset");
                level.levelId = $"level_{levelNumber:00}";
                level.displayName = $"Уровень {levelNumber}";
                level.visualTheme = theme;
                level.sequence = new List<MinigameDefinition> { minigame };
                EditorUtility.SetDirty(level);

                if (!catalog.levels.Contains(level)) catalog.levels.Add(level);
            }

            EditorUtility.SetDirty(catalog);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = catalog;
            EditorGUIUtility.PingObject(catalog);

            Debug.Log($"Готово: {LevelCount} уровней (Level_01..Level_{LevelCount:00}) и LevelCatalog собраны/обновлены.");
        }

        // По ТЗ нарастание сложности должно ощущаться и по картинке, не
        // только по цифрам. Раньше для этого фон просто темнел, но
        // персонажи нарисованы светлыми, и на тёмном фоне они выглядели бы
        // вырезанными. Теперь у игры четыре нарисованных фона с одним и тем
        // же дневным светом, а "страшнее" становится за счёт деталей: луг
        // (уровни 1–2) → засохшая пустошь с первыми флагами гоблинов
        // (3–5) → руины и лагерь гоблинов (6–8) → крепость гоблинов (9–10),
        // см. ArtSpecs.BackgroundForLevel. Тонировку и затемнение поэтому
        // отключаем (белый цвет, 0): картинка показывается как нарисована.
        // Если картинки нет, останется прежний запасной вариант —
        // затемнение цветом от светлого к тёмному.
        private static LevelVisualTheme BuildTheme(int levelIndex)
        {
            var theme = LoadOrCreate<LevelVisualTheme>($"{ThemesFolder}/Theme_Level{levelIndex + 1}.asset");

            var background = EditorBuildUtils.LoadArtSprite(ArtSpecs.BackgroundForLevel(levelIndex));
            theme.backgroundSprite = background;

            if (background != null)
            {
                theme.ambientTint = Color.white;
                theme.darknessAmount = 0f;
            }
            else
            {
                float t = (LevelCount > 1) ? levelIndex / (float)(LevelCount - 1) : 0f;
                var brightTint = new Color(0.75f, 0.85f, 0.95f);
                var darkTint = new Color(0.32f, 0.22f, 0.4f);
                theme.ambientTint = Color.Lerp(brightTint, darkTint, t);
                theme.darknessAmount = Mathf.Lerp(0f, 0.7f, t);
            }

            EditorUtility.SetDirty(theme);
            return theme;
        }

        private static TowerMinigameDefinition BuildTowerDefinition(int towerLevelIndex)
        {
            var path = $"{MinigamesFolder}/TowerMinigame_{towerLevelIndex + 1:00}.asset";
            var def = LoadOrCreate<TowerMinigameDefinition>(path);
            def.towerSizes = new List<int>(TowerSizes);
            def.heroStartingPower = 5;
            // maxBeatableAtOnce и powerPerWin оставляем на дефолтах класса
            // (1 и 1) — это тоже не "сложность уровня", а базовое правило
            // игры, одинаковое везде (см. комментарии в
            // TowerMinigameDefinition, почему именно такие значения).
            LinkControllerPrefab(def, TowerMinigamePrefabPath);
            EditorUtility.SetDirty(def);
            return def;
        }

        private static DoorMinigameDefinition BuildDoorDefinition(int doorLevelIndex)
        {
            var path = $"{MinigamesFolder}/DoorMinigame_{doorLevelIndex + 1:00}.asset";
            var def = LoadOrCreate<DoorMinigameDefinition>(path);
            def.doorsPerRound = DoorsPerRound;
            def.correctDoorsRequired = DoorRequiredProgression[doorLevelIndex];
            LinkControllerPrefab(def, DoorMinigamePrefabPath);
            EditorUtility.SetDirty(def);
            return def;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            var instance = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(instance, path);
            return instance;
        }

        // Если соответствующий префаб мини-игры уже собран (см.
        // PrefabBuilderMenu) — подключаем его сюда сами. Если ещё нет
        // (например, кто-то запустил только этот пункт меню, минуя
        // "1. Build Prefabs") — тихо оставляем поле пустым, его можно
        // будет перетащить в инспекторе вручную позже.
        private static void LinkControllerPrefab(MinigameDefinition definition, string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return;

            var so = new SerializedObject(definition);
            so.FindProperty("controllerPrefab").objectReferenceValue = prefab;
            so.ApplyModifiedProperties();
        }
    }
}
