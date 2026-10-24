using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using InkAndAsh.Economy;

namespace InkAndAsh.Tests.EditMode
{
    [TestFixture]
    public class CaravanInventoryTests
    {
        private CaravanInventory _footPacks;
        private CaravanInventory _carriage;

        [SetUp]
        public void SetUp()
        {
            _footPacks = new CaravanInventory(StorageTier.FootPacks);
            _carriage = new CaravanInventory(StorageTier.HorseCarriage);
        }

        #region 1. Storage Tiers & Capacity Limits

        [Test]
        public void FootPacks_CapacityAndWeightLimits_Enforced()
        {
            Assert.That(_footPacks.Tier, Is.EqualTo(StorageTier.FootPacks));
            Assert.That(_footPacks.MaxSlots, Is.EqualTo(30));
            Assert.That(_footPacks.MaxWeightKg, Is.EqualTo(50.0f));

            // Fill up to 30 slots with distinct non-stacking items
            for (int i = 0; i < 30; i++)
            {
                bool added = _footPacks.AddItem($"item_{i}", $"Item {i}", 1.0f, 1, isPerishable: true);
                Assert.That(added, Is.True, $"Item {i} should be accepted within 30 slot limit.");
            }

            Assert.That(_footPacks.UsedSlots, Is.EqualTo(30));

            // 31st slot rejected
            bool overflow = _footPacks.AddItem("item_overflow", "Overflow Item", 0.5f, 1, isPerishable: true);
            Assert.That(overflow, Is.False, "31st distinct item must be rejected by Foot Packs.");
            Assert.That(_footPacks.UsedSlots, Is.EqualTo(30));
        }

        [Test]
        public void FootPacks_WeightCap_Enforced()
        {
            // Add 49kg
            Assert.That(_footPacks.AddItem("item_heavy", "Heavy Stone", 49.0f, 1, isPerishable: false), Is.True);
            Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(49.0f));

            // Add exactly 1kg to reach 50.0kg
            Assert.That(_footPacks.AddItem("item_small", "Small Stone", 1.0f, 1, isPerishable: false), Is.True);
            Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(50.0f));

            // Adding 0.1kg more must be rejected
            Assert.That(_footPacks.AddItem("item_grain", "Grain", 0.1f, 1, isPerishable: false), Is.False);
            Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(50.0f));
        }

        [Test]
        public void HorseCarriage_CapacityAndWeightLimits_Enforced()
        {
            Assert.That(_carriage.Tier, Is.EqualTo(StorageTier.HorseCarriage));
            Assert.That(_carriage.MaxSlots, Is.EqualTo(120));
            Assert.That(_carriage.MaxWeightKg, Is.EqualTo(300.0f));

            // Test weight limit at 300kg
            Assert.That(_carriage.AddItem("item_bulk", "Bulk Timber", 300.0f, 1, isPerishable: false), Is.True);
            Assert.That(_carriage.CurrentWeightKg, Is.EqualTo(300.0f));

            // Exceeding 300kg rejected
            Assert.That(_carriage.AddItem("item_extra", "Extra Load", 0.5f, 1, isPerishable: false), Is.False);
            Assert.That(_carriage.CurrentWeightKg, Is.EqualTo(300.0f));
        }

        #endregion

        #region 2. Item Removal & Multi-Stack Deduction

        [Test]
        public void RemoveItem_RejectsNonPositiveQuantities()
        {
            _footPacks.AddItem("item_iron", "Iron", 1.0f, 5, isPerishable: false);

            Assert.That(_footPacks.RemoveItem("item_iron", 0), Is.False);
            Assert.That(_footPacks.RemoveItem("item_iron", -3), Is.False);
            Assert.That(_footPacks.Items[0].Quantity, Is.EqualTo(5), "Negative or zero removal must not alter quantity.");
        }

        [Test]
        public void RemoveItem_RejectsNullOrMissingItem()
        {
            Assert.That(_footPacks.RemoveItem(null, 1), Is.False);
            Assert.That(_footPacks.RemoveItem("", 1), Is.False);
            Assert.That(_footPacks.RemoveItem("missing_id", 1), Is.False);
        }

        [Test]
        public void RemoveItem_RejectsQuantityExceedingAvailable()
        {
            _footPacks.AddItem("item_herbs", "Herbs", 0.3f, 3, isPerishable: false);

            bool result = _footPacks.RemoveItem("item_herbs", 4);
            Assert.That(result, Is.False);
            Assert.That(_footPacks.Items[0].Quantity, Is.EqualTo(3));
        }

        [Test]
        public void RemoveItem_CleanlyDeductsAcrossMultipleStacks()
        {
            // Perishable goods don't stack upon AddItem, occupying separate slots
            _footPacks.AddItem("item_fish", "Fish A", 1.5f, 1, durability: 90f, isPerishable: true);
            _footPacks.AddItem("item_fish", "Fish B", 1.5f, 1, durability: 80f, isPerishable: true);
            _footPacks.AddItem("item_fish", "Fish C", 1.5f, 1, durability: 70f, isPerishable: true);

            Assert.That(_footPacks.UsedSlots, Is.EqualTo(3));
            Assert.That(_footPacks.Items.Sum(i => i.Quantity), Is.EqualTo(3));

            // Remove 2 fish across stacks
            bool removed = _footPacks.RemoveItem("item_fish", 2);

            Assert.That(removed, Is.True);
            Assert.That(_footPacks.UsedSlots, Is.EqualTo(1));
            Assert.That(_footPacks.Items[0].Quantity, Is.EqualTo(1));

            // Remove the final remaining fish
            Assert.That(_footPacks.RemoveItem("item_fish", 1), Is.True);
            Assert.That(_footPacks.UsedSlots, Is.EqualTo(0));
            Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(0f));
        }

        #endregion

        #region 3. Real-Time Durability Decay

        [Test]
        public void DurabilityDecay_BaseRateInClearWeather_FootPacks()
        {
            _footPacks.AddItem("item_bloom", "Canopy Bloom", 0.2f, 1, durability: 100f, isPerishable: true);

            // 1% base per real minute in clear weather (multiplier 1.0)
            // 10 real minutes => 10% decay
            _footPacks.TickDecay(10.0f, biomeSpoilageMultiplier: 1.0f);

            Assert.That(_footPacks.Items[0].Durability, Is.EqualTo(90.0f).Within(0.0001f));
        }

        [Test]
        public void DurabilityDecay_CarriageProvides60PercentReduction()
        {
            _carriage.AddItem("item_bloom", "Canopy Bloom", 0.2f, 1, durability: 100f, isPerishable: true);

            // Carriage multiplier is 0.40 (60% decay reduction)
            // 10 real minutes => 10 * 1.0 * 0.40 = 4% decay
            _carriage.TickDecay(10.0f, biomeSpoilageMultiplier: 1.0f);

            Assert.That(_carriage.Items[0].Durability, Is.EqualTo(96.0f).Within(0.0001f));
        }

        [Test]
        public void DurabilityDecay_WeatherMultipliersAppliedCorrectly()
        {
            _footPacks.AddItem("item_meat_desert", "Desert Meat", 1.0f, 1, durability: 100f, isPerishable: true);
            _footPacks.AddItem("item_meat_frost", "Frost Meat", 1.0f, 1, durability: 100f, isPerishable: true);

            // Desert heat (+50% spoilage = 1.5x)
            var desertPack = new CaravanInventory(StorageTier.FootPacks);
            desertPack.AddItem("item_meat", "Meat", 1.0f, 1, durability: 100f, isPerishable: true);
            desertPack.TickDecay(10.0f, biomeSpoilageMultiplier: 1.5f);
            Assert.That(desertPack.Items[0].Durability, Is.EqualTo(85.0f).Within(0.0001f)); // 10 * 1.5 = 15%

            // Mountain freezing (0.2x spoilage)
            var frostPack = new CaravanInventory(StorageTier.FootPacks);
            frostPack.AddItem("item_meat", "Meat", 1.0f, 1, durability: 100f, isPerishable: true);
            frostPack.TickDecay(10.0f, biomeSpoilageMultiplier: 0.2f);
            Assert.That(frostPack.Items[0].Durability, Is.EqualTo(98.0f).Within(0.0001f)); // 10 * 0.2 = 2%
        }

        [Test]
        public void DurabilityDecay_ClampsAtZeroAndSetsSpoiled()
        {
            _footPacks.AddItem("item_meat", "Wild Meat", 1.0f, 1, durability: 5f, isPerishable: true);

            bool eventFired = false;
            string spoiledId = null;
            _footPacks.OnItemDecayed += (id, isSpoiled) =>
            {
                eventFired = true;
                spoiledId = id;
            };

            // 10 minutes decay = 10% decay, starting from 5%
            _footPacks.TickDecay(10.0f, 1.0f);

            Assert.That(_footPacks.Items[0].Durability, Is.EqualTo(0f));
            Assert.That(_footPacks.Items[0].IsSpoiled, Is.True);
            Assert.That(eventFired, Is.True);
            Assert.That(spoiledId, Is.EqualTo("item_meat"));
            Assert.That(_footPacks.UsedSlots, Is.EqualTo(1), "Spoiled items must not automatically drop from inventory.");

            // Subsequent decay ticks must not re-fire event or make durability negative
            eventFired = false;
            _footPacks.TickDecay(10.0f, 1.0f);
            Assert.That(_footPacks.Items[0].Durability, Is.EqualTo(0f));
            Assert.That(eventFired, Is.False);
        }

        [Test]
        public void DurabilityDecay_NonPerishableGoodsDoNotDecay()
        {
            _footPacks.AddItem("item_salt", "Glacier Salt", 1.5f, 1, durability: 100f, isPerishable: false);

            _footPacks.TickDecay(100.0f, biomeSpoilageMultiplier: 2.0f);

            Assert.That(_footPacks.Items[0].Durability, Is.EqualTo(100f));
            Assert.That(_footPacks.Items[0].IsSpoiled, Is.False);
        }

        #endregion

        #region 4. Moisture Weight

        [Test]
        public void MoistureWeight_AbsorbentItemsGain50PercentInHumidWeather()
        {
            // Golden Barley is moisture absorbent
            _footPacks.AddItem("item_golden_barley", "Golden Barley", unitWeightKg: 10.0f, quantity: 1, isPerishable: true, isAbsorbent: true);
            // Scrap iron is non-absorbent
            _footPacks.AddItem("item_scrap_iron", "Scrap Iron", unitWeightKg: 10.0f, quantity: 1, isPerishable: false, isAbsorbent: false);

            // Dry weather
            _footPacks.SetHumidWeather(false);
            Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(20.0f).Within(0.0001f));

            // Humid weather (Rain / Swamp)
            _footPacks.SetHumidWeather(true);
            // Barley becomes 10 * 1.5 = 15kg; Iron remains 10kg => Total 25kg
            Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(25.0f).Within(0.0001f));

            // Back to dry
            _footPacks.SetHumidWeather(false);
            Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(20.0f).Within(0.0001f));
        }

        [Test]
        public void MoistureWeight_HumidOverweightBlocksFurtherAdditions()
        {
            // Carriage max is 300kg. Add 200kg of barley (absorbent).
            _carriage.AddItem("item_golden_barley", "Barley", unitWeightKg: 200.0f, quantity: 1, isPerishable: true, isAbsorbent: true);
            // In dry weather: 200kg (within 300kg)
            Assert.That(_carriage.CurrentWeightKg, Is.EqualTo(200.0f));

            // Rain starts: 200 * 1.5 = 300kg (exactly at cap)
            _carriage.SetHumidWeather(true);
            Assert.That(_carriage.CurrentWeightKg, Is.EqualTo(300.0f));

            // Adding 1kg fails because 300 + 1 > 300
            bool added = _carriage.AddItem("item_extra", "Extra", 1.0f, 1, isPerishable: false);
            Assert.That(added, Is.False);

            // Return to dry weather: 200kg
            _carriage.SetHumidWeather(false);
            Assert.That(_carriage.CurrentWeightKg, Is.EqualTo(200.0f));

            // Adding 1kg now succeeds
            Assert.That(_carriage.AddItem("item_extra", "Extra", 1.0f, 1, isPerishable: false), Is.True);
        }

        #endregion

        #region 5. Campsite Cooking

        [Test]
        public void CampsiteCooking_ValidRecipeConsumesIngredientsAndGrants24HourBuff()
        {
            _footPacks.AddItem("item_golden_barley", "Golden Barley", 1.0f, 2, durability: 95f, isPerishable: true);
            _footPacks.AddItem("item_spring_water", "Spring Water", 2.0f, 1, durability: 100f, isPerishable: false);

            var ingredients = new List<string> { "item_golden_barley", "item_spring_water" };

            bool cooked = _footPacks.CookRecipe(ingredients, out string buffId, out float buffDurationHours);

            Assert.That(cooked, Is.True);
            Assert.That(buffDurationHours, Is.EqualTo(24.0f));
            Assert.That(string.IsNullOrEmpty(buffId), Is.False);

            // 1 barley was consumed, leaving 1
            var barley = _footPacks.Items.FirstOrDefault(i => i.ItemId == "item_golden_barley");
            Assert.That(barley, Is.Not.Null);
            Assert.That(barley.Quantity, Is.EqualTo(1));

            // Spring water was completely consumed
            var water = _footPacks.Items.FirstOrDefault(i => i.ItemId == "item_spring_water");
            Assert.That(water, Is.Null);
        }

        [Test]
        public void CampsiteCooking_RejectsSpoiledIngredients()
        {
            // Add spoiled barley (0% durability)
            _footPacks.AddItem("item_golden_barley", "Golden Barley", 1.0f, 1, durability: 0f, isPerishable: true);
            _footPacks.AddItem("item_spring_water", "Spring Water", 2.0f, 1, durability: 100f, isPerishable: false);

            var ingredients = new List<string> { "item_golden_barley", "item_spring_water" };

            bool cooked = _footPacks.CookRecipe(ingredients, out string buffId, out float buffDurationHours);

            Assert.That(cooked, Is.False, "Cooking must reject spoiled ingredients (0% durability).");
            Assert.That(_footPacks.Items.Count, Is.EqualTo(2), "Ingredients must not be consumed on failed recipe.");
        }

        [Test]
        public void CampsiteCooking_RejectsMissingIngredients()
        {
            _footPacks.AddItem("item_golden_barley", "Golden Barley", 1.0f, 1, durability: 100f, isPerishable: true);

            var ingredients = new List<string> { "item_golden_barley", "item_wild_game_meat" }; // Meat missing

            bool cooked = _footPacks.CookRecipe(ingredients, out _, out _);

            Assert.That(cooked, Is.False);
            Assert.That(_footPacks.Items[0].Quantity, Is.EqualTo(1));
        }

        #endregion
    }
}
