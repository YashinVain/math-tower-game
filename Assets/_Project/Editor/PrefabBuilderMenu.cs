using UnityEditor;
using UnityEngine;
using TMPro;
using MathGame.Minigames.Towers;
using MathGame.Minigames.Doors;

namespace MathGame.EditorTools
{
    // Собирает префабы Golem / TowerMinigame / Door / DoorMinigame кодом,
    // а не руками через Hierarchy/Inspector — ровно то же самое, что
    // описано пошагово в docs/EDITOR_SETUP.md (разделы 3-4), но без риска
    // ошибиться на одном из шагов. Ручная сборка в документе остаётся как
    // способ понять, из чего вообще состоит каждый префаб, и как
    // отправная точка, если потом что-то в них нужно будет поменять руками.
    //
    // Каждый метод сначала проверяет, нет ли уже готового префаба по этому
    // пути — если есть, пропускает его и не перезаписывает (чтобы не
    // затереть ручные правки, если вы уже что-то донастроили после
    // предыдущего запуска).
    public static class PrefabBuilderMenu
    {
        private const string TowersFolder = "Assets/_Project/Prefabs/Minigames/Towers";
        private const string DoorsFolder = "Assets/_Project/Prefabs/Minigames/Doors";

        [MenuItem("MathGame/1. Build Tower And Door Prefabs")]
        public static void BuildPrefabs()
        {
            var golemPrefab = BuildGolemPrefab();
            BuildTowerMinigamePrefab(golemPrefab);

            var doorPrefab = BuildDoorPrefab();
            BuildDoorMinigamePrefab(doorPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Готово: Golem, TowerMinigame, Door, DoorMinigame — в " +
                      $"{TowersFolder} и {DoorsFolder}. Дальше: MathGame → 2. Bootstrap Sample Level Data.");
        }

        [MenuItem("MathGame/0. Build Everything (Prefabs + Sample Levels)")]
        public static void BuildEverything()
        {
            BuildPrefabs();
            SampleDataMenu.CreateSampleData();
        }

        private static Sprite CreatePlaceholderSprite(Color color)
        {
            var texture = new Texture2D(4, 4) { name = "PlaceholderSquare" };
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }

        private static TextMeshPro CreateWorldLabel(Transform parent, string name, float yOffset)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, yOffset, 0f);
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 4;
            tmp.text = "0";
            return tmp;
        }

        private static void SetField(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"Поле '{fieldName}' не найдено на {target.GetType().Name} — проверьте, не переименовалось ли оно в скрипте.");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }

        private static bool AlreadyExists(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;

        private static GameObject BuildGolemPrefab()
        {
            var path = $"{TowersFolder}/Golem.prefab";
            if (AlreadyExists(path))
            {
                Debug.Log($"{path} уже существует — пропускаю, чтобы не затереть ваши правки.");
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }

            var golem = new GameObject("Golem");
            golem.AddComponent<SpriteRenderer>().sprite = CreatePlaceholderSprite(new Color(0.45f, 0.2f, 0.55f));
            golem.AddComponent<BoxCollider2D>();
            var golemView = golem.AddComponent<GolemView>();

            var label = CreateWorldLabel(golem.transform, "ExpressionLabel", 1f);
            SetField(golemView, "expressionLabel", label);

            var prefab = PrefabUtility.SaveAsPrefabAsset(golem, path);
            Object.DestroyImmediate(golem);
            return prefab;
        }

        private static void BuildTowerMinigamePrefab(GameObject golemPrefab)
        {
            var path = $"{TowersFolder}/TowerMinigame.prefab";
            if (AlreadyExists(path))
            {
                Debug.Log($"{path} уже существует — пропускаю.");
                return;
            }

            var root = new GameObject("TowerMinigame");
            var controller = root.AddComponent<TowerMinigameController>();

            var hero = new GameObject("Hero");
            hero.transform.SetParent(root.transform, false);
            hero.transform.localPosition = new Vector3(-2f, 0f, 0f);
            hero.AddComponent<SpriteRenderer>().sprite = CreatePlaceholderSprite(new Color(0.3f, 0.6f, 1f));
            var heroView = hero.AddComponent<HeroView>();

            var powerLabel = CreateWorldLabel(hero.transform, "PowerLabel", 1f);

            var golemSlots = new GameObject("GolemSlots");
            golemSlots.transform.SetParent(root.transform, false);

            SetField(controller, "hero", heroView);
            SetField(controller, "golemPrefab", golemPrefab.GetComponent<GolemView>());
            SetField(controller, "golemSlotParent", golemSlots.transform);
            SetField(controller, "heroPowerLabel", powerLabel);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static GameObject BuildDoorPrefab()
        {
            var path = $"{DoorsFolder}/Door.prefab";
            if (AlreadyExists(path))
            {
                Debug.Log($"{path} уже существует — пропускаю.");
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }

            var door = new GameObject("Door");
            var renderer = door.AddComponent<SpriteRenderer>();
            renderer.sprite = CreatePlaceholderSprite(new Color(0.55f, 0.35f, 0.2f));
            door.AddComponent<BoxCollider2D>();
            var doorView = door.AddComponent<DoorView>();

            var label = CreateWorldLabel(door.transform, "ExpressionLabel", 1f);
            SetField(doorView, "expressionLabel", label);
            SetField(doorView, "doorRenderer", renderer);

            var prefab = PrefabUtility.SaveAsPrefabAsset(door, path);
            Object.DestroyImmediate(door);
            return prefab;
        }

        private static void BuildDoorMinigamePrefab(GameObject doorPrefab)
        {
            var path = $"{DoorsFolder}/DoorMinigame.prefab";
            if (AlreadyExists(path))
            {
                Debug.Log($"{path} уже существует — пропускаю.");
                return;
            }

            var root = new GameObject("DoorMinigame");
            var controller = root.AddComponent<DoorMinigameController>();

            var hero = new GameObject("Hero");
            hero.transform.SetParent(root.transform, false);
            hero.AddComponent<SpriteRenderer>().sprite = CreatePlaceholderSprite(new Color(0.3f, 0.6f, 1f));

            var targetLabel = CreateWorldLabel(hero.transform, "HeroTargetLabel", 1f);

            var doors = new GameObject("Doors");
            doors.transform.SetParent(root.transform, false);
            doors.transform.localPosition = new Vector3(2f, 0f, 0f);

            SetField(controller, "heroTargetLabel", targetLabel);
            SetField(controller, "doorPrefab", doorPrefab.GetComponent<DoorView>());
            SetField(controller, "doorsParent", doors.transform);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }
    }
}
