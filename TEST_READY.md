# Test Ready Certification: The Ink and the Ash

**Project:** The Ink and the Ash  
**Track:** Opaque-Box E2E Testing Suite  
**Status:** READY (All Tests Passing, 100% Green)  
**Execution Environment:** .NET 8.0 SDK / Unity 6 URP EditMode Test Framework  
**Date:** 2026-10-24  
**Author:** teamwork_preview_test_writer (E2E Testing Architect & Writer)  

---

## 1. Test Runner Command

### Primary CLI Command (SDK Standalone Runner)
Execute the comprehensive E2E test suite with zero scene dependencies and instantaneous execution:

```powershell
dotnet test "d:\Unity Projects\Ink and Ash\standalone-tests\InkAndAsh.StandaloneTests.csproj" --verbosity normal
```

### Headless Unity 6 Batchmode Runner
```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe" `
  -batchmode `
  -nographics `
  -runTests `
  -testPlatform EditMode `
  -projectPath "d:\Unity Projects\Ink and Ash" `
  -testResults "d:\Unity Projects\Ink and Ash\EditMode-Results.xml"
```

---

## 2. Test Execution Summary

```
Test Run Summary:
  Total Test Cases:    117
  Passed:              117
  Failed:              0
  Skipped:             0
  Execution Duration:  ~160 ms
  Warnings / Errors:   0
```

---

## 3. Test Tier Breakdown

| Test Tier | Description | Minimum Threshold | Tests Implemented | Status |
|---|---|---|---|---|
| **Tier 1: Feature Coverage** | Primary happy-path coverage across 4 assigned features | $\ge 5$ per feature ($\ge 20$ total) | **32 tests** | PASS |
| **Tier 2: Boundary & Corner Cases** | Extreme values, clamping, resource limits, invalid inputs | $\ge 5$ per feature ($\ge 20$ total) | **29 tests** | PASS |
| **Tier 3: Cross-Feature Combinations** | Multi-system and pairwise interactions across domains | $\ge 10$ total | **15 tests** | PASS |
| **Tier 4: Real-World Scenarios** | Multi-step end-to-end realistic gameplay workflows | $\ge 5$ total | **10 tests** (5 workflows + 5 audits) | PASS |
| **Unit Verification (Data)** | Embedded data catalog validation tests | N/A | **31 tests** | PASS |
| **Total** | | | **117 tests** | **100% PASS** |

---

## 4. Feature Coverage Checklist

### Feature 1: Data Catalog Integrity (`DataCatalogE2ETests.cs`)
- [x] All 16 unified trade goods loaded from `items.json` without nulls or parsing errors.
- [x] All 16 item weights strictly positive (`baseWeightKg > 0`) and prices positive.
- [x] Perishable items (`Canopy Bloom`, `Wild Game Meat`, `River Fish`, `Golden Barley`, etc.) marked `isPerishable: true` with positive decay rates.
- [x] Non-perishable items (`Ironwood Resin`, `Glacier Salt`, `Scrap Iron`, etc.) have `decayRatePerMinute: 0.0`.
- [x] Sample NPCs cover Tier 1 (Core), Tier 2 (Anchor), and Tier 3 (Background) with PAD bounds strictly in $[-100.0, 100.0]$.
- [x] 6 canonical factions validated (`Traditionalists`, `Pragmatists`, `YoungRadicals`, `TheBurdened`, `Outsiders`, `ImperialBureaucracy`).
- [x] Sample quest contains complete 5-Act progression: Friction, Ripple, Crisis, Aftermath, Legacy.
- [x] 7 regional biomes loaded with associated Spires (Verdant, Tidal, Sylvan, Frost, Mire, Sun, Canopy) and environmental multipliers.
- [x] Dynamic trade margin pricing tested across biomes.
- [x] Boundary testing: extreme negative/positive PAD bounds, out-of-range clamping, zero/negative weight rejection, negative decay rejection.
- [x] Error handling: missing files and malformed JSON caught gracefully without crashing.

### Feature 2: CTB Combat Timeline Engine (`CtbCombatTimelineE2ETests.cs`)
- [x] Turn delay formula calculation: $\text{BaseTurnDelay} = \left(\frac{\text{SkillCost}}{\text{Agility}}\right) \times \text{WeatherMultiplier}$.
- [x] Turn progression: $\text{NextTurnTime} = \text{CurrentTime} + \text{BaseTurnDelay}$.
- [x] Ascending priority timeline queue sorting and deterministic tie-breaking (allies before enemies, higher agility).
- [x] Dynamic timeline previewing before committing actions without state mutation.
- [x] Sync Move detection: active allies with turn delta $\le 2.0$ units.
- [x] Intervention reactions: expending 25 FP advances target ally timeline by 15 units (clamped to CurrentTime).
- [x] The Mediator's Anchor: guarantees Sync Move between linked allies regardless of timeline distance.
- [x] Injury Debuffs: $-20\%$ turn generation (Agility $\times 0.8$), $+15\%$ delay cost (SkillCost $\times 1.15$), net $+43.75\%$ delay.
- [x] Boundary testing: Agility $= 0$ safe clamping, free action cost $= 0$, delta $= 2.000$ vs $2.001$, dead ally sync move rejection, insufficient FP rejection.

### Feature 3: Caravan Inventory & Perishable Economy (`CaravanInventoryE2ETests.cs`)
- [x] Capacity tiers enforcement: Foot Packs (30 slots, 50.0 kg max) vs Horse Carriage (120 slots, 300.0 kg max).
- [x] Durability decay ticking: $1.0\%$ base loss per real minute ($60\times$ in-game ratio).
- [x] Horse Carriage $60\%$ decay reduction (decay multiplier $0.40$).
- [x] Weather decay multipliers: Desert Heat $+50\%$ ($1.5\%/\text{min}$), Swamp/Rain $+50\%$ ($1.5\%/\text{min}$), Freezing ($0.2\%/\text{min}$).
- [x] Moisture weight penalty: absorbent goods (Golden Barley, Coarse Fiber) gain $+50\%$ weight in Rain/Swamp, reverting in dry biomes.
- [x] Spoilage handling: durability reaching $0.0\%$ clamps at zero, marks `IsSpoiled = true`, and emits `OnItemDecayed`.
- [x] Campsite cooking: consumes required un-spoiled ingredients, grants $24$-hour expedition buff, rejects spoiled ingredients.
- [x] Boundary testing: exact 30th slot accepted and 31st rejected; exact 50.0 kg accepted and 50.01 kg rejected; non-perishables suffer zero decay.

### Feature 4: Time & Weather Systems (`TimeAndWeatherE2ETests.cs`)
- [x] Clock progression ratio: 1 real-time minute = 1 in-game hour (1 real second = 1 in-game minute).
- [x] Sequential hour tick event emission on 60-second real-time intervals.
- [x] Day rollover and day tick event emission crossing Hour 23 to Hour 0.
- [x] Campsite rest: 8-hour time skip advancing in-game clock, firing sequential hour ticks, and processing decay.
- [x] Weather transitions across 7 biomes setting environmental multipliers (spoilage, stamina, combat delay).
- [x] Spire calibration: beacon lighting calms regional weather hazards, repels monsters, and normalizes multipliers to $1.0\times$.
- [x] Boundary testing: zero delta, 59.9-second tick (no hour rollover), paused clock freezing time, midnight rest rollover.

### Feature 5: Real-World Scenarios (`RealWorldWorkflowsE2ETests.cs`)
- [x] **Workflow 1 (Overworld Grain Pilgrimage Across Three Biomes)**: Grasslands purchase, Swamp Rain moisture penalty (+50% weight), Desert Heat decay with Carriage cooling, resale at desert outpost.
- [x] **Workflow 2 (Tactical CTB Boss Encounter with Synergistic Teamwork)**: Ash Behemoth encounter, Vicar 25 FP Intervention, Mediator's Anchor, synchronized tandem strike, victory without party casualties.
- [x] **Workflow 3 (Party Defeat, Camp Reload, and Restorative Recovery)**: Wipeout defeat, camp reload with Injury Debuffs, stew preparation, 8-hour rest, debuff clearance and stat normalization.
- [x] **Workflow 4 (The Mill and the Measure Complete 5-Act Quest Lifecycle)**: Friction $\rightarrow$ Ripple $\rightarrow$ Crisis $\rightarrow$ Aftermath $\rightarrow$ Legacy, unlocking permanent smithy repairs boon.
- [x] **Workflow 5 (Frost Spire Ascent & Regional Weather Stabilization)**: Mountains entry under blizzard, slowed combat turns, 3-step Spire calibration, beacon lit, weather calms to Clear, combat speed restored.

---

## 5. Artifact Index

- `TEST_INFRA.md`: Full test infrastructure specification, philosophy, thresholds, and runner invocation.
- `TEST_READY.md`: This certification document.
- `standalone-tests/E2ETests/DataCatalogE2ETests.cs`: 23 tests covering Data Catalog Integrity.
- `standalone-tests/E2ETests/CtbCombatTimelineE2ETests.cs`: 22 tests covering CTB Combat Timeline.
- `standalone-tests/E2ETests/CaravanInventoryE2ETests.cs`: 19 tests covering Caravan Inventory.
- `standalone-tests/E2ETests/TimeAndWeatherE2ETests.cs`: 17 tests covering Time & Weather.
- `standalone-tests/E2ETests/RealWorldWorkflowsE2ETests.cs`: 5 complete multi-system realistic workflows.
- `standalone-tests/E2ETests/Contracts/`: Pure C# contracts matching `PROJECT.md` interface specifications.
- `standalone-tests/InkAndAsh.StandaloneTests.csproj`: SDK-style test project targeting net8.0.
