#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using InkAndAsh.Data;
using NUnit.Framework;

namespace InkAndAsh.StandaloneTests.E2ETests
{
    [TestFixture]
    public class DataCatalogE2ETests
    {
        private string _catalogsDir = string.Empty;
        private DataManager _dataManager = null!;

        [SetUp]
        public void Setup()
        {
            _dataManager = new DataManager();

            // Locate Catalogs directory across standalone test runner environments
            string baseDir = AppContext.BaseDirectory;
            string[] candidates = new[]
            {
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "Assets", "Resources", "Catalogs")),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "Assets", "Resources", "Catalogs")),
                Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Resources", "Catalogs")),
                Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "Assets", "Resources", "Catalogs"))
            };

            foreach (var candidate in candidates)
            {
                if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "items.json")))
                {
                    _catalogsDir = candidate;
                    break;
                }
            }

            Assert.That(Directory.Exists(_catalogsDir), Is.True, $"Could not locate Catalogs directory. Checked: {string.Join(", ", candidates)}");
            bool loaded = _dataManager.LoadAllCatalogsFromDirectory(_catalogsDir);
            Assert.That(loaded, Is.True, "Failed to load catalogs via DataManager.");
        }

        #region Tier 1: Feature Coverage (Baseline Happy Path)

        [Test]
        public void F1_T1_01_All16ItemsLoadedSuccessfully()
        {
            Assert.That(_dataManager.Items.Count, Is.EqualTo(16), "Expected exactly 16 items in the unified trade goods catalog.");

            string[] expectedIds = new[]
            {
                "item_golden_barley", "item_sun_dried_kelp", "item_ironwood_resin", "item_glacier_salt",
                "item_bog_ash", "item_solar_clay", "item_canopy_bloom", "item_wild_game_meat",
                "item_fresh_river_fish", "item_spring_water", "item_aromatic_herbs", "item_timber_logs",
                "item_scrap_iron", "item_coarse_fiber", "item_tallow_wax", "item_imperial_ink"
            };

            foreach (var id in expectedIds)
            {
                Assert.That(_dataManager.Items.ContainsKey(id), Is.True, $"Catalog missing required item ID: {id}");
                var item = _dataManager.GetItem(id);
                Assert.That(item, Is.Not.Null);
                Assert.That(item.name, Is.Not.Null.And.Not.Empty);
            }
        }

        [Test]
        public void F1_T1_02_ItemWeightsAndPricesPositive()
        {
            foreach (var item in _dataManager.Items.Values)
            {
                Assert.That(item.baseWeightKg, Is.GreaterThan(0f), $"Item '{item.itemId}' base weight must be strictly positive.");
                Assert.That(item.basePrice, Is.GreaterThan(0), $"Item '{item.itemId}' base price must be positive.");
                Assert.That(item.usageCategories, Is.Not.Null.And.Not.Empty, $"Item '{item.itemId}' must have at least one usage category.");
            }
        }

        [Test]
        public void F1_T1_03_PerishableItemsHavePositiveDecay()
        {
            string[] perishableIds = new[]
            {
                "item_golden_barley", "item_sun_dried_kelp", "item_canopy_bloom",
                "item_wild_game_meat", "item_fresh_river_fish", "item_aromatic_herbs", "item_tallow_wax"
            };

            foreach (var id in perishableIds)
            {
                var item = _dataManager.GetItem(id);
                Assert.That(item, Is.Not.Null, $"Missing perishable item {id}");
                Assert.That(item.isPerishable, Is.True, $"Item '{id}' must be marked as perishable.");
                Assert.That(item.decayRatePerMinute, Is.GreaterThan(0f), $"Perishable item '{id}' must have a positive decay rate.");
            }
        }

        [Test]
        public void F1_T1_04_MineralItemsHaveZeroDecay()
        {
            string[] mineralIds = new[]
            {
                "item_ironwood_resin", "item_glacier_salt", "item_bog_ash", "item_solar_clay",
                "item_spring_water", "item_timber_logs", "item_scrap_iron", "item_coarse_fiber", "item_imperial_ink"
            };

            foreach (var id in mineralIds)
            {
                var item = _dataManager.GetItem(id);
                Assert.That(item, Is.Not.Null, $"Missing non-perishable item {id}");
                Assert.That(item.isPerishable, Is.False, $"Item '{id}' must be non-perishable.");
                Assert.That(item.decayRatePerMinute, Is.EqualTo(0f), $"Non-perishable item '{id}' must have 0.0 decay rate.");
            }
        }

        [Test]
        public void F1_T1_05_NpcTiersAndPadBounds()
        {
            Assert.That(_dataManager.Npcs.Count, Is.GreaterThanOrEqualTo(3), "Expected at least 3 sample NPCs in catalog.");

            var tiersFound = new HashSet<int>();
            foreach (var npc in _dataManager.Npcs.Values)
            {
                Assert.That(npc.tier, Is.InRange(1, 3), $"NPC '{npc.npcId}' has invalid tier {npc.tier}.");
                tiersFound.Add(npc.tier);

                Assert.That(npc.padEmotionalState, Is.Not.Null, $"NPC '{npc.npcId}' missing PAD state.");
                Assert.That(npc.padEmotionalState.pleasure, Is.InRange(-100f, 100f));
                Assert.That(npc.padEmotionalState.arousal, Is.InRange(-100f, 100f));
                Assert.That(npc.padEmotionalState.dominance, Is.InRange(-100f, 100f));

                Assert.That(FactionNames.IsValidFaction(npc.factionAffiliation), Is.True,
                    $"NPC '{npc.npcId}' has unrecognized faction affiliation: {npc.factionAffiliation}");
            }

            Assert.That(tiersFound.Contains(1), Is.True, "Catalog must include at least one Tier 1 Core Cast NPC.");
            Assert.That(tiersFound.Contains(2), Is.True, "Catalog must include at least one Tier 2 Village Anchor NPC.");
            Assert.That(tiersFound.Contains(3), Is.True, "Catalog must include at least one Tier 3 Background NPC.");
        }

        [Test]
        public void F1_T1_06_QuestArcContains5Acts()
        {
            Assert.That(_dataManager.Quests.Count, Is.GreaterThanOrEqualTo(1), "Expected at least 1 sample quest in catalog.");

            foreach (var quest in _dataManager.Quests.Values)
            {
                Assert.That(quest.acts, Is.Not.Null);
                Assert.That(quest.acts.Count, Is.EqualTo(5), $"Quest '{quest.questId}' must have exactly 5 acts.");

                var actEnums = quest.acts.Select(a => a.act).ToList();
                Assert.That(actEnums, Is.EqualTo(new[]
                {
                    QuestAct.Friction,
                    QuestAct.Ripple,
                    QuestAct.Crisis,
                    QuestAct.Aftermath,
                    QuestAct.Legacy
                }), $"Quest '{quest.questId}' does not have the mandatory 5-Act progression sequence.");
            }
        }

        [Test]
        public void F1_T1_07_All7BiomesLoadedWithSpires()
        {
            Assert.That(_dataManager.Biomes.Count, Is.EqualTo(7), "Expected exactly 7 regional biomes.");

            var expectedSpires = new Dictionary<string, string>
            {
                { "biome_grasslands", "Verdant Spire" },
                { "biome_coast", "Tidal Spire" },
                { "biome_forest", "Sylvan Spire" },
                { "biome_mountains", "Frost Spire" },
                { "biome_swamp", "Mire Spire" },
                { "biome_desert", "Sun Spire" },
                { "biome_jungle", "Canopy Spire" }
            };

            foreach (var (biomeId, spireName) in expectedSpires)
            {
                var biome = _dataManager.GetBiome(biomeId);
                Assert.That(biome, Is.Not.Null, $"Biome '{biomeId}' not found.");
                Assert.That(biome.spireName, Is.EqualTo(spireName));
                Assert.That(biome.spoilageMultiplier, Is.GreaterThan(0f));
                Assert.That(biome.staminaDrainMultiplier, Is.GreaterThan(0f));
            }
        }

        [Test]
        public void F1_T1_08_DynamicBiomePriceModifiers()
        {
            var barley = _dataManager.GetItem("item_golden_barley");
            Assert.That(barley, Is.Not.Null);

            // In Grasslands (origin), price is basePrice * 1.0 = 10
            Assert.That(barley.GetPriceInBiome("The Grasslands"), Is.EqualTo(10f));

            // In The Mountains, scarce grain price is basePrice * 2.0 = 20
            Assert.That(barley.GetPriceInBiome("The Mountains"), Is.EqualTo(20f));

            // In The Desert, price is basePrice * 1.8 = 18
            Assert.That(barley.GetPriceInBiome("The Desert"), Is.EqualTo(18f));
        }

        #endregion

        #region Tier 2: Boundary & Corner Cases

        [Test]
        public void F1_T2_01_PADAtExtremeNegativeBound()
        {
            var pad = new PADState(-100f, -100f, -100f);
            Assert.That(pad.IsValid(), Is.True);
            pad.Clamp();
            Assert.That(pad.pleasure, Is.EqualTo(-100f));
            Assert.That(pad.arousal, Is.EqualTo(-100f));
            Assert.That(pad.dominance, Is.EqualTo(-100f));
        }

        [Test]
        public void F1_T2_02_PADAtExtremePositiveBound()
        {
            var pad = new PADState(100f, 100f, 100f);
            Assert.That(pad.IsValid(), Is.True);
            pad.Clamp();
            Assert.That(pad.pleasure, Is.EqualTo(100f));
            Assert.That(pad.arousal, Is.EqualTo(100f));
            Assert.That(pad.dominance, Is.EqualTo(100f));
        }

        [Test]
        public void F1_T2_03_PADOutOfRangeClampedCorrectly()
        {
            var pad = new PADState(-150f, 250f, 0f);
            Assert.That(pad.IsValid(), Is.False, "PAD state outside [-100, 100] should fail IsValid().");
            pad.Clamp();
            Assert.That(pad.pleasure, Is.EqualTo(-100f));
            Assert.That(pad.arousal, Is.EqualTo(100f));
            Assert.That(pad.dominance, Is.EqualTo(0f));
            Assert.That(pad.IsValid(), Is.True);
        }

        [Test]
        public void F1_T2_04_ZeroOrNegativeWeightItemRejected()
        {
            var invalidItem1 = new ItemData { itemId = "test_zero", name = "Test", baseWeightKg = 0f, usageCategories = new[] { "Trade" } };
            Assert.That(invalidItem1.IsValid(), Is.False);

            var invalidItem2 = new ItemData { itemId = "test_neg", name = "Test", baseWeightKg = -1.5f, usageCategories = new[] { "Trade" } };
            Assert.That(invalidItem2.IsValid(), Is.False);
        }

        [Test]
        public void F1_T2_05_NegativeDecayRateItemRejected()
        {
            var item = new ItemData
            {
                itemId = "test_decay",
                name = "Test",
                baseWeightKg = 1.0f,
                decayRatePerMinute = -0.05f,
                usageCategories = new[] { "Trade" }
            };
            Assert.That(item.IsValid(), Is.False);
        }

        [Test]
        public void F1_T2_06_QuestWithFewerThan5ActsRejected()
        {
            var quest = new QuestData
            {
                questId = "quest_short",
                npcId = "npc_test",
                acts = new List<QuestActData>
                {
                    new QuestActData { actId = "a1", act = QuestAct.Friction },
                    new QuestActData { actId = "a2", act = QuestAct.Ripple }
                }
            };

            Assert.That(quest.IsValid(), Is.False);
        }

        [Test]
        public void F1_T2_07_MissingFileHandledGracefullyWithoutException()
        {
            var dm = new DataManager();
            bool result = dm.LoadAllCatalogsFromDirectory("C:\\Path\\That\\Does\\Not\\Exist_9999");
            Assert.That(result, Is.False);
            Assert.That(dm.Items.Count, Is.EqualTo(0));
            Assert.That(dm.GetItem("any_id"), Is.Null);
        }

        [Test]
        public void F1_T2_08_MalformedJsonReturnsFalseGracefully()
        {
            var dm = new DataManager();
            bool result = dm.LoadItems("{ malformed json, unquoted keys }");
            Assert.That(result, Is.False);
            Assert.That(dm.Items.Count, Is.EqualTo(0));
        }

        [Test]
        public void F1_T2_09_UnknownFactionDefaultsToOutsiders()
        {
            var npc = new NpcData
            {
                npcId = "npc_rebel",
                name = "Rebel Scout",
                tier = 3,
                factionAffiliation = "UnknownRebels",
                padEmotionalState = new PADState(0, 0, 0)
            };

            npc.ValidateAndClamp();
            Assert.That(npc.factionAffiliation, Is.EqualTo(FactionNames.Outsiders));
        }

        #endregion

        #region Tier 3: Cross-Feature Combinations

        [Test]
        public void F1_T3_01_ItemPrimaryBiomesExistInBiomeCatalog()
        {
            var validBiomeNames = new HashSet<string>(_dataManager.Biomes.Values.Select(b => b.name), StringComparer.OrdinalIgnoreCase);
            validBiomeNames.Add("Universal");
            validBiomeNames.Add("Imperial Posts");

            foreach (var item in _dataManager.Items.Values)
            {
                Assert.That(validBiomeNames.Contains(item.primaryBiome), Is.True,
                    $"Item '{item.itemId}' primary biome '{item.primaryBiome}' is not in the recognized world biomes matrix.");
            }
        }

        [Test]
        public void F1_T3_02_NpcHomeBiomesExistInBiomeCatalog()
        {
            var validBiomeNames = new HashSet<string>(_dataManager.Biomes.Values.Select(b => b.name), StringComparer.OrdinalIgnoreCase);

            foreach (var npc in _dataManager.Npcs.Values)
            {
                Assert.That(validBiomeNames.Contains(npc.homeBiome), Is.True,
                    $"NPC '{npc.npcId}' home biome '{npc.homeBiome}' does not match any valid regional biome.");
            }
        }

        [Test]
        public void F1_T3_03_QuestTargetNpcExistsInNpcCatalog()
        {
            foreach (var quest in _dataManager.Quests.Values)
            {
                Assert.That(_dataManager.Npcs.ContainsKey(quest.npcId), Is.True,
                    $"Quest '{quest.questId}' references non-existent NPC '{quest.npcId}'.");
            }
        }

        [Test]
        public void F1_T3_04_QuestActVGrantsValidPermanentBoon()
        {
            var quest = _dataManager.GetQuest("quest_kaelen_grain_crisis");
            Assert.That(quest, Is.Not.Null);

            var actV = quest!.acts.Find(a => a.act == QuestAct.Legacy);
            Assert.That(actV, Is.Not.Null);
            Assert.That(actV!.reward, Is.Not.Null);
            Assert.That(actV.reward.unlockedBoon, Is.Not.Null.And.Not.Empty);
            Assert.That(actV.reward.unlockedBoon, Is.EqualTo("Boon_Free_Village_Smithy_Repairs"));
        }

        #endregion

        #region Tier 4: Real-World Scenarios

        [Test]
        public void F1_T4_01_CompleteCatalogLifecycleAudit()
        {
            // Verify all data files load with zero parse exceptions
            Assert.That(_dataManager.IsLoaded, Is.True);
            Assert.That(_dataManager.Items.Count, Is.EqualTo(16));
            Assert.That(_dataManager.Npcs.Count, Is.GreaterThanOrEqualTo(3));
            Assert.That(_dataManager.Quests.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(_dataManager.Biomes.Count, Is.EqualTo(7));

            // Verify non-null zero-allocation retrieval
            Assert.That(_dataManager.GetItem("item_golden_barley"), Is.Not.Null);
            Assert.That(_dataManager.GetNpc("npc_anchor_kael_01"), Is.Not.Null);
            Assert.That(_dataManager.GetQuest("quest_kaelen_grain_crisis"), Is.Not.Null);
            Assert.That(_dataManager.GetBiome("biome_grasslands"), Is.Not.Null);
        }

        [Test]
        public void F1_T4_02_Simulated5ActQuestProgression()
        {
            var quest = _dataManager.GetQuest("quest_kaelen_grain_crisis");
            Assert.That(quest, Is.Not.Null);

            // Start at Act I Friction
            Assert.That(quest.currentAct, Is.EqualTo(QuestAct.Friction));

            // Complete Act I -> advances to Ripple
            bool adv1 = quest.CompleteCurrentAct();
            Assert.That(adv1, Is.True);
            Assert.That(quest.currentAct, Is.EqualTo(QuestAct.Ripple));
            Assert.That(quest.actCompletionFlags["Act_I_Friction"], Is.True);

            // Complete Act II -> advances to Crisis
            bool adv2 = quest.CompleteCurrentAct();
            Assert.That(adv2, Is.True);
            Assert.That(quest.currentAct, Is.EqualTo(QuestAct.Crisis));
            Assert.That(quest.actCompletionFlags["Act_II_Ripple"], Is.True);

            // Complete Act III -> advances to Aftermath
            bool adv3 = quest.CompleteCurrentAct();
            Assert.That(adv3, Is.True);
            Assert.That(quest.currentAct, Is.EqualTo(QuestAct.Aftermath));
            Assert.That(quest.actCompletionFlags["Act_III_Crisis"], Is.True);

            // Complete Act IV -> advances to Legacy
            bool adv4 = quest.CompleteCurrentAct();
            Assert.That(adv4, Is.True);
            Assert.That(quest.currentAct, Is.EqualTo(QuestAct.Legacy));
            Assert.That(quest.actCompletionFlags["Act_IV_Aftermath"], Is.True);

            // Complete Act V -> finishes quest, stays on Legacy
            bool adv5 = quest.CompleteCurrentAct();
            Assert.That(adv5, Is.False, "Act V Legacy is terminal and cannot advance further.");
            Assert.That(quest.currentAct, Is.EqualTo(QuestAct.Legacy));
            Assert.That(quest.actCompletionFlags["Act_V_Legacy"], Is.True);
        }

        #endregion
    }
}
