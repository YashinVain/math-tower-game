using System;
using System.IO;
using UnityEngine;
using MathGame.Data;

namespace MathGame.Services
{
    public class ProgressService : IProgressService
    {
        private const string FileName = "progress.json";
        private readonly string _filePath;
        private ProgressData _data;

        public ProgressService()
        {
            _filePath = Path.Combine(Application.persistentDataPath, FileName);
            _data = Load();
        }

        private ProgressData Load()
        {
            if (File.Exists(_filePath))
            {
                try
                {
                    string json = File.ReadAllText(_filePath);
                    var loaded = JsonUtility.FromJson<ProgressData>(json);
                    if (loaded != null) return loaded;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Не удалось прочитать {_filePath}, прогресс начнётся заново: {e.Message}");
                }
            }
            return new ProgressData();
        }

        private void Save()
        {
            string json = JsonUtility.ToJson(_data, true);
            File.WriteAllText(_filePath, json);
        }

        public bool IsLevelCompleted(string levelId) => _data.IsCompleted(levelId);

        public bool IsLevelUnlocked(LevelCatalog catalog, string levelId)
        {
            int index = catalog.levels.FindIndex(l => l.levelId == levelId);
            if (index <= 0) return true; // первый уровень (или неизвестный id) не блокируем

            string previousId = catalog.levels[index - 1].levelId;
            return _data.IsCompleted(previousId) || _data.IsCompleted(levelId);
        }

        public void MarkLevelCompleted(string levelId)
        {
            _data.MarkCompleted(levelId);
            Save();
        }

        public void ResetProgress()
        {
            _data = new ProgressData();
            Save();
        }
    }
}
