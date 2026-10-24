# Original User Request

## 2026-10-24T19:51:01Z

Scaffold and implement the full C# codebase architecture for a Unity 6 (URP) cozy-melancholic JRPG called *The Ink and the Ash*. The project targets Android and Windows PC at 60 FPS. This run delivers everything that can be built in pure C# without purchased art assets — project structure, assembly definitions, data models, JSON catalogs, core runtime systems, and unit tests — plus initializes a public GitHub repository.

Working directory: d:\Unity Projects\Ink and Ash
Integrity mode: development

Reference material: The full game design document is at `d:\Unity Projects\Ink and Ash\Ink and Ash.md`. The technical spec is at `d:\Unity Projects\Ink and Ash\specs.md`. The agent guidelines are at `d:\Unity Projects\Ink and Ash\agents.md`. Read all three before beginning work.

## Requirements

### R1. Unity 6 Project Structure and Assembly Definitions

Set up a valid Unity 6 project in the working directory (which already contains design docs at the root). Create the standard Unity folder hierarchy (`Assets/`, `Packages/`, `ProjectSettings/`). Configure ProjectSettings for: Universal Render Pipeline (URP), Linear color space, .NET Standard 2.1 API compatibility, 60 FPS target frame rate, ASTC texture compression for Android, and BC7 for Windows Standalone. Create seven Assembly Definition files under `Assets/Scripts/` that enforce compile-time separation:

- `InkAndAsh.Core` — GameManager, TimeManager, event channels, object pooling, save system hooks
- `InkAndAsh.Data` — JSON schemas, ScriptableObject definitions, item/NPC/quest data catalogs
- `InkAndAsh.Combat` — CTB timeline engine, turn calculation, skills, status effects, teamwork mechanics
- `InkAndAsh.Economy` — Caravan inventory ledger, item decay/spoilage, campsite cooking
- `InkAndAsh.World` — Biome definitions, Spire calibration state, overworld map data, weather system
- `InkAndAsh.Narrative` — Dialogue data structures, 5-Act quest state machine, PAD emotional model, faction tracking, rumor propagation
- `InkAndAsh.UI` — Timeline bar, caravan ledger UI, dialogue box, campsite interface (placeholder MonoBehaviours with public API surface; no visual layout without assets)

Each assembly definition must declare only the inter-assembly references it actually needs (no circular dependencies). Add `com.unity.nuget.newtonsoft-json` to the Packages manifest for complex JSON serialization.

### R2. Data Models and JSON Catalogs

Implement C# data model classes for all game entities and serialize seed data into human-readable JSON files under `Assets/Data/`.

**Items (16 unified trade goods):** Golden Barley, Sun-Dried Kelp, Ironwood Resin, Glacier Salt, Bog Ash, Solar Clay, Canopy Bloom, Wild Game Meat, Fresh River Fish, Spring Water, Aromatic Herbs, Timber Logs, Scrap Iron, Coarse Fiber, Tallow Wax, Imperial Ink. Each item record contains: `itemId`, `name`, `primaryBiome`, `baseWeightKg`, `decayRatePerMinute` (0 for non-perishable), `isPerishable`, and `usageCategories` (array of: Trade, Crafting, Culinary, Alchemy). Populate all 16 items with values consistent with the GDD's perishability and biome-origin tables.

**NPCs:** Data structure supporting three tiers (Core Cast ~10-15, Village Anchors ~40-50, Background Cast ~150+). Fields: `npcId`, `tier`, `name`, `homeBiome`, `factionAffiliation` (one of: Traditionalists, Pragmatists, YoungRadicals, TheBurdened, Outsiders, ImperialBureaucracy), PAD emotional state (`pleasure`, `arousal`, `dominance` each -100 to +100), relationship vectors (`imperialFear`, `factionTrust`). Create at least 3 sample NPC JSON records (one per tier).

**Quest Arcs:** 5-Act state machine data: `questId`, `npcId`, `currentAct` (enum: Friction, Ripple, Crisis, Aftermath, Legacy), per-act completion flags, reward hooks. Create at least 1 sample quest JSON record.

**Biomes (7):** Grasslands/Verdant Spire, Coast/Tidal Spire, Forest/Sylvan Spire, Mountains/Frost Spire, Swamp/Mire Spire, Desert/Sun Spire, Jungle/Canopy Spire. Fields: `biomeId`, `name`, `spireName`, `weatherHazards`, `spoilageMultiplier`, `staminaDrainMultiplier`, `spireCalibrated` (bool).

Implement a `DataManager` class that loads and validates all JSON catalogs at runtime with fallback error logging.

### R3. Core Runtime Systems

Implement the following systems as C# MonoBehaviours or plain classes within their respective assemblies:

**GameManager (Core):** Persistent singleton (`DontDestroyOnLoad`) holding references to all runtime data stores. Survives scene transitions.

**TimeManager (Core):** 1 real-time minute = 1 in-game hour. Tracks current in-game hour, day, and weather state. Exposes events for hour-tick and day-tick.

**Event Channels (Core):** ScriptableObject-based event channel pattern. Create base generic `GameEvent<T>` and `GameEventListener<T>` classes. Create concrete event types for: `OnCombatStarted`, `OnCombatEnded`, `OnTimeHourTick`, `OnTimeDayTick`, `OnItemDecayed`, `OnQuestActAdvanced`, `OnSpireCalibrated`.

**CTB Combat Engine (Combat):** Implement the turn-order formula: `BaseTurnDelay = (SkillCost / Agility) * WeatherMultiplier`, `NextTurnTime = CurrentTime + BaseTurnDelay`. Maintain a sorted timeline queue of combatants. Support: adding/removing combatants, previewing timeline shifts before committing an action, Sync Move detection (two allies with timeline delta <= 2), Intervention spending (25 Focus Points to intercept or advance ally by up to 15 timeline units), Mediator's Anchor (links two allies for guaranteed Sync Moves). Track Injury Debuffs (-20% turn generation, +15% skill delay costs) and clearing conditions. The combat engine must be testable without MonoBehaviour dependencies (pure C# logic with a thin MonoBehaviour wrapper).

**Caravan Inventory (Economy):** List-based ledger (not spatial grid). Two capacity tiers: Foot Packs (30 slots, 50 kg max, no decay protection) and Horse-Drawn Carriage (120 slots, 300 kg max, 60% decay reduction). Real-time durability decay: 1% base per real-time minute, modified by biome weather multipliers (Desert +50%, Rain/Swamp +50%, Freezing reduces to 0.2%/min). Expose methods for adding items, removing items, querying current weight/slot usage, and ticking decay.

**Weather System (World):** Per-biome weather states (Clear, Rain, Heat, Freezing) with configurable spoilage and stamina multipliers matching the GDD values.

### R4. Unit Tests

Create an Edit Mode test assembly (`InkAndAsh.Tests.EditMode`) using Unity Test Framework (NUnit). Write tests covering:

- JSON deserialization: all 16 items load without null references or parsing exceptions.
- JSON deserialization: sample NPC and quest records load correctly with valid PAD ranges and faction enums.
- CTB formula: `BaseTurnDelay` calculation produces correct values for known inputs across multiple agility/skill-cost/weather-multiplier combinations.
- CTB timeline ordering: given 4 combatants with known agility values, the timeline queue sorts correctly after each action.
- CTB Sync Move detection: returns true when two allies have timeline delta <= 2, false otherwise.
- CTB Injury Debuffs: -20% turn generation and +15% delay costs are applied and cleared correctly.
- Caravan inventory: adding items respects slot and weight limits; decay ticks reduce durability at the correct rate; weather multipliers are applied correctly; carriage tier provides 60% decay reduction.
- TimeManager: advancing 1 real-time minute produces exactly 1 in-game hour tick.

### R5. Git Repository

Initialize a Git repository in the working directory. Create a `.gitignore` appropriate for Unity (exclude Library/, Temp/, Obj/, UserSettings/, build outputs, IDE caches). Commit all project files. Create a public GitHub repository named `ink-and-ash` under the authenticated account (`gh` CLI is authenticated as `miguelp001`) and push the initial commit.

## Acceptance Criteria

### Project Structure
- [ ] The working directory is a valid Unity 6 project that can be opened in Unity Hub without import errors (ProjectSettings/, Assets/, Packages/ all present and correctly configured).
- [ ] Seven `.asmdef` files exist under `Assets/Scripts/`, one per assembly, with no circular dependency references.
- [ ] `com.unity.nuget.newtonsoft-json` is listed in `Packages/manifest.json`.

### Data Completeness
- [ ] `Assets/Data/Items/` contains a JSON file with exactly 16 item records, each having all required fields populated with GDD-consistent values.
- [ ] `Assets/Data/NPCs/` contains at least 3 sample NPC JSON records (one Tier 1, one Tier 2, one Tier 3) with valid PAD ranges (-100 to +100) and valid faction affiliations.
- [ ] `Assets/Data/Quests/` contains at least 1 sample quest JSON record with a valid 5-Act state structure.
- [ ] `Assets/Data/Biomes/` contains 7 biome JSON records matching the GDD biome matrix.
- [ ] `DataManager` loads all JSON catalogs without exceptions when invoked in a test harness.

### Core Systems
- [ ] `GameManager` persists across scene transitions via `DontDestroyOnLoad`.
- [ ] `TimeManager` correctly maps 1 real-time minute to 1 in-game hour and fires hour/day tick events.
- [ ] CTB combat engine correctly calculates `BaseTurnDelay = (SkillCost / Agility) * WeatherMultiplier` and maintains a correctly sorted timeline queue.
- [ ] Sync Move detection, Intervention mechanics, and Mediator's Anchor function according to GDD rules.
- [ ] Caravan inventory enforces slot/weight limits per tier and applies decay with correct weather multipliers.

### Tests
- [ ] All unit tests in `InkAndAsh.Tests.EditMode` pass when run via `dotnet test` or Unity Test Runner.
- [ ] Test coverage includes at minimum: JSON deserialization (items, NPCs, quests), CTB formula calculations, CTB timeline ordering, Sync Move detection, Injury Debuff application/clearing, caravan slot/weight limits, caravan decay rates, and TimeManager hour-tick mapping.

### Git Repository
- [ ] The working directory is a Git repository with at least one commit containing all project files.
- [ ] A public GitHub repository `miguelp001/ink-and-ash` exists and contains the pushed commit(s).
- [ ] `.gitignore` excludes Unity-standard directories (Library/, Temp/, Obj/, UserSettings/).
