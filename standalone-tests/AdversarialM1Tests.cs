using System;
using System.Collections.Generic;
using System.IO;
using InkAndAsh.Data;
using Newtonsoft.Json;
using NUnit.Framework;

namespace InkAndAsh.StandaloneTests
{
    [TestFixture]
    public class AdversarialM1Tests
    {
        private string _catalogsPath;
        private DataManager _manager;

        [SetUp]
        public void Setup()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string probePath = Path.Combine(baseDir, "Assets", "Resources", "Catalogs");
            if (!Directory.Exists(probePath))
            {
                probePath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "Assets", "Resources", "Catalogs"));
            }
            if (!Directory.Exists(probePath))
            {
                probePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Resources", "Catalogs"));
            }

            _catalogsPath = probePath;
            Assert.That(Directory.Exists(_catalogsPath), Is.True, $"Catalogs directory must exist at {_catalogsPath}");

            _manager = new DataManager();
            bool loaded = _manager.LoadAllCatalogsFromDirectory(_catalogsPath);
            Assert.That(loaded, Is.True, "Catalogs should load successfully.");
        }

        #region Dispatch Question 1: PAD Coordinates & Clamping

        [Test]
        public void PAD_ExtremeOutOfRangeValues_ClampToMinus100AndPlus100()
        {
            var padNegative = new PADState(-9999f, -100.01f, -1e20f);
            Assert.That(padNegative.IsValid(), Is.False, "Out-of-range negative PAD must be invalid.");
            padNegative.Clamp();
            Assert.That(padNegative.pleasure, Is.EqualTo(-100f));
            Assert.That(padNegative.arousal, Is.EqualTo(-100f));
            Assert.That(padNegative.dominance, Is.EqualTo(-100f));
            Assert.That(padNegative.IsValid(), Is.True, "Clamped PAD must be valid.");

            var padPositive = new PADState(9999f, 100.01f, 1e20f);
            Assert.That(padPositive.IsValid(), Is.False, "Out-of-range positive PAD must be invalid.");
            padPositive.Clamp();
            Assert.That(padPositive.pleasure, Is.EqualTo(100f));
            Assert.That(padPositive.arousal, Is.EqualTo(100f));
            Assert.That(padPositive.dominance, Is.EqualTo(100f));
            Assert.That(padPositive.IsValid(), Is.True);
        }

        [Test]
        public void PAD_BoundaryEdgeValues_Minus100AndPlus100AreValid()
        {
            var minPad = new PADState(-100f, -100f, -100f);
            Assert.That(minPad.IsValid(), Is.True);
            minPad.Clamp();
            Assert.That(minPad.pleasure, Is.EqualTo(-100f));

            var maxPad = new PADState(100f, 100f, 100f);
            Assert.That(maxPad.IsValid(), Is.True);
            maxPad.Clamp();
            Assert.That(maxPad.pleasure, Is.EqualTo(100f));
        }

        [Test]
        public void PAD_ApplyDelta_OverAndUnderflow_ClampsProperly()
        {
            var pad = new PADState(90f, -90f, 0f);
            var delta = new PADState(30f, -30f, 50f);
            pad.ApplyDelta(delta);

            Assert.That(pad.pleasure, Is.EqualTo(100f), "90 + 30 should clamp to 100.");
            Assert.That(pad.arousal, Is.EqualTo(-100f), "-90 + -30 should clamp to -100.");
            Assert.That(pad.dominance, Is.EqualTo(50f));
            Assert.That(pad.IsValid(), Is.True);
        }

        [Test]
        public void PAD_ApplyNullDelta_DoesNotThrow()
        {
            var pad = new PADState(10f, 20f, 30f);
            Assert.DoesNotThrow(() => pad.ApplyDelta(null));
            Assert.That(pad.pleasure, Is.EqualTo(10f));
        }

        [Test]
        public void Npc_ValidateAndClamp_ClampsPADAndRelationshipVectorsAndSanitizesFaction()
        {
            var npc = new NpcData
            {
                npcId = "npc_stress_01",
                tier = 1,
                name = "Stress NPC",
                homeBiome = "The Mountains",
                factionAffiliation = "NonExistentFaction",
                padEmotionalState = new PADState(-250f, 300f, 0f),
                relationshipVectors = new RelationshipVectors(-50f, 150f)
            };

            Assert.That(npc.IsValid(), Is.True);
            Assert.That(npc.padEmotionalState.IsValid(), Is.False);
            Assert.That(npc.relationshipVectors.IsValid(), Is.False);

            npc.ValidateAndClamp();

            Assert.That(npc.padEmotionalState.pleasure, Is.EqualTo(-100f));
            Assert.That(npc.padEmotionalState.arousal, Is.EqualTo(100f));
            Assert.That(npc.padEmotionalState.dominance, Is.EqualTo(0f));
            Assert.That(npc.padEmotionalState.IsValid(), Is.True);

            Assert.That(npc.relationshipVectors.imperialFear, Is.EqualTo(0f));
            Assert.That(npc.relationshipVectors.factionTrust, Is.EqualTo(100f));
            Assert.That(npc.relationshipVectors.IsValid(), Is.True);

            Assert.That(npc.factionAffiliation, Is.EqualTo(FactionNames.Outsiders), "Invalid faction must fallback to Outsiders.");
        }

        [Test]
        public void DataManager_LoadNpcs_WithOutOfRangePAD_ClampsUponLoading()
        {
            string json = @"[
                {
                    ""npcId"": ""npc_out_of_bounds"",
                    ""tier"": 2,
                    ""name"": ""Bounds Tester"",
                    ""homeBiome"": ""The Desert"",
                    ""factionAffiliation"": ""Pragmatists"",
                    ""padEmotionalState"": { ""pleasure"": -500.0, ""arousal"": 500.0, ""dominance"": 999.0 },
                    ""relationshipVectors"": { ""imperialFear"": -20.0, ""factionTrust"": 200.0 }
                }
            ]";

            var localDm = new DataManager();
            bool result = localDm.LoadNpcs(json);

            Assert.That(result, Is.True);
            var loaded = localDm.GetNpc("npc_out_of_bounds");
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.padEmotionalState.pleasure, Is.EqualTo(-100f));
            Assert.That(loaded.padEmotionalState.arousal, Is.EqualTo(100f));
            Assert.That(loaded.padEmotionalState.dominance, Is.EqualTo(100f));
            Assert.That(loaded.padEmotionalState.IsValid(), Is.True);
            Assert.That(loaded.relationshipVectors.imperialFear, Is.EqualTo(0f));
            Assert.That(loaded.relationshipVectors.factionTrust, Is.EqualTo(100f));
        }

        #endregion

        #region Dispatch Question 2: Querying Non-Existent Entity IDs

        [Test]
        public void QueryNonExistent_Item_ReturnsNullWithoutException()
        {
            Assert.That(_manager.GetItem("item_does_not_exist"), Is.Null);
            Assert.That(_manager.GetItem(""), Is.Null);
            Assert.That(_manager.GetItem(null), Is.Null);
            Assert.That(_manager.GetItem("   "), Is.Null);
            Assert.That(_manager.GetItem("item_golden_barley_extra_suffix"), Is.Null);
        }

        [Test]
        public void QueryNonExistent_Npc_ReturnsNullWithoutException()
        {
            Assert.That(_manager.GetNpc("npc_fake_999"), Is.Null);
            Assert.That(_manager.GetNpc(""), Is.Null);
            Assert.That(_manager.GetNpc(null), Is.Null);
            Assert.That(_manager.GetNpc("   "), Is.Null);
        }

        [Test]
        public void QueryNonExistent_Biome_ReturnsNullWithoutException()
        {
            Assert.That(_manager.GetBiome("biome_moon"), Is.Null);
            Assert.That(_manager.GetBiome(""), Is.Null);
            Assert.That(_manager.GetBiome(null), Is.Null);
            Assert.That(_manager.GetBiome("   "), Is.Null);
        }

        [Test]
        public void QueryNonExistent_Quest_ReturnsNullWithoutException()
        {
            Assert.That(_manager.GetQuest("quest_fake_arc"), Is.Null);
            Assert.That(_manager.GetQuest(""), Is.Null);
            Assert.That(_manager.GetQuest(null), Is.Null);
            Assert.That(_manager.GetQuest("   "), Is.Null);
        }

        [Test]
        public void QueryCaseInsensitive_EntityLookups_ResolveCorrectly()
        {
            Assert.That(_manager.GetItem("ITEM_GOLDEN_BARLEY"), Is.Not.Null);
            Assert.That(_manager.GetItem("Item_Golden_Barley"), Is.Not.Null);
            Assert.That(_manager.GetNpc("NPC_CORE_VESPER_01"), Is.Not.Null);
            Assert.That(_manager.GetBiome("BIOME_GRASSLANDS"), Is.Not.Null);
            Assert.That(_manager.GetQuest("QUEST_KAELEN_GRAIN_CRISIS"), Is.Not.Null);
        }

        #endregion

        #region Dispatch Question 3: Extreme Perishable Decay Rates and Negative Weights

        [Test]
        public void ItemData_NegativeWeight_RejectedByIsValid()
        {
            var item = new ItemData
            {
                itemId = "item_neg_wt",
                name = "Negative Weight Item",
                baseWeightKg = -5.0f,
                decayRatePerMinute = 0.01f,
                usageCategories = new[] { "Trade" }
            };
            Assert.That(item.IsValid(), Is.False, "baseWeightKg <= 0 must fail IsValid.");
        }

        [Test]
        public void ItemData_ZeroWeight_RejectedByIsValid()
        {
            var item = new ItemData
            {
                itemId = "item_zero_wt",
                name = "Zero Weight Item",
                baseWeightKg = 0.0f,
                decayRatePerMinute = 0.01f,
                usageCategories = new[] { "Trade" }
            };
            Assert.That(item.IsValid(), Is.False, "baseWeightKg == 0 must fail IsValid.");
        }

        [Test]
        public void ItemData_NegativeDecayRate_RejectedByIsValid()
        {
            var item = new ItemData
            {
                itemId = "item_neg_decay",
                name = "Negative Decay Item",
                baseWeightKg = 1.0f,
                decayRatePerMinute = -0.05f,
                usageCategories = new[] { "Trade" }
            };
            Assert.That(item.IsValid(), Is.False, "decayRatePerMinute < 0 must fail IsValid.");
        }

        [Test]
        public void ItemData_ExtremeHighDecayRate_AcceptedIfNonNegative()
        {
            var item = new ItemData
            {
                itemId = "item_extreme_decay",
                name = "Extreme Decay Item",
                baseWeightKg = 1.0f,
                decayRatePerMinute = 10000.0f,
                usageCategories = new[] { "Trade" }
            };
            Assert.That(item.IsValid(), Is.True, "Positive extreme decay rate is technically valid data.");
        }

        [Test]
        public void DataManager_LoadItems_SkipsNegativeWeightAndDecayItems()
        {
            string json = @"[
                {
                    ""itemId"": ""item_good"",
                    ""name"": ""Good Item"",
                    ""baseWeightKg"": 1.0,
                    ""decayRatePerMinute"": 0.01,
                    ""usageCategories"": [""Trade""]
                },
                {
                    ""itemId"": ""item_bad_weight"",
                    ""name"": ""Bad Weight Item"",
                    ""baseWeightKg"": -2.5,
                    ""decayRatePerMinute"": 0.01,
                    ""usageCategories"": [""Trade""]
                },
                {
                    ""itemId"": ""item_bad_decay"",
                    ""name"": ""Bad Decay Item"",
                    ""baseWeightKg"": 1.0,
                    ""decayRatePerMinute"": -1.0,
                    ""usageCategories"": [""Trade""]
                },
                {
                    ""itemId"": ""item_empty_cat"",
                    ""name"": ""No Categories Item"",
                    ""baseWeightKg"": 1.0,
                    ""decayRatePerMinute"": 0.0,
                    ""usageCategories"": []
                }
            ]";

            var localDm = new DataManager();
            bool result = localDm.LoadItems(json);

            Assert.That(result, Is.True);
            Assert.That(localDm.Items.Count, Is.EqualTo(1));
            Assert.That(localDm.GetItem("item_good"), Is.Not.Null);
            Assert.That(localDm.GetItem("item_bad_weight"), Is.Null);
            Assert.That(localDm.GetItem("item_bad_decay"), Is.Null);
            Assert.That(localDm.GetItem("item_empty_cat"), Is.Null);
        }

        #endregion

        #region Dispatch Question 4: Parsing and Looking Up All 16 Items

        [Test]
        public void All16Items_ParsedAndLookedUpWithoutException()
        {
            string[] all16Ids = new[]
            {
                "item_golden_barley",
                "item_sun_dried_kelp",
                "item_ironwood_resin",
                "item_glacier_salt",
                "item_bog_ash",
                "item_solar_clay",
                "item_canopy_bloom",
                "item_wild_game_meat",
                "item_fresh_river_fish",
                "item_spring_water",
                "item_aromatic_herbs",
                "item_timber_logs",
                "item_scrap_iron",
                "item_coarse_fiber",
                "item_tallow_wax",
                "item_imperial_ink"
            };

            Assert.That(_manager.Items.Count, Is.EqualTo(16), "Exactly 16 items must be present.");

            foreach (var id in all16Ids)
            {
                ItemData item = null;
                Assert.DoesNotThrow(() => { item = _manager.GetItem(id); }, $"Looking up {id} must not throw.");
                Assert.That(item, Is.Not.Null, $"Item {id} must exist in catalog.");
                Assert.That(item.itemId, Is.EqualTo(id));
                Assert.That(item.name, Is.Not.Null.And.Not.Empty);
                Assert.That(item.primaryBiome, Is.Not.Null.And.Not.Empty);
                Assert.That(item.baseWeightKg, Is.GreaterThan(0f));
                Assert.That(item.usageCategories, Is.Not.Null.And.Not.Empty);

                if (item.isPerishable)
                {
                    Assert.That(item.decayRatePerMinute, Is.GreaterThan(0f), $"Perishable item {id} must have decay rate > 0.");
                }
                else
                {
                    Assert.That(item.decayRatePerMinute, Is.EqualTo(0f), $"Non-perishable item {id} must have decay rate == 0.");
                }
            }
        }

        #endregion

        #region Additional Stress & Adversarial Edge Cases

        [Test]
        public void DataManager_LoadMalformedJson_DoesNotThrowUnhandledException()
        {
            var dm = new DataManager();
            Assert.DoesNotThrow(() => dm.LoadItems("THIS IS NOT JSON {{{"));
            Assert.DoesNotThrow(() => dm.LoadNpcs("{ \"unclosed\": [}"));
            Assert.DoesNotThrow(() => dm.LoadQuests(""));
            Assert.DoesNotThrow(() => dm.LoadBiomes("   "));
            Assert.DoesNotThrow(() => dm.LoadItems(null));

            Assert.That(dm.Items.Count, Is.EqualTo(0));
            Assert.That(dm.Npcs.Count, Is.EqualTo(0));
            Assert.That(dm.Quests.Count, Is.EqualTo(0));
            Assert.That(dm.Biomes.Count, Is.EqualTo(0));
        }

        [Test]
        public void DataManager_LoadArrayWithNullElements_DoesNotCrash()
        {
            string json = @"[
                null,
                {
                    ""itemId"": ""item_survivor"",
                    ""name"": ""Survivor Item"",
                    ""baseWeightKg"": 1.0,
                    ""decayRatePerMinute"": 0.0,
                    ""usageCategories"": [""Trade""]
                },
                null
            ]";

            var dm = new DataManager();
            bool result = dm.LoadItems(json);

            Assert.That(result, Is.True);
            Assert.That(dm.Items.Count, Is.EqualTo(1));
            Assert.That(dm.GetItem("item_survivor"), Is.Not.Null);
        }

        [Test]
        public void ItemData_GetPriceInBiome_NullBiomeName_HandledOrIdentified()
        {
            var barley = _manager.GetItem("item_golden_barley");
            Assert.That(barley, Is.Not.Null);

            // Adversarial check: querying price with non-existent biome name should return base price
            float unknownBiomePrice = barley.GetPriceInBiome("The Moon");
            Assert.That(unknownBiomePrice, Is.EqualTo(barley.basePrice));

            // Adversarial check: querying with null biomeName
            // Note: In C# Dictionary.TryGetValue(null, out _), an ArgumentNullException is thrown if biomeName is null.
            // Let's verify whether it throws or handles null:
            Assert.Throws<ArgumentNullException>(() => barley.GetPriceInBiome(null),
                "Passing null to Dictionary.TryGetValue without a null-check causes ArgumentNullException.");
        }

        [Test]
        public void QuestData_5ActProgression_BeyondLegacy_ReturnsFalse()
        {
            var quest = _manager.GetQuest("quest_kaelen_grain_crisis");
            Assert.That(quest, Is.Not.Null);

            // Advance through all 5 acts
            quest.CompleteCurrentAct(); // 1 -> 2
            quest.CompleteCurrentAct(); // 2 -> 3
            quest.CompleteCurrentAct(); // 3 -> 4
            quest.CompleteCurrentAct(); // 4 -> 5 (Legacy)

            Assert.That(quest.currentAct, Is.EqualTo(QuestAct.Legacy));

            // Advancing past Legacy must return false and not overflow enum
            bool beyond = quest.CompleteCurrentAct();
            Assert.That(beyond, Is.False, "Advancing past Legacy must return false.");
            Assert.That(quest.currentAct, Is.EqualTo(QuestAct.Legacy), "Act must stay on Legacy.");
        }

        [Test]
        public void BiomeData_NegativeSpoilageMultiplier_FailsValidation()
        {
            var biome = new BiomeData
            {
                biomeId = "biome_bad",
                name = "Bad Biome",
                spireName = "Bad Spire",
                spoilageMultiplier = -0.5f,
                staminaDrainMultiplier = 1.0f
            };
            Assert.That(biome.IsValid(), Is.False, "Negative spoilage multiplier must fail IsValid.");
        }

        [Test]
        public void BiomeData_ZeroOrNegativeStaminaDrain_FailsValidation()
        {
            var biome1 = new BiomeData
            {
                biomeId = "biome_zero_stamina",
                name = "Zero Stamina Biome",
                spireName = "Spire",
                spoilageMultiplier = 1.0f,
                staminaDrainMultiplier = 0.0f
            };
            Assert.That(biome1.IsValid(), Is.False, "Zero stamina drain multiplier must fail IsValid.");

            var biome2 = new BiomeData
            {
                biomeId = "biome_neg_stamina",
                name = "Negative Stamina Biome",
                spireName = "Spire",
                spoilageMultiplier = 1.0f,
                staminaDrainMultiplier = -1.0f
            };
            Assert.That(biome2.IsValid(), Is.False, "Negative stamina drain multiplier must fail IsValid.");
        }

        #endregion
    }
}
