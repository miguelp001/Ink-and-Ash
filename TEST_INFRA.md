# Test Infrastructure Specification: The Ink and the Ash

**Project:** The Ink and the Ash  
**Engine & Target:** Unity 6 URP / .NET Standard 2.1 & .NET 8.0  
**Document Type:** Opaque-Box E2E Testing Architecture  
**Document Version:** 1.0.0  
**Author:** teamwork_preview_test_writer  

---

## 1. Test Philosophy

The testing framework for *The Ink and the Ash* is strictly **opaque-box and requirement-driven**. 

1. **Decoupled from Implementation Details**: Tests are derived solely from user requirements, game design documents (`Ink and Ash.md`), technical specifications (`specs.md`), and narrative guidelines (`agents.md`). Tests exercise public interface contracts and observable behaviors without assuming private internal state or class implementation quirks.
2. **Deterministic & Self-Contained**: Every test establishes its own fixture, executes isolated logic, asserts on known mathematical or contract-bound outputs, and cleans up without lingering side effects.
3. **Dual Execution Environment**:
   - **Standalone CLI Runner (`dotnet test`)**: Headless, lightning-fast execution of core deterministic domain engines (`Combat`, `Economy`, `Data`, `World`, `Core`) via SDK-style C# test project (`standalone-tests/InkAndAsh.StandaloneTests.csproj`).
   - **Unity UTF (Unity Test Framework EditMode)**: Editor-integrated NUnit runner executing within Unity 6 for engine-integrated validation and asset-dependent assertions (`Assets/Tests/EditMode/`).
4. **Authoritative Output Derivation**: Expected test values are calculated from the explicit formulas and tables in the game design specifications:
   - CTB Turn Delay: $\text{BaseTurnDelay} = \left(\frac{\text{SkillCost}}{\text{Agility}}\right) \times \text{WeatherMultiplier}$
   - Next Turn Time: $\text{NextTurnTime} = \text{CurrentTime} + \text{BaseTurnDelay}$
   - Perishable Decay: $\text{DecayRate} = \text{BaseRate} \times \text{WeatherMultiplier} \times (1.0 - \text{CarriageReduction})$
   - Time Scaling: $1.0\text{ real minute} = 1.0\text{ in-game hour}$ ($60\times$ temporal compression)

---

## 2. Feature Inventory & Coverage Thresholds

The E2E testing track verifies 4 primary functional domains across 4 progressive testing tiers:

| Domain | Feature Scope | Key System Contracts |
|---|---|---|
| **F1: Data Catalog Integrity** | 16 Unified Goods, 3-Tier NPCs, 5-Act Quests, 7 Biomes | `DataManager`, `ItemData`, `NpcData`, `QuestData`, `BiomeData` |
| **F2: CTB Combat Timeline** | Formula, Timeline Queue, Sync Moves, Interventions, Anchor, Injuries | `CtbCombatEngine`, `Combatant`, `TimelineQueue` |
| **F3: Caravan Inventory** | Storage Tiers, Weight/Slots, Decay Ticks, Moisture Penalty, Cooking Buffs | `CaravanInventory`, `StorageTier`, `CookingSystem` |
| **F4: Time & Weather** | 1min=1hr, Hour/Day Ticks, 8hr Campsite Rest, Biome Hazards, Spire State | `TimeManager`, `WeatherSystem`, `BiomeManager` |

### Coverage Threshold Rules:
- **Tier 1 (Feature Coverage)**: At least 5 explicit test cases per feature ($\ge 20$ tests total).
- **Tier 2 (Boundary & Corner Cases)**: At least 5 boundary/corner test cases per feature ($\ge 20$ tests total).
- **Tier 3 (Cross-Feature Combinations)**: Pairwise and multi-system interaction tests covering cross-domain mechanics ($\ge 10$ tests total).
- **Tier 4 (Real-World Scenarios)**: At least 5 end-to-end multi-step realistic gameplay workflows ($\ge 5$ workflows).

---

## 3. Tier 1: Feature Coverage (Baseline Happy Path)

### Feature 1: Data Catalog Integrity
1. `F1_T1_01_All16ItemsLoadedSuccessfully`: Loads `items.json` and asserts exact count of 16 items.
2. `F1_T1_02_ItemWeightsAndPricesPositive`: Asserts all 16 items have `baseWeightKg > 0` and `basePrice > 0`.
3. `F1_T1_03_PerishableItemsHavePositiveDecay`: Verifies perishable items (`Canopy Bloom`, `Wild Game Meat`, etc.) have `isPerishable == true` and `decayRatePerMinute > 0`.
4. `F1_T1_04_MineralItemsHaveZeroDecay`: Verifies stable goods (`Ironwood Resin`, `Glacier Salt`, `Scrap Iron`, etc.) have `decayRatePerMinute == 0`.
5. `F1_T1_05_NpcTiersAndPadBounds`: Asserts sample NPCs represent Core, Anchor, Background tiers and have PAD values in $[-100, 100]$.
6. `F1_T1_06_QuestArcContains5Acts`: Verifies sample quest contains exactly 5 acts (`Friction`, `Ripple`, `Crisis`, `Aftermath`, `Legacy`).
7. `F1_T1_07_All7BiomesLoadedWithSpires`: Asserts all 7 biomes are loaded with unique Spire names and environmental multipliers.

### Feature 2: CTB Combat Timeline Engine
1. `F2_T1_01_TurnDelayFormulaStandard`: Verifies $\text{SkillCost}=100, \text{Agility}=50, \text{Weather}=1.0 \implies \text{Delay}=2.0$.
2. `F2_T1_02_TimelineQueueOrderingAscending`: 4 combatants sort strictly by ascending `NextTurnTime`.
3. `F2_T1_03_SyncMoveDetectedWithinDelta2`: Two allies with $|\text{Time}_A - \text{Time}_B| \le 2.0$ trigger Sync Move availability.
4. `F2_T1_04_InterventionAdvancesTargetTimeline`: Expending 25 FP advances target ally timeline by 15 units.
5. `F2_T1_05_MediatorAnchorGuaranteesSyncMove`: Anchor links two allies, guaranteeing Sync Move regardless of timeline delta.
6. `F2_T1_06_InjuryDebuffIncreasesDelay`: Injury debuff applies $-20\%$ turn gen and $+15\%$ delay cost ($+43.75\%$ net delay).

### Feature 3: Caravan Inventory & Perishable Economy
1. `F3_T1_01_FootPacksCapacityEnforced`: Foot Packs enforces 30 slots max and 50 kg max.
2. `F3_T1_02_HorseCarriageCapacityEnforced`: Horse-Drawn Carriage enforces 120 slots max and 300 kg max.
3. `F3_T1_03_BaseDurabilityDecayTicking`: 10 real minutes elapsed in clear weather reduces perishable durability by $10\%$.
4. `F3_T1_04_CarriageProvides60PercentDecayReduction`: Carriage reduces decay by $60\%$ (decay multiplier $0.40$).
5. `F3_T1_05_CampsiteCookingConsumesIngredientsAndGrantsBuff`: Preparing meal consumes valid ingredients and applies 24-hr buff.

### Feature 4: Time & Weather Systems
1. `F4_T1_01_OneMinuteToInGameHourRatio`: Advancing 60 real seconds increments clock by exactly 1 in-game hour.
2. `F4_T1_02_HourTickEventFiresSequentially`: Advancing 180 real seconds emits 3 sequential hour ticks.
3. `F4_T1_03_DayTickEventFiresOn24HourRollover`: Crossing Hour 23 to Hour 0 rolls day from 1 to 2 and fires day tick.
4. `F4_T1_04_CampsiteRestAdvances8Hours`: Rest action skips 8 in-game hours, processing sequential hour ticks and decay.
5. `F4_T1_05_BiomeWeatherTransitions`: Biome selection updates environmental spoilage, stamina, and delay multipliers.

---

## 4. Tier 2: Boundary & Corner Cases

### Feature 1: Data Catalog Integrity
1. `F1_T2_01_PADAtExtremeNegativeBound`: Validates NPC with PAD $(-100, -100, -100)$ is accepted without error.
2. `F1_T2_02_PADAtExtremePositiveBound`: Validates NPC with PAD $(+100, +100, +100)$ is accepted without error.
3. `F1_T2_03_PADOutOfRangeClamped`: Asserts PAD values exceeding $[-100, +100]$ are clamped or flagged invalid.
4. `F1_T2_04_ZeroWeightItemRejected`: Asserts item with `baseWeightKg <= 0` fails schema validation.
5. `F1_T2_05_NegativeDecayRateRejected`: Asserts item with negative decay rate is rejected.
6. `F1_T2_06_MissingActInQuestFails`: Asserts quest with fewer than 5 acts fails validation.

### Feature 2: CTB Combat Timeline Engine
1. `F2_T2_01_ZeroAgilityHandledSafely`: Agility of 0 does not throw DivideByZero; clamps to minimum 1.
2. `F2_T2_02_FreeSkillActionCostZero`: Skill cost of 0 produces 0 delay, allowing immediate consecutive action.
3. `F2_T2_03_SyncMoveAtExactDeltaThreshold`: $|\text{Time}_A - \text{Time}_B| = 2.000$ returns `true`; $2.001$ returns `false`.
4. `F2_T2_04_InterventionWithInsufficientFP`: Intervening with $24.99$ FP fails and does not deduct points.
5. `F2_T2_05_InterventionTimelineClampToCurrentTime`: Timeline advance never places an actor before `CurrentTime`.
6. `F2_T2_06_TimelineTieBreakingDeterministic`: Equal `NextTurnTime` prioritizes allies over enemies, then higher Agility.

### Feature 3: Caravan Inventory & Perishable Economy
1. `F3_T2_01_InventorySlotExactLimit`: Adding 30th item to Foot Packs succeeds; adding 31st item fails.
2. `F3_T2_02_InventoryWeightExactLimit`: Total weight of exactly $50.00$ kg succeeds; $50.01$ kg is rejected.
3. `F3_T2_03_DurabilityClampedAtZero`: Perishable decay down past 0% clamps at 0.0% and flags `IsSpoiled = true`.
4. `F3_T2_04_DecayInFreezingEnvironment`: Mountain freezing reduces base decay rate to $0.2\%/\text{min}$.
5. `F3_T2_05_SpoiledIngredientsRejectedInCooking`: Recipes cannot consume spoiled ingredients ($0\%$ durability).
6. `F3_T2_06_MoistureWeightRemovalOnDryBiome`: Absorbent goods return to base weight when transitioning from Rain to Clear.

### Feature 4: Time & Weather Systems
1. `F4_T2_01_TickTimeWithZeroDelta`: Delta of 0 seconds causes zero progression and zero emitted events.
2. `F4_T2_02_TimeAdvanceAt59Seconds`: Advancing 59.99 real seconds updates minutes without triggering hour tick.
3. `F4_T2_03_PausedClockFreezesAllTime`: When paused, real-time delta causes no in-game time advancement.
4. `F4_T2_04_MidnightRestRollover`: 8-hour rest starting at 20:00 rolls over to 04:00 Day 2, firing day tick and 8 hour ticks.
5. `F4_T2_05_ExtremeWeatherMultipliersApplied`: Blizzard weather applies $0.2\times$ spoilage and $2.0\times$ stamina multipliers.

---

## 5. Tier 3: Cross-Feature Combinations (Pairwise Interactions)

1. `F_T3_01_CaravanDecayUnderDesertHeat`: Combines Caravan Inventory decay, Weather System Desert Heat ($+50\%$), and Foot Packs ($0\%$ protection) $\implies 1.5\%/\text{min}$.
2. `F_T3_02_CarriageInFreezingMountain`: Combines Caravan Inventory Carriage ($60\%$ reduction) and Mountain Freezing ($0.2\times$) $\implies 0.08\%/\text{min}$ ($0.8\%$ lost per 10 mins).
3. `F_T3_03_MoistureWeightEncumbranceInRain`: Rain weather increases Barley weight by $+50\%$, tipping a near-capacity carriage over its maximum weight limit.
4. `F_T3_04_WeatherMultiplierAffectsCombatTurnDelay`: Blizzard weather ($1.25\times$ delay multiplier) scales CTB `BaseTurnDelay` upward across all combatants.
5. `F_T3_05_CombatInjuryDebuffClearedByCampsiteRestAndMeal`: Combatant suffers Injury Debuff in combat; taking 8-hour rest AND eating cooked meal clears debuff; rest alone leaves debuff active.
6. `F_T3_06_TimeAdvancementTicksCaravanDecay`: Advancing TimeManager clock automatically drives Caravan Inventory durability degradation.
7. `F_T3_07_CookingExpeditionBuffExpiresAt24InGameHours`: Cooking meal grants buff; advancing clock by 23.9 in-game hours retains buff; advancing past 24.0 hours removes buff.
8. `F_T3_08_SpireCalibrationCalmsWeatherAndCombatDelay`: Calibrating Frost Spire resets harsh blizzard weather to Clear and returns combat delay multiplier to $1.0\times$.
9. `F_T3_09_MediatorAnchorWithWeatherDelayDisparity`: Even when severe weather widens timeline gap to $\Delta > 20$, Mediator's Anchor preserves Sync Move capability.
10. `F_T3_10_PerishableItemSpoilsDuringRestTriggeringEvent`: 8-hour campsite rest advances time by 480 in-game minutes, causing low-durability meat to spoil and dispatch `OnItemDecayed`.

---

## 6. Tier 4: Real-World Application Scenarios (Realistic Workflows)

### Workflow 1: Overworld Grain Pilgrimage Across Three Biomes
- **Path**: Grasslands (purchase Golden Barley) $\rightarrow$ Swamp (Rain moisture penalty) $\rightarrow$ Desert (Sun Spire Heat).
- **Actions**:
  1. Caravan buys 20 kg Golden Barley in Grasslands (dry, 20 kg).
  2. Travel to Swamp under Heavy Rain: moisture adds $+50\%$ weight ($30$ kg).
  3. Caravan moves to Desert: heat dries barley back to 20 kg, but desert heat increases decay to $1.5\%/\text{min}$.
  4. Carriage cooling lining reduces net decay to $0.6\%/\text{min}$.
  5. Arrive at desert outpost with un-spoiled barley ready for trade.

### Workflow 2: Tactical CTB Boss Encounter with Synergistic Teamwork
- **Participants**: Vicar (Agility 50), Weaver (Agility 40), Hostile Ash-Beast (Agility 60).
- **Actions**:
  1. Ash-Beast acts first at $t=1.67$, targeting Weaver.
  2. Vicar expends 25 FP to perform Intervention, advancing Weaver's timeline from $t=2.5$ to $t=2.0$.
  3. Vicar establishes Mediator's Anchor linking Vicar and Weaver.
  4. Vicar and Weaver execute simultaneous Sync Move without extra turn delay.
  5. Beast defeated without party casualty.

### Workflow 3: Party Defeat, Camp Reload, and Restorative Recovery
- **Actions**:
  1. Party wiped out in combat, triggering defeat condition.
  2. Game state reloads at last campsite; party members receive Injury Debuff ($-20\%$ turn gen, $+15\%$ delay cost).
  3. Verify combatant effective agility drops to $40$ and delay increases by $+43.75\%$.
  4. Camp cooks restorative meal using Golden Barley and Glacier Salt, granting 24-hr stamina buff.
  5. Party sleeps for 8 hours (TimeManager advances 8 hours).
  6. Injury Debuff is cleared; party restored to full combat efficiency.

### Workflow 4: The Mill and the Measure (5-Act Narrative & Resource Delivery)
- **Actions**:
  1. Initiate Quest: Act I Friction (Stalled Millstones requires 10 kg Golden Barley).
  2. Player delivers 10 kg barley: Act I completes, advancing state machine to Act II Ripple.
  3. Mediate dispute between elders and Kaelen: Act II completes, advancing to Act III Crisis.
  4. De-escalate midnight inspection by Imperial tax collector: Act III completes, advancing to Act IV Aftermath.
  5. Establish communal granary: Act IV completes, advancing to Act V Legacy.
  6. Claim Act V reward: unlocked permanent boon `Boon_Free_Village_Smithy_Repairs`.

### Workflow 5: Frost Spire Ascent and Regional Weather Stabilization
- **Actions**:
  1. Party enters The Mountains under freezing blizzard ($0.2\times$ spoilage, $2.0\times$ stamina drain, $1.25\times$ combat delay).
  2. CTB combat reflects slowed speed ($1.25\times$ base delay).
  3. Party reaches summit and completes Spire calibration sequence (Ascend Peak, Clear Hazard, Light Beacon).
  4. `SpireCalibrated` transitions to `true`, dispatching `OnSpireCalibrated`.
  5. Weather stabilizes to Clear; stamina drain normalizes to $1.0\times$; combat delay multiplier resets to $1.0\times$.

---

## 7. Test Architecture & Runner Invocation

### Directory Layout
```
d:\Unity Projects\Ink and Ash\
├── Assets\
│   └── Tests\
│       └── EditMode\
│           ├── InkAndAsh.Tests.EditMode.asmdef
│           └── E2ETests\
│               ├── DataCatalogE2ETests.cs
│               ├── CtbCombatTimelineE2ETests.cs
│               ├── CaravanInventoryE2ETests.cs
│               ├── TimeAndWeatherE2ETests.cs
│               └── RealWorldWorkflowsE2ETests.cs
└── standalone-tests\
    ├── InkAndAsh.StandaloneTests.csproj
    └── E2ETests\
        ├── DataCatalogE2ETests.cs
        ├── CtbCombatTimelineE2ETests.cs
        ├── CaravanInventoryE2ETests.cs
        ├── TimeAndWeatherE2ETests.cs
        └── RealWorldWorkflowsE2ETests.cs
```

### CLI Runner Commands

#### 1. Pure C# Standalone Runner (`dotnet test`)
```powershell
dotnet test "d:\Unity Projects\Ink and Ash\standalone-tests\InkAndAsh.StandaloneTests.csproj" --verbosity normal
```

#### 2. Unity Headless Batchmode Runner
```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe" `
  -batchmode `
  -nographics `
  -runTests `
  -testPlatform EditMode `
  -projectPath "d:\Unity Projects\Ink and Ash" `
  -testResults "d:\Unity Projects\Ink and Ash\EditMode-Results.xml"
```
