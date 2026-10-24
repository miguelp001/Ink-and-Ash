# Comprehensive Project Environment & Architecture Survey Report
**Project:** The Ink and the Ash  
**Working Directory:** `d:\Unity Projects\Ink and Ash`  
**Surveyed By:** `teamwork_preview_explorer` (Survey Explorer 1)  
**Date:** 2026-10-24  

---

## 1. Executive Summary

This survey provides a complete, factual, and verified assessment of the project filesystem, development environment, tooling, Unity 6 configuration, and Git repository state for *The Ink and the Ash*.

### Key Findings
1. **Target Working Directory (`d:\Unity Projects\Ink and Ash`):** Contains master design documents (`Ink and Ash.md`, `specs.md`, `agents.md`), an agent coordination tree (`.agents`), a basic 50-line `.gitignore`, and an incomplete/broken `.git` directory (only contains `config` and `HEAD`, lacking `refs/` and `objects/`). `Assets/`, `Packages/`, and `ProjectSettings/` do not yet exist in this root directory.
2. **Adjacent Project Discovery (`D:\Unity Projects\my project\Ink and Ash\Ink and Ash`):** A fully scaffolded Unity 6 project was created and opened earlier today (active at 3:53 PM). It contains complete `ProjectSettings/`, `Packages/manifest.json`, and imported asset packages (`Assets/Synty`, `Assets/Universal Sound FX`, `Assets/Quibli`, `Assets/Procedural Worlds`, `Assets/Plugins/Pixel Crushers`).
3. **Remote GitHub Repository (`miguelp001/Ink-and-Ash`):** Public repository is already created on GitHub under the authenticated account (`miguelp001`). It has 1 commit (`ba198e12a2dc3c02c423d8bb320cfaeb84c89849`, "Initial check-in", 2026-10-24T01:20:11Z) containing baseline Unity 6 configuration (`ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json`, etc.).
4. **Toolchain Availability:**
   - Unity Editor: Unity 6 version **`6000.6.4f1`** is installed at `C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe`.
   - .NET SDK: .NET SDK **`11.0.100-rc.1`** and **`9.0.318`** installed with .NET runtimes 8.0, 9.0, 10.0, and 11.0.
   - Git: Version **`2.55.0.windows.3`** installed.
   - GitHub CLI (`gh`): Version **`2.102.0`** authenticated as **`miguelp001`** with `repo` scopes.
5. **Build and Test Verification:**
   - Legacy Unity-generated `.csproj` files target `.NETFramework,Version=v4.7.1` which is not installed on this machine, causing legacy `dotnet build` / `MSBuild` to fail with error `MSB3644`.
   - Modern SDK-style `.csproj` projects (`<Project Sdk="Microsoft.NET.Sdk">`) targeting `net8.0` / `netstandard2.1` build cleanly and run with `dotnet test`.
   - Unity Editor headless test runner (`Unity.exe -runTests -batchmode ...`) is also fully supported.

---

## 2. Target Workspace File & Directory Structure

Target Root: `d:\Unity Projects\Ink and Ash`

```
d:\Unity Projects\Ink and Ash\
├── .agents/
│   └── teamwork/
│       ├── ORIGINAL_REQUEST.md          (10,260 bytes, detailed prompt)
│       ├── orchestrator_1/              (Orchestrator workspace)
│       ├── explorer_survey_1/           (Survey Explorer 1 workspace - this agent)
│       ├── sentinel_1/                  (Sentinel workspace)
│       ├── spec_miner_survey_2/         (Spec Miner 2 workspace)
│       └── spec_miner_survey_3/         (Spec Miner 3 workspace)
├── .git/                                (Broken repository structure)
│   ├── config                           (130 bytes, standard git core config)
│   └── HEAD                             (21 bytes, "ref: refs/heads/main\n")
├── .gitignore                           (585 bytes, 50 lines)
├── agents.md                            (3,460 bytes, 42 lines, agent rules)
├── Ink and Ash.md                       (52,502 bytes, 546 lines, master GDD)
└── specs.md                             (3,126 bytes, 36 lines, technical spec)
```

### Filesystem Observations
- `Assets/` directory: **Absent**
- `Packages/` directory: **Absent**
- `ProjectSettings/` directory: **Absent**
- `Assets/Scripts/` directory: **Absent**
- Assembly definition files (`*.asmdef`): **None present**
- C# source files (`*.cs`): **None present**

---

## 3. Unity Project Version, Packages, and Codebase Analysis

### 3.1 Unity Version
- **Target Unity Version:** `6000.6.4f1` (Unity 6)
- **Editor Binary:** `C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe`
- **Revision:** `12bfff696524` (recorded in remote `ProjectVersion.txt`)

### 3.2 Package Manifest Analysis (`Packages/manifest.json`)
From the baseline manifest in the GitHub remote and adjacent Unity project:
```json
{
  "dependencies": {
    "com.unity.ai.assistant": "2.21.0-pre.1",
    "com.unity.ai.inference": "2.6.1",
    "com.unity.ai.navigation": "2.0.14",
    "com.unity.collab-proxy": "2.13.6",
    "com.unity.ide.rider": "3.0.40",
    "com.unity.ide.visualstudio": "2.0.27",
    "com.unity.inputsystem": "1.20.0",
    "com.unity.pipeline": "0.9.1-exp.1",
    "com.unity.render-pipelines.universal": "17.7.0",
    "com.unity.test-framework": "1.8.0",
    "com.unity.timeline": "6.6.0",
    "com.unity.ugui": "2.6.0",
    ... (standard unity modules)
  }
}
```
**Status against R1 requirements:**
- Universal Render Pipeline (`com.unity.render-pipelines.universal: 17.7.0`): **Present in baseline**
- Input System (`com.unity.inputsystem: 1.20.0`): **Present in baseline**
- Test Framework (`com.unity.test-framework: 1.8.0`): **Present in baseline**
- Newtonsoft JSON (`com.unity.nuget.newtonsoft-json`): **MISSING**. Must be added to `Packages/manifest.json` per R1.

### 3.3 Assembly Definitions (`Assets/Scripts/*.asmdef`)
None of the seven gameplay assembly definitions exist yet. Per R1, the following must be created:
1. `InkAndAsh.Core.asmdef` (`Assets/Scripts/Core/`)
2. `InkAndAsh.Data.asmdef` (`Assets/Scripts/Data/`)
3. `InkAndAsh.Combat.asmdef` (`Assets/Scripts/Combat/`)
4. `InkAndAsh.Economy.asmdef` (`Assets/Scripts/Economy/`)
5. `InkAndAsh.World.asmdef` (`Assets/Scripts/World/`)
6. `InkAndAsh.Narrative.asmdef` (`Assets/Scripts/Narrative/`)
7. `InkAndAsh.UI.asmdef` (`Assets/Scripts/UI/`)
plus test assembly:
8. `InkAndAsh.Tests.EditMode.asmdef` (`Assets/Tests/EditMode/` or `Assets/Scripts/Tests/`)

### 3.4 Existing Codebase
- In `d:\Unity Projects\Ink and Ash`: Exactly **0 C# source files**.
- In adjacent `D:\Unity Projects\my project\Ink and Ash\Ink and Ash`: Contains vendor code from imported packages (Pixel Crushers Dialogue System runtime & editor scripts, Quibli shaders/utilities, Procedural Worlds framework, and Unity tutorial readme scripts). No gameplay or game architecture scripts exist.

---

## 4. Command Line Environment & Toolchain Assessment

### 4.1 Tool Versions and Verification
| Tool | Installed Version | Location / Details | Status |
|---|---|---|---|
| **Git** | `2.55.0.windows.3` | `C:\Program Files\Git\cmd\git.exe` | Functional |
| **GitHub CLI (`gh`)** | `2.102.0` (2026-09-30) | Logged into `miguelp001` via SSH keyring with `repo`, `admin:public_key`, `gist`, `read:org` scopes | Authenticated & Ready |
| **.NET SDK** | `11.0.100-rc.1.26425.128` (default), `9.0.318` | `C:\Program Files\dotnet\sdk` | Functional |
| **.NET Runtimes** | `Microsoft.NETCore.App` (8.0.31, 9.0.20, 10.0.12, 11.0.0-rc.1) | `C:\Program Files\dotnet\shared` | Functional |
| **Unity Editor** | `6000.6.4f1` | `C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe` | Verified batchmode |
| **VS MSBuild** | `17.14.60.43110` (VS 2022 Community) | `C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe` | Functional for modern SDKs |
| **unity-mcp** | MCP server available | Requires active Unity Editor session; returned `Unity not detected` | Ready when editor opens |

### 4.2 Build and Test Execution Analysis
Testing command execution yielded critical insight into project testability:

1. **Legacy .NET Framework Issue with `dotnet build`:**
   Running `dotnet build` or VS MSBuild on Unity's generated `.sln` or `.csproj` files fails with:
   `error MSB3644: The reference assemblies for .NETFramework,Version=v4.7.1 were not found.`
   This occurs because Unity's legacy Visual Studio integration emits non-SDK `.csproj` files targeting .NET Framework 4.7.1, but this machine only has modern .NET Core / .NET 8/9/11 installed.

2. **Zero-Friction Testing Strategy:**
   - **Standalone SDK-Style Test Project:** Creating a lightweight SDK-style `.csproj` (e.g. `tests/InkAndAsh.Tests.Standalone/InkAndAsh.Tests.csproj` or `Assets/Scripts/Tests/InkAndAsh.Tests.csproj`) targeting `net8.0` with NUnit:
     ```xml
     <Project Sdk="Microsoft.NET.Sdk">
       <PropertyGroup>
         <TargetFramework>net8.0</TargetFramework>
         <ImplicitUsings>enable</ImplicitUsings>
         <Nullable>enable</Nullable>
       </PropertyGroup>
       <ItemGroup>
         <PackageReference Include="NUnit" Version="4.2.2" />
         <PackageReference Include="NUnit3TestAdapter" Version="4.6.0" />
         <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
         <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
       </ItemGroup>
       <ItemGroup>
         <Reference Include="UnityEngine">
           <HintPath>C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Data\Managed\UnityEngine.dll</HintPath>
         </Reference>
       </ItemGroup>
     </Project>
     ```
     This allows running unit tests via standard `dotnet test` in ~1.5 seconds directly from PowerShell without starting Unity.
   - **Unity Test Framework (UTF) EditMode Tests:**
     Unity 6 EditMode tests can also be executed in batchmode:
     ```powershell
     & "C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe" -batchmode -quit -projectPath "d:\Unity Projects\Ink and Ash" -runTests -testPlatform EditMode -testResults "TestResults.xml" -logFile "unity_test.log"
     ```

---

## 5. Git Status Assessment

### 5.1 Local Git Status in `d:\Unity Projects\Ink and Ash`
- **State:** Incomplete / Damaged `.git` directory.
  - Contains only `config` and `HEAD`.
  - Missing `refs/` directory and `objects/` directory.
  - Command `git status` fails with:
    `fatal: not a git repository (or any of the parent directories): .git`
- **Remotes:** No remotes configured in local `.git/config`.
- **Commits:** No local commits.
- **`.gitignore`:** 50 lines (585 bytes). Standard basic Unity ignore rules.

### 5.2 Remote GitHub Repository Status
- **Repository:** `https://github.com/miguelp001/Ink-and-Ash`
- **Visibility:** Public
- **Owner:** `miguelp001`
- **Default Branch:** `main`
- **Commit History:** 1 commit:
  - Commit SHA: `ba198e12a2dc3c02c423d8bb320cfaeb84c89849`
  - Author: `miguelp001 <33874625+miguelp001@users.noreply.github.com>`
  - Date: `2026-10-24T01:20:11Z`
  - Message: `"Initial check-in"`
- **Contents of Remote Commit:**
  - `.agents/`
  - `.gitattributes` (171 lines)
  - `.gitignore` (114 lines, full GitHub Unity template)
  - `.vsconfig`
  - `Assets/` (Input actions, URP settings, TutorialInfo, Scenes/SampleScene)
  - `Packages/` (`manifest.json`, `packages-lock.json`)
  - `ProjectSettings/` (full Unity 6 URP project configuration)
  - `Ink and Ash.md`, `agents.md`, `specs.md`

### 5.3 Git Repair and Sync Procedure
To restore the local Git repository cleanly at `d:\Unity Projects\Ink and Ash`:
1. Re-initialize Git:
   ```powershell
   git init -b main
   ```
2. Attach remote:
   ```powershell
   git remote add origin https://github.com/miguelp001/Ink-and-Ash.git
   ```
3. Fetch / sync or commit files.

---

## 6. Recommendations for Project Orchestrator

1. **Project Directory Scaffolding (Milestone 1):**
   - Synchronize or generate standard Unity 6 `ProjectSettings/` (configuring Linear Color Space, URP, ASTC texture compression for Android, BC7 for Windows, .NET Standard 2.1 API compatibility).
   - Create `Packages/manifest.json` including `com.unity.nuget.newtonsoft-json` (version `3.2.1` or latest compatible).
   - Create the 7 gameplay Assembly Definition files under `Assets/Scripts/` with explicit inter-assembly dependencies (Core, Data, Combat, Economy, World, Narrative, UI) and zero circular references.
   - Use the comprehensive 114-line `.gitignore` template matching the remote repository.

2. **Data & JSON Catalogs (Milestone 2):**
   - Populate `Assets/Data/Items/` with the exact 16 items specified in GDD.
   - Populate `Assets/Data/NPCs/` with 3 tiered sample NPC records (Tier 1 Core, Tier 2 Village Anchor, Tier 3 Background) including PAD scores (-100 to +100) and faction vectors.
   - Populate `Assets/Data/Quests/` with 5-Act quest state structure (Friction, Ripple, Crisis, Aftermath, Legacy).
   - Populate `Assets/Data/Biomes/` with 7 biomes and Spire calibration metadata.

3. **Pure C# Logic Systems (Milestone 3):**
   - Keep CTB combat logic, TimeManager, Caravan inventory, and Weather math decoupled from MonoBehaviours to ensure clean unit testing without Unity runtime dependencies.

4. **Testing Architecture (Milestone 4):**
   - Set up both Unity Test Framework EditMode tests (`InkAndAsh.Tests.EditMode.asmdef`) and an SDK-style test project for instant CLI execution with `dotnet test`.

5. **Git Push (Milestone 5):**
   - Repair local `.git`, link to `origin https://github.com/miguelp001/Ink-and-Ash.git`, commit all scaffolded assets and code, and push to `main`.
