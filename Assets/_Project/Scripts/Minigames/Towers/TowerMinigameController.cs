using System.Collections.Generic;
using UnityEngine;
using TMPro;
using MathGame.Data;
using MathGame.Minigames.Framework;

namespace MathGame.Minigames.Towers
{
    // Реализация мини-игры "Башни" (см. docs/ARCHITECTURE.md — "Как
    // добавить новый тип мини-игры" описывает именно этот класс как пример).
    // Держит свою силу героя и текущий список големов сам — с другими
    // мини-играми в этом же уровне не делится ничем, кроме общего контракта
    // MinigameController.
    public class TowerMinigameController : MinigameController
    {
        [SerializeField] private HeroView hero;
        [SerializeField] private GolemView golemPrefab;
        [SerializeField] private Transform golemSlotParent;
        [SerializeField] private TextMeshPro heroPowerLabel;
        [SerializeField] private float golemSpacing = 2.5f;
        [SerializeField] private float towerSpacing = 6f;
        [SerializeField] private float approachOffset = 1.3f;
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
            _inputLocked = false;

            foreach (var golem in _activeGolems)
                if (golem != null) Destroy(golem.gameObject);
            _activeGolems.Clear();

            int size = _definition.towerSizes[towerIndex];
            // Каждая следующая башня дальше по X — отсюда ощущение движения вперёд по локации.
            float towerStartX = hero.transform.position.x + towerSpacing * (towerIndex + 1);
            var difficulty = _context.BuildDifficulty();

            for (int i = 0; i < size; i++)
            {
                Vector3 pos = new Vector3(
                    towerStartX + i * golemSpacing,
                    golemSlotParent.position.y,
                    golemSlotParent.position.z);

                var golem = Instantiate(golemPrefab, pos, Quaternion.identity, golemSlotParent);
                var problem = _context.ProblemGenerator.Generate(difficulty);
                golem.Init(problem, OnGolemSelected);
                _activeGolems.Add(golem);
            }

            hero.MoveTo(ApproachPositionFor(_activeGolems[0]), moveDuration);
        }

        private void OnGolemSelected(GolemView golem)
        {
            if (_inputLocked || !_activeGolems.Contains(golem)) return;
            _inputLocked = true;

            hero.MoveTo(ApproachPositionFor(golem), moveDuration * 0.5f, () =>
            {
                if (golem.Answer <= _heroPower) WinAgainstGolem(golem);
                else LoseToGolem();
            });
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

        private Vector3 ApproachPositionFor(GolemView golem)
        {
            return golem.transform.position - new Vector3(approachOffset, 0f, 0f);
        }

        private void UpdateHeroLabel()
        {
            if (heroPowerLabel != null) heroPowerLabel.text = _heroPower.ToString();
        }
    }
}
