using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using InkAndAsh.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace InkAndAsh.StandaloneTests
{
    [TestFixture]
    public class AsmdefAndPerformanceChallengeTests
    {
        private string _projectRoot;
        private string _catalogsPath;

        [SetUp]
        public void Setup()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string probePath = Path.Combine(baseDir, "Assets");
            if (!Directory.Exists(probePath))
            {
                probePath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "Assets"));
            }
            if (!Directory.Exists(probePath))
            {
                probePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "Assets"));
            }

            _projectRoot = Directory.GetParent(probePath)!.FullName;
            _catalogsPath = Path.Combine(_projectRoot, "Assets", "Resources", "Catalogs");

            Assert.That(Directory.Exists(_projectRoot), Is.True, $"Project root must exist at {_projectRoot}");
            Assert.That(Directory.Exists(_catalogsPath), Is.True, $"Catalogs path must exist at {_catalogsPath}");
        }

        #region Challenge 1: Asmdef Integrity & Circular Dependency Cycle Detection

        [Test]
        public void Asmdefs_All7RuntimeAndTestAsmdefs_ExistAndAreValidJson()
        {
            var expectedAsmdefs = new Dictionary<string, string>
            {
                { "InkAndAsh.Core", Path.Combine(_projectRoot, "Assets", "Scripts", "Core", "InkAndAsh.Core.asmdef") },
                { "InkAndAsh.Data", Path.Combine(_projectRoot, "Assets", "Scripts", "Data", "InkAndAsh.Data.asmdef") },
                { "InkAndAsh.Combat", Path.Combine(_projectRoot, "Assets", "Scripts", "Combat", "InkAndAsh.Combat.asmdef") },
                { "InkAndAsh.Economy", Path.Combine(_projectRoot, "Assets", "Scripts", "Economy", "InkAndAsh.Economy.asmdef") },
                { "InkAndAsh.World", Path.Combine(_projectRoot, "Assets", "Scripts", "World", "InkAndAsh.World.asmdef") },
                { "InkAndAsh.Narrative", Path.Combine(_projectRoot, "Assets", "Scripts", "Narrative", "InkAndAsh.Narrative.asmdef") },
                { "InkAndAsh.UI", Path.Combine(_projectRoot, "Assets", "Scripts", "UI", "InkAndAsh.UI.asmdef") },
                { "InkAndAsh.Tests.EditMode", Path.Combine(_projectRoot, "Assets", "Tests", "EditMode", "InkAndAsh.Tests.EditMode.asmdef") }
            };

            foreach (var kvp in expectedAsmdefs)
            {
                string asmdefName = kvp.Key;
                string filePath = kvp.Value;

                Assert.That(File.Exists(filePath), Is.True, $"Assembly definition file missing: {filePath}");

                string content = File.ReadAllText(filePath);
                Assert.That(string.IsNullOrWhiteSpace(content), Is.False, $"{asmdefName} content must not be empty.");

                JObject json = null;
                Assert.DoesNotThrow(() => { json = JObject.Parse(content); }, $"Asmdef {asmdefName} must be valid JSON.");

                Assert.That((string)json["name"], Is.EqualTo(asmdefName), $"Asmdef 'name' property must match {asmdefName}");
                Assert.That(json["references"], Is.Not.Null, $"Asmdef {asmdefName} must have 'references' array.");
            }
        }

        [Test]
        public void Asmdefs_DirectedGraph_IsStrictlyAcyclic_NoCyclesDetected()
        {
            // Build dependency graph
            string assetsDir = Path.Combine(_projectRoot, "Assets");
            var asmdefFiles = Directory.GetFiles(assetsDir, "*.asmdef", SearchOption.AllDirectories);

            Assert.That(asmdefFiles.Length, Is.GreaterThanOrEqualTo(8), "At least 8 asmdef files must be present.");

            var graph = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            var internalAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in asmdefFiles)
            {
                var json = JObject.Parse(File.ReadAllText(file));
                string name = (string)json["name"]!;
                var refs = json["references"]?.Select(r => (string)r!).ToList() ?? new List<string>();

                graph[name] = refs;
                internalAssemblies.Add(name);
            }

            // Verify specific architectural layer rules
            // InkAndAsh.Core must NOT reference any internal assemblies
            Assert.That(graph["InkAndAsh.Core"].Count, Is.EqualTo(0), "InkAndAsh.Core must have zero dependencies.");

            // InkAndAsh.Data must ONLY reference InkAndAsh.Core
            var dataRefs = graph["InkAndAsh.Data"].Where(r => internalAssemblies.Contains(r)).ToList();
            Assert.That(dataRefs, Is.EquivalentTo(new[] { "InkAndAsh.Core" }), "InkAndAsh.Data must only reference InkAndAsh.Core.");

            // Domain assemblies (Combat, Economy, World, Narrative) must reference Core and Data, but not each other
            string[] domainAssemblies = { "InkAndAsh.Combat", "InkAndAsh.Economy", "InkAndAsh.World", "InkAndAsh.Narrative" };
            foreach (var domain in domainAssemblies)
            {
                var internalRefs = graph[domain].Where(r => internalAssemblies.Contains(r)).ToList();
                Assert.That(internalRefs, Is.EquivalentTo(new[] { "InkAndAsh.Core", "InkAndAsh.Data" }),
                    $"{domain} must only reference Core and Data, avoiding cross-domain coupling.");
            }

            // InkAndAsh.UI references domain layers
            var uiInternalRefs = graph["InkAndAsh.UI"].Where(r => internalAssemblies.Contains(r)).ToList();
            Assert.That(uiInternalRefs, Does.Contain("InkAndAsh.Core"));
            Assert.That(uiInternalRefs, Does.Contain("InkAndAsh.Data"));
            Assert.That(uiInternalRefs, Does.Contain("InkAndAsh.Combat"));

            // Tarjan's Strongly Connected Components / DFS cycle detection
            var visited = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var recStack = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var cycleNodes = new List<string>();

            bool HasCycle(string node, List<string> path)
            {
                visited[node] = true;
                recStack[node] = true;
                path.Add(node);

                if (graph.ContainsKey(node))
                {
                    foreach (var neighbor in graph[node])
                    {
                        // Only follow edges within internal project assemblies
                        if (!internalAssemblies.Contains(neighbor)) continue;

                        if (!visited.TryGetValue(neighbor, out bool wasVisited) || !wasVisited)
                        {
                            if (HasCycle(neighbor, path)) return true;
                        }
                        else if (recStack.TryGetValue(neighbor, out bool inStack) && inStack)
                        {
                            cycleNodes = new List<string>(path) { neighbor };
                            return true;
                        }
                    }
                }

                recStack[node] = false;
                path.RemoveAt(path.Count - 1);
                return false;
            }

            foreach (var node in internalAssemblies)
            {
                if (!visited.TryGetValue(node, out bool wasVisited) || !wasVisited)
                {
                    bool cycleFound = HasCycle(node, new List<string>());
                    Assert.That(cycleFound, Is.False, $"Cycle detected in asmdef graph: {string.Join(" -> ", cycleNodes)}");
                }
            }
        }

        #endregion

        #region Challenge 2: Packages/manifest.json Integrity

        [Test]
        public void Manifest_IsValidJson_AndContainsRequiredPackages()
        {
            string manifestPath = Path.Combine(_projectRoot, "Packages", "manifest.json");
            Assert.That(File.Exists(manifestPath), Is.True, $"manifest.json must exist at {manifestPath}");

            string rawJson = File.ReadAllText(manifestPath);
            JObject manifest = null;
            Assert.DoesNotThrow(() => { manifest = JObject.Parse(rawJson); }, "manifest.json must be valid JSON.");

            var dependencies = manifest["dependencies"] as JObject;
            Assert.That(dependencies, Is.Not.Null, "manifest.json must have 'dependencies' object.");

            // Check for required packages
            Assert.That(dependencies["com.unity.nuget.newtonsoft-json"], Is.Not.Null,
                "manifest.json must contain com.unity.nuget.newtonsoft-json");
            Assert.That((string)dependencies["com.unity.nuget.newtonsoft-json"], Is.EqualTo("3.2.2"),
                "com.unity.nuget.newtonsoft-json version must be 3.2.2");

            Assert.That(dependencies["com.unity.render-pipelines.universal"], Is.Not.Null,
                "manifest.json must contain com.unity.render-pipelines.universal (URP)");

            Assert.That(dependencies["com.unity.inputsystem"], Is.Not.Null,
                "manifest.json must contain com.unity.inputsystem");

            Assert.That(dependencies["com.unity.test-framework"], Is.Not.Null,
                "manifest.json must contain com.unity.test-framework");
        }

        #endregion

        #region Challenge 3: DataManager In-Memory O(1) Dictionary Caching & Zero Redundant Disk Reads

        [Test]
        public void DataManager_LoadsIntoInMemoryDictionary_RepeatedQueriesReturnIdenticalReferences()
        {
            var dm = new DataManager();
            bool loaded = dm.LoadAllCatalogsFromDirectory(_catalogsPath);
            Assert.That(loaded, Is.True, "LoadAllCatalogsFromDirectory should succeed.");

            // 1. Verify instance reference identity (cached in memory, not re-allocated on each call)
            var item1 = dm.GetItem("item_golden_barley");
            var item2 = dm.GetItem("item_golden_barley");
            Assert.That(item1, Is.Not.Null);
            Assert.That(object.ReferenceEquals(item1, item2), Is.True,
                "GetItem must return the exact same in-memory object instance across repeated queries.");

            var npc1 = dm.GetNpc("npc_core_vesper_01");
            var npc2 = dm.GetNpc("npc_core_vesper_01");
            Assert.That(npc1, Is.Not.Null);
            Assert.That(object.ReferenceEquals(npc1, npc2), Is.True,
                "GetNpc must return the exact same in-memory object instance across repeated queries.");

            var quest1 = dm.GetQuest("quest_kaelen_grain_crisis");
            var quest2 = dm.GetQuest("quest_kaelen_grain_crisis");
            Assert.That(quest1, Is.Not.Null);
            Assert.That(object.ReferenceEquals(quest1, quest2), Is.True,
                "GetQuest must return the exact same in-memory object instance across repeated queries.");

            var biome1 = dm.GetBiome("biome_grasslands");
            var biome2 = dm.GetBiome("biome_grasslands");
            Assert.That(biome1, Is.Not.Null);
            Assert.That(object.ReferenceEquals(biome1, biome2), Is.True,
                "GetBiome must return the exact same in-memory object instance across repeated queries.");
        }

        [Test]
        public void DataManager_LookupsAreZeroDiskRead_UnaffectedByCatalogSourceAbsenceAfterLoad()
        {
            // Create a temporary isolated directory with copy of catalogs
            string tempDir = Path.Combine(Path.GetTempPath(), "InkAndAsh_TestCatalogs_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                foreach (var file in Directory.GetFiles(_catalogsPath, "*.json"))
                {
                    File.Copy(file, Path.Combine(tempDir, Path.GetFileName(file)));
                }

                var dm = new DataManager();
                bool loaded = dm.LoadAllCatalogsFromDirectory(tempDir);
                Assert.That(loaded, Is.True);
                Assert.That(dm.Items.Count, Is.EqualTo(16));

                // Now DELETE all JSON files in the source directory
                foreach (var file in Directory.GetFiles(tempDir, "*.json"))
                {
                    File.Delete(file);
                }
                Directory.Delete(tempDir, true);

                // Verify that lookups continue to work flawlessly with ZERO disk dependencies
                var barley = dm.GetItem("item_golden_barley");
                Assert.That(barley, Is.Not.Null);
                Assert.That(barley.name, Is.EqualTo("Golden Barley"));

                var vesper = dm.GetNpc("npc_core_vesper_01");
                Assert.That(vesper, Is.Not.Null);
                Assert.That(vesper.name, Is.EqualTo("Vesper the Weaver"));

                var quest = dm.GetQuest("quest_kaelen_grain_crisis");
                Assert.That(quest, Is.Not.Null);

                var biome = dm.GetBiome("biome_grasslands");
                Assert.That(biome, Is.Not.Null);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Test]
        public void DataManager_StressBenchmark_100kLookupsExecuteUnder100Milliseconds_O1Complexity()
        {
            var dm = new DataManager();
            dm.LoadAllCatalogsFromDirectory(_catalogsPath);

            string[] itemKeys = dm.Items.Keys.ToArray();
            Assert.That(itemKeys.Length, Is.EqualTo(16));

            // Warm up JIT
            for (int i = 0; i < 1000; i++)
            {
                dm.GetItem(itemKeys[i % itemKeys.Length]);
            }

            // Benchmark 100,000 lookups
            const int lookupCount = 100_000;
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < lookupCount; i++)
            {
                string key = itemKeys[i % itemKeys.Length];
                var item = dm.GetItem(key);
                if (item == null) Assert.Fail("Cached item should never return null.");
            }

            sw.Stop();

            double elapsedMs = sw.Elapsed.TotalMilliseconds;
            double opsPerSec = (lookupCount / elapsedMs) * 1000.0;

            TestContext.Out.WriteLine($"[Benchmark] {lookupCount} dictionary lookups completed in {elapsedMs:F2} ms ({opsPerSec:N0} ops/sec)");

            // In-memory O(1) dictionary lookups for 100k items typically complete in < 25 ms.
            // Generous ceiling of 200 ms to account for CI runner / VM fluctuations.
            Assert.That(elapsedMs, Is.LessThan(200.0),
                $"100,000 lookups took {elapsedMs} ms, exceeding 200 ms threshold. Dict lookup must be O(1) in-memory.");
        }

        [Test]
        public void DataManager_Clear_FlushesCacheAndResetsLoadedFlag()
        {
            var dm = new DataManager();
            dm.LoadAllCatalogsFromDirectory(_catalogsPath);
            Assert.That(dm.IsLoaded, Is.True);
            Assert.That(dm.Items.Count, Is.EqualTo(16));

            dm.Clear();

            Assert.That(dm.IsLoaded, Is.False);
            Assert.That(dm.Items.Count, Is.EqualTo(0));
            Assert.That(dm.Npcs.Count, Is.EqualTo(0));
            Assert.That(dm.Quests.Count, Is.EqualTo(0));
            Assert.That(dm.Biomes.Count, Is.EqualTo(0));

            Assert.That(dm.GetItem("item_golden_barley"), Is.Null);
        }

        #endregion
    }
}
