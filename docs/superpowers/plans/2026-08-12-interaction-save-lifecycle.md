# Interaction, Save, and Scene Lifecycle Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 完成 K/E 交互、可靠的背包/装备/武学/已完成任务存档，并让新游戏、读档与场景切换使用互不覆盖的初始化流程。

**Architecture:** `GameManager` 持有显式场景进入模式，`SceneDirector` 只在新游戏模式初始化数据；`SaveManager` 使用单次具名场景回调并按固定顺序恢复基础属性与各子系统。`GlobalSystemsBootstrapper` 保证从主菜单进入 Demo 时完整单例集合已存在，交互组件通过幂等 `EnsureOn` 同时接入生成器和既有场景。

**Tech Stack:** Unity `6000.4.10f1`、C#、Unity Test Framework `1.6.0`、NUnit、Legacy Input Manager、PlayerPrefs + JsonUtility。

## Global Constraints

- 保持 Unity `6000.4.10f1`、内置 2D 渲染管线，不引入 URP。
- 系统层命名空间继续使用 `YuanHaiLu.GameSystem`。
- 不实现活跃任务、世界敌人、拾取物和区域标志持久化。
- 不改变 PlayerPrefs + JSON 存储介质，不新增第三方依赖。
- 现有未提交改动属于用户，实施时在其基础上增量修改，不回退或覆盖。
- 修改 `ProjectSettings/InputManager.asset` 后，最终人工验证前必须重启 Unity。
- 不推送、合并或发布。

## File Map

- Create `Assets/Tests/EditMode/YuanHaiLu.EditModeTests.asmdef`: EditMode 测试程序集。
- Create `Assets/Tests/EditMode/TestSceneFactory.cs`: 创建和清理真实 Unity 组件的测试工具。
- Create `Assets/Scripts/Properties/AssemblyInfo.cs`: 仅向 EditMode 测试程序集开放内部恢复入口。
- Create `Assets/Tests/EditMode/GameManagerEntryModeTests.cs`: 场景进入模式测试。
- Create `Assets/Tests/EditMode/PersistenceTests.cs`: 属性、背包、武学与任务恢复测试。
- Create `Assets/Tests/EditMode/InteractionTests.cs`: 交互组件幂等接入和事件触发条件测试。
- Create `Assets/Tests/EditMode/GlobalSystemsBootstrapperTests.cs`: 全局系统幂等创建测试。
- Create `Assets/Scripts/System/GlobalSystemsBootstrapper.cs`: 主菜单与场景共享的全局单例补全入口。
- Modify `Assets/Scripts/Core/GameManager.cs`: 场景进入模式和会话上下文。
- Modify `Assets/Scripts/Character/CharacterStats.cs`: 暴露基础属性并支持不治疗的装备重算。
- Modify `Assets/Scripts/Character/MartialArtsSystem.cs`: 健壮、替换式武学恢复。
- Modify `Assets/Scripts/Character/PlayerInteraction.cs`: 提供幂等接入 API 并保持最近目标策略。
- Modify `Assets/Scripts/System/InventoryManager.cs`: 合并代码物品库、健壮恢复、装备重算和新游戏重置。
- Modify `Assets/Scripts/System/QuestManager.cs`: 已完成任务恢复与新游戏重置。
- Modify `Assets/Scripts/System/SaveManager.cs`: 版本化数据、单次回调和固定恢复顺序。
- Modify `Assets/Scripts/Core/SceneBootstrapper.cs`: 使用统一系统补全和交互接入。
- Modify `Assets/Scripts/Map/SceneDirector.cs`: 既有场景交互保障和新游戏初始化门。
- Modify `Assets/Scripts/UI/MainMenu.cs`: 正确场景名、系统补全、按钮绑定及新游戏重置。
- Modify `Assets/Scripts/Editor/MainMenuSceneGenerator.cs`: 生成与运行时逻辑一致的主菜单。
- Modify `Assets/Scripts/Editor/DemoSceneGenerator.cs`: 使用幂等交互接入。
- Modify `Assets/Scenes/MainMenu.unity`: 序列化首场景名改为 `Demo_YanLiuTown`。
- Modify `ProjectSettings/InputManager.asset`: 保留 `Interact` 的 K/E 映射。
- Modify `AGENTS.md`, `README.md`, `SETUP_GUIDE.md`: 更新长期记忆、交接和使用说明。
- Modify `.zcode/plans/plan-sess_12dba3a0-9d1f-412c-9643-d44a8025eddb.md`: 记录本次实际实施状态。

---

### Task 1: 建立测试程序集和显式场景进入模式

**Files:**
- Create: `Assets/Tests/EditMode/YuanHaiLu.EditModeTests.asmdef`
- Create: `Assets/Tests/EditMode/TestSceneFactory.cs`
- Create: `Assets/Scripts/Properties/AssemblyInfo.cs`
- Create: `Assets/Tests/EditMode/GameManagerEntryModeTests.cs`
- Modify: `Assets/Scripts/Core/GameManager.cs`

**Interfaces:**
- Produces: `GameManager.SceneEntryMode`, `CurrentSceneEntryMode`, `BeginSceneEntry(SceneEntryMode)`, `CompleteSceneEntry()`, `ShouldInitializeNewGame`。
- Consumes: existing `GameManager.Instance` singleton lifecycle。

- [ ] **Step 1: 创建测试程序集和测试清理工具**

`YuanHaiLu.EditModeTests.asmdef` 使用：

```json
{
  "name": "YuanHaiLu.EditModeTests",
  "rootNamespace": "YuanHaiLu.Tests.EditMode",
  "references": ["YuanHaiLu"],
  "includePlatforms": ["Editor"],
  "optionalUnityReferences": ["TestAssemblies"]
}
```

`TestSceneFactory` 提供 `Create(string)` 和 `DestroyAll()`；`DestroyAll()` 对测试创建的根对象调用 `Object.DestroyImmediate`，不向生产类添加仅供测试使用的清理方法。

使用以下测试工具实现，确保每个测试都清理 GameObject 和单例自动属性的 backing field：

```csharp
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using YuanHaiLu.Character;
using YuanHaiLu.Core;
using YuanHaiLu.Dialogue;
using YuanHaiLu.GameSystem;

namespace YuanHaiLu.Tests.EditMode
{
    internal static class TestSceneFactory
    {
        private static readonly List<GameObject> Roots = new List<GameObject>();

        internal static GameObject Create(string name)
        {
            var gameObject = new GameObject(name);
            Roots.Add(gameObject);
            return gameObject;
        }

        internal static GameObject CreatePlayer()
        {
            var player = Create("Player");
            player.tag = "Player";
            player.AddComponent<SpriteRenderer>();
            player.AddComponent<Animator>();
            player.AddComponent<Rigidbody2D>();
            player.AddComponent<BoxCollider2D>();
            player.AddComponent<CharacterStats>();
            return player;
        }

        internal static void DestroyAll()
        {
            for (int i = Roots.Count - 1; i >= 0; i--)
            {
                if (Roots[i] != null)
                    Object.DestroyImmediate(Roots[i]);
            }
            Roots.Clear();

            ResetSingleton<GameManager>();
            ResetSingleton<SaveManager>();
            ResetSingleton<InventoryManager>();
            ResetSingleton<QuestManager>();
            ResetSingleton<GameTimeManager>();
            ResetSingleton<DialogueManager>();
        }

        private static void ResetSingleton<T>()
        {
            typeof(T).GetField("<Instance>k__BackingField",
                BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, null);
        }
    }
}
```

`Assets/Scripts/Properties/AssemblyInfo.cs` 内容为：

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("YuanHaiLu.EditModeTests")]
```

- [ ] **Step 2: 写入进入模式的失败测试**

测试真实 `GameManager` 组件：

```csharp
[Test]
public void LoadGameEntrySkipsNewGameInitializationUntilCompleted()
{
    var gameObject = TestSceneFactory.Create("GameManager");
    var manager = gameObject.AddComponent<GameManager>();

    manager.BeginSceneEntry(GameManager.SceneEntryMode.LoadGame);

    Assert.That(manager.ShouldInitializeNewGame, Is.False);
    manager.CompleteSceneEntry();
    Assert.That(manager.CurrentSceneEntryMode, Is.EqualTo(GameManager.SceneEntryMode.Active));
}
```

该测试捕获的回归是：读档场景被误判为新游戏，或完成读档后仍残留加载上下文。

- [ ] **Step 3: 运行测试并确认 RED**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/lhw/code/yuanHaiLu -runTests -testPlatform EditMode -testFilter YuanHaiLu.Tests.EditMode.GameManagerEntryModeTests -testResults /tmp/yuanHaiLu-editmode.xml -logFile /tmp/yuanHaiLu-editmode.log -quit
```

Expected: FAIL，原因是 `SceneEntryMode` 和相关 API 尚不存在。

- [ ] **Step 4: 实现最小进入模式**

在 `GameManager` 中加入：

```csharp
public enum SceneEntryMode
{
    NewGame,
    LoadGame,
    SceneTransition,
    Active
}

public SceneEntryMode CurrentSceneEntryMode { get; private set; } = SceneEntryMode.NewGame;
public bool ShouldInitializeNewGame => CurrentSceneEntryMode == SceneEntryMode.NewGame;

public void BeginSceneEntry(SceneEntryMode mode)
{
    CurrentSceneEntryMode = mode;
}

public void CompleteSceneEntry()
{
    CurrentSceneEntryMode = SceneEntryMode.Active;
}
```

`OnDestroy` 仅在 `Instance == this` 时清空单例，保证场景销毁后静态引用不悬空。

- [ ] **Step 5: 运行测试并确认 GREEN**

重复 Step 3 命令。Expected: 测试通过，XML 中 failure 数为 0。

- [ ] **Step 6: 提交该独立任务**

```bash
git add Assets/Tests/EditMode Assets/Scripts/Core/GameManager.cs
git commit -m "feat: add explicit scene entry modes"
```

### Task 2: 使属性和背包装备可以精确往返恢复

**Files:**
- Create: `Assets/Tests/EditMode/PersistenceTests.cs`
- Modify: `Assets/Scripts/Character/CharacterStats.cs`
- Modify: `Assets/Scripts/System/InventoryManager.cs`

**Interfaces:**
- Produces: `BaseAttack`, `BaseDefense`, `BaseAgility`, `BaseMaxHp`, `BaseMaxMp`；六参数 `SetEquipmentBonus`（末参数 `adjustCurrentResources = true`）；`InventoryManager.ResetForNewGame()`；健壮的 `LoadSaveData`。
- Consumes: `ItemDatabase.AllItems`, `InventorySaveData`, existing `CharacterStats.SetBaseFromLoad`。

- [ ] **Step 1: 写入代码物品库和装备恢复失败测试**

使用真实玩家、真实 `CharacterStats` 和真实 `InventoryManager`：

```csharp
[Test]
public void InventoryLoadRestoresEquipmentWithoutHealingSavedResources()
{
    var player = TestSceneFactory.CreatePlayer();
    var stats = player.GetComponent<CharacterStats>();
    stats.SetBaseFromLoad(15, 5, 10, 100, 50, 40, 20);
    var inventory = TestSceneFactory.Create("Inventory").AddComponent<InventoryManager>();
    var data = new InventoryManager.InventorySaveData
    {
        slotItemIds = new[] { "herb_medicinal" },
        slotAmounts = new[] { 2 },
        equippedWeapon = "sword_iron",
        equippedArmor = "",
        equippedAccessory = "",
        gold = 77
    };

    inventory.LoadSaveData(data);

    Assert.That(inventory.GetItemData("sword_iron"), Is.Not.Null);
    Assert.That(stats.attack, Is.EqualTo(20));
    Assert.That(stats.currentHp, Is.EqualTo(40));
    Assert.That(inventory.Gold, Is.EqualTo(77));
}
```

再写一个较短数组恢复测试，先污染第二个槽位，再加载单槽数组，断言第二槽已清空。它捕获“旧会话残余槽位未被覆盖”的回归。

- [ ] **Step 2: 运行单类测试并确认 RED**

使用 Task 1 的 Unity 命令，将 `-testFilter` 改为 `YuanHaiLu.Tests.EditMode.PersistenceTests`。Expected: FAIL，代码物品库未并入 Inventory，装备恢复不重算或会改变当前生命。

- [ ] **Step 3: 暴露基础属性并区分普通装备与读档重算**

在 `CharacterStats` 中加入只读基础属性：

```csharp
public int BaseAttack => _baseAttack;
public int BaseDefense => _baseDefense;
public int BaseAgility => _baseAgility;
public int BaseMaxHp => _baseMaxHp;
public int BaseMaxMp => _baseMaxMp;
```

将装备入口改为：

```csharp
public void SetEquipmentBonus(
    int attackBonus,
    int defenseBonus,
    int agilityBonus,
    int maxHpBonus,
    int maxMpBonus,
    bool adjustCurrentResources = true)
```

普通装备保持当前正向补血体验；`adjustCurrentResources == false` 时仅重算上限并把当前 HP/MP clamp 到新上限，不增加资源。

- [ ] **Step 4: 合并代码物品库并实现替换式背包加载**

`LoadItemDatabase` 先注册 `ItemDatabase.AllItems`，再用 `Resources.LoadAll<ItemData>("Items")` 覆盖同 ID，允许正式资源替代 Demo 代码定义。

`LoadSaveData` 必须：

1. 把全部 `slots` 替换为新的空槽。
2. 仅遍历 `slotItemIds` 与 `slotAmounts` 的共同有效长度。
3. 未知或空 ID 保持空槽并记录 warning。
4. 恢复装备 ID 与金钱。
5. 调用 `ApplyEquipmentStats(false)`，保持存档当前 HP/MP。
6. 触发 `OnGoldChanged` 和 `OnInventoryChanged`。

`ApplyEquipmentStats` 接受 `bool adjustCurrentResources = true` 并把参数传给 `CharacterStats.SetEquipmentBonus`。`ResetForNewGame()` 清空槽位、装备，并把金钱恢复为 Awake 时捕获的初始值。

- [ ] **Step 5: 运行持久化测试并确认 GREEN**

重复 Step 2 命令。Expected: 所有 `PersistenceTests` 通过。

- [ ] **Step 6: 提交该独立任务**

```bash
git add Assets/Tests/EditMode/PersistenceTests.cs Assets/Scripts/Character/CharacterStats.cs Assets/Scripts/System/InventoryManager.cs
git commit -m "fix: restore inventory and equipment consistently"
```

### Task 3: 健壮恢复武学与已完成任务

**Files:**
- Modify: `Assets/Tests/EditMode/PersistenceTests.cs`
- Modify: `Assets/Scripts/Character/MartialArtsSystem.cs`
- Modify: `Assets/Scripts/System/QuestManager.cs`

**Interfaces:**
- Produces: null-safe `MartialArtsSystem.LoadSaveData`；`QuestManager.ResetForNewGame()`；去空去重的 `LoadCompletedQuests`。
- Consumes: `MartialArtsSaveData`, `MartialSkillDatabase.AllSkills`, `QuestManager.CompletedQuestIds`。

- [ ] **Step 1: 添加武学替换式恢复失败测试**

先加载一份含两个技能槽的数据，再加载空数组，断言 `LearnedSkills.Count == 0` 且四个 `EquippedSkills` 全为 null。再加载含未知 ID 的数据，断言未知 ID 被忽略且不抛异常。

```csharp
martial.LoadSaveData(new MartialArtsSystem.MartialArtsSaveData
{
    learnedSkillIds = null,
    equippedSkillIds = null
}, MartialSkillDatabase.AllSkills);

Assert.That(martial.LearnedSkills, Is.Empty);
Assert.That(martial.EquippedSkills, Is.All.Null);
```

- [ ] **Step 2: 添加任务替换、去空和去重失败测试**

```csharp
questManager.LoadCompletedQuests(new[] { "q_main_01", "", "q_main_01", null, "q_side_02" });

CollectionAssert.AreEqual(
    new[] { "q_main_01", "q_side_02" },
    questManager.GetCompletedQuests());
```

随后调用 `ResetForNewGame()`，断言已完成和活跃任务列表均为空。

- [ ] **Step 3: 运行测试并确认 RED**

运行 `PersistenceTests`。Expected: 武学 null 数组导致异常或旧装备槽残留，且任务重置 API 不存在。

- [ ] **Step 4: 实现最小健壮恢复**

`MartialArtsSystem.LoadSaveData` 首先 `_learnedSkills.Clear()` 和 `System.Array.Clear(_equippedSkills, 0, _equippedSkills.Length)`；对 `data`、两个数组和 `allSkills` 分别做 null guard；只装备已成功恢复的技能。

`QuestManager.LoadCompletedQuests` 保持输入顺序地去除 null、空字符串和重复项；`ResetForNewGame()` 清空 `activeQuests` 和 `completedQuestIds`。

- [ ] **Step 5: 运行测试并确认 GREEN**

运行 `PersistenceTests`。Expected: 所有测试通过。

- [ ] **Step 6: 提交该独立任务**

```bash
git add Assets/Tests/EditMode/PersistenceTests.cs Assets/Scripts/Character/MartialArtsSystem.cs Assets/Scripts/System/QuestManager.cs
git commit -m "fix: make martial arts and quest restore replace state"
```

### Task 4: 完成交互组件及既有场景接入

**Files:**
- Create: `Assets/Tests/EditMode/InteractionTests.cs`
- Modify: `Assets/Scripts/Character/PlayerInteraction.cs`
- Modify: `Assets/Scripts/Map/EventTrigger.cs`
- Modify: `Assets/Scripts/Core/SceneBootstrapper.cs`
- Modify: `Assets/Scripts/Map/SceneDirector.cs`
- Modify: `Assets/Scripts/Editor/DemoSceneGenerator.cs`
- Modify: `ProjectSettings/InputManager.asset`

**Interfaces:**
- Produces: `PlayerInteraction.EnsureOn(GameObject)`；`EventTrigger.CanInteract()`。
- Consumes: `IInteractable`, `GameManager.CanPlayerAct()`, HUD prompt API, `Interact` input axis。

- [ ] **Step 1: 写入幂等接入和事件条件失败测试**

```csharp
[Test]
public void EnsureOnAddsExactlyOneInteractionComponent()
{
    var player = TestSceneFactory.CreatePlayer();

    var first = PlayerInteraction.EnsureOn(player);
    var second = PlayerInteraction.EnsureOn(player);

    Assert.That(second, Is.SameAs(first));
    Assert.That(player.GetComponents<PlayerInteraction>(), Has.Length.EqualTo(1));
}

[Test]
public void OneShotInteractiveEventStopsBeingCandidateAfterTrigger()
{
    var trigger = TestSceneFactory.Create("Event").AddComponent<EventTrigger>();
    trigger.requireInteract = true;
    trigger.triggerOnce = true;
    trigger.hasTriggered = true;

    Assert.That(trigger.CanInteract(), Is.False);
}
```

第一个测试捕获重复挂载组件，第二个捕获已结束事件仍显示提示。

- [ ] **Step 2: 运行测试并确认 RED**

运行 `InteractionTests`。Expected: `EnsureOn` 尚不存在；事件接口测试随后用于保护已有用户改动。

- [ ] **Step 3: 实现幂等 API 并统一三个接入点**

`PlayerInteraction.EnsureOn` 对 null 抛 `ArgumentNullException`，否则返回已有组件或 `AddComponent<PlayerInteraction>()`。

以下位置都调用同一 API：

- `DemoSceneGenerator.CreatePlayer`
- `SceneBootstrapper.SetupPlayer`
- `SceneDirector.Start` 在任何进入模式判断之前

保持当前最近目标、0.15 秒检测、`CanPlayerAct()` 守卫和 HUD 提示逻辑。`EventTrigger.CanInteract()` 继续返回 `requireInteract && !(hasTriggered && triggerOnce)`。

- [ ] **Step 4: 确认输入轴完整**

`InputManager.asset` 中 `Interact` 必须为 K 主键、E 备用键，且只存在一个同名轴。运行：

```bash
rtk rg -n -C 6 'm_Name: Interact' ProjectSettings/InputManager.asset
```

Expected: 单个匹配段包含 `positiveButton: k` 和 `altPositiveButton: e`。

- [ ] **Step 5: 运行测试并确认 GREEN**

运行 `InteractionTests`。Expected: 所有测试通过。

- [ ] **Step 6: 提交该独立任务**

```bash
git add Assets/Tests/EditMode/InteractionTests.cs Assets/Scripts/Character/PlayerInteraction.cs Assets/Scripts/Map/EventTrigger.cs Assets/Scripts/Core/SceneBootstrapper.cs Assets/Scripts/Map/SceneDirector.cs Assets/Scripts/Editor/DemoSceneGenerator.cs ProjectSettings/InputManager.asset
git commit -m "feat: connect player interaction across scenes"
```

### Task 5: 版本化存档并消除重复 sceneLoaded 回调

**Files:**
- Modify: `Assets/Tests/EditMode/PersistenceTests.cs`
- Modify: `Assets/Scripts/System/SaveManager.cs`
- Modify: `Assets/Scripts/Map/SceneDirector.cs`

**Interfaces:**
- Produces: `SaveData.saveVersion = 2` 和基础属性字段；具名 `OnSceneLoadedForLoad`；internal 单次 `ApplySaveDataToLoadedScene(SaveData)`。
- Consumes: Task 1 进入模式、Task 2 基础属性/背包恢复、Task 3 武学和任务恢复。

- [ ] **Step 1: 写入版本化往返和精确资源恢复失败测试**

创建玩家、装备铁剑、设置当前 HP/MP、构造 version 2 `SaveData`，调用真实恢复入口后断言：

```csharp
Assert.That(stats.BaseAttack, Is.EqualTo(15));
Assert.That(stats.attack, Is.EqualTo(20));
Assert.That(stats.currentHp, Is.EqualTo(40));
Assert.That(player.transform.position, Is.EqualTo(new Vector3(3f, -2f, 0f)));
Assert.That(GameManager.Instance.CurrentSceneEntryMode,
    Is.EqualTo(GameManager.SceneEntryMode.Active));
```

再对 `JsonUtility.ToJson`/`FromJson` 做真实往返，断言 `saveVersion` 和嵌套 inventory/martialArts 数据保留。

- [ ] **Step 2: 运行测试并确认 RED**

运行 `PersistenceTests`。Expected: version 2 基础字段和可测试恢复入口不存在。

- [ ] **Step 3: 扩展 SaveData 和保存路径**

`SaveData` 新增：

```csharp
public int saveVersion;
public int baseAttack;
public int baseDefense;
public int baseAgility;
public int baseMaxHp;
public int baseMaxMp;
```

`SaveGame` 设置 `saveVersion = 2`，基础字段来自 `CharacterStats.Base*`；保留旧 `attack/defense/agility/maxHp/maxMp` 字段用于读取旧存档。

- [ ] **Step 4: 用具名单次回调替换匿名订阅**

`LoadGame` 在 JSON 解析、非空 `sceneName` 和 `Application.CanStreamedLevelBeLoaded(sceneName)` 校验成功后：

1. 解除可能残留的 `OnSceneLoadedForLoad`。
2. 保存 `_pendingLoadData`。
3. 调用 `GameManager.BeginSceneEntry(LoadGame)`。
4. 订阅具名回调。
5. 调用 `SceneManager.LoadScene`。

`OnSceneLoadedForLoad` 的第一条操作是解除自身并取走 `_pendingLoadData`；`OnDestroy` 也解除回调。这样异常恢复不会留下重复订阅。

- [ ] **Step 5: 按固定顺序实现恢复入口**

`ApplySaveDataToLoadedScene` 使用如下顺序：玩家身份与位置 → 基础属性/等级/经验 → 背包装备 → 武学 → 已完成任务 → 章节/玩家名 → `Exploration` → `CompleteSceneEntry()`。

版本 2 使用 `base*`；旧版使用旧总属性，且旧版无 inventory 时不应用装备。任何子组件缺失都记录错误，但方法最终仍结束场景进入上下文。

- [ ] **Step 6: 让 SceneDirector 只初始化新游戏**

`Start` 先确保玩家交互组件存在；只有 `GameManager.Instance == null || GameManager.Instance.ShouldInitializeNewGame` 时启动 `PlayIntroSequence()`。新游戏初始数据写入完成后调用 `CompleteSceneEntry()`；读档和普通场景切换均不发放初始物资、不改出生点、不播放开场教学。

- [ ] **Step 7: 运行测试并确认 GREEN**

运行 `PersistenceTests` 与 `GameManagerEntryModeTests`。Expected: 全部通过。

- [ ] **Step 8: 提交该独立任务**

```bash
git add Assets/Tests/EditMode/PersistenceTests.cs Assets/Scripts/System/SaveManager.cs Assets/Scripts/Map/SceneDirector.cs
git commit -m "fix: make save restoration single-use and versioned"
```

### Task 6: 补全主菜单全局系统并修复场景启动

**Files:**
- Create: `Assets/Scripts/System/GlobalSystemsBootstrapper.cs`
- Create: `Assets/Tests/EditMode/GlobalSystemsBootstrapperTests.cs`
- Modify: `Assets/Scripts/UI/MainMenu.cs`
- Modify: `Assets/Scripts/Core/SceneBootstrapper.cs`
- Modify: `Assets/Scripts/Editor/MainMenuSceneGenerator.cs`
- Modify: `Assets/Scenes/MainMenu.unity`

**Interfaces:**
- Produces: `GlobalSystemsBootstrapper.EnsureRequiredSystems(GameManager)`；MainMenu 默认 `Demo_YanLiuTown`。
- Consumes: Save/Inventory/Quest/GameTime/Dialogue 单例，Task 1 进入模式，Task 2/3 新游戏重置。

- [ ] **Step 1: 写入全局系统幂等创建失败测试**

```csharp
[Test]
public void EnsureRequiredSystemsCreatesOneOfEveryPersistentManager()
{
    var root = TestSceneFactory.Create("GameManager");
    var gameManager = root.AddComponent<GameManager>();

    GlobalSystemsBootstrapper.EnsureRequiredSystems(gameManager);
    GlobalSystemsBootstrapper.EnsureRequiredSystems(gameManager);

    Assert.That(Object.FindObjectsByType<SaveManager>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
    Assert.That(Object.FindObjectsByType<InventoryManager>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
    Assert.That(Object.FindObjectsByType<QuestManager>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
    Assert.That(Object.FindObjectsByType<GameTimeManager>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
    Assert.That(Object.FindObjectsByType<DialogueManager>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
}
```

该测试捕获从主菜单进入 Demo 后管理器缺失或重复。

- [ ] **Step 2: 运行测试并确认 RED**

运行 `GlobalSystemsBootstrapperTests`。Expected: bootstrapper 尚不存在。

- [ ] **Step 3: 实现统一系统补全**

`EnsureRequiredSystems(GameManager owner)` 对 owner 做 null guard；对每个缺失单例创建命名子对象并挂到 owner 下：`SaveManager`、`InventoryManager`、`QuestManager`、`GameTimeManager`、`DialogueManager`。已有实例不重复创建。

`MainMenu.Start` 和 `SceneBootstrapper.InitializeSystems` 调用此入口；后者继续负责 AudioManager 等场景能力。

- [ ] **Step 4: 修复 MainMenu 运行时行为**

`firstSceneName` 默认值改为 `Demo_YanLiuTown`。`Start` 补全系统并按 `Btn_新游戏/继续游戏/设置/退出` 名称为无持久监听的按钮绑定一次运行时监听。

`OnNewGame` 执行：

1. `InventoryManager.ResetForNewGame()`。
2. `QuestManager.ResetForNewGame()`。
3. 玩家名和章节恢复默认值。
4. `GameManager.BeginSceneEntry(NewGame)` 并进入 `Exploration`。
5. 加载 `Demo_YanLiuTown`。

设置面板引用缺失时输出 warning，不抛 `NullReferenceException`。继续游戏直接使用 `SaveManager.LoadGame()`，不保留过期注释。

- [ ] **Step 5: 同步生成器和现有场景序列化值**

`MainMenuSceneGenerator` 生成的 `MainMenu` 组件沿用默认场景名，并让运行时 `Start` 负责绑定。现有 `MainMenu.unity` 的 `firstSceneName` 改为 `Demo_YanLiuTown`。

- [ ] **Step 6: 运行测试并确认 GREEN**

运行 `GlobalSystemsBootstrapperTests`。Expected: 重复调用后每个必需管理器恰好一个。

- [ ] **Step 7: 提交该独立任务**

```bash
git add Assets/Scripts/System/GlobalSystemsBootstrapper.cs Assets/Tests/EditMode/GlobalSystemsBootstrapperTests.cs Assets/Scripts/UI/MainMenu.cs Assets/Scripts/Core/SceneBootstrapper.cs Assets/Scripts/Editor/MainMenuSceneGenerator.cs Assets/Scenes/MainMenu.unity
git commit -m "fix: bootstrap complete systems from main menu"
```

### Task 7: 更新记忆、交接和使用文档

**Files:**
- Modify: `AGENTS.md`
- Modify: `README.md`
- Modify: `SETUP_GUIDE.md`
- Modify: `.zcode/plans/plan-sess_12dba3a0-9d1f-412c-9643-d44a8025eddb.md`

**Interfaces:**
- Consumes: Tasks 1-6 的最终行为和验证结果。
- Produces: 与实际工程一致的长期记忆、项目交接、快速开始和本次实施记录。

- [ ] **Step 1: 更新 AGENTS.md**

将交互缺失和存档完全缺失从 P0 移入修复历史；剩余限制明确为“活跃任务及世界标志尚未持久化”。记录：场景进入模式、单次读档回调、基础属性/装备恢复顺序、主菜单系统补全、代码物品库回退、现有场景的运行时交互保障、输入配置改动后需重启 Unity。

- [ ] **Step 2: 更新 README.md**

使用实际脚本数量和代码行数；Unity 版本统一为 `6000.4.10f1`；操作表写 K/E 交互；快速开始加载 `MainMenu` 或 `Demo_YanLiuTown`；功能状态说明已保存背包/装备/金钱/武学/已完成任务，但活跃任务和世界状态未保存。

- [ ] **Step 3: 更新 SETUP_GUIDE.md**

删除 Unity 2022/2023 和 URP 建议，改为 Unity `6000.4.10f1` + 2D Core；场景名、Build Settings、物理 Layer、SortingLayer 和实际输入轴与 ProjectSettings 对齐。

- [ ] **Step 4: 更新会话实施记录**

把 `.zcode/plans/plan-sess_12dba3a0-9d1f-412c-9643-d44a8025eddb.md` 从预期改动改成实际完成项、测试证据、未完成限制和 Unity 人工验证清单，并注明长期事实以 `AGENTS.md` 为准。

- [ ] **Step 5: 做文档一致性检索**

```bash
rtk rg -n '2022\.3|2023\.2|2D \(URP\)|YanLiuTown|交互键系统缺失|存档系统不完整|33个脚本|7000行' AGENTS.md README.md SETUP_GUIDE.md
```

Expected: 不再出现过期版本、错误场景名和已解决问题；`Demo_YanLiuTown` 的正确引用除外。

- [ ] **Step 6: 提交文档任务**

```bash
git add AGENTS.md README.md SETUP_GUIDE.md .zcode/plans/plan-sess_12dba3a0-9d1f-412c-9643-d44a8025eddb.md
git commit -m "docs: update project memory and handoff"
```

### Task 8: 全量验证、审查和交付检查

**Files:**
- Verify all files changed by Tasks 1-7。

**Interfaces:**
- Consumes: approved design and all implementation tasks。
- Produces: fresh test/build/review evidence and remaining manual QA list。

- [ ] **Step 1: 运行全部 EditMode 测试**

```bash
/Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/lhw/code/yuanHaiLu -runTests -testPlatform EditMode -testResults /tmp/yuanHaiLu-editmode-all.xml -logFile /tmp/yuanHaiLu-editmode-all.log -quit
```

Expected: Unity exit code 0，XML 中 tests-failed 为 0，日志无 C# 编译错误。

- [ ] **Step 2: 运行无测试的批处理编译导入检查**

```bash
/Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/lhw/code/yuanHaiLu -quit -logFile /tmp/yuanHaiLu-compile.log
```

Expected: exit code 0；`rtk rg -n 'error CS|Compilation failed|Scripts have compiler errors' /tmp/yuanHaiLu-compile.log` 无匹配。

- [ ] **Step 3: 检查资源和 Git 完整性**

```bash
rtk git diff --check de87830..HEAD
rtk git status --short
rtk rg -n -C 6 'm_Name: Interact' ProjectSettings/InputManager.asset
rtk rg -n 'firstSceneName: Demo_YanLiuTown' Assets/Scenes/MainMenu.unity
rtk rg -n 'PlayerInteraction\.EnsureOn' Assets/Scripts
```

Expected: 无空白错误；所有新增 Unity 资产都有 `.meta`；输入轴、场景名和三个交互接入点均存在。

- [ ] **Step 4: 按固定点审查最终差异**

以设计提交 `de87830` 为 fixed point，执行 standards/spec 两轴审查：

```bash
rtk git log --oneline de87830..HEAD
rtk git diff de87830...HEAD
```

规格来源为 `docs/superpowers/specs/2026-08-12-interaction-save-lifecycle-design.md`；标准来源为 `AGENTS.md` 和项目 RTK 约定。修复所有高优先级问题，或在交付中明确记录未解决风险。

- [ ] **Step 5: 人工 Play 验证清单**

在 Unity 重启后验证：主菜单新游戏；K/E NPC 对话；自动/手动 EventTrigger；存档后位置、资源、背包、装备、武学、已完成任务往返；读档不重复初始物资；读档后卸装属性正确；再次切场景不重放旧存档。

如果当前执行环境不能可靠自动操作 Unity Play，交付时明确将这些列为未执行，而不是声称通过。

- [ ] **Step 6: 最终状态检查**

```bash
rtk git status --short --branch
rtk git log --oneline -10
```

确认没有临时日志、测试结果或未预期文件进入仓库；总结自动验证证据、人工验证缺口和剩余非目标限制。
