## 2026-10-24T20:19:01Z
You are teamwork_preview_worker (Core Runtime Systems Worker, M2).
Your working directory is: d:\Unity Projects\Ink and Ash\.agents\teamwork\worker_m2
Your parent is: fc826616-6237-4548-b6ac-f0d5352bcd07 (Project Orchestrator)

MANDATORY: Read ORIGINAL_REQUEST.md first:
d:\Unity Projects\Ink and Ash\.agents\teamwork\ORIGINAL_REQUEST.md

Also read:
- d:\Unity Projects\Ink and Ash\.agents\teamwork\orchestrator_1\PROJECT.md
- d:\Unity Projects\Ink and Ash\.agents\teamwork\spec_miner_survey_3\spec_report.md
- d:\Unity Projects\Ink and Ash\TEST_INFRA.md
- d:\Unity Projects\Ink and Ash\standalone-tests/E2ETests/ (all existing tests)
- d:\Unity Projects\Ink and Ash\agents.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Your Exclusive File Write Boundaries:
- `Assets/Scripts/Core/GameManager.cs`
- `Assets/Scripts/Core/TimeManager.cs`
- `Assets/Scripts/Core/Events/GameEvent.cs`
- `Assets/Scripts/Core/Events/GameEventListener.cs`
- `Assets/Scripts/Core/Events/ConcreteEvents.cs`
- `Assets/Scripts/Combat/CtbCombatEngine.cs`
- `Assets/Scripts/Combat/Combatant.cs`
- `Assets/Scripts/Combat/TimelineQueue.cs`
- `Assets/Scripts/Combat/SkillAction.cs`
- `Assets/Scripts/Combat/CombatController.cs`
- `Assets/Scripts/Economy/CaravanInventory.cs`
- `Assets/Scripts/Economy/CookingSystem.cs`
- `Assets/Scripts/World/WeatherSystem.cs`
- `Assets/Scripts/World/BiomeManager.cs`
- `Assets/Scripts/Narrative/QuestManager.cs`
- `standalone-tests/InkAndAsh.StandaloneTests.csproj`

Detailed Requirements to Implement:
1. GameManager (`Assets/Scripts/Core/GameManager.cs`):
   - Persistent singleton (`DontDestroyOnLoad` in Unity runtime, static instance fallback for pure tests).
   - 5 states: `Exploration`, `Campsite`, `Combat`, `Dialogue`, `Paused`.
   - References to runtime systems (`TimeManager`, `WeatherSystem`, `CaravanInventory`, `DataManager`).
2. TimeManager (`Assets/Scripts/Core/TimeManager.cs`):
   - 1 real-time minute = 1 in-game hour (60x time speed).
   - 24-hour day cycle.
   - Raises `OnTimeHourTick` (int hour, int day) and `OnTimeDayTick` (int day).
   - Campsite Rest: advances time by 8 in-game hours (8 minutes real-time equivalent), ticks durability decay and weather for all 8 hours.
3. Event Channels (`Assets/Scripts/Core/Events/`):
   - `GameEvent<T>` (ScriptableObject base with raise and listener registration) and `GameEventListener<T>` (MonoBehaviour listener).
   - Pure C# event fallback interfaces so systems work decoupled in testing.
   - Concrete events: `OnCombatStarted`, `OnCombatEnded`, `OnTimeHourTick`, `OnTimeDayTick`, `OnItemDecayed`, `OnQuestActAdvanced`, `OnSpireCalibrated`.
4. Pure C# CTB Combat Engine (`Assets/Scripts/Combat/`):
   - `CtbCombatEngine`, `Combatant`, `TimelineQueue`, `SkillAction`.
   - Turn delay formula: `BaseTurnDelay = (SkillCost / Agility) * WeatherMultiplier`.
   - `NextTurnTime = CurrentTime + BaseTurnDelay`.
   - Dynamic timeline priority queue sorted by `NextTurnTime` with deterministic tie-breaking.
   - Action preview: `PreviewAction(...)` simulates timeline shifts before committing.
   - Sync Move detection: two allies adjacent on timeline with delta <= 2.0 timeline units.
   - Intervention: spend 25 Focus Points to advance target ally forward on timeline by up to 15 timeline units (`Math.Max(CurrentTime, targetTime - 15)`).
   - Mediator's Anchor: links two allies to guarantee Sync Moves regardless of delays.
   - Injury Debuffs: -20% turn generation (`EffectiveAgility = Agility * 0.8`), +15% skill delay costs. Cleared by full 8-hour campsite rest + cooked meal.
   - Thin MonoBehaviour `CombatController.cs`.
5. Caravan Inventory (`Assets/Scripts/Economy/`):
   - List-based caravan ledger (not spatial grid).
   - Capacity tiers: `FootPacks` (30 slots, 50kg max, 0% protection) vs `HorseCarriage` (120 slots, 300kg max, 60% decay reduction).
   - Durability decay ticking: 1% base per real-time minute, modified by biome weather (Desert +50%, Rain/Swamp +50%, Freezing 0.2%/min). Carriage reduces decay by 60% (0.40x multiplier).
   - Moisture weight: +50% weight for Golden Barley and Coarse Fiber in Rain/Swamp biomes.
   - Campsite Cooking: recipe validation from available unified ingredients, deducts ingredients, applies 24 in-game hour expedition buffs.
6. Weather System (`Assets/Scripts/World/`):
   - 7 biomes climate and dynamic weather states: `Clear`, `Rain`, `Heat`, `Freezing`, `Ashfall`.
   - Spoilage and stamina multipliers matching GDD values.
   - Spire calibration support.
7. QuestManager (`Assets/Scripts/Narrative/`):
   - 5-Act state machine: `Friction`, `Ripple`, `Crisis`, `Aftermath`, `Legacy`.
8. Verification:
   - Update `standalone-tests/InkAndAsh.StandaloneTests.csproj` to compile these production classes.
   - Run `dotnet test standalone-tests/InkAndAsh.StandaloneTests.csproj` to verify all 124+ unit tests and E2E tests pass.
   - Ensure zero compiler errors and zero warnings.

Write your handoff report to:
`d:\Unity Projects\Ink and Ash\.agents\teamwork\worker_m2\handoff.md`
Maintain `progress.md` in your directory.
When finished, send a message to parent (fc826616-6237-4548-b6ac-f0d5352bcd07) notifying of completion.
