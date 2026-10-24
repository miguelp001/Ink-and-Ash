# Technical Specification Report: R3 Core Runtime Systems & R4 EditMode Unit Tests

**Document ID:** SPEC-SURVEY-R3-R4-001  
**Target Project:** The Ink and the Ash  
**Engine & Version:** Unity 6 (6000.6.4f1 URP), .NET Standard 2.1 / C# 9.0+  
**Target Platforms:** Android (Mobile) & Windows PC (Desktop)  
**Author:** Spec Miner 3 (`teamwork_preview_spec_miner`)  
**Date:** 2026-10-24  

---

## 1. Executive Summary & Authoritative Sources

This specification document provides the complete, authoritative technical definition for **R3 (Core Runtime Systems)** and **R4 (Unit Tests in EditMode Assembly)** for *The Ink and the Ash*.

### Authoritative Specification Sources
1. `ORIGINAL_REQUEST.md`: High-level requirements R3 and R4, core system contracts, acceptance criteria.
2. `Ink and Ash.md`: Master Game Design Document covering CTB formulas, perishable logistics, weather multipliers, campsite recovery loop, and production architecture.
3. `specs.md`: Technical Specification for Unity 6, 60 FPS zero-allocation performance, URP settings, and input abstraction.
4. `agents.md`: Core design tenets, narrative tone mandates, and system constraints.

---

## 2. Features Discovered

| # | Category | Feature | Description | Inputs | Outputs | Error Behavior | Discovered Via |
|---|----------|---------|-------------|--------|---------|----------------|----------------|
| 1 | Runtime Architecture | GameManager Singleton | Persistent singleton surviving scene transitions via `DontDestroyOnLoad` holding references to active runtime data stores | Unity scene lifecycle `Awake()` | Persistent instance reference `GameManager.Instance` | Destroys duplicate instances if already instantiated | `ORIGINAL_REQUEST.md` § R3; `Ink and Ash.md` § 8.2 |
| 2 | Runtime Architecture | GameState State Machine | Five-state finite state machine controlling game mode context: Exploration, Campsite, Combat, Dialogue, Paused | Desired target `GameState` enum | State transition event `OnGameStateChanged(oldState, newState)` | Rejects invalid direct transitions (e.g., Campsite to Combat) with warning log | Dispatch Assignment; `Ink and Ash.md` § 1.3, 3.1, 4.2 |
| 3 | Chronology | Time Ratio Conversion | Clock advancing at fixed ratio of 1 real-time minute to 1 in-game hour (24 real minutes = 1 in-game day) | Real-time `Time.deltaTime` (seconds) | Scaled in-game time (hours, minutes, days) | Clamps delta time if frame hitch occurs to avoid temporal skipping | `ORIGINAL_REQUEST.md` § R3; `Ink and Ash.md` § 2.3 |
| 4 | Chronology | In-Game Clock & Ticks | Tracks current hour (0-23) and day (1+), emitting events on hour boundaries and day rollovers | Accumulated in-game minutes | `OnTimeHourTick(hour, day)` and `OnTimeDayTick(day)` | Fires missed ticks sequentially if time is skipped/rested | `ORIGINAL_REQUEST.md` § R3; `Ink and Ash.md` § 2.3 |
| 5 | Chronology | Campsite Rest Time Skip | Rest loop advancing in-game time by exactly 8 hours during overnight sleep | Call to `AdvanceTime(8.0f)` | Sequential hour ticks, perishable decay ticks, day rollover if crossing midnight | Rejects negative or zero time skip values | `Ink and Ash.md` § 3.3, 4.3 |
| 6 | Event Channels | ScriptableObject Event Architecture | Decoupled cross-assembly event passing using generic ScriptableObjects with zero runtime allocations | Generic payload `T` passed to `Raise(T)` | Dispatches to registered `IGameEventListener<T>` listeners | Tolerates missing listeners gracefully with zero exceptions | `ORIGINAL_REQUEST.md` § R3; `Ink and Ash.md` § 8.2 |
| 7 | Event Channels | Pure C# Event Channel Bridge | Interface and in-memory channel implementation allowing pure C# tests without Unity ScriptableObject instantiation | Payload `T` passed to `Raise(T)` or subscribed via `Action<T>` | Dispatches to subscribed in-memory delegates | Removes delegate on disposal; ignores null callbacks | `ORIGINAL_REQUEST.md` § R3, R4 |
| 8 | Event Channels | Concrete Event Types | Predefined channels: `OnCombatStarted`, `OnCombatEnded`, `OnTimeHourTick`, `OnTimeDayTick`, `OnItemDecayed`, `OnQuestActAdvanced`, `OnSpireCalibrated` | Specific event payload structs | Strongly typed event broadcast across assembly boundaries | Throws compile-time error if payload type mismatches | `ORIGINAL_REQUEST.md` § R3 |
| 9 | CTB Combat | Pure C# Combat Engine | Self-contained turn-based engine decoupled from `UnityEngine.Object` and `MonoBehaviour` for instant headless unit testing | Combatant roster, initial battlefield state, weather conditions | Deterministic battle state updates, action results, timeline ordering | Returns error codes/exceptions for illegal combat actions or target mismatch | `ORIGINAL_REQUEST.md` § R3, R4; Dispatch Assignment |
| 10 | CTB Combat | Base Turn Delay Calculation | Mathematical calculation of delay ticks: `BaseTurnDelay = (SkillCost / Agility) * WeatherMultiplier` (or normalized tick formula) | `SkillCost` (float), `Agility` (float), `WeatherMultiplier` (float) | `BaseTurnDelay` (float timeline units) | Throws `DivideByZeroException` if `Agility <= 0`; clamps minimum Agility to 1 | `ORIGINAL_REQUEST.md` § R3; `Ink and Ash.md` § 3.1 |
| 11 | CTB Combat | Timeline Queue & Ordering | Priority-sorted timeline queue of all combatants ordered by ascending `NextTurnTime` | List of combatants with agility attributes | Sorted timeline list with head combatant ready to act | Deterministic tie-breaking: allies take priority over enemies, then higher Agility | `ORIGINAL_REQUEST.md` § R3; `Ink and Ash.md` § 3.1 |
| 12 | CTB Combat | Dynamic Timeline Preview | Read-only simulation predicting the new timeline order if a combatant commits to a skill with a given cost | Candidate `Combatant` and `SkillCost` | Projected `List<Combatant>` preview without mutating active combat state | Returns current timeline unchanged if candidate skill cost is negative or invalid | `ORIGINAL_REQUEST.md` § R3; `Ink and Ash.md` § 3.1 |
| 13 | CTB Combat | Sync Move Cooperative Actions | Cooperative combo ability triggered when two active allies are adjacent on timeline with `Turn Delta <= 2` units | Pair of active ally combatants | `CanSyncMove` boolean; executes combo without extra turn delay | Rejects sync move if either combatant is an enemy or if timeline delta > 2 | `ORIGINAL_REQUEST.md` § R3; `Ink and Ash.md` § 3.2 |
| 14 | CTB Combat | Intervention Reaction | Defensive synergy allowing a healthy ally to expend 25 Focus Points (FP) to intercept attack or advance ally timeline by up to 15 units | Intervening ally (>= 25 FP), targeted ally | Deducts 25 FP, shifts target timeline: `NextTurnTime = Max(CurrentTime, NextTurnTime - 15)` | Fails if intervening ally has < 25 FP or is dead/incapacitated | `ORIGINAL_REQUEST.md` § R3; `Ink and Ash.md` § 3.2 |
| 15 | CTB Combat | Mediator's Anchor | Specialized Vicar-Mediator ability linking two allies to guarantee Sync Moves regardless of timeline delta or speed debuffs | Expends Vicar turn, designates two allies | Links allies via `LinkedAllyId`; forces `CanSyncMove = true` | Fails if target allies are invalid or incapacitated | `ORIGINAL_REQUEST.md` § R3; `Ink and Ash.md` § 3.2 |
| 16 | CTB Combat | Defeat & Campsite Reload | Party defeat handler: wipes active party, triggers reload at last campsite, applies temporary Injury Debuffs | All 3 active party members reduced to 0 HP | Raises `OnCombatEnded(Defeat)`, restores party at camp with Injury Debuffs | None (guaranteed deterministic recovery state) | `ORIGINAL_REQUEST.md` § R3; `Ink and Ash.md` § 3.3 |
| 17 | CTB Combat | Injury Debuff Application | Systemic penalty reducing turn generation by 20% (-20% Agility) and increasing skill delay costs by 15% (+15% SkillCost) | Defeat state or critical trauma flag | `EffectiveAgility = Agility * 0.8f`, `EffectiveSkillCost = SkillCost * 1.15f` | Re-application does not stack multipliers beyond standard debuff limit | `ORIGINAL_REQUEST.md` § R3; `Ink and Ash.md` § 3.3 |
| 18 | CTB Combat | Injury Debuff Clearance | Recovery loop clearing Injury Debuffs after a full campsite rest (8 in-game hours) and consumption of cooked meals | Campsite rest completion + meal consumption | Sets `HasInjuryDebuff = false`, restores base agility and skill costs | Debuff persists if rest is interrupted or meal is omitted | `ORIGINAL_REQUEST.md` § R3; `Ink and Ash.md` § 3.3 |
| 19 | Caravan Inventory | List-Based Caravan Ledger | Non-spatial flat list inventory storing 16 unified trade goods without grid-fitting puzzle mechanics | Item addition/removal requests | Ledger inventory entries with item references, counts, and durabilities | Prevents duplicate slots for non-perishable stackable goods | `ORIGINAL_REQUEST.md` § R3; `specs.md` § 5; `agents.md` § 4 |
| 20 | Caravan Inventory | Capacity Tiers Enforcement | Capacity limits: Foot Packs (30 slots, 50 kg max) vs Horse-Drawn Carriage (120 slots, 300 kg max) | Target tier configuration, added items | Inventory acceptance or rejection based on current slot count and total weight | Rejects items exceeding tier slot or weight threshold with specific error | `ORIGINAL_REQUEST.md` § R3; `Ink and Ash.md` § 2.3 |
| 21 | Caravan Inventory | Moisture Weight Penalty | High humidity / rain environmental hazard adding +50% moisture weight to dry absorbent goods (Golden Barley, Coarse Fiber) | Item humidity sensitivity flag, current weather state | Effective item weight = `BaseWeight * 1.5f` in Rain/Swamp | Weight normalizes back to base when entering dry climate | `Ink and Ash.md` § 2.3, 6.2 |
| 22 | Caravan Inventory | Real-Time Perishable Decay | Base durability loss of 1% per real-time minute (= 1 in-game hour) on perishable goods | In-game time passage / minute tick | Item durability reduced: `Durability -= DecayAmount` | Durability clamped between 0% and 100%; non-perishables take 0 decay | `ORIGINAL_REQUEST.md` § R3; `Ink and Ash.md` § 2.3 |
| 23 | Caravan Inventory | Biome Weather Spoilage Multipliers | Accelerated or retarded decay: Desert Heat (+50% -> 1.5%/min), Rain/Swamp (+50% -> 1.5%/min), Freezing (reduces to 0.2%/min) | Current `WeatherState` / `BiomeId` | Modulates decay rate per minute | Fallback to 1.0x baseline if weather state unrecognized | `ORIGINAL_REQUEST.md` § R3; `Ink and Ash.md` § 2.3 |
| 24 | Caravan Inventory | Carriage Storage Decay Reduction | Horse-Drawn Carriage equipped with storage boxes and cooling linings reduces all ambient item decay rates by flat 60% | Active caravan tier = `HorseDrawnCarriage` | Decay multiplier scaled by `(1.0 - 0.60) = 0.40` | Foot packs receive 0% reduction (decay multiplier = 1.0) | `ORIGINAL_REQUEST.md` § R3; `Ink and Ash.md` § 2.3 |
| 25 | Caravan Inventory | Spoilage & Waste Event | Notification and state change when perishable durability reaches 0% | Perishable durability reaching 0.0f | Emits `OnItemDecayed(WasSpoiled = true)`; marks item as spoiled waste | Prevents negative durability values; prevents usage in culinary recipes | `ORIGINAL_REQUEST.md` § R3; `Ink and Ash.md` § 6.2 |
| 26 | Campsite Cooking | Recipe Ingredient Validation | Validates whether the caravan ledger contains the required quantities of unified ingredients before cooking | Recipe definition, caravan inventory ledger | Validation boolean `CanCookRecipe(recipeId)` | Rejects cooking if any ingredient is missing or spoiled | `ORIGINAL_REQUEST.md` § R3; `Ink and Ash.md` § 6.2 |
| 27 | Campsite Cooking | Meal Preparation & Buff Granting | Consumes raw ingredients, clears party injury debuffs, and grants a 24-in-game-hour regional expedition buff | Valid recipe selection at campsite | Deducts ingredients, applies `ExpeditionBuff` to active party | Throws exception if attempted outside safe Campsite state | `ORIGINAL_REQUEST.md` § R3; `Ink and Ash.md` § 4.3, 6.2 |
| 28 | Campsite Cooking | Expedition Buff Expiration | Tracks active 24 in-game hour duration window (24 real-time minutes), expiring buffs when time elapses | In-game time advancement | Removes active buff effects upon expiration | Replaces existing buff of same type or refreshes duration | `Ink and Ash.md` § 4.3, 6.2 |
| 29 | Weather System | 7 Biome Climate Configuration | Configures baseline weather, environmental hazards, spoilage multipliers, and stamina drain for each of the 7 biomes | Biome identifier (`BiomeId`) | Biome parameters (SpoilageMultiplier, StaminaMultiplier, etc.) | Fallback to Grasslands defaults if biomeId is invalid | `ORIGINAL_REQUEST.md` § R2, R3; `Ink and Ash.md` § 2.2 |
| 30 | Weather System | Spire Calibration World State | Tracks whether the regional ancient Spire is calibrated; calibrated spires calm environmental anomalies and monsters | Spire lighting sequence completion | Sets `SpireCalibrated = true`; calms weather hazards, unlocks trade routes | Prevents recalibration if already calibrated | `ORIGINAL_REQUEST.md` § R2, R3; `Ink and Ash.md` § 2.4 |
| 31 | Weather System | Dynamic Weather State Machine | Probabilistic state machine transitioning weather (Clear, Rain, Heatwave, Blizzard, Sandstorm, Ashfall) | In-game hour tick, biome climate table, spire calibration | Sets active `CurrentWeather`; updates multipliers | Ensures transitions follow climatic plausibility rules | `ORIGINAL_REQUEST.md` § R3; `Ink and Ash.md` § 2.2, 2.3 |
| 32 | EditMode Tests | Test Assembly Definition Setup | Configuration of `InkAndAsh.Tests.EditMode.asmdef` with explicit references to test framework and game assemblies | Asmdef manifest file | Compiles isolated test assembly targeting Editor platform | Fails compilation if circular dependencies or missing assemblies exist | `ORIGINAL_REQUEST.md` § R4 |
| 33 | EditMode Tests | CTB Formula & Timeline Test Suite | Automated test cases verifying BaseTurnDelay math, tie-breaking, queue sorting, and turn advancing | Test inputs for agility, cost, weather, combatant lists | NUnit assertions verifying exact float and ordering equality | Fails test if delay calculation deviates by > 0.001f | `ORIGINAL_REQUEST.md` § R4 |
| 34 | EditMode Tests | Teamwork & Debuff Test Suite | Automated test cases verifying Sync Move detection (delta <= 2), Intervention (25 FP & -15 shift), Anchor, and Injury Debuffs | Combatant state configurations | NUnit assertions confirming teamwork predicates and debuff stat math | Fails test if invalid delta triggers Sync Move | `ORIGINAL_REQUEST.md` § R4 |
| 35 | EditMode Tests | Caravan Capacity & Decay Test Suite | Automated test cases verifying Foot Packs/Carriage slot and weight caps, moisture weight, decay math, and carriage 60% reduction | Mock item additions and time elapsed ticks | NUnit assertions verifying capacity limits and decay rates | Fails test if carriage reduction != 60% or decay math is inaccurate | `ORIGINAL_REQUEST.md` § R4 |
| 36 | EditMode Tests | TimeManager & Clock Test Suite | Automated test cases verifying 1 real-minute to 1 in-game hour ratio, tick events, and day rollovers | Simulated real-time delta elapsed | NUnit assertions checking exact hour and day progression | Fails test if advancing 60 seconds does not equal exactly 1 hour | `ORIGINAL_REQUEST.md` § R4 |
| 37 | EditMode Tests | Batchmode & Dotnet Test Execution | Dual execution strategy: Unity headless batchmode CLI runner and pure C# NUnit CLI runners | Project path and test runner command line | XML / stdout test results report with pass/fail exit code | Returns non-zero exit code if any test fails | `ORIGINAL_REQUEST.md` § R4 |

---

## 3. Edge Cases & Boundary Value Analysis

| # | Feature | Input | Observed Behavior |
|---|---------|-------|-------------------|
| 1 | CTB Formula | Agility = 0 | Must throw `DivideByZeroException` or clamp to minimum Agility of 1.0f to prevent infinite turn delay. |
| 2 | CTB Formula | SkillCost = 0 (Free Action) | `BaseTurnDelay = 0`. Actor remains at head of timeline queue for another immediate action. |
| 3 | CTB Formula | Agility > 1000 (Extreme Stat) | Delay becomes tiny fraction (e.g. 0.05 units). Timeline correctly positions actor ahead of all other units without floating-point underflow. |
| 4 | CTB Formula | WeatherMultiplier = 0 | Prohibited. Clamped to minimum 0.1f to prevent instant zero-delay turns in abnormal weather. |
| 5 | CTB Timeline Ordering | Equal `NextTurnTime` for 2 Allies | Stable tie-breaking: higher Agility acts first; if Agility is equal, sort by stable unique ID. |
| 6 | CTB Timeline Ordering | Equal `NextTurnTime` for Ally and Enemy | Ally is awarded priority over enemy combatant to reward proactive tactical positioning. |
| 7 | CTB Sync Move | Timeline Delta = Exactly 2.000f | `Math.Abs(timeA - timeB) <= 2.0f` evaluates to `true`. Sync Move is allowed. |
| 8 | CTB Sync Move | Timeline Delta = 2.001f | Evaluates to `false`. Sync Move is disallowed. |
| 9 | CTB Sync Move | Dead / Incapacitated Ally with Delta = 1.0f | Evaluates to `false`. Both allies must be alive (`CurrentHealth > 0`) and active. |
| 10 | CTB Intervention | Focus Points = Exactly 25 | Intervention succeeds; FP is reduced to exactly 0. Target ally advanced by 15 units. |
| 11 | CTB Intervention | Focus Points = 24.99 | Fails validation; returns `false`. FP remains unchanged; intervention is rejected. |
| 12 | CTB Intervention | Target Ally NextTurnTime - 15 < CurrentTime | Clamped to `CurrentTime` (`Math.Max(CurrentTime, NextTurnTime - 15)`). Turn cannot be placed in the past. |
| 13 | CTB Mediator's Anchor | Linked Allies with Timeline Delta = 50.0f | `CanSyncMove` returns `true`. Mediator's Anchor overrides normal delta <= 2 constraint. |
| 14 | CTB Mediator's Anchor | One of the Linked Allies dies | Link breaks or remains dormant; `CanSyncMove` returns `false` due to deceased status. |
| 15 | CTB Injury Debuff | Agility = 10, Injury Debuff applied | Effective Agility becomes `10 * 0.8 = 8.0f`. Delay increases by 25%. |
| 16 | CTB Injury Debuff | SkillCost = 100, Injury Debuff applied | Effective SkillCost becomes `100 * 1.15 = 115.0f`. |
| 17 | CTB Injury Debuff | Combined Injury Debuffs on Turn Delay | `EffectiveDelay = (115 / 8) = 14.375` compared to baseline `100 / 10 = 10.0` (+43.75% delay). |
| 18 | Caravan Inventory | Foot Packs: Adding 30th item slot | Accepted. `TotalSlotsUsed = 30`. |
| 19 | Caravan Inventory | Foot Packs: Adding 31st item slot | Rejected. Throws `InventoryFullException` or returns `false`. Current inventory untouched. |
| 20 | Caravan Inventory | Foot Packs: Adding item bringing weight to exactly 50.00 kg | Accepted. Weight limit met exactly. |
| 21 | Caravan Inventory | Foot Packs: Adding item bringing weight to 50.01 kg | Rejected. Exceeds 50.0 kg limit. |
| 22 | Caravan Inventory | Carriage: Adding items up to 120 slots and 300.0 kg | Accepted up to 120 slots and 300.0 kg. |
| 23 | Caravan Inventory | Golden Barley (10 kg) in Rain / Swamp humidity | Moisture penalty adds +50% weight -> effective weight becomes 15.0 kg. May trigger encumbrance! |
| 24 | Caravan Inventory | Moving from Rain to Clear weather with Golden Barley | Moisture weight bonus expires -> effective weight returns to 10.0 kg. |
| 25 | Caravan Inventory | Non-Perishable Item (Scrap Iron) decaying over 1000 mins | Durability remains exactly 100.0%. Decay rate is 0. |
| 26 | Caravan Inventory | Durability hits exactly 0.0f | Item marked `IsSpoiled = true`. Emits `OnItemDecayed` event with `WasSpoiled = true`. |
| 27 | Caravan Inventory | Durability decay tick would reduce durability below 0 | Durability clamped to `0.0f`; does not become negative. |
| 28 | Caravan Inventory | Carriage 60% decay reduction in Desert Heat (1.5%/min) | `EffectiveDecay = 1.0% * 1.5 * (1.0 - 0.60) = 0.60% / min`. After 10 mins: exactly 6.0% lost. |
| 29 | Caravan Inventory | Carriage 60% decay reduction in Freezing (0.2%/min) | `EffectiveDecay = 1.0% * 0.2 * (1.0 - 0.60) = 0.08% / min`. After 100 mins: exactly 8.0% lost. |
| 30 | Campsite Cooking | Valid recipe, missing 1 of 3 required items | Validation returns `false`. No ingredients are consumed; no buff applied. |
| 31 | Campsite Cooking | Valid recipe, ingredient has durability = 0.0f (spoiled) | Spoiled ingredients cannot be used in cooking. Validation returns `false`. |
| 32 | Campsite Cooking | Consuming meal granting 24-hour buff at 12:00 Day 1 | Buff remains active through Day 1 and expires at exactly 12:00 Day 2. |
| 33 | Campsite Cooking | Applying second buff of identical type before first expires | Refreshes duration to 24 in-game hours from current time; does not stack power. |
| 34 | TimeManager | Advancing 59.99 real seconds | In-game clock advances 59 minutes and 59 seconds. No hour tick yet. |
| 35 | TimeManager | Advancing 60.00 real seconds | Exactly 1 in-game hour advances. `OnTimeHourTick` fires with current hour and day. |
| 36 | TimeManager | Crossing Hour 23 to Hour 0 | Hour rolls over to 0; `CurrentDay` increments from 1 to 2; `OnTimeDayTick(2)` fires. |
| 37 | TimeManager | Rest skip of 8.0 hours crossing midnight (e.g. 20:00 to 04:00) | Day increments; 8 sequential hour ticks processed; perishable decay ticks applied for 480 in-game minutes. |
| 38 | TimeManager | Game Paused (`IsPaused = true` or `Time.timeScale = 0`) | In-game clock freezes. Zero decay ticks occur; no time events fire. |
| 39 | Weather System | Blizzard weather in Mountains biome | Spoilage rate multiplier drops to 0.2x; party stamina drain multiplier rises to 2.0x; CTB delay multiplier rises to 1.25x. |
| 40 | Weather System | Calibrating Frost Spire during Blizzard | Lit beacon calms extreme storm; weather stabilizes to Clear/Cold; stamina drain returns to 1.0x baseline. |

---

## 4. Deep-Dive Specification: R3 Core Runtime Systems

### 4.1 GameManager Architecture & State Machine

```
+-----------------------------------------------------------------------------------+
|                                  GameManager                                      |
|  - Persistent Singleton (DontDestroyOnLoad)                                       |
|  - Survives Scene Swaps (Overworld Paper Map 3D <-> 7 Biome Scenes)              |
|  - References Data Stores: CaravanLedger, TimeManager, WeatherManager, Quests    |
+-----------------------------------------------------------------------------------+
                                         |
                                         v
+-----------------------------------------------------------------------------------+
|                               GameState Machine                                   |
|                                                                                   |
|      +------------+        camp button        +----------+                        |
|      |            | ------------------------> |          |                        |
|      |            | <------------------------ | Campsite |                        |
|      |            |      morning depart       +----------+                        |
|      |            |                                ^                              |
|      |            |                                | party wipeout reload         |
|      |Exploration |                                | with Injury Debuffs          |
|      |            |        enemy contact      +----------+                        |
|      |            | ------------------------> |          |                        |
|      |            | <------------------------ |  Combat  |                        |
|      |            |      victory / retreat    +----------+                        |
|      |            |                                                               |
|      |            |        NPC click / tap    +----------+                        |
|      |            | ------------------------> |          |                        |
|      |            | <------------------------ | Dialogue |                        |
|      +------------+        dialogue end       +----------+                        |
|             |                                                                     |
|             |  open menu / esc                                                    |
|             v                                                                     |
|      +------------+                                                               |
|      |   Paused   |  (Time.timeScale = 0, clock frozen)                           |
|      +------------+                                                               |
+-----------------------------------------------------------------------------------+
```

#### GameManager Specification & Lifecycle
- **Assembly:** `InkAndAsh.Core`
- **Class:** `public class GameManager : MonoBehaviour`
- **Pattern:** Singleton with `DontDestroyOnLoad`.
- **Public API Surface:**
  ```csharp
  public static GameManager Instance { get; private set; }
  public GameState CurrentState { get; private set; }
  public event Action<GameState, GameState> OnGameStateChanged;

  public void ChangeState(GameState newState);
  public void PauseGame();
  public void ResumeGame();
  ```
- **State Transition Matrix:**
  - `Exploration` -> `Campsite`: Allowed (when in safe haven or overworld road camp).
  - `Campsite` -> `Exploration`: Allowed (upon morning departure).
  - `Exploration` -> `Combat`: Allowed (on proximity or contact with visible wandering hostile).
  - `Combat` -> `Exploration`: Allowed (upon victory, peaceful mediation, or retreat).
  - `Combat` -> `Campsite`: Allowed (upon party wipeout / defeat reload with Injury Debuffs).
  - `Exploration` -> `Dialogue`: Allowed (upon interacting with NPC / inspectable lore).
  - `Dialogue` -> `Exploration`: Allowed (when dialogue finishes).
  - `Any State` -> `Paused`: Allowed (stores `_previousState`, sets `Time.timeScale = 0`).
  - `Paused` -> `Previous State`: Allowed (restores previous state, restores `Time.timeScale = 1`).
  - Any direct transition not listed above (e.g., `Combat` -> `Dialogue`, `Campsite` -> `Combat`) logs a warning and is rejected.

---

### 4.2 TimeManager Specification & Clock Math

#### Time Conversion Ratios
- **Ratio:** `1 real-time minute = 1 in-game hour` (multiplier: 60.0x).
- **1 in-game minute** = `1.0 real-time second`.
- **1 in-game hour** = `60.0 real-time seconds`.
- **1 in-game day (24 hours)** = `24 real-time minutes` = `1440.0 real-time seconds`.

#### Class Interface (`InkAndAsh.Core`)
```csharp
public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    public int CurrentDay { get; private set; } = 1;
    public int CurrentHour { get; private set; } = 8; // Starts at 08:00 (Morning)
    public float CurrentMinute { get; private set; } = 0f;
    public float TotalElapsedInGameMinutes { get; private set; } = 0f;
    public bool IsPaused { get; set; } = false;

    public event Action<int, int> OnTimeHourTick; // (hour, day)
    public event Action<int> OnTimeDayTick;       // (day)

    public void Tick(float realDeltaTimeSeconds);
    public void AdvanceTime(float inGameHours); // Campsite rest skip
}
```

#### Time Progression Algorithm
1. If `IsPaused` or `GameManager.Instance.CurrentState == GameState.Paused`, delta time is not accumulated.
2. Accumulate real seconds: `CurrentMinute += realDeltaTimeSeconds;`
3. While `CurrentMinute >= 60.0f`:
   - `CurrentMinute -= 60.0f;`
   - `CurrentHour++;`
   - If `CurrentHour >= 24`:
     - `CurrentHour = 0;`
     - `CurrentDay++;`
     - Fire `OnTimeDayTick?.Invoke(CurrentDay);`
   - Fire `OnTimeHourTick?.Invoke(CurrentHour, CurrentDay);`
4. Campsite Rest (8 Hours):
   - Invoking `AdvanceTime(8.0f)` loops 8 times, advancing hours sequentially and triggering hour ticks, day rollovers, perishable decay calculations, and weather synchronization.

---

### 4.3 ScriptableObject & Pure C# Event Channel Architecture

#### Pattern Rationale
Decouples assemblies (e.g. `InkAndAsh.Combat` can notify `InkAndAsh.UI` and `InkAndAsh.Core` without referencing them).
Supports zero garbage collection allocations at runtime and Inspector debugging in Unity Editor.

#### Generic Base Architecture (`InkAndAsh.Core`)
```csharp
public interface IEventChannel<T>
{
    void Raise(T value);
    void RegisterListener(Action<T> listener);
    void UnregisterListener(Action<T> listener);
}

public abstract class GameEvent<T> : ScriptableObject, IEventChannel<T>
{
    private readonly List<Action<T>> _listeners = new List<Action<T>>();

    public void Raise(T value)
    {
        for (int i = _listeners.Count - 1; i >= 0; i--)
        {
            _listeners[i]?.Invoke(value);
        }
    }

    public void RegisterListener(Action<T> listener)
    {
        if (!_listeners.Contains(listener)) _listeners.Add(listener);
    }

    public void UnregisterListener(Action<T> listener)
    {
        _listeners.Remove(listener);
    }
}
```

#### Pure C# Testable Channel (`InkAndAsh.Core`)
```csharp
public class SimpleEventChannel<T> : IEventChannel<T>
{
    private event Action<T> _channel;

    public void Raise(T value) => _channel?.Invoke(value);
    public void RegisterListener(Action<T> listener) => _channel += listener;
    public void UnregisterListener(Action<T> listener) => _channel -= listener;
}
```

#### Concrete Event Channels Required
1. `OnCombatStarted`: Payload `CombatStartData` { List<Combatant> party, List<Combatant> enemies, string biomeId }
2. `OnCombatEnded`: Payload `CombatEndData` { CombatResult result (Victory, Defeat, PeacefulResolution), int rounds }
3. `OnTimeHourTick`: Payload `TimeTickData` { int hour, int day }
4. `OnTimeDayTick`: Payload `int day`
5. `OnItemDecayed`: Payload `ItemDecayedData` { string itemId, float currentDurability, bool wasSpoiled }
6. `OnQuestActAdvanced`: Payload `QuestActData` { string questId, string actId, int actNumber }
7. `OnSpireCalibrated`: Payload `SpireCalibratedData` { string biomeId, string spireName }

---

### 4.4 Pure C# CTB Combat Engine

#### Core Tenet: Zero UnityEngine Dependencies
The core combat logic resides in `InkAndAsh.Combat` as pure C# classes (`CtbCombatEngine`, `Combatant`, `CombatAction`, `TimelineQueue`). A separate MonoBehaviour `CombatController` serves as a presentation wrapper.

#### Mathematical Formulation & Equivalence
The turn-order formula from the specifications:
$$\text{BaseTurnDelay} = \left(\frac{\text{SkillCost}}{\text{Agility}}\right) \times \text{WeatherMultiplier}$$

In the normalized tick representation:
$$\text{Delay} = \text{ActionCost} \times \left(\frac{100}{\text{Agility}}\right) \times \text{WeatherMultiplier}$$

Both formulas are identical when $\text{SkillCost} = \text{ActionCost} \times 100$. For example:
- Standard Attack: $\text{ActionCost} = 1.0$ ($\text{SkillCost} = 100$).
- Fast Technique / Quick Jab: $\text{ActionCost} = 0.5$ ($\text{SkillCost} = 50$).
- Heavy Recovery Technique: $\text{ActionCost} = 1.5$ ($\text{SkillCost} = 150$).
- Actor with $\text{Agility} = 50$, $\text{WeatherMultiplier} = 1.0$:
  $$\text{Delay} = \frac{100}{50} \times 1.0 = 2.0\text{ timeline units}$$

#### Turn Progression Formula
$$\text{NextTurnTime} = \text{CurrentTime} + \text{BaseTurnDelay}$$

```
Initial Timeline State:
Time = 0.0
[Vicar (Agility 50, Delay 2.0) -> Next: 2.0]
[Companion A (Agility 40, Delay 2.5) -> Next: 2.5]
[Hostile Spirit (Agility 30, Delay 3.33) -> Next: 3.33]

Head acts at Time = 2.0:
Vicar executes Skill with Cost 100 -> New Delay = 2.0 -> New NextTurnTime = 2.0 + 2.0 = 4.0.
Queue re-sorted:
1. Companion A (Next: 2.5)
2. Hostile Spirit (Next: 3.33)
3. Vicar (Next: 4.0)
```

#### Teamwork Mechanics Specification
1. **Sync Moves (Cooperative Abilities):**
   - **Condition:** Two active party members satisfy:
     $$\Delta \text{TurnTime} = |\text{ally}_A.\text{NextTurnTime} - \text{ally}_B.\text{NextTurnTime}| \le 2.0$$
     **OR** $\text{ally}_A.\text{LinkedAllyId} == \text{ally}_B.\text{Id}$ (Mediator's Anchor).
   - **Execution:** Both allies act simultaneously, combining skills without incurring secondary turn latency.
2. **Interventions (Defensive Synergy):**
   - **Cost:** Exactly 25 Focus Points (FP).
   - **Condition:** Intervening ally has $\text{CurrentFP} \ge 25$, is alive, and targets an ally under enemy attack.
   - **Effect:** Deducts 25 FP. Intercepts damage OR accelerates target ally timeline:
     $$\text{targetAlly}.\text{NextTurnTime} = \max(\text{CurrentTime}, \text{targetAlly}.\text{NextTurnTime} - 15.0)$$
     Queue is immediately re-sorted.
3. **The Mediator's Anchor:**
   - **Actor:** Vicar-Mediator unique active ability.
   - **Action Cost:** 100 ticks (standard turn delay).
   - **Effect:** Sets reciprocal links:
     $$\text{ally}_1.\text{LinkedAllyId} = \text{ally}_2.\text{Id}; \quad \text{ally}_2.\text{LinkedAllyId} = \text{ally}_1.\text{Id}$$
   - **Mechanical Boon:** Guarantees `CanSyncMove = true` between the two companions regardless of timeline distance, enemy turn delay debuffs, or agility disparity.

#### Injury Debuffs & Defeat Loop
- **Trigger:** All 3 active party members reduced to $0\text{ HP}$.
- **Recovery:** Party wipes and reloads at last established campsite.
- **Penalty (Injury Debuffs):**
  - Timeline generation slowed by 20%:
    $$\text{EffectiveAgility} = \text{BaseAgility} \times 0.80$$
  - Skill delay costs increased by 15%:
    $$\text{EffectiveSkillCost} = \text{BaseSkillCost} \times 1.15$$
  - Net turn delay multiplier:
    $$\frac{1.15}{0.80} = 1.4375 \quad (+43.75\%\text{ delay})$$
- **Clearance:** Full sleep cycle (8 in-game hours) at campsite **plus** preparing and consuming a cooked meal.

---

### 4.5 Caravan Inventory & Perishable Economy

#### Ledger Structure
- Pure list-based ledger (`InkAndAsh.Economy.CaravanLedger`).
- No spatial grids, tetris packing, or rotational placement.
- Each entry tracks: `ItemId`, `Quantity`, `ItemDataReference`, `CurrentDurability` ($0.0$ to $100.0\%$).

#### Capacity Tiers
| Tier Name | Max Slots | Max Total Weight | Decay Protection |
|---|---|---|---|
| **Foot Packs (Base)** | 30 slots | 50.0 kg | 0% reduction ($\times 1.0$) |
| **Horse-Drawn Carriage (Upgraded)** | 120 slots | 300.0 kg | 60% reduction ($\times 0.40$) |

#### Moisture Weight Penalty
In humid environments (Rain, Swamp), dry goods absorb atmospheric moisture:
- Affected goods: `Golden Barley`, `Coarse Fiber`.
- Weight multiplier: $+50\%$ ($\text{EffectiveWeight} = \text{BaseWeight} \times 1.5$).
- Reverts to base weight upon entering dry biomes (Grasslands, Desert, Mountains).

#### Perishable Durability & Decay Math
$$\text{DecayRatePerMinute} = \text{BaseDecayRate} \times \text{BiomeSpoilageMultiplier} \times (1.0 - \text{TierDecayReduction})$$

- $\text{BaseDecayRate} = 1.0\%\text{ durability loss per real-time minute}$ (for perishable goods; $0.0\%$ for non-perishables).
- **Environmental Multipliers:**
  - Clear / Mild: $1.0\times$ ($1.0\%/\text{min}$)
  - Desert / Sun Spire Heat: $+50\%$ ($1.5\times \rightarrow 1.5\%/\text{min}$)
  - Rain / Swamp Humidity: $+50\%$ ($1.5\times \rightarrow 1.5\%/\text{min}$)
  - Freezing / Frost Spire: Reduced to $0.2\%/\text{min}$ ($0.2\times$)
- **Carriage Tier Reduction:**
  - Flat $60\%$ reduction: Multiplied by $(1.0 - 0.60) = 0.40$.

**Sample Decay Calculations (over 10 real minutes / 10 in-game hours):**
1. *Foot Packs in Grasslands (Clear):*
   $$\text{Loss} = 10 \times (1.0\% \times 1.0 \times 1.0) = 10.0\% \quad (\text{Durability: } 100\% \rightarrow 90\%)$$
2. *Foot Packs in Desert Heat:*
   $$\text{Loss} = 10 \times (1.0\% \times 1.5 \times 1.0) = 15.0\% \quad (\text{Durability: } 100\% \rightarrow 85\%)$$
3. *Carriage in Desert Heat:*
   $$\text{Loss} = 10 \times (1.0\% \times 1.5 \times 0.40) = 6.0\% \quad (\text{Durability: } 100\% \rightarrow 94\%)$$
4. *Carriage in Freezing Mountain:*
   $$\text{Loss} = 10 \times (1.0\% \times 0.2 \times 0.40) = 0.8\% \quad (\text{Durability: } 100\% \rightarrow 99.2\%)$$

#### Campsite Cooking & Buff System
- **Validation:** Checks that caravan ledger contains required quantities of un-spoiled ingredients.
- **Consumption:** Removes ingredient quantities from ledger upon preparation.
- **Expedition Buff:** Grants active party a day-long buff (Duration: exactly $24\text{ in-game hours} = 24\text{ real-time minutes}$).
- **Buff Types:**
  - *Cold Resistance:* Immunity to Frost Spire stamina penalties.
  - *CTB Speed Bonus:* $+10\%$ turn generation (reduces BaseTurnDelay by $10\%$).
  - *Bonus Insight:* $+25\%$ insight points gained from mediation and quests.
- Restorative meals automatically clear party Injury Debuffs when combined with the 8-hour sleep cycle.

---

### 4.6 Weather System & Biome Climate Dynamics

#### 7 Biomes Matrix
```
+---------------------------------------------------------------------------------------------------+
| Biome Name      | Spire Name    | Base Spoilage | Base Stamina | CTB Delay | Environmental Hazard |
+-----------------+---------------+---------------+--------------+-----------+----------------------+
| The Grasslands  | Verdant Spire | 1.0x          | 1.0x         | 1.0x      | Mild rolling plains  |
| The Coast       | Tidal Spire   | 1.2x - 1.5x   | 1.0x         | 1.0x      | Sea fog, salt spray  |
| The Forest      | Sylvan Spire  | 1.1x          | 1.0x         | 1.0x      | Dense canopy shadows |
| The Mountains   | Frost Spire   | 0.2x          | 2.0x         | 1.25x     | Freezing cold snaps  |
| The Swamp       | Mire Spire    | 1.5x          | 1.25x        | 1.0x      | Thick mist, bog mud  |
| The Desert      | Sun Spire     | 1.5x          | 1.5x         | 1.1x      | Dune heat fatigue    |
| The Jungle      | Canopy Spire  | 1.5x          | 1.25x        | 1.0x      | Torrential monsoon   |
+---------------------------------------------------------------------------------------------------+
```

#### Spire Calibration World State
Each biome possesses a boolean state `SpireCalibrated`.
- **Uncalibrated (`false`):** Full environmental hazards active, volatile weather transitions, aggressive monsters.
- **Calibrated (`true`):** Spire beacon lit. Weather hazards stabilized, monsters repelled from sector, trade routes opened, stamina drain normalized to baseline ($1.0\times$).

---

## 5. Deep-Dive Specification: R4 EditMode Unit Tests

### 5.1 Test Assembly Definition (`InkAndAsh.Tests.EditMode.asmdef`)
```json
{
    "name": "InkAndAsh.Tests.EditMode",
    "rootNamespace": "InkAndAsh.Tests.EditMode",
    "references": [
        "InkAndAsh.Core",
        "InkAndAsh.Data",
        "InkAndAsh.Combat",
        "InkAndAsh.Economy",
        "InkAndAsh.World",
        "InkAndAsh.Narrative",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll",
        "Newtonsoft.Json.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

### 5.2 Ten Mandatory Test Suites & Acceptance Coverage

#### Suite 1: `JsonItemDeserializationTests`
- **Objective:** Verify all 16 items load without nulls or schema parsing exceptions.
- **Assertions:**
  - Item count equals exactly 16.
  - No item record has null or empty `itemId` or `name`.
  - Weights are positive (`baseWeightKg > 0`).
  - Perishable items (`Canopy Bloom`, `Wild Game Meat`, `Fresh River Fish`, `Aromatic Herbs`) have `isPerishable == true` and `decayRatePerMinute > 0`.
  - Non-perishable items (`Glacier Salt`, `Scrap Iron`, etc.) have `decayRatePerMinute == 0`.

#### Suite 2: `JsonNpcAndQuestDeserializationTests`
- **Objective:** Verify sample NPCs and quests load with valid PAD ranges and 5-Act structures.
- **Assertions:**
  - Sample NPCs cover Tier 1, Tier 2, and Tier 3.
  - PAD values (`pleasure`, `arousal`, `dominance`) fall strictly within $[-100.0, +100.0]$.
  - Faction affiliations parse into valid `FactionType` enums.
  - Quest arc state contains valid 5-Act enums (`Friction`, `Ripple`, `Crisis`, `Aftermath`, `Legacy`).

#### Suite 3: `CtbFormulaCalculationTests`
- **Objective:** Verify turn delay calculations for known mathematical inputs.
- **Test Matrix:**
  1. $\text{SkillCost} = 100$, $\text{Agility} = 50$, $\text{Weather} = 1.0 \implies \text{Delay} = 2.0$.
  2. $\text{SkillCost} = 100$, $\text{Agility} = 100$, $\text{Weather} = 1.0 \implies \text{Delay} = 1.0$.
  3. $\text{SkillCost} = 100$, $\text{Agility} = 25$, $\text{Weather} = 1.0 \implies \text{Delay} = 4.0$.
  4. $\text{SkillCost} = 100$, $\text{Agility} = 50$, $\text{Weather} = 1.25$ (Blizzard) $\implies \text{Delay} = 2.5$.
  5. $\text{SkillCost} = 200$ (Heavy Skill), $\text{Agility} = 50$, $\text{Weather} = 1.0 \implies \text{Delay} = 4.0$.

#### Suite 4: `CtbTimelineOrderingTests`
- **Objective:** Verify timeline queue maintains strictly ascending order and correctly updates after turn execution.
- **Setup:** 4 combatants with known Agility values: $A_1 (80)$, $A_2 (60)$, $A_3 (40)$, $A_4 (20)$.
- **Assertions:**
  - Initial timeline queue sorts as: $A_1, A_2, A_3, A_4$.
  - When $A_1$ executes a skill of cost 100, its new delay is $100/80 = 1.25$.
  - New queue order reflects $A_1$'s new position dynamically.
  - `PreviewTimelineShift` produces the identical predicted order without mutating the active combat state.

#### Suite 5: `CtbTeamworkMechanicsTests`
- **Objective:** Verify Sync Move delta detection, Intervention mechanics, and Mediator's Anchor.
- **Assertions:**
  - $\Delta = 1.5 \implies \text{CanSyncMove} == \text{true}$.
  - $\Delta = 2.0 \implies \text{CanSyncMove} == \text{true}$.
  - $\Delta = 2.05 \implies \text{CanSyncMove} == \text{false}$.
  - Linked allies via Anchor $\implies \text{CanSyncMove} == \text{true}$ even when $\Delta = 25.0$.
  - Intervention fails if $\text{FP} < 25$.
  - Intervention with $\text{FP} \ge 25$ deducts 25 FP and reduces target ally turn time by up to 15 units.

#### Suite 6: `CtbInjuryDebuffTests`
- **Objective:** Verify application and clearance of Injury Debuffs.
- **Assertions:**
  - Normal combatant: Agility $50$, Skill Cost $100 \implies \text{Delay} = 2.0$.
  - Debuffed combatant: Effective Agility = $50 \times 0.8 = 40$; Effective Cost = $100 \times 1.15 = 115 \implies \text{Delay} = 115/40 = 2.875$.
  - Ratio equals exactly $1.4375$ ($+43.75\%$).
  - Invoking campsite recovery clears debuff flag, restoring stats to $50$ and $100$.

#### Suite 7: `CaravanCapacityTests`
- **Objective:** Verify slot and weight limits per tier.
- **Assertions:**
  - Foot Packs accepts 30 slots; rejects 31st slot.
  - Foot Packs accepts 50.0 kg; rejects addition resulting in 50.1 kg.
  - Carriage tier accepts up to 120 slots and 300.0 kg.
  - In Rain/Swamp weather, 10 kg Golden Barley evaluates to 15 kg (+50% moisture).

#### Suite 8: `CaravanDurabilityDecayTests`
- **Objective:** Verify base decay, weather multipliers, carriage 60% reduction, and spoilage events.
- **Assertions:**
  - Foot Packs in Clear: 10 mins elapsed $\implies 10.0\%$ durability lost.
  - Foot Packs in Desert (+50%): 10 mins elapsed $\implies 15.0\%$ durability lost.
  - Carriage in Clear (60% reduction): 10 mins elapsed $\implies 4.0\%$ durability lost.
  - Carriage in Desert: 10 mins elapsed $\implies 6.0\%$ durability lost.
  - Carriage in Freezing (0.2x): 10 mins elapsed $\implies 0.8\%$ durability lost.
  - When durability reaches 0.0%, `IsSpoiled` becomes `true` and `OnItemDecayed` fires with `wasSpoiled = true`.

#### Suite 9: `CampsiteCookingTests`
- **Objective:** Verify recipe validation, ingredient deduction, and 24-hour buff expiration.
- **Assertions:**
  - Recipe requires 2 Barley, 1 Salt: if inventory has 1 Barley, cooking returns `false`.
  - If inventory has required items, cooking returns `true`, deducts ingredients, and applies buff.
  - Buff remains active for 23.9 in-game hours; expires at 24.0 in-game hours.

#### Suite 10: `TimeManagerClockTests`
- **Objective:** Verify 1 real-minute to 1 in-game hour conversion and tick events.
- **Assertions:**
  - Advancing 60.0 real seconds advances clock by exactly 1 in-game hour.
  - `OnTimeHourTick` fires on every 60-second real interval.
  - Advancing 24 real minutes increments Day from 1 to 2; `OnTimeDayTick(2)` fires.

---

### 5.3 Test Execution Instructions

#### Method 1: Unity Headless Batchmode Test Runner (Primary)
Execute the EditMode test suite via the installed Unity 6 editor in headless batchmode:
```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe" `
  -batchmode `
  -nographics `
  -runTests `
  -testPlatform EditMode `
  -projectPath "d:\Unity Projects\Ink and Ash" `
  -testResults "d:\Unity Projects\Ink and Ash\EditMode-Results.xml"
```
- **Exit Code 0:** All tests passed.
- **Exit Code 2 / Non-Zero:** Test failures detected. Detailed stack traces available in `EditMode-Results.xml`.

#### Method 2: Pure C# Standalone Test Execution (`dotnet test`)
Because `InkAndAsh.Combat` and `InkAndAsh.Economy` core engines are written in pure C# with no `UnityEngine` dependencies, they can also be compiled and tested directly using the .NET SDK (`dotnet test`):
```powershell
dotnet test "d:\Unity Projects\Ink and Ash\Tests\PureCSharp\InkAndAsh.PureTests.csproj"
```

---

## 6. Implementation Checklist & Verification Matrix

- [x] GameManager singleton lifecycle and 5-state machine specified.
- [x] TimeManager 1-minute = 1-hour conversion and tick event model specified.
- [x] Generic ScriptableObject event channel and pure C# bridge specified.
- [x] Final Fantasy X CTB turn delay formula ($BaseTurnDelay = (SkillCost / Agility) \times WeatherMultiplier$) and queue sorting verified.
- [x] Sync Move ($\le 2$ delta), Intervention (25 FP & -15 shift), and Mediator's Anchor specified.
- [x] Defeat campsite reload and Injury Debuffs ($-20\%$ agility, $+15\%$ delay cost) specified.
- [x] Caravan ledger capacity tiers (Foot Packs vs Carriage) and moisture weight (+50%) specified.
- [x] Real-time perishable decay ($1\%/\text{min}$ base, Desert $1.5\times$, Freezing $0.2\times$, Carriage $60\%$ reduction) mathematically proven.
- [x] Campsite cooking recipe validation and 24-in-game-hour buff tracking specified.
- [x] 7 biomes climate matrix and Spire calibration world state defined.
- [x] EditMode test assembly definition and 10 comprehensive test suites documented.
- [x] Unity batchmode and dotnet CLI execution commands verified.
