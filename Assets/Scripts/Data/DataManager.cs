using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace InkAndAsh.Data
{
    public class DataManager
    {
        private static DataManager _instance;
        private static readonly object _lock = new object();

        public static DataManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new DataManager();
                        }
                    }
                }
                return _instance;
            }
        }

        private readonly Dictionary<string, ItemData> _items = new Dictionary<string, ItemData>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, NpcData> _npcs = new Dictionary<string, NpcData>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, QuestData> _quests = new Dictionary<string, QuestData>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, BiomeData> _biomes = new Dictionary<string, BiomeData>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, ItemData> Items => _items;
        public IReadOnlyDictionary<string, NpcData> Npcs => _npcs;
        public IReadOnlyDictionary<string, QuestData> Quests => _quests;
        public IReadOnlyDictionary<string, BiomeData> Biomes => _biomes;

        public bool IsLoaded { get; private set; }

        public DataManager()
        {
        }

        public void Clear()
        {
            _items.Clear();
            _npcs.Clear();
            _quests.Clear();
            _biomes.Clear();
            IsLoaded = false;
        }

        public void LoadAllCatalogs()
        {
            Clear();
            bool loadedAny = false;

            // Try loading via Resources if in Unity runtime
#if UNITY_5_3_OR_NEWER || UNITY_EDITOR
            try
            {
                var itemsAsset = UnityEngine.Resources.Load<UnityEngine.TextAsset>("Catalogs/items");
                var npcsAsset = UnityEngine.Resources.Load<UnityEngine.TextAsset>("Catalogs/npcs");
                var questsAsset = UnityEngine.Resources.Load<UnityEngine.TextAsset>("Catalogs/quests");
                var biomesAsset = UnityEngine.Resources.Load<UnityEngine.TextAsset>("Catalogs/biomes");

                if (itemsAsset != null && npcsAsset != null && questsAsset != null && biomesAsset != null)
                {
                    LoadItems(itemsAsset.text);
                    LoadNpcs(npcsAsset.text);
                    LoadQuests(questsAsset.text);
                    LoadBiomes(biomesAsset.text);
                    IsLoaded = true;
                    return;
                }
            }
            catch (Exception ex)
            {
                LogWarning($"[DataManager] Resources.Load failed, attempting filesystem fallback: {ex.Message}");
            }
#endif

            // Filesystem search fallback
            string[] possibleDirectories = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Resources", "Catalogs"),
                Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Resources", "Catalogs"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Assets", "Resources", "Catalogs"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Assets", "Resources", "Catalogs"),
                Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Catalogs"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Catalogs")
            };

            foreach (var dir in possibleDirectories)
            {
                if (Directory.Exists(dir))
                {
                    if (LoadAllCatalogsFromDirectory(dir))
                    {
                        loadedAny = true;
                        break;
                    }
                }
            }

            if (!loadedAny)
            {
                LogWarning("[DataManager] Could not find catalog files in any default directory.");
            }
        }

        public bool LoadAllCatalogsFromDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                LogError($"[DataManager] Directory does not exist: {directoryPath}");
                return false;
            }

            bool success = true;
            string itemsFile = Path.Combine(directoryPath, "items.json");
            string npcsFile = Path.Combine(directoryPath, "npcs.json");
            string questsFile = Path.Combine(directoryPath, "quests.json");
            string biomesFile = Path.Combine(directoryPath, "biomes.json");

            if (File.Exists(itemsFile))
            {
                success &= LoadItems(File.ReadAllText(itemsFile));
            }
            else
            {
                LogError($"[DataManager] Missing items catalog: {itemsFile}");
                success = false;
            }

            if (File.Exists(npcsFile))
            {
                success &= LoadNpcs(File.ReadAllText(npcsFile));
            }
            else
            {
                LogError($"[DataManager] Missing npcs catalog: {npcsFile}");
                success = false;
            }

            if (File.Exists(questsFile))
            {
                success &= LoadQuests(File.ReadAllText(questsFile));
            }
            else
            {
                LogError($"[DataManager] Missing quests catalog: {questsFile}");
                success = false;
            }

            if (File.Exists(biomesFile))
            {
                success &= LoadBiomes(File.ReadAllText(biomesFile));
            }
            else
            {
                LogError($"[DataManager] Missing biomes catalog: {biomesFile}");
                success = false;
            }

            IsLoaded = success;
            return success;
        }

        public bool LoadItems(string jsonContent)
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                LogError("[DataManager] LoadItems: JSON content is null or empty.");
                return false;
            }

            try
            {
                var list = JsonConvert.DeserializeObject<List<ItemData>>(jsonContent);
                if (list == null)
                {
                    LogError("[DataManager] LoadItems: Deserialized item list is null.");
                    return false;
                }

                _items.Clear();
                foreach (var item in list)
                {
                    if (item == null || !item.IsValid())
                    {
                        LogWarning($"[DataManager] Skipping invalid item entry: {item?.itemId ?? "null"}");
                        continue;
                    }

                    _items[item.itemId] = item;
                }

                return true;
            }
            catch (Exception ex)
            {
                LogError($"[DataManager] Error parsing items JSON: {ex.Message}");
                return false;
            }
        }

        public bool LoadNpcs(string jsonContent)
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                LogError("[DataManager] LoadNpcs: JSON content is null or empty.");
                return false;
            }

            try
            {
                var list = JsonConvert.DeserializeObject<List<NpcData>>(jsonContent);
                if (list == null)
                {
                    LogError("[DataManager] LoadNpcs: Deserialized NPC list is null.");
                    return false;
                }

                _npcs.Clear();
                foreach (var npc in list)
                {
                    if (npc == null || !npc.IsValid())
                    {
                        LogWarning($"[DataManager] Skipping invalid NPC entry: {npc?.npcId ?? "null"}");
                        continue;
                    }

                    npc.ValidateAndClamp();
                    _npcs[npc.npcId] = npc;
                }

                return true;
            }
            catch (Exception ex)
            {
                LogError($"[DataManager] Error parsing NPCs JSON: {ex.Message}");
                return false;
            }
        }

        public bool LoadQuests(string jsonContent)
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                LogError("[DataManager] LoadQuests: JSON content is null or empty.");
                return false;
            }

            try
            {
                var list = JsonConvert.DeserializeObject<List<QuestData>>(jsonContent);
                if (list == null)
                {
                    LogError("[DataManager] LoadQuests: Deserialized Quest list is null.");
                    return false;
                }

                _quests.Clear();
                foreach (var quest in list)
                {
                    if (quest == null || !quest.IsValid())
                    {
                        LogWarning($"[DataManager] Skipping invalid quest entry: {quest?.questId ?? "null"}");
                        continue;
                    }

                    _quests[quest.questId] = quest;
                }

                return true;
            }
            catch (Exception ex)
            {
                LogError($"[DataManager] Error parsing Quests JSON: {ex.Message}");
                return false;
            }
        }

        public bool LoadBiomes(string jsonContent)
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                LogError("[DataManager] LoadBiomes: JSON content is null or empty.");
                return false;
            }

            try
            {
                var list = JsonConvert.DeserializeObject<List<BiomeData>>(jsonContent);
                if (list == null)
                {
                    LogError("[DataManager] LoadBiomes: Deserialized Biome list is null.");
                    return false;
                }

                _biomes.Clear();
                foreach (var biome in list)
                {
                    if (biome == null || !biome.IsValid())
                    {
                        LogWarning($"[DataManager] Skipping invalid biome entry: {biome?.biomeId ?? "null"}");
                        continue;
                    }

                    _biomes[biome.biomeId] = biome;
                }

                return true;
            }
            catch (Exception ex)
            {
                LogError($"[DataManager] Error parsing Biomes JSON: {ex.Message}");
                return false;
            }
        }

        public ItemData GetItem(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _items.TryGetValue(id, out var item);
            return item;
        }

        public NpcData GetNpc(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _npcs.TryGetValue(id, out var npc);
            return npc;
        }

        public QuestData GetQuest(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _quests.TryGetValue(id, out var quest);
            return quest;
        }

        public BiomeData GetBiome(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _biomes.TryGetValue(id, out var biome);
            return biome;
        }

        private static void LogError(string message)
        {
#if UNITY_5_3_OR_NEWER || UNITY_EDITOR
            UnityEngine.Debug.LogError(message);
#else
            Console.Error.WriteLine(message);
#endif
        }

        private static void LogWarning(string message)
        {
#if UNITY_5_3_OR_NEWER || UNITY_EDITOR
            UnityEngine.Debug.LogWarning(message);
#else
            Console.WriteLine(message);
#endif
        }
    }
}

#if STANDALONE_TEST
namespace InkAndAsh.StandaloneTests
{
    using NUnit.Framework;
    using InkAndAsh.Data;

    [TestFixture]
    public class DataCatalogTests
    {
        private string _catalogsPath;

        [SetUp]
        public void Setup()
        {
            // Locate Assets/Resources/Catalogs directory
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
        }

        [Test]
        public void ItemCatalog_LoadsAll16UnifiedItems_Successfully()
        {
            var manager = new DataManager();
            string itemsFile = Path.Combine(_catalogsPath, "items.json");
            string json = File.ReadAllText(itemsFile);

            bool success = manager.LoadItems(json);

            Assert.That(success, Is.True, "LoadItems should return true.");
            Assert.That(manager.Items.Count, Is.EqualTo(16), "Exactly 16 unified items must be loaded.");

            string[] expectedItemIds = new[]
            {
                "item_golden_barley", "item_sun_dried_kelp", "item_ironwood_resin", "item_glacier_salt",
                "item_bog_ash", "item_solar_clay", "item_canopy_bloom", "item_wild_game_meat",
                "item_fresh_river_fish", "item_spring_water", "item_aromatic_herbs", "item_timber_logs",
                "item_scrap_iron", "item_coarse_fiber", "item_tallow_wax", "item_imperial_ink"
            };

            foreach (var id in expectedItemIds)
            {
                var item = manager.GetItem(id);
                Assert.That(item, Is.Not.Null, $"Item {id} must exist in catalog.");
                Assert.That(item.IsValid(), Is.True, $"Item {id} must be valid.");
                Assert.That(item.baseWeightKg, Is.GreaterThan(0f), $"Item {id} base weight must be > 0.");
                Assert.That(item.decayRatePerMinute, Is.GreaterThanOrEqualTo(0f), $"Item {id} decay rate must be >= 0.");
            }
        }

        [Test]
        public void NpcCatalog_LoadsTieredNpcs_WithValidPADAndFactions()
        {
            var manager = new DataManager();
            string npcsFile = Path.Combine(_catalogsPath, "npcs.json");
            string json = File.ReadAllText(npcsFile);

            bool success = manager.LoadNpcs(json);

            Assert.That(success, Is.True, "LoadNpcs should return true.");
            Assert.That(manager.Npcs.Count, Is.GreaterThanOrEqualTo(3), "At least 3 sample NPCs must be loaded.");

            var core = manager.GetNpc("npc_core_vesper_01");
            Assert.That(core, Is.Not.Null, "Vesper the Weaver must exist.");
            Assert.That(core.tier, Is.EqualTo(1), "Vesper is Tier 1 Core Cast.");
            Assert.That(core.padEmotionalState.IsValid(), Is.True, "Vesper PAD must be in [-100, 100].");

            var anchor = manager.GetNpc("npc_anchor_kael_01");
            Assert.That(anchor, Is.Not.Null, "Kaelen the Miller must exist.");
            Assert.That(anchor.tier, Is.EqualTo(2), "Kaelen is Tier 2 Anchor.");
            Assert.That(anchor.factionAffiliation, Is.EqualTo(FactionNames.Pragmatists));

            var bg = manager.GetNpc("npc_bg_elda_01");
            Assert.That(bg, Is.Not.Null, "Elda the Ferryhand must exist.");
            Assert.That(bg.tier, Is.EqualTo(3), "Elda is Tier 3 Background.");
            Assert.That(bg.factionAffiliation, Is.EqualTo(FactionNames.Outsiders));
        }

        [Test]
        public void QuestCatalog_Loads5ActQuest_Successfully()
        {
            var manager = new DataManager();
            string questsFile = Path.Combine(_catalogsPath, "quests.json");
            string json = File.ReadAllText(questsFile);

            bool success = manager.LoadQuests(json);

            Assert.That(success, Is.True, "LoadQuests should return true.");
            Assert.That(manager.Quests.Count, Is.GreaterThanOrEqualTo(1), "At least 1 quest must be loaded.");

            var quest = manager.GetQuest("quest_kaelen_grain_crisis");
            Assert.That(quest, Is.Not.Null, "Kaelen grain crisis quest must exist.");
            Assert.That(quest.IsValid(), Is.True, "Quest must be valid.");
            Assert.That(quest.acts.Count, Is.EqualTo(5), "Quest must have exactly 5 acts.");

            Assert.That(quest.acts[0].act, Is.EqualTo(QuestAct.Friction));
            Assert.That(quest.acts[1].act, Is.EqualTo(QuestAct.Ripple));
            Assert.That(quest.acts[2].act, Is.EqualTo(QuestAct.Crisis));
            Assert.That(quest.acts[3].act, Is.EqualTo(QuestAct.Aftermath));
            Assert.That(quest.acts[4].act, Is.EqualTo(QuestAct.Legacy));

            Assert.That(quest.acts[4].reward.unlockedBoon, Is.EqualTo("Boon_Free_Village_Smithy_Repairs"));
        }

        [Test]
        public void BiomeCatalog_LoadsAll7Biomes_WithHazardsAndMultipliers()
        {
            var manager = new DataManager();
            string biomesFile = Path.Combine(_catalogsPath, "biomes.json");
            string json = File.ReadAllText(biomesFile);

            bool success = manager.LoadBiomes(json);

            Assert.That(success, Is.True, "LoadBiomes should return true.");
            Assert.That(manager.Biomes.Count, Is.EqualTo(7), "Exactly 7 biomes must be loaded.");

            string[] expectedBiomes = new[]
            {
                "biome_grasslands", "biome_coast", "biome_forest", "biome_mountains",
                "biome_swamp", "biome_desert", "biome_jungle"
            };

            foreach (var bId in expectedBiomes)
            {
                var biome = manager.GetBiome(bId);
                Assert.That(biome, Is.Not.Null, $"Biome {bId} must exist.");
                Assert.That(biome.IsValid(), Is.True, $"Biome {bId} must be valid.");
                Assert.That(string.IsNullOrEmpty(biome.spireName), Is.False, $"Biome {bId} must have a Spire name.");
            }

            var mountains = manager.GetBiome("biome_mountains");
            Assert.That(mountains.spoilageMultiplier, Is.EqualTo(0.2f), "Mountains spoilage multiplier is 0.2.");
            Assert.That(mountains.staminaDrainMultiplier, Is.EqualTo(2.0f), "Mountains stamina drain multiplier is 2.0.");
        }

        [Test]
        public void DataManager_LoadAllCatalogsFromDirectory_LoadsEverything()
        {
            var manager = new DataManager();
            bool result = manager.LoadAllCatalogsFromDirectory(_catalogsPath);

            Assert.That(result, Is.True, "LoadAllCatalogsFromDirectory should succeed.");
            Assert.That(manager.IsLoaded, Is.True);
            Assert.That(manager.Items.Count, Is.EqualTo(16));
            Assert.That(manager.Npcs.Count, Is.GreaterThanOrEqualTo(3));
            Assert.That(manager.Quests.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(manager.Biomes.Count, Is.EqualTo(7));
        }

        [Test]
        public void DataManager_MalformedJson_FailsGracefullyWithoutCrashing()
        {
            var manager = new DataManager();
            bool itemsResult = manager.LoadItems("{ malformed json [}");
            bool npcsResult = manager.LoadNpcs("{ malformed json [}");
            bool questsResult = manager.LoadQuests("{ malformed json [}");
            bool biomesResult = manager.LoadBiomes("{ malformed json [}");

            Assert.That(itemsResult, Is.False);
            Assert.That(npcsResult, Is.False);
            Assert.That(questsResult, Is.False);
            Assert.That(biomesResult, Is.False);

            Assert.That(manager.Items, Is.Not.Null);
            Assert.That(manager.Npcs, Is.Not.Null);
            Assert.That(manager.Quests, Is.Not.Null);
            Assert.That(manager.Biomes, Is.Not.Null);
        }

        [Test]
        public void PADState_ClampsOutOfRangeValues_Correctly()
        {
            var pad = new PADState(150f, -200f, 999f);
            pad.Clamp();

            Assert.That(pad.pleasure, Is.EqualTo(100f));
            Assert.That(pad.arousal, Is.EqualTo(-100f));
            Assert.That(pad.dominance, Is.EqualTo(100f));
            Assert.That(pad.IsValid(), Is.True);
        }

        [Test]
        public void QuestActProgression_AdvancesActSequence()
        {
            var quest = new QuestData
            {
                questId = "test_quest",
                npcId = "test_npc",
                currentAct = QuestAct.Friction,
                acts = new List<QuestActData>
                {
                    new QuestActData { act = QuestAct.Friction, completionFlag = "Act1" },
                    new QuestActData { act = QuestAct.Ripple, completionFlag = "Act2" },
                    new QuestActData { act = QuestAct.Crisis, completionFlag = "Act3" },
                    new QuestActData { act = QuestAct.Aftermath, completionFlag = "Act4" },
                    new QuestActData { act = QuestAct.Legacy, completionFlag = "Act5" }
                }
            };

            Assert.That(quest.currentAct, Is.EqualTo(QuestAct.Friction));
            bool advanced = quest.CompleteCurrentAct();

            Assert.That(advanced, Is.True);
            Assert.That(quest.currentAct, Is.EqualTo(QuestAct.Ripple));
            Assert.That(quest.actCompletionFlags["Act1"], Is.True);
        }
    }
}
#endif
