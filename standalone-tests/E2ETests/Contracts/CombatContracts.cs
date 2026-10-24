#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace InkAndAsh.StandaloneTests.E2ETests.Contracts
{
    public class Combatant
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsAlly { get; set; }
        public float BaseAgility { get; set; } = 50f;
        public float CurrentHealth { get; set; } = 100f;
        public float MaxHealth { get; set; } = 100f;
        public float FocusPoints { get; set; } = 50f;
        public float NextTurnTime { get; set; } = 0f;
        public bool HasInjuryDebuff { get; set; }
        public string? LinkedAllyId { get; set; }

        public bool IsAlive => CurrentHealth > 0f;
        public float EffectiveAgility => HasInjuryDebuff ? Math.Max(1.0f, BaseAgility * 0.8f) : Math.Max(1.0f, BaseAgility);
    }

    public class CtbCombatEngine
    {
        private readonly List<Combatant> _timeline = new();

        public float CurrentTime { get; private set; }
        public IReadOnlyList<Combatant> Timeline => _timeline;

        public void AddCombatant(Combatant combatant)
        {
            if (combatant == null) return;
            _timeline.Add(combatant);
            SortTimeline();
        }

        public void RemoveCombatant(string id)
        {
            _timeline.RemoveAll(c => c.Id == id);
        }

        public float CalculateTurnDelay(Combatant actor, int skillCost, float weatherMultiplier = 1.0f)
        {
            if (actor == null) return 0f;

            float effectiveSkillCost = actor.HasInjuryDebuff ? skillCost * 1.15f : skillCost;
            float effectiveAgility = Math.Max(1.0f, actor.EffectiveAgility);
            float safeWeather = Math.Max(0.1f, weatherMultiplier);

            return (effectiveSkillCost / effectiveAgility) * safeWeather;
        }

        public void CommitAction(Combatant actor, int skillCost, float weatherMultiplier = 1.0f)
        {
            if (actor == null) return;

            CurrentTime = Math.Max(CurrentTime, actor.NextTurnTime);
            float delay = CalculateTurnDelay(actor, skillCost, weatherMultiplier);
            actor.NextTurnTime = CurrentTime + delay;
            SortTimeline();

            if (_timeline.Count > 0)
            {
                CurrentTime = _timeline[0].NextTurnTime;
            }
        }

        public List<Combatant> PreviewAction(Combatant actor, int skillCost, float weatherMultiplier = 1.0f)
        {
            if (actor == null) return new List<Combatant>(_timeline);

            float actionTime = Math.Max(CurrentTime, actor.NextTurnTime);
            float delay = CalculateTurnDelay(actor, skillCost, weatherMultiplier);
            float projectedTime = actionTime + delay;

            var copy = _timeline.Select(c => new Combatant
            {
                Id = c.Id,
                Name = c.Name,
                IsAlly = c.IsAlly,
                BaseAgility = c.BaseAgility,
                CurrentHealth = c.CurrentHealth,
                MaxHealth = c.MaxHealth,
                FocusPoints = c.FocusPoints,
                NextTurnTime = c.Id == actor.Id ? projectedTime : c.NextTurnTime,
                HasInjuryDebuff = c.HasInjuryDebuff,
                LinkedAllyId = c.LinkedAllyId
            }).ToList();

            copy.Sort(CompareCombatants);
            return copy;
        }

        public bool CheckSyncMoveAvailable(Combatant allyA, Combatant allyB)
        {
            if (allyA == null || allyB == null) return false;
            if (!allyA.IsAlly || !allyB.IsAlly) return false;
            if (!allyA.IsAlive || !allyB.IsAlive) return false;

            if (!string.IsNullOrEmpty(allyA.LinkedAllyId) && allyA.LinkedAllyId == allyB.Id)
            {
                return true;
            }

            return Math.Abs(allyA.NextTurnTime - allyB.NextTurnTime) <= 2.0f;
        }

        public bool PerformIntervention(Combatant protector, Combatant targetAlly)
        {
            if (protector == null || targetAlly == null) return false;
            if (!protector.IsAlly || !targetAlly.IsAlly) return false;
            if (!protector.IsAlive || !targetAlly.IsAlive) return false;
            if (protector.FocusPoints < 25f) return false;

            protector.FocusPoints -= 25f;
            targetAlly.NextTurnTime = Math.Max(CurrentTime, targetAlly.NextTurnTime - 15.0f);
            SortTimeline();
            return true;
        }

        public void SetMediatorsAnchor(Combatant allyA, Combatant allyB)
        {
            if (allyA == null || allyB == null) return;
            allyA.LinkedAllyId = allyB.Id;
            allyB.LinkedAllyId = allyA.Id;
        }

        public void ApplyInjuryDebuff(Combatant combatant)
        {
            if (combatant == null) return;
            combatant.HasInjuryDebuff = true;
        }

        public void ClearInjuryDebuff(Combatant combatant)
        {
            if (combatant == null) return;
            combatant.HasInjuryDebuff = false;
        }

        private void SortTimeline()
        {
            _timeline.Sort(CompareCombatants);
        }

        private static int CompareCombatants(Combatant a, Combatant b)
        {
            int timeComparison = a.NextTurnTime.CompareTo(b.NextTurnTime);
            if (timeComparison != 0) return timeComparison;

            if (a.IsAlly != b.IsAlly)
            {
                return a.IsAlly ? -1 : 1;
            }

            int agiComparison = b.EffectiveAgility.CompareTo(a.EffectiveAgility);
            if (agiComparison != 0) return agiComparison;

            return string.Compare(a.Id, b.Id, StringComparison.Ordinal);
        }
    }
}
