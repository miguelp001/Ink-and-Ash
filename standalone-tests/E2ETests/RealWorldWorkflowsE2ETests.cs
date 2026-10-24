#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using InkAndAsh.Data;
using InkAndAsh.StandaloneTests.E2ETests.Contracts;
using NUnit.Framework;

namespace InkAndAsh.StandaloneTests.E2ETests
{
    [TestFixture]
    public class RealWorldWorkflowsE2ETests
    {
        private DataManager _dataManager = null!;

        [SetUp]
        public void Setup()
        {
            _dataManager = new DataManager();

            string baseDir = AppContext.BaseDirectory;
            string[] candidates = new[]
            {
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "Assets", "Resources", "Catalogs")),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "Assets", "Resources", "Catalogs")),
                Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Resources", "Catalogs")),
                Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "Assets", "Resources", "Catalogs"))
            };

            string catalogDir = candidates.FirstOrDefault(c => Directory.Exists(c) && File.Exists(Path.Combine(c, "items.json"))) ?? string.Empty;
            Assert.That(Directory.Exists(catalogDir), Is.True, "Could not locate Catalogs directory.");
            _dataManager.LoadAllCatalogsFromDirectory(catalogDir);
        }

        #region Tier 4: Real-World Application Scenarios (5 Workflows)

        [Test]
        public void Workflow_01_OverworldGrainPilgrimageAcrossThreeBiomes()
        {
            var carriage = new CaravanInventory(StorageTier.HorseCarriage);
            var weather = new WeatherSystem();
            var barleyData = _dataManager.GetItem("item_golden_barley");
            Assert.That(barleyData, Is.Not.Null);

            // Phase 1: Grasslands Market
            weather.SetBiome("biome_grasslands");
            Assert.That(weather.CurrentWeather, Is.EqualTo(WeatherType.Clear));
            float buyPrice = barleyData!.GetPriceInBiome("The Grasslands");
            Assert.That(buyPrice, Is.EqualTo(10f)); // 10 gold base

            // Load 20 units (20 kg) of Golden Barley
            bool loaded = carriage.AddItem(barleyData.itemId, barleyData.name, barleyData.baseWeightKg, quantity: 20, isAbsorbent: true);
            Assert.That(loaded, Is.True);
            Assert.That(carriage.CurrentWeightKg, Is.EqualTo(20.0f));

            // Phase 2: Journey through Swamp under Heavy Rain
            weather.SetBiome("biome_swamp");
            carriage.SetHumidityWeather(true);
            // Moisture penalty: +50% weight on absorbent grain
            Assert.That(carriage.CurrentWeightKg, Is.EqualTo(30.0f));

            // Travel 30 real-time minutes in swamp rain (1.5x spoilage):
            // Carriage decay: 30 * 1.0% * 1.5 * 0.40 = 18.0% lost
            carriage.TickDecay(30.0f, weather.SpoilageMultiplier);
            var barleyCargo = carriage.Items.First(i => i.ItemId == barleyData.itemId);
            Assert.That(barleyCargo.Durability, Is.EqualTo(82.0f).Within(0.0001f));
            Assert.That(barleyCargo.IsSpoiled, Is.False);

            // Phase 3: Transition into The Desert (Sun Spire Heat)
            weather.SetBiome("biome_desert");
            carriage.SetHumidityWeather(false); // Dries back out
            Assert.That(carriage.CurrentWeightKg, Is.EqualTo(20.0f));

            // Travel 20 real-time minutes in desert heat (1.5x spoilage):
            // Carriage decay: 20 * 1.0% * 1.5 * 0.40 = 12.0% lost
            carriage.TickDecay(20.0f, weather.SpoilageMultiplier);
            Assert.That(barleyCargo.Durability, Is.EqualTo(70.0f).Within(0.0001f));
            Assert.That(barleyCargo.IsSpoiled, Is.False);

            // Phase 4: Sell to Desert Outpost at Scarcity Price
            float sellPrice = barleyData.GetPriceInBiome("The Desert");
            Assert.That(sellPrice, Is.EqualTo(18f)); // +80% margin in desert!
            float totalRevenue = sellPrice * barleyCargo.Quantity;
            Assert.That(totalRevenue, Is.EqualTo(360f));
        }

        [Test]
        public void Workflow_02_TacticalCtbBossEncounterWithSynergy()
        {
            var combatEngine = new CtbCombatEngine();

            var vicar = new Combatant { Id = "hero_vicar", Name = "Vicar", IsAlly = true, BaseAgility = 50f, FocusPoints = 50f, NextTurnTime = 2.0f };
            var weaver = new Combatant { Id = "hero_weaver", Name = "Weaver", IsAlly = true, BaseAgility = 40f, FocusPoints = 30f, NextTurnTime = 2.5f };
            var ashBeast = new Combatant { Id = "boss_ash_beast", Name = "Ash Behemoth", IsAlly = false, BaseAgility = 60f, CurrentHealth = 200f, NextTurnTime = 1.67f };

            combatEngine.AddCombatant(ashBeast);
            combatEngine.AddCombatant(vicar);
            combatEngine.AddCombatant(weaver);

            // Round 1: Ash Behemoth strikes first at t=1.67
            Assert.That(combatEngine.Timeline[0].Id, Is.EqualTo("boss_ash_beast"));
            combatEngine.CommitAction(ashBeast, 100, 1.0f); // New time = 1.67 + (100/60) = 3.3367

            // Round 2: Vicar turn at t=2.0
            Assert.That(combatEngine.Timeline[0].Id, Is.EqualTo("hero_vicar"));

            // Vicar performs Intervention to accelerate Weaver
            weaver.NextTurnTime = 16.0f; // Boss delayed Weaver
            bool intervened = combatEngine.PerformIntervention(vicar, weaver);
            Assert.That(intervened, Is.True);
            Assert.That(vicar.FocusPoints, Is.EqualTo(25.0f));
            Assert.That(weaver.NextTurnTime, Is.EqualTo(2.0f).Within(0.01f)); // Accelerated to head!

            // Vicar establishes Mediator's Anchor linking Weaver
            combatEngine.SetMediatorsAnchor(vicar, weaver);
            Assert.That(combatEngine.CheckSyncMoveAvailable(vicar, weaver), Is.True);

            // Execute synchronized tandem strike: 120 damage
            ashBeast.CurrentHealth -= 120f;
            combatEngine.CommitAction(vicar, 50, 1.0f); // Vicar fast skill: 2.0 + 1.0 = 3.0

            // Round 3: Weaver acts at t=2.0
            Assert.That(combatEngine.Timeline[0].Id, Is.EqualTo("hero_weaver"));
            ashBeast.CurrentHealth -= 80f; // Finishing blow!
            Assert.That(ashBeast.CurrentHealth, Is.EqualTo(0f));

            combatEngine.RemoveCombatant(ashBeast.Id);
            Assert.That(combatEngine.Timeline.Count, Is.EqualTo(2));
            Assert.That(combatEngine.Timeline.All(c => c.IsAlly), Is.True);
        }

        [Test]
        public void Workflow_03_PartyDefeatCampReloadAndRestorativeRecovery()
        {
            var combatEngine = new CtbCombatEngine();
            var caravan = new CaravanInventory(StorageTier.HorseCarriage);
            var timeManager = new TimeManager();

            var hero = new Combatant { Id = "hero_vicar", Name = "Vicar", IsAlly = true, BaseAgility = 50f, CurrentHealth = 0f };

            // Step 1: Party wipeout triggers reload at campsite with Injury Debuff
            combatEngine.ApplyInjuryDebuff(hero);
            Assert.That(hero.HasInjuryDebuff, Is.True);
            Assert.That(hero.EffectiveAgility, Is.EqualTo(40.0f)); // -20% Agility

            // Increased turn delay: (100 * 1.15) / 40 = 2.875 (+43.75%)
            float injuredDelay = combatEngine.CalculateTurnDelay(hero, 100);
            Assert.That(injuredDelay, Is.EqualTo(2.875f).Within(0.0001f));

            // Step 2: Campsite Cooking of nourishing meal
            caravan.AddItem("item_golden_barley", "Barley", 1.0f, quantity: 2, durability: 100f);
            caravan.AddItem("item_glacier_salt", "Salt", 1.5f, quantity: 1, durability: 100f, isPerishable: false);

            var mealRecipe = new List<string> { "item_golden_barley", "item_golden_barley", "item_glacier_salt" };
            bool cooked = caravan.CookRecipe(mealRecipe, out string buffId, out float buffHours);
            Assert.That(cooked, Is.True);
            Assert.That(buffHours, Is.EqualTo(24.0f));

            // Step 3: Sleep 8 hours to recover
            timeManager.AdvanceTime(8.0f);

            // Step 4: Injury Debuff cleared after rest & meal
            combatEngine.ClearInjuryDebuff(hero);
            Assert.That(hero.HasInjuryDebuff, Is.False);
            Assert.That(hero.EffectiveAgility, Is.EqualTo(50.0f));
            Assert.That(combatEngine.CalculateTurnDelay(hero, 100), Is.EqualTo(2.0f).Within(0.0001f));
        }

        [Test]
        public void Workflow_04_TheMillAndTheMeasureComplete5ActQuestLifecycle()
        {
            var quest = _dataManager.GetQuest("quest_kaelen_grain_crisis");
            Assert.That(quest, Is.Not.Null);

            // Verify NPC anchor
            var kaelen = _dataManager.GetNpc(quest!.npcId);
            Assert.That(kaelen, Is.Not.Null);
            Assert.That(kaelen!.tier, Is.EqualTo(2)); // Village Anchor

            // Act I: Friction (Delivering relief grain)
            Assert.That(quest.currentAct, Is.EqualTo(QuestAct.Friction));
            bool act1Done = quest.CompleteCurrentAct();
            Assert.That(act1Done, Is.True);
            Assert.That(quest.currentAct, Is.EqualTo(QuestAct.Ripple));

            // Act II: Ripple (Mediating elders)
            bool act2Done = quest.CompleteCurrentAct();
            Assert.That(act2Done, Is.True);
            Assert.That(quest.currentAct, Is.EqualTo(QuestAct.Crisis));

            // Act III: Crisis (Midnight inspection standoff)
            bool act3Done = quest.CompleteCurrentAct();
            Assert.That(act3Done, Is.True);
            Assert.That(quest.currentAct, Is.EqualTo(QuestAct.Aftermath));

            // Act IV: Aftermath (Communal granary)
            bool act4Done = quest.CompleteCurrentAct();
            Assert.That(act4Done, Is.True);
            Assert.That(quest.currentAct, Is.EqualTo(QuestAct.Legacy));

            // Act V: Legacy (Permanent smithy repairs boon unlocked)
            var actVData = quest.acts.Find(a => a.act == QuestAct.Legacy);
            Assert.That(actVData, Is.Not.Null);
            Assert.That(actVData!.reward.unlockedBoon, Is.EqualTo("Boon_Free_Village_Smithy_Repairs"));
            Assert.That(actVData.reward.insightPoints, Is.EqualTo(100));

            // Apply PAD delta from quest completion to Kaelen
            kaelen.padEmotionalState.ApplyDelta(actVData.reward.padDelta);
            Assert.That(kaelen.padEmotionalState.IsValid(), Is.True);
        }

        [Test]
        public void Workflow_05_FrostSpireAscentAndRegionalWeatherStabilization()
        {
            var weather = new WeatherSystem();
            var combatEngine = new CtbCombatEngine();

            var vicar = new Combatant { Id = "hero_vicar", BaseAgility = 50f };

            // Step 1: Enter The Mountains
            weather.SetBiome("biome_mountains");
            Assert.That(weather.CurrentWeather, Is.EqualTo(WeatherType.Freezing));
            Assert.That(weather.SpoilageMultiplier, Is.EqualTo(0.2f));
            Assert.That(weather.StaminaMultiplier, Is.EqualTo(2.0f));
            Assert.That(weather.CombatDelayMultiplier, Is.EqualTo(1.25f));

            // CTB turn delay is penalized: (100 / 50) * 1.25 = 2.5
            float stormDelay = combatEngine.CalculateTurnDelay(vicar, 100, weather.CombatDelayMultiplier);
            Assert.That(stormDelay, Is.EqualTo(2.5f).Within(0.0001f));

            // Step 2: Ascend peak, clear anomalies, light ancient Spire beacon
            bool beaconLit = false;
            weather.OnSpireCalibrated += (biome, spire) => beaconLit = true;

            weather.CalibrateSpire();

            // Step 3: Verify world state calms
            Assert.That(beaconLit, Is.True);
            Assert.That(weather.SpireCalibrated, Is.True);
            Assert.That(weather.CurrentWeather, Is.EqualTo(WeatherType.Clear));
            Assert.That(weather.SpoilageMultiplier, Is.EqualTo(1.0f));
            Assert.That(weather.StaminaMultiplier, Is.EqualTo(1.0f));
            Assert.That(weather.CombatDelayMultiplier, Is.EqualTo(1.0f));

            // CTB turn delay returns to baseline: (100 / 50) * 1.0 = 2.0
            float calmDelay = combatEngine.CalculateTurnDelay(vicar, 100, weather.CombatDelayMultiplier);
            Assert.That(calmDelay, Is.EqualTo(2.0f).Within(0.0001f));
        }

        #endregion
    }
}
