# Handoff Report: R3 Core Runtime Systems & R4 EditMode Unit Tests

**Agent ID:** teamwork_preview_spec_miner (Spec Miner 3)  
**Parent ID:** fc826616-6237-4548-b6ac-f0d5352bcd07 (Project Orchestrator)  
**Working Directory:** `d:\Unity Projects\Ink and Ash\.agents\teamwork\spec_miner_survey_3`  
**Date:** 2026-10-24  
**Type:** Hard Handoff (Task Complete)  

---

## 1. Observation

1. **Authoritative Specification Inputs:**
   - In `ORIGINAL_REQUEST.md` (lines 42–70):
     - Line 46: `GameManager (Core): Persistent singleton (DontDestroyOnLoad) holding references to all runtime data stores. Survives scene transitions.`
     - Line 48: `TimeManager (Core): 1 real-time minute = 1 in-game hour. Tracks current in-game hour, day, and weather state. Exposes events for hour-tick and day-tick.`
     - Line 50: `Event Channels (Core): ScriptableObject-based event channel pattern. Create base generic GameEvent<T> and GameEventListener<T> classes. Create concrete event types for: OnCombatStarted, OnCombatEnded, OnTimeHourTick, OnTimeDayTick, OnItemDecayed, OnQuestActAdvanced, OnSpireCalibrated.`
     - Line 52: `CTB Combat Engine (Combat): Implement the turn-order formula: BaseTurnDelay = (SkillCost / Agility) * WeatherMultiplier, NextTurnTime = CurrentTime + BaseTurnDelay. Maintain a sorted timeline queue of combatants. Support: adding/removing combatants, previewing timeline shifts before committing an action, Sync Move detection (two allies with timeline delta <= 2), Intervention spending (25 Focus Points to intercept or advance ally by up to 15 timeline units), Mediator's Anchor (links two allies for guaranteed Sync Moves). Track Injury Debuffs (-20% turn generation, +15% skill delay costs) and clearing conditions. The combat engine must be testable without MonoBehaviour dependencies (pure C# logic with a thin MonoBehaviour wrapper).`
     - Line 54: `Caravan Inventory (Economy): List-based ledger (not spatial grid). Two capacity tiers: Foot Packs (30 slots, 50 kg max, no decay protection) and Horse-Drawn Carriage (120 slots, 300 kg max, 60% decay reduction). Real-time durability decay: 1% base per real-time minute, modified by biome weather multipliers (Desert +50%, Rain/Swamp +50%, Freezing reduces to 0.2%/min). Expose methods for adding items, removing items, querying current weight/slot usage, and ticking decay.`
     - Line 56: `Weather System (World): Per-biome weather states (Clear, Rain, Heat, Freezing) with configurable spoilage and stamina multipliers matching the GDD values.`
     - Lines 58–70: Details R4 unit tests covering JSON deserialization, CTB formula, CTB timeline ordering, Sync Move detection, Injury Debuffs, Caravan limits & decay, and TimeManager.
   - In `Ink and Ash.md` (lines 86–94, 136–167, 244, 412–436):
     - Line 86: `Time Ratio: Time advances at a ratio of 1 minute real-time to 1 hour in-game. A full 24-hour day completes in 24 minutes of real-time play...`
     - Line 88: `Desert / Sun Spire Heat: +50% spoilage rate multiplier (1.5% durability loss/min).`
     - Line 89: `Rain / Swamp Humidity: +50% spoilage and moisture weight multiplier (1.5% durability loss/min).`
     - Line 90: `Freezing Temperatures: Freezing reduces organic decay to 0.2% per minute but accelerates party stamina drain by 2.0x per travel unit.`
     - Line 92: `Foot Packs (Base Tier): Capacity limit of 30 slots (50 kg maximum weight). Provides zero environmental protection...`
     - Line 93: `Horse-Drawn Carriage (Upgraded Tier): Capacity limit of 120 slots (300 kg maximum weight)... reduces ambient item decay rates by a flat 60% across all biomes.`
     - Line 138: `Base Turn Delay = (Skill Cost / Agility Attribute) * Weather Multiplier`
     - Line 140: `Next Turn Time = CurrentTime + Base Turn Delay`
     - Line 154: `Sync Moves: Triggered when two active party members align adjacently on the CTB timeline (defined as a Turn Delta <= 2 timeline units)...`
     - Line 155: `Interventions: ...spending 25 Focus Points to intercept the incoming strike, absorb damage or shift the targeted ally forward on the timeline by up to 15 timeline units.`
     - Line 156: `The Mediator's Anchor: ...links two chosen allies on the timeline, guaranteeing access to Sync Moves regardless of incoming speed debuffs or turn delays.`
     - Line 162: `Upon reloading, all active party members suffer temporary Injury Debuffs that reduce physical efficiency, slow timeline turn generation by 20% and increase skill delay costs by 15%.`
     - Line 165: `Campsite recovery loop: rest full in-game sleep cycle (8 in-game hours) + prepare and consume restorative cooked meals.`
     - Line 244: `Buff Duration Window of 24 in-game hours.`
   - In `specs.md` (lines 5–10, 31–36):
     - Unity 6 URP, 60 FPS zero-allocation performance target, list-based caravan ledger, CTB combat model.
   - In `agents.md` (lines 13–18, 29–35):
     - Point-and-click / tap-to-move input magnetism, toggle states over holding, CTB combat with visible timeline.
2. **Environment & Tooling Observations:**
   - PowerShell command `dotnet --version` output: `11.0.100-rc.1.26425.128` (successful).
   - Unity executable found at `C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe`.
   - Unity CLI tool found at `C:\Users\migue\AppData\Local\Unity\bin\unity.exe` (`1.0.0-beta.12`).
   - Project directory: `d:\Unity Projects\Ink and Ash`. Currently contains root documents and metadata; `Assets/` hierarchy will be scaffolded per R1.

---

## 2. Logic Chain

1. **GameManager & State Machine Architecture:**
   - Based on lines 46 and 495 of `Ink and Ash.md` and dispatch instructions, the GameManager must survive scene transitions between the 3D Overworld Paper Map and the 7 biome scenes.
   - Therefore, `GameManager` must be implemented as a MonoBehaviour singleton executing `DontDestroyOnLoad(gameObject)` with duplicate instance destruction.
   - The five game states required are `Exploration`, `Campsite`, `Combat`, `Dialogue`, and `Paused`. Transitions must be explicitly constrained (e.g. Combat cannot transition directly to Dialogue; party wipeout in Combat triggers a transition to Campsite with Injury Debuffs).

2. **TimeManager & Clock Mechanics:**
   - The ratio `1 real minute = 1 in-game hour` means real-time seconds translate directly to in-game minutes ($60\times$ speed factor).
   - In-game day consists of 24 in-game hours (24 real minutes = 1440 real seconds).
   - Time tracking requires accumulator arithmetic: whenever accumulated minutes reach 60, increment hour and fire `OnTimeHourTick`. Whenever hour reaches 24, increment day, reset hour to 0, and fire `OnTimeDayTick`.
   - Resting at campsite advances time by 8 in-game hours. The system must process 8 hourly ticks, advance days if passing midnight, apply durability decay to perishables, and update weather.

3. **Pure C# CTB Combat Engine & Timeline Math:**
   - Requirement line 52 mandates that the combat engine be testable without MonoBehaviour dependencies.
   - Therefore, the combat system must consist of pure C# classes (`CtbCombatEngine`, `Combatant`, `TimelineQueue`) with a separate MonoBehaviour wrapper (`CombatController`).
   - Turn delay formula is $\text{BaseTurnDelay} = (\text{SkillCost} / \text{Agility}) \times \text{WeatherMultiplier}$. This is mathematically identical to normalized ticks $\text{Delay} = \text{ActionCost} \times (100 / \text{Agility}) \times \text{WeatherMultiplier}$ when $\text{SkillCost} = \text{ActionCost} \times 100$.
   - Timeline ordering is a sorted priority queue on `NextTurnTime`. Tie breaking: allies precede enemies, then higher agility, then stable ID.
   - Sync Moves require $\Delta \le 2.0$ timeline units between two active allies OR active Mediator's Anchor link between them.
   - Intervention requires $25\text{ FP}$, accelerating target ally by up to 15 timeline units ($\max(\text{CurrentTime}, \text{targetTime} - 15)$) and re-sorting the queue.
   - Injury Debuffs apply $-20\%$ turn generation ($\text{EffectiveAgility} = \text{Agility} \times 0.8$) and $+15\%$ skill delay cost ($\text{EffectiveSkillCost} = \text{SkillCost} \times 1.15$). The combined delay increase is $(1.15 / 0.8) = 1.4375\times$ ($+43.75\%$). Clearance requires 8 hours campsite rest and a cooked meal.

4. **Caravan Inventory & Decay Mathematics:**
   - A list-based ledger is strictly non-spatial. Capacity tiers: Foot Packs (30 slots, 50 kg max, 0% protection) vs Carriage (120 slots, 300 kg max, 60% decay reduction).
   - Environmental moisture adds $+50\%$ weight to Golden Barley and Coarse Fiber in Rain/Swamp biomes.
   - Base decay is $1.0\%$ per real-time minute ($= 1\text{ in-game hour}$). Multipliers: Desert $+50\%$ ($1.5\%/\text{min}$), Rain/Swamp $+50\%$ ($1.5\%/\text{min}$), Freezing ($0.2\%/\text{min}$). Carriage applies $60\%$ reduction: $(1.0 - 0.60) = 0.40$ factor.
   - Non-perishable items have a decay rate of 0. Spoilage occurs when durability reaches $0.0\%$, emitting `OnItemDecayed(WasSpoiled = true)`.
   - Campsite cooking validates available ingredients, deducts them, and grants day-long expedition buffs lasting exactly 24 in-game hours ($= 24\text{ real-time minutes}$).

5. **EditMode Unit Testing Architecture (R4):**
   - The test assembly `InkAndAsh.Tests.EditMode.asmdef` references `InkAndAsh.Core`, `InkAndAsh.Data`, `InkAndAsh.Combat`, `InkAndAsh.Economy`, `InkAndAsh.World`, `InkAndAsh.Narrative`, `UnityEngine.TestRunner`, and `UnityEditor.TestRunner`.
   - Because the core combat and inventory systems are pure C#, tests can run via Unity Test Runner CLI (`-runTests -testPlatform EditMode`) or via `dotnet test`.
   - 10 comprehensive test suites have been defined covering all acceptance criteria, edge cases, and mathematical models.

---

## 3. Caveats

1. **Unity Asset Hierarchy:** The physical `Assets/` directory has not yet been populated by the project scaffolding agent. All assembly definitions, classes, and test files must be created by the subsequent implementation agents.
2. **Love/Hate Integration:** Social simulation uses Pixel Crushers Love/Hate Engine data schemas (PAD values $-100$ to $+100$). The pure C# core handles these as standard structs/floats; the third-party asset package will be imported during asset phase.
3. **Audio / Visual Assets:** Google Lyria audio generation and Synty 3D models are intentionally decoupled from this pure C# architecture. Systems expose clean event channels (`OnCombatStarted`, `OnCombatEnded`, `OnTimeHourTick`, etc.) for audio triggers.

---

## 4. Conclusion

The complete architectural and algorithmic specification for **R3 (Core Runtime Systems)** and **R4 (EditMode Unit Tests)** has been mined and documented in `spec_report.md`.
- All 37 distinct features have been categorized and mapped with explicit inputs, outputs, error behaviors, and authoritative sources.
- 40 edge cases and boundary conditions have been analyzed with exact mathematical formulations.
- Clear separation between Pure C# logic (`InkAndAsh.Combat`, `InkAndAsh.Economy`) and Unity Engine wrappers ensures $100\%$ automated testability in EditMode without scene dependency.
- All acceptance criteria from `ORIGINAL_REQUEST.md` have been fully decomposed into testable specifications.

---

## 5. Verification Method

1. **Inspect Specification Artifacts:**
   - Open and review `d:\Unity Projects\Ink and Ash\.agents\teamwork\spec_miner_survey_3\spec_report.md`.
   - Verify the presence and completeness of the **Features Discovered** and **Edge Cases** tables.
   - Verify that all formulas ($BaseTurnDelay$, decay math, injury debuff ratios, capacity tiers) match GDD specifications.
2. **Validate Test Suite Coverage:**
   - Confirm that the 10 defined unit test suites in `spec_report.md` § 5.2 address all requirements in `ORIGINAL_REQUEST.md` § R4.
3. **Execution Command Verification:**
   - Once implementation is scaffolded, run EditMode tests via Unity CLI:
     ```powershell
     & "C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe" -batchmode -nographics -runTests -testPlatform EditMode -projectPath "d:\Unity Projects\Ink and Ash" -testResults "d:\Unity Projects\Ink and Ash\EditMode-Results.xml"
     ```
   - Invalidation condition: Any discrepancy between `spec_report.md` formulas and implemented test assertions constitutes a defect.
