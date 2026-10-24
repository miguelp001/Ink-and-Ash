using System;
using System.Collections.Generic;

namespace InkAndAsh.Combat
{
    public class CtbCombatEngine
    {
        private readonly TimelineQueue _queue = new TimelineQueue();

        public float CurrentTime { get; private set; }
        public IReadOnlyList<Combatant> Timeline => _queue.AsReadOnly();

        public void AddCombatant(Combatant combatant)
        {
            if (combatant == null) return;
            _queue.Add(combatant);
        }

        public void RemoveCombatant(string id)
        {
            _queue.Remove(id);
        }

        public void Clear()
        {
            _queue.Clear();
            CurrentTime = 0f;
        }

        public static float CalculateTurnDelay(float agility, int skillCost, float weatherMultiplier = 1.0f)
        {
            float effectiveAgility = Math.Max(1.0f, agility);
            float safeWeather = Math.Max(0.1f, weatherMultiplier);
            return (skillCost / effectiveAgility) * safeWeather;
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
            _queue.Sort();

            if (_queue.Count > 0)
            {
                CurrentTime = _queue.Peek().NextTurnTime;
            }
        }

        public List<Combatant> PreviewAction(Combatant actor, int skillCost, float weatherMultiplier = 1.0f)
        {
            if (actor == null) return _queue.CloneList();

            float actionTime = Math.Max(CurrentTime, actor.NextTurnTime);
            float delay = CalculateTurnDelay(actor, skillCost, weatherMultiplier);
            float projectedTime = actionTime + delay;

            var copy = _queue.CloneList();
            var projectedActor = copy.Find(c => c.Id == actor.Id);
            if (projectedActor != null)
            {
                projectedActor.NextTurnTime = projectedTime;
            }

            copy.Sort(TimelineQueue.CompareCombatants);
            return copy;
        }

        public bool CheckSyncMoveAvailable(Combatant allyA, Combatant allyB)
        {
            if (allyA == null || allyB == null) return false;
            if (!allyA.IsAlly || !allyB.IsAlly) return false;
            if (!allyA.IsAlive || !allyB.IsAlive) return false;

            // Mediator's Anchor override
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
            _queue.Sort();
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
    }
}

#if STANDALONE_TEST
namespace InkAndAsh.StandaloneTests
{
    using NUnit.Framework;
    using InkAndAsh.Combat;

    [TestFixture]
    public class CtbCombatEngineProductionTests
    {
        private CtbCombatEngine _engine;
        private Combatant _vicar;
        private Combatant _weaver;
        private Combatant _beast;

        [SetUp]
        public void Setup()
        {
            _engine = new CtbCombatEngine();

            _vicar = new Combatant
            {
                Id = "hero_vicar",
                Name = "Vicar",
                IsAlly = true,
                BaseAgility = 50f,
                CurrentHealth = 100f,
                MaxHealth = 100f,
                FocusPoints = 50f,
                NextTurnTime = 2.0f
            };

            _weaver = new Combatant
            {
                Id = "hero_weaver",
                Name = "Weaver",
                IsAlly = true,
                BaseAgility = 40f,
                CurrentHealth = 80f,
                MaxHealth = 80f,
                FocusPoints = 30f,
                NextTurnTime = 2.5f
            };

            _beast = new Combatant
            {
                Id = "enemy_beast",
                Name = "Ash Beast",
                IsAlly = false,
                BaseAgility = 60f,
                CurrentHealth = 120f,
                MaxHealth = 120f,
                FocusPoints = 0f,
                NextTurnTime = 1.67f
            };

            _engine.AddCombatant(_vicar);
            _engine.AddCombatant(_weaver);
            _engine.AddCombatant(_beast);
        }

        [Test]
        public void Ctb_TurnDelayFormula_StandardInputs()
        {
            // Delay = (SkillCost / Agility) * Weather
            float delay1 = _engine.CalculateTurnDelay(_vicar, 100, 1.0f);
            Assert.That(delay1, Is.EqualTo(2.0f)); // 100 / 50 = 2.0

            float delay2 = _engine.CalculateTurnDelay(_weaver, 100, 1.0f);
            Assert.That(delay2, Is.EqualTo(2.5f)); // 100 / 40 = 2.5

            float blizzardDelay = _engine.CalculateTurnDelay(_vicar, 100, 1.25f);
            Assert.That(blizzardDelay, Is.EqualTo(2.5f)); // 2.0 * 1.25 = 2.5
        }

        [Test]
        public void Ctb_TimelineQueue_DeterministicAscendingSort()
        {
            Assert.That(_engine.Timeline.Count, Is.EqualTo(3));
            Assert.That(_engine.Timeline[0].Id, Is.EqualTo("enemy_beast"));
            Assert.That(_engine.Timeline[1].Id, Is.EqualTo("hero_vicar"));
            Assert.That(_engine.Timeline[2].Id, Is.EqualTo("hero_weaver"));
        }

        [Test]
        public void Ctb_SyncMove_DeltaWithinThreshold()
        {
            // Vicar (2.0) and Weaver (2.5) delta = 0.5 <= 2.0
            bool sync = _engine.CheckSyncMoveAvailable(_vicar, _weaver);
            Assert.That(sync, Is.True);

            // Shift Weaver far away
            _weaver.NextTurnTime = 10.0f;
            Assert.That(_engine.CheckSyncMoveAvailable(_vicar, _weaver), Is.False);

            // Mediator's Anchor guarantees sync move regardless of delta
            _engine.SetMediatorsAnchor(_vicar, _weaver);
            Assert.That(_engine.CheckSyncMoveAvailable(_vicar, _weaver), Is.True);
        }

        [Test]
        public void Ctb_Intervention_Spends25FP_AdvancesTarget()
        {
            _weaver.NextTurnTime = 20.0f;
            bool ok = _engine.PerformIntervention(_vicar, _weaver);

            Assert.That(ok, Is.True);
            Assert.That(_vicar.FocusPoints, Is.EqualTo(25.0f));
            Assert.That(_weaver.NextTurnTime, Is.EqualTo(5.0f)); // 20 - 15 = 5.0
        }

        [Test]
        public void Ctb_InjuryDebuff_AppliesAgilityAndCostPenalties()
        {
            _engine.ApplyInjuryDebuff(_vicar);

            Assert.That(_vicar.EffectiveAgility, Is.EqualTo(40.0f)); // 50 * 0.8 = 40
            float delay = _engine.CalculateTurnDelay(_vicar, 100, 1.0f);
            // Effective cost = 115, effective agility = 40 -> 115 / 40 = 2.875 (+43.75% over 2.0)
            Assert.That(delay, Is.EqualTo(2.875f));

            _engine.ClearInjuryDebuff(_vicar);
            Assert.That(_engine.CalculateTurnDelay(_vicar, 100, 1.0f), Is.EqualTo(2.0f));
        }

        [Test]
        public void Ctb_PreviewAction_DoesNotMutateActiveTimeline()
        {
            var preview = _engine.PreviewAction(_vicar, 200, 1.0f);

            Assert.That(_vicar.NextTurnTime, Is.EqualTo(2.0f), "Preview must not mutate active actor turn time.");
            Assert.That(preview.Find(c => c.Id == _vicar.Id)!.NextTurnTime, Is.GreaterThan(2.0f));
        }
    }
}
#endif
