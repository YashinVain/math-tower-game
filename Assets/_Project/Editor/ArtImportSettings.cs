using System.IO;
using UnityEditor;
using UnityEngine;

namespace MathGame.EditorTools
{
    // Одна таблица "какого размера что в игре" и пути ко всем картинкам.
    //
    // Размер в игре задаётся ВЫСОТОЙ в мировых единицах (юнитах): герой
    // 1.9 юнита, гоблин 1.7, дверь 2.0. Почему именно так (а не масштабом
    // объекта): тогда у всех объектов масштаб остаётся 1, подписи над
    // головами не растягиваются вместе с картинкой, а коллайдеры сами
    // подгоняются под картинку. Хотите крупнее/мельче героя — поменяйте
    // число здесь, Unity сам переимпортирует картинку (см. GetVersion ниже).
    public static class ArtSpecs
    {
        public const string ArtRoot = "Assets/_Project/Art/";

        public const string CharactersFolder = ArtRoot + "Characters/";
        public const string DoorsFolder = ArtRoot + "Doors/";
        public const string BackgroundsFolder = ArtRoot + "Backgrounds/";
        public const string UIFolder = ArtRoot + "UI/";

        public const string Hero = CharactersFolder + "hero.png";
        public static readonly string[] Goblins =
        {
            CharactersFolder + "goblin.png",
            CharactersFolder + "goblin_2.png",
            CharactersFolder + "goblin_3.png",
        };

        public const string DoorClosed = DoorsFolder + "door_closed.png";
        public const string DoorOpen = DoorsFolder + "door_open.png";
        public const string DoorLocked = DoorsFolder + "door_locked.png";

        // Четыре фона по нарастанию "страшности": луг → пустошь → руины →
        // крепость гоблинов. Какой уровень какой фон получает — см.
        // BackgroundForLevel.
        public static readonly string[] Backgrounds =
        {
            BackgroundsFolder + "bg_1_meadow.png",
            BackgroundsFolder + "bg_2_wasteland.png",
            BackgroundsFolder + "bg_3_ruins.png",
            BackgroundsFolder + "bg_4_fortress.png",
        };

        // Фоны ВНУТРИ ЗАМКА для уровней с дверями (тот же рост «страшности»,
        // что у уличных: зал → заброшенный зал → логово → тронный зал).
        // Если файла ещё нет, уровень получает уличный фон (см.
        // BackgroundForLevel) — игра работает и с частью картинок.
        public static readonly string[] CastleBackgrounds =
        {
            BackgroundsFolder + "bg_castle_1_hall.png",
            BackgroundsFolder + "bg_castle_2_neglected.png",
            BackgroundsFolder + "bg_castle_3_lair.png",
            BackgroundsFolder + "bg_castle_4_throne.png",
        };

        // Где на всех фонах игры (стены замка и улица) проходит "земля" — доля
        // высоты картинки снизу, на ней стоят ноги героя, дверей и основание
        // башни. Рисуют фоны Tools/MakeWallArt.ps1 и Tools/MakeSceneryArt.ps1
        // (у стен это последняя строка вывода) — число должно совпадать.
        public const float SceneFloorLine = 0.1709f;

        // Башня собирается из трёх блоков одинакового размера (нижний этаж
        // с дверью, средний, верхний с зубцами), см. TowerView.
        public const string TowersFolder = ArtRoot + "Towers/";
        public const string TowerBase = TowersFolder + "tower_base.png";
        public const string TowerMid = TowersFolder + "tower_mid.png";
        public const string TowerTop = TowersFolder + "tower_top.png";

        public const string MenuBackground = UIFolder + "menu_background.png";
        public const string LevelButtonFrame = UIFolder + "level_button_frame.png";
        public const string IconLock = UIFolder + "icon_lock.png";
        public const string IconCheck = UIFolder + "icon_check.png";
        public const string IconTick = UIFolder + "icon_tick.png"; // галочка для коробочек настроек

        // Слайдер: дорожка и заливка — "таблетки" для 9-slice (скругление по
        // краям 18 px), бегунок — золотой кружок. Рисует Tools/MakeUiArt.ps1.
        public const string SliderTrack = UIFolder + "slider_track.png";
        public const string SliderFill = UIFolder + "slider_fill.png";
        public const string SliderKnob = UIFolder + "slider_knob.png";
        public const float SliderBarCornerPx = 18f;

        // Растягиваемые детали интерфейса, собранные из рамки уровня
        // (Tools/MakeUiArt.ps1): широкая кнопка и доска для окон. У обеих
        // уголки по 76 px — это границы 9-slice (уголки не растягиваются,
        // стороны и центр тянутся под любой размер).
        public const string ButtonWide = UIFolder + "button_wide.png";
        public const string PanelBoard = UIFolder + "panel_board.png";
        public const int SlicedCornerPx = 76;

        // Высота в мире, юниты.
        // Герой крупнее гоблинов (1.7) — раньше выглядел «маленьким» (было
        // 1.9, то есть 17% высоты экрана; теперь около 20%). Выше поднимать
        // не стали: башня на 4 этажа должна помещаться в экран.
        public const float HeroHeight = 2.3f;
        public const float GoblinHeight = 1.7f;
        // Дверь заметно выше героя (1.9) — как настоящая дверь в замке. Ширина
        // получается около 2.46. На узких окнах DoorMinigameController
        // автоматически уменьшает двери, чтобы все четыре помещались.
        public const float DoorHeight = 2.8f;

        // Зазор между макушкой/верхом двери и подписью над ней.
        public const float LabelGap = 0.3f;

        // Высота одного блока башни в мире (расстояние между этажами). Должна
        // совпадать с TowerView.floorHeight. 4 этажа такой высоты ещё
        // помещаются в обзор камеры (5.7) вместе с верхним блоком и флагом.
        public const float TowerFloorHeight = 2.35f;

        // Картинка верхнего этажа нарисована примерно на 13% уже средней
        // (стена 802 px против 912, площадка 1030 против 1184). Чтобы
        // стены башни не сужались скачком, при импорте верхний блок
        // увеличивается во столько раз (отношение ширин площадок).
        public const float TowerTopScale = 1.15f;

        // Номер "ступени страшности" уровня: 1–2 → 0, 3–5 → 1, 6–8 → 2, 9–10 → 3.
        private static int StageForLevel(int levelIndex)
        {
            if (levelIndex < 2) return 0;
            if (levelIndex < 5) return 1;
            if (levelIndex < 8) return 2;
            return 3;
        }

        // Уровни-башни — на улице, уровни-двери — внутри замка (если картинка
        // замка уже есть; иначе временно уличный фон той же ступени).
        public static string BackgroundForLevel(int levelIndex, bool isDoorLevel)
        {
            int stage = StageForLevel(levelIndex);
            if (isDoorLevel && File.Exists(CastleBackgrounds[stage])) return CastleBackgrounds[stage];
            return Backgrounds[stage];
        }

        public static bool TryGetWorldHeight(string assetPath, out float height)
        {
            if (assetPath.StartsWith(CharactersFolder))
            {
                height = Path.GetFileName(assetPath).StartsWith("hero") ? HeroHeight : GoblinHeight;
                return true;
            }
            if (assetPath.StartsWith(DoorsFolder))
            {
                height = DoorHeight;
                return true;
            }
            if (assetPath.StartsWith(TowersFolder))
            {
                height = Path.GetFileName(assetPath).StartsWith("tower_top")
                    ? TowerFloorHeight * TowerTopScale
                    : TowerFloorHeight;
                return true;
            }
            height = 0f;
            return false;
        }

        // Персонажи, двери и фоны "стоят" на нижнем крае картинки — точка
        // привязки (pivot) внизу по центру. Тогда позиция объекта в мире —
        // это его "ноги", и героя, гоблинов и двери легко поставить на одну
        // линию земли. У интерфейса точка привязки по центру.
        public static SpriteAlignment PivotFor(string assetPath)
        {
            if (assetPath.StartsWith(CharactersFolder) ||
                assetPath.StartsWith(DoorsFolder) ||
                assetPath.StartsWith(TowersFolder) ||
                assetPath.StartsWith(BackgroundsFolder))
                return SpriteAlignment.BottomCenter;
            return SpriteAlignment.Center;
        }
    }

    // Автоматически настраивает импорт КАЖДОЙ картинки из Assets/_Project/Art:
    // делает её спрайтом, подбирает масштаб (Pixels Per Unit) под высоту из
    // ArtSpecs, ставит точку привязки, отключает mipmap'ы и сжатие (чтобы
    // чёткие контуры мультяшной графики не мылились). Раньше это делалось
    // бы руками в Inspector для каждого из ~15 файлов; теперь любой новый
    // PNG, положенный в эти папки, настроится сам.
    public class ArtImportSettings : AssetPostprocessor
    {
        // Меняйте номер, когда меняете правила/числа выше: Unity
        // переимпортирует все картинки заново и применит новые настройки.
        public override uint GetVersion() => 8;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtSpecs.ArtRoot)) return;
            if (Path.GetFileName(assetPath) == "PlaceholderSquare.png") return; // старый квадрат-заглушка

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            // Картинки большие (герой ~1300 px, а на экране он ~130 px), и
            // без mipmap'ов (заранее уменьшенных копий текстуры) такое
            // сильное уменьшение даёт "ступеньки" и мерцание — именно это
            // выглядело как "пиксельность". Mipmap'ы + трилинейная
            // фильтрация делают уменьшение плавным. Фильтр Кайзера даёт
            // самые чёткие уменьшенные копии.
            importer.mipmapEnabled = true;
            importer.mipmapFilter = TextureImporterMipFilter.KaiserFilter;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Trilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;

            float pixelsPerUnit = 100f;
            if (ArtSpecs.TryGetWorldHeight(assetPath, out float worldHeight) &&
                TryReadPngHeight(assetPath, out int pixelHeight) && pixelHeight > 0)
            {
                pixelsPerUnit = pixelHeight / worldHeight;
            }

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)ArtSpecs.PivotFor(assetPath);
            settings.spritePixelsPerUnit = pixelsPerUnit;
            importer.SetTextureSettings(settings);

            // 9-slice для растягиваемых кнопки и доски.
            if (assetPath == ArtSpecs.ButtonWide || assetPath == ArtSpecs.PanelBoard)
            {
                float b = ArtSpecs.SlicedCornerPx;
                importer.spriteBorder = new Vector4(b, b, b, b);
            }
            // ... и для дорожки и заливки слайдера (скруглённые концы).
            else if (assetPath == ArtSpecs.SliderTrack || assetPath == ArtSpecs.SliderFill)
            {
                float b = ArtSpecs.SliderBarCornerPx;
                importer.spriteBorder = new Vector4(b, b, b, b);
            }
        }

        // Высота PNG лежит прямо в заголовке файла (байты 20–23) — так не
        // нужно загружать всю картинку, чтобы узнать её размер.
        private static bool TryReadPngHeight(string assetPath, out int height)
        {
            height = 0;
            try
            {
                using (var stream = File.OpenRead(assetPath))
                {
                    var header = new byte[24];
                    if (stream.Read(header, 0, 24) < 24) return false;
                    height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
                    return true;
                }
            }
            catch (IOException)
            {
                return false;
            }
        }

        [MenuItem("MathGame/Reimport Art")]
        public static void ReimportAll()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/_Project/Art" });
            foreach (var guid in guids)
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
            Debug.Log($"Картинки переимпортированы: {guids.Length} шт.");
        }
    }
}
