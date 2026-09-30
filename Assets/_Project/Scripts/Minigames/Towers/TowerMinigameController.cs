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
    // ВАЖНО про правила башни (после нескольких переделок подряд — итоговая
    // версия): одна башня — это ОДНО решение, как один раунд в "Дверях", а
    // не цепочка из N последовательных решений. В башне сразу видно все
    // големы этажа; ровно maxBeatableAtOnce из них (по умолчанию 1) прямо
    // сейчас проходимы по силе героя, остальные — заведомо нет (это
    // ловушки, а не "пока нет"). Как только герой побеждает ВСЕХ проходимых
    // прямо сейчас големов, башня считается пройденной — оставшиеся ловушки
    // исчезают сами (ровно как непойманные двери исчезают при переходе к
    // следующему раунду в DoorMinigameController.SpawnRound), и герой идёт
    // к следующей башне. Это осознанный отказ от более ранней идеи "каждый
    // голем должен быть побеждён, если бить в правильном порядке" — та идея
    // ломалась математически: чтобы ловушка гарантированно стала проходимой
    // ВНУТРИ одной башни, сила героя должна расти взрывным образом (кратно
    // на каждом этаже), и уже на 4-этажной башне требуемый диапазон чисел
    // становится нереалистично большим. Текущая схема арифметически
    // надёжна при любом размере башни: ловушке нужно только оказаться в
    // (сила героя; 2×MaxValue] — окно фиксированного размера, не зависящее
    // от количества этажей.
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
        private readonly HashSet<GolemView> _requiredGolems = new HashSet<GolemView>(); // те, кого обязательно победить, чтобы пройти башню
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
            _requiredGolems.Clear();

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
            var slots = PickTargets(size, difficulty);

            // У slots ВСЕГДА ровно size элементов — в отличие от более
            // ранней версии, здесь башню больше никогда не укорачивают:
            // ловушке достаточно попасть в фиксированное по размеру окно
            // (сила героя; 2×MaxValue], а не "успеть" стать проходимой
            // через цепочку из нескольких этажей — такое окно почти всегда
            // можно заполнить нужным числом различных ловушек (см.
            // PickTargets), а в совсем крайних случаях PickDistinctTarget
            // допускает повтор числа, но не теряет этаж целиком.
            for (int i = 0; i < slots.Count; i++)
            {
                // baseHeight поднимает нижний этаж над головой героя — иначе
                // этаж 0 оказывается на одной высоте с героем, и подпись
                // силы героя накладывается на подпись первого примера.
                Vector3 pos = new Vector3(
                    towerX,
                    golemSlotParent.position.y + baseHeight + i * floorHeight,
                    golemSlotParent.position.z);

                var golem = Instantiate(golemPrefab, pos, Quaternion.identity, golemSlotParent);
                var problem = _context.ProblemGenerator.GenerateWithAnswer(difficulty, slots[i].Target);
                golem.Init(problem, OnGolemSelected);
                _activeGolems.Add(golem);
                if (slots[i].IsRequired) _requiredGolems.Add(golem);
            }

            Vector3 heroTarget = new Vector3(towerX - _heroToTowerDistance, hero.transform.position.y, hero.transform.position.z);
            hero.MoveTo(heroTarget, moveDuration, () => _inputLocked = false);
        }

        private readonly struct TowerSlot
        {
            public readonly int Target;
            public readonly bool IsRequired;

            public TowerSlot(int target, bool isRequired)
            {
                Target = target;
                IsRequired = isRequired;
            }
        }

        // Строит ответы для ВСЕХ големов этажа сразу (не по одному) — так
        // проще гарантировать, что все ответы разные. "Обязательные" (их
        // ровно maxBeatableAtOnce, минимум 1) получают ответ ≤ силы героя —
        // их нужно найти и победить, чтобы пройти башню. Остальные —
        // ловушки: ответ строго ВЫШЕ силы героя, но не выше того, что вообще
        // можно показать суммой двух чисел из диапазона (2×MaxValue) —
        // ловушку не нужно побеждать, кликать её нельзя ни при каких
        // условиях (клик = поражение). Как только все обязательные
        // побеждены, ловушки убираются сами (см. WinAgainstGolem) — точно
        // так же, как непойманные двери убираются при переходе к следующему
        // раунду в DoorMinigameController.
        private List<TowerSlot> PickTargets(int towerSize, DifficultyContext difficulty)
        {
            var slots = new List<TowerSlot>(towerSize);
            var usedTargets = new HashSet<int>(); // чтобы не было двух големов с одинаковым ответом на одном этаже (в том числе "зеркальных" вроде "1+2"/"2+1")

            int requiredCount = Mathf.Clamp(_definition.maxBeatableAtOnce, 1, towerSize);
            for (int i = 0; i < requiredCount; i++)
            {
                int t = PickDistinctTarget(usedTargets, Mathf.Min(0, _heroPower), Mathf.Max(0, _heroPower));
                usedTargets.Add(t);
                slots.Add(new TowerSlot(t, isRequired: true));
            }

            // Окно для ловушек — от (сила героя + 1) до максимума, который
            // вообще можно показать суммой двух чисел из диапазона. Ширина
            // этого окна НЕ зависит от размера башни (в отличие от более
            // ранней версии, где окно определялось тем, насколько герой
            // гарантированно вырастет за этот же этаж — и схлопывалось до
            // нуля, как только сила героя упиралась в свой предел). Предел
            // силы героя (см. HeroPowerCap) всегда меньше 2×MaxValue, так
            // что это окно никогда не бывает пустым целиком.
            int maxRepresentable = difficulty.MaxValue * 2;
            int trapLo = _heroPower + 1;
            int trapHi = maxRepresentable;
            int trapCount = towerSize - requiredCount;
            for (int i = 0; i < trapCount; i++)
            {
                int t = trapLo <= trapHi
                    ? PickDistinctTarget(usedTargets, trapLo, trapHi)
                    : trapHi; // экстремальные настройки (сила героя уже у самого предела диапазона) — берём максимум, какой есть, а не укорачиваем башню
                usedTargets.Add(t);
                slots.Add(new TowerSlot(t, isRequired: false));
            }

            Shuffle(slots); // иначе обязательные примеры всегда оказывались бы на одних и тех же этажах
            return slots;
        }

        private static void Shuffle(List<TowerSlot> list)
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
            // дважды на одном этаже — баг, который заметил игрок) был выше,
            // чем должен быть: 20 попыток почти всегда достаточно, но не
            // гарантированно.
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
        // держит силу героя заметно ниже 2×MaxValue — то есть всегда
        // оставляет ловушкам, где разместиться (см. PickTargets).
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
            _requiredGolems.Remove(golem);
            golem.PlayDefeatedByHero(() =>
            {
                _activeGolems.Remove(golem);
                _inputLocked = false;

                if (_requiredGolems.Count > 0) return; // ещё остались обязательные големы на этом этаже

                // Все обязательные побеждены — башня пройдена. Оставшиеся
                // ловушки больше не нужны: убираем сразу, а не оставляем
                // стоять (и кликабельными!), пока герой идёт к следующей
                // башне — иначе случайный клик по ловушке убьёт героя уже
                // ПОСЛЕ того, как башня по сути пройдена.
                foreach (var leftover in _activeGolems)
                    if (leftover != null) Destroy(leftover.gameObject);
                _activeGolems.Clear();

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
