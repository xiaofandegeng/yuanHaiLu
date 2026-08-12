# Quest Runtime and Persistence Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a reliable quest runtime that resolves complete templates by ID, owns independent objective progress, reports real gameplay events, grants rewards once, and round-trips active quests through v3 saves.

**Architecture:** `QuestDatabase` supplies immutable templates; `ActiveQuest` deep-copies runtime objectives while retaining template display data. `QuestManager` remains the only progression and reward authority, `QuestGiver` coordinates NPC dialogue with accept/submit actions, and gameplay components report successful actions through `UpdateObjective`. `SaveManager` stores stable IDs and primitive progress records and migrates v2 saves to empty active-quest state.

**Tech Stack:** Unity `6000.4.10f1`, C#, built-in 2D renderer, Unity Test Framework `1.6.0`, NUnit EditMode and PlayMode tests.

## Global Constraints

- Work on the existing `main` branch and preserve unrelated user changes.
- Keep `M01_01` through `M01_05` as the pre-Tianshu Yanliu chapter IDs.
- Keep the default protagonist name `凌霜`; character creation is outside this phase.
- Continue using Unity built-in 2D rendering; do not add URP or third-party runtime dependencies.
- Keep v2 player/inventory/martial-arts migration intact while adding v3 quest data.
- Do not build the full Yanliu scene chain, Tianshu, reputation, companions, chronicles, or history quizzes in this phase.
- Every new Unity asset must include its `.meta` file before commit.

---

## File Map

- Create `Assets/Scripts/System/QuestDatabase.cs`: code-backed M01 templates plus Resources override support.
- Create `Assets/Scripts/System/QuestGiver.cs`: NPC quest dialogue and accept/submit coordinator.
- Create `Assets/Scripts/System/QuestTarget.cs`: enemy death objective reporter.
- Modify `Assets/Scripts/System/QuestManager.cs`: runtime objectives, lookup, persistence DTOs, restore, and rewards.
- Modify `Assets/Scripts/System/SaveManager.cs`: v3 quest payload and v2 migration.
- Modify `Assets/Scripts/Character/NPCBase.cs`: delegate configured quest interactions to `QuestGiver`.
- Modify `Assets/Scripts/Character/MartialArtsSystem.cs`: report first-time skill learning.
- Modify `Assets/Scripts/Map/AreaTrigger.cs`: report configured area targets.
- Modify `Assets/Scripts/Map/ItemPickup.cs`: report only fully successful pickup quantities.
- Modify `Assets/Scripts/System/InventoryManager.cs`: emit the requested successful add amount.
- Modify `Assets/Scripts/UI/QuestUI.cs`: render runtime objectives.
- Create `Assets/Tests/EditMode/QuestRuntimeTests.cs`, `QuestPersistenceTests.cs`, and `QuestIntegrationTests.cs`.

---

### Task 1: Quest Templates and Independent Runtime State

**Files:**
- Create: `Assets/Scripts/System/QuestDatabase.cs`
- Modify: `Assets/Scripts/System/QuestManager.cs`
- Modify: `Assets/Scripts/UI/QuestUI.cs`
- Create: `Assets/Tests/EditMode/QuestRuntimeTests.cs`

**Interfaces:**
- Produces: `QuestDatabase.Get(string id)`, `QuestDatabase.AllQuests`, `ActiveQuest.Objectives`, `QuestManager.CanAcceptQuestById(string id)`, `QuestManager.AcceptQuestById(string id)`.
- Consumes: existing `QuestData`, completed IDs, player level, and `Resources/Quests` overrides.

- [ ] **Step 1: Write the failing database and clone tests**

```csharp
[Test]
public void QuestDatabaseResolvesCompleteM01Template()
{
    QuestData quest = QuestDatabase.Get("M01_01");
    Assert.That(quest, Is.Not.Null);
    Assert.That(quest.questName, Is.EqualTo("初到烟柳镇"));
    Assert.That(quest.objectives, Has.Length.EqualTo(3));
    Assert.That(quest.unlockQuestIds, Is.EqualTo(new[] { "M01_02" }));
}

[Test]
public void AcceptedQuestOwnsProgressWithoutMutatingTemplate()
{
    var manager = CreateQuestManager();
    Assert.That(manager.AcceptQuestById("M01_01"), Is.True);
    manager.UpdateObjective(QuestObjective.ObjectiveType.ReachArea, "yanliu_inn");
    Assert.That(manager.GetActiveQuest("M01_01").Objectives[0].currentAmount, Is.EqualTo(1));
    Assert.That(QuestDatabase.Get("M01_01").objectives[0].currentAmount, Is.Zero);
}
```

Also assert an unknown ID returns `false` and leaves the active list empty.

- [ ] **Step 2: Run the focused test and verify RED**

```bash
/Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/lhw/code/yuanHaiLu -runTests -testPlatform EditMode -testFilter YuanHaiLu.Tests.EditMode.QuestRuntimeTests -testResults /tmp/yuanHaiLu-quest-runtime-red.xml -logFile /tmp/yuanHaiLu-quest-runtime-red.log
```

Expected: compilation/test failure because the database and runtime objective property do not exist.

- [ ] **Step 3: Implement the code-backed quest database**

```csharp
public static class QuestDatabase
{
    public static IReadOnlyDictionary<string, QuestData> AllQuests { get; }
    public static QuestData Get(string id);
}
```

Create all five M01 templates. Use stable targets:

```text
M01_01: yanliu_inn, innkeeper_zhao, drunk_old_man
M01_02: liu_apothecary, su_wanqing, herb_medicinal
M01_03: teacher_chen_intro, quest_old_book, teacher_chen_return
M01_04: north_mountain, bandit, boss_heifeng
M01_05: su_wanqing_farewell, innkeeper_route, yanliu_south_exit
```

Load `Resources.LoadAll<QuestData>("Quests")` after code templates and replace matching IDs only when `questId` is non-empty.

- [ ] **Step 4: Implement runtime cloning and ID acceptance**

Add `public QuestObjective[] Objectives { get; }` to `ActiveQuest`. Deep-copy every objective field in the constructor. Move completion checks, `UpdateObjective`, and `QuestUI` rendering to the runtime array. Reject null/empty quest IDs and templates without IDs. Zero-objective tasks become `Completable` immediately.

- [ ] **Step 5: Run the focused test and verify GREEN**

Repeat Step 2 using `green` output names. Expected: all runtime tests pass and templates retain zero progress.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/System/QuestDatabase.cs Assets/Scripts/System/QuestDatabase.cs.meta Assets/Scripts/System/QuestManager.cs Assets/Scripts/UI/QuestUI.cs Assets/Tests/EditMode/QuestRuntimeTests.cs Assets/Tests/EditMode/QuestRuntimeTests.cs.meta
git commit -m "feat: add stable quest templates and runtime state"
```

---

### Task 2: v3 Active Quest Persistence

**Files:**
- Modify: `Assets/Scripts/System/QuestManager.cs`
- Modify: `Assets/Scripts/System/SaveManager.cs`
- Create: `Assets/Tests/EditMode/QuestPersistenceTests.cs`
- Modify: `Assets/Tests/EditMode/PersistenceTests.cs`

**Interfaces:**
- Produces: `QuestObjectiveSaveData`, `ActiveQuestSaveData`, `QuestSaveData`, `QuestManager.GetSaveData()`, `QuestManager.LoadSaveData(QuestSaveData data)`.
- Consumes: `QuestDatabase.Get`, runtime objectives, existing v2 `SaveData`.

- [ ] **Step 1: Write failing v3 round-trip tests**

```csharp
QuestManager.QuestSaveData snapshot = manager.GetSaveData();
string json = JsonUtility.ToJson(snapshot);
manager.ResetForNewGame();
manager.LoadSaveData(JsonUtility.FromJson<QuestManager.QuestSaveData>(json));
ActiveQuest restored = manager.GetActiveQuest("M01_01");
Assert.That(restored.Objectives[0].currentAmount, Is.EqualTo(1));
Assert.That(restored.Objectives[1].currentAmount, Is.Zero);
Assert.That(restored.acceptTime, Is.EqualTo(original.acceptTime));
```

Add cases for duplicate IDs, unknown templates, progress clamping/matching by `(type, targetId)`, v2 empty-active migration, and v3 JSON completed IDs.

- [ ] **Step 2: Run persistence tests and verify RED**

Run the Task 1 Unity command with filter `YuanHaiLu.Tests.EditMode.QuestPersistenceTests` and persistence-specific `/tmp` outputs. Expected: missing DTO and manager methods.

- [ ] **Step 3: Implement primitive save DTOs and restore**

```csharp
[Serializable] public class QuestObjectiveSaveData
{
    public QuestObjective.ObjectiveType type;
    public string targetId;
    public int currentAmount;
}
[Serializable] public class ActiveQuestSaveData
{
    public string questId;
    public ActiveQuest.QuestState state;
    public long acceptTimeBinary;
    public QuestObjectiveSaveData[] objectives;
}
[Serializable] public class QuestSaveData
{
    public ActiveQuestSaveData[] activeQuests;
    public string[] completedQuestIds;
}
```

Clear old state first. Restore from `QuestDatabase`, ignore duplicates/unknown IDs, map targets by type plus ID, clamp progress, and derive objective completion and quest state. Completed records go only into `completedQuestIds`.

- [ ] **Step 4: Upgrade SaveManager to v3**

Set `CURRENT_SAVE_VERSION = 3`, add `public QuestManager.QuestSaveData quests;`, write it in `SaveGame`, and use:

```csharp
if (saveData.saveVersion >= 3 && saveData.quests != null)
    QuestManager.Instance.LoadSaveData(saveData.quests);
else
    QuestManager.Instance.LoadCompletedQuests(saveData.completedQuests);
```

Do not change v2 base-stat, inventory, or martial-arts restoration order.

- [ ] **Step 5: Run `QuestPersistenceTests` and existing `PersistenceTests`**

Expected: both pass, including v2 and legacy-stat migration.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/System/QuestManager.cs Assets/Scripts/System/SaveManager.cs Assets/Tests/EditMode/QuestPersistenceTests.cs Assets/Tests/EditMode/QuestPersistenceTests.cs.meta Assets/Tests/EditMode/PersistenceTests.cs
git commit -m "feat: persist active quests in v3 saves"
```

---

### Task 3: Rewards and Gameplay Objective Sources

**Files:**
- Create: `Assets/Scripts/System/QuestTarget.cs`
- Modify: `Assets/Scripts/System/QuestManager.cs`
- Modify: `Assets/Scripts/Character/MartialArtsSystem.cs`
- Modify: `Assets/Scripts/Map/AreaTrigger.cs`
- Modify: `Assets/Scripts/Map/ItemPickup.cs`
- Modify: `Assets/Scripts/System/InventoryManager.cs`
- Create: `Assets/Tests/EditMode/QuestIntegrationTests.cs`

**Interfaces:**
- Produces: `bool QuestManager.CompleteQuest(string questId)`, `QuestTarget.ReportDefeat()`, `AreaTrigger.ReportAreaReached()`.
- Consumes: `UpdateObjective`, `CharacterStats.OnDeath`, `AddItem`, `LearnSkill`.

- [ ] **Step 1: Write failing integration tests**

Prove completion succeeds once, a second call returns false, and gold/experience/item/skill rewards do not repeat. Cover first versus duplicate skill learning, one death report, one configured area report, failed inventory add with no collection progress, and successful requested quantity reporting.

- [ ] **Step 2: Run `QuestIntegrationTests` and verify RED**

Use the focused Unity command with integration-specific result/log files. Expected: missing APIs and return values.

- [ ] **Step 3: Make reward settlement idempotent**

Change `CompleteQuest` to return `bool`. Record completion and remove the active quest before reward/event callbacks. Resolve `rewardSkillId` through `MartialSkillDatabase` and call the player's `MartialArtsSystem.LearnSkill`; warn without aborting other rewards when a dependency is missing.

- [ ] **Step 4: Implement reporters**

`QuestTarget` subscribes to sibling `CharacterStats.OnDeath`, accepts only `KillEnemy`/`DefeatBoss`, and reports once. `AreaTrigger` reports its non-empty `questTargetId` after valid player entry. `ItemPickup` reports only after a fully successful add. Preserve the original requested amount in `InventoryManager.AddItem` and emit it through `OnItemAdded`. `MartialArtsSystem.LearnSkill` reports only after first insertion.

- [ ] **Step 5: Run integration and runtime tests and verify GREEN**

Expected: both focused classes pass with no unexpected logs.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/System/QuestTarget.cs Assets/Scripts/System/QuestTarget.cs.meta Assets/Scripts/System/QuestManager.cs Assets/Scripts/Character/MartialArtsSystem.cs Assets/Scripts/Map/AreaTrigger.cs Assets/Scripts/Map/ItemPickup.cs Assets/Scripts/System/InventoryManager.cs Assets/Tests/EditMode/QuestIntegrationTests.cs Assets/Tests/EditMode/QuestIntegrationTests.cs.meta
git commit -m "feat: connect quest progress to gameplay events"
```

---

### Task 4: Quest Giver Dialogue Lifecycle

**Files:**
- Create: `Assets/Scripts/System/QuestGiver.cs`
- Modify: `Assets/Scripts/Character/NPCBase.cs`
- Modify: `Assets/Tests/EditMode/QuestIntegrationTests.cs`

**Interfaces:**
- Produces: `bool QuestGiver.TryHandleInteraction(GameObject player)` and internal `ResolvePendingInteraction()`.
- Consumes: `DialogueManager.OnDialogueEnd`, quest lookup, accept, progress, and completion APIs.

- [ ] **Step 1: Write failing lifecycle tests**

Verify these state transitions:

```text
available + canAccept → intro dialogue → accept once → optional talk progress
active → progress dialogue → talk progress once per interaction
completable + canComplete → completion dialogue → reward once
completed → post-completion dialogue without accepting or rewarding again
busy DialogueManager → no dialogue and no pending quest action
```

- [ ] **Step 2: Run integration tests and verify RED**

Expected: `QuestGiver` and NPC delegation do not exist.

- [ ] **Step 3: Implement QuestGiver as a non-IInteractable coordinator**

```csharp
public string questId;
public string interactionTargetId;
public bool canAcceptQuest = true;
public bool canCompleteQuest = true;
public string[] completedDialogue;
public bool TryHandleInteraction(GameObject player);
```

Choose template intro/progress/complete dialogue by state. Subscribe only after dialogue starts, unsubscribe before resolving, and clear pending state on disable/destroy. After acceptance, report the configured talk target; active interaction reports it once; completion calls `CompleteQuest` only when permitted.

- [ ] **Step 4: Delegate from NPCBase**

At the start of `NPCBase.OnInteract`, let a sibling `QuestGiver` handle the interaction. Fall back to existing dialogue when it returns false. `QuestGiver` must not implement `IInteractable`, preserving one interaction candidate per NPC.

- [ ] **Step 5: Run integration tests and verify GREEN**

Expected: all lifecycle cases pass and event subscriptions do not leak.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/System/QuestGiver.cs Assets/Scripts/System/QuestGiver.cs.meta Assets/Scripts/Character/NPCBase.cs Assets/Tests/EditMode/QuestIntegrationTests.cs
git commit -m "feat: add quest-aware npc interaction lifecycle"
```

---

### Task 5: Verification, Review, and Handoff

**Files:**
- Modify: `AGENTS.md`
- Modify: `README.md`
- Modify: `SETUP_GUIDE.md`
- Modify: `docs/superpowers/plans/2026-08-12-quest-runtime-persistence.md`

**Interfaces:**
- Consumes: all prior task outputs and phase-one acceptance criteria.
- Produces: verified phase-one baseline and updated project memory.

- [ ] **Step 1: Run all EditMode tests**

```bash
/Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/lhw/code/yuanHaiLu -runTests -testPlatform EditMode -testResults /tmp/yuanHaiLu-editmode-quest-phase1.xml -logFile /tmp/yuanHaiLu-editmode-quest-phase1.log
```

- [ ] **Step 2: Run all PlayMode tests**

```bash
/Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/lhw/code/yuanHaiLu -runTests -testPlatform PlayMode -testResults /tmp/yuanHaiLu-playmode-quest-phase1.xml -logFile /tmp/yuanHaiLu-playmode-quest-phase1.log
```

- [ ] **Step 3: Run batch compilation/import**

```bash
/Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/lhw/code/yuanHaiLu -quit -logFile /tmp/yuanHaiLu-compile-quest-phase1.log
```

Expected for Steps 1-3: exit 0, all tests pass, and no C# error/warning, compilation failure, or package error.

- [ ] **Step 4: Review the phase diff against `dd2657f`**

Check template immutability, event unsubscription, duplicate rewards, v2 migration, invalid input, Unity object lifetime, and scope. Resolve every high-priority finding and rerun affected tests.

- [ ] **Step 5: Perform Unity Play smoke QA**

From `MainMenu`, verify existing new game, movement, combat, NPC interaction and pause behavior. Use a configured quest test object to confirm accept, objective update, save, menu return, continue, and completion without Console errors.

- [ ] **Step 6: Update durable documentation**

Record v3 ordering, `QuestDatabase`, runtime cloning, source wiring, `QuestGiver`, exact test counts, QA evidence, and remaining phase-two content gap in `AGENTS.md`, `README.md`, and `SETUP_GUIDE.md`. Mark completed plan checkboxes.

- [ ] **Step 7: Run hygiene checks**

```bash
git diff --check
git status --short
git diff --stat dd2657f..HEAD
```

- [ ] **Step 8: Commit documentation**

```bash
git add AGENTS.md README.md SETUP_GUIDE.md docs/superpowers/plans/2026-08-12-quest-runtime-persistence.md
git commit -m "docs: hand off quest runtime phase one"
```
