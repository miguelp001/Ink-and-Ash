#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using InkAndAsh.StandaloneTests.E2ETests.Contracts;
using NUnit.Framework;

namespace InkAndAsh.StandaloneTests.E2ETests
{
    [TestFixture]
    public class CaravanInventoryE2ETests
    {
        private CaravanInventory _footPacks = null!;
        private CaravanInventory _horseCarriage = null!;

        [SetUp]
        public void Setup()
        {
            _footPacks = new CaravanInventory(StorageTier.FootPacks);
            _horseCarriage = new CaravanInventory(StorageTier.HorseCarriage);
        }

        #region Tier 1: Feature Coverage (Baseline Happy Path)

        [Test]
        public void F3_T1_01_FootPacksCapacityLimits()
        {
            Assert.That(_footPacks.Tier, Is.EqualTo(StorageTier.FootPacks));
            Assert.That(_footPacks.MaxSlots, Is.EqualTo(30));
            Assert.That(_footPacks.MaxWeightKg, Is.EqualTo(50.0f));
            Assert.That(_footPacks.DecayReductionMultiplier, Is.EqualTo(1.0f));
        }

        [Test]
        public void F3_T1_02_HorseCarriageCapacityLimits()
        {
            Assert.That(_horseCarriage.Tier, Is.EqualTo(StorageTier.HorseCarriage));
            Assert.That(_horseCarriage.MaxSlots, Is.EqualTo(120));
            Assert.That(_horseCarriage.MaxWeightKg, Is.EqualTo(300.0f));
            Assert.That(_horseCarriage.DecayReductionMultiplier, Is.EqualTo(0.40f), "Carriage must provide 60% decay reduction.");
        }

        [Test]
        public void F3_T1_03_BaseDurabilityDecayTicking()
        {
            _footPacks.AddItem("item_wild_game_meat", "Wild Game Meat", 2.0f, quantity: 1, durability: 100f, isPerishable: true);

            // 10 real-time minutes in Clear weather (1.0% / min base * 1.0 weather * 1.0 tier = 10% lost)
            _footPacks.TickDecay(10.0f, 1.0f);

            var meat = _footPacks.Items.First(i => i.ItemId == "item_wild_game_meat");
            Assert.That(meat.Durability, Is.EqualTo(90.0f).Within(0.0001f));
        }

        [Test]
        public void F3_T1_04_CarriageProvides60PercentDecayReduction()
        {
            _horseCarriage.AddItem("item_wild_game_meat", "Wild Game Meat", 2.0f, quantity: 1, durability: 100f, isPerishable: true);

            // 10 real-time minutes: 10 * 1.0% * 1.0 weather * (1.0 - 0.60) = 4.0% lost
            _horseCarriage.TickDecay(10.0f, 1.0f);

            var meat = _horseCarriage.Items.First(i => i.ItemId == "item_wild_game_meat");
            Assert.That(meat.Durability, Is.EqualTo(96.0f).Within(0.0001f));
        }

        [Test]
        public void F3_T1_05_DesertHeatIncreasesDecayBy50Percent()
        {
            _footPacks.AddItem("item_fresh_river_fish", "Fresh River Fish", 1.5f, quantity: 1, durability: 100f, isPerishable: true);

            // 10 real-time minutes in Desert Heat (+50% = 1.5x multiplier): 10 * 1.0% * 1.5 * 1.0 = 15.0% lost
            _footPacks.TickDecay(10.0f, 1.5f);

            var fish = _footPacks.Items.First(i => i.ItemId == "item_fresh_river_fish");
            Assert.That(fish.Durability, Is.EqualTo(85.0f).Within(0.0001f));
        }

        [Test]
        public void F3_T1_06_FreezingWeatherReducesDecayTo02Percent()
        {
            _footPacks.AddItem("item_fresh_river_fish", "Fresh River Fish", 1.5f, quantity: 1, durability: 100f, isPerishable: true);

            // 10 real-time minutes in Mountains Freezing (0.2x multiplier): 10 * 1.0% * 0.2 * 1.0 = 2.0% lost
            _footPacks.TickDecay(10.0f, 0.2f);

            var fish = _footPacks.Items.First(i => i.ItemId == "item_fresh_river_fish");
            Assert.That(fish.Durability, Is.EqualTo(98.0f).Within(0.0001f));
        }

        [Test]
        public void F3_T1_07_MoistureWeightPenaltyInRainAndSwamp()
        {
            // Golden Barley: 10 kg dry
            _footPacks.AddItem("item_golden_barley", "Golden Barley", 1.0f, quantity: 10, isAbsorbent: true);
            Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(10.0f).Within(0.0001f));

            // Enter Rain / Swamp: +50% moisture weight -> 15.0 kg
            _footPacks.SetHumidityWeather(true);
            Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(15.0f).Within(0.0001f));

            // Return to Clear weather -> 10.0 kg
            _footPacks.SetHumidityWeather(false);
            Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(10.0f).Within(0.0001f));
        }

        [Test]
        public void F3_T1_08_CampsiteCookingConsumesIngredientsAndGrantsBuff()
        {
            _horseCarriage.AddItem("item_golden_barley", "Golden Barley", 1.0f, quantity: 5, durability: 100f);
            _horseCarriage.AddItem("item_glacier_salt", "Glacier Salt", 1.5f, quantity: 2, durability: 100f, isPerishable: false);

            var recipe = new List<string> { "item_golden_barley", "item_golden_barley", "item_glacier_salt" };
            bool cooked = _horseCarriage.CookRecipe(recipe, out string buffId, out float durationHours);

            Assert.That(cooked, Is.True);
            Assert.That(buffId, Is.EqualTo("Buff_Expedition_WarmthAndResolve"));
            Assert.That(durationHours, Is.EqualTo(24.0f));

            // Verify ingredients were consumed
            var remainingBarley = _horseCarriage.Items.First(i => i.ItemId == "item_golden_barley");
            Assert.That(remainingBarley.Quantity, Is.EqualTo(3)); // 5 - 2 = 3

            var remainingSalt = _horseCarriage.Items.First(i => i.ItemId == "item_glacier_salt");
            Assert.That(remainingSalt.Quantity, Is.EqualTo(1)); // 2 - 1 = 1
        }

        #endregion

        #region Tier 2: Boundary & Corner Cases

        [Test]
        public void F3_T2_01_FootPacksSlot30AcceptedSlot31Rejected()
        {
            for (int i = 1; i <= 30; i++)
            {
                bool added = _footPacks.AddItem($"unique_item_{i}", $"Item {i}", 0.1f, quantity: 1, isPerishable: true);
                Assert.That(added, Is.True, $"Slot {i} should be accepted.");
            }

            Assert.That(_footPacks.UsedSlots, Is.EqualTo(30));

            // 31st item should be rejected
            bool overflow = _footPacks.AddItem("item_overflow", "Overflow Item", 0.1f, quantity: 1, isPerishable: true);
            Assert.That(overflow, Is.False, "31st item should exceed Foot Packs slot limit.");
            Assert.That(_footPacks.UsedSlots, Is.EqualTo(30));
        }

        [Test]
        public void F3_T2_02_FootPacksWeight50Accepted50Point01Rejected()
        {
            // Add exactly 50.0 kg
            bool addedExact = _footPacks.AddItem("bulk_cargo", "Bulk Ore", 50.0f, quantity: 1, isPerishable: false);
            Assert.That(addedExact, Is.True);
            Assert.That(_footPacks.CurrentWeightKg, Is.EqualTo(50.0f).Within(0.0001f));

            // Add 0.01 kg extra
            bool addedExtra = _footPacks.AddItem("tiny_pebble", "Pebble", 0.01f, quantity: 1, isPerishable: false);
            Assert.That(addedExtra, Is.False, "Adding weight exceeding 50.0 kg must be rejected.");
        }

        [Test]
        public void F3_T2_03_PerishableDurabilityClampsAtZero()
        {
            _footPacks.AddItem("item_game_meat", "Game Meat", 2.0f, quantity: 1, durability: 5.0f, isPerishable: true);

            // Tick decay that exceeds remaining durability (10% decay on 5% durability)
            _footPacks.TickDecay(10.0f, 1.0f);

            var meat = _footPacks.Items.First(i => i.ItemId == "item_game_meat");
            Assert.That(meat.Durability, Is.EqualTo(0.0f));
            Assert.That(meat.IsSpoiled, Is.True);
        }

        [Test]
        public void F3_T2_04_NonPerishableItemSuffersZeroDecay()
        {
            _footPacks.AddItem("item_scrap_iron", "Scrap Iron", 5.0f, quantity: 2, durability: 100f, isPerishable: false);

            // Tick massive decay (1000 real minutes)
            _footPacks.TickDecay(1000.0f, 2.0f);

            var iron = _footPacks.Items.First(i => i.ItemId == "item_scrap_iron");
            Assert.That(iron.Durability, Is.EqualTo(100.0f));
            Assert.That(iron.IsSpoiled, Is.False);
        }

        [Test]
        public void F3_T2_05_SpoiledIngredientsRejectedForCooking()
        {
            // Add spoiled meat (durability = 0) and valid salt
            _footPacks.AddItem("item_wild_game_meat", "Meat", 2.0f, quantity: 1, durability: 0.0f, isPerishable: true);
            _footPacks.AddItem("item_glacier_salt", "Salt", 1.5f, quantity: 1, durability: 100f, isPerishable: false);

            var recipe = new List<string> { "item_wild_game_meat", "item_glacier_salt" };
            bool cooked = _footPacks.CookRecipe(recipe, out string _, out float _);

            Assert.That(cooked, Is.False, "Cooking with spoiled ingredients must be rejected.");
        }

        [Test]
        public void F3_T2_06_CookingFailsWhenIngredientsMissing()
        {
            _footPacks.AddItem("item_golden_barley", "Barley", 1.0f, quantity: 1, durability: 100f);

            // Recipe requires 2 barley and 1 salt
            var recipe = new List<string> { "item_golden_barley", "item_golden_barley", "item_glacier_salt" };
            bool cooked = _footPacks.CookRecipe(recipe, out string _, out float _);

            Assert.That(cooked, Is.False);
            // Verify partial ingredients were NOT consumed on failure
            Assert.That(_footPacks.Items.First(i => i.ItemId == "item_golden_barley").Quantity, Is.EqualTo(1));
        }

        #endregion

        #region Tier 3: Cross-Feature Combinations

        [Test]
        public void F3_T3_01_CarriageInDesertHeatDecayMath()
        {
            _horseCarriage.AddItem("item_canopy_bloom", "Canopy Bloom", 0.2f, quantity: 1, durability: 100f, isPerishable: true);

            // 10 real-time minutes in Desert (+50% = 1.5x) with Carriage (60% reduction = 0.40):
            // Effective decay = 10 * 1.0% * 1.5 * 0.40 = 6.0% lost
            _horseCarriage.TickDecay(10.0f, 1.5f);

            var bloom = _horseCarriage.Items.First(i => i.ItemId == "item_canopy_bloom");
            Assert.That(bloom.Durability, Is.EqualTo(94.0f).Within(0.0001f));
        }

        [Test]
        public void F3_T3_02_CarriageInFreezingMountainDecayMath()
        {
            _horseCarriage.AddItem("item_fresh_river_fish", "River Fish", 1.5f, quantity: 1, durability: 100f, isPerishable: true);

            // 10 real-time minutes in Freezing (0.2x) with Carriage (0.40):
            // Effective decay = 10 * 1.0% * 0.2 * 0.40 = 0.8% lost
            _horseCarriage.TickDecay(10.0f, 0.2f);

            var fish = _horseCarriage.Items.First(i => i.ItemId == "item_fresh_river_fish");
            Assert.That(fish.Durability, Is.EqualTo(99.2f).Within(0.0001f));
        }

        [Test]
        public void F3_T3_03_MoistureWeightEncumbranceInRain()
        {
            // Fill carriage to 280 kg dry (limit: 300 kg)
            _horseCarriage.AddItem("item_golden_barley", "Golden Barley", 1.0f, quantity: 100, isAbsorbent: true); // 100 kg
            _horseCarriage.AddItem("timber_bulk", "Timber", 180.0f, quantity: 1, isAbsorbent: false);              // 180 kg
            Assert.That(_horseCarriage.CurrentWeightKg, Is.EqualTo(280.0f));

            // Enter rain: Barley moisture increases by +50% (100 -> 150 kg), pushing total to 330 kg!
            _horseCarriage.SetHumidityWeather(true);
            Assert.That(_horseCarriage.CurrentWeightKg, Is.EqualTo(330.0f));
            Assert.That(_horseCarriage.CurrentWeightKg, Is.GreaterThan(_horseCarriage.MaxWeightKg), "Moisture should trigger overweight condition.");
        }

        [Test]
        public void F3_T3_04_PerishableItemSpoilsDuringTravelEmittingEvent()
        {
            bool spoilEventFired = false;
            string spoiledItemId = string.Empty;

            _footPacks.OnItemDecayed += (itemId, wasSpoiled) =>
            {
                spoilEventFired = true;
                spoiledItemId = itemId;
            };

            _footPacks.AddItem("item_delicate_meat", "Meat", 1.0f, quantity: 1, durability: 2.0f, isPerishable: true);

            // Tick decay of 5% (more than remaining 2%)
            _footPacks.TickDecay(5.0f, 1.0f);

            Assert.That(spoilEventFired, Is.True);
            Assert.That(spoiledItemId, Is.EqualTo("item_delicate_meat"));
        }

        #endregion

        #region Tier 4: Real-World Scenarios

        [Test]
        public void F3_T4_01_MultiStagePilgrimageDecayAndCampMeal()
        {
            // Initial stocking: 10 kg Golden Barley, 5 kg Game Meat, 2 kg Glacier Salt
            _horseCarriage.AddItem("item_golden_barley", "Golden Barley", 1.0f, quantity: 10, durability: 100f, isPerishable: true, isAbsorbent: true);
            _horseCarriage.AddItem("item_wild_game_meat", "Wild Game Meat", 1.0f, quantity: 5, durability: 100f, isPerishable: true);
            _horseCarriage.AddItem("item_glacier_salt", "Glacier Salt", 1.0f, quantity: 2, durability: 100f, isPerishable: false);

            Assert.That(_horseCarriage.CurrentWeightKg, Is.EqualTo(17.0f));

            // Leg 1: Travel 20 real minutes through Grasslands (Clear, 1.0x)
            // Carriage decay = 20 * 1.0% * 1.0 * 0.40 = 8.0% lost
            _horseCarriage.TickDecay(20.0f, 1.0f);

            var meat = _horseCarriage.Items.First(i => i.ItemId == "item_wild_game_meat");
            Assert.That(meat.Durability, Is.EqualTo(92.0f).Within(0.0001f));

            // Leg 2: Travel 30 real minutes through Swamp (Rain, 1.5x)
            // Carriage decay = 30 * 1.0% * 1.5 * 0.40 = 18.0% lost
            _horseCarriage.SetHumidityWeather(true);
            Assert.That(_horseCarriage.CurrentWeightKg, Is.EqualTo(22.0f)); // Barley 10 * 1.5 = 15 kg + 5 kg meat + 2 kg salt = 22 kg
            _horseCarriage.TickDecay(30.0f, 1.5f);

            Assert.That(meat.Durability, Is.EqualTo(74.0f).Within(0.0001f));

            // Reach campsite: Prepare nutritious stew (2 barley, 1 meat, 1 salt)
            var stewRecipe = new List<string> { "item_golden_barley", "item_golden_barley", "item_wild_game_meat", "item_glacier_salt" };
            bool dinnerCooked = _horseCarriage.CookRecipe(stewRecipe, out string buffId, out float buffHours);

            Assert.That(dinnerCooked, Is.True);
            Assert.That(buffId, Is.EqualTo("Buff_Expedition_WarmthAndResolve"));
            Assert.That(buffHours, Is.EqualTo(24.0f));

            // Verify remaining inventory
            Assert.That(_horseCarriage.Items.First(i => i.ItemId == "item_golden_barley").Quantity, Is.EqualTo(8));
            Assert.That(_horseCarriage.Items.First(i => i.ItemId == "item_wild_game_meat").Quantity, Is.EqualTo(4));
            Assert.That(_horseCarriage.Items.First(i => i.ItemId == "item_glacier_salt").Quantity, Is.EqualTo(1));
        }

        #endregion
    }
}
