using System.Collections.Generic;
using UnityEngine;
using TMPro;
using MathGame.Data;
using MathGame.Minigames.Framework;

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
        [SerializeField] private float floorHeight = 1.2f;
        [SerializeField] private float baseHeight = 1.5f;
        [SerializeField] private float towerSpacing = 5f;
        [SerializeField] private float approachOffset = 1.5f;
        [SerializeField] private float moveDuration = 0.6f;

        private TowerMinigameDefinition _definition;
        private MinigameRuntimeContext _context;
        private int _heroPower;
        private int _towerIndex;
        private readonly List<GolemView> _activeGolems = new List<GolemView>();
        private bool _inputLocked;

        public override void Begin(MinigameDefinition definition, MinigameRuntimeContext context)
        {
            _definition = (TowerMinigameDefinition)definition;
            _context = context;
            _heroPower = _definition.heroStartingPower;
            UpdateHeroLabel();
            SpawnTower(0);
        }

        private void SpawnTower(int towerIndex)
        {
            _towerIndex = towerIndex;
            _inputLocked = true; // ждём, пока герой дойдёт до подножия новой башни

            foreach (var golem in _activeGolems)
                if (golem != null) Destroy(golem.gameObject);
            _activeGolems.Clear();

            int size = _definition.towerSizes[towerIndex];
            // Каждая следующая башня дальше по X — отсюда ощущение движения вперёд по локации.
            float towerX = hero.transform.position.x + towerSpacing * (towerIndex + 1);
            var difficulty = _context.BuildDifficulty();
            var beatableSlots = PickBeatableSlots(size);

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
                    ? GenerateProblemRelativeToPower(difficulty, beatable: true)
                    : GenerateProblemRelativeToPower(difficulty, beatable: false);
                golem.Init(problem, OnGolemSelected);
                _activeGolems.Add(golem);
            }

            Vector3 heroTarget = new Vector3(towerX - approachOffset, hero.transform.position.y, hero.transform.position.z);
            hero.MoveTo(heroTarget, moveDuration, () => _inputLocked = false);
        }

        // Выбирает, какие места в башне получат "проходимый" (по силе
        // герою) пример — случайно, чтобы расположение не угадывалось
        // заранее, и не больше definition.maxBeatableAtOnce штук сразу.
        private HashSet<int> PickBeatableSlots(int towerSize)
        {
            int budget = Mathf.Clamp(_definition.maxBeatableAtOnce, 0, towerSize);
            var slots = new HashSet<int>();
            while (slots.Count < budget)
                slots.Add(Random.Range(0, towerSize));
            return slots;
        }

        // Единой ProblemGenerator недостаточно — ему ничего не известно про
        // текущую силу героя, а тут нужно подбирать примеры, которые
        // заведомо игрок либо может, либо пока не может победить. Идём
        // "перебором": генерируем обычные примеры, пока не найдём такой,
        // что подходит; если диапазон чисел в настройках совсем не
        // позволяет найти нужный вариант (например, диапазон настолько
        // мал, что все примеры меньше силы героя) — отдаём последний
        // сгенерированный, ошибки не будет, просто в этот раз ограничение
        // не выдержится идеально.
        private MathProblem GenerateProblemRelativeToPower(DifficultyContext difficulty, bool beatable)
        {
            MathProblem problem = default;
            for (int attempt = 0; attempt < 20; attempt++)
            {
                problem = _context.ProblemGenerator.Generate(difficulty);
                bool isBeatable = problem.Answer <= _heroPower;
                if (isBeatable == beatable) return problem;
            }
            return problem;
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
            if (_definition.powerCap > 0) _heroPower = Mathf.Min(_heroPower, _definition.powerCap);
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
