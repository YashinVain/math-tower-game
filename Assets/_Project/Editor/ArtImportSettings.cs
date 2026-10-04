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

        public const string MenuBackground = UIFolder + "menu_background.png";
        public const string LevelButtonFrame = UIFolder + "level_button_frame.png";
        public const string IconLock = UIFolder + "icon_lock.png";
        public const string IconCheck = UIFolder + "icon_check.png";

        // Высота в мире, юниты.
        public const float HeroHeight = 1.9f;
        public const float GoblinHeight = 1.7f;
        public const float DoorHeight = 2.0f;

        // Зазор между макушкой/верхом двери и подписью над ней.
        public const float LabelGap = 0.3f;

        // Уровни 1–2 → луг, 3–5 → пустошь, 6–8 → руины, 9–10 → крепость.
        public static string BackgroundForLevel(int levelIndex)
        {
            if (levelIndex < 2) return Backgrounds[0];
            if (levelIndex < 5) return Backgrounds[1];
            if (levelIndex < 8) return Backgrounds[2];
            return Backgrounds[3];
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
        public override uint GetVersion() => 1;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtSpecs.ArtRoot)) return;
            if (Path.GetFileName(assetPath) == "PlaceholderSquare.png") return; // старый квадрат-заглушка

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
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
