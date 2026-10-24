# Project: The Ink and the Ash (Unity 6 C# Implementation)

## Architecture

"The Ink and the Ash" is architected as an acyclic Directed Acyclic Graph (DAG) across 7 gameplay domains, runtime data stores, event channels, and an EditMode test assembly.

```
                      [ InkAndAsh.UI ]
                             │
     ┌───────────────┬───────┴───────┬───────────────┐
     ▼               ▼               ▼               ▼
[InkAndAsh.Combat] [InkAndAsh.Economy] [InkAndAsh.World] [InkAndAsh.Narrative]
     │               │               │               │
     └───────────────┼───────────────┴───────────────┘
                     ▼
             [ InkAndAsh.Data ] (Newtonsoft.Json)
                     │
                     ▼
             [ InkAndAsh.Core ] (Event Channels, Time, State)

             [ InkAndAsh.Tests.EditMode ] ──► (References all assemblies)
```

### Domain Boundaries
1. **InkAndAsh.Core**: Core state, time management, singleton lifecycle, generic ScriptableObject event channels (`GameEvent<T>`), and base math utilities. No dependencies on other game assemblies.
2. **InkAndAsh.Data**: Pure C# data models, DTOs, and `DataManager` using Newtonsoft.Json for item catalog, NPC catalogs, quest definitions, and biome configurations. Depends on `InkAndAsh.Core`.
3. **InkAndAsh.Combat**: Pure C# CTB Combat Engine (`CtbCombatEngine`, `Combatant`, `TimelineQueue`) with FFX action-cost formulas, Sync Moves, Interventions, and Mediator's Anchor, plus thin MonoBehaviour controller wrappers. Depends on `InkAndAsh.Core`, `InkAndAsh.Data`.
4. **InkAndAsh.Economy**: Caravan inventory ledger, 2-tier storage (Foot Packs vs Horse-Drawn Carriage), environmental perishable durability decay, and campsite cooking recipes. Depends on `InkAndAsh.Core`, `InkAndAsh.Data`.
5. **InkAndAsh.World**: Weather system, 7 biomes environmental states (Clear, Rain, Heat, Freezing), travel modifiers, and Spire calibration parameters. Depends on `InkAndAsh.Core`, `InkAndAsh.Data`.
6. **InkAndAsh.Narrative**: 5-Act quest state machine (Friction, Ripple, Crisis, Aftermath, Legacy), NPC dialogue node traversal, PAD emotional states ([-100, 100]), and faction relationship vectors. Depends on `InkAndAsh.Core`, `InkAndAsh.Data`.
7. **InkAndAsh.UI**: Event channel listeners, HUD controllers, timeline visualization bindings. Depends on all system assemblies.
8. **InkAndAsh.Tests.EditMode**: Automated NUnit unit tests covering pure C# engines and Unity systems without scene dependencies.

---

## Code Layout

```
d:\Unity Projects\Ink and Ash\
├── Assets\
│   ├── Resources\
│   │   └── Catalogs\
│   │       ├── items.json             (16 unified trade goods)
│   │       ├── npcs.json              (3+ sample NPCs with PAD & Love/Hate)
│   │       ├── quests.json            (1+ sample 5-Act quest)
│   │       └── biomes.json            (7 biomes with weather & hazards)
│   ├── Scripts\
│   │   ├── Core\
│   │   │   ├── InkAndAsh.Core.asmdef
│   │   │   ├── GameManager.cs
│   │   │   ├── TimeManager.cs
│   │   │   └── Events\
│   │   │       ├── GameEvent.cs
│   │   │       ├── GameEventListener.cs
│   │   │       └── ConcreteEvents.cs
│   │   ├── Data\
│   │   │   ├── InkAndAsh.Data.asmdef
│   │   │   ├── DataManager.cs
│   │   │   ├── ItemData.cs
│   │   │   ├── NpcData.cs
│   │   │   ├── QuestData.cs
│   │   │   └── BiomeData.cs
│   │   ├── Combat\
│   │   │   ├── InkAndAsh.Combat.asmdef
│   │   │   ├── CtbCombatEngine.cs
│   │   │   ├── Combatant.cs
│   │   │   ├── TimelineQueue.cs
│   │   │   ├── SkillAction.cs
│   │   │   └── CombatController.cs
│   │   ├── Economy\
│   │   │   ├── InkAndAsh.Economy.asmdef
│   │   │   ├── CaravanInventory.cs
│   │   │   ├── CookingSystem.cs
│   │   │   └── TradeLedger.cs
│   │   ├── World\
│   │   │   ├── InkAndAsh.World.asmdef
│   │   │   ├── WeatherSystem.cs
│   │   │   └── BiomeManager.cs
│   │   ├── Narrative\
│   │   │   ├── InkAndAsh.Narrative.asmdef
│   │   │   ├── QuestManager.cs
│   │   │   └── DialogueSystem.cs
│   │   └── UI\
│   │       ├── InkAndAsh.UI.asmdef
│   │       └── TimelineView.cs
│   └── Tests\
│       └── EditMode\
│           ├── InkAndAsh.Tests.EditMode.asmdef
│           ├── DataManagerTests.cs
│           ├── CtbCombatEngineTests.cs
│           ├── CaravanInventoryTests.cs
│           ├── WeatherSystemTests.cs
│           ├── TimeManagerTests.cs
│           └── QuestManagerTests.cs
├── Packages\
│   └── manifest.json                  (Includes com.unity.nuget.newtonsoft-json: 3.2.2)
├── ProjectSettings\
│   └── ProjectVersion.txt             (Unity 6000.6.4f1)
├── standalone-tests\
│   └── InkAndAsh.StandaloneTests.csproj (SDK-style net8.0 runner for instant dotnet test)
└── .gitignore                         (Unity-standard ignore rules)
```

---

## Feature Inventory

| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | Unity 6 Project Layout | Assets, Packages, ProjectSettings directory scaffolding | M1 | Survey / R1 |
| 2 | Package Manifest Setup | Packages/manifest.json with newtonsoft-json 3.2.2 & URP | M1 | Survey / R1 |
| 3 | Core Asmdef | `InkAndAsh.Core.asmdef` with no dependencies | M1 | Survey / R1 |
| 4 | Data Asmdef | `InkAndAsh.Data.asmdef` referencing Core & Newtonsoft | M1 | Survey / R1 |
| 5 | Combat Asmdef | `InkAndAsh.Combat.asmdef` referencing Core & Data | M1 | Survey / R1 |
| 6 | Economy Asmdef | `InkAndAsh.Economy.asmdef` referencing Core & Data | M1 | Survey / R1 |
| 7 | World Asmdef | `InkAndAsh.World.asmdef` referencing Core & Data | M1 | Survey / R1 |
| 8 | Narrative Asmdef | `InkAndAsh.Narrative.asmdef` referencing Core & Data | M1 | Survey / R1 |
| 9 | UI Asmdef | `InkAndAsh.UI.asmdef` referencing all domain systems | M1 | Survey / R1 |
| 10 | EditMode Test Asmdef | `InkAndAsh.Tests.EditMode.asmdef` referencing all systems | M1 | Survey / R1 |
| 11 | Standalone Test Csproj | SDK-style test project for dotnet test CLI execution | M1 | Survey / R1 |
| 12 | Item Data Models | C# models for trade goods with categories, weights, decay rates | M1 | Survey / R2 |
| 13 | 16 Unified Items JSON | `items.json` catalog containing all 16 specified goods | M1 | Survey / R2 |
| 14 | NPC Data Models | C# models for 3-tier NPCs with PAD [-100, 100] & Love/Hate | M1 | Survey / R2 |
| 15 | 3+ Sample NPCs JSON | `npcs.json` catalog with Core, Anchor, and Background NPCs | M1 | Survey / R2 |
| 16 | Quest Data Models | C# models for 5-Act quests (Friction, Ripple, Crisis, Aftermath, Legacy) | M1 | Survey / R2 |
| 17 | Sample Quest JSON | `quests.json` catalog with complete 5-Act arc | M1 | Survey / R2 |
| 18 | Biome Data Models | C# models for 7 biomes, weather states, hazards, multipliers | M1 | Survey / R2 |
| 19 | 7 Biomes JSON | `biomes.json` catalog for Grasslands, Coast, Forest, etc. | M1 | Survey / R2 |
| 20 | DataManager Implementation | Runtime JSON catalog loading, validation, and dictionary caching | M1 | Survey / R2 |
| 21 | Generic Event Channels | `GameEvent<T>` and `GameEventListener<T>` ScriptableObjects | M2 | Survey / R3 |
| 22 | Concrete Event Channels | CombatStarted, CombatEnded, HourTick, DayTick, ItemDecayed, SpireCalibrated | M2 | Survey / R3 |
| 23 | TimeManager Implementation | 1 real min = 1 in-game hour, 24-hr day cycle, hour/day tick dispatch | M2 | Survey / R3 |
| 24 | Campsite Rest Cycle | 8-hour rest advance with durability decay & weather tick | M2 | Survey / R3 |
| 25 | Weather System | Dynamic weather transitions across 7 biomes, spoilage/stamina modifiers | M2 | Survey / R3 |
| 26 | Caravan Inventory Ledger | List-based non-grid inventory with slot & weight limits | M2 | Survey / R3 |
| 27 | Storage Capacity Tiers | Foot Packs (30 slots, 50kg) vs Carriage (120 slots, 300kg, 60% decay reduction) | M2 | Survey / R3 |
| 28 | Durability Decay Ticking | 1% base/min, Desert/Rain +50%, Freezing 0.2%/min, moisture weight | M2 | Survey / R3 |
| 29 | Campsite Cooking & Buffs | Recipe validation, unified ingredient consumption, 24-hour expedition buffs | M2 | Survey / R3 |
| 30 | Pure C# CTB Combat Engine | FFX timeline model with decoupled pure C# logic | M2 | Survey / R3 |
| 31 | CTB Turn Delay Formula | BaseTurnDelay = (SkillCost / Agility) * WeatherMultiplier | M2 | Survey / R3 |
| 32 | Timeline Priority Queue | Dynamic turn order queue sorted by NextTurnTime with tie-breakers | M2 | Survey / R3 |
| 33 | Action Commit & Recalculation | Commit action, advance NextTurnTime, re-sort queue | M2 | Survey / R3 |
| 34 | Timeline Action Preview | Preview timeline shift before committing action | M2 | Survey / R3 |
| 35 | Sync Move Detection | Detect adjacent allies on timeline with Turn Delta <= 2 units | M2 | Survey / R3 |
| 36 | Intervention Action | Spend 25 Focus Points to advance ally up to 15 timeline units | M2 | Survey / R3 |
| 37 | Mediator's Anchor | Link 2 allies guaranteeing Sync Moves regardless of delays | M2 | Survey / R3 |
| 38 | Injury Debuffs System | -20% turn generation (Agility * 0.8), +15% delay cost, campsite clearing | M2 | Survey / R3 |
| 39 | GameManager Lifecycle | Persistent singleton (DontDestroyOnLoad), 5-state machine | M2 | Survey / R3 |
| 40 | EditMode Test Harness | Unit tests in `InkAndAsh.Tests.EditMode` | M3 | Survey / R4 |
| 41 | Data Catalog Unit Tests | Tests verifying loading and integrity of items, NPCs, quests, biomes | M3 | Survey / R4 |
| 42 | CTB Engine Unit Tests | Tests for formula math, queue ordering, sync moves, interventions, injuries | M3 | Survey / R4 |
| 43 | Inventory & Decay Unit Tests | Tests for slot/weight limits, carriage protection, weather decay, cooking | M3 | Survey / R4 |
| 44 | Time & Weather Unit Tests | Tests for hour/day ticks, 8-hour rest, weather transitions | M3 | Survey / R4 |
| 45 | Git Init & Configuration | Proper Unity .gitignore, git init on main branch | M4 | Survey / R5 |
| 46 | GitHub Push via gh | Commit codebase and push to public `miguelp001/ink-and-ash` | M4 | Survey / R5 |
| 47 | Final E2E Suite Verification | 100% pass on all E2E test tiers | M3 / E2E | Survey / Dual Track |

---

## Milestones

| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | Foundation Layer (R1 & R2) | Scaffolding, manifest.json, 7 gameplay asmdefs, test asmdef, SDK csproj, data models, 16 items JSON, NPCs JSON, Quests JSON, Biomes JSON, DataManager | None | DONE |
| M2 | Core Runtime Systems (R3) | GameManager, TimeManager, Event Channels, WeatherSystem, CaravanInventory, pure C# CtbCombatEngine | M1 | DONE |
| M3 | Unit Testing & Verification (R4) | EditMode Unit Test Suite (InkAndAsh.Tests.EditMode), standalone runner, all test suites | M1, M2 | DONE |
| M4 | Git Repository & GitHub Setup (R5) | Git re-init, .gitignore, initial commit, GitHub push to miguelp001/ink-and-ash via gh | M1-M3 | IN_PROGRESS |
| E2E | Opaque-Box E2E Testing Track | Independent requirement-driven test infra, Tiers 1-4 test cases, TEST_READY.md | None (Parallel) | DONE |

---

## Interface Contracts

### 1. InkAndAsh.Core ↔ InkAndAsh.Data
```csharp
namespace InkAndAsh.Core.Events {
    public abstract class GameEvent<T> : ScriptableObject {
        public void Raise(T value);
        public void RegisterListener(IGameEventListener<T> listener);
        public void UnregisterListener(IGameEventListener<T> listener);
    }
}
```

### 2. InkAndAsh.Data ↔ All Domain Systems
```csharp
namespace InkAndAsh.Data {
    public class DataManager {
        public static DataManager Instance { get; }
        public IReadOnlyDictionary<string, ItemData> Items { get; }
        public IReadOnlyDictionary<string, NpcData> Npcs { get; }
        public IReadOnlyDictionary<string, QuestData> Quests { get; }
        public IReadOnlyDictionary<string, BiomeData> Biomes { get; }
        public void LoadAllCatalogs();
    }
}
```

### 3. InkAndAsh.Combat Pure C# Interface
```csharp
namespace InkAndAsh.Combat {
    public class CtbCombatEngine {
        public float CurrentTime { get; }
        public IReadOnlyList<Combatant> Timeline { get; }
        public void AddCombatant(Combatant combatant);
        public void RemoveCombatant(string id);
        public float CalculateTurnDelay(Combatant actor, int skillCost, float weatherMultiplier = 1.0f);
        public void CommitAction(Combatant actor, int skillCost, float weatherMultiplier = 1.0f);
        public List<Combatant> PreviewAction(Combatant actor, int skillCost, float weatherMultiplier = 1.0f);
        public bool CheckSyncMoveAvailable(Combatant allyA, Combatant allyB);
        public bool PerformIntervention(Combatant protector, Combatant targetAlly);
        public void SetMediatorsAnchor(Combatant allyA, Combatant allyB);
        public void ApplyInjuryDebuff(Combatant combatant);
        public void ClearInjuryDebuff(Combatant combatant);
    }
}
```

### 4. InkAndAsh.Economy Caravan Inventory Interface
```csharp
namespace InkAndAsh.Economy {
    public enum StorageTier { FootPacks, HorseCarriage }
    public class CaravanInventory {
        public StorageTier Tier { get; }
        public int MaxSlots { get; }
        public float MaxWeightKg { get; }
        public float CurrentWeightKg { get; }
        public int UsedSlots { get; }
        public bool AddItem(string itemId, int quantity = 1, float durability = 100f);
        public bool RemoveItem(string itemId, int quantity = 1);
        public void TickDecay(float realMinutesElapsed, float biomeWeatherMultiplier = 1.0f);
        public bool CookRecipe(List<string> ingredientIds, out string buffId, out float buffDurationHours);
    }
}
```

### 5. InkAndAsh.World Weather Interface
```csharp
namespace InkAndAsh.World {
    public enum WeatherType { Clear, Rain, Heat, Freezing, Ashfall }
    public class WeatherSystem {
        public WeatherType CurrentWeather { get; }
        public float SpoilageMultiplier { get; }
        public float StaminaMultiplier { get; }
        public float CombatDelayMultiplier { get; }
        public void SetBiome(string biomeId);
        public void SetWeather(WeatherType weather);
    }
}
```
