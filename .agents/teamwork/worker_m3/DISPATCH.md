## 2026-10-24T20:41:47Z
You are teamwork_preview_worker (Unit Testing & Quality Worker, M3).
Your working directory is: d:\Unity Projects\Ink and Ash\.agents\teamwork\worker_m3
Your parent is: fc826616-6237-4548-b6ac-f0d5352bcd07 (Project Orchestrator)

MANDATORY: Read ORIGINAL_REQUEST.md first:
d:\Unity Projects\Ink and Ash\.agents\teamwork\ORIGINAL_REQUEST.md

Also read:
- d:\Unity Projects\Ink and Ash\.agents\teamwork\orchestrator_1\PROJECT.md
- d:\Unity Projects\Ink and Ash\TEST_INFRA.md
- d:\Unity Projects\Ink and Ash\.agents\teamwork\reviewer_m2_2\handoff.md
- d:\Unity Projects\Ink and Ash\.agents\teamwork\challenger_m2_1\handoff.md
- d:\Unity Projects\Ink and Ash\agents.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Your Exclusive File Write Boundaries:
- `Assets/Tests/EditMode/DataManagerTests.cs`
- `Assets/Tests/EditMode/CtbCombatEngineTests.cs`
- `Assets/Tests/EditMode/CaravanInventoryTests.cs`
- `Assets/Tests/EditMode/WeatherSystemTests.cs`
- `Assets/Tests/EditMode/TimeManagerTests.cs`
- `Assets/Tests/EditMode/QuestManagerTests.cs`
- `Assets/Tests/EditMode/InkAndAsh.Tests.EditMode.asmdef`
- `Assets/Scripts/Economy/CaravanInventory.cs`
- `Assets/Scripts/Combat/CtbCombatEngine.cs`
- `standalone-tests/InkAndAsh.StandaloneTests.csproj`

Detailed Requirements to Implement:
1. Populate `Assets/Tests/EditMode/`:
   - Write comprehensive unit tests in NUnit runnable under Unity EditMode test framework AND `dotnet test`:
     * `DataManagerTests.cs`: loading all 16 items, verifying weights/durabilities, 3+ NPCs with PAD clamping and Love/Hate, 5-Act quests, 7 biomes.
     * `CtbCombatEngineTests.cs`: turn-order formula, ascending timeline queue, dynamic queue recalculation on action commit, action preview, Sync Move detection (delta <= 2.0), Intervention (25 FP -> 15 timeline units advance), Mediator's Anchor, Injury Debuffs (-20% turn gen, +15% delay cost), campsite clearing.
     * `CaravanInventoryTests.cs`: Foot Packs (30 slots, 50kg) vs Horse Carriage (120 slots, 300kg, 60% decay reduction), real-time durability decay down to 0%, moisture weight +50% in rain/swamp biomes, campsite cooking recipe validation and 24-hr buffs.
     * `WeatherSystemTests.cs`: 7 biomes climate and dynamic weather transitions, spoilage and stamina multipliers.
     * `TimeManagerTests.cs`: 1 real min = 1 in-game hour, hour tick, day tick, 8-hour campsite rest.
     * `QuestManagerTests.cs`: 5-Act progression (`Friction`, `Ripple`, `Crisis`, `Aftermath`, `Legacy`).
2. Apply Defensive Fixes:
   - In `CaravanInventory.cs`: ensure `RemoveItem(string itemId, int quantity)` rejects `quantity <= 0`, and cleanly deducts across multiple stacks if needed.
   - In `CtbCombatEngine.cs`: ensure `CalculateTurnDelay` safely clamps `agility <= 0` to minimum 1.0f.
3. Asmdef Configuration:
   - Ensure `Assets/Tests/EditMode/InkAndAsh.Tests.EditMode.asmdef` references all required assemblies:
     `"InkAndAsh.Core"`, `"InkAndAsh.Data"`, `"InkAndAsh.Combat"`, `"InkAndAsh.Economy"`, `"InkAndAsh.World"`, `"InkAndAsh.Narrative"` and `"UnityEngine.TestRunner"`, `"UnityEditor.TestRunner"`.
4. Standalone Test Runner:
   - Update `standalone-tests/InkAndAsh.StandaloneTests.csproj` to include these EditMode test classes.
   - Run `dotnet test standalone-tests/InkAndAsh.StandaloneTests.csproj`.
   - Confirm 100% of tests pass cleanly with 0 failures, 0 errors, and 0 warnings.
5. Write your handoff report to:
`d:\Unity Projects\Ink and Ash\.agents\teamwork\worker_m3\handoff.md`
Maintain `progress.md` in your directory.
When finished, send a message to parent (fc826616-6237-4548-b6ac-f0d5352bcd07) notifying of completion.
