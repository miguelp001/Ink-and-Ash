using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using InkAndAsh.Combat;

namespace InkAndAsh.Tests.EditMode
{
    [TestFixture]
    public class CtbCombatEngineTests
    {
        private CtbCombatEngine _engine;
        private Combatant _vicar;
        private Combatant _weaver;
        private Combatant _beast;
        private Combatant _sentry;

        [SetUp]
        public void SetUp()
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
                NextTurnTime = 3.5f
            };

            _beast = new Combatant
            {
                Id = "enemy_beast",
                Name = "Ash Beast",
                IsAlly = false,
                BaseAgility = 60f,
                CurrentHealth = 150f,
                MaxHealth = 150f,
                FocusPoints = 0f,
                NextTurnTime = 1.5f
            };

            _sentry = new Combatant
            {
                Id = "enemy_sentry",
                Name = "Imperial Sentry",
                IsAlly = false,
                BaseAgility = 30f,
                CurrentHealth = 60f,
                MaxHealth = 60f,
                FocusPoints = 0f,
                NextTurnTime = 6.0f
            };
        }

        #region 1. Turn Delay Formula Tests

        [Test]
        public void TurnDelayFormula_StandardCalculation_ProducesExpectedDelay()
        {
            // BaseTurnDelay = (SkillCost / Agility) * WeatherMultiplier
            // 100 / 50 * 1.0 = 2.0
            float delayVicar = _engine.CalculateTurnDelay(_vicar, 100, 1.0f);
            Assert.That(delayVicar, Is.EqualTo(2.0f).Within(0.0001f));

            // 100 / 40 * 1.0 = 2.5
            float delayWeaver = _engine.CalculateTurnDelay(_weaver, 100, 1.0f);
            Assert.That(delayWeaver, Is.EqualTo(2.5f).Within(0.0001f));

            // Weather multiplier (Blizzard 1.25x): 100 / 50 * 1.25 = 2.5
            float delayBlizzard = _engine.CalculateTurnDelay(_vicar, 100, 1.25f);
            Assert.That(delayBlizzard, Is.EqualTo(2.5f).Within(0.0001f));
        }

        [Test]
        public void TurnDelayFormula_ZeroSkillCost_ProducesZeroDelay()
        {
            float delay = _engine.CalculateTurnDelay(_vicar, 0, 1.0f);
            Assert.That(delay, Is.EqualTo(0f));
        }

        [Test]
        public void TurnDelayFormula_ZeroOrNegativeAgility_SafelyClampedToOne()
        {
            var zeroAgiActor = new Combatant
            {
                Id = "zero_agi",
                Name = "Stunned Hero",
                BaseAgility = 0f
            };

            float delayZero = _engine.CalculateTurnDelay(zeroAgiActor, 100, 1.0f);
            Assert.That(float.IsNaN(delayZero), Is.False, "Delay must not be NaN.");
            Assert.That(float.IsInfinity(delayZero), Is.False, "Delay must not be infinite.");
            Assert.That(delayZero, Is.EqualTo(100.0f).Within(0.0001f));

            var negAgiActor = new Combatant
            {
                Id = "neg_agi",
                Name = "Cursed Hero",
                BaseAgility = -50f
            };

            float delayNeg = _engine.CalculateTurnDelay(negAgiActor, 100, 1.0f);
            Assert.That(delayNeg, Is.EqualTo(100.0f).Within(0.0001f));

            // Static overload verification
            float staticDelay = CtbCombatEngine.CalculateTurnDelay(0f, 100, 1.0f);
            Assert.That(staticDelay, Is.EqualTo(100.0f).Within(0.0001f));
        }

        [Test]
        public void TurnDelayFormula_NegativeOrZeroWeatherMultiplier_ClampedToMinimum()
        {
            float delayZeroWeather = _engine.CalculateTurnDelay(_vicar, 100, 0f);
            Assert.That(delayZeroWeather, Is.EqualTo(0.2f).Within(0.0001f)); // (100 / 50) * 0.1

            float delayNegWeather = _engine.CalculateTurnDelay(_vicar, 100, -2.5f);
            Assert.That(delayNegWeather, Is.EqualTo(0.2f).Within(0.0001f));
        }

        #endregion

        #region 2. Timeline Queue Ordering & Dynamic Recalculation

        [Test]
        public void TimelineQueue_OrdersAscendingNextTurnTime()
        {
            _engine.AddCombatant(_vicar);   // 2.0
            _engine.AddCombatant(_weaver);  // 3.5
            _engine.AddCombatant(_beast);   // 1.5
            _engine.AddCombatant(_sentry);  // 6.0

            Assert.That(_engine.Timeline.Count, Is.EqualTo(4));
            Assert.That(_engine.Timeline[0].Id, Is.EqualTo("enemy_beast"));  // 1.5
            Assert.That(_engine.Timeline[1].Id, Is.EqualTo("hero_vicar"));   // 2.0
            Assert.That(_engine.Timeline[2].Id, Is.EqualTo("hero_weaver"));  // 3.5
            Assert.That(_engine.Timeline[3].Id, Is.EqualTo("enemy_sentry")); // 6.0
        }

        [Test]
        public void TimelineQueue_TieBreaking_PrioritizesAllyOverEnemy()
        {
            var ally = new Combatant { Id = "ally_test", IsAlly = true, NextTurnTime = 4.0f, BaseAgility = 30f };
            var enemy = new Combatant { Id = "enemy_test", IsAlly = false, NextTurnTime = 4.0f, BaseAgility = 30f };

            _engine.AddCombatant(enemy);
            _engine.AddCombatant(ally);

            Assert.That(_engine.Timeline[0].Id, Is.EqualTo("ally_test"));
            Assert.That(_engine.Timeline[1].Id, Is.EqualTo("enemy_test"));
        }

        [Test]
        public void TimelineQueue_TieBreaking_HigherAgilityBeforeLower()
        {
            var swift = new Combatant { Id = "swift_hero", IsAlly = true, NextTurnTime = 5.0f, BaseAgility = 60f };
            var slow = new Combatant { Id = "slow_hero", IsAlly = true, NextTurnTime = 5.0f, BaseAgility = 30f };

            _engine.AddCombatant(slow);
            _engine.AddCombatant(swift);

            Assert.That(_engine.Timeline[0].Id, Is.EqualTo("swift_hero"));
            Assert.That(_engine.Timeline[1].Id, Is.EqualTo("slow_hero"));
        }

        [Test]
        public void ActionCommit_AdvancesActorNextTurnTimeAndReSortsQueue()
        {
            _engine.AddCombatant(_beast);  // 1.5
            _engine.AddCombatant(_vicar);  // 2.0
            _engine.AddCombatant(_weaver); // 3.5

            // Beast acts first at 1.5 with skill cost 90, Agility 60 -> delay 1.5 -> new time 3.0
            _engine.CommitAction(_beast, 90, 1.0f);

            Assert.That(_beast.NextTurnTime, Is.EqualTo(3.0f).Within(0.0001f));
            Assert.That(_engine.Timeline[0].Id, Is.EqualTo("hero_vicar"));  // Now Vicar is first (2.0)
            Assert.That(_engine.Timeline[1].Id, Is.EqualTo("enemy_beast")); // Beast is second (3.0)
            Assert.That(_engine.Timeline[2].Id, Is.EqualTo("hero_weaver")); // Weaver is third (3.5)
            Assert.That(_engine.CurrentTime, Is.EqualTo(2.0f).Within(0.0001f));
        }

        #endregion

        #region 3. Action Preview

        [Test]
        public void ActionPreview_ProjectsTimelineWithoutMutatingEngineState()
        {
            _engine.AddCombatant(_beast);  // 1.5
            _engine.AddCombatant(_vicar);  // 2.0
            _engine.AddCombatant(_weaver); // 3.5

            // Preview Beast taking a heavy skill (cost 180, Agility 60 -> delay 3.0 -> projected 4.5)
            var preview = _engine.PreviewAction(_beast, 180, 1.0f);

            Assert.That(preview.Count, Is.EqualTo(3));
            Assert.That(preview[0].Id, Is.EqualTo("hero_vicar"));  // 2.0
            Assert.That(preview[1].Id, Is.EqualTo("hero_weaver")); // 3.5
            Assert.That(preview[2].Id, Is.EqualTo("enemy_beast")); // 4.5

            // Verify actual engine state remained untouched
            Assert.That(_engine.Timeline[0].Id, Is.EqualTo("enemy_beast"));
            Assert.That(_beast.NextTurnTime, Is.EqualTo(1.5f));
            Assert.That(_engine.CurrentTime, Is.EqualTo(0f));
        }

        #endregion

        #region 4. Sync Move Detection

        [Test]
        public void SyncMove_ReturnsTrueWhenDeltaIsWithinTwoUnits()
        {
            _vicar.NextTurnTime = 4.0f;
            _weaver.NextTurnTime = 5.5f; // Delta = 1.5 <= 2.0
            Assert.That(_engine.CheckSyncMoveAvailable(_vicar, _weaver), Is.True);

            _weaver.NextTurnTime = 6.0f; // Delta = 2.0 <= 2.0 (Exact boundary)
            Assert.That(_engine.CheckSyncMoveAvailable(_vicar, _weaver), Is.True);
        }

        [Test]
        public void SyncMove_ReturnsFalseWhenDeltaExceedsTwoUnits()
        {
            _vicar.NextTurnTime = 4.0f;
            _weaver.NextTurnTime = 6.001f; // Delta = 2.001 > 2.0
            Assert.That(_engine.CheckSyncMoveAvailable(_vicar, _weaver), Is.False);

            _weaver.NextTurnTime = 10.0f; // Delta = 6.0 > 2.0
            Assert.That(_engine.CheckSyncMoveAvailable(_vicar, _weaver), Is.False);
        }

        [Test]
        public void SyncMove_RequiresAliveAllies()
        {
            _vicar.NextTurnTime = 4.0f;
            _beast.NextTurnTime = 4.5f;
            // Ally vs Enemy cannot perform Sync Move
            Assert.That(_engine.CheckSyncMoveAvailable(_vicar, _beast), Is.False);

            // Dead ally cannot perform Sync Move
            _weaver.NextTurnTime = 4.2f;
            _weaver.CurrentHealth = 0f; // Dead
            Assert.That(_engine.CheckSyncMoveAvailable(_vicar, _weaver), Is.False);
        }

        #endregion

        #region 5. Intervention Mechanic

        [Test]
        public void Intervention_Deducts25FPAndAdvancesTarget15Units()
        {
            _engine.AddCombatant(_vicar);
            _engine.AddCombatant(_weaver);

            _vicar.FocusPoints = 50f;
            _weaver.NextTurnTime = 25.0f;

            bool success = _engine.PerformIntervention(_vicar, _weaver);

            Assert.That(success, Is.True);
            Assert.That(_vicar.FocusPoints, Is.EqualTo(25f));
            Assert.That(_weaver.NextTurnTime, Is.EqualTo(10.0f).Within(0.0001f)); // 25 - 15 = 10
        }

        [Test]
        public void Intervention_ClampsToCurrentTime()
        {
            _engine.AddCombatant(_vicar);
            _engine.AddCombatant(_weaver);

            _engine.CommitAction(_vicar, 500, 1.0f); // Advance CurrentTime to 12.0
            float currentTime = _engine.CurrentTime;

            _weaver.NextTurnTime = currentTime + 5.0f; // 17.0
            _vicar.FocusPoints = 30f;

            bool success = _engine.PerformIntervention(_vicar, _weaver);

            Assert.That(success, Is.True);
            // 17 - 15 = 2.0, but CurrentTime is 12.0, so it must clamp to 12.0
            Assert.That(_weaver.NextTurnTime, Is.EqualTo(currentTime).Within(0.0001f));
        }

        [Test]
        public void Intervention_FailsWithInsufficientFP()
        {
            _vicar.FocusPoints = 24.99f;
            _weaver.NextTurnTime = 20.0f;

            bool success = _engine.PerformIntervention(_vicar, _weaver);

            Assert.That(success, Is.False);
            Assert.That(_vicar.FocusPoints, Is.EqualTo(24.99f), "FP must not be deducted on failed intervention.");
            Assert.That(_weaver.NextTurnTime, Is.EqualTo(20.0f), "Target timeline must not move.");
        }

        [Test]
        public void Intervention_FailsForDeadOrNonAlly()
        {
            _vicar.FocusPoints = 50f;
            _vicar.CurrentHealth = 0f;
            Assert.That(_engine.PerformIntervention(_vicar, _weaver), Is.False);

            _vicar.CurrentHealth = 100f;
            Assert.That(_engine.PerformIntervention(_vicar, _beast), Is.False, "Cannot intervene on an enemy.");
        }

        #endregion

        #region 6. Mediator's Anchor

        [Test]
        public void MediatorsAnchor_GuaranteesSyncMoveAtAnyTimelineDelta()
        {
            _vicar.NextTurnTime = 5.0f;
            _weaver.NextTurnTime = 55.0f; // Delta = 50.0 >> 2.0

            Assert.That(_engine.CheckSyncMoveAvailable(_vicar, _weaver), Is.False);

            _engine.SetMediatorsAnchor(_vicar, _weaver);

            Assert.That(_vicar.LinkedAllyId, Is.EqualTo(_weaver.Id));
            Assert.That(_weaver.LinkedAllyId, Is.EqualTo(_vicar.Id));
            Assert.That(_engine.CheckSyncMoveAvailable(_vicar, _weaver), Is.True,
                "Mediator's Anchor must guarantee Sync Move availability regardless of timeline gap.");
        }

        [Test]
        public void MediatorsAnchor_FailsWhenAnchoredAllyDies()
        {
            _engine.SetMediatorsAnchor(_vicar, _weaver);
            _weaver.CurrentHealth = 0f; // Dead

            Assert.That(_engine.CheckSyncMoveAvailable(_vicar, _weaver), Is.False,
                "Dead combatants cannot execute Sync Moves even when anchored.");
        }

        #endregion

        #region 7. Injury Debuffs & Campsite Clearing

        [Test]
        public void InjuryDebuff_AppliesTurnGenerationAndDelayPenalties()
        {
            // Normal delay: 100 / 50 * 1.0 = 2.0
            float normalDelay = _engine.CalculateTurnDelay(_vicar, 100, 1.0f);
            Assert.That(normalDelay, Is.EqualTo(2.0f).Within(0.0001f));

            // Apply Injury Debuff:
            // -20% turn generation: EffectiveAgility = 50 * 0.8 = 40
            // +15% delay cost: SkillCost = 100 * 1.15 = 115
            // Net delay: 115 / 40 = 2.875 (+43.75%)
            _engine.ApplyInjuryDebuff(_vicar);

            Assert.That(_vicar.HasInjuryDebuff, Is.True);
            Assert.That(_vicar.EffectiveAgility, Is.EqualTo(40f).Within(0.0001f));

            float injuredDelay = _engine.CalculateTurnDelay(_vicar, 100, 1.0f);
            Assert.That(injuredDelay, Is.EqualTo(2.875f).Within(0.0001f));
        }

        [Test]
        public void InjuryDebuff_ClearingRestoresNormalCombatPerformance()
        {
            _engine.ApplyInjuryDebuff(_vicar);
            Assert.That(_vicar.HasInjuryDebuff, Is.True);

            _engine.ClearInjuryDebuff(_vicar);

            Assert.That(_vicar.HasInjuryDebuff, Is.False);
            Assert.That(_vicar.EffectiveAgility, Is.EqualTo(50f).Within(0.0001f));
            Assert.That(_engine.CalculateTurnDelay(_vicar, 100, 1.0f), Is.EqualTo(2.0f).Within(0.0001f));
        }

        [Test]
        public void CampsiteClearing_RestWorkflowClearsInjuriesAcrossParty()
        {
            var party = new List<Combatant> { _vicar, _weaver };

            // Simulate post-combat defeat/injuries
            foreach (var hero in party)
            {
                _engine.ApplyInjuryDebuff(hero);
                Assert.That(hero.HasInjuryDebuff, Is.True);
            }

            // Party rests at campsite with hot cooked meal
            foreach (var hero in party)
            {
                _engine.ClearInjuryDebuff(hero);
            }

            foreach (var hero in party)
            {
                Assert.That(hero.HasInjuryDebuff, Is.False, $"Hero '{hero.Name}' should have debuff cleared after rest.");
                Assert.That(hero.EffectiveAgility, Is.EqualTo(hero.BaseAgility));
            }
        }

        #endregion

        #region 8. Queue Management

        [Test]
        public void RemoveCombatant_RemovesTargetFromTimeline()
        {
            _engine.AddCombatant(_vicar);
            _engine.AddCombatant(_weaver);
            _engine.AddCombatant(_beast);

            Assert.That(_engine.Timeline.Count, Is.EqualTo(3));

            _engine.RemoveCombatant("enemy_beast");

            Assert.That(_engine.Timeline.Count, Is.EqualTo(2));
            Assert.That(_engine.Timeline.Any(c => c.Id == "enemy_beast"), Is.False);
        }

        [Test]
        public void Clear_ResetsEngineTimelineAndClock()
        {
            _engine.AddCombatant(_vicar);
            _engine.CommitAction(_vicar, 100, 1.0f);

            Assert.That(_engine.Timeline.Count, Is.GreaterThan(0));
            Assert.That(_engine.CurrentTime, Is.GreaterThan(0f));

            _engine.Clear();

            Assert.That(_engine.Timeline.Count, Is.EqualTo(0));
            Assert.That(_engine.CurrentTime, Is.EqualTo(0f));
        }

        #endregion
    }
}
