using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using InkAndAsh.Data;

namespace InkAndAsh.Tests.EditMode
{
    [TestFixture]
    public class DataManagerTests
    {
        private DataManager _dataManager;
        private string _catalogsDirectory;

        [SetUp]
        public void SetUp()
        {
            _dataManager = new DataManager();
            _catalogsDirectory = LocateCatalogsDirectory();
            Assert.That(Directory.Exists(_catalogsDirectory), Is.True, $"Catalogs directory not found. Checked candidate paths.");
            bool loaded = _dataManager.LoadAllCatalogsFromDirectory(_catalogsDirectory);
            Assert.That(loaded, Is.True, "Failed to load catalogs via DataManager.");
        }

        private static string LocateCatalogsDirectory()
        {
            string baseDir = AppContext.BaseDirectory;
            string currentDir = Directory.GetCurrentDirectory();

            string[] candidates = new[]
            {
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "Assets", "Resources", "Catalogs")),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "Assets", "Resources", "Catalogs")),
                Path.GetFullPath(Path.Combine(currentDir, "Assets", "Resources", "Catalogs")),
                Path.GetFullPath(Path.Combine(currentDir, "..", "Assets", "Resources", "Catalogs")),
                Path.GetFullPath(Path.Combine(baseDir, "Assets", "Resources", "Catalogs"))
            };

            foreach (var candidate in candidates)
            {
                if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "items.json")))
                {
                    return candidate;
                }
            }

            return candidates[0];
        }

        #region Item Catalog Tests

        [Test]
        public void ItemsCatalog_LoadsAll16UnifiedItems()
        {
            Assert.That(_dataManager.Items, Is.Not.Null);
            Assert.That(_dataManager.Items.Count, Is.EqualTo(16), "Catalog must contain exactly 16 unified trade goods.");
        }

        [Test]
        public void ItemsCatalog_ContainsAllExpectedItemIds()
        {
            string[] expectedIds = new[]
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

            foreach (var id in expectedIds)
            {
                var item = _dataManager.GetItem(id);
                Assert.That(item, Is.Not.Null, $"Item '{id}' must exist in catalog.");
                Assert.That(item.itemId, Is.EqualTo(id));
                Assert.That(string.IsNullOrEmpty(item.name), Is.False);
                Assert.That(string.IsNullOrEmpty(item.primaryBiome), Is.False);
            }
        }

        [Test]
        public void ItemsCatalog_AllItemsHavePositiveWeightAndPrice()
        {
            foreach (var kvp in _dataManager.Items)
            {
                var item = kvp.Value;
                Assert.That(item.baseWeightKg, Is.GreaterThan(0f), $"Item '{item.itemId}' must have positive baseWeightKg.");
                Assert.That(item.basePrice, Is.GreaterThan(0), $"Item '{item.itemId}' must have positive basePrice.");
                Assert.That(item.IsValid(), Is.True, $"Item '{item.itemId}' IsValid() must return true.");
            }
        }

        [Test]
        public void ItemsCatalog_PerishableItemsHavePositiveDecayRate()
        {
            string[] perishableIds = new[]
            {
                "item_golden_barley",
                "item_sun_dried_kelp",
                "item_canopy_bloom",
                "item_wild_game_meat",
                "item_fresh_river_fish",
                "item_aromatic_herbs",
                "item_tallow_wax"
            };

            foreach (var id in perishableIds)
            {
                var item = _dataManager.GetItem(id);
                Assert.That(item, Is.Not.Null, $"Perishable item '{id}' should exist.");
                Assert.That(item.isPerishable, Is.True, $"Item '{id}' must be marked isPerishable = true.");
                Assert.That(item.decayRatePerMinute, Is.GreaterThan(0f), $"Perishable item '{id}' must have positive decayRatePerMinute.");
            }
        }

        [Test]
        public void ItemsCatalog_StableGoodsHaveZeroDecayRate()
        {
            string[] stableIds = new[]
            {
                "item_ironwood_resin",
                "item_glacier_salt",
                "item_bog_ash",
                "item_solar_clay",
                "item_spring_water",
                "item_timber_logs",
                "item_scrap_iron",
                "item_coarse_fiber",
                "item_imperial_ink"
            };

            foreach (var id in stableIds)
            {
                var item = _dataManager.GetItem(id);
                Assert.That(item, Is.Not.Null, $"Stable good '{id}' should exist.");
                Assert.That(item.isPerishable, Is.False, $"Item '{id}' should be isPerishable = false.");
                Assert.That(item.decayRatePerMinute, Is.EqualTo(0f), $"Stable good '{id}' must have decayRatePerMinute = 0.");
            }
        }

        [Test]
        public void ItemsCatalog_UsageCategoriesPopulated()
        {
            var validCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Trade", "Crafting", "Culinary", "Alchemy"
            };

            foreach (var kvp in _dataManager.Items)
            {
                var item = kvp.Value;
                Assert.That(item.usageCategories, Is.Not.Null, $"Item '{item.itemId}' must have usageCategories.");
                Assert.That(item.usageCategories.Length, Is.GreaterThan(0), $"Item '{item.itemId}' must specify at least one category.");

                foreach (var cat in item.usageCategories)
                {
                    Assert.That(validCategories.Contains(cat), Is.True, $"Category '{cat}' on '{item.itemId}' is not recognized.");
                }
            }
        }

        #endregion

        #region NPC Catalog Tests

        [Test]
        public void NpcCatalog_LoadsAtLeastThreeNpcs()
        {
            Assert.That(_dataManager.Npcs, Is.Not.Null);
            Assert.That(_dataManager.Npcs.Count, Is.GreaterThanOrEqualTo(3), "Catalog must contain at least 3 NPCs.");
        }

        [Test]
        public void NpcCatalog_RepresentsCoreAnchorAndBackgroundTiers()
        {
            var tiers = _dataManager.Npcs.Values.Select(n => n.tier).ToHashSet();
            Assert.That(tiers.Contains(1), Is.True, "Catalog must contain Tier 1 (Core) NPC.");
            Assert.That(tiers.Contains(2), Is.True, "Catalog must contain Tier 2 (Anchor) NPC.");
            Assert.That(tiers.Contains(3), Is.True, "Catalog must contain Tier 3 (Background) NPC.");
        }

        [Test]
        public void NpcCatalog_PadEmotionalStatesWithinValidRange()
        {
            foreach (var npc in _dataManager.Npcs.Values)
            {
                Assert.That(npc.padEmotionalState, Is.Not.Null, $"NPC '{npc.npcId}' must have padEmotionalState.");
                Assert.That(npc.padEmotionalState.pleasure, Is.InRange(-100f, 100f), $"NPC '{npc.npcId}' pleasure out of bounds.");
                Assert.That(npc.padEmotionalState.arousal, Is.InRange(-100f, 100f), $"NPC '{npc.npcId}' arousal out of bounds.");
                Assert.That(npc.padEmotionalState.dominance, Is.InRange(-100f, 100f), $"NPC '{npc.npcId}' dominance out of bounds.");
                Assert.That(npc.padEmotionalState.IsValid(), Is.True);
            }
        }

        [Test]
        public void NpcCatalog_RelationshipVectorsWithinValidRange()
        {
            foreach (var npc in _dataManager.Npcs.Values)
            {
                if (npc.relationshipVectors != null)
                {
                    Assert.That(npc.relationshipVectors.imperialFear, Is.InRange(0f, 100f), $"NPC '{npc.npcId}' imperialFear out of range.");
                    Assert.That(npc.relationshipVectors.factionTrust, Is.InRange(0f, 100f), $"NPC '{npc.npcId}' factionTrust out of range.");
                    Assert.That(npc.relationshipVectors.IsValid(), Is.True);
                }
            }
        }

        [Test]
        public void NpcCatalog_AllFactionsAreValid()
        {
            foreach (var npc in _dataManager.Npcs.Values)
            {
                Assert.That(FactionNames.IsValidFaction(npc.factionAffiliation), Is.True,
                    $"NPC '{npc.npcId}' has invalid faction '{npc.factionAffiliation}'.");
            }
        }

        [Test]
        public void PADState_ClampRestrictsExtremeValues()
        {
            var pad = new PADState(-150f, 250f, -999f);
            Assert.That(pad.IsValid(), Is.False);

            pad.Clamp();
            Assert.That(pad.pleasure, Is.EqualTo(-100f));
            Assert.That(pad.arousal, Is.EqualTo(100f));
            Assert.That(pad.dominance, Is.EqualTo(-100f));
            Assert.That(pad.IsValid(), Is.True);
        }

        [Test]
        public void RelationshipVectors_ClampRestrictsValues()
        {
            var rel = new RelationshipVectors(-50f, 150f);
            Assert.That(rel.IsValid(), Is.False);

            rel.Clamp();
            Assert.That(rel.imperialFear, Is.EqualTo(0f));
            Assert.That(rel.factionTrust, Is.EqualTo(100f));
            Assert.That(rel.IsValid(), Is.True);
        }

        #endregion

        #region Quest Catalog Tests

        [Test]
        public void QuestCatalog_LoadsQuestWithFiveActs()
        {
            Assert.That(_dataManager.Quests, Is.Not.Null);
            Assert.That(_dataManager.Quests.Count, Is.GreaterThanOrEqualTo(1), "Catalog must contain at least 1 sample quest.");

            var quest = _dataManager.Quests.Values.First();
            Assert.That(quest.IsValid(), Is.True);
            Assert.That(quest.acts, Is.Not.Null);
            Assert.That(quest.acts.Count, Is.EqualTo(5), "Sample quest must define exactly 5 acts.");

            var expectedActs = new[]
            {
                QuestAct.Friction,
                QuestAct.Ripple,
                QuestAct.Crisis,
                QuestAct.Aftermath,
                QuestAct.Legacy
            };

            for (int i = 0; i < 5; i++)
            {
                Assert.That(quest.acts[i].act, Is.EqualTo(expectedActs[i]));
                Assert.That(string.IsNullOrEmpty(quest.acts[i].title), Is.False);
                Assert.That(string.IsNullOrEmpty(quest.acts[i].description), Is.False);
            }
        }

        #endregion

        #region Biomes Catalog Tests

        [Test]
        public void BiomesCatalog_LoadsExactlySevenBiomes()
        {
            Assert.That(_dataManager.Biomes, Is.Not.Null);
            Assert.That(_dataManager.Biomes.Count, Is.EqualTo(7), "There must be exactly 7 biomes.");
        }

        [Test]
        public void BiomesCatalog_AllSevenBiomesHaveUniqueSpires()
        {
            string[] expectedBiomeIds = new[]
            {
                "biome_grasslands",
                "biome_coast",
                "biome_forest",
                "biome_mountains",
                "biome_swamp",
                "biome_desert",
                "biome_jungle"
            };

            var spires = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var biomeId in expectedBiomeIds)
            {
                var biome = _dataManager.GetBiome(biomeId);
                Assert.That(biome, Is.Not.Null, $"Biome '{biomeId}' must exist in catalog.");
                Assert.That(string.IsNullOrEmpty(biome.spireName), Is.False, $"Biome '{biomeId}' must have spireName.");
                Assert.That(spires.Add(biome.spireName), Is.True, $"Spire name '{biome.spireName}' must be unique.");
                Assert.That(biome.spoilageMultiplier, Is.GreaterThan(0f));
                Assert.That(biome.staminaDrainMultiplier, Is.GreaterThan(0f));
            }
        }

        #endregion

        #region Robustness & Error Handling Tests

        [Test]
        public void ErrorHandling_MalformedJsonHandledSafely()
        {
            var dm = new DataManager();
            bool result = dm.LoadItems("{ malformed json content ]");
            Assert.That(result, Is.False, "Malformed JSON must return false without uncaught exception.");
        }

        [Test]
        public void ErrorHandling_NullOrEmptyJsonReturnsFalse()
        {
            var dm = new DataManager();
            Assert.That(dm.LoadItems(null), Is.False);
            Assert.That(dm.LoadItems(""), Is.False);
            Assert.That(dm.LoadNpcs(null), Is.False);
            Assert.That(dm.LoadQuests(null), Is.False);
            Assert.That(dm.LoadBiomes(null), Is.False);
        }

        [Test]
        public void ErrorHandling_MissingDirectoryReturnsFalse()
        {
            var dm = new DataManager();
            bool result = dm.LoadAllCatalogsFromDirectory("C:\\NonExistent_Directory_Path_99999");
            Assert.That(result, Is.False);
        }

        #endregion
    }
}
