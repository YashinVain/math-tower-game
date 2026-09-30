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
    //
    // ПРАВИЛА БАШНИ (итоговая версия): побеждать нужно ВСЕХ големов этажа,
    // без исключений — ни один не исчезает сам и не остаётся недоступным
    // навсегда. В момент появления этажа ровно maxBeatableAtOnce големов
    // (по умолчанию 1) проходимы прямо сейчас; остальные — нет, но КАЖДЫЙ
    // из них гарантированно станет проходимым, если бить големов в
    // правильном порядке (сначала те, что уже по силам). Раньше это
    // гарантировалось требованием "цель каждого следующего голема выше
    // суммы силы ВСЕХ предыдущих" — арифметически это заставляет силу
    // героя расти взрывным образом (почти удваиваться на каждом этаже), и
    // уже 4-этажная башня требовала нереалистично широкого диапазона чисел
    // в настройках. Сейчас вместо этого все "пока не проходимые" цели
    // ставятся ПЛОТНО сразу над текущей силой героя (сила+1, сила+2, ...) —
    // после победы над первым проходимым големом сила героя обычно сразу
    // перекрывает их все разом, а не поднимается на один шаг за раз. Это
    // работает при любом размере башни без экспоненциального роста чисел.
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
            // Потолок силы героя применяется именно ЗДЕСЬ, между башнями —
            // не в WinAgainstGolem на каждой победе. Если бы сила героя
            // подрезалась потолком ВНУТРИ башни, PickTargets мог посчитать
            // "пока не проходимую" цель исходя из будущего роста силы,
            // которого на самом деле не случится (WinAgainstGolem обрежет
            // его раньше) — и такая цель рисковала бы никогда не стать по
            // силам. Внутри одной башни сила героя растёт свободно (её всё
            // равно не даёт выйти за диапазон maxRepresentable в
            // PickTargets), а между башнями — придерживается потолком, чтобы
            // не улетать бесконечно далеко от следующего диапазона чисел.
            _heroPower = Mathf.Min(_heroPower, HeroPowerCap());
            UpdateHeroLabel();
            var targets = PickTargets(size, difficulty);

            // targets.Count ВСЕГДА равен size — башня больше никогда не
            // укорачивается (см. PickTargets).
            for (int i = 0; i < targets.Count; i++)
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

        // Строит ответы для ВСЕХ големов этажа сразу — так проще
        // гарантировать, что они все разные и что каждый рано или поздно
        // станет проходимым. Сначала — ровно maxBeatableAtOnce (минимум 1)
        // "сразу проходимых" целей (≤ силы героя). Остальные — "пока не
        // проходимые": каждая цель строго выше ТЕКУЩЕЙ силы героя (иначе
        // это не было бы выбором с самого начала), но не выше того, что
        // герой гарантированно наберёт, если будет побеждать големов по
        // возрастанию их цели — этот "потолок" пересчитывается на каждом
        // шаге и растёт вместе с уже распределёнными целями, поэтому почти
        // всегда хватает буквально одной первой победы, чтобы разом
        // открыть все остальные. Если для каких-то экстремальных настроек
        // одной попытки не хватило — пробуем построить весь этаж заново (до
        // 30 раз); и только в совсем крайнем случае берём максимум, какой
        // есть, лишь бы не укоротить башню и не выйти за диапазон чисел.
        private List<int> PickTargets(int towerSize, DifficultyContext difficulty)
        {
            int maxRepresentable = difficulty.MaxValue * 2; // максимум, который вообще можно показать суммой двух чисел из диапазона
            int requiredCount = Mathf.Clamp(_definition.maxBeatableAtOnce, 1, towerSize);

            for (int attempt = 0; attempt < 30; attempt++)
            {
                var targets = TryBuildTower(towerSize, requiredCount, maxRepresentable, allowShortfall: false);
                if (targets != null)
                {
                    Shuffle(targets);
                    return targets;
                }
            }

            // 30 попыток с новыми случайными числами не нашли рабочую
            // комбинацию — это значит, что диапазон чисел в настройках
            // действительно слишком узкий для такой большой башни. Строим
            // ещё раз, разрешая взять максимум из доступного диапазона там,
            // где не хватило места (возможен редкий повтор числа), но
            // ЭТАЖ НЕ ТЕРЯЕМ и за диапазон не выходим.
            var fallback = TryBuildTower(towerSize, requiredCount, maxRepresentable, allowShortfall: true);
            Shuffle(fallback);
            return fallback;
        }

        // Возвращает null, если на каком-то шаге совсем не осталось места
        // для очередной "пока не проходимой" цели (и allowShortfall=false) —
        // тогда PickTargets просто попробует ещё раз с новыми случайными
        // числами, а не оставит этаж неполным.
        private List<int> TryBuildTower(int towerSize, int requiredCount, int maxRepresentable, bool allowShortfall)
        {
            var targets = new List<int>(towerSize);
            var usedTargets = new HashSet<int>();

            int reachable = _heroPower;
            for (int i = 0; i < requiredCount; i++)
            {
                int t = PickDistinctTarget(usedTargets, Mathf.Min(0, _heroPower), Mathf.Min(Mathf.Max(0, _heroPower), maxRepresentable));
                usedTargets.Add(t);
                targets.Add(t);
                reachable += t;
            }

            int notYetCount = towerSize - requiredCount;
            for (int i = 0; i < notYetCount; i++)
            {
                int lo = _heroPower + 1; // строго выше НАЧАЛЬНОЙ силы героя — иначе был бы проходим сразу, до всякого выбора
                int hi = Mathf.Min(reachable, maxRepresentable);
                if (lo > hi)
                {
                    if (!allowShortfall) return null;
                    hi = Mathf.Min(maxRepresentable, lo);
                }

                int t = PickDistinctTarget(usedTargets, lo, hi);
                usedTargets.Add(t);
                targets.Add(t);
                reachable += t; // после победы над этим ответом сила героя вырастет ещё больше — окно для следующих целей расширяется
            }

            return targets;
        }

        private static void Shuffle(List<int> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private int PickDistinctTarget(HashSet<int> usedTargets, int lo, int hi)
        {
            for (int attempt = 0; attempt < 20; attempt++)
            {
                int candidate = Random.Range(lo, hi + 1);
                if (!usedTargets.Contains(candidate)) return candidate;
            }

            // 20 случайных попыток не нашли свободное число — ищем
            // перебором, а не сдаёмся сразу. Без этого шага при небольшом
            // диапазоне (мало вариантов чисел, а големов несколько) шанс
            // на случайное совпадение двух примеров (например, "2+2"
            // дважды на одном этаже) был выше, чем должен быть: 20 попыток
            // почти всегда достаточно, но не гарантированно.
            for (int candidate = lo; candidate <= hi; candidate++)
                if (!usedTargets.Contains(candidate)) return candidate;

            // Буквально все числа в [lo, hi] уже заняты другими примерами
            // этого же этажа — диапазон настроек слишком маленький для
            // стольких разных чисел сразу (в игре есть защита в настройках
            // — минимальная ширина диапазона, — которая должна делать этот
            // случай очень редким). Совпадение здесь неизбежно, но хотя бы
            // число остаётся в пределах диапазона.
            return lo;
        }

        // definition.powerCap, если его явно задали в настройках уровня,
        // иначе — 1.5×MaxValue из текущего диапазона чисел. Без верхнего
        // предела сила героя со временем перерастает то, что вообще можно
        // выразить суммой двух чисел из диапазона — тогда пример неизбежно
        // приходится показывать с числом за пределами настроек. 1.5×MaxValue
        // держит силу героя заметно ниже 2×MaxValue — то есть у следующей
        // башни всегда остаётся запас чисел выше стартовой силы героя для
        // "пока не проходимых" целей (см. PickTargets).
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
            // Без потолка здесь — он применяется только между башнями (см.
            // SpawnTower), не после каждой отдельной победы. Иначе сила
            // героя ВНУТРИ этой же башни могла бы обрезаться ниже того,
            // что PickTargets уже заложил в "пока не проходимые" цели, и
            // кто-то из големов так и не стал бы проходимым по-настоящему.
            _heroPower += golem.Answer;
            UpdateHeroLabel();

            hero.PlayVictoryPulse();
            golem.PlayDefeatedByHero(() =>
            {
                _activeGolems.Remove(golem);
                _inputLocked = false;

                if (_activeGolems.Count > 0) return; // на этаже остались ещё големы — PickTargets гарантирует, что все они рано или поздно станут проходимыми

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
