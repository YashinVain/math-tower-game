using System.Collections.Generic;
using UnityEngine;
using TMPro;
using MathGame.Data;
using MathGame.Minigames.Framework;
using MathGame.Utils;

namespace MathGame.Minigames.Doors
{
    // Реализация мини-игры "Двери". Нужно выбрать correctDoorsRequired
    // верных дверей подряд; при ошибке — либо смерть героя (обычный режим),
    // либо дверь просто не открывается и можно попробовать другую
    // (упрощённый режим, settings.doorsEasyModeEnabled — специально для
    // младшей аудитории, как в задании).
    public class DoorMinigameController : MinigameController
    {
        [SerializeField] private Transform hero;
        [SerializeField] private TextMeshPro heroTargetLabel;
        [SerializeField] private DoorView doorPrefab;
        [SerializeField] private Transform doorsParent;
        // Расстояние между центрами соседних дверей. Дверь с картинкой
        // высотой 2.0 (см. ArtSpecs.DoorHeight) имеет ширину около 1.76, так
        // что между дверьми остаётся небольшой просвет (2.05 − 1.76).
        [SerializeField] private float doorSpacing = 2.05f;
        // Обзор камеры теперь такой же, как в башнях (5.7 и 4.3): после
        // подключения настоящей графики герой и двери стали крупнее
        // прежних квадратов, а четырём дверям в ряд нужно больше места по
        // ширине — при прежнем обзоре 4.2 они не помещались до правого края
        // на узких окнах. Заодно "Двери" и "Башни" теперь выглядят
        // одинаково: тот же размер героя на экране и та же линия земли
        // (раньше игрок просил именно этого).
        [SerializeField] private float cameraOrthographicSize = 5.7f;
        // Смещение камеры вверх ОТ реальной позиции героя (см.
        // ConfigureCamera), а не абсолютная высота в мире — иначе любой
        // сдвиг родительского объекта в сцене незаметно сдвинул бы героя
        // относительно экрана. Позиция героя — его "ноги" (pivot внизу
        // картинки), с этим смещением ноги стоят примерно на 12% высоты
        // экрана от низа — в полосе земли на нарисованных фонах.
        [SerializeField] private float cameraY = 4.3f;
        // Доли ширины экрана от левого края (0..1) — см. TowerMinigameController
        // и ScreenLayout: тот же приём, что и у башен, только здесь двигаем
        // не героя (он тут не ходит), а doorsParent под героя.
        [SerializeField] private float heroScreenFraction = 0.3f;
        // Было 0.6 — подняли до 0.62 вместе с расширением обзора камеры
        // выше: на самом узком из проверяемых соотношений сторон (4:3)
        // четыре двери с запасом под подписи едва помещаются до правого
        // края экрана при прежней доле (посчитано вручную: при 0.6 остаток
        // места до края был меньше ширины подписи над дверью).
        [SerializeField] private float doorsScreenFraction = 0.62f;

        private DoorMinigameDefinition _definition;
        private MinigameRuntimeContext _context;
        private readonly List<DoorView> _activeDoors = new List<DoorView>();
        private int _heroTargetNumber;
        private int _correctDoorsPassed;
        private bool _inputLocked;

        public override void Begin(MinigameDefinition definition, MinigameRuntimeContext context)
        {
            _definition = (DoorMinigameDefinition)definition;
            _context = context;
            _correctDoorsPassed = 0;
            ConfigureCamera();
            SpawnRound();
        }

        // Двери разложены в ряд по горизонтали (не вверх, как башни) — им
        // нужен свой обзор камеры, отдельный от TowerMinigameController.
        //
        // Здесь же (как и в TowerMinigameController) пересчитываем, куда
        // реально поставить героя и группу дверей по X — под фактический
        // aspect камеры, а не под заранее угаданное число.
        private void ConfigureCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            cam.orthographicSize = cameraOrthographicSize;
            var pos = cam.transform.position;
            pos.y = hero.position.y + cameraY;
            cam.transform.position = pos;

            float halfWidth = ScreenLayout.HalfWidth(cameraOrthographicSize, cam.aspect);
            var follow = cam.GetComponent<CameraFollowX>();
            if (follow != null)
                follow.SetOffsetX(ScreenLayout.OffsetXForHeroFraction(halfWidth, heroScreenFraction));

            float heroToDoorsDistance = ScreenLayout.DistanceForTargetFraction(halfWidth, heroScreenFraction, doorsScreenFraction);
            var doorsPos = doorsParent.position;
            doorsPos.x = hero.position.x + heroToDoorsDistance;
            doorsParent.position = doorsPos;
        }

        private void SpawnRound()
        {
            _inputLocked = false;

            foreach (var door in _activeDoors)
                if (door != null) Destroy(door.gameObject);
            _activeDoors.Clear();

            var difficulty = _context.BuildDifficulty();
            var targetProblem = _context.ProblemGenerator.Generate(difficulty);
            _heroTargetNumber = targetProblem.Answer;
            if (heroTargetLabel != null) heroTargetLabel.text = _heroTargetNumber.ToString();

            int doorCount = Mathf.Max(2, _definition.doorsPerRound);
            int correctSlot = Random.Range(0, doorCount);
            var usedAnswers = new HashSet<int> { _heroTargetNumber };

            float startX = doorsParent.position.x - doorSpacing * (doorCount - 1) * 0.5f;

            for (int i = 0; i < doorCount; i++)
            {
                MathProblem problem;
                if (i == correctSlot)
                {
                    problem = _context.ProblemGenerator.GenerateWithAnswer(difficulty, _heroTargetNumber);
                }
                else
                {
                    problem = GenerateDistinctDistractor(difficulty, usedAnswers);
                    usedAnswers.Add(problem.Answer);
                }

                Vector3 pos = new Vector3(startX + i * doorSpacing, doorsParent.position.y, doorsParent.position.z);
                var door = Instantiate(doorPrefab, pos, Quaternion.identity, doorsParent);
                door.Init(problem, OnDoorChosen);
                _activeDoors.Add(door);
            }
        }

        private MathProblem GenerateDistinctDistractor(DifficultyContext difficulty, HashSet<int> usedAnswers)
        {
            for (int attempt = 0; attempt < 20; attempt++)
            {
                var candidate = _context.ProblemGenerator.Generate(difficulty);
                if (!usedAnswers.Contains(candidate.Answer)) return candidate;
            }

            // Диапазон чисел совсем маленький, и 20 попыток не нашли
            // уникальный ответ. Совпадение с другой неверной дверью в этом
            // крайнем случае — просто совпадающие числа на дверях, не
            // страшно. А вот совпасть с heroTargetNumber нельзя ни в каком
            // случае — это создало бы вторую "правильную" дверь, а условие
            // задания — ровно одна.
            MathProblem fallback;
            int guard = 0;
            do
            {
                fallback = _context.ProblemGenerator.Generate(difficulty);
                guard++;
            } while (fallback.Answer == _heroTargetNumber && guard < 50);

            return fallback;
        }

        private void OnDoorChosen(DoorView door)
        {
            if (_inputLocked || !_activeDoors.Contains(door)) return;

            bool correct = door.Answer == _heroTargetNumber;
            if (correct)
            {
                _inputLocked = true;
                door.PlayOpenCorrect(() =>
                {
                    _correctDoorsPassed++;
                    if (_correctDoorsPassed >= _definition.correctDoorsRequired) RaiseCompleted();
                    else SpawnRound();
                });
                return;
            }

            if (_context.Settings.doorsEasyModeEnabled)
            {
                door.PlayLocked();
                return; // герой не погибает, пробуем другую дверь
            }

            _inputLocked = true;
            door.PlayGolemAmbush(RaiseFailed);
        }
    }
}
