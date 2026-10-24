#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using InkAndAsh.Combat;
using InkAndAsh.Economy;

namespace InkAndAsh.StandaloneTests
{
    [TestFixture]
    public class Milestone2AdversarialStressTests
    {
        private CtbCombatEngine _combatEngine = null!;
        private CaravanInventory _footPacks = null!;
        private CaravanInventory _carriage = null!;

        [SetUp]
        public void Setup()
        {
            _combatEngine = new CtbCombatEngine();
            _footPacks = new CaravanInventory(StorageTier.FootPacks);
            _carriage = new CaravanInventory(StorageTier.HorseCarriage);
        }

        #region 1. Agility Stress Tests (Zero / Negative Agility in Turn Formula)

        [Test]
        public void AgilityStress_ZeroAgility_ClampedToOne_NoDivideByZero()
        {
            var actor = new Combatant
            {
                Id = "hero_zero_agi",
                Name = "Zero Agility Hero",
                IsAlly = true,
                BaseAgility = 0f
            };

            Assert.That(actor.EffectiveAgility, Is.EqualTo(1.0f));
            float delay = _combatEngine.CalculateTurnDelay(actor, 100, 1.0f);
            Assert.That(float.IsInfinity(delay), Is.False, "Delay must not be infinite.");
            Assert.That(float.IsNaN(delay), Is.False, "Delay must not be NaN.");
            Assert.That(delay, Is.EqualTo(100.0f).Within(0.0001f));
        }

        [Test]
        public void AgilityStress_NegativeAgility_ClampedToOne()
        {
            var actor = new Combatant
            {
                Id = "hero_neg_agi",
                Name = "Negative Agility Hero",
                IsAlly = true,
                BaseAgility = -50f
            };

            Assert.That(actor.EffectiveAgility, Is.EqualTo(1.0f), "Negative BaseAgility must clamp to 1.0.");
            float delay = _combatEngine.CalculateTurnDelay(actor, 100, 1.0f);
            Assert.That(delay, Is.EqualTo(100.0f).Within(0.0001f));
        }

        [Test]
        public void AgilityStress_ExtremeNegativeAgility_ClampedToOne()
        {
            var actor = new Combatant
            {
                Id = "hero_extreme_neg_agi",
                Name = "Extreme Negative Agility Hero",
                IsAlly = true,
                BaseAgility = -999999f
            };

            Assert.That(actor.EffectiveAgility, Is.EqualTo(1.0f));
            float delay = _combatEngine.CalculateTurnDelay(actor, 100, 1.0f);
            Assert.That(delay, Is.EqualTo(100.0f).Within(0.0001f));
        }

        [Test]
        public void AgilityStress_TinyPositiveAgility_ClampedToOne()
        {
            var actor = new Combatant
            {
                Id = "hero_tiny_agi",
                Name = "Tiny Agility Hero",
                IsAlly = true,
                BaseAgility = 0.0001f
            };

            Assert.That(actor.EffectiveAgility, Is.EqualTo(1.0f));
            float delay = _combatEngine.CalculateTurnDelay(actor, 100, 1.0f);
            Assert.That(delay, Is.EqualTo(100.0f).Within(0.0001f));
        }

        [Test]
        public void AgilityStress_ZeroAgility_WithInjuryDebuff_ClampedToOne()
        {
            var actor = new Combatant
            {
                Id = "hero_zero_injured",
                Name = "Zero Agility Injured Hero",
                IsAlly = true,
                BaseAgility = 0f,
                HasInjuryDebuff = true
            };

            Assert.That(actor.EffectiveAgility, Is.EqualTo(1.0f));
            float delay = _combatEngine.CalculateTurnDelay(actor, 100, 1.0f);
            Assert.That(delay, Is.EqualTo(115.0f).Within(0.0001f));
        }

        [Test]
        public void AgilityStress_NegativeAgility_WithInjuryDebuff_ClampedToOne()
        {
            var actor = new Combatant
            {
                Id = "hero_neg_injured",
                Name = "Negative Agility Injured Hero",
                IsAlly = true,
                BaseAgility = -20f,
                HasInjuryDebuff = true
            };

            Assert.That(actor.EffectiveAgility, Is.EqualTo(1.0f));
            float delay = _combatEngine.CalculateTurnDelay(actor, 100, 1.0f);
            Assert.That(delay, Is.EqualTo(115.0f).Within(0.0001f));
        }

        [Test]
        public void AgilityStress_ZeroSkillCost_ReturnsZeroDelay()
        {
            var actor = new Combatant { BaseAgility = 50f };
            float delay = _combatEngine.CalculateTurnDelay(actor, 0, 1.0f);
            Assert.That(delay, Is.EqualTo(0f));
        }

        [Test]
        public void AgilityStress_ExtremeHighSkillCost_CalculatesAccurately()
        {
            var actor = new Combatant { BaseAgility = 50f };
            float delay = _combatEngine.CalculateTurnDelay(actor, 1_000_000, 1.0f);
            Assert.That(delay, Is.EqualTo(20_000f).Within(0.01f));
        }

        [Test]
        public void AgilityStress_NegativeOrZeroWeatherMultiplier_ClampedToMinimumPointOne()
        {
            var actor = new Combatant { BaseAgility = 50f };
            float delayZero = _combatEngine.CalculateTurnDelay(actor, 100, 0f);
            Assert.That(delayZero, Is.EqualTo(0.2f).Within(0.0001f));

            float delayNeg = _combatEngine.CalculateTurnDelay(actor, 100, -2.5f);
            Assert.That(delayNeg, Is.EqualTo(0.2f).Within(0.0001f));
        }

        [Test]
        public void AgilityStress_NullActor_ReturnsZero()
        {
            float delay = _combatEngine.CalculateTurnDelay(null!, 100, 1.0f);
            Assert.That(delay, Is.EqualTo(0f));
        }

        #endregion

        #region 2. Extreme Timeline Shifts & Intervention Stress Tests

        [Test]
        public void Intervention_InsufficientFP_24_99_Rejected_NoFPSpent()
        {
            var protector = new Combatant
            {
                Id = "hero_p",
                IsAlly = true,
                CurrentHealth = 100f,
                FocusPoints = 24.99f
            };
            var target = new Combatant
            {
                Id = "hero_t",
                IsAlly = true,
                CurrentHealth = 100f,
                NextTurnTime = 30.0f
            };

            bool success = _combatEngine.PerformIntervention(protector, target);
            Assert.That(success, Is.False);
            Assert.That(protector.FocusPoints, Is.EqualTo(24.99f));
            Assert.That(target.NextTurnTime, Is.EqualTo(30.0f));
        }

        [Test]
        public void Intervention_ZeroFP_Rejected_NoFPSpent()
        {
            var protector = new Combatant { Id = "p", IsAlly = true, FocusPoints = 0f };
            var target = new Combatant { Id = "t", IsAlly = true, NextTurnTime = 20.0f };

            bool success = _combatEngine.PerformIntervention(protector, target);
            Assert.That(success, Is.False);
            Assert.That(protector.FocusPoints, Is.EqualTo(0f));
            Assert.That(target.NextTurnTime, Is.EqualTo(20.0f));
        }

        [Test]
        public void Intervention_NegativeFP_Rejected_NoFPSpent()
        {
            var protector = new Combatant { Id = "p", IsAlly = true, FocusPoints = -10f };
            var target = new Combatant { Id = "t", IsAlly = true, NextTurnTime = 20.0f };

            bool success = _combatEngine.PerformIntervention(protector, target);
            Assert.That(success, Is.False);
            Assert.That(protector.FocusPoints, Is.EqualTo(-10f));
        }

        [Test]
        public void Intervention_DeadProtector_Rejected()
        {
            var protector = new Combatant { Id = "p", IsAlly = true, CurrentHealth = 0f, FocusPoints = 50f };
            var target = new Combatant { Id = "t", IsAlly = true, CurrentHealth = 100f, NextTurnTime = 20.0f };

            bool success = _combatEngine.PerformIntervention(protector, target);
            Assert.That(success, Is.False);
            Assert.That(protector.FocusPoints, Is.EqualTo(50f));
        }

        [Test]
        public void Intervention_DeadTargetAlly_Rejected()
        {
            var protector = new Combatant { Id = "p", IsAlly = true, CurrentHealth = 100f, FocusPoints = 50f };
            var target = new Combatant { Id = "t", IsAlly = true, CurrentHealth = 0f, NextTurnTime = 20.0f };

            bool success = _combatEngine.PerformIntervention(protector, target);
            Assert.That(success, Is.False);
            Assert.That(protector.FocusPoints, Is.EqualTo(50f));
        }

        [Test]
        public void Intervention_EnemyProtector_Rejected()
        {
            var enemyProtector = new Combatant { Id = "e_p", IsAlly = false, CurrentHealth = 100f, FocusPoints = 50f };
            var allyTarget = new Combatant { Id = "a_t", IsAlly = true, CurrentHealth = 100f, NextTurnTime = 20.0f };

            bool success = _combatEngine.PerformIntervention(enemyProtector, allyTarget);
            Assert.That(success, Is.False);
            Assert.That(enemyProtector.FocusPoints, Is.EqualTo(50f));
        }

        [Test]
        public void Intervention_EnemyTarget_Rejected()
        {
            var allyProtector = new Combatant { Id = "a_p", IsAlly = true, CurrentHealth = 100f, FocusPoints = 50f };
            var enemyTarget = new Combatant { Id = "e_t", IsAlly = false, CurrentHealth = 100f, NextTurnTime = 20.0f };

            bool success = _combatEngine.PerformIntervention(allyProtector, enemyTarget);
            Assert.That(success, Is.False);
            Assert.That(allyProtector.FocusPoints, Is.EqualTo(50f));
        }

        [Test]
        public void Intervention_AdvancesTargetPastCurrentTime_ClampedToCurrentTime()
        {
            var actor = new Combatant { Id = "actor", IsAlly = true, BaseAgility = 50f, NextTurnTime = 10.0f };
            _combatEngine.AddCombatant(actor);
            _combatEngine.CommitAction(actor, 100, 1.0f);
            Assert.That(_combatEngine.CurrentTime, Is.EqualTo(12.0f));

            var protector = new Combatant { Id = "p", IsAlly = true, FocusPoints = 50f, CurrentHealth = 100f };
            var target = new Combatant { Id = "t", IsAlly = true, CurrentHealth = 100f, NextTurnTime = 15.0f };
            _combatEngine.AddCombatant(protector);
            _combatEngine.AddCombatant(target);

            bool success = _combatEngine.PerformIntervention(protector, target);
            Assert.That(success, Is.True);
            Assert.That(target.NextTurnTime, Is.EqualTo(12.0f).Within(0.0001f));
            Assert.That(target.NextTurnTime, Is.GreaterThanOrEqualTo(_combatEngine.CurrentTime));
        }

        [Test]
        public void Intervention_TargetAlreadyAtOrBeforeCurrentTime_ClampedToCurrentTime()
        {
            var actor = new Combatant { Id = "actor", IsAlly = true, BaseAgility = 50f, NextTurnTime = 20.0f };
            _combatEngine.AddCombatant(actor);
            _combatEngine.CommitAction(actor, 100, 1.0f);

            var protector = new Combatant { Id = "p", IsAlly = true, FocusPoints = 50f, CurrentHealth = 100f };
            var target = new Combatant { Id = "t", IsAlly = true, CurrentHealth = 100f, NextTurnTime = 22.0f };

            bool success = _combatEngine.PerformIntervention(protector, target);
            Assert.That(success, Is.True);
            Assert.That(target.NextTurnTime, Is.EqualTo(22.0f).Within(0.0001f));
        }

        [Test]
        public void Intervention_ChainedInterventions_ConsumesFPSequentiallyUntilDepleted()
        {
            var protector = new Combatant { Id = "p", IsAlly = true, CurrentHealth = 100f, FocusPoints = 60f };
            var target = new Combatant { Id = "t", IsAlly = true, CurrentHealth = 100f, NextTurnTime = 50.0f };

            Assert.That(_combatEngine.PerformIntervention(protector, target), Is.True);
            Assert.That(protector.FocusPoints, Is.EqualTo(35.0f));
            Assert.That(target.NextTurnTime, Is.EqualTo(35.0f));

            Assert.That(_combatEngine.PerformIntervention(protector, target), Is.True);
            Assert.That(protector.FocusPoints, Is.EqualTo(10.0f));
            Assert.That(target.NextTurnTime, Is.EqualTo(20.0f));

            Assert.That(_combatEngine.PerformIntervention(protector, target), Is.False);
            Assert.That(protector.FocusPoints, Is.EqualTo(10.0f));
            Assert.That(target.NextTurnTime, Is.EqualTo(20.0f));
        }

        [Test]
        public void CommitAction_WithZeroSkillCost_AdvancesWithoutDelayPastCurrentTime()
        {
            var actor = new Combatant { Id = "swift_hero", IsAlly = true, BaseAgility = 50f, NextTurnTime = 5.0f };
            _combatEngine.AddCombatant(actor);

            _combatEngine.CommitAction(actor, 0, 1.0f);

            Assert.That(actor.NextTurnTime, Is.EqualTo(5.0f));
            Assert.That(_combatEngine.CurrentTime, Is.EqualTo(5.0f));
        }

        [Test]
        public void CommitAction_MultipleActors_TimelineSortingRemainsAscending()
        {
            var a = new Combatant { Id = "a", IsAlly = true, BaseAgility = 50f, NextTurnTime = 1.0f };
            var b = new Combatant { Id = "b", IsAlly = true, BaseAgility = 40f, NextTurnTime = 3.0f };
            var c = new Combatant { Id = "c", IsAlly = false, BaseAgility = 60f, NextTurnTime = 2.0f };

            _combatEngine.AddCombatant(a);
            _combatEngine.AddCombatant(b);
            _combatEngine.AddCombatant(c);

            Assert.That(_combatEngine.Timeline[0].Id, Is.EqualTo("a"));
            Assert.That(_combatEngine.Timeline[1].Id, Is.EqualTo("c"));
            Assert.That(_combatEngine.Timeline[2].Id, Is.EqualTo("b"));

            _combatEngine.CommitAction(a, 200, 1.0f);

            Assert.That(_combatEngine.Timeline[0].Id, Is.EqualTo("c"));
            Assert.That(_combatEngine.Timeline[1].Id, Is.EqualTo("b"));
            Assert.That(_combatEngine.Timeline[2].Id, Is.EqualTo("a"));
            Assert.That(_combatEngine.CurrentTime, Is.EqualTo(2.0f));
        }

        #endregion

        #region 3. Sync Move Detection Stress Tests (Deltas 2.0, 2.0001, 0.0)

        [Test]
        public void SyncMove_ExactDeltaZero_ReturnsTrue()
        {
            var allyA = new Combatant { Id = "a1", IsAlly = true, CurrentHealth = 100f, NextTurnTime = 10.0f };
            var allyB = new Combatant { Id = "a2", IsAlly = true, CurrentHealth = 100f, NextTurnTime = 10.0f };

            Assert.That(_combatEngine.CheckSyncMoveAvailable(allyA, allyB), Is.True);
        }

        [Test]
        public void SyncMove_ExactDeltaTwo_ReturnsTrue()
        {
            var allyA = new Combatant { Id = "a1", IsAlly = true, CurrentHealth = 100f, NextTurnTime = 10.0f };
            var allyB = new Combatant { Id = "a2", IsAlly = true, CurrentHealth = 100f, NextTurnTime = 12.0f };

            Assert.That(_combatEngine.CheckSyncMoveAvailable(allyA, allyB), Is.True);
            Assert.That(_combatEngine.CheckSyncMoveAvailable(allyB, allyA), Is.True);
        }

        [Test]
        public void SyncMove_DeltaTwoPointZeroZeroZeroOne_ReturnsFalse()
        {
            var allyA = new Combatant { Id = "a1", IsAlly = true, CurrentHealth = 100f, NextTurnTime = 10.0f };
            var allyB = new Combatant { Id = "a2", IsAlly = true, CurrentHealth = 100f, NextTurnTime = 12.0001f };

            Assert.That(_combatEngine.CheckSyncMoveAvailable(allyA, allyB), Is.False);
            Assert.That(_combatEngine.CheckSyncMoveAvailable(allyB, allyA), Is.False);
        }

        [Test]
        public void SyncMove_DeltaTwoPointZeroZeroZeroZeroZeroOne_ReturnsFalse()
        {
            var allyA = new Combatant { Id = "a1", IsAlly = true, CurrentHealth = 100f, NextTurnTime = 10.0f };
            var allyB = new Combatant { Id = "a2", IsAlly = true, CurrentHealth = 100f, NextTurnTime = 12.00001f };

            Assert.That(_combatEngine.CheckSyncMoveAvailable(allyA, allyB), Is.False);
        }

        [Test]
        public void SyncMove_NegativeDeltaTwo_ReturnsTrue()
        {
            var allyA = new Combatant { Id = "a1", IsAlly = true, CurrentHealth = 100f, NextTurnTime = 12.0f };
            var allyB = new Combatant { Id = "a2", IsAlly = true, CurrentHealth = 100f, NextTurnTime = 10.0f };

            Assert.That(_combatEngine.CheckSyncMoveAvailable(allyA, allyB), Is.True);
        }

        [Test]
        public void SyncMove_NegativeDeltaTwoPointZeroZeroZeroOne_ReturnsFalse()
        {
            var allyA = new Combatant { Id = "a1", IsAlly = true, CurrentHealth = 100f, NextTurnTime = 12.0001f };
            var allyB = new Combatant { Id = "a2", IsAlly = true, CurrentHealth = 100f, NextTurnTime = 10.0f };

            Assert.That(_combatEngine.CheckSyncMoveAvailable(allyA, allyB), Is.False);
        }

        [Test]
        public void SyncMove_DeadAllyA_ReturnsFalse()
        {
            var allyA = new Combatant { Id = "a1", IsAlly = true, CurrentHealth = 0f, NextTurnTime = 10.0f };
            var allyB = new Combatant { Id = "a2", IsAlly = true, CurrentHealth = 100f, NextTurnTime = 10.5f };

            Assert.That(_combatEngine.CheckSyncMoveAvailable(allyA, allyB), Is.False);
        }

        [Test]
        public void SyncMove_DeadAllyB_ReturnsFalse()
        {
            var allyA = new Combatant { Id = "a1", IsAlly = true, CurrentHealth = 100f, NextTurnTime = 10.0f };
            var allyB = new Combatant { Id = "a2", IsAlly = true, CurrentHealth = 0f, NextTurnTime = 10.5f };

            Assert.That(_combatEngine.CheckSyncMoveAvailable(allyA, allyB), Is.False);
        }

        [Test]
        public void SyncMove_BothDead_ReturnsFalse()
        {
            var allyA = new Combatant { Id = "a1", IsAlly = true, CurrentHealth = 0f, NextTurnTime = 10.0f };
            var allyB = new Combatant { Id = "a2", IsAlly = true, CurrentHealth = 0f, NextTurnTime = 10.0f };

            Assert.That(_combatEngine.CheckSyncMoveAvailable(allyA, allyB), Is.False);
        }

        [Test]
        public void SyncMove_EnemyVsAlly_ReturnsFalse()
        {
            var enemy = new Combatant { Id = "e1", IsAlly = false, CurrentHealth = 100f, NextTurnTime = 10.0f };
            var ally = new Combatant { Id = "a1", IsAlly = true, CurrentHealth = 100f, NextTurnTime = 10.0f };

            Assert.That(_combatEngine.CheckSyncMoveAvailable(enemy, ally), Is.False);
            Assert.That(_combatEngine.CheckSyncMoveAvailable(ally, enemy), Is.False);
        }

        [Test]
        public void SyncMove_EnemyVsEnemy_ReturnsFalse()
        {
            var enemy1 = new Combatant { Id = "e1", IsAlly = false, CurrentHealth = 100f, NextTurnTime = 10.0f };
            var enemy2 = new Combatant { Id = "e2", IsAlly = false, CurrentHealth = 100f, NextTurnTime = 10.0f };

            Assert.That(_combatEngine.CheckSyncMoveAvailable(enemy1, enemy2), Is.False);
        }

        [Test]
        public void SyncMove_NullCombatant_ReturnsFalse()
        {
            var ally = new Combatant { Id = "a1", IsAlly = true, CurrentHealth = 100f, NextTurnTime = 10.0f };

            Assert.That(_combatEngine.CheckSyncMoveAvailable(ally, null!), Is.False);
            Assert.That(_combatEngine.CheckSyncMoveAvailable(null!, ally), Is.False);
            Assert.That(_combatEngine.CheckSyncMoveAvailable(null!, null!), Is.False);
        }

        #endregion

        #region 4. Mediator's Anchor Stress Tests (Severe Delay Debuffs & Overrides)

        [Test]
        public void MediatorsAnchor_GuaranteesSyncMove_AtExtremeDelta500()
        {
            var allyA = new Combatant { Id = "a1", IsAlly = true, CurrentHealth = 100f, NextTurnTime = 0.0f };
            var allyB = new Combatant { Id = "a2", IsAlly = true, CurrentHealth = 100f, NextTurnTime = 500.0f };

            Assert.That(_combatEngine.CheckSyncMoveAvailable(allyA, allyB), Is.False);

            _combatEngine.SetMediatorsAnchor(allyA, allyB);
            Assert.That(_combatEngine.CheckSyncMoveAvailable(allyA, allyB), Is.True);
            Assert.That(_combatEngine.CheckSyncMoveAvailable(allyB, allyA), Is.True);
        }

        [Test]
        public void MediatorsAnchor_GuaranteesSyncMove_UnderSevereInjuryDebuffAndBlizzardWeather()
        {
            var allyA = new Combatant { Id = "a1", IsAlly = true, CurrentHealth = 100f, NextTurnTime = 5.0f };
            var allyB = new Combatant { Id = "a2", IsAlly = true, CurrentHealth = 100f, NextTurnTime = 50.0f };

            _combatEngine.SetMediatorsAnchor(allyA, allyB);
            _combatEngine.ApplyInjuryDebuff(allyA);
            _combatEngine.ApplyInjuryDebuff(allyB);

            Assert.That(allyA.HasInjuryDebuff, Is.True);
            Assert.That(allyB.HasInjuryDebuff, Is.True);
            Assert.That(_combatEngine.CheckSyncMoveAvailable(allyA, allyB), Is.True,
                "Mediator's Anchor must override timeline delta even under severe debuffs.");
        }

        [Test]
        public void MediatorsAnchor_FailsWhenAnchoredAllyDies()
        {
            var allyA = new Combatant { Id = "a1", IsAlly = true, CurrentHealth = 100f, NextTurnTime = 5.0f };
            var allyB = new Combatant { Id = "a2", IsAlly = true, CurrentHealth = 100f, NextTurnTime = 50.0f };

            _combatEngine.SetMediatorsAnchor(allyA, allyB);
            Assert.That(_combatEngine.CheckSyncMoveAvailable(allyA, allyB), Is.True);

            allyB.CurrentHealth = 0f;
            Assert.That(allyB.IsAlive, Is.False);
            Assert.That(_combatEngine.CheckSyncMoveAvailable(allyA, allyB), Is.False,
                "Mediator's Anchor must NOT permit sync move if an anchored ally is dead.");
        }

        [Test]
        public void MediatorsAnchor_FailsWhenBothAlliesDie()
        {
            var allyA = new Combatant { Id = "a1", IsAlly = true, CurrentHealth = 0f, NextTurnTime = 5.0f };
            var allyB = new Combatant { Id = "a2", IsAlly = true, CurrentHealth = 0f, NextTurnTime = 50.0f };

            _combatEngine.SetMediatorsAnchor(allyA, allyB);
            Assert.That(_combatEngine.CheckSyncMoveAvailable(allyA, allyB), Is.False);
        }

        [Test]
        public void MediatorsAnchor_BrokenWhenAllyReAnchoredToDifferentAlly()
        {
            var allyA = new Combatant { Id = "a1", IsAlly = true, CurrentHealth = 100f, NextTurnTime = 0.0f };
            var allyB = new Combatant { Id = "a2", IsAlly = true, CurrentHealth = 100f, NextTurnTime = 100.0f };
            var allyC = new Combatant { Id = "a3", IsAlly = true, CurrentHealth = 100f, NextTurnTime = 200.0f };

            _combatEngine.SetMediatorsAnchor(allyA, allyB);
            Assert.That(_combatEngine.CheckSyncMoveAvailable(allyA, allyB), Is.True);

            _combatEngine.SetMediatorsAnchor(allyA, allyC);
            Assert.That(_combatEngine.CheckSyncMoveAvailable(allyA, allyC), Is.True);
            Assert.That(_combatEngine.CheckSyncMoveAvailable(allyA, allyB), Is.False);
        }

        [Test]
        public void MediatorsAnchor_NullAlly_NoCrash()
        {
            var allyA = new Combatant { Id = "a1", IsAlly = true };
            Assert.DoesNotThrow(() => _combatEngine.SetMediatorsAnchor(allyA, null!));
            Assert.DoesNotThrow(() => _combatEngine.SetMediatorsAnchor(null!, allyA));
            Assert.DoesNotThrow(() => _combatEngine.SetMediatorsAnchor(null!, null!));
        }

        #endregion

        #region 5. Inventory Over-Capacity Stress Tests (Slot Limit & Weight Limit)

        [Test]
        public void Inventory_FootPacks_Exactly30DistinctItems_Accepted()
        {
            for (int i = 1; i <= 30; i++)
            {
                bool added = _footPacks.AddItem($"p_item_{i}", $"Perishable {i}", 0.1f, 1, durability: 100f, isPerishable: true);
                Assert.That(added, Is.True, $"Item {i} should be accepted.");
            }

            Assert.That(_footPacks.UsedSlots, Is.EqualTo(30));
            Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(3.0f).Within(0.0001f));
        }

        [Test]
        public void Inventory_FootPacks_31stDistinctItem_Rejected()
        {
            for (int i = 1; i <= 30; i++)
            {
                _footPacks.AddItem($"p_item_{i}", $"Perishable {i}", 0.1f, 1, durability: 100f, isPerishable: true);
            }

            bool overflow = _footPacks.AddItem("p_item_31", "Perishable 31", 0.1f, 1, durability: 100f, isPerishable: true);
            Assert.That(overflow, Is.False, "31st perishable item must be rejected when slots are full.");
            Assert.That(_footPacks.UsedSlots, Is.EqualTo(30));
        }

        [Test]
        public void Inventory_FootPacks_StackingExistingNonPerishable_AcceptedWhenAtSlotCap()
        {
            for (int i = 1; i <= 29; i++)
            {
                _footPacks.AddItem($"p_item_{i}", $"Perishable {i}", 0.1f, 1, durability: 100f, isPerishable: true);
            }

            bool ironAdded = _footPacks.AddItem("item_scrap_iron", "Scrap Iron", 1.0f, 1, durability: 100f, isPerishable: false);
            Assert.That(ironAdded, Is.True);
            Assert.That(_footPacks.UsedSlots, Is.EqualTo(30));

            bool stackAdded = _footPacks.AddItem("item_scrap_iron", "Scrap Iron", 1.0f, 5, durability: 100f, isPerishable: false);
            Assert.That(stackAdded, Is.True, "Stacking existing non-perishable must succeed even at slot cap.");
            Assert.That(_footPacks.UsedSlots, Is.EqualTo(30), "UsedSlots must remain 30.");

            var iron = _footPacks.Items.First(i => i.ItemId == "item_scrap_iron");
            Assert.That(iron.Quantity, Is.EqualTo(6));
        }

        [Test]
        public void Inventory_FootPacks_AddingPerishable_RejectedWhenAtSlotCap_EvenIfSameId()
        {
            for (int i = 1; i <= 29; i++)
            {
                _footPacks.AddItem($"p_item_{i}", $"Perishable {i}", 0.1f, 1, durability: 100f, isPerishable: true);
            }

            _footPacks.AddItem("item_wild_game_meat", "Game Meat", 0.1f, 1, durability: 100f, isPerishable: true);
            Assert.That(_footPacks.UsedSlots, Is.EqualTo(30));

            bool extraMeat = _footPacks.AddItem("item_wild_game_meat", "Game Meat", 0.1f, 1, durability: 100f, isPerishable: true);
            Assert.That(extraMeat, Is.False, "Adding perishable item when slots are full must be rejected.");
            Assert.That(_footPacks.UsedSlots, Is.EqualTo(30));
        }

        [Test]
        public void Inventory_FootPacks_Exactly50Kg_Accepted()
        {
            bool added = _footPacks.AddItem("bulk_ore", "Bulk Ore", 50.0f, 1, isPerishable: false);
            Assert.That(added, Is.True);
            Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(50.0f).Within(0.0001f));
        }

        [Test]
        public void Inventory_FootPacks_Exceeding50KgByPointZeroOne_Rejected()
        {
            _footPacks.AddItem("bulk_ore", "Bulk Ore", 50.0f, 1, isPerishable: false);

            bool overflow = _footPacks.AddItem("tiny_feather", "Feather", 0.01f, 1, isPerishable: false);
            Assert.That(overflow, Is.False, "Weight exceeding 50.0 kg must be rejected.");
            Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(50.0f).Within(0.0001f));
        }

        [Test]
        public void Inventory_FootPacks_MassiveOverweight_Rejected_StateUntouched()
        {
            bool added = _footPacks.AddItem("massive_boulder", "Boulder", 1000.0f, 1, isPerishable: false);
            Assert.That(added, Is.False);
            Assert.That(_footPacks.UsedSlots, Is.EqualTo(0));
            Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(0f));
        }

        [Test]
        public void Inventory_Carriage_120Slots_Accepted_121stRejected()
        {
            for (int i = 1; i <= 120; i++)
            {
                bool added = _carriage.AddItem($"c_item_{i}", $"Item {i}", 0.1f, 1, durability: 100f, isPerishable: true);
                Assert.That(added, Is.True, $"Carriage slot {i} should be accepted.");
            }

            Assert.That(_carriage.UsedSlots, Is.EqualTo(120));

            bool overflow = _carriage.AddItem("c_item_121", "Item 121", 0.1f, 1, durability: 100f, isPerishable: true);
            Assert.That(overflow, Is.False, "121st item in Carriage must be rejected.");
            Assert.That(_carriage.UsedSlots, Is.EqualTo(120));
        }

        [Test]
        public void Inventory_Carriage_300Kg_Accepted_300Point01Rejected()
        {
            bool added300 = _carriage.AddItem("cargo_300", "Cargo 300", 300.0f, 1, isPerishable: false);
            Assert.That(added300, Is.True);
            Assert.That(_carriage.CurrentWeightKg, Is.EqualTo(300.0f).Within(0.0001f));

            bool addedExtra = _carriage.AddItem("cargo_extra", "Extra", 0.01f, 1, isPerishable: false);
            Assert.That(addedExtra, Is.False);
            Assert.That(_carriage.CurrentWeightKg, Is.EqualTo(300.0f).Within(0.0001f));
        }

        [Test]
        public void Inventory_ZeroOrNegativeQuantity_Rejected()
        {
            Assert.That(_footPacks.AddItem("item_ore", "Ore", 1.0f, 0), Is.False);
            Assert.That(_footPacks.AddItem("item_ore", "Ore", 1.0f, -5), Is.False);
            Assert.That(_footPacks.UsedSlots, Is.EqualTo(0));
        }

        [Test]
        public void Inventory_NullOrEmptyItemId_Rejected()
        {
            Assert.That(_footPacks.AddItem(null!, "Null Item", 1.0f, 1), Is.False);
            Assert.That(_footPacks.AddItem("", "Empty Item", 1.0f, 1), Is.False);
            Assert.That(_footPacks.UsedSlots, Is.EqualTo(0));
        }

        [Test]
        public void Inventory_RemoveItem_NonExistentItem_ReturnsFalse()
        {
            Assert.That(_footPacks.RemoveItem("non_existent", 1), Is.False);
        }

        [Test]
        public void Inventory_RemoveItem_QuantityExceedingAvailable_ReturnsFalse_DoesNotDeduct()
        {
            _footPacks.AddItem("item_ore", "Ore", 1.0f, 3, isPerishable: false);
            bool removed = _footPacks.RemoveItem("item_ore", 5);

            Assert.That(removed, Is.False);
            Assert.That(_footPacks.Items[0].Quantity, Is.EqualTo(3));
        }

        [Test]
        public void Inventory_RemoveItem_PartialRemoval_DecrementsQuantity()
        {
            _footPacks.AddItem("item_ore", "Ore", 1.0f, 5, isPerishable: false);
            bool removed = _footPacks.RemoveItem("item_ore", 2);

            Assert.That(removed, Is.True);
            Assert.That(_footPacks.Items[0].Quantity, Is.EqualTo(3));
            Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(3.0f));
        }

        [Test]
        public void Inventory_RemoveItem_ExactQuantity_RemovesSlotCompletely()
        {
            _footPacks.AddItem("item_ore", "Ore", 1.0f, 3, isPerishable: false);
            bool removed = _footPacks.RemoveItem("item_ore", 3);

            Assert.That(removed, Is.True);
            Assert.That(_footPacks.UsedSlots, Is.EqualTo(0));
            Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(0f));
        }

        [Test]
        public void Inventory_RemoveItem_NegativeQuantity_ExposesQuantityInflationDefect()
        {
            _footPacks.AddItem("item_ore", "Ore", 1.0f, 3, isPerishable: false);
            Assert.That(_footPacks.Items[0].Quantity, Is.EqualTo(3));

            // Calling RemoveItem with negative quantity
            bool result = _footPacks.RemoveItem("item_ore", -5);

            TestContext.Out.WriteLine($"RemoveItem(-5) returned {result}, new quantity is {_footPacks.Items[0].Quantity}");
            Assert.That(_footPacks.Items[0].Quantity, Is.EqualTo(8), "Defect confirmed: negative removal increments quantity bypassing weight/slot limits!");
        }

        [Test]
        public void SyncMove_SelfComparison_PermitsSelfSyncDefect()
        {
            var ally = new Combatant { Id = "solo_hero", IsAlly = true, CurrentHealth = 100f, NextTurnTime = 5.0f };

            // Calling CheckSyncMoveAvailable with ally compared to itself
            bool selfSync = _combatEngine.CheckSyncMoveAvailable(ally, ally);
            TestContext.Out.WriteLine($"Self-sync returned: {selfSync}");
            Assert.That(selfSync, Is.True, "Defect confirmed: CheckSyncMoveAvailable permits self-sync because delta is 0 <= 2.0 without checking distinct IDs.");
        }

        [Test]
        public void Intervention_SelfIntervention_PermitsSelfTurnBoostDefect()
        {
            var ally = new Combatant { Id = "solo_hero", IsAlly = true, CurrentHealth = 100f, FocusPoints = 50f, NextTurnTime = 30.0f };

            // Calling PerformIntervention with protector == target
            bool selfIntervention = _combatEngine.PerformIntervention(ally, ally);
            TestContext.Out.WriteLine($"Self-intervention returned: {selfIntervention}, new time: {ally.NextTurnTime}");
            Assert.That(selfIntervention, Is.True, "Defect confirmed: PerformIntervention permits self-boosting because it does not verify protector != targetAlly.");
            Assert.That(ally.NextTurnTime, Is.EqualTo(15.0f));
        }

        [Test]
        public void CommitAction_NegativeSkillCost_ReversesTimelineClockDefect()
        {
            var actor = new Combatant { Id = "time_traveler", IsAlly = true, BaseAgility = 50f, NextTurnTime = 10.0f };
            _combatEngine.AddCombatant(actor);

            // Establish CurrentTime at 10.0f
            _combatEngine.CommitAction(actor, 0, 1.0f);
            Assert.That(_combatEngine.CurrentTime, Is.EqualTo(10.0f));

            // Now commit with negative skill cost (-200 => delay = -4.0)
            _combatEngine.CommitAction(actor, -200, 1.0f);

            // New NextTurnTime = 10 - 4 = 6.0!
            // And CurrentTime reverses from 10.0f to 6.0f!
            TestContext.Out.WriteLine($"CurrentTime after negative skill cost: {_combatEngine.CurrentTime}, actor time: {actor.NextTurnTime}");
            Assert.That(actor.NextTurnTime, Is.EqualTo(6.0f));
            Assert.That(_combatEngine.CurrentTime, Is.EqualTo(6.0f),
                "Defect confirmed: Negative skill cost allows timeline time-travel, causing CurrentTime to regress backwards.");
        }



        #endregion

        #region 6. Spoilage & Durability Stress Tests (Durability down to 0%)

        [Test]
        public void Spoilage_DurabilityClampsAtZero_DoesNotGoNegative()
        {
            _footPacks.AddItem("item_delicate_meat", "Meat", 1.0f, 1, durability: 5.0f, isPerishable: true);

            _footPacks.TickDecay(50.0f, 1.0f);

            var item = _footPacks.Items[0];
            Assert.That(item.Durability, Is.EqualTo(0.0f));
            Assert.That(item.IsSpoiled, Is.True);
        }

        [Test]
        public void Spoilage_ItemDoesNotDropFromInventory_RemainsSpoiledInSlot()
        {
            _footPacks.AddItem("item_delicate_meat", "Meat", 1.0f, 1, durability: 2.0f, isPerishable: true);
            Assert.That(_footPacks.UsedSlots, Is.EqualTo(1));

            _footPacks.TickDecay(10.0f, 1.0f);

            Assert.That(_footPacks.UsedSlots, Is.EqualTo(1), "Spoiled item must NOT drop automatically; it stays in inventory.");
            Assert.That(_footPacks.Items[0].IsSpoiled, Is.True);
            Assert.That(_footPacks.Items[0].Durability, Is.EqualTo(0.0f));
        }

        [Test]
        public void Spoilage_FiresOnItemDecayedEventOnce()
        {
            int eventFireCount = 0;
            string? spoiledId = null;
            bool spoiledState = false;

            _footPacks.OnItemDecayed += (id, isSpoiled) =>
            {
                eventFireCount++;
                spoiledId = id;
                spoiledState = isSpoiled;
            };

            _footPacks.AddItem("item_game_meat", "Game Meat", 1.0f, 1, durability: 3.0f, isPerishable: true);

            _footPacks.TickDecay(10.0f, 1.0f);

            Assert.That(eventFireCount, Is.EqualTo(1));
            Assert.That(spoiledId, Is.EqualTo("item_game_meat"));
            Assert.That(spoiledState, Is.True);

            _footPacks.TickDecay(10.0f, 1.0f);
            Assert.That(eventFireCount, Is.EqualTo(1), "OnItemDecayed must not re-fire on already-spoiled item.");
        }

        [Test]
        public void Spoilage_ItemAddedWithZeroDurability_DoesNotFireDecayEventOnTick()
        {
            int eventFireCount = 0;
            _footPacks.OnItemDecayed += (id, s) => eventFireCount++;

            _footPacks.AddItem("item_already_spoiled", "Rotten Food", 1.0f, 1, durability: 0.0f, isPerishable: true);
            Assert.That(_footPacks.Items[0].IsSpoiled, Is.True);

            _footPacks.TickDecay(10.0f, 1.0f);
            Assert.That(eventFireCount, Is.EqualTo(0), "No new spoil transition event should fire for pre-spoiled items.");
        }

        [Test]
        public void Spoilage_NonPerishableItem_ZeroDecayOverThousandsOfMinutes()
        {
            _footPacks.AddItem("item_ironwood_resin", "Resin", 1.0f, 1, durability: 100f, isPerishable: false);

            _footPacks.TickDecay(100_000.0f, 5.0f);

            var item = _footPacks.Items[0];
            Assert.That(item.Durability, Is.EqualTo(100.0f));
            Assert.That(item.IsSpoiled, Is.False);
        }

        [Test]
        public void Spoilage_SpoiledItem_RejectedByCookingSystem()
        {
            _footPacks.AddItem("item_wild_game_meat", "Meat", 1.0f, 1, durability: 0.0f, isPerishable: true);
            _footPacks.AddItem("item_glacier_salt", "Salt", 1.0f, 1, durability: 100f, isPerishable: false);

            var recipe = new List<string> { "item_wild_game_meat", "item_glacier_salt" };
            bool cooked = _footPacks.CookRecipe(recipe, out string _, out float _);

            Assert.That(cooked, Is.False, "Cooking must reject 0% durability spoiled meat.");
            Assert.That(_footPacks.Items[0].Quantity, Is.EqualTo(1), "Spoiled item was not consumed.");
        }

        [Test]
        public void Spoilage_SpoiledItem_CanBeRemovedManually()
        {
            _footPacks.AddItem("item_rotten_meat", "Meat", 1.0f, 1, durability: 0.0f, isPerishable: true);
            Assert.That(_footPacks.UsedSlots, Is.EqualTo(1));

            bool removed = _footPacks.RemoveItem("item_rotten_meat", 1);
            Assert.That(removed, Is.True);
            Assert.That(_footPacks.UsedSlots, Is.EqualTo(0));
        }

        #endregion

        #region 7. Moisture Weight Across Weather Switches Stress Tests

        [Test]
        public void MoistureWeight_NonAbsorbentItem_UnchangedInHumidWeather()
        {
            _footPacks.AddItem("item_glacier_salt", "Salt", 10.0f, 1, isAbsorbent: false);
            Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(10.0f));

            _footPacks.SetHumidityWeather(true);
            Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(10.0f));

            _footPacks.SetHumidityWeather(false);
            Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(10.0f));
        }

        [Test]
        public void MoistureWeight_AbsorbentItem_GainsExact50PercentInHumidWeather()
        {
            _footPacks.AddItem("item_golden_barley", "Barley", 2.0f, 10, isAbsorbent: true);
            Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(20.0f).Within(0.0001f));

            _footPacks.SetHumidityWeather(true);
            Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(30.0f).Within(0.0001f));

            _footPacks.SetHumidityWeather(false);
            Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(20.0f).Within(0.0001f));
        }

        [Test]
        public void MoistureWeight_100WeatherSwitches_NoWeightDriftOrLeakage()
        {
            _footPacks.AddItem("item_golden_barley", "Barley", 1.0f, 10, isAbsorbent: true);
            _footPacks.AddItem("item_coarse_fiber", "Fiber", 0.5f, 20, isAbsorbent: true);
            _footPacks.AddItem("item_scrap_iron", "Iron", 5.0f, 2, isAbsorbent: false);

            for (int i = 0; i < 100; i++)
            {
                _footPacks.SetHumidityWeather(true);
                Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(40.0f).Within(0.0001f), $"Drift detected on humid switch {i}");

                _footPacks.SetHumidityWeather(false);
                Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(30.0f).Within(0.0001f), $"Drift detected on dry switch {i}");
            }
        }

        [Test]
        public void MoistureWeight_HumidWeatherPushesInventoryOverweight_BlocksSubsequentAdditions()
        {
            _footPacks.AddItem("item_golden_barley", "Barley", 1.0f, 35, isAbsorbent: true);
            Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(35.0f));

            _footPacks.SetHumidityWeather(true);
            Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(52.5f));
            Assert.That(_footPacks.CurrentWeightKg, Is.GreaterThan(_footPacks.MaxWeightKg));

            bool addAttempt = _footPacks.AddItem("item_feather", "Feather", 0.001f, 1, isPerishable: false);
            Assert.That(addAttempt, Is.False, "Must not allow adding items when moisture has caused overweight.");
        }

        [Test]
        public void MoistureWeight_ReturnToDryWeather_ClearsOverweightCondition_PermitsAdditions()
        {
            _footPacks.AddItem("item_golden_barley", "Barley", 1.0f, 35, isAbsorbent: true);
            _footPacks.SetHumidityWeather(true);
            Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(52.5f));

            _footPacks.SetHumidityWeather(false);
            Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(35.0f));

            bool addAttempt = _footPacks.AddItem("item_salt", "Glacier Salt", 5.0f, 1, isPerishable: false);
            Assert.That(addAttempt, Is.True);
            Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(40.0f));
        }

        [Test]
        public void MoistureWeight_AbsorbentItemAddedDuringHumidWeather_CalculatesWeightWith50PercentPenalty()
        {
            _footPacks.SetHumidityWeather(true);

            bool added = _footPacks.AddItem("item_golden_barley", "Barley", 1.0f, 10, isAbsorbent: true);
            Assert.That(added, Is.True);
            Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(15.0f));

            bool addedOverweight = _footPacks.AddItem("item_golden_barley_2", "Barley 2", 1.0f, 25, isAbsorbent: true);
            Assert.That(addedOverweight, Is.False, "Adding absorbent item that would push weight past max in humid weather must be rejected.");
            Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(15.0f));
        }

        #endregion
    }
}
