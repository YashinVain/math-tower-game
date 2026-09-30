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
        // Насколько центр камеры должен быть ВЫШЕ героя по Y (не
        // абсолютная высота в мире!). Раньше это была абсолютная мировая
        // высота, и расчёт молча предполагал, что герой стоит на Y=0 — а
        // MinigameHost в сцене был сдвинут на -1 по Y, так что герой на
        // самом деле стоял ниже, чем камера считала, и вылезал за нижний
        // край экрана. Теперь считается от реальной позиции героя (см.
        // ConfigureCamera) — сработает, даже если герой когда-нибудь
        // окажется в другом месте сцены.
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
            pos.y = hero.transform.position.y + cameraY;
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
            var targets = PickTargets(size, difficulty);

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
                var problem = _context.ProblemGenerator.GenerateWithAnswer(difficulty, targets[i]);
                golem.Init(problem, OnGolemSelected);
                _activeGolems.Add(golem);
            }

            Vector3 heroTarget = new Vector3(towerX - _heroToTowerDistance, hero.transform.position.y, hero.transform.position.z);
            hero.MoveTo(heroTarget, moveDuration, () => _inputLocked = false);
        }

        // Строит ответы для всех големов этажа сразу, а не по одному — это
        // важно: раньше "непроходимые" големы были непроходимыми НАВСЕГДА
        // (их цель ставилась один раз и никогда не могла стать достижимой),
        // а правило "нужно победить всех големов этажа" при этом никуда не
        // делось — значит, единственным выходом для такого голема было
        // кликнуть и проиграть. Игрок абсолютно справедливо назвал это
        // "этаж скипается сам" (я убирал такого голема автоматически, чтобы
        // хоть как-то не давать умереть) — но правильное решение другое:
        // "непроходимый" должен быть непроходимым только ПОКА ТЫ ЕГО НЕ
        // ЗАСЛУЖИЛ, а не навсегда. Сначала выбираем "первую волну" — ровно
        // столько проходимых прямо сейчас примеров, сколько разрешает
        // maxBeatableAtOnce (минимум 1). Складываем, насколько вырастет
        // сила героя, если победить их все — это гарантированный потолок.
        // "Вторая волна" (оставшиеся места) получает цель строго ВЫШЕ
        // текущей силы героя (иначе это не было бы выбором), но не выше
        // этого гарантированного потолка — то есть каждый голем на этаже
        // рано или поздно станет проходимым, если бить их в правильном
        // порядке. Кликать каждого голема обязательно — скипов больше нет.
        private int[] PickTargets(int towerSize, DifficultyContext difficulty)
        {
            var targets = new int[towerSize];
            var usedTargets = new HashSet<int>(); // чтобы не было двух големов с одинаковым ответом на одном этаже (в том числе "зеркальных" вроде "1+2"/"2+1")

            int budget = Mathf.Clamp(_definition.maxBeatableAtOnce, 1, towerSize);
            var firstWaveSlots = new HashSet<int>();
            while (firstWaveSlots.Count < budget)
                firstWaveSlots.Add(Random.Range(0, towerSize));

            int cap = HeroPowerCap();
            int guaranteedPowerAfterFirstWave = _heroPower;
            foreach (int slot in firstWaveSlots)
            {
                int t = PickDistinctTarget(usedTargets, Mathf.Min(0, _heroPower), Mathf.Max(0, _heroPower));
                targets[slot] = t;
                usedTargets.Add(t);
                guaranteedPowerAfterFirstWave = Mathf.Min(guaranteedPowerAfterFirstWave + t, cap);
            }

            int maxRepresentable = difficulty.MaxValue * 2; // максимум, который вообще можно показать суммой двух чисел из диапазона
            for (int i = 0; i < towerSize; i++)
            {
                if (firstWaveSlots.Contains(i)) continue;

                int lo = _heroPower + 1;
                int hi = Mathf.Min(guaranteedPowerAfterFirstWave, maxRepresentable);
                int t = lo <= hi
                    ? PickDistinctTarget(usedTargets, lo, hi)
                    : PickDistinctTarget(usedTargets, Mathf.Min(0, _heroPower), Mathf.Max(0, _heroPower)); // герой уже и так достаточно силён — пусть этот голем тоже будет сразу проходимым, диапазон важнее "сложности"

                targets[i] = t;
                usedTargets.Add(t);
            }

            return targets;
        }

        private int PickDistinctTarget(HashSet<int> usedTargets, int lo, int hi)
        {
            int target = lo;
            for (int attempt = 0; attempt < 20; attempt++)
            {
                target = Random.Range(lo, hi + 1);
                if (!usedTargets.Contains(target)) break; // нашли ответ, которого ещё нет на этом этаже
            }
            return target;
        }

        // definition.powerCap, если его явно задали в настройках уровня,
        // иначе — 1.5×MaxValue из текущего диапазона чисел. Без верхнего
        // предела сила героя со временем перерастает то, что вообще можно
        // выразить суммой двух чисел из диапазона — тогда пример неизбежно
        // приходится показывать с числом за пределами настроек (баг,
        // который видел игрок: "11 + 6" при диапазоне 0..6). Ровно
        // MaxValue в качестве предела тоже пробовали — хватало для защиты
        // от бага, но герой почти сразу упирался в потолок и переставал
        // расти вообще ("счётчик силы не добавляет цифры"). 1.5×MaxValue
        // оставляет заметный рост силы и всё ещё держит запас выше потолка
        // (вплоть до 2×MaxValue) для вторoй волны.
        private int HeroPowerCap() =>
            _definition.powerCap > 0 ? _definition.powerCap : Mathf.RoundToInt(_difficultyMaxValue * 1.5f);

        private void OnGolemSelected(GolemView golem)
        {
            if (_inputLocked || !_activeGolems.Contains(golem)) return;
            _inputLocked = true;

            if (golem.Answer <= _heroPower) WinAgainstGolem(golem);
            else LoseToGolem();
        }

        private void WinAgainstGolem(GolemView golem)
        {
            _heroPower = Mathf.Min(_heroPower + golem.Answer, HeroPowerCap());
            UpdateHeroLabel();

            hero.PlayVictoryPulse();
            golem.PlayDefeatedByHero(() =>
            {
                _activeGolems.Remove(golem);
                _inputLocked = false;

                if (_activeGolems.Count > 0) return; // на этаже остались ещё голема — PickTargets гарантирует, что все они рано или поздно станут проходимыми

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
