using System;
using System.IO;
using UnityEngine;
using MathGame.Data;

namespace MathGame.Services
{
    // Хранит настройки в JSON-файле в Application.persistentDataPath — это
    // стандартная для Unity папка данных пользователя (не внутри проекта и
    // не внутри сборки игры), одна и та же между запусками игры на одном
    // устройстве. Реализация спрятана за ISettingsService, чтобы остальной
    // код не знал, что это именно JSON-файл — при необходимости способ
    // хранения можно будет поменять, не трогая код, который читает настройки.
    public class SettingsService : ISettingsService
    {
        private const string FileName = "settings.json";
        private readonly string _filePath;

        public GameSettingsData Current { get; private set; }

        public SettingsService()
        {
            _filePath = Path.Combine(Application.persistentDataPath, FileName);
            Current = Load();
        }

        private GameSettingsData Load()
        {
            var result = LoadRaw();
            // На случай сохранения с диска, сделанного до появления этого
            // ограничения (или отредактированного руками) — а не только на
            // случай live-редактирования в SettingsPanel, где та же проверка
            // тоже есть. Без этого игрок мог бы так и не увидеть экран
            // настроек и играть со старым слишком узким диапазоном, который
            // ломает башни (см. комментарий у GameSettingsData.ClampNumberRange).
            result.ClampNumberRange();
            return result;
        }

        private GameSettingsData LoadRaw()
        {
            if (File.Exists(_filePath))
            {
                try
                {
                    string json = File.ReadAllText(_filePath);
                    var loaded = JsonUtility.FromJson<GameSettingsData>(json);
                    if (loaded != null) return loaded;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Не удалось прочитать {_filePath}, использую настройки по умолчанию: {e.Message}");
                }
            }
            return new GameSettingsData();
        }

        public void Save()
        {
            string json = JsonUtility.ToJson(Current, true);
            File.WriteAllText(_filePath, json);
        }

        public void ResetToDefaults()
        {
            Current = new GameSettingsData();
            Save();
        }
    }
}
