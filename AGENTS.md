# AGENTS.md — 渊海录项目交接与记忆

> 本文件是接手本项目（开发者或 AI 助手）的首选入口。长期事实以本文件为准。
> 最后更新：2026-08-12

## 0. 30 秒速览

| 项 | 内容 |
|----|------|
| 项目 | 渊海录（YuanHaiLu）— Unity 6 像素武侠 RPG（俯视角 2D） |
| 引擎 | Unity `6000.4.10f1`（2D Core / 内置 2D，**不是 URP**） |
| 平台 | macOS Apple Silicon（可扩 PC/WebGL/移动） |
| 代码规模 | 48 个运行时/编辑器 C# 文件，约 10,600 行；另有 5 个测试文件 |
| 状态 | 烟柳镇 Demo 可运行；核心框架完整，内容和动画待填充 |
| 版本控制 | Git，默认分支 `main`；`.gitignore` 已配置 |
| 测试 | Unity Test Framework `1.6.0`；11 个 EditMode 测试 |
| 设计文档 | `docs/01-art-style-guide.md`、`docs/02-story-design.md`、`docs/superpowers/specs/`、`README.md`、`SETUP_GUIDE.md` |

## 1. 如何运行

### 1.1 首次打开

1. Unity Hub → Open → 选择本仓库根目录。
2. 使用 Unity `6000.4.10f1`，等待首次导入和编译。
3. Console 不应存在 C# 编译错误。

### 1.2 场景

场景可由编辑器工具程序化重建：

```text
Tools → 渊海录 → 生成主菜单场景
Tools → 渊海录 → 生成Demo场景
```

对应实现：`Assets/Scripts/Editor/MainMenuSceneGenerator.cs` 与 `DemoSceneGenerator.cs`。

Build Profiles / Build Settings 应包含：

```text
0  Assets/Scenes/MainMenu.unity
1  Assets/Scenes/Demo_YanLiuTown.unity
```

可从主菜单运行，也可直接打开 Demo；直接运行 Demo 默认按新游戏初始化。

### 1.3 关键操作守则

- 修改 `ProjectSettings/*.asset` 后必须重启 Unity，尤其是 InputManager 和 TagManager。
- `.meta` 必须与 Unity 资源一起提交；不要提交 `Library/`、`Temp/`、日志、`.csproj`、`.sln`。
- 精灵可用 `Tools → 渊海录 → 配置所有精灵为像素模式` 批量处理。

### 1.4 操作键

| 按键 | 功能 |
|------|------|
| WASD / 方向键 | 移动 |
| J | 攻击（3 连击，可暴击） |
| K / E | 最近目标交互 |
| Left Shift | 冲刺 |
| 数字键 1–4 | 已装备武学 |
| Space / Enter | 推进对话 |
| 数字键 1–9 | 对话分支 |
| Tab / Q | 背包 / 任务日志 |
| ESC | 暂停 |

Legacy Input Manager 中 `Interact` 只有一个轴：K 主键、E 备用键。

## 2. 架构总览

### 2.1 命名空间分层

```text
YuanHaiLu.Core
  GameConfig / GameManager / SceneBootstrapper / CameraFollow / PixelPerfectCamera

YuanHaiLu.Character
  PlayerController / PlayerCombat / PlayerInteraction / CharacterStats
  EnemyAI / NPCBase / MartialArtsSystem / MartialSkill / LevelSystem / CharacterAudio

YuanHaiLu.Effects
  EffectsManager（特效池、命中火花、伤害数字、剑气、屏闪）

YuanHaiLu.Map
  TileMapManager / AreaTrigger / TeleportPoint / Destructible
  ItemPickup / EventTrigger / SceneDirector

YuanHaiLu.Dialogue
  DialogueManager（打字机、条件、动作、分支）

YuanHaiLu.GameSystem
  GlobalSystemsBootstrapper / SaveManager / InventoryManager / ItemDatabase
  QuestManager / MartialSkillDatabase / ShopManager / LootTable
  AudioManager / GameTimeManager / ScreenTransition / PlayerDeathHandler

YuanHaiLu.UI
  HUD / MainMenu / InventoryUI / QuestUI / PauseMenu / DialogueUI

YuanHaiLu.Editor
  DemoSceneGenerator / MainMenuSceneGenerator / PixelArtImporter
  ProjectInitializer / SetupBuildSettings

YuanHaiLu.Combat
  DamageCalculator（预留）
```

### 2.2 核心模式

- `GameManager` 同时维护游戏状态和场景进入模式。
- `SceneEntryMode` 区分 `NewGame`、`LoadGame`、`SceneTransition`、`Active`；只有新游戏允许 `SceneDirector` 发放初始属性/武学/物资。
- `GlobalSystemsBootstrapper` 是主菜单和游戏场景共用的管理器补全入口，确保 Save/Inventory/Quest/GameTime/Dialogue 各一个。
- 管理器通过 `Instance` 和 `DontDestroyOnLoad` 跨场景；新增管理器应接入统一 Bootstrapper，不要在入口场景复制一套逻辑。
- 系统通过 `event System.Action` 解耦，例如 HP、任务、对话、技能等事件。
- 战斗判定帧由 Animator 事件调用 `PlayerCombat.OnAttackHitFrame()`。
- `PlayerInteraction` 扫描最近的 `IInteractable`，受 `GameManager.CanPlayerAct()` 控制。

### 2.3 典型存档恢复顺序

```text
LoadGame 读取并校验 JSON
  → 设置 SceneEntryMode.LoadGame
  → 注册具名 sceneLoaded 回调后加载场景
  → 回调先解除订阅
  → 恢复身份/等级/基础属性/HP/MP/位置
  → 恢复背包/装备/金钱并重算派生属性
  → 恢复武学和装备槽
  → 恢复已完成任务
  → 状态切到 Exploration
  → SceneEntryMode.Active
```

`SaveData.saveVersion == 2` 保存基础属性，避免把装备加成重复当作基础值；旧/过渡存档有迁移路径。

## 3. 关键约定

### 3.1 像素规格

- 内部分辨率 `480×270`，PPU `16`。
- 瓦片 `16×16`，角色帧 `48×48`。
- Filter Mode = Point，Compression = None，无抗锯齿，VSync = Don't Sync。

### 3.2 Sorting Layer

必须与 `GameConfig.SORTING_*` 一致：

```text
Ground → Environment → Character → Foreground → UI
```

### 3.3 物理 Layer

```text
6: Player
7: Enemy
8: NPC
9: Environment
```

没有 `Interactable` 物理层；交互依赖接口过滤。

### 3.4 脚本与数据位置

| 类型 | 位置/命名空间 |
|------|---------------|
| 运行时脚本 | `Assets/Scripts/<子系统>/` / `YuanHaiLu.<子系统>` |
| 系统脚本 | `Assets/Scripts/System/` / `YuanHaiLu.GameSystem` |
| 编辑器工具 | `Assets/Scripts/Editor/` / `YuanHaiLu.Editor` |
| 测试 | `Assets/Tests/EditMode/` / `YuanHaiLu.Tests.EditMode` |
| 数据 SO | 定义在 System；实例放 `Assets/Resources/Items|Quests/` |

系统命名空间禁止使用 `YuanHaiLu.System`，它会与 .NET `System` 冲突。

### 3.5 物品和装备

- `InventoryManager` 先加载 `ItemDatabase.AllItems` 代码表。
- `Resources/Items` 下同 ID 的 ScriptableObject 会覆盖代码表，便于逐步内容化。
- 普通装备允许按上限增量调整当前资源；读档装备重算使用 `adjustCurrentResources=false`，只 clamp，不治疗。
- `LoadSaveData` 是替换式恢复：先清空槽位，再载入有效共同长度，未知 ID 跳过并警告。

## 4. 开发与测试流程

1. 修改代码和对应测试。
2. 运行相关 EditMode 测试。
3. 切回 Unity 触发编译，检查 Console。
4. 涉及场景或输入时重启 Unity 后 Play 验证。
5. 更新本文件或 `docs/` 中的长期事实。
6. 提交前运行全量测试、批处理编译和 `git diff --check`。

全部 EditMode 测试命令：

```bash
/Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath /absolute/path/to/yuanHaiLu \
  -runTests \
  -testPlatform EditMode \
  -testResults /tmp/yuanHaiLu-editmode.xml \
  -logFile /tmp/yuanHaiLu-editmode.log
```

`-runTests` 时不要传 `-quit`，否则可能在结果写出前退出。当前测试覆盖场景入口、存档迁移/往返、背包装备、武学任务、交互幂等和全局系统补全。

## 5. 已知问题与未完成项

### P0

当前没有已知的运行时阻塞 P0。新增功能后仍需在 Unity Play 中做端到端验证。

### P1 — 数据与内容

- 存档尚未包含活跃任务目标进度、敌人状态、拾取物状态、区域标志和其他世界状态。
- `Assets/Sprites/Generated/` 和部分瓦片是程序生成的占位美术。
- 没有正式 Animator Controller / 动画剪辑；代码虽写入参数，但当前动画不会完整播放。
- 物品/任务主要由代码表和 Markdown 设计稿提供，正式 `.asset` 资源仍待制作。

### P2 — 增强

- 天枢城等后续 5 个区域未制作。
- 商店 UI、武学技能树、小地图、BOSS 战待实现。
- BGM/SFX 仍为空或占位。

## 6. 文件地图

```text
yuanHaiLu/
├── Assets/
│   ├── Scripts/                 48 个 .cs，约 10,600 行
│   ├── Tests/EditMode/          11 个测试 + 测试工具
│   ├── Scenes/                  MainMenu + Demo_YanLiuTown
│   ├── Sprites/Generated/       占位精灵
│   ├── Art/Tilesets/            瓦片与参考
│   └── Resources/
│       ├── Items/item_database.md
│       └── Quests/quest_database.md
├── docs/
│   ├── 01-art-style-guide.md
│   ├── 02-story-design.md
│   └── superpowers/specs|plans/ 本次规格与实施计划
├── tools/                       资源生成脚本
├── ProjectSettings/             修改后需重启 Unity
├── Packages/manifest.json
├── README.md
├── SETUP_GUIDE.md
└── AGENTS.md                    本文件
```

## 7. 修复历史（2026-08-12）

### 第一批：Demo 运行阻塞

1. 补 `Attack(J)`、`Dash(Left Shift)` 输入轴。
2. SortingLayer 对齐 `Ground/Environment/Character/Foreground/UI`。
3. 修复 `CharacterStats.RestoreMp` 恒定回满的问题。
4. 为远程武学飞行物补触发碰撞体。

### 第二批：武学、装备和工程化

5. 清理重复 using。
6. 武学快捷键增加状态守卫，避免与对话数字键冲突。
7. 武学学习链路改接 `MartialSkillDatabase` → `MartialArtsSystem`。
8. 属性拆分为基础值与装备加成，升级修改基础值。
9. 建立 `.gitignore` 和 Git 仓库，清理临时日志。

### 第三批：交互、可靠存档与启动生命周期

10. 新增 `PlayerInteraction`，K/E 驱动统一 `IInteractable`；生成器、Bootstrapper、SceneDirector 三处幂等保障。
11. 修复 NPC 使用不存在的 `Interactable` Layer；按键式 `EventTrigger` 接入接口。
12. 背包合并代码物品库，恢复时清理残余槽位、忽略未知 ID，并正确重算装备属性。
13. 武学和已完成任务改为 null-safe、去重、替换式恢复；新游戏可清理旧会话数据。
14. 存档升级为 v2，保存基础属性、背包/装备/金钱、武学和已完成任务；提供旧存档迁移。
15. 修复 `sceneLoaded` 在加载后才订阅且匿名回调不解除的问题，改为加载前注册、单次具名回调。
16. `SceneDirector` 只在新游戏初始化，读档不再重置出生点或重复发初始物资。
17. 新增 `GlobalSystemsBootstrapper`，修复主菜单进入 Demo 后管理器缺失；主菜单按钮运行时绑定，首场景名改为 `Demo_YanLiuTown`。
18. 建立 EditMode 测试程序集和 11 个回归测试。

## 8. 当前人工 QA 清单

自动测试不能替代 Play 验证。涉及本批改动时至少检查：

- 主菜单新游戏能进入 Demo。
- K/E NPC 对话、手动事件和传送点可达。
- 自动事件不显示交互提示，一次性事件不会再次成为目标。
- 存档往返精确恢复位置、HP/MP、背包、装备、金钱、武学和已完成任务。
- 读档不发初始物资、不覆盖位置；卸装后属性正确。
- 后续场景加载不会重复应用旧存档。

## 9. 推送到 GitHub（尚未执行）

```bash
git remote add origin git@github.com:<用户名>/yuanHaiLu.git
git branch -M main
git push -u origin main
```
