# Authoritative Specification Report: R1, R2, R5
**Project:** *The Ink and the Ash* (Unity 6 URP Cozy-Melancholic JRPG)  
**Agent:** `teamwork_preview_spec_miner` (Spec Miner 2)  
**Assigned Scope:** R1 (Project Structure & Asmdefs), R2 (Data Models & JSON Catalogs), R5 (Git & GitHub Setup)  
**Date:** 2026-10-24  

---

## 1. Executive Summary

This specification report documents the complete, authoritative technical specifications, schemas, interfaces, constraints, and operational commands for:
1. **R1: Unity 6 (URP) Project Structure and Assembly Definitions** (Folder hierarchy, 7 system `.asmdef` files + 1 edit-mode test `.asmdef`, dependency graph, `Packages/manifest.json` dependencies, and `ProjectSettings`).
2. **R2: Data Models and JSON Catalogs** (`DataManager` architecture, 16 unified trade items with perishability and trade margins, 3-tier NPC schema with PAD emotional states and Love/Hate relationship vectors, 5-Act quest state machine schema, and 7-biome environmental matrix).
3. **R5: Git Repository & GitHub Setup** (Unity-compliant `.gitignore`, repair of corrupted initial `.git` state, initial commit staging, and GitHub CLI public repository creation under `miguelp001/ink-and-ash`).

---

## 2. Features Discovered

| # | Category | Feature | Description | Inputs | Outputs | Error Behavior | Discovered Via |
|---|----------|---------|-------------|--------|---------|----------------|----------------|
| 1 | R1 Structure | Standard Unity 6 Directory Layout | Root folders `Assets/`, `Packages/`, `ProjectSettings/`, `Assets/Scripts/`, `Assets/Data/`, `Assets/Tests/EditMode/`, `Assets/Scenes/`, `Assets/Settings/` | Directory path creation | Organized Unity filesystem | Directory creation failure if permission denied | ORIGINAL_REQUEST.md line 16; Unity 6 Project Standard |
| 2 | R1 Asmdef | `InkAndAsh.Core.asmdef` | Foundation assembly for GameManager, TimeManager, Event Channels, Object Pooling, Save Hooks | C# source files under `Assets/Scripts/Core/` | Compiles `InkAndAsh.Core.dll` | Compiler error if circular dependencies exist | ORIGINAL_REQUEST.md line 18; GDD Section 8.1 |
| 3 | R1 Asmdef | `InkAndAsh.Data.asmdef` | Assembly for data schemas, catalogs, ScriptableObjects, and DataManager | References: `InkAndAsh.Core`, `Newtonsoft.Json` | Compiles `InkAndAsh.Data.dll` | Compiler error if Core or Newtonsoft missing | ORIGINAL_REQUEST.md line 19; GDD Section 8.2 |
| 4 | R1 Asmdef | `InkAndAsh.Combat.asmdef` | Pure C# CTB combat logic, turn queue, Sync moves, Interventions, Anchor, injury debuffs | References: `InkAndAsh.Core`, `InkAndAsh.Data` | Compiles `InkAndAsh.Combat.dll` | Compile error if missing Data/Core | ORIGINAL_REQUEST.md line 20; GDD Section 3.1 |
| 5 | R1 Asmdef | `InkAndAsh.Economy.asmdef` | Caravan ledger (Foot Packs vs Carriage), real-time perishable decay, campsite cooking buffs | References: `InkAndAsh.Core`, `InkAndAsh.Data` | Compiles `InkAndAsh.Economy.dll` | Compile error if missing Data/Core | ORIGINAL_REQUEST.md line 21; GDD Section 6.1 |
| 6 | R1 Asmdef | `InkAndAsh.World.asmdef` | 7 biomes data, Spire calibration states (3-step), weather patterns, overworld travel data | References: `InkAndAsh.Core`, `InkAndAsh.Data` | Compiles `InkAndAsh.World.dll` | Compile error if missing Data/Core | ORIGINAL_REQUEST.md line 22; GDD Section 2.2 |
| 7 | R1 Asmdef | `InkAndAsh.Narrative.asmdef` | 5-Act quest state machine, PAD emotional model, 6 factions, dialogue graphs, rumor propagation | References: `InkAndAsh.Core`, `InkAndAsh.Data` | Compiles `InkAndAsh.Narrative.dll` | Compile error if missing Data/Core | ORIGINAL_REQUEST.md line 23; GDD Section 5.1 |
| 8 | R1 Asmdef | `InkAndAsh.UI.asmdef` | Placeholder MonoBehaviours for timeline bar, caravan ledger, dialogue box, campsite interface | References: `InkAndAsh.Core`, `InkAndAsh.Data`, `InkAndAsh.Combat`, `InkAndAsh.Economy`, `InkAndAsh.World`, `InkAndAsh.Narrative`, `Unity.TextMeshPro`, `UnityEngine.UI` | Compiles `InkAndAsh.UI.dll` | Compile error if upstream systems missing | ORIGINAL_REQUEST.md line 24; GDD Section 8.4 |
| 9 | R1 Asmdef | `InkAndAsh.Tests.EditMode.asmdef` | Test assembly for NUnit edit mode unit tests | References: all system asmdefs, `UnityEngine.TestRunner`, `UnityEditor.TestRunner`, `nunit.framework.dll` | Compiles test assembly | Excluded from player release build | ORIGINAL_REQUEST.md line 60; GDD Section 8.2 |
| 10 | R1 Packages | Packages/manifest.json | Package manifest including URP 17.7.0, newtonsoft-json 3.2.2, test-framework 1.8.0, ugui 2.6.0, inputsystem 1.20.0 | JSON dependency dictionary | Unity resolves and downloads packages | Resolution failure if package name/version invalid | Local Unity 6000.6.4f1 package cache inspection |
| 11 | R1 Settings | ProjectSettings Config | Linear color space (`m_ActiveColorSpace: 1`), .NET Standard 2.1 (`apiCompatibilityLevel: 6`), 60 FPS target, ASTC for Android, BC7 for Windows | YAML project settings assets | Configures Unity player build settings | Falls back to default gamma/mono if missing | ORIGINAL_REQUEST.md line 16; test project ProjectSettings |
| 12 | R2 Data | DataManager Architecture | Central catalog loader and validator using Newtonsoft Json.NET and fast lookup dictionaries | File paths to JSON catalog files | Validated in-memory item, NPC, quest, biome registries | Logs `LogError` with fallback to empty collection, never crashes | ORIGINAL_REQUEST.md line 40; GDD Section 8.2 |
| 13 | R2 Items | 16-Item Unified Trade Catalog | Unified pool: 16 tradable/craftable/culinary/alchemical items with perishability and weights | JSON array in `Assets/Data/Items/items.json` | 16 populated `ItemData` objects | Validation fails if weight <= 0 or decayRate < 0 | ORIGINAL_REQUEST.md line 32; GDD Section 6.1 |
| 14 | R2 Items | Perishability & Decay Math | Raw organic durability decay: 1% base/min; Desert +50% (1.5%/min); Swamp/Rain +50% (1.5%/min); Freezing 0.2%/min; Carriage reduces decay by 60% | Minute ticks, weather state, carriage tier | Updated item durability float (0.0 to 1.0) | Clamps at 0.0 (spoiled item), logs spoil event | GDD Section 2.3 & 6.2 |
| 15 | R2 Items | Biome Trade Margins | Biome-origin purchase price discounts and cross-biome scarcity resale margins (+50% to +150%) | Base value, origin biome, current biome | Dynamic buy/sell price calculation | Negative prices prevented by clamping | GDD Section 6.1 & 6.3 |
| 16 | R2 NPCs | 3-Tier NPC Data Schema | Schema supporting Tier 1 Core Cast (~10-15), Tier 2 Village Anchors (~40-50), Tier 3 Background (~150+) | JSON array in `Assets/Data/NPCs/npcs.json` | Deserialized `NpcData` instances | Logs error if tier not in [1, 3] | ORIGINAL_REQUEST.md line 34; GDD Section 5.4 |
| 17 | R2 NPCs | PAD Emotional Model | Pleasure, Arousal, Dominance emotional vectors (-100 to +100) reflecting psychological states | Deltas from dialogue choices, shared food, dispute outcomes | Dynamic PAD coordinates affecting dialogue triggers | Clamps out-of-range values to [-100, +100] | ORIGINAL_REQUEST.md line 34; GDD Section 5.3 |
| 18 | R2 NPCs | Relationship Vectors | Pixel Crushers Love/Hate vectors: `imperialFear` (0 to 100) and `factionTrust` (0 to 100) | Dialogue actions, quest progress, rumors | Influences NPC cooperation and willingness to speak | Clamped to [0, 100] | GDD Section 5.3 & 5.4 |
| 19 | R2 NPCs | 6 Internal Village Factions | Canonical factions: Traditionalists, Pragmatists, YoungRadicals, TheBurdened, Outsiders, ImperialBureaucracy | Faction string / enum identifier | Maps NPC loyalty and conflict axis | Fallback to Outsiders if unrecognized | GDD Section 5.2 |
| 20 | R2 Quests | 5-Act Quest State Machine | 5 Acts: Friction (Act I), Ripple (Act II), Crisis (Act III), Aftermath (Act IV), Legacy (Act V) | JSON records in `Assets/Data/Quests/quests.json` | `QuestData` state with per-act flags and rewards | Transitions validated; invalid skips rejected | ORIGINAL_REQUEST.md line 36; GDD Section 5.1 |
| 21 | R2 Quests | Reciprocity & Legacy Boons | Act V rewards: free smithy maintenance, alchemical cooling lining, imperial patrol distraction | Act V completion trigger | Permanent world state modifiers | Unregistered boon logs warning | GDD Section 5.1 & 6.3 |
| 22 | R2 Biomes | 7 Biomes Matrix Catalog | Grasslands, Coast, Forest, Mountains, Swamp, Desert, Jungle with Spire names, hazards, multipliers | JSON array in `Assets/Data/Biomes/biomes.json` | 7 `BiomeData` records with environmental specs | Default to Grasslands if biomeId unknown | ORIGINAL_REQUEST.md line 38; GDD Section 2.2 |
| 23 | R2 Biomes | Spire Calibration Sequence | 3-step state: 1. Ascend Peak, 2. Clear Hazards, 3. Light Beacon -> Regional World State Shift | Spire interaction events | `spireCalibrated: true`, monster repel aura active | Sequence skipping prohibited | GDD Section 2.4 |
| 24 | R5 Git | Unity-Standard .gitignore | Excludes Library/, Temp/, Obj/, Build/, Logs/, UserSettings/, IDE caches, .apk, .aab; tracks .meta | Git stage commands | Ignores ephemeral/generated files | Unignored Library/ commits bloat repo | ORIGINAL_REQUEST.md line 73; specs.md line 36 |
| 25 | R5 Git | Git Initialization & Commit | Repair corrupted `.git`, initialize branch `main`, stage all project files, create initial commit | `git init`, `git branch -M main`, `git add`, `git commit` | Clean git repository with initial commit SHA | Fails if working tree locked or invalid | System inspection; ORIGINAL_REQUEST.md line 73 |
| 26 | R5 GitHub | GitHub CLI Public Repo | Create public repo `miguelp001/ink-and-ash` via `gh repo create` and push `main` branch | `gh repo create miguelp001/ink-and-ash --public --source=. --remote=origin --push` | Remote repository live on GitHub | Fails if name collision or network drop | gh auth status check; ORIGINAL_REQUEST.md line 73 |

---

## 3. Edge Cases

| # | Feature | Input / Condition | Observed / Documented Behavior |
|---|---------|-------------------|--------------------------------|
| 1 | Assembly Definitions | Circular dependency (e.g. Data references Combat, Combat references Data) | Unity compilation fails with `error CS0246` / cyclical assembly definition error. Architecture strictly guarantees a Directed Acyclic Graph (Core <- Data <- Systems <- UI). |
| 2 | Assembly Definitions | `Newtonsoft.Json` reference missing in `InkAndAsh.Data.asmdef` | Compiling `JsonConvert` calls fails. Must specify `"overrideReferences": false` or include `"Newtonsoft.Json"` in references. |
| 3 | Assembly Definitions | Test assembly compiled without `UNITY_INCLUDE_TESTS` constraint | Test code and `nunit.framework.dll` would leak into player standalone builds, causing build errors or bloat. |
| 4 | DataManager Loading | Missing catalog JSON file (e.g. `items.json` missing or path typo) | DataManager catches `FileNotFoundException`, logs `Debug.LogError`, initializes empty list, and sets catalog status to failed without throwing an unhandled exception. |
| 5 | DataManager Loading | Malformed JSON syntax (trailing comma or unmatched brackets) | DataManager catches `JsonReaderException`, logs detailed file position and error message, returns fallback empty catalog. |
| 6 | Item Catalog | Item with `baseWeightKg <= 0` or negative decay rate | DataManager validation catches invalid numerical property, logs validation error, and rejects or clamps to minimum valid values (`baseWeightKg = 0.01f`, `decayRate = 0.0f`). |
| 7 | Item Durability Decay | Extreme freezing environment applied to non-perishable item (e.g. `Ironwood Resin` with base decay = 0) | Durability decay remains 0.0%/min (`0 * 0.2 = 0`). Only organic items with `isPerishable: true` calculate decay. |
| 8 | Item Durability Decay | Organic item in Desert heat reaches 0% durability | Durability drops to 0.0, item is flagged as `isSpoiled: true`, name changes to `"Spoiled Rations"` or triggers `OnItemDecayed` event, rendering it unsuitable for cooking. |
| 9 | NPC PAD Values | Input PAD pleasure = 150.0 (outside [-100, +100]) | DataManager validation clamps value to +100.0 and logs a warning, preserving mathematical bounds of the Love/Hate PAD space. |
| 10 | NPC Factions | Unknown faction name string in JSON (e.g. `"Rebels"`) | DataManager logs warning and maps to fallback enum `FactionAffiliation.Outsiders`. |
| 11 | Quest State Machine | Quest act completion skipped (attempting to trigger Act III Crisis while Act I is incomplete) | Quest state machine rejects transition, maintains current act, and logs illegal transition attempt. |
| 12 | Quest Rewards | Act V completion executed multiple times | Permanent boons (free smithy repairs, carriage cooling) are idempotent flags; duplicate execution does not double-apply bonuses. |
| 13 | Biome Travel | Traveling in Mountains/Frost Spire without cold gear or warming meal buff | Party stamina drain accelerates by 2.0x, triggering travel fatigue and combat injury vulnerability. |
| 14 | Git Setup | Existing corrupted `.git` folder (contains only `config` and `HEAD`) | Standard `git status` fails with `fatal: not a git repository`. Executing `git init` in the directory safely re-creates missing ref and object databases without destroying working directory files. |
| 15 | GitHub CLI Repo | Remote repository `miguelp001/ink-and-ash` already exists on GitHub | `gh repo create` returns error `GraphQL: Name already exists on this account`. Workaround: check existence via `gh repo view miguelp001/ink-and-ash`, attach remote via `git remote add origin`, and push with `git push -u origin main`. |

---

## 4. Deep Dive: R1 - Unity 6 (URP) Project Structure & Assembly Definitions

### 4.1 Required Directory Hierarchy
Under `d:\Unity Projects\Ink and Ash\`:
```
Assets/
├── Audio/
├── Data/
│   ├── Biomes/
│   │   └── biomes.json
│   ├── Items/
│   │   └── items.json
│   ├── NPCs/
│   │   └── npcs.json
│   └── Quests/
│       └── quests.json
├── Materials/
├── Prefabs/
├── Scenes/
├── Scripts/
│   ├── Combat/
│   │   └── InkAndAsh.Combat.asmdef
│   ├── Core/
│   │   ├── Events/
│   │   └── InkAndAsh.Core.asmdef
│   ├── Data/
│   │   ├── Models/
│   │   └── InkAndAsh.Data.asmdef
│   ├── Economy/
│   │   └── InkAndAsh.Economy.asmdef
│   ├── Narrative/
│   │   └── InkAndAsh.Narrative.asmdef
│   ├── UI/
│   │   └── InkAndAsh.UI.asmdef
│   └── World/
│       └── InkAndAsh.World.asmdef
├── Settings/
│   ├── URP_HighQuality_PC.asset
│   ├── URP_LowPower_Mobile.asset
│   └── UniversalRenderer.asset
└── Tests/
    └── EditMode/
        └── InkAndAsh.Tests.EditMode.asmdef
Packages/
└── manifest.json
ProjectSettings/
├── EditorSettings.asset
├── GraphicsSettings.asset
├── ProjectSettings.asset
├── ProjectVersion.txt
└── QualitySettings.asset
```

### 4.2 Assembly Definition Dependency Graph
The compile-time architecture enforces strict unidirectional layering with zero circular references:

```
                  ┌──────────────────────┐
                  │   InkAndAsh.Core     │
                  └──────────▲───────────┘
                             │
                  ┌──────────┴───────────┐
                  │   InkAndAsh.Data     │
                  └─────▲────▲─────▲─────┘
         ┌──────────────┘    │     └──────────────┐
         │                   │                    │
┌────────┴────────┐  ┌───────┴────────┐  ┌────────┴────────┐
│ InkAndAsh.Combat│  │InkAndAsh.Economy│  │ InkAndAsh.World │
└────────▲────────┘  └───────▲────────┘  └────────▲────────┘
         │                   │                    │
         └──────────────┐    │     ┌──────────────┘
                        │    │     │
                  ┌─────┴────┴─────┴─────┐
                  │ InkAndAsh.Narrative  │
                  └──────────▲───────────┘
                             │
                  ┌──────────┴───────────┐
                  │    InkAndAsh.UI      │
                  └──────────────────────┘
                             ▲
                             │ (References all above + TestRunner)
                  ┌──────────┴───────────┐
                  │InkAndAsh.Tests.EditMode│
                  └──────────────────────┘
```

### 4.3 Exact `.asmdef` JSON Specifications

#### 1. `Assets/Scripts/Core/InkAndAsh.Core.asmdef`
```json
{
    "name": "InkAndAsh.Core",
    "rootNamespace": "InkAndAsh.Core",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

#### 2. `Assets/Scripts/Data/InkAndAsh.Data.asmdef`
```json
{
    "name": "InkAndAsh.Data",
    "rootNamespace": "InkAndAsh.Data",
    "references": [
        "InkAndAsh.Core"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

#### 3. `Assets/Scripts/Combat/InkAndAsh.Combat.asmdef`
```json
{
    "name": "InkAndAsh.Combat",
    "rootNamespace": "InkAndAsh.Combat",
    "references": [
        "InkAndAsh.Core",
        "InkAndAsh.Data"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

#### 4. `Assets/Scripts/Economy/InkAndAsh.Economy.asmdef`
```json
{
    "name": "InkAndAsh.Economy",
    "rootNamespace": "InkAndAsh.Economy",
    "references": [
        "InkAndAsh.Core",
        "InkAndAsh.Data"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

#### 5. `Assets/Scripts/World/InkAndAsh.World.asmdef`
```json
{
    "name": "InkAndAsh.World",
    "rootNamespace": "InkAndAsh.World",
    "references": [
        "InkAndAsh.Core",
        "InkAndAsh.Data"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

#### 6. `Assets/Scripts/Narrative/InkAndAsh.Narrative.asmdef`
```json
{
    "name": "InkAndAsh.Narrative",
    "rootNamespace": "InkAndAsh.Narrative",
    "references": [
        "InkAndAsh.Core",
        "InkAndAsh.Data"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

#### 7. `Assets/Scripts/UI/InkAndAsh.UI.asmdef`
```json
{
    "name": "InkAndAsh.UI",
    "rootNamespace": "InkAndAsh.UI",
    "references": [
        "InkAndAsh.Core",
        "InkAndAsh.Data",
        "InkAndAsh.Combat",
        "InkAndAsh.Economy",
        "InkAndAsh.World",
        "InkAndAsh.Narrative",
        "Unity.TextMeshPro",
        "UnityEngine.UI"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

#### 8. `Assets/Tests/EditMode/InkAndAsh.Tests.EditMode.asmdef`
```json
{
    "name": "InkAndAsh.Tests.EditMode",
    "rootNamespace": "InkAndAsh.Tests.EditMode",
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
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

### 4.4 `Packages/manifest.json` Specification
The Unity 6 Package Manager manifest must declare the following exact package versions:
```json
{
  "dependencies": {
    "com.unity.inputsystem": "1.20.0",
    "com.unity.nuget.newtonsoft-json": "3.2.2",
    "com.unity.render-pipelines.universal": "17.7.0",
    "com.unity.shadergraph": "17.6.0",
    "com.unity.test-framework": "1.8.0",
    "com.unity.ugui": "2.6.0",
    "com.unity.modules.ai": "1.0.0",
    "com.unity.modules.animation": "1.0.0",
    "com.unity.modules.audio": "1.0.0",
    "com.unity.modules.imageconversion": "1.0.0",
    "com.unity.modules.imgui": "1.0.0",
    "com.unity.modules.jsonserialize": "1.0.0",
    "com.unity.modules.physics": "1.0.0",
    "com.unity.modules.ui": "1.0.0",
    "com.unity.modules.uielements": "1.0.0",
    "com.unity.modules.unitywebrequest": "1.0.0"
  }
}
```

### 4.5 ProjectSettings Configuration Values
- **Unity Version (`ProjectSettings/ProjectVersion.txt`)**:
  `m_EditorVersion: 6000.6.4f1`
  `m_EditorVersionWithRevision: 6000.6.4f1 (12bfff696524)`
- **Color Space**: Linear (`m_ActiveColorSpace: 1` in `ProjectSettings.asset`)
- **API Compatibility Level**: .NET Standard 2.1 (`apiCompatibilityLevel: 6` in `ProjectSettings.asset`)
- **Target Frame Rate**: 60 FPS (`accelerometerFrequency: 60`, runtime `Application.targetFrameRate = 60`)
- **Texture Compression**:
  - Android: ASTC (`m_EtcTextureCompressorBehavior: 1` in `EditorSettings.asset`)
  - Standalone PC: BC7 (`m_Bc7TextureCompressor: 0` in `EditorSettings.asset`)

---

## 5. Deep Dive: R2 - Data Models and JSON Catalogs

### 5.1 C# Data Models & Schemas

#### A. Item Model (`ItemData.cs`)
```csharp
namespace InkAndAsh.Data
{
    [System.Serializable]
    public class ItemData
    {
        public string itemId;
        public string name;
        public string primaryBiome;
        public float baseWeightKg;
        public float decayRatePerMinute;
        public bool isPerishable;
        public string[] usageCategories;
        public int basePrice;
        public System.Collections.Generic.Dictionary<string, float> biomePriceModifiers;
        public string description;
        public string storageNotes;

        public bool IsValid()
        {
            if (string.IsNullOrEmpty(itemId) || string.IsNullOrEmpty(name)) return false;
            if (baseWeightKg <= 0f) return false;
            if (decayRatePerMinute < 0f) return false;
            if (usageCategories == null || usageCategories.Length == 0) return false;
            return true;
        }
    }
}
```

#### B. NPC Model (`NpcData.cs`)
```csharp
namespace InkAndAsh.Data
{
    [System.Serializable]
    public class PADState
    {
        public float pleasure;   // -100 to +100
        public float arousal;    // -100 to +100
        public float dominance;  // -100 to +100

        public bool IsValid() =>
            pleasure >= -100f && pleasure <= 100f &&
            arousal >= -100f && arousal <= 100f &&
            dominance >= -100f && dominance <= 100f;

        public void Clamp()
        {
            pleasure = UnityEngine.Mathf.Clamp(pleasure, -100f, 100f);
            arousal = UnityEngine.Mathf.Clamp(arousal, -100f, 100f);
            dominance = UnityEngine.Mathf.Clamp(dominance, -100f, 100f);
        }
    }

    [System.Serializable]
    public class RelationshipVectors
    {
        public float imperialFear; // 0 to 100
        public float factionTrust; // 0 to 100
    }

    [System.Serializable]
    public class DialogueResponse
    {
        public string responseId;
        public string text;
        public PADState targetPadDelta;
        public string nextQuestState;
        public string nextBranchId;
    }

    [System.Serializable]
    public class DialogueBranch
    {
        public string branchId;
        public string triggerCondition;
        public string dialogueText;
        public System.Collections.Generic.List<DialogueResponse> availableResponses;
    }

    [System.Serializable]
    public class NpcData
    {
        public string npcId;
        public int tier; // 1: Core Cast, 2: Village Anchor, 3: Background Cast
        public string name;
        public string homeBiome;
        public string factionAffiliation; // Traditionalists, Pragmatists, YoungRadicals, TheBurdened, Outsiders, ImperialBureaucracy
        public PADState padEmotionalState;
        public RelationshipVectors relationshipVectors;
        public System.Collections.Generic.List<DialogueBranch> dialogueTreeBranches;

        public bool IsValid()
        {
            if (string.IsNullOrEmpty(npcId) || string.IsNullOrEmpty(name)) return false;
            if (tier < 1 || tier > 3) return false;
            if (padEmotionalState == null || !padEmotionalState.IsValid()) return false;
            return true;
        }
    }
}
```

#### C. Quest Model (`QuestData.cs`)
```csharp
namespace InkAndAsh.Data
{
    public enum QuestAct
    {
        Friction = 1,
        Ripple = 2,
        Crisis = 3,
        Aftermath = 4,
        Legacy = 5
    }

    [System.Serializable]
    public class QuestReward
    {
        public int insightPoints;
        public PADState padDelta;
        public System.Collections.Generic.Dictionary<string, int> itemRewards;
        public string unlockedBoon;
    }

    [System.Serializable]
    public class QuestActData
    {
        public string actId;
        public QuestAct act;
        public string title;
        public string description;
        public string completionFlag;
        public bool isCompleted;
        public QuestReward reward;
    }

    [System.Serializable]
    public class QuestData
    {
        public string questId;
        public string questTitle;
        public string npcId;
        public string targetBiome;
        public QuestAct currentAct;
        public System.Collections.Generic.Dictionary<string, bool> actCompletionFlags;
        public System.Collections.Generic.List<QuestActData> acts;

        public bool IsValid()
        {
            if (string.IsNullOrEmpty(questId) || string.IsNullOrEmpty(npcId)) return false;
            if (acts == null || acts.Count != 5) return false;
            return true;
        }
    }
}
```

#### D. Biome Model (`BiomeData.cs`)
```csharp
namespace InkAndAsh.Data
{
    [System.Serializable]
    public class WeatherHazard
    {
        public string hazardName;
        public string weatherState; // Clear, Rain, Heat, Freezing, Storm, Fog, Blizzard
        public float probability;
        public float spoilageModifier;
        public float staminaDrainModifier;
    }

    [System.Serializable]
    public class BiomeData
    {
        public string biomeId;
        public string name;
        public string spireName;
        public float spoilageMultiplier;
        public float staminaDrainMultiplier;
        public float travelSpeedModifier;
        public int hazardRating; // 1 to 5
        public bool spireCalibrated;
        public System.Collections.Generic.List<WeatherHazard> weatherHazards;
        public string[] primaryResources;

        public bool IsValid()
        {
            if (string.IsNullOrEmpty(biomeId) || string.IsNullOrEmpty(spireName)) return false;
            if (spoilageMultiplier < 0f || staminaDrainMultiplier <= 0f) return false;
            return true;
        }
    }
}
```

---

### 5.2 The 16 Unified Trade Goods Catalog Specification

| # | Item ID | Name | Primary Biome | Base Wt (kg) | Decay/min | Perishable | Usage Categories | Base Price | Trade Margins & Scarcity Profiles | Storage Notes |
|---|---------|------|---------------|--------------|-----------|------------|------------------|------------|-----------------------------------|---------------|
| 1 | `item_golden_barley` | Golden Barley | The Grasslands | 1.0 | 0.002 (0.2%) | True | Trade, Culinary | 10 | Grasslands 1.0x, Mountains 2.0x, Desert 1.8x, Coast 1.3x | Dry storage recommended. |
| 2 | `item_sun_dried_kelp` | Sun-Dried Kelp | The Coast | 0.5 | 0.001 (0.1%) | True | Trade, Crafting, Culinary, Alchemy | 12 | Coast 1.0x, Mountains 2.2x, Desert 1.9x, Forest 1.4x | Vulnerable to high humidity. |
| 3 | `item_ironwood_resin` | Ironwood Resin | The Forest | 0.8 | 0.0 | False | Trade, Crafting, Alchemy | 25 | Forest 1.0x, Grasslands 1.5x, Desert 1.8x, Coast 1.4x | Non-perishable; stable cargo. |
| 4 | `item_glacier_salt` | Glacier Salt | The Mountains | 1.5 | 0.0 | False | Trade, Culinary, Alchemy | 30 | Mountains 1.0x, Swamp 2.5x, Jungle 2.2x, Desert 2.0x | Non-perishable; preserves meat. |
| 5 | `item_bog_ash` | Bog Ash | The Swamp | 1.0 | 0.0 | False | Trade, Crafting, Alchemy | 18 | Swamp 1.0x, Grasslands 1.8x, Mountains 1.6x, Forest 1.4x | Non-perishable; stable mineral. |
| 6 | `item_solar_clay` | Solar Clay | The Desert | 3.0 | 0.0 | False | Trade, Crafting | 20 | Desert 1.0x, Mountains 2.4x, Swamp 2.0x, Coast 1.6x | Heavy carriage bulk load. |
| 7 | `item_canopy_bloom` | Canopy Bloom | The Jungle | 0.2 | 0.020 (2.0%) | True | Trade, Crafting, Culinary, Alchemy | 35 | Jungle 1.0x, Desert 2.5x, Mountains 2.2x, Grasslands 1.8x | High perishability; wilts in heat. |
| 8 | `item_wild_game_meat` | Wild Game Meat | Universal | 2.0 | 0.030 (3.0%) | True | Trade, Culinary | 15 | Universal 1.0x, Desert 2.0x, Mountains 1.5x | Extreme perishability; rots rapidly without salt. |
| 9 | `item_fresh_river_fish` | Fresh River Fish | Universal | 1.5 | 0.035 (3.5%) | True | Trade, Culinary | 14 | Universal 1.0x, Desert 2.2x, Mountains 1.8x | Extreme perishability; degrades rapidly. |
| 10 | `item_spring_water` | Spring Water | Universal | 2.5 | 0.0 | False | Trade, Culinary, Alchemy | 8 | Universal 1.0x, Desert 3.0x | Non-perishable; requires clean barrel. |
| 11 | `item_aromatic_herbs` | Aromatic Herbs | Universal | 0.3 | 0.010 (1.0%) | True | Trade, Culinary, Alchemy | 16 | Universal 1.0x, Mountains 1.8x, Coast 1.4x | Moderate perishability; wilts over time. |
| 12 | `item_timber_logs` | Timber Logs | The Forest | 10.0 | 0.0 | False | Trade, Crafting | 12 | Forest 1.0x, Desert 2.5x, Grasslands 1.6x | Non-perishable; heavy carriage bulk load. |
| 13 | `item_scrap_iron` | Scrap Iron | Imperial Posts | 5.0 | 0.0 | False | Trade, Crafting | 28 | Imperial Posts 1.0x, Swamp 2.2x, Jungle 2.0x | Non-perishable; required for upgrades. |
| 14 | `item_coarse_fiber` | Coarse Fiber | The Grasslands | 1.0 | 0.0 | False | Trade, Crafting | 10 | Grasslands 1.0x, Mountains 1.7x, Jungle 1.5x | Non-perishable; standard utility textile. |
| 15 | `item_tallow_wax` | Tallow Wax | Universal | 0.8 | 0.002 (0.2%) | True | Trade, Crafting, Alchemy | 14 | Universal 1.0x, Mountains 1.8x, Desert 1.6x | Melts in Desert heat. |
| 16 | `item_imperial_ink` | Imperial Ink | Imperial Posts | 0.5 | 0.0 | False | Trade, Crafting | 50 | Imperial Posts 1.0x, All Biomes 2.0x | Non-perishable; high-value trade commodity. |

---

### 5.3 3 Sample NPCs Specification

#### NPC 1: `npc_core_vesper_01` (Tier 1: Core Cast Companion)
```json
{
  "npcId": "npc_core_vesper_01",
  "tier": 1,
  "name": "Vesper the Weaver",
  "homeBiome": "The Forest",
  "factionAffiliation": "TheBurdened",
  "padEmotionalState": {
    "pleasure": -20.0,
    "arousal": 40.0,
    "dominance": -10.0
  },
  "relationshipVectors": {
    "imperialFear": 65.0,
    "factionTrust": 45.0
  },
  "dialogueTreeBranches": [
    {
      "branchId": "dlg_vesper_intro",
      "triggerCondition": "always",
      "dialogueText": "The threads we carry snap under tension. The empire counts every yard of linen, but never the hands that grew raw spinning it.",
      "availableResponses": [
        {
          "responseId": "resp_vesper_comfort",
          "text": "Rest by the fire tonight, Vesper. We share the watch and the load.",
          "targetPadDelta": { "pleasure": 20.0, "arousal": -15.0, "dominance": 10.0 },
          "nextQuestState": "",
          "nextBranchId": "dlg_vesper_relieved"
        }
      ]
    }
  ]
}
```

#### NPC 2: `npc_anchor_kael_01` (Tier 2: Village Anchor)
```json
{
  "npcId": "npc_anchor_kael_01",
  "tier": 2,
  "name": "Kaelen the Miller",
  "homeBiome": "The Grasslands",
  "factionAffiliation": "Pragmatists",
  "padEmotionalState": {
    "pleasure": -15.0,
    "arousal": 45.0,
    "dominance": -30.0
  },
  "relationshipVectors": {
    "imperialFear": 75.0,
    "factionTrust": 20.0
  },
  "dialogueTreeBranches": [
    {
      "branchId": "dlg_kael_act1_intro",
      "triggerCondition": "pad_emotional_state.arousal > 30.0",
      "dialogueText": "The imperial tax collectors took half the harvest. If the mill stops turning, the village starves before first freeze.",
      "availableResponses": [
        {
          "responseId": "resp_offer_grain",
          "text": "We have Golden Barley in our carriage. Let us share what we carry.",
          "targetPadDelta": { "pleasure": 25.0, "arousal": -20.0, "dominance": 10.0 },
          "nextQuestState": "Ripple",
          "nextBranchId": "dlg_kael_act1_accept"
        }
      ]
    }
  ]
}
```

#### NPC 3: `npc_bg_elda_01` (Tier 3: Living Background Cast)
```json
{
  "npcId": "npc_bg_elda_01",
  "tier": 3,
  "name": "Elda the Ferryhand",
  "homeBiome": "The Swamp",
  "factionAffiliation": "Outsiders",
  "padEmotionalState": {
    "pleasure": 10.0,
    "arousal": -10.0,
    "dominance": 0.0
  },
  "relationshipVectors": {
    "imperialFear": 30.0,
    "factionTrust": 60.0
  },
  "dialogueTreeBranches": [
    {
      "branchId": "dlg_elda_rumor",
      "triggerCondition": "always",
      "dialogueText": "Word travels upriver faster than my skiff. They say a Vicar carries grain without demanding tax receipts.",
      "availableResponses": [
        {
          "responseId": "resp_elda_thanks",
          "text": "We pass freely and leave full bellies behind us.",
          "targetPadDelta": { "pleasure": 10.0, "arousal": 0.0, "dominance": 5.0 },
          "nextQuestState": "",
          "nextBranchId": ""
        }
      ]
    }
  ]
}
```

---

### 5.4 5-Act Quest Specification

#### Quest Record: `quest_kaelen_grain_crisis`
```json
{
  "questId": "quest_kaelen_grain_crisis",
  "questTitle": "The Mill and the Measure",
  "npcId": "npc_anchor_kael_01",
  "targetBiome": "The Grasslands",
  "currentAct": "Friction",
  "actCompletionFlags": {
    "Act_I_Friction": false,
    "Act_II_Ripple": false,
    "Act_III_Crisis": false,
    "Act_IV_Aftermath": false,
    "Act_V_Legacy": false
  },
  "acts": [
    {
      "actId": "act_1_friction",
      "act": "Friction",
      "title": "Act I: Stalled Millstones",
      "description": "Imperial grain seizures have halted Kaelen's mill. Deliver 10 kg of Golden Barley to resume flour grinding for the settlement.",
      "completionFlag": "Act_I_Friction",
      "isCompleted": false,
      "reward": {
        "insightPoints": 25,
        "padDelta": { "pleasure": 20.0, "arousal": -15.0, "dominance": 10.0 },
        "itemRewards": {},
        "unlockedBoon": ""
      }
    },
    {
      "actId": "act_2_ripple",
      "act": "Ripple",
      "title": "Act II: Bitter Measures",
      "description": "Traditionalist elders suspect Kaelen of hoarding imperial favor after receiving relief grain. Mediate between the elders and Kaelen.",
      "completionFlag": "Act_II_Ripple",
      "isCompleted": false,
      "reward": {
        "insightPoints": 35,
        "padDelta": { "pleasure": 15.0, "arousal": -10.0, "dominance": 15.0 },
        "itemRewards": {},
        "unlockedBoon": ""
      }
    },
    {
      "actId": "act_3_crisis",
      "act": "Crisis",
      "title": "Act III: Midnight Inspection",
      "description": "An imperial tax inspector arrives to confiscate the remaining flour while angry villagers arm themselves. De-escalate the standoff without violence.",
      "completionFlag": "Act_III_Crisis",
      "isCompleted": false,
      "reward": {
        "insightPoints": 50,
        "padDelta": { "pleasure": 30.0, "arousal": -30.0, "dominance": 25.0 },
        "itemRewards": { "item_imperial_ink": 1 },
        "unlockedBoon": ""
      }
    },
    {
      "actId": "act_4_aftermath",
      "act": "Aftermath",
      "title": "Act IV: Shared Hearth",
      "description": "Help the village establish an autonomous communal granary so future audits leave no one hungry.",
      "completionFlag": "Act_IV_Aftermath",
      "isCompleted": false,
      "reward": {
        "insightPoints": 40,
        "padDelta": { "pleasure": 25.0, "arousal": -10.0, "dominance": 20.0 },
        "itemRewards": {},
        "unlockedBoon": ""
      }
    },
    {
      "actId": "act_5_legacy",
      "act": "Legacy",
      "title": "Act V: The Unbroken Millstone",
      "description": "The settlement achieves economic self-reliance. Kaelen's mill now serves all neighboring roads freely.",
      "completionFlag": "Act_V_Legacy",
      "isCompleted": false,
      "reward": {
        "insightPoints": 100,
        "padDelta": { "pleasure": 40.0, "arousal": -20.0, "dominance": 30.0 },
        "itemRewards": {},
        "unlockedBoon": "Boon_Free_Village_Smithy_Repairs"
      }
    }
  ]
}
```

---

### 5.5 7 Biomes Matrix Catalog Specification

| # | Biome ID | Name | Associated Spire | Spoilage Mult | Stamina Mult | Speed Mod | Hazards & Weather Patterns | Spire Status |
|---|----------|------|------------------|---------------|--------------|-----------|----------------------------|--------------|
| 1 | `biome_grasslands` | The Grasslands | Verdant Spire | 1.0x | 1.0x | 1.00x | Mild weather, rolling plains. Clear (70%), Rain (25%), Storm (5%). | Uncalibrated (`false`) |
| 2 | `biome_coast` | The Coast | Tidal Spire | 1.5x | 1.1x | 0.90x | Heavy fog, salt spray. Fog (40%), Rain (40%), Clear (20%). | Uncalibrated (`false`) |
| 3 | `biome_forest` | The Forest | Sylvan Spire | 1.1x | 1.2x | 0.85x | Dense canopy, root obstacles. Overcast (50%), Rain (30%), Clear (20%). | Uncalibrated (`false`) |
| 4 | `biome_mountains` | The Mountains | Frost Spire | 0.2x | 2.0x | 0.70x | Freezing elevations, blizzards. Freezing (50%), Blizzard (30%), Clear (20%). | Uncalibrated (`false`) |
| 5 | `biome_swamp` | The Swamp | Mire Spire | 1.5x | 1.5x | 0.65x | Mud, bog mist, humidity. Swamp Mist (45%), Heavy Rain (45%), Clear (10%). | Uncalibrated (`false`) |
| 6 | `biome_desert` | The Desert | Sun Spire | 1.5x | 1.8x | 0.80x | Sun dunes, heat fatigue, sandstorms. Heat (60%), Sandstorm (30%), Clear (10%). | Uncalibrated (`false`) |
| 7 | `biome_jungle` | The Jungle | Canopy Spire | 1.6x | 1.6x | 0.75x | Humid rain-canopies, poisonous flora. Tropical Rain (50%), Humid Heat (40%), Clear (10%). | Uncalibrated (`false`) |

---

### 5.6 DataManager Implementation Specification
The `DataManager` class resides in `InkAndAsh.Data` and exposes:
- **Methods**:
  - `void LoadAllCatalogs()`: Loads and validates all catalogs from disk.
  - `bool LoadItems(string jsonContent)`: Populates item registry.
  - `bool LoadNpcs(string jsonContent)`: Populates NPC registry.
  - `bool LoadQuests(string jsonContent)`: Populates Quest registry.
  - `bool LoadBiomes(string jsonContent)`: Populates Biome registry.
  - `ItemData GetItem(string id)`: Zero-allocation dictionary lookup.
  - `NpcData GetNpc(string id)`: Zero-allocation dictionary lookup.
  - `QuestData GetQuest(string id)`: Zero-allocation dictionary lookup.
  - `BiomeData GetBiome(string id)`: Zero-allocation dictionary lookup.
- **Error Handling**:
  - Catches `JsonException` and `IOException` without throwing upwards.
  - Logs `Debug.LogError($"[DataManager] Error loading catalog: {ex.Message}")`.
  - Reverts to empty dictionary so callers never receive null pointers.

---

## 6. Deep Dive: R5 - Git Repository & GitHub Setup

### 6.1 Unity-Compliant `.gitignore` Specification
The existing `.gitignore` at the project root must be maintained and verified to cover all Unity 6 standard ignores:
```gitignore
# Unity standard ignore
/[Ll]ibrary/
/[Tt]emp/
/[Oo]bj/
/[Bb]uild/
/[Bb]uilds/
/[Ll]ogs/
/[Uu]ser[Ss]ettings/
/[Mm]emoryCaptures/

# Asset meta data should only be ignored if the corresponding asset is also ignored
!/[Aa]ssets/**/*.meta

# Visual Studio / Rider / IDE cache
.vs/
.idea/
.gradle/
ExportedObj/
.consulo/
*.csproj
*.unityproj
*.sln
*.suo
*.tmp
*.user
*.userprefs
*.pidb
*.booproj
*.svd
*.pdb
*.mdb
*.opendb
*.VC.db

# Unity3D generated meta files
*.pidb.meta
*.pdb.meta
*.mdb.meta
sysinfo.txt

# Builds
*.apk
*.aab
*.unitypackage
*.app

# OS generated
.DS_Store
Thumbs.db
```

### 6.2 Git Repository State & Repair Procedure
- **Observation:** `git status` reports `fatal: not a git repository`. Inspection of `.git` reveals an incomplete directory with only `HEAD` and `config` files present.
- **Repair Command Sequence:**
  ```powershell
  # 1. Initialize git safely (re-creates HEAD, refs, and objects)
  git init

  # 2. Set default branch to main
  git branch -M main

  # 3. Stage initial project assets, configurations, and documentation
  git add .gitignore
  git add "Ink and Ash.md"
  git add agents.md
  git add specs.md
  git add Packages/
  git add ProjectSettings/
  git add Assets/

  # 4. Create initial commit
  git commit -m "feat: initial project scaffold for The Ink and the Ash (Unity 6 URP)"
  ```

### 6.3 GitHub CLI Creation & Push Protocol
- **Account Verification:** `gh auth status` verifies active login under `miguelp001` with `repo` scope and SSH protocol.
- **CLI Commands:**
  ```powershell
  # Create public repository on GitHub and push local main branch
  gh repo create miguelp001/ink-and-ash --public --source=. --remote=origin --push
  ```
- **Fallback / Idempotency Handling:**
  If the remote repository already exists:
  ```powershell
  # Check existence
  gh repo view miguelp001/ink-and-ash

  # If exists, link remote and push
  git remote add origin git@github.com:miguelp001/ink-and-ash.git
  git push -u origin main
  ```

---

## 7. Acceptance Criteria Verification Matrix

| Requirement | Acceptance Criteria | Verification Method |
|-------------|---------------------|---------------------|
| R1 Folder Structure | Assets/, Packages/, ProjectSettings/ exist with standard subfolders | Verify directory listing with PowerShell `Test-Path` |
| R1 Assembly Definitions | 7 system .asmdef files compile without circular references | Inspect each .asmdef references array; build via `dotnet build` or Unity |
| R1 Packages Manifest | `com.unity.nuget.newtonsoft-json: 3.2.2` present in Packages/manifest.json | Inspect `Packages/manifest.json` |
| R2 Item Catalog | 16 unique items in `Assets/Data/Items/items.json` with valid weights, decay, and biome origins | Unit test: `DataManager.LoadItems()` parses exactly 16 items without error |
| R2 NPC Catalog | 3+ sample NPCs in `Assets/Data/NPCs/npcs.json` with valid PAD [-100, 100] and 6 canonical factions | Unit test: `DataManager.LoadNpcs()` verifies PAD validity and faction enums |
| R2 Quest Catalog | 1+ sample quest in `Assets/Data/Quests/quests.json` with 5-Act state structure | Unit test: `DataManager.LoadQuests()` verifies 5 acts present per quest |
| R2 Biome Catalog | 7 biomes in `Assets/Data/Biomes/biomes.json` matching GDD spoilage and stamina multipliers | Unit test: `DataManager.LoadBiomes()` verifies 7 biomes loaded |
| R2 DataManager | Runtime loading and dictionary retrieval without exceptions | EditMode unit test calling `DataManager` methods |
| R5 Git Setup | Repository initialized, .gitignore excludes Library/Temp, commit created | `git log -n 1` shows initial commit |
| R5 GitHub Push | `miguelp001/ink-and-ash` public repo exists on GitHub and holds pushed commit | `gh repo view miguelp001/ink-and-ash` succeeds |
