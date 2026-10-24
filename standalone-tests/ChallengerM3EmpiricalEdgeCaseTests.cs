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
    public class ChallengerM3EmpiricalEdgeCaseTests
    {
        private CaravanInventory _footPacks = null!;
        private CaravanInventory _carriage = null!;
        private CtbCombatEngine _combatEngine = null!;

        [SetUp]
        public void Setup()
        {
            _footPacks = new CaravanInventory(StorageTier.FootPacks);
            _carriage = new CaravanInventory(StorageTier.HorseCarriage);
            _combatEngine = new CtbCombatEngine();
        }

        #region 1. Multi-Stack Inventory Deduction

        [Test]
        public void MultiStack_Deduction_TwoStacksOf5_Removing7_VerifiesDeductionOrder()
        {
            // Player has two stacks of 5 Golden Barley
            // Stack 1 (index 0): added first, 5 items, durability 90%
            // Stack 2 (index 1): added second, 5 items, durability 100%
            bool added1 = _footPacks.AddItem("item_golden_barley", "Golden Barley Stack 1", 1.0f, quantity: 5, durability: 90f, isPerishable: true, isAbsorbent: true);
            bool added2 = _footPacks.AddItem("item_golden_barley", "Golden Barley Stack 2", 1.0f, quantity: 5, durability: 100f, isPerishable: true, isAbsorbent: true);

            Assert.That(added1, Is.True);
            Assert.That(added2, Is.True);
            Assert.That(_footPacks.UsedSlots, Is.EqualTo(2));
            Assert.That(_footPacks.Items[0].Name, Is.EqualTo("Golden Barley Stack 1"));
            Assert.That(_footPacks.Items[0].Quantity, Is.EqualTo(5));
            Assert.That(_footPacks.Items[1].Name, Is.EqualTo("Golden Barley Stack 2"));
            Assert.That(_footPacks.Items[1].Quantity, Is.EqualTo(5));

            // Remove 7 items
            bool removed = _footPacks.RemoveItem("item_golden_barley", 7);
            Assert.That(removed, Is.True);

            // Total remaining must be 3
            int totalRemaining = _footPacks.Items.Sum(i => i.Quantity);
            Assert.That(totalRemaining, Is.EqualTo(3));
            Assert.That(_footPacks.UsedSlots, Is.EqualTo(1));

            var remainingStack = _footPacks.Items[0];
            TestContext.Out.WriteLine($"Remaining stack name: '{remainingStack.Name}', Quantity: {remainingStack.Quantity}, Durability: {remainingStack.Durability}");

            // Requirement 1 from Dispatch:
            // "if player has two stacks of 5 Golden Barley, removing 7 items should deduct 5 from stack 1 and 2 from stack 2."
            // Under FIFO deduction:
            // Stack 1 is depleted (5 deducted) -> 0 remaining (removed).
            // Stack 2 is partially deducted (2 deducted) -> 3 remaining (Stack 2 remains in inventory).
            //
            // Under current implementation (backward loop):
            // Stack 2 is depleted (5 deducted) -> 0 remaining (removed).
            // Stack 1 is partially deducted (2 deducted) -> 3 remaining (Stack 1 remains in inventory).
            Assert.That(remainingStack.Name, Is.EqualTo("Golden Barley Stack 2"),
                "DEFECT FOUND: RemoveItem iterates backwards (_items.Count - 1 down to 0), deducting 5 from stack 2 and 2 from stack 1 (LIFO), instead of deducting 5 from stack 1 and 2 from stack 2 (FIFO).");
        }

        [Test]
        public void MultiStack_Deduction_InsufficientQuantity_DoesNotMutateAnyStack()
        {
            _footPacks.AddItem("item_golden_barley", "Barley 1", 1.0f, quantity: 5, durability: 90f, isPerishable: true);
            _footPacks.AddItem("item_golden_barley", "Barley 2", 1.0f, quantity: 5, durability: 100f, isPerishable: true);

            // Total is 10. Attempt to remove 11.
            bool removed = _footPacks.RemoveItem("item_golden_barley", 11);

            Assert.That(removed, Is.False);
            Assert.That(_footPacks.UsedSlots, Is.EqualTo(2));
            Assert.That(_footPacks.Items[0].Quantity, Is.EqualTo(5), "Stack 1 must not be partially deducted on failure.");
            Assert.That(_footPacks.Items[1].Quantity, Is.EqualTo(5), "Stack 2 must not be partially deducted on failure.");
        }

        [Test]
        public void MultiStack_Deduction_ExactTotalQuantity_RemovesAllMatchingStacks()
        {
            _footPacks.AddItem("item_golden_barley", "Barley 1", 1.0f, quantity: 5, durability: 90f, isPerishable: true);
            _footPacks.AddItem("item_golden_barley", "Barley 2", 1.0f, quantity: 5, durability: 100f, isPerishable: true);

            bool removed = _footPacks.RemoveItem("item_golden_barley", 10);

            Assert.That(removed, Is.True);
            Assert.That(_footPacks.UsedSlots, Is.EqualTo(0));
            Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(0f));
        }

        [Test]
        public void MultiStack_Deduction_WeightUpdatesAccuratelyUnderHumidWeather()
        {
            _footPacks.AddItem("item_golden_barley", "Barley 1", 1.0f, quantity: 5, durability: 90f, isPerishable: true, isAbsorbent: true);
            _footPacks.AddItem("item_golden_barley", "Barley 2", 1.0f, quantity: 5, durability: 100f, isPerishable: true, isAbsorbent: true);

            _footPacks.SetHumidityWeather(true);
            // 10 units * 1.0kg * 1.5 (absorbent humid multiplier) = 15.0kg
            Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(15.0f).Within(0.0001f));

            _footPacks.RemoveItem("item_golden_barley", 7);
            // 3 units remaining * 1.0kg * 1.5 = 4.5kg
            Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(4.5f).Within(0.0001f));
        }

        [Test]
        public void MultiStack_Deduction_DoesNotAffectOtherItemTypes()
        {
            _footPacks.AddItem("item_golden_barley", "Barley 1", 1.0f, quantity: 5, isPerishable: true);
            _footPacks.AddItem("item_scrap_iron", "Scrap Iron", 2.0f, quantity: 4, isPerishable: false);
            _footPacks.AddItem("item_golden_barley", "Barley 2", 1.0f, quantity: 5, isPerishable: true);

            Assert.That(_footPacks.UsedSlots, Is.EqualTo(3));

            _footPacks.RemoveItem("item_golden_barley", 7);

            var iron = _footPacks.Items.FirstOrDefault(i => i.ItemId == "item_scrap_iron");
            Assert.That(iron, Is.Not.Null);
            Assert.That(iron!.Quantity, Is.EqualTo(4), "Scrap Iron must remain completely untouched.");
        }

        #endregion

        #region 2. Non-Positive Quantity Removals

        [Test]
        public void NonPositiveQuantity_RemoveItem_Zero_ReturnsFalse()
        {
            _footPacks.AddItem("barley", "Barley", 1.0f, quantity: 10, isPerishable: false);
            Assert.That(_footPacks.Items[0].Quantity, Is.EqualTo(10));

            bool result = _footPacks.RemoveItem("barley", 0);

            Assert.That(result, Is.False, "RemoveItem with quantity 0 must return false.");
            Assert.That(_footPacks.Items[0].Quantity, Is.EqualTo(10), "Quantity must remain unaltered.");
        }

        [Test]
        public void NonPositiveQuantity_RemoveItem_NegativeFive_ReturnsFalse()
        {
            _footPacks.AddItem("barley", "Barley", 1.0f, quantity: 10, isPerishable: false);
            Assert.That(_footPacks.Items[0].Quantity, Is.EqualTo(10));

            bool result = _footPacks.RemoveItem("barley", -5);

            Assert.That(result, Is.False, "RemoveItem with quantity -5 must return false.");
            Assert.That(_footPacks.Items[0].Quantity, Is.EqualTo(10), "Quantity must not be incremented or altered.");
        }

        [Test]
        public void NonPositiveQuantity_EmptyInventory_ReturnsFalse()
        {
            Assert.That(_footPacks.RemoveItem("barley", 0), Is.False);
            Assert.That(_footPacks.RemoveItem("barley", -5), Is.False);
            Assert.That(_footPacks.UsedSlots, Is.EqualTo(0));
        }

        [Test]
        public void NonPositiveQuantity_NullOrEmptyItemId_ReturnsFalse()
        {
            Assert.That(_footPacks.RemoveItem(null!, 0), Is.False);
            Assert.That(_footPacks.RemoveItem(null!, -5), Is.False);
            Assert.That(_footPacks.RemoveItem("", 0), Is.False);
            Assert.That(_footPacks.RemoveItem("", -5), Is.False);
        }

        #endregion

        #region 3. Agility <= 0 in CtbCombatEngine.CalculateTurnDelay

        [Test]
        public void Agility_Zero_Instance_ClampsToOne_NoDivideByZero()
        {
            var actor = new Combatant
            {
                Id = "zero_agi_hero",
                Name = "Paralyzed Monk",
                IsAlly = true,
                BaseAgility = 0f
            };

            float delay = _combatEngine.CalculateTurnDelay(actor, 100, 1.0f);

            Assert.That(float.IsNaN(delay), Is.False, "Turn delay must not be NaN.");
            Assert.That(float.IsInfinity(delay), Is.False, "Turn delay must not be Infinity.");
            Assert.That(delay, Is.GreaterThan(0f), "Turn delay must be strictly positive.");
            Assert.That(delay, Is.EqualTo(100.0f).Within(0.0001f), "Delay for skillCost 100 with clamped Agility 1.0 must be 100.0.");
        }

        [Test]
        public void Agility_Negative_Instance_ClampsToOne_NoNegativeDelay()
        {
            var actor = new Combatant
            {
                Id = "neg_agi_hero",
                Name = "Slow Cursed Knight",
                IsAlly = true,
                BaseAgility = -50f
            };

            float delay = _combatEngine.CalculateTurnDelay(actor, 100, 1.0f);

            Assert.That(float.IsNaN(delay), Is.False);
            Assert.That(float.IsInfinity(delay), Is.False);
            Assert.That(delay, Is.GreaterThan(0f), "Turn delay must not be negative.");
            Assert.That(delay, Is.EqualTo(100.0f).Within(0.0001f));
        }

        [Test]
        public void Agility_Zero_StaticOverload_ClampsToOne()
        {
            float delay = CtbCombatEngine.CalculateTurnDelay(0f, 100, 1.0f);

            Assert.That(float.IsNaN(delay), Is.False);
            Assert.That(float.IsInfinity(delay), Is.False);
            Assert.That(delay, Is.EqualTo(100.0f).Within(0.0001f));
        }

        [Test]
        public void Agility_Negative_StaticOverload_ClampsToOne()
        {
            float delay = CtbCombatEngine.CalculateTurnDelay(-25f, 100, 1.0f);

            Assert.That(float.IsNaN(delay), Is.False);
            Assert.That(float.IsInfinity(delay), Is.False);
            Assert.That(delay, Is.EqualTo(100.0f).Within(0.0001f));
        }

        [Test]
        public void Agility_ZeroWithInjuryDebuff_ClampsToOne()
        {
            var actor = new Combatant
            {
                Id = "zero_agi_injured",
                BaseAgility = 0f,
                HasInjuryDebuff = true
            };

            float delay = _combatEngine.CalculateTurnDelay(actor, 100, 1.0f);

            // Cost = 100 * 1.15 = 115, Agility = 1.0 -> delay = 115.0
            Assert.That(delay, Is.EqualTo(115.0f).Within(0.0001f));
        }

        [Test]
        public void Agility_ExtremeNegative_EffectiveAgilityClampsToOne()
        {
            var actor = new Combatant
            {
                Id = "extreme_neg",
                BaseAgility = -999999f
            };

            Assert.That(actor.EffectiveAgility, Is.EqualTo(1.0f));
            float delay = _combatEngine.CalculateTurnDelay(actor, 100, 1.0f);
            Assert.That(delay, Is.EqualTo(100.0f).Within(0.0001f));
        }

        #endregion

        #region 4. Test Isolation Stress Tests

        [Test]
        public void TestIsolation_CaravanInventory_InstancesDoNotShareItemsList()
        {
            var invA = new CaravanInventory();
            var invB = new CaravanInventory();

            invA.AddItem("item_a", "Item A", 1.0f, 1);
            Assert.That(invA.UsedSlots, Is.EqualTo(1));
            Assert.That(invB.UsedSlots, Is.EqualTo(0), "Inventory B must remain empty when A is modified.");
        }

        [Test]
        public void TestIsolation_CtbCombatEngine_InstancesDoNotShareTimelineQueue()
        {
            var engineA = new CtbCombatEngine();
            var engineB = new CtbCombatEngine();

            var c = new Combatant { Id = "c1", BaseAgility = 50f };
            engineA.AddCombatant(c);

            Assert.That(engineA.Timeline.Count, Is.EqualTo(1));
            Assert.That(engineB.Timeline.Count, Is.EqualTo(0), "Engine B must have independent timeline.");
        }

        #endregion
    }
}
