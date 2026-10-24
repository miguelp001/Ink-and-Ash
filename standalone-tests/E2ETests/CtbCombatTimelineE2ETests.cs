#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using InkAndAsh.StandaloneTests.E2ETests.Contracts;
using NUnit.Framework;

namespace InkAndAsh.StandaloneTests.E2ETests
{
    [TestFixture]
    public class CtbCombatTimelineE2ETests
    {
        private CtbCombatEngine _engine = null!;
        private Combatant _vicar = null!;
        private Combatant _weaver = null!;
        private Combatant _sentinel = null!;
        private Combatant _hostileBeast = null!;

        [SetUp]
        public void Setup()
        {
            _engine = new CtbCombatEngine();

            _vicar = new Combatant
            {
                Id = "hero_vicar",
                Name = "Vicar of the Order",
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
                Name = "Vesper the Weaver",
                IsAlly = true,
                BaseAgility = 40f,
                CurrentHealth = 80f,
                MaxHealth = 80f,
                FocusPoints = 30f,
                NextTurnTime = 2.5f
            };

            _sentinel = new Combatant
            {
                Id = "hero_sentinel",
                Name = "Caravan Guard",
                IsAlly = true,
                BaseAgility = 30f,
                CurrentHealth = 120f,
                MaxHealth = 120f,
                FocusPoints = 40f,
                NextTurnTime = 3.33f
            };

            _hostileBeast = new Combatant
            {
                Id = "enemy_ash_hound",
                Name = "Ash-Coated Stalker",
                IsAlly = false,
                BaseAgility = 60f,
                CurrentHealth = 90f,
                MaxHealth = 90f,
                FocusPoints = 0f,
                NextTurnTime = 1.67f
            };
        }

        #region Tier 1: Feature Coverage (Baseline Happy Path)

        [Test]
        public void F2_T1_01_TurnDelayFormulaStandard()
        {
            // BaseTurnDelay = (SkillCost / Agility) * WeatherMultiplier
            // 100 / 50 * 1.0 = 2.0
            float delay = _engine.CalculateTurnDelay(_vicar, 100, 1.0f);
            Assert.That(delay, Is.EqualTo(2.0f).Within(0.0001f));
        }

        [Test]
        public void F2_T1_02_TurnDelayWithHighAgility()
        {
            var swiftRogue = new Combatant { BaseAgility = 100f };
            // 100 / 100 * 1.0 = 1.0
            float delay = _engine.CalculateTurnDelay(swiftRogue, 100, 1.0f);
            Assert.That(delay, Is.EqualTo(1.0f).Within(0.0001f));
        }

        [Test]
        public void F2_T1_03_TurnDelayWithLowAgility()
        {
            var heavyKnight = new Combatant { BaseAgility = 25f };
            // 100 / 25 * 1.0 = 4.0
            float delay = _engine.CalculateTurnDelay(heavyKnight, 100, 1.0f);
            Assert.That(delay, Is.EqualTo(4.0f).Within(0.0001f));
        }

        [Test]
        public void F2_T1_04_TurnDelayInSevereWeather()
        {
            // Mountain Blizzard: 1.25x combat delay
            // (100 / 50) * 1.25 = 2.5
            float delay = _engine.CalculateTurnDelay(_vicar, 100, 1.25f);
            Assert.That(delay, Is.EqualTo(2.5f).Within(0.0001f));
        }

        [Test]
        public void F2_T1_05_TimelineOrderingAscending()
        {
            _engine.AddCombatant(_vicar);       // 2.0
            _engine.AddCombatant(_weaver);      // 2.5
            _engine.AddCombatant(_sentinel);    // 3.33
            _engine.AddCombatant(_hostileBeast);// 1.67

            Assert.That(_engine.Timeline.Count, Is.EqualTo(4));
            Assert.That(_engine.Timeline[0].Id, Is.EqualTo("enemy_ash_hound"));
            Assert.That(_engine.Timeline[1].Id, Is.EqualTo("hero_vicar"));
            Assert.That(_engine.Timeline[2].Id, Is.EqualTo("hero_weaver"));
            Assert.That(_engine.Timeline[3].Id, Is.EqualTo("hero_sentinel"));
        }

        [Test]
        public void F2_T1_06_ActionCommitAdvancesTimeAndResorts()
        {
            _engine.AddCombatant(_hostileBeast); // 1.67
            _engine.AddCombatant(_vicar);        // 2.0
            _engine.AddCombatant(_weaver);       // 2.5

            // Hostile acts with cost 100: delay = 100 / 60 = 1.6667 => new NextTurnTime = 1.67 + 1.6667 = 3.3367
            _engine.CommitAction(_hostileBeast, 100, 1.0f);

            // Timeline queue is re-sorted: Vicar (2.0) should now be at the head
            Assert.That(_engine.Timeline[0].Id, Is.EqualTo("hero_vicar"));
            Assert.That(_engine.CurrentTime, Is.EqualTo(2.0f).Within(0.01f));
            Assert.That(_hostileBeast.NextTurnTime, Is.GreaterThan(3.3f));
        }

        [Test]
        public void F2_T1_07_SyncMoveDetectedWithinDelta2()
        {
            _vicar.NextTurnTime = 2.0f;
            _weaver.NextTurnTime = 3.5f; // Delta = 1.5 <= 2.0

            bool canSync = _engine.CheckSyncMoveAvailable(_vicar, _weaver);
            Assert.That(canSync, Is.True, "Sync move should be available when Delta <= 2.0.");
        }

        [Test]
        public void F2_T1_08_InterventionDeducts25FPAndAdvances15Units()
        {
            _engine.AddCombatant(_vicar);
            _engine.AddCombatant(_weaver);

            _vicar.FocusPoints = 50f;
            _weaver.NextTurnTime = 20.0f;

            bool success = _engine.PerformIntervention(_vicar, _weaver);

            Assert.That(success, Is.True);
            Assert.That(_vicar.FocusPoints, Is.EqualTo(25.0f));
            Assert.That(_weaver.NextTurnTime, Is.EqualTo(5.0f).Within(0.0001f)); // 20 - 15 = 5.0
        }

        [Test]
        public void F2_T1_09_MediatorsAnchorGuaranteesSyncMove()
        {
            _vicar.NextTurnTime = 2.0f;
            _weaver.NextTurnTime = 30.0f; // Delta = 28.0 >> 2.0

            Assert.That(_engine.CheckSyncMoveAvailable(_vicar, _weaver), Is.False);

            // Establish Mediator's Anchor link
            _engine.SetMediatorsAnchor(_vicar, _weaver);

            Assert.That(_vicar.LinkedAllyId, Is.EqualTo(_weaver.Id));
            Assert.That(_weaver.LinkedAllyId, Is.EqualTo(_vicar.Id));
            Assert.That(_engine.CheckSyncMoveAvailable(_vicar, _weaver), Is.True,
                "Mediator's Anchor must guarantee Sync Move regardless of timeline delta.");
        }

        [Test]
        public void F2_T1_10_InjuryDebuffIncreasesDelayCost()
        {
            // Normal delay: 100 / 50 = 2.0
            float normalDelay = _engine.CalculateTurnDelay(_vicar, 100, 1.0f);
            Assert.That(normalDelay, Is.EqualTo(2.0f).Within(0.0001f));

            // Apply Injury Debuff: Agility * 0.8 = 40, SkillCost * 1.15 = 115
            _engine.ApplyInjuryDebuff(_vicar);
            Assert.That(_vicar.HasInjuryDebuff, Is.True);
            Assert.That(_vicar.EffectiveAgility, Is.EqualTo(40.0f).Within(0.0001f));

            // Injured delay: 115 / 40 = 2.875
            float injuredDelay = _engine.CalculateTurnDelay(_vicar, 100, 1.0f);
            Assert.That(injuredDelay, Is.EqualTo(2.875f).Within(0.0001f));

            // Ratio: 2.875 / 2.0 = 1.4375 (+43.75% increase)
            Assert.That(injuredDelay / normalDelay, Is.EqualTo(1.4375f).Within(0.0001f));

            // Clear injury debuff
            _engine.ClearInjuryDebuff(_vicar);
            Assert.That(_vicar.HasInjuryDebuff, Is.False);
            Assert.That(_engine.CalculateTurnDelay(_vicar, 100, 1.0f), Is.EqualTo(2.0f).Within(0.0001f));
        }

        #endregion

        #region Tier 2: Boundary & Corner Cases

        [Test]
        public void F2_T2_01_ZeroAgilityClampedToMinimumOne()
        {
            var zeroAgilityUnit = new Combatant { BaseAgility = 0f };
            // Should clamp to 1.0, not throw DivideByZeroException
            float delay = _engine.CalculateTurnDelay(zeroAgilityUnit, 100, 1.0f);
            Assert.That(delay, Is.EqualTo(100.0f).Within(0.0001f));
        }

        [Test]
        public void F2_T2_02_FreeActionSkillCostZero()
        {
            // Free skill: 0 / 50 = 0 delay
            float delay = _engine.CalculateTurnDelay(_vicar, 0, 1.0f);
            Assert.That(delay, Is.EqualTo(0f));
        }

        [Test]
        public void F2_T2_03_SyncMoveAtExactDelta2000Threshold()
        {
            _vicar.NextTurnTime = 10.0f;

            // Exactly 2.0 delta
            _weaver.NextTurnTime = 12.0f;
            Assert.That(_engine.CheckSyncMoveAvailable(_vicar, _weaver), Is.True);

            // 2.001 delta
            _weaver.NextTurnTime = 12.001f;
            Assert.That(_engine.CheckSyncMoveAvailable(_vicar, _weaver), Is.False);
        }

        [Test]
        public void F2_T2_04_SyncMoveWithIncapacitatedAllyRejected()
        {
            _vicar.NextTurnTime = 10.0f;
            _weaver.NextTurnTime = 10.5f;
            _weaver.CurrentHealth = 0f; // Incapacitated / knocked out

            Assert.That(_weaver.IsAlive, Is.False);
            Assert.That(_engine.CheckSyncMoveAvailable(_vicar, _weaver), Is.False,
                "Incapacitated ally cannot execute Sync Move.");
        }

        [Test]
        public void F2_T2_05_InterventionFailsWith24_99FP()
        {
            _vicar.FocusPoints = 24.99f;
            _weaver.NextTurnTime = 20.0f;

            bool success = _engine.PerformIntervention(_vicar, _weaver);
            Assert.That(success, Is.False);
            Assert.That(_vicar.FocusPoints, Is.EqualTo(24.99f), "FP must not be deducted on failed intervention.");
            Assert.That(_weaver.NextTurnTime, Is.EqualTo(20.0f));
        }

        [Test]
        public void F2_T2_06_InterventionTimelineClampToCurrentTime()
        {
            _vicar.FocusPoints = 50f;
            _weaver.NextTurnTime = 5.0f; // 5 - 15 = -10, which is in the past!

            _engine.CommitAction(_vicar, 100); // Advances current time
            float currentTime = _engine.CurrentTime;

            bool success = _engine.PerformIntervention(_vicar, _weaver);
            Assert.That(success, Is.True);
            Assert.That(_weaver.NextTurnTime, Is.GreaterThanOrEqualTo(currentTime),
                "Timeline advance must not position an actor before CurrentTime.");
        }

        [Test]
        public void F2_T2_07_TieBreakingAlliesBeforeEnemies()
        {
            _vicar.NextTurnTime = 5.0f;
            _hostileBeast.NextTurnTime = 5.0f;

            _engine.AddCombatant(_hostileBeast);
            _engine.AddCombatant(_vicar);

            // Equal time: Ally must be prioritized over Enemy
            Assert.That(_engine.Timeline[0].Id, Is.EqualTo("hero_vicar"));
            Assert.That(_engine.Timeline[1].Id, Is.EqualTo("enemy_ash_hound"));
        }

        [Test]
        public void F2_T2_08_TieBreakingHigherAgilityFirst()
        {
            _vicar.NextTurnTime = 10.0f;  // Agility 50
            _weaver.NextTurnTime = 10.0f; // Agility 40

            _engine.AddCombatant(_weaver);
            _engine.AddCombatant(_vicar);

            // Equal time and both allies: Higher agility acts first
            Assert.That(_engine.Timeline[0].Id, Is.EqualTo("hero_vicar"));
            Assert.That(_engine.Timeline[1].Id, Is.EqualTo("hero_weaver"));
        }

        #endregion

        #region Tier 3: Cross-Feature Combinations

        [Test]
        public void F2_T3_01_CombatDelayUnderBlizzardWeatherMultiplier()
        {
            float clearDelay = _engine.CalculateTurnDelay(_vicar, 100, 1.0f);
            float blizzardDelay = _engine.CalculateTurnDelay(_vicar, 100, 1.25f);

            Assert.That(blizzardDelay, Is.EqualTo(clearDelay * 1.25f).Within(0.0001f));
        }

        [Test]
        public void F2_T3_02_DynamicTimelinePreviewMatchesCommittedActionOrder()
        {
            _engine.AddCombatant(_hostileBeast); // 1.67
            _engine.AddCombatant(_vicar);        // 2.0
            _engine.AddCombatant(_weaver);       // 2.5

            // Preview Vicar committing a heavy skill (cost 200 => delay 4.0 => next turn 6.0)
            var preview = _engine.PreviewAction(_vicar, 200, 1.0f);

            // Verify preview predicts correct order
            Assert.That(preview[0].Id, Is.EqualTo("enemy_ash_hound"));
            Assert.That(preview[1].Id, Is.EqualTo("hero_weaver"));
            Assert.That(preview[2].Id, Is.EqualTo("hero_vicar"));

            // Verify active timeline was NOT mutated by preview
            Assert.That(_engine.Timeline[0].Id, Is.EqualTo("enemy_ash_hound"));
            Assert.That(_engine.Timeline[1].Id, Is.EqualTo("hero_vicar"));
        }

        [Test]
        public void F2_T3_03_MediatorsAnchorPreservesSyncMoveUnderInjuryDebuff()
        {
            _vicar.NextTurnTime = 2.0f;
            _weaver.NextTurnTime = 15.0f;

            _engine.SetMediatorsAnchor(_vicar, _weaver);
            _engine.ApplyInjuryDebuff(_vicar);
            _engine.ApplyInjuryDebuff(_weaver);

            // Even with injury debuffs and large delta, anchor guarantees sync move
            Assert.That(_engine.CheckSyncMoveAvailable(_vicar, _weaver), Is.True);
        }

        #endregion

        #region Tier 4: Real-World Scenarios

        [Test]
        public void F2_T4_01_MultiRoundTacticalEncounter()
        {
            // Initial combat setup
            _engine.AddCombatant(_vicar);        // Next: 2.0
            _engine.AddCombatant(_weaver);       // Next: 2.5
            _engine.AddCombatant(_hostileBeast); // Next: 1.67

            // Round 1: Hostile acts at 1.67
            Assert.That(_engine.Timeline[0].Id, Is.EqualTo("enemy_ash_hound"));
            _engine.CommitAction(_hostileBeast, 100, 1.0f); // New time = 1.67 + 1.67 = 3.34

            // Round 2: Vicar acts at 2.0
            Assert.That(_engine.Timeline[0].Id, Is.EqualTo("hero_vicar"));
            // Vicar notices Weaver is close behind (2.5 - 2.0 = 0.5 <= 2.0)
            Assert.That(_engine.CheckSyncMoveAvailable(_vicar, _weaver), Is.True);

            // Execute coordinated Sync Move: Vicar commits fast technique (cost 50 => delay 1.0)
            _engine.CommitAction(_vicar, 50, 1.0f); // New time = 2.0 + 1.0 = 3.0

            // Round 3: Weaver acts at 2.5
            Assert.That(_engine.Timeline[0].Id, Is.EqualTo("hero_weaver"));
            _engine.CommitAction(_weaver, 100, 1.0f); // New time = 2.5 + 2.5 = 5.0

            // Round 4: Vicar acts again at 3.0 before hostile acts at 3.34!
            Assert.That(_engine.Timeline[0].Id, Is.EqualTo("hero_vicar"));

            // Vicar deals decisive blow defeating the beast
            _hostileBeast.CurrentHealth = 0f;
            _engine.RemoveCombatant(_hostileBeast.Id);

            Assert.That(_engine.Timeline.Count, Is.EqualTo(2));
            Assert.That(_engine.Timeline.All(c => c.IsAlly), Is.True, "All enemies defeated.");
        }

        #endregion
    }
}
