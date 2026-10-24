## 2026-10-24T20:03:51Z
You are teamwork_preview_test_writer (E2E Testing Architect & Writer).
Your working directory is: d:\Unity Projects\Ink and Ash\.agents\teamwork\test_writer_e2e
Your parent is: fc826616-6237-4548-b6ac-f0d5352bcd07 (Project Orchestrator)

MANDATORY: Read ORIGINAL_REQUEST.md first:
d:\Unity Projects\Ink and Ash\.agents\teamwork\ORIGINAL_REQUEST.md

Also read:
- d:\Unity Projects\Ink and Ash\.agents\teamwork\orchestrator_1\PROJECT.md
- d:\Unity Projects\Ink and Ash\.agents\teamwork\spec_miner_survey_2\spec_report.md
- d:\Unity Projects\Ink and Ash\.agents\teamwork\spec_miner_survey_3\spec_report.md
- d:\Unity Projects\Ink and Ash\agents.md

Your Task: Opaque-Box E2E Testing Track
You are responsible for designing and writing the requirement-driven E2E test suite derived strictly from user requirements (not implementation design).

Your Deliverables:
1. `d:\Unity Projects\Ink and Ash\TEST_INFRA.md`:
   - Follow the template in Project Pattern:
     * Test Philosophy (Opaque-box, requirement-driven)
     * Feature Inventory & Coverage thresholds:
       - Tier 1: Feature Coverage (>=5 per feature)
       - Tier 2: Boundary & Corner Cases (>=5 per feature)
       - Tier 3: Cross-Feature Combinations (pairwise interactions)
       - Tier 4: Real-World Application Scenarios (>=5 realistic application workflows)
     * Test Architecture & runner invocation (`dotnet test` and Unity UTF)
2. E2E Test Suite Implementation:
   - Create E2E test classes in `standalone-tests/E2ETests/` (and/or `Assets/Tests/EditMode/E2ETests/`) that execute tests across:
     * Data Catalog Integrity: 16 items loaded, weights > 0, perishables have decay, mineral items 0 decay, PAD bounds [-100, 100], 5 Acts per quest, 7 biomes.
     * CTB Combat Timeline: Turn Delay formula calculations, timeline ordering, Sync Moves (delta <= 2), Interventions (25 FP -> 15 timeline advance), Mediator's Anchor, Injury Debuffs (-20% turn gen, +15% delay cost).
     * Caravan Inventory: Foot Packs (30 slots, 50kg) vs Horse Carriage (120 slots, 300kg, 60% decay reduction), moisture weight increase in rain/swamp biomes, durability decay ticking, cooking recipes and 24-hr buffs.
     * Time & Weather Systems: 1 real min = 1 in-game hour, hour tick, day tick, 8-hour campsite rest, weather transitions.
3. Verification:
   - Ensure the test suite compiles cleanly with `dotnet test standalone-tests/InkAndAsh.StandaloneTests.csproj`.
4. Publish `TEST_READY.md`:
   - Write `d:\Unity Projects\Ink and Ash\TEST_READY.md` summarizing the test runner command, test tier counts, and feature coverage checklist.

Write your handoff report to:
`d:\Unity Projects\Ink and Ash\.agents\teamwork\test_writer_e2e\handoff.md`
Maintain `progress.md` in your directory.
When finished, send a message to parent (fc826616-6237-4548-b6ac-f0d5352bcd07) notifying of completion.
