using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MathGame.Minigames.Towers;
using MathGame.Minigames.Doors;
using MathGame.UI.Menu;
using static MathGame.EditorTools.EditorBuildUtils;

namespace MathGame.EditorTools
{
    // Собирает префабы Golem / TowerMinigame / Door / DoorMinigame / LevelButton
    // кодом, а не руками через Hierarchy/Inspector — ровно то же самое, что
    // описано пошагово в docs/EDITOR_SETUP.md (Приложение A), но без риска
    // ошибиться на одном из шагов. Ручная сборка в документе остаётся как
    // способ понять, из чего вообще состоит каждый префаб, и как
    // отправная точка, если потом что-то в них нужно будет поменять руками.
    //
    // Каждый метод сначала проверяет, нет ли уже готового префаба по этому
    // пути — если есть, пропускает его и не перезаписывает (чтобы не
    // затереть ручные правки, если вы уже что-то донастроили после
    // предыдущего запуска). Поэтому, чтобы пересобрать префаб с нуля (как
    // после подключения графики), его файл сначала нужно удалить.
    //
    // Картинки берутся из Assets/_Project/Art (пути и размеры в игре — в
    // ArtSpecs). Размер картинки в мире задаётся при импорте (Pixels Per
    // Unit), поэтому у всех объектов масштаб 1, а позиция объекта — это
    // "ноги" персонажа/низ двери (pivot внизу картинки).
    public static class PrefabBuilderMenu
    {
        private const string TowersFolder = "Assets/_Project/Prefabs/Minigames/Towers";
        private const string DoorsFolder = "Assets/_Project/Prefabs/Minigames/Doors";
        private const string UIFolder = "Assets/_Project/Prefabs/UI";

        [MenuItem("MathGame/1. Build Prefabs")]
        public static void BuildPrefabs()
        {
            // Размер картинок в мире считается при импорте — убедимся, что
            // все картинки импортированы по актуальным правилам до сборки.
            ArtImportSettings.ReimportAll();

            var golemPrefab = BuildGolemPrefab();
            BuildTowerMinigamePrefab(golemPrefab);

            var doorPrefab = BuildDoorPrefab();
            BuildDoorMinigamePrefab(doorPrefab);

            BuildLevelButtonPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Готово: Golem, TowerMinigame, Door, DoorMinigame, LevelButton собраны. Дальше: MathGame → 2. Build Level Catalog (10 Levels).");
        }

        [MenuItem("MathGame/0. Build Absolutely Everything")]
        public static void BuildEverything()
        {
            BuildPrefabs();
            LevelCatalogMenu.BuildLevelCatalog();
            SceneBuilderMenu.BuildAllScenes();
            Debug.Log("Готово: префабы, все 10 уровней и все 3 сцены собраны и добавлены в Build Settings. Можно открывать Boot.unity и жать Play.");
        }

        private static bool AlreadyExists(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;

        // Спрайт-рендерер с картинкой (или квадратом-заглушкой, если картинки
        // нет). Цвет белый — то есть без тонировки: цвет рисунка не
        // искажается (раньше квадраты красились в цвет через color).
        private static SpriteRenderer AddArtRenderer(GameObject go, Sprite sprite, int sortingOrder)
        {
            var spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite != null ? sprite : GetPlaceholderSprite();
            spriteRenderer.color = Color.white;
            spriteRenderer.sortingOrder = sortingOrder;
            return spriteRenderer;
        }

        private static GameObject BuildGolemPrefab()
        {
            var path = $"{TowersFolder}/Golem.prefab";
            if (AlreadyExists(path))
            {
                Debug.Log($"{path} уже существует — пропускаю, чтобы не затереть ваши правки.");
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }

            // Файл и класс по-прежнему называются Golem (так сущность
            // называлась в самом начале), но на экране это гоблин, у
            // которого три внешних вида — выбирается случайный.
            var variants = new List<Sprite>();
            foreach (var spritePath in ArtSpecs.Goblins)
            {
                var sprite = LoadArtSprite(spritePath);
                if (sprite != null) variants.Add(sprite);
            }

            var golem = new GameObject("Golem");
            var golemRenderer = AddArtRenderer(golem, variants.Count > 0 ? variants[0] : null, 1);
            // Коллайдер добавляется ПОСЛЕ назначения спрайта — Unity сразу
            // подгоняет его под размер картинки (а GolemView потом
            // подгоняет под выбранный вид).
            golem.AddComponent<BoxCollider2D>();
            var golemView = golem.AddComponent<GolemView>();

            var label = CreateWorldLabel(golem.transform, "ExpressionLabel", ArtSpecs.GoblinHeight + ArtSpecs.LabelGap);
            SetField(golemView, "expressionLabel", label);
            SetObjectArrayField(golemView, "variants", variants.ToArray());

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
            AddArtRenderer(hero, LoadArtSprite(ArtSpecs.Hero), 1);
            var heroView = hero.AddComponent<HeroView>();

            var powerLabel = CreateWorldLabel(hero.transform, "PowerLabel", ArtSpecs.HeroHeight + ArtSpecs.LabelGap);

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
            var renderer = AddArtRenderer(door, LoadArtSprite(ArtSpecs.DoorClosed), 0);
            door.AddComponent<BoxCollider2D>();
            var doorView = door.AddComponent<DoorView>();

            var label = CreateWorldLabel(door.transform, "ExpressionLabel", ArtSpecs.DoorHeight + ArtSpecs.LabelGap);
            SetField(doorView, "expressionLabel", label);
            SetField(doorView, "doorRenderer", renderer);
            SetField(doorView, "openSprite", LoadArtSprite(ArtSpecs.DoorOpen));
            SetField(doorView, "lockedSprite", LoadArtSprite(ArtSpecs.DoorLocked));

            // Гоблин-засада: выскакивает из двери, когда в обычном режиме
            // выбрана неверная дверь. Пока дверь не открыта — выключен.
            var ambusher = new GameObject("Ambusher");
            ambusher.transform.SetParent(door.transform, false);
            var ambusherRenderer = AddArtRenderer(ambusher, LoadArtSprite(ArtSpecs.Goblins[0]), 2);
            ambusher.SetActive(false);
            SetField(doorView, "ambusherRenderer", ambusherRenderer);

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
            AddArtRenderer(hero, LoadArtSprite(ArtSpecs.Hero), 1);
            // HeroView здесь не для ходьбы (герой в "Дверях" не двигается) —
            // он нужен только за тем, что делает в Awake(): сообщает
            // CameraFollowX, за кем следить. Без этого камера в сценах
            // "Дверей" вообще не знала, куда смотреть, и застревала на
            // стартовой позиции.
            hero.AddComponent<HeroView>();

            var targetLabel = CreateWorldLabel(hero.transform, "HeroTargetLabel", ArtSpecs.HeroHeight + ArtSpecs.LabelGap);

            var doors = new GameObject("Doors");
            doors.transform.SetParent(root.transform, false);
            // Стартовая позиция для вида в редакторе до игры — в момент
            // запуска ConfigureCamera() в DoorMinigameController сама
            // пересчитает doorsParent.position.x под реальный aspect камеры
            // (см. ScreenLayout), так что это число ни на что не влияет в игре.
            doors.transform.localPosition = new Vector3(4.5f, 0f, 0f);

            SetField(controller, "hero", hero.transform);
            SetField(controller, "heroTargetLabel", targetLabel);
            SetField(controller, "doorPrefab", doorPrefab.GetComponent<DoorView>());
            SetField(controller, "doorsParent", doors.transform);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        // Кнопка уровня: рамка из дерева и камня, внутри номер; для
        // закрытого уровня поверх рамки затемнение и замок, для пройденного
        // — золотая медаль с галочкой в углу. Включением и выключением
        // замка и медали занимается LevelButton.Setup (код не менялся —
        // меняется только то, что нарисовано).
        private static void BuildLevelButtonPrefab()
        {
            var path = $"{UIFolder}/LevelButton.prefab";
            if (AlreadyExists(path))
            {
                Debug.Log($"{path} уже существует — пропускаю.");
                return;
            }

            var frameSprite = LoadArtSprite(ArtSpecs.LevelButtonFrame);
            var lockSprite = LoadArtSprite(ArtSpecs.IconLock);
            var checkSprite = LoadArtSprite(ArtSpecs.IconCheck);

            var buttonGo = new GameObject("LevelButton", typeof(RectTransform));
            var rect = buttonGo.GetComponent<RectTransform>();
            SetPreferredSize(buttonGo, 160, 160);

            var image = buttonGo.AddComponent<Image>();
            image.sprite = frameSprite;
            image.color = frameSprite != null ? Color.white : new Color(1f, 1f, 1f, 0.9f);
            image.preserveAspect = true;
            var button = buttonGo.AddComponent<Button>();
            button.targetGraphic = image;

            // Стандартная "недоступная" тонировка кнопки делает её
            // полупрозрачной — для закрытого уровня это бы просвечивало
            // фон сквозь рамку. Закрытость показывает затемнение с замком,
            // поэтому здесь цвет недоступной кнопки не меняется.
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.93f, 0.75f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f);
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var numberRect = CreateUIObject("NumberLabel", rect);
            StretchFull(numberRect);
            var numberLabel = numberRect.gameObject.AddComponent<TextMeshProUGUI>();
            numberLabel.text = "1";
            numberLabel.fontSize = 64;
            numberLabel.fontStyle = FontStyles.Bold;
            // Светлый цвет: внутри рамки тёмный "пергамент".
            numberLabel.color = new Color(0.98f, 0.92f, 0.75f);
            numberLabel.alignment = TextAlignmentOptions.Center;
            numberLabel.raycastTarget = false;

            // Закрытый уровень: рамка затемняется (та же картинка рамки,
            // закрашенная в полупрозрачный чёрный — повторяет скруглённые
            // углы) и по центру замок.
            var lockOverlay = CreateIcon(rect, "LockIcon", new Color(0f, 0f, 0f, 0.6f), 160, 160);
            if (frameSprite != null) lockOverlay.sprite = frameSprite;
            lockOverlay.preserveAspect = true;
            lockOverlay.raycastTarget = false;
            StretchFull(lockOverlay.rectTransform);

            var lockImage = CreateIcon(lockOverlay.rectTransform, "LockSprite", Color.white, 70, 90);
            if (lockSprite != null) lockImage.sprite = lockSprite;
            lockImage.preserveAspect = true;
            lockImage.raycastTarget = false;
            AnchorAt(lockImage.rectTransform, 0.5f, 0.5f, 70, 90);

            // Пройденный уровень: золотая медаль с галочкой в правом верхнем
            // углу, чуть выступает за рамку.
            var completedIcon = CreateIcon(rect, "CompletedIcon", Color.white, 60, 60);
            if (checkSprite != null) completedIcon.sprite = checkSprite;
            else completedIcon.color = new Color(0.25f, 0.8f, 0.35f, 0.85f);
            completedIcon.preserveAspect = true;
            completedIcon.raycastTarget = false;
            var completedRect = completedIcon.rectTransform;
            completedRect.anchorMin = new Vector2(1, 1);
            completedRect.anchorMax = new Vector2(1, 1);
            completedRect.anchoredPosition = new Vector2(-8, -8);

            var levelButton = buttonGo.AddComponent<LevelButton>();
            SetField(levelButton, "button", button);
            SetField(levelButton, "numberLabel", numberLabel);
            SetField(levelButton, "lockIcon", lockOverlay.gameObject);
            SetField(levelButton, "completedIcon", completedIcon.gameObject);

            PrefabUtility.SaveAsPrefabAsset(buttonGo, path);
            Object.DestroyImmediate(buttonGo);
        }

        // fontSize — мировые единицы, не пиксели (прошли путь 4 → 0.5 →
        // 1.0 → 1.3 → 1.5, теперь 1.8 — числа и примеры должны хорошо
        // читаться на нарисованных фонах). yOffset считается от "ног"
        // объекта (pivot внизу картинки): высота картинки + небольшой
        // зазор (ArtSpecs.LabelGap) — подпись всегда стоит ровно над
        // макушкой / верхом двери. Белый жирный текст с чёрной обводкой
        // (отдельный материал, см. GetWorldLabelMaterial) читается и на
        // светлом лугу, и на тёмной крепости; sortingOrder выше, чем у
        // картинок, чтобы подпись никогда не пряталась за ними.
        private static TextMeshPro CreateWorldLabel(Transform parent, string name, float yOffset)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, yOffset, 0f);
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 1.8f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.text = "0";
            tmp.fontSharedMaterial = GetWorldLabelMaterial(tmp.font);
            tmp.sortingOrder = 10;
            return tmp;
        }
    }
}
