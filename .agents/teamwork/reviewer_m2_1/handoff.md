# Milestone 2 Review & Adversarial Challenge Report

**Agent**: teamwork_preview_reviewer (Reviewer 1, Milestone 2)  
**Parent**: fc826616-6237-4548-b6ac-f0d5352bcd07 (Project Orchestrator)  
**Working Directory**: `d:\Unity Projects\Ink and Ash\.agents\teamwork\reviewer_m2_1`  
**Target Milestone**: Milestone 2 (Pure C# CTB Combat Engine and Core State Systems)  
**Date**: 2026-10-24  

---

## 1. Observation

Direct file inspection and command execution results:

1. **Test Execution**:
   Command: `dotnet test "standalone-tests/InkAndAsh.StandaloneTests.csproj" --verbosity normal`
   Result:
   ```
   Test Run Successful.
   Total tests: 156
        Passed: 156
   Total time: 0.7487 Seconds
   Build succeeded.
       0 Warning(s)
       0 Error(s)
   ```
   156 tests passing out of 156 total across both production test fixtures and E2E suites.

2. **CTB Turn Delay Math** (`Assets/Scripts/Combat/CtbCombatEngine.cs:30-39`):
   ```csharp
   public float CalculateTurnDelay(Combatant actor, int skillCost, float weatherMultiplier = 1.0f)
   {
       if (actor == null) return 0f;

       float effectiveSkillCost = actor.HasInjuryDebuff ? skillCost * 1.15f : skillCost;
       float effectiveAgility = Math.Max(1.0f, actor.EffectiveAgility);
       float safeWeather = Math.Max(0.1f, weatherMultiplier);

       return (effectiveSkillCost / effectiveAgility) * safeWeather;
   }
   ```
   Agility penalty in `Assets/Scripts/Combat/Combatant.cs:19`:
   ```csharp
   public float EffectiveAgility => HasInjuryDebuff ? Math.Max(1.0f, BaseAgility * 0.8f) : Math.Max(1.0f, BaseAgility);
   ```
   NextTurnTime calculation in `Assets/Scripts/Combat/CtbCombatEngine.cs:45-47`:
   ```csharp
   CurrentTime = Math.Max(CurrentTime, actor.NextTurnTime);
   float delay = CalculateTurnDelay(actor, skillCost, weatherMultiplier);
   actor.NextTurnTime = CurrentTime + delay;
   ```

3. **Dynamic Timeline Priority Queue & Ordering** (`Assets/Scripts/Combat/TimelineQueue.cs:53-74`):
   Deterministic multi-tier sort:
   - Primary: `NextTurnTime` ascending.
   - Tie-Breaker 1: Allies before enemies (`a.IsAlly != b.IsAlly ? (a.IsAlly ? -1 : 1) : 0`).
   - Tie-Breaker 2: Higher effective agility first (`b.EffectiveAgility.CompareTo(a.EffectiveAgility)`).
   - Tie-Breaker 3: Deterministic ordinal string Id comparison (`string.Compare(a.Id, b.Id, StringComparison.Ordinal)`).

4. **Action Preview Purity** (`Assets/Scripts/Combat/CtbCombatEngine.cs:56-73`):
   Performs deep copy via `_queue.CloneList()`, applies projected timeline shift strictly to the cloned actor, and returns sorted preview without mutating the live `_queue` or `actor.NextTurnTime`.

5. **Sync Move Detection & Mediator's Anchor** (`Assets/Scripts/Combat/CtbCombatEngine.cs:75-88, 103-108`):
   - Baseline Sync: requires both combatants to be allies (`IsAlly`), alive (`IsAlive`), with $|NextTurnTime_A - NextTurnTime_B| \le 2.0$.
   - Mediator's Anchor: `SetMediatorsAnchor` links `LinkedAllyId` bidirectionally; `CheckSyncMoveAvailable` overrides delta check if `allyA.LinkedAllyId == allyB.Id`, while still enforcing alliance and alive status.

6. **Intervention Mechanics** (`Assets/Scripts/Combat/CtbCombatEngine.cs:90-101`):
   Guards `protector.FocusPoints >= 25f`, deducts 25 FP, and advances `targetAlly.NextTurnTime = Math.Max(CurrentTime, targetAlly.NextTurnTime - 15.0f)` with immediate queue re-sorting.

7. **Injury Debuff Application & Campsite Clearing**:
   - `ApplyInjuryDebuff` sets `HasInjuryDebuff = true`, triggering -20% agility and +15% skill cost (compound delay multiplier $1.15 / 0.8 = 1.4375$, or +43.75%).
   - `ClearInjuryDebuff` sets `HasInjuryDebuff = false`.
   - Verified integration in `RealWorldWorkflowsE2ETests.cs:142-168` and `CtbCombatEngineProductionTests.cs:235-247`.

8. **Integrity Violation Audit**:
   - No hardcoded test return values in source files.
   - No dummy facades or skipped algorithmic logic.
   - All systems execute real runtime calculations.

---

## 2. Logic Chain

1. *Step 1 (Formula Verification)*: Observation 2 proves `CalculateTurnDelay` computes $(\text{SkillCost} / \text{Agility}) \times \text{WeatherMultiplier}$ when uninjured, and $(\text{SkillCost} \times 1.15 / (\text{Agility} \times 0.8)) \times \text{WeatherMultiplier}$ when injured. Zero agility is safely clamped to $1.0\text{f}$. `CommitAction` advances `actor.NextTurnTime = CurrentTime + delay` and sets `CurrentTime` to the next ready combatant, adhering strictly to the FFX CTB specification.
2. *Step 2 (Queue Stability & Preview Purity)*: Observation 3 establishes a mathematically total order with three levels of deterministic tie-breaking. Observation 4 verifies that `PreviewAction` operates entirely on cloned data structures, ensuring zero side-effects on live combat state.
3. *Step 3 (Teamwork Mechanics)*: Observations 5 & 6 establish that Sync Moves enforce the $\le 2.0$ timeline delta and alive/ally constraints. Mediator's Anchor overrides delta while preserving alive/ally validation. Intervention enforces the 25 FP expenditure and clamps advance to `CurrentTime`.
4. *Step 4 (Injury Recovery Lifecycle)*: Observation 7 verifies that injury penalties match GDD specifications (+43.75% delay penalty), and clearance restores baseline agility and turn delay.
5. *Step 5 (Empirical Validation)*: Observation 1 confirms all 156 tests run and pass without warnings or errors.

---

## 3. Caveats

- In `Assets/Scripts/Combat/CtbCombatEngine.cs`:
  - `CheckSyncMoveAvailable(allyA, allyB)` currently does not check `allyA.Id != allyB.Id`. Passing the same combatant reference twice evaluates delta to 0 and returns `true`.
  - `CalculateTurnDelay` does not clamp `skillCost` to non-negative values; passing negative costs could produce negative turn delays.
  - `PerformIntervention(protector, targetAlly)` does not forbid `protector.Id == targetAlly.Id`.
  These are defensive edge cases identified during adversarial review and are documented below as minor hardening recommendations. They do not block Milestone 2 requirements.
- 3D models, sound effects, and UI view bindings are deferred to subsequent milestones per `ORIGINAL_REQUEST.md`.

---

## 4. Conclusion

**Definitive Verdict**: **`APPROVE`**

Milestone 2 implementation of the pure C# CTB Combat Engine and core state systems satisfies all requirements outlined in `ORIGINAL_REQUEST.md`, `PROJECT.md`, and `agents.md`. The codebase exhibits genuine mathematical models, strict compliance with assembly definitions, deterministic priority queue sorting, verified teamwork mechanics (Sync Moves, Interventions, Mediator's Anchor), zero compiler warnings, and zero integrity violations.

---

## 5. Verification Method

To independently reproduce this verification:
```powershell
dotnet test "d:\Unity Projects\Ink and Ash\standalone-tests\InkAndAsh.StandaloneTests.csproj" --verbosity normal
```
Expected result:
```
Passed! - Failed: 0, Passed: 156, Skipped: 0, Total: 156, Duration: ~750 ms
```

Files to inspect:
- `Assets/Scripts/Combat/Combatant.cs`
- `Assets/Scripts/Combat/CtbCombatEngine.cs`
- `Assets/Scripts/Combat/TimelineQueue.cs`
- `Assets/Scripts/Combat/SkillAction.cs`
- `Assets/Scripts/Combat/CombatController.cs`

---

## Quality Review Report

### Review Summary
**Verdict**: **`APPROVE`**

### Findings

#### [Minor] Finding 1: Self-Sync Move Boundary Guard
- **What**: `CheckSyncMoveAvailable` returns `true` when given the same combatant as both `allyA` and `allyB`.
- **Where**: `Assets/Scripts/Combat/CtbCombatEngine.cs:75-88`
- **Why**: A Sync Move represents a tandem attack between two distinct allies. If a caller passes `(hero, hero)`, $|t - t| = 0 \le 2.0$ returns true.
- **Suggestion**: Add `if (string.Equals(allyA.Id, allyB.Id, StringComparison.Ordinal)) return false;` at the beginning of `CheckSyncMoveAvailable`.

#### [Minor] Finding 2: Negative Skill Cost Delay Guard
- **What**: `CalculateTurnDelay` accepts negative `skillCost` without clamping.
- **Where**: `Assets/Scripts/Combat/CtbCombatEngine.cs:30-39`
- **Why**: A negative skill cost results in a negative turn delay, potentially setting `NextTurnTime < CurrentTime`.
- **Suggestion**: Use `int safeCost = Math.Max(0, skillCost);` before calculating delay.

#### [Minor] Finding 3: Self-Intervention Guard
- **What**: `PerformIntervention` allows `protector` and `targetAlly` to be the same combatant.
- **Where**: `Assets/Scripts/Combat/CtbCombatEngine.cs:90-101`
- **Why**: If game design intends Intervention to be an ally-protecting action, self-targeting allows self-boosting timeline by 15 units for 25 FP.
- **Suggestion**: Add `if (string.Equals(protector.Id, targetAlly.Id, StringComparison.Ordinal)) return false;`.

### Verified Claims
- `BaseTurnDelay = (SkillCost / Agility) * WeatherMultiplier` $\rightarrow$ verified via `Ctb_TurnDelayFormula_StandardInputs`, `F2_T1_01_TurnDelayFormulaStandard` $\rightarrow$ PASS.
- `NextTurnTime = CurrentTime + BaseTurnDelay` $\rightarrow$ verified via `F2_T1_06_ActionCommitAdvancesTimeAndResorts` $\rightarrow$ PASS.
- Timeline priority queue ordering & action preview $\rightarrow$ verified via `Ctb_TimelineQueue_DeterministicAscendingSort`, `Ctb_PreviewAction_DoesNotMutateActiveTimeline` $\rightarrow$ PASS.
- Sync Move detection delta $\le 2.0$ $\rightarrow$ verified via `Ctb_SyncMove_DeltaWithinThreshold`, `F2_T1_07_SyncMoveDetectedWithinDelta2` $\rightarrow$ PASS.
- Intervention spending 25 FP and advancing target by up to 15 units $\rightarrow$ verified via `Ctb_Intervention_Spends25FP_AdvancesTarget`, `F2_T1_08_InterventionDeducts25FPAndAdvances15Units` $\rightarrow$ PASS.
- Mediator's Anchor links two allies for guaranteed Sync Moves $\rightarrow$ verified via `F2_T1_09_MediatorsAnchorGuaranteesSyncMove`, `F2_T3_03_MediatorsAnchorPreservesSyncMoveUnderInjuryDebuff` $\rightarrow$ PASS.
- Injury Debuffs (-20% Agility, +15% Skill Cost, net +43.75% delay, campsite clearing) $\rightarrow$ verified via `Ctb_InjuryDebuff_AppliesAgilityAndCostPenalties`, `Workflow_03_PartyDefeatCampReloadAndRestorativeRecovery` $\rightarrow$ PASS.

### Coverage Gaps
- None. All 15 production system files and their integration points are covered.

### Unverified Items
- None.

---

## Adversarial Challenge Report

### Challenge Summary
**Overall Risk Assessment**: **LOW**

### Challenges

#### [Low] Challenge 1: Self-Targeting in Teamwork Mechanics
- **Assumption challenged**: Combat callers will always pass two distinct characters to `CheckSyncMoveAvailable` and `PerformIntervention`.
- **Attack scenario**: AI or UI passes the active actor as both source and target. Active actor triggers a solitary Sync Move or uses Intervention on self to repeatedly take immediate turns.
- **Blast radius**: Low. Standard UI selection logic filters out self, but engine-level defense prevents logic errors.
- **Mitigation**: Add identity guards (`allyA.Id == allyB.Id`).

#### [Low] Challenge 2: Floating Point Precision Near Delta Threshold
- **Assumption challenged**: Floating point subtraction $|A - B|$ is exact at boundary $2.0$.
- **Attack scenario**: Due to prior floating point accumulation, delta is $2.00000024$, causing an intended Sync Move to fail.
- **Blast radius**: Very Low.
- **Mitigation**: Use epsilon tolerance: `Math.Abs(allyA.NextTurnTime - allyB.NextTurnTime) <= 2.0001f`.

### Stress Test Results
- Agility $= 0$ clamp: `Math.Max(1.0f, BaseAgility)` avoids DivideByZeroException $\rightarrow$ PASS.
- Severe blizzard weather ($1.25\times$ delay multiplier): verified formula linearity $\rightarrow$ PASS.
- Incapacitated ally during Sync Move: dead ally properly rejected $\rightarrow$ PASS.
- Insufficient Focus Points ($24.99$ FP): rejected, zero FP deducted $\rightarrow$ PASS.
- Intervention timeline clamp: target cannot be moved into past before `CurrentTime` $\rightarrow$ PASS.
- Tie breaking under equal turn time: Ally prioritized over Enemy; higher agility prioritized; Ordinal ID fallback $\rightarrow$ PASS.

### Unchallenged Areas
- Full 3D rendering and audio triggers: Out of scope for Milestone 2 (pure C# systems).

---

## Integrity Audit Certification
- **Hardcoded test results**: None detected.
- **Dummy or facade implementations**: None detected.
- **Bypassed tasks or shortcuts**: None detected.
- **Fabricated verification outputs**: None detected; independently verified via CLI test suite execution.
- **Self-certifying work**: None detected.
- **Integrity Status**: **CLEAN / COMPLIANT**.
