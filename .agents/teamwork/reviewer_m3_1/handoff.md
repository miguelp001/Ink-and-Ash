# Milestone 3 Review & Adversarial Audit Handoff Report

**Reviewer**: teamwork_preview_reviewer (Reviewer 1, Milestone 3)  
**Parent**: fc826616-6237-4548-b6ac-f0d5352bcd07 (Project Orchestrator)  
**Date**: 2026-10-24T20:58:00Z  
**Verdict**: REQUEST_CHANGES  

---

## 1. Observation

### 1.1 Independent Test Suite Execution
Executed command:
```powershell
dotnet test standalone-tests/InkAndAsh.StandaloneTests.csproj --verbosity normal
```
Verbatim execution result:
```
Test run for D:\Unity Projects\Ink and Ash\standalone-tests\bin\Debug\net8.0\InkAndAsh.StandaloneTests.dll (.NETCoreApp,Version=v8.0)
A total of 1 test files matched the specified pattern.
NUnit Adapter 4.6.0.0: Test execution complete

Test Run Successful.
Total tests: 289
     Passed: 289
 Total time: 1.1985 Seconds
Build succeeded.
    0 Warning(s)
    0 Error(s)
```
Independent reproduction confirms 289 tests passed with 0 warnings and 0 errors.

### 1.2 Inspection of Assembly Definition
File: `Assets/Tests/EditMode/InkAndAsh.Tests.EditMode.asmdef`
Lines 4-13:
```json
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "InkAndAsh.Core",
        "InkAndAsh.Data",
        "InkAndAsh.Combat",
        "InkAndAsh.Economy",
        "InkAndAsh.World",
        "InkAndAsh.Narrative"
    ],
```
The assembly definition declares references to Unity test frameworks and all 6 gameplay domain assemblies. Precompiled reference to `nunit.framework.dll` and define constraint `UNITY_INCLUDE_TESTS` are configured.

### 1.3 Inspection of Defensive Hardening
1. `Assets/Scripts/Economy/CaravanInventory.cs`:
   - Line 69: `if (string.IsNullOrEmpty(itemId) || quantity <= 0) return false;` in `AddItem`.
   - Line 122: `if (string.IsNullOrEmpty(itemId) || quantity <= 0) return false;` in `RemoveItem`.
   - Lines 124-133: Computes `totalAvailable` across matching slots via `StringComparison.OrdinalIgnoreCase`. If `totalAvailable < quantity`, returns `false` without partial mutation.
   - Lines 135-149: Sequential multi-stack deduction removes empty slots cleanly via `_items.RemoveAt(i)`.
   - Lines 62-65: Added alias `SetHumidWeather(bool isHumid)` forwarding to `SetHumidityWeather(bool isHumid)`.
2. `Assets/Scripts/Combat/CtbCombatEngine.cs`:
   - Lines 30-35: Static method `CalculateTurnDelay(float agility, int skillCost, float weatherMultiplier = 1.0f)` clamps agility with `float effectiveAgility = Math.Max(1.0f, agility);` and `float safeWeather = Math.Max(0.1f, weatherMultiplier);`.
   - Lines 37-46: Instance method `CalculateTurnDelay(Combatant actor, int skillCost, float weatherMultiplier = 1.0f)` clamps agility with `float effectiveAgility = Math.Max(1.0f, actor.EffectiveAgility);` and `float safeWeather = Math.Max(0.1f, weatherMultiplier);`.

### 1.4 Unity Editor Compilation Defect Observed
File: `Assets/Tests/EditMode/CtbCombatEngineTests.cs`
Lines 1-5:
```csharp
using System;
using System.Collections.Generic;
using NUnit.Framework;
using InkAndAsh.Combat;
```
Line 435:
```csharp
Assert.That(_engine.Timeline.Any(c => c.Id == "enemy_beast"), Is.False);
```
Observation: Line 435 invokes LINQ extension method `Any(...)` on `_engine.Timeline` (type `IReadOnlyList<Combatant>`). The file does not include `using System.Linq;`.
File: `standalone-tests/InkAndAsh.StandaloneTests.csproj` line 4:
```xml
<ImplicitUsings>enable</ImplicitUsings>
```
The standalone runner succeeds because the .NET 8 SDK automatically injects `System.Linq` into implicit global usings.
However, Unity 6 does not enable implicit global usings when compiling `InkAndAsh.Tests.EditMode.asmdef`. Under Unity compilation, line 435 results in compiler error `CS1061: 'IReadOnlyList<Combatant>' does not contain a definition for 'Any'`.

---

## 2. Logic Chain

1. *Integrity Audit*:
   - Checked all production scripts in `Assets/Scripts/` for hardcoded test results, facade logic or task bypasses.
   - Verified that `CalculateTurnDelay`, `CommitAction`, `RemoveItem`, `AddItem`, `SetBiome`, `Tick`, `AdvanceQuestAct` execute mathematical formulas, state mutations and data structures rather than dummy mocks.
   - Checked test execution logs: authentic and independently verified (289 passed).
   - Finding: Zero integrity violations.

2. *Requirement Conformance*:
   - The 6 EditMode test suites directly test R4 requirements: JSON loading for 16 items, 3-tier NPCs, 5-act quests, 7 biomes, CTB turn delay formula, dynamic timeline sorting, sync move detection, intervention mechanics, mediator anchor, injury debuffs, caravan slot and weight caps, real-time durability decay, moisture weight, campsite cooking, temporal compression (60s = 1h), midnight day rollover and campsite rest.
   - All defensive fixes requested by orchestrator are present and verified.

3. *Cross-Environment Compatibility Analysis*:
   - Standalone .NET 8 project enables `<ImplicitUsings>enable</ImplicitUsings>`.
   - In Unity 6, asmdef assemblies compile against .NET Standard 2.1 without implicit global usings.
   - In `DataManagerTests.cs` (line 4) and `CaravanInventoryTests.cs` (line 3), `using System.Linq;` was explicitly imported.
   - In `CtbCombatEngineTests.cs`, `using System.Linq;` was omitted. Line 435 calls `_engine.Timeline.Any(...)`.
   - In Unity Editor, this will fail compilation with error CS1061, violating Acceptance Criteria R1 ("The working directory is a valid Unity 6 project that can be opened in Unity Hub without import errors").

4. *Actionable Remediation*:
   - Adding `using System.Linq;` to `Assets/Tests/EditMode/CtbCombatEngineTests.cs` resolves the issue without side effects.

---

## 3. Caveats

- Unity Editor was not actively running during this review turn because work is executed in headless CLI mode; verification of the CS1061 diagnostic is based on standard C# Roslyn compilation rules for .NET Standard 2.1 assemblies without implicit usings.
- Audio and art asset integrations remain excluded as planned for pure C# architecture scaffolding.

---

## 4. Conclusion

The Milestone 3 work product is well-crafted, mathematically sound and free of integrity violations. Test coverage is thorough across all 6 gameplay domains. Defensive hardening in `CaravanInventory.cs` and `CtbCombatEngine.cs` works as intended.
However, because `Assets/Tests/EditMode/CtbCombatEngineTests.cs` omits `using System.Linq;`, the test assembly will produce compile error CS1061 when imported into Unity Hub.
Therefore, the verdict is **REQUEST_CHANGES** for this single high-priority compilation fix.

---

## 5. Review Findings & Attack Surface

### 5.1 Review Summary
**Verdict**: REQUEST_CHANGES

### 5.2 Findings

#### Major Finding 1: Missing `using System.Linq;` in `CtbCombatEngineTests.cs`
- **What**: Line 435 calls `.Any(c => c.Id == "enemy_beast")` on `IReadOnlyList<Combatant>` without importing `System.Linq`.
- **Where**: `Assets/Tests/EditMode/CtbCombatEngineTests.cs`, line 435.
- **Why**: Standalone `dotnet test` masks this defect due to `<ImplicitUsings>enable</ImplicitUsings>` in `InkAndAsh.StandaloneTests.csproj`. Unity 6 compiles asmdef assemblies without implicit usings, which causes compiler error CS1061 upon opening the project in Unity Hub or running Unity Test Runner.
- **Suggestion**: Add `using System.Linq;` to `Assets/Tests/EditMode/CtbCombatEngineTests.cs` alongside the existing using directives.

### 5.3 Adversarial Stress-Testing Report
- **Attack Scenario 1**: Negative skill cost passed to CTB engine.
  * Observed: `CalculateTurnDelay` accepts negative `skillCost`, which could yield negative delays if caller provides bad data.
  * Blast Radius: Low; skill costs are defined via validated data catalogs.
  * Mitigation: Suggest clamping `Math.Max(0, skillCost)` in future engine hardening.
- **Attack Scenario 2**: Zero or negative agility.
  * Observed: Clamped to `1.0f` via `Math.Max(1.0f, agility)`.
  * Result: Passed without divide-by-zero or NaN.
- **Attack Scenario 3**: Multi-stack inventory depletion.
  * Observed: `RemoveItem` verifies `totalAvailable < quantity` before touching slots, avoiding partial deductions on failed operations.
  * Result: Passed cleanly.

---

## 6. Verification Method

To verify the test suite:
```powershell
dotnet test standalone-tests/InkAndAsh.StandaloneTests.csproj --verbosity normal
```

To verify the missing using directive:
1. Inspect lines 1-5 and line 435 of `Assets/Tests/EditMode/CtbCombatEngineTests.cs`.
2. Confirm presence of `Any(...)` on line 435 and absence of `using System.Linq;` at top of file.
3. Apply `using System.Linq;` to ensure compatibility across both Unity Editor and standalone runners.
