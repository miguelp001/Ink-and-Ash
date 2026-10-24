## 2026-10-24T19:54:19Z
You are teamwork_preview_spec_miner (Spec Miner 3).
Your working directory is: d:\Unity Projects\Ink and Ash\.agents\teamwork\spec_miner_survey_3
Your parent is: fc826616-6237-4548-b6ac-f0d5352bcd07 (Project Orchestrator)

MANDATORY: Read ORIGINAL_REQUEST.md first:
d:\Unity Projects\Ink and Ash\.agents\teamwork\ORIGINAL_REQUEST.md

Also thoroughly read:
- d:\Unity Projects\Ink and Ash\Ink and Ash.md
- d:\Unity Projects\Ink and Ash\specs.md
- d:\Unity Projects\Ink and Ash\agents.md

Your task:
Mine and document all detailed specifications, algorithms, interfaces, and acceptance criteria for:
1. R3: Core Runtime Systems:
   - GameManager: State machine (Exploration, Campsite, Combat, Dialogue, Paused), singleton/service lifecycle
   - TimeManager: Day/night cycle, expedition time tracking, tick events, weather synchronization
   - Event Channels: ScriptableObject or C# event architecture for decoupled communication across assemblies
   - Pure C# CTB Combat Engine:
     * Final Fantasy X inspired Conditional Turn-Based model
     * Visible action timeline / queue calculation based on Actor Agility (Tick formula: Delay = ActionCost * (100 / Agility))
     * Turn order recalculation dynamically on action execution
     * Status effects, health, damage calculation, defeat handling
     * Decoupled pure C# core so it can run and be unit-tested without UnityEngine runtime dependencies
   - Caravan Inventory:
     * List-based caravan ledger (no spatial grid)
     * Weight capacity and encumbrance penalties
     * Perishable decay / spoilage ticks over time/travel
     * Campsite cooking recipe validation and day-long expedition buff application
   - Weather System:
     * 7 biomes climate and dynamic weather states (Clear, Rain, Ashfall, Blizzard, etc.)
     * Transitions and impacts on travel speed and spoilage rate
2. R4: Unit Tests in EditMode Assembly:
   - EditMode test assembly `InkAndAsh.Tests.EditMode` setup (.asmdef with references to test framework and system assemblies)
   - Test suites covering all core systems: CTB timeline math, turn ordering, inventory weight & decay, recipe cooking buffs, weather transitions, DataManager JSON loading
   - Edge cases, boundary value analysis, regression tests
   - How tests can be run via dotnet test or Unity CLI / runner

Write your detailed extraction report to:
`d:\Unity Projects\Ink and Ash\.agents\teamwork\spec_miner_survey_3\spec_report.md`
and standard handoff to:
`d:\Unity Projects\Ink and Ash\.agents\teamwork\spec_miner_survey_3\handoff.md`.
Maintain `progress.md` in your directory.
When finished, send a message to parent (fc826616-6237-4548-b6ac-f0d5352bcd07) notifying of completion.
