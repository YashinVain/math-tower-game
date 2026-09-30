using System.Collections.Generic;
using UnityEngine;
using TMPro;
using MathGame.Data;
using MathGame.Minigames.Framework;
using MathGame.Utils;

namespace MathGame.Minigames.Towers
{
    // Реализация мини-игры "Башни" (см. docs/ARCHITECTURE.md — "Как
    // добавить новый тип мини-игры" описывает именно этот класс как пример).
    // Этажи одной башни растут вверх (герой стоит у подножия и не
    // забирается наверх — бьёт снизу), а сами башни идут одна за другой по
    // горизонтали. Держит свою силу героя и текущий список големов сам — с
    // другими мини-играми в этом же уровне не делится ничем, кроме общего
    // контракта MinigameController.
    public class TowerMinigameController : MinigameController
    {
        [SerializeField] private HeroView hero;
        [SerializeField] private GolemView golemPrefab;
        [SerializeField] private Transform golemSlotParent;
        [SerializeField] private TextMeshPro heroPowerLabel;
        [SerializeField] private float floorHeight = 2.2f;
        [SerializeField] private float baseHeight = 1f;
        [SerializeField] private float towerSpacing = 5f;
        [SerializeField] private float moveDuration = 0.6f;
        [SerializeField] private float cameraOrthographicSize = 5.7f;
        [SerializeField] private float cameraY = 4.3f;
        // Доли ширины экрана от левого края (0..1) — не мировые единицы.
        // Герой ближе к левому краю, башня — правее центра. Пересчитываются
        // в мировые единицы в ConfigureCamera() под реальный aspect камеры,
        // поэтому расстановка не ломается при смене формы окна (см. ScreenLayout).
        [SerializeField] private float heroScreenFraction = 0.3f;
        [SerializeField] private float towerScreenFraction = 0.7f;

        private TowerMinigameDefinition _definition;
        private MinigameRuntimeContext _context;
        private int _heroPower;
        private int _towerIndex;
        private int _difficultyMaxValue;
        private float _heroToTowerDistance;
        private float _towerOriginX;
        private readonly List<GolemView> _activeGolems = new List<GolemView>();
        private bool _inputLocked;

        public override void Begin(MinigameDefinition definition, MinigameRuntimeContext context)
        {
            _definition = (TowerMinigameDefinition)definition;
            _context = context;
            _heroPower = _definition.heroStartingPower;
            UpdateHeroLabel();
            ConfigureCamera();
            SpawnTower(0);
        }

        // Башни растут вверх, двери — в стороны: одной общей настройки
        // камеры на оба типа мини-игр не хватает (проверено на практике).
        // Каждый тип сам настраивает высоту обзора и то, на какой Y
        // смотреть, при своём запуске — сравните с DoorMinigameController.
        //
        // Заодно здесь же считаем, куда на самом деле поставить героя и
        // башню по X (ScreenLayout), а не берём готовое число: cam.aspect —
        // это реальное соотношение сторон ИМЕННО СЕЙЧАС, какого бы размера
        // ни было окно игрока.
        private void ConfigureCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            cam.orthographicSize = cameraOrthographicSize;
            var pos = cam.transform.position;
            pos.y = cameraY;
            cam.transform.position = pos;

            float halfWidth = ScreenLayout.HalfWidth(cameraOrthographicSize, cam.aspect);
            var follow = cam.GetComponent<CameraFollowX>();
            if (follow != null)
                follow.SetOffsetX(ScreenLayout.OffsetXForHeroFraction(halfWidth, heroScreenFraction));

            _heroToTowerDistance = ScreenLayout.DistanceForTargetFraction(halfWidth, heroScreenFraction, towerScreenFraction);
            _towerOriginX = hero.transform.position.x + _heroToTowerDistance;
        }

        private void SpawnTower(int towerIndex)
        {
            _towerIndex = towerIndex;
            _inputLocked = true; // ждём, пока герой дойдёт до подножия новой башни

            foreach (var golem in _activeGolems)
                if (golem != null) Destroy(golem.gameObject);
            _activeGolems.Clear();

            int size = _definition.towerSizes[towerIndex];
            // _towerOriginX — X первой башни (уже посчитан в ConfigureCamera
            // так, чтобы герой встал на нужную долю экрана). Каждая
            // следующая башня — просто на towerSpacing дальше от неё; так
            // герой ощутимо идёт вперёд, но при этом ПОСЛЕ прихода снова
            // встаёт на ту же самую долю экрана у каждой башни, а не
            // постепенно сползает к краю, как было раньше (towerSpacing
            // умножался на towerIndex от уже сдвинутой позиции героя).
            float towerX = _towerOriginX + towerSpacing * towerIndex;
            var difficulty = _context.BuildDifficulty();
            _difficultyMaxValue = difficulty.MaxValue;
            var beatableSlots = PickBeatableSlots(size);
            var usedTargets = new HashSet<int>(); // чтобы в одной башне не было двух големов с одинаковым ответом (то есть и "1+2"/"2+1" тоже не встретятся — у них один и тот же ответ)

            for (int i = 0; i < size; i++)
            {
                // baseHeight поднимает нижний этаж над головой героя — иначе
                // этаж 0 оказывается на одной высоте с героем, и подпись
                // силы героя накладывается на подпись первого примера.
                Vector3 pos = new Vector3(
                    towerX,
                    golemSlotParent.position.y + baseHeight + i * floorHeight,
                    golemSlotParent.position.z);

                var golem = Instantiate(golemPrefab, pos, Quaternion.identity, golemSlotParent);
                var problem = beatableSlots.Contains(i)
                    ? GenerateProblemRelativeToPower(difficulty, beatable: true, usedTargets)
                    : GenerateProblemRelativeToPower(difficulty, beatable: false, usedTargets);
                usedTargets.Add(problem.Answer);
                golem.Init(problem, OnGolemSelected);
                _activeGolems.Add(golem);
            }

            Vector3 heroTarget = new Vector3(towerX - _heroToTowerDistance, hero.transform.position.y, hero.transform.position.z);
            hero.MoveTo(heroTarget, moveDuration, () => _inputLocked = false);
        }

        // Выбирает, какие места в башне получат "проходимый" (по силе
        // герою) пример — случайно, чтобы расположение не угадывалось
        // заранее. Не больше definition.maxBeatableAtOnce штук сразу, но
        // и не меньше одного — минимум 1 нижняя граница жёсткая: без хотя
        // бы одного проходимого голема уровень становится непроходимым.
        private HashSet<int> PickBeatableSlots(int towerSize)
        {
            int budget = Mathf.Clamp(_definition.maxBeatableAtOnce, 1, towerSize);
            var slots = new HashSet<int>();
            while (slots.Count < budget)
                slots.Add(Random.Range(0, towerSize));
            return slots;
        }

        // Раньше это было "подбором": генерировали обычный случайный пример
        // и проверяли, подходит ли он по силе — если диапазон чисел в
        // настройках был сильно меньше или сильно больше силы героя,
        // подходящий пример мог вообще не встретиться за отведённые
        // попытки, и после них возвращалось что получилось — то есть
        // условие "проходимый"/"непроходимый" могло не выполниться совсем.
        // Правильный способ — строить пример "от ответа" (как уже сделано
        // для двери с верным ответом в DoorMinigameController): сначала
        // сами решаем, каким должен быть ответ (обязательно ≤ силы героя,
        // либо обязательно больше), а затем GenerateWithAnswer собирает
        // под это число сам пример. Это гарантия, а не вероятность.
        //
        // target для "непроходимого" голема дополнительно зажат сверху в
        // 2×MaxValue — это максимум, который в принципе можно показать
        // суммой двух чисел из настроенного диапазона (см. также
        // WinAgainstGolem — сила героя тоже не растёт выше MaxValue именно
        // за тем, чтобы для "непроходимого" всегда оставался запас выше
        // heroPower, но всё ещё в пределах 2×MaxValue).
        //
        // usedTargets — чтобы в одной башне не было двух големов с
        // одинаковым ответом: иначе они могут визуально совпасть вплоть до
        // "зеркальных" примеров вроде "1+2" и "2+1" (у обоих ответ 3).
        private MathProblem GenerateProblemRelativeToPower(DifficultyContext difficulty, bool beatable, HashSet<int> usedTargets)
        {
            int target = 0;
            for (int attempt = 0; attempt < 20; attempt++)
            {
                if (beatable)
                {
                    int upperBound = Mathf.Max(0, _heroPower);
                    target = Random.Range(Mathf.Min(0, upperBound), upperBound + 1);
                }
                else
                {
                    int spread = Mathf.Max(1, difficulty.MaxValue - difficulty.MinValue + 1);
                    int maxRepresentable = difficulty.MaxValue * 2;
                    target = Mathf.Min(_heroPower + Random.Range(1, spread + 1), maxRepresentable);
                }

                if (!usedTargets.Contains(target)) break; // нашли ответ, которого ещё нет в этой башне
            }

            return _context.ProblemGenerator.GenerateWithAnswer(difficulty, target);
        }

        private void OnGolemSelected(GolemView golem)
        {
            if (_inputLocked || !_activeGolems.Contains(golem)) return;
            _inputLocked = true;

            if (golem.Answer <= _heroPower) WinAgainstGolem(golem);
            else LoseToGolem();
        }

        private void WinAgainstGolem(GolemView golem)
        {
            _heroPower += golem.Answer;
            // Предел силы героя: definition.powerCap, если его явно задали
            // в настройках уровня, иначе — MaxValue из текущего диапазона
            // чисел. Без этого сила героя со временем перерастает то, что
            // вообще можно выразить суммой двух чисел из диапазона — и
            // тогда "непроходимый" пример для голема неизбежно приходится
            // показывать с числом за пределами настроек (баг, который
            // видел игрок: "11 + 6" при диапазоне 0..6). Держа силу героя
            // не выше MaxValue, всегда остаётся запас чисел ВЫШЕ его силы,
            // но всё ещё внутри 2×MaxValue — как раз для непроходимых.
            int cap = _definition.powerCap > 0 ? _definition.powerCap : _difficultyMaxValue;
            _heroPower = Mathf.Min(_heroPower, cap);
            UpdateHeroLabel();

            hero.PlayVictoryPulse();
            golem.PlayDefeatedByHero(() =>
            {
                _activeGolems.Remove(golem);
                _inputLocked = false;

                if (_activeGolems.Count > 0) return; // ждём, какого голема игрок выберет следующим

                if (_towerIndex + 1 < _definition.towerSizes.Count) SpawnTower(_towerIndex + 1);
                else RaiseCompleted();
            });
        }

        private void LoseToGolem()
        {
            hero.PlayDefeat(RaiseFailed);
        }

        private void UpdateHeroLabel()
        {
            if (heroPowerLabel != null) heroPowerLabel.text = _heroPower.ToString();
        }
    }
}
