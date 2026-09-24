using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using MathGame.Data;

namespace MathGame.EditorTools
{
    // Инструмент только для редактора (Unity автоматически компилирует всё
    // в папке "Editor" отдельно от игры, в сборку это не попадёт). Создаёт
    // стартовый набор ассетов — 2 уровня (башни и двери) и каталог, — чтобы
    // не заводить всё руками с нуля. controllerPrefab у мини-игр всё равно
    // нужно назначить вручную после того, как собраны префабы
    // TowerMinigame/DoorMinigame — этого код сделать не может, только
    // перетаскивание в инспекторе.
    public static class SampleDataMenu
    {
        private const string LevelsFolder = "Assets/_Project/ScriptableObjects/Levels";
        private const string MinigamesFolder = "Assets/_Project/ScriptableObjects/Minigames";
        private const string ThemesFolder = "Assets/_Project/ScriptableObjects/Themes";
        private const string CatalogPath = "Assets/_Project/ScriptableObjects/LevelCatalog.asset";
        private const string TowerMinigamePrefabPath = "Assets/_Project/Prefabs/Minigames/Towers/TowerMinigame.prefab";
        private const string DoorMinigamePrefabPath = "Assets/_Project/Prefabs/Minigames/Doors/DoorMinigame.prefab";

        [MenuItem("MathGame/2. Bootstrap Sample Level Data")]
        public static void CreateSampleData()
        {
            var theme = LoadOrCreate<LevelVisualTheme>($"{ThemesFolder}/Theme_Default.asset");

            var towerDef = LoadOrCreate<TowerMinigameDefinition>($"{MinigamesFolder}/TowerMinigame_01.asset");
            towerDef.towerSizes = new List<int> { 2, 3, 4 };
            towerDef.heroStartingPower = 5;
            LinkControllerPrefab(towerDef, TowerMinigamePrefabPath);
            EditorUtility.SetDirty(towerDef);

            var doorDef = LoadOrCreate<DoorMinigameDefinition>($"{MinigamesFolder}/DoorMinigame_01.asset");
            doorDef.doorsPerRound = 4;
            doorDef.correctDoorsRequired = 3;
            LinkControllerPrefab(doorDef, DoorMinigamePrefabPath);
            EditorUtility.SetDirty(doorDef);

            var level1 = LoadOrCreate<LevelDefinition>($"{LevelsFolder}/Level_01_Towers.asset");
            level1.levelId = "level_01";
            level1.displayName = "Уровень 1";
            level1.visualTheme = theme;
            level1.sequence = new List<MinigameDefinition> { towerDef };
            EditorUtility.SetDirty(level1);

            var level2 = LoadOrCreate<LevelDefinition>($"{LevelsFolder}/Level_02_Doors.asset");
            level2.levelId = "level_02";
            level2.displayName = "Уровень 2";
            level2.visualTheme = theme;
            level2.sequence = new List<MinigameDefinition> { doorDef };
            EditorUtility.SetDirty(level2);

            var catalog = LoadOrCreate<LevelCatalog>(CatalogPath);
            if (!catalog.levels.Contains(level1)) catalog.levels.Add(level1);
            if (!catalog.levels.Contains(level2)) catalog.levels.Add(level2);
            EditorUtility.SetDirty(catalog);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = catalog;
            EditorGUIUtility.PingObject(catalog);

            Debug.Log("Готово: Level_01_Towers, Level_02_Doors и LevelCatalog созданы/обновлены.");
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
