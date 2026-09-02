# AGENTS.md — 渊海录项目交接与记忆

> 本文件是接手本项目（开发者或 AI 助手）的首选入口。长期事实以本文件为准。
> 最后更新：2026-08-29

## 0. 30 秒速览

| 项 | 内容 |
|----|------|
| 项目 | 渊海录（YuanHaiLu）— Unity 6 像素武侠 RPG（俯视角 2D） |
| 引擎 | Unity `6000.4.10f1`（2D Core / 内置 2D，**不是 URP**） |
| 平台 | macOS Apple Silicon（可扩 PC/WebGL/移动） |
| 代码规模 | 78 个运行时/编辑器 C# 文件；另有 28 个测试 .cs |
| 状态 | 单主角 MVP 垂直切片（docs/15，实施方向 docs/18）：固定男主 + 三武器流派 + `MvpWorldModule` 密集小模块装配的烟柳镇↔客栈闭环 + MVP_01 河岸失物 |
| 版本控制 | Git，默认分支 `main`；`.gitignore` 已配置；已收敛为单一主分支 `main` |
| 测试 | 139 EditMode + 14 PlayMode + 52 Python 全通过 |
| 设计/交接 | `docs/01-art-style-guide.md`、`docs/02-story-design.md`、`docs/15-single-hero-mvp-design.md`、`docs/18-dense-pixel-mvp-implementation-handoff.md`（现行） |

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
Tools → 渊海录 → 生成客栈室内场景
```

对应实现：`Assets/Scripts/Editor/MainMenuSceneGenerator.cs`、`DemoSceneGenerator.cs` 与 `InnSceneGenerator.cs`。公共装配（管理器/摄像机/玩家/UI）统一走 `PlaySceneAssembler.cs`，两个玩法生成器只保留差异化内容。

Build Profiles / Build Settings 应包含：

```text
0  Assets/Scenes/MainMenu.unity
1  Assets/Scenes/Demo_YanLiuTown.unity
2  Assets/Scenes/Demo_Inn.unity
3–12  Assets/Scenes/Regions/*.unity
13–25 Assets/Scenes/Interiors/*.unity
```

`Demo_Inn.unity` 是客栈玩法场景：从正式 `Interiors/inn.unity` Tilemap 克隆后叠加玩家、掌柜老赵（MVP_01）与回镇出口；正式室内基线文件保持纯 Tilemap，供 EnvironmentArtTests 反复重建（与 Demo_YanLiuTown / Regions/yanliu 同构）。可从主菜单运行，也可直接打开 Demo；`MvpDirectPlayFallback` 会在直开玩法场景且仍处于 `MainMenu` 状态时进入 `Exploration`，不会覆盖正常读档或跨场景状态。

> ProjectSettings 例外说明：本分支相对 main 唯一的 ProjectSettings 变更是
> `EditorBuildSettings.asset` 加入 `Demo_Inn.unity`（运行时按场景名加载所必需）。
> 其余 ProjectSettings 文件一律与 main 保持一致。

### 1.3 关键操作守则

- 修改 `ProjectSettings/*.asset` 后必须重启 Unity，尤其是 InputManager 和 TagManager。
- `.meta` 必须与 Unity 资源一起提交；不要提交 `Library/`、`Temp/`、日志、`.csproj`、`.sln`。
- 精灵可用 `Tools → 渊海录 → 配置所有精灵为像素模式` 批量处理。

### 1.4 操作键

| 按键 | 功能 |
|------|------|
| WASD / 方向键 | 移动 |
| J | 攻击（连击随流派 3–5 段，可暴击） |
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
  GameConfig / GameManager / PlayerAppearance / WeaponStyle
  CameraFollow / PixelPerfectCamera / MvpDirectPlayFallback

YuanHaiLu.Art
  CharacterArtCatalog / EnvironmentArtCatalog / CharacterVisual / MvpArtCatalog
  MvpStaticVisual / MvpWorldModule / PlayerAppearanceBinder / RegionSceneDefinition
  ArtAssetId / RegionEnvironmentController

YuanHaiLu.Character
  PlayerController / PlayerCombat / PlayerInteraction / CharacterStats
  EnemyAI / NPCBase / MartialArtsSystem / MartialSkill / LevelSystem / CharacterAudio

YuanHaiLu.Effects
  EffectsManager（特效池、命中火花、伤害数字、剑气、屏闪）

YuanHaiLu.Map
  AreaTrigger / TeleportPoint / Destructible
  ItemPickup / EventTrigger / SceneDirector

YuanHaiLu.Dialogue
  DialogueManager（打字机、条件、动作、分支）

YuanHaiLu.GameSystem
  GlobalSystemsBootstrapper / SaveManager / InventoryManager / ItemDatabase
  QuestDatabase / QuestManager / QuestGiver / QuestTarget / QuestStageGate
  MartialSkillDatabase / LootTable
  AudioManager / GameTimeManager / ScreenTransition / PlayerDeathHandler

YuanHaiLu.UI
  HUD / MainMenu / PauseMenu / DialogueUI

YuanHaiLu.Editor
  DemoSceneGenerator / MainMenuSceneGenerator / InnSceneGenerator / PlaySceneAssembler
  MvpSceneModuleAssembler / MvpDenseSceneLayouts
  PixelArtImporter / ProjectInitializer / SetupBuildSettings / ArtImportRules / ArtAssetValidator
  CharacterAnimationBuilder / RegionSceneBuilder / EnvironmentTileBuilder
  CharacterShowcaseGenerator / EnvironmentShowcaseGenerator / FormalSceneCapture
  VisualRegressionCapture
```

### 2.2 核心模式

- `GameManager` 同时维护游戏状态和场景进入模式。
- `SceneEntryMode` 区分 `NewGame`、`LoadGame`、`SceneTransition`、`Active`；只有新游戏允许 `SceneDirector` 发放初始属性/武学/物资。
- `GlobalSystemsBootstrapper` 是主菜单和游戏场景共用的管理器补全入口，确保 Save/Inventory/Quest/GameTime/Dialogue 各一个。
- 管理器通过 `Instance` 和 `DontDestroyOnLoad` 跨场景；新增管理器应接入统一 Bootstrapper，不要在入口场景复制一套逻辑。
- 系统通过 `event System.Action` 解耦，例如 HP、任务、对话、技能等事件。
- 战斗判定帧由 Animator 事件调用 `PlayerCombat.OnAttackHitFrame()`。
- `PlayerInteraction` 扫描最近的 `IInteractable`，受 `GameManager.CanPlayerAct()` 控制。
- `CharacterArtCatalog` 和 `EnvironmentArtCatalog` 以稳定 snake_case ID 作为运行时唯一入口；正式场景不得创建运行时 Texture/Sprite 作为美术回退。
- `PlayerAppearance` 保有 2 性别 × 6 职业共 12 套资产，但 docs/15 起玩家默认固定男主 `player_male_swordsman`（`PlayerAppearance.Default`），菜单不再提供外观选择。
- `WeaponStyle`（sword / gauntlets / dart）是三武器流派的唯一运行时模型：不可变配置表 + 稳定 ID、非法回退 sword、近战档案与 `ActiveSkillId`。`PlayerCombat` 与 `MartialArtsSystem` 全部从它取值；`GameManager.SetWeaponStyle` 非法 ID 抛错，存档迁移侧先 `ParseOrDefault` 归一化。
- `QuestStageGate` 把场景对象激活绑定到顺序任务的当前步骤（复审 P0）：接任务/未到步骤前整体失活，防止敌人被提前击杀、任务物品被提前拾走造成软锁。受其管控的组件必须把协程启动放在 `OnEnable`（失活会杀死协程，重激活要能重启）——`ItemPickup` 即按此约定实现。
- `GameManager.TransitionCarry` 在 `AreaTrigger` 场景切换前抓取场景本地玩家的等级/属性/HP/MP/武学，落地后回放；烟柳镇 ↔ Demo_Inn 往返不丢成长。
- 正式环境由 `RegionSceneBuilder` 生成 7 层 Tilemap；必须用批量 `SetTiles` 后保存，逐格 `SetTile` 在 Unity 6 批处理路径曾出现未序列化问题。

### 2.3 典型存档恢复顺序

```text
LoadGame 读取并校验 JSON
  → 设置 SceneEntryMode.LoadGame
  → 注册具名 sceneLoaded 回调后加载场景
  → 回调先解除订阅
  → 恢复身份/等级/基础属性/HP/MP/位置
  → 恢复背包/装备/金钱并重算派生属性
  → 恢复武学和装备槽
  → v3 按稳定 ID 恢复活跃任务、目标进度、接取时间和已完成任务
    （v2 只恢复已完成任务，并清空活跃任务）
  → v4 起主角外观固定男主；v1–v3 旧档迁移为 player_male_swordsman
  → v5 恢复武器流派 weaponStyleId；缺失或非法回退 sword
  → 状态切到 Exploration
  → SceneEntryMode.Active
```

`SaveData.saveVersion == 5` 是当前格式；基础属性仍按 v2 语义恢复，任务按 v3 语义恢复，外观按 v4 语义归一为男主，流派按 v5 语义恢复。不要降低版本号或改变既有字段含义。任务目标恢复按 type+targetId 顺序消费，同一任务允许重复目标对（MVP_01 首尾两步都找掌柜）。

## 3. 关键约定

### 3.1 像素规格

- 内部分辨率 `480×270`，PPU `16`。
- 瓦片 `16×16`，正式角色帧 `32×32`，PPU `16`。
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
| 测试 | `Assets/Tests/EditMode/`、`Assets/Tests/PlayMode/` |
| 数据 SO | 定义在 System；实例放 `Assets/Resources/Items|Quests/` |

系统命名空间禁止使用 `YuanHaiLu.System`，它会与 .NET `System` 冲突。

### 3.5 物品和装备

- `InventoryManager` 先加载 `ItemDatabase.AllItems` 代码表。
- `Resources/Items` 下同 ID 的 ScriptableObject 会覆盖代码表，便于逐步内容化。
- 普通装备允许按上限增量调整当前资源；读档装备重算使用 `adjustCurrentResources=false`，只 clamp，不治疗。
- `LoadSaveData` 是替换式恢复：先清空槽位，再载入有效共同长度，未知 ID 跳过并警告。

### 3.6 任务运行时

- `QuestDatabase` 提供 `M01_01`–`M01_05` 与 `MVP_01` 稳定代码模板；`Resources/Quests` 下同 ID 的 `QuestData` 可覆盖代码模板。
- `ActiveQuest` 深复制模板目标，模板只提供显示和奖励数据，运行时进度不得写回模板。
- `QuestManager` 是接取、目标推进、提交、奖励与 v3 序列化的唯一权威；损坏进度会钳制并警告，未知模板/目标会跳过并警告。
- `QuestData.sequentialObjectives` 为真时目标严格按序推进（只有第一个未完成目标接收进度）；M01 系列保持自由顺序，MVP_01 为顺序任务。
- `MVP_01` 河岸失物五步固定顺序：找掌柜 → 到河岸 → 杀 2 水匪 → 拾荷包 → 回掌柜复命；接取与提交都在客栈室内 `NPC_掌柜老赵` 的对话后结算。
- 河岸水匪与荷包由 `QuestStageGate` 门控（复审 P0）：未接任务或未到对应步骤时整体失活，玩家无法提前消耗；`QuestStageGate` 订阅 QuestManager 的接取/目标/完成事件刷新。
- `QuestGiver` 与 `NPCBase` 同物体配置，但不实现 `IInteractable`；任务行为只在它启动的对话结束后结算。
- `QuestTarget`、`AreaTrigger`、`ItemPickup` 和 `MartialArtsSystem` 只在真实成功行为后上报进度；重复死亡、区域、拾取或学习不会重复计数。`AreaTrigger` 的任务上报先于一次性地名显示判定，先入区后接任务也能补报。

### 3.7 正式美术流水线

- 可编辑源位于 `Assets/ArtSource/`；烘焙结果位于 `Assets/Art/`，两者都提交 Git。
- `tools/art_pipeline/` 只用确定性像素模块、清单和规范色板生成资源；当前范围为 97 个角色与 23 个环境配方。
- 每个输出带 `.art.json`、稳定帧名、pivot、SHA-256；`ArtImportRules` 精确切片，`ArtAssetValidator` 检查哈希/尺寸/持久资源。
- 角色分类固定：12 Player、15 Named、36 NPC、24 Enemies、10 Bosses。
- 环境固定：10 Regions（tianshu/cangyue/yanliu/chisha/youhuang/hanyuan/prologue_village/luoyuan/jueyun/zhenyue）和 13 Interiors（inn/residence/shop/pharmacy/academy/yamen/palace/temple/cave/tomb/dungeon/military_camp/ship_cabin）。
- `CharacterVisual.Apply` 中 UnityEngine.Object 的组件检查必须用显式两段 `== null`；禁止 `GetComponent<T>() ?? AddComponent<T>()`，Unity “fake null” 曾导致 `MissingComponentException`。
- docs/18 起 MVP 试玩画面由 `MvpWorldModule` 密集小模块装配：`Assets/ArtSource/MVP/dense_pixel/layouts/{town,inn}.json` 声明 placements（asset/x/y/layer/sortingOrder/role），`MvpSceneModuleAssembler` 在两个 Demo 场景放置 `Assets/Art/MVP/dense_pixel/environment/` 下 ≤64×64 持久精灵，排序映射 `Ground→Default/-100`、`Environment→Environment`、`Foreground→Foreground`；掌柜/两水匪为 48×48、荷包 16×16（`Resources/Art/MVP/dense_pixel/actors/`，`MvpArtCatalog` dense 优先、旧 `Art/MVP/` 根兜底）。docs/18 §3 的 v2 三层整屏资源（`Assets/ArtSource|Art/Environment/MVP/v2/`）与 `CreateMvpSceneLayers` 暂作 §6.C 过渡回滚点保留，Gate R1 批准前禁止删除。离屏截图必须**先**绑定 480×270 `RenderTexture`，再刷新 `PixelPerfectCamera`，否则 Unity 会把视口按编辑器 Game View 钳制并在两侧留下清屏色。
- 主菜单与烟柳镇实际渲染基线见 `Assets/Art/Characters/Player/previews/main-menu-character-selection.png`、`Assets/Art/Environment/previews/demo-yanliu-gameplay.png`。

## 4. 开发与测试流程

1. 修改代码和对应测试。
2. 运行相关 EditMode / PlayMode 测试。
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

PlayMode 测试把 `-testPlatform` 改为 `PlayMode` 并使用独立结果文件。`-runTests` 时不要传 `-quit`，否则可能在结果写出前退出。`-executeMethod` 场景重建则必须带 `-quit`，否则批处理编辑器会常驻。当前全量基线为 EditMode 139/139、PlayMode 14/14、Python 52/52（docs/18 §6.B 完成态，2026-08-29）；证据 XML 必须晚于其证明的提交时间。

美术确定性验证：

```bash
python3 -m unittest discover -s tools/art_pipeline/tests -v
python3 -m tools.art_pipeline.build --all   # 当前应 built=0 skipped=219
python3 -m tools.art_pipeline.validate --all
```

若批处理日志出现 `Unsupported protocol version '1.18.1'` 或许可证连接挂起，先退出 Unity Hub，再运行批处理；测试结束后可重新打开 Hub。不要同时保留陈旧的 Unity 批处理进程。

## 5. 已知问题与未完成项

### P0

当前没有已知的运行时阻塞 P0。新增功能后仍需在 Unity Play 中做端到端验证。

### P1 — 数据与内容

- v5 已保存主角外观、武器流派与活跃任务，但尚未保存敌人状态、唯一拾取物、一次性事件、区域标志和其他世界状态（读档后 Demo 敌人与门控对象按场景初始状态重置，任务进度仍按存档恢复）。
- `MVP_01` 已在 Demo/客栈全链路接线；`M01_01`–`M01_05` 运行时模板已完成但烟柳镇场景尚未为其配置 `QuestGiver` 与目标，属阶段二内容。
- docs/15 冻结项仍然有效：其余 11 套主角外观、85 个非 MVP 角色、10 户外、12 个非 inn 室内与批量美术流水线保持原样。MVP 美术例外（docs/18 §3）为：固定男主 48×48 可动画源帧、两张 Demo 的密集小模块与 v2 三层过渡资产，及掌柜/两水匪/荷包静态精灵（48px 人物/16px 荷包，旧 32px `Resources/Art/MVP/` 根保留兜底）；此前 7 张功能小图继续保留。所有例外均经持久资源加载，禁止运行时生成；正式角色/环境美术零改动。
- 正式 97 角色和 23 环境已完成确定性第一版，但仍需要人工精修表情、攻击动作节奏、地标细节和区域独特构图。
- 角色 Controller/动画资源已生成；当前基础状态切换仅完整覆盖 down 向 idle/walk，四方向 BlendTree 和所有动作的运行时过渡仍可继续深化。
- 物品/任务主要由代码表和 Markdown 设计稿提供，正式 `.asset` 资源仍待制作。

### P2 — 增强

- 10 个户外区域和 13 个室内场景资源/骨架均已生成，但烟柳镇之外仍需填入玩法内容、传送关系和剧情对象。
- 商店 UI、武学技能树、小地图、BOSS 战待实现。
- BGM/SFX 仍为空或占位；同一缺失资源只警告一次，避免脚步音刷屏。

## 6. 文件地图

```text
yuanHaiLu/
├── Assets/
│   ├── Scripts/                 75 个运行时/编辑器 .cs
│   ├── Tests/EditMode/          139 个测试用例
│   ├── Tests/PlayMode/          14 个测试用例
│   ├── ArtSource/               稳定 PNG/JSON、模块、布局、清单
│   ├── Art/                     97 角色 + 23 环境输出和验收图
│   ├── Prefabs/Characters/      97 个正式 Prefab
│   ├── AnimatorControllers/     97 个正式 Controller
│   ├── Tilemaps/Formal/         23 套持久 Tile 资产
│   ├── Scenes/                  MainMenu + 2 Demo + 10 Regions + 13 Interiors + 3 Showcase
│   └── Resources/Art/           两个正式目录资产
├── docs/
│   ├── 01-art-style-guide.md
│   ├── 02-story-design.md
│   ├── 15-single-hero-mvp-design.md
│   └── 18-dense-pixel-mvp-implementation-handoff.md
├── tools/art_pipeline/          确定性美术 baker/validator（52 测试）
├── ProjectSettings/             修改后需重启 Unity
├── Packages/manifest.json
├── CLAUDE.md                    Claude Code 项目指引
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
18. 建立 EditMode 测试程序集和 14 个回归测试。
19. 审查阶段修复返回主菜单状态、无效新游戏场景提前清档、装备上限资源裁剪和直接 Play Demo 被锁在主菜单状态的问题。
20. 统一项目初始化工具的 Layer/SortingLayer 基线，并把现有 Sprites/Tilesets 固定为 PPU 16、Point、无压缩导入。

### 第四批：Unity 实机冒烟与呈现链路

21. 主菜单 Canvas 补 `GraphicRaycaster`，并默认选择“新游戏”，鼠标和键盘 Enter 均可进入 Demo。
22. 像素摄像机增加全屏清底摄像机，修复场景切换后视口外残留主菜单画面。
23. 修复未配置摄像机边界的反向 Clamp，并把 Demo 主摄像机/生成器的 Z 位置改为 `-10`，世界精灵恢复可见。
24. 玩家、战斗、敌人和 NPC 在 Animator Controller 缺失时跳过参数写入，消除每帧告警刷屏。
25. `PauseMenu` 在引用为空时自动生成可见且可点击的继续、保存、返回主菜单面板。
26. 音频管理器缓存缺失 BGM/SFX 结果，同一占位音效只警告一次。
27. 移除项目未使用且已停止支持的 Unity IAP 4.15，启动时不再弹 Package Errors；同步清理重复 using、过期查找 API 和未使用字段告警。
28. 测试扩展为 21 个 EditMode + 1 个 PlayMode；最终两组测试均全量通过。

### 第五批：任务运行时与 v3 持久化

29. 新增 `QuestDatabase`，用稳定 ID 提供 `M01_01`–`M01_05` 模板，并允许 Resources 同 ID 覆盖；`ActiveQuest` 深复制目标，模板不再被运行时污染。
30. 存档升级为 v3，保存活跃任务、目标进度、状态、接取时间和已完成 ID；v2 迁移为空活跃任务，基础属性恢复顺序保持兼容。
31. `QuestManager.CompleteQuest` 改为幂等结算；经验、金钱、物品和武学依赖分别处理，单一依赖缺失不再吞掉其他奖励。
32. 新增 `QuestTarget`，并接通区域、拾取、首次学习等真实目标来源；区域在目标尚未激活时会重试，成功上报后才锁定。
33. 新增非交互候选的 `QuestGiver`，由 `NPCBase` 委托；接取、交谈推进和提交只在对应对话结束后执行，忙碌对话不会串台。
34. 审查阶段补齐未知存档目标/越界进度警告和真实 MonoBehaviour 任务链 PlayMode 测试。
35. 最终验证：43/43 EditMode、2/2 PlayMode、批处理编译均通过；阶段差异已按规格与代码标准双轴审查。

### 第六批：完整角色/环境美术与 v4 主流程接入（2026-08-13）

36. 建立 `tools/art_pipeline` 确定性像素流水线：schema、规范色板、模块合成、baker、SHA-256 校验和 34 个 Python 测试。
37. 完成 97 套独立角色资源：12 主角、15 Named、36 NPC、24 Enemy、10 Boss；自动生成精确切片、97 Controller、97 Prefab 和目录资产。
38. 完成 10 户外 + 13 室内配方、布局、地标、持久 Tile 与 23 个 7 层 Tilemap 场景；Build Settings 自动包含 25 个可构建场景。
39. 烟柳镇 Demo 改为从正式 `Assets/Scenes/Regions/yanliu.unity` 克隆，叠加玩家、剧情 NPC、敌人、事件、碰撞与 UI；删除生成器中的地面/角色色块回退。
40. 主菜单新增 2 性别 × 6 职业选择、正式 idle 预览和选中态；`PlayerAppearanceBinder` 保证菜单→Demo 与后续切场景不丢外观。
41. 存档升级 v4，保存 `playerArtId`；v1–v3 和非法外观 ID 安全迁移为 `player_female_swordsman`。
42. 修复 Unity 6 批处理场景中逐格 `Tilemap.SetTile` 未序列化：改用批量 `SetTiles`，增加地面数量和 Buildings 结构层回归断言。
43. 修复 `CharacterVisual` 使用 `??` 遇到 Unity fake-null 时无法创建 Animator 的 `MissingComponentException`。
44. 修复 `PlayerCombat.Update()` 在 GameManager 引导前/销毁期空引用；无管理器时安全等待。
45. 增加主菜单与烟柳镇离屏实际渲染验收图，并由截图发现/修复动画整表拉花、RectTransform 偏移、AspectRatioFitter 覆盖尺寸、菜单文字裁剪等视觉问题。
46. 最终验证：81/81 EditMode、6/6 PlayMode、34/34 Python；`build --all` 为 `built=0 skipped=120`，全资产校验通过。

### 第七批：正式美术构图与视觉证据（2026-08-14）

47. 户外布局改由明确 `scenery` 与地标坐标直接写入 10 份 Layout JSON；市场、山寺、运河、烽火台、竹祠、冰湖、宗祠、遗迹、剑宗与碑林不再共享一排地标构图。
48. 环境 source builder 为各区域生成专属植被/地形簇与叙事地标轮廓；环境仍是纯 2D、16×16 Tile，角色全部维持 32×32 帧。
49. 序章 `RegionEnvironmentController` 只替换 normal/burned 的 Tile 与地标精灵；碰撞、锚点和场景 ID 不变，天气精确为 `clear` / `ember_wind`。
50. 新增 `VisualRegressionCapture`：固定 480×270 截图，在 finally 中恢复活动场景、Canvas、相机目标、RenderTexture 和抗锯齿；临时审查图输出到 `/private/tmp/yuanhailu-art-review/`，尚非用户人工批准的仓库基线。
51. 实际验证：Python 45/45、EditMode 101/101、PlayMode 7/7；`build --all` 为 `built=0 skipped=121`，全资产校验通过。

### 第八批：单主角 MVP 垂直切片（2026-08-14，docs/15）

52. 新增 `WeaponStyle`（sword/gauntlets/dart）不可变配置表：稳定 ID、非法回退 sword、近战档案与各自主动技（剑气斩/冲拳/回风三镖）；`MartialSkill` 支持多投射物扇形与位移伤害；`MartialSkillDatabase.Add` 收 `SkillSpec` 配置对象。
53. 主角固定男主 `player_male_swordsman`（其余 11 套外观资产保留不动）；主菜单只留新游戏/继续游戏 + 三流派按钮，预览恒为同一男主。
54. 存档升级 v5：新增 `weaponStyleId`；v1–v4 迁移为男主+sword，非法流派回退 sword。
55. `PlayerCombat` 全部战斗参数（射程/判定盒/连击数/攻击时长/伤害系数/斩击色）由 `WeaponStyleId` 驱动，支持运行时 `OnWeaponStyleChanged` 热切换。
56. 新增 `MVP_01` 河岸失物顺序任务与 `sequentialObjectives` 顺序门；Demo 河岸子区（ReachArea + 2 名水匪 + 荷包拾取）与客栈门接线。
57. 新增 `InnSceneGenerator` 与 `Demo_Inn.unity`：客栈室内掌柜老赵（接取/提交）、回镇出口与完整 UI；`GameManager.TransitionCarry` 保证往返不丢等级/属性/武学。
58. 修复三处既有缺陷：冷却字典迭代中写回崩溃、`EffectsManager` 静态快捷方法 `?.` 遇 fake-null 崩溃、任务重复目标对恢复相互覆盖。
59. `AreaTrigger` 任务上报先于一次性地名判定；Build Settings 扩为 26 场景（含 Demo_Inn）。

### 第九批：复审修复与干净分支重建（2026-08-17）

60. 复审 P0：新增 `QuestStageGate`，河岸水匪/荷包只在 MVP_01 对应顺序步骤激活，杜绝"接任务前清空敌人/拾走荷包导致任务永久软锁"；配套 PlayMode 回归（QuestStageGatePlayModeTests：乱序不可能消耗、按序可完整完成）。
61. 复审 P1：出生点由地图外 (0,-5) 改为客栈门外可达格 (7.5,7.6)；客栈回镇落点与门触发盒不再重叠（原 (7.5,8.6) 落地即重叠、会被立刻传回客栈）；新增出生点/客栈门/河岸/荷包 BFS 可达性接线测试与 MainFlow 出生点运行时断言。
62. 复审 P1：`MartialArtsSystem` 远程技能 cast_burst 特效补显式 `== null`；两个玩法生成器的管理器/摄像机/玩家/UI 装配统一提取到 `PlaySceneAssembler`。
63. 复审发现并修复门控与协程生命周期的冲突：`ItemPickup` 弹出/延迟协程改在 `OnEnable` 启动，失活→激活循环后仍可拾取。
64. 复审 P2：`WeaponStyle` 九组 switch 收敛为不可变配置表；`MartialSkillDatabase` 改 `SkillSpec` 配置对象。
65. 冻结范围整改：从 main 重建 `codex/single-hero-mvp-v2`，只包含 docs/15 + MVP 代码/测试/场景；不改任何美术资产、正式场景基线与 `VisualRegressionCapture`（后者在 main 上不含审查角色功能）；唯一 ProjectSettings 变更为 EditorBuildSettings 加入 Demo_Inn（文档化例外）。
66. 最终验证：EditMode 125/125、PlayMode 11/11、Python 45/45（main 基线）。

### 第十批：MVP 呈现与流程可读性返工（2026-08-22，docs/16）

67. 两个 `Demo_*` 场景改为固定 30×16.875 玩法视口，接入持久 480×270 MVP 背景；HUD 压缩到左上状态条、底部技能栏与右上金币，主角改为可读的四向 32×32 男主帧。
68. 修复出生点→客栈门碰撞立面封死的路径，拆出真实门洞；掌柜移到柜台前沿可交互位；直开客栈/烟柳镇时由 `MvpDirectPlayFallback` 进入探索状态。
69. `VisualRegressionCapture` 隔离附加场景并在绑定 RenderTexture 后刷新像素相机，杜绝叠景和左右清屏色边带；河岸审查图临时展示任务门控对象后完整恢复。
70. 新增全帧离屏截图、紧凑 HUD、直开回退、相机边界与门洞路线回归；最终人工视觉批准与三流派完整试玩仍是合并门禁。

### 第十一批：MVP 原生像素美术整合（2026-08-22，docs/17）

71. 淘汰 Demo 中“高密度整图概念背景 + 旧角色贴片”的混搭，烟柳镇与客栈改为 `Ground → Environment → Character → Foreground` 原生 480×270 像素层；玩家重画为靛蓝短披、米白内衫、朱砂腰绦与钢剑，掌柜/水匪/荷包切换为同调色板 32×32 持久精灵。
72. 两个 Demo 的碰撞、任务锚点与可走路线保持原坐标；视口外旧镇 NPC 和冻结角色不再混入试玩画面。`build --all` 纳入 MVP 层与静态精灵的确定性构建。
73. 验证：Python 48/48、EditMode 138/138、PlayMode 14/14；三张 480×270 实拍写入 `/private/tmp/yuanhailu-mvp-rework-review/`，仍需用户按 1× 画面做最终视觉验收。

### 第十二批：冗余与历史产物清理（2026-08-23）

74. 全项目引用盘点后删除已被双重取代（docs/17 三层 → docs/18 模块化）的 v1 整屏背景 `Assets/Art/Environment/MVP/mvp_{yanliu,inn}_backdrop.png` 与 AI 概念图源 `Assets/ArtSource/Environment/MVP/*_concept_v1.png`（含 `.meta`，共约 4.9MB）；删除前逐一核实：两个 Demo 场景无 GUID 引用、Scripts/Tests 无路径引用、`build.py` 已在 181fd79 移除其构建注册。
75. 删除孤立的 `tools/art_pipeline/mvp_backdrop_builder.py` 与 `test_mvp_backdrop_builder.py`（除自身测试外零引用）；删除 `docs/16-thumbnails/` 全目录（缩样审批流已被实际实施取代，且脚本依赖被删除的 v1 背景图；历史记录保留在 22ce274/057c7c8 两个提交）。
76. docs/18 §3 保护清单全部保留不动：v2 三层资产、`mvp_scene_layer_builder.py`、`PlaySceneAssembler.CreateMvpSceneLayers` 与整屏层测试仍是 `MvpWorldModule` 转绿前的过渡回滚点；`source_audit.py` 有活跃引用同样保留；C# 侧零改动（Unity 许可证阻塞期间不引入无法编译验证的变更）。
77. 验证（独立 worktree 于提交态 `1e14b52` 复核）：Python 48/48、`build --all` built=0 skipped=131、`validate --all` 通过；在途未提交的 48px 男主/dense 模块脏文件不受影响（其 Python 侧实跑 52/52）。
78. 同步修正 AGENTS.md 内部不一致：速览与 §4 测试基线统一为 138/14/48、代码规模 78+28、文件地图与设计文档清单补 docs/16–18 现状。

### 第十三批：历史代码与冗余脚手架清理（2026-08-23）

79. 全工程交叉引用盘点后删除 3 个孤立/未接入的历史脚本（含 .meta）：
    - `Assets/Scripts/Core/SceneBootstrapper.cs`：已被 `GlobalSystemsBootstrapper` 和 `PlaySceneAssembler` 取代，场景和测试中零引用；
    - `Assets/Scripts/Combat/DamageCalculator.cs`：早期预留的伤害公式计算器，实际战斗由 `PlayerCombat` 与 `CharacterStats` 直接结算，从未接入；同步清理空目录 `Assets/Scripts/Combat/`；
    - `Assets/Scripts/System/ShopManager.cs`：早期商店管理器脚手架与空预设数据，当前阶段无 UI 或场景接入。
80. 删除历史设计过程临时目录 `docs/superpowers/`（草稿已由 docs/18 等正式文档收敛覆盖）。
81. 同步更新 `AGENTS.md`、`SETUP_GUIDE.md`、`docs/04`、`docs/05` 中的引用描述与代码总数（78 → 75）。

### 第十四批：docs/18 §6 模块化装配与许可证恢复（2026-08-29）

82. 用户恢复 Unity `6000.4.10f1` 许可证后完成 §6.A 复核：48px 男主重建产物（`.png.meta` 精确切片、`.controller` 骨骼引用、`formal-character-build.txt` 哈希行）随 f7c1ba5 入库；全量 EditMode 139/139 全绿，测试重序列化噪声按惯例还原。
83. §6.B 按严格 TDD 执行：先写红的结构测试 `DemoScenesAssembleDensePixelModulesThroughMvpWorldModule`（两 Demo 必须各有恰好一个 `MvpWorldModule`、不得再存在 `[MVP Ground]/[MVP Environment]/[MVP Foreground]` 整屏对象、装配精灵全部为持久资产且 ≤64×64），再实现运行时 `MvpWorldModule` 契约组件与编辑器 `MvpDenseSceneLayouts`/`MvpSceneModuleAssembler`，按 `Assets/ArtSource/MVP/dense_pixel/layouts/{town,inn}.json` 放置模块并映射 `Ground→Default/-100`、`Environment→Environment`、`Foreground→Foreground`，最后才改造两个生成器与 `PlaySceneAssembler`。
84. 密集演员接入：`PlaySceneAssembler.ConfigureDenseActorSprite` 配置 48px 掌柜/两水匪与 16px 荷包的导入契约（荷包 1× 缩放等价旧 32px 半缩放占地）；`MvpArtCatalog` 改为 `dense_pixel/actors/` 优先、旧 `Art/MVP/` 根兜底，掉落/武器小图不受影响；碰撞、任务锚点与玩法坐标零变化。
85. 顺带项目清理：删除 `DemoSceneGenerator` 中约 170 行占位时代死代码（`CreateMapGround`/`DrawGroundTiles`/`CreateMapWalls` 等，全部依赖被禁的运行时 `Sprite.Create`）。
86. 截图回归前提按稀疏构图重写为 `GameplayCaptureFillsTheFullLogicalFrameWithHudAndWorldContent`：断言精确 480×270、顶部 HUD 带横贯逻辑宽度（Unity 视口钳制回归的哨兵——钳制时 HUD 会一并缩进中段）与内容占比下限；稀疏留白密度本身交 Gate R1 人工把关。
87. 最终验证：EditMode 139/139、PlayMode 14/14、Python 52/52、`build --all` built=0 skipped=219、`validate --all` 通过；三张 480×270 1× 复核图输出 `/private/tmp/yuanhailu-mvp-rework-review/` 待 Gate R1；§6.C（删除 v2 三层/`mvp_scene_layer_builder.py`/`CreateMvpSceneLayers`/旧图层测试及 `build.py` 注册）在用户批准 1× 截图前禁止执行。

### 第十五批：死代码与冗余资产清理（2026-09-02）

88. 全工程引用盘点（脚本 GUID 全 Assets 扫描 + 类名全代码扫描双重验证，HUD/PauseMenu/DialogueUI/MainMenu 等作有引用对照）后删除零引用死代码与冗余资产：
    - C#（含 `.meta`）：`Assets/Scripts/UI/InventoryUI.cs`、`Assets/Scripts/UI/QuestUI.cs`、`Assets/Scripts/Map/TileMapManager.cs` — 场景/prefab/asset/代码全部零引用，`PlaySceneAssembler` 从不创建背包/任务日志 UI；`LevelSystem.cs` 的 `CharacterStatsExtensions` 整类删除（`Heal`/`RestoreMp` 被 `CharacterStats` 同名实例方法遮蔽、从未被扩展形式调用，`OnKillEnemy` 零调用）；`InventoryManager.cs` 的 `ItemDataExtensions` 整类删除（唯一调用者是已删的 InventoryUI）。
    - Python：`environment_modules.py`（零引用）、`character_modules.py`（仅 `source_audit` 一行包装；`test_character_roster.py` 改为直调 `assert_character_sources_complete`，测试数不变）、`generate_precision_showcase.py`（零引用的一次性展示图生成器）。
    - 资产：`Assets/MobileDependencyResolver/`（EDM4U 1.0MB 26 文件，零代码引用；2132bdc 删除后被 7187d43 误加回，经用户确认再次删除）；`Assets/Art/MVP/previews/` 8 张 chibi 重制前的第三方参考/过程展示图（零 GUID 引用；保留 6 张 Gate R1 捕获图）。
    - 本地未跟踪垃圾：`Logs/`、`YuanHaiLu*.csproj`、`.zcode/plans/` 过期会话计划。
    - docs/18 §6.C 冻结项（v2 三层资产、`mvp_scene_layer_builder.py`、`CreateMvpSceneLayers`、整屏层测试）按用户决定保持不动，待 Gate R1 批准。
89. 文档同步至 v5/docs-15 现状：README.md 与 SETUP_GUIDE.md 重写 v3/v4 时代描述（删 12 外观/2 性别×6 职业选择与 Tab 背包/Q 任务日志操作项，改固定男主+三武器流派；25→26 场景；101+7+45→139+14+52；代码计数 68→75、测试文件 19→28；结构树去 Combat/ 已删脚本；build 预期 skipped=219；编辑器工具表对齐现存 21 个菜单项）；AGENTS.md 文件映射去已删脚本、docs 清单改为现存 01/02/15/18、§10 重写为外部 AI 分支约束；CLAUDE.md 首次入库。

## 8. 当前人工 QA 清单

自动测试不能替代 Play 验证。docs/15 验收标准第 6 条要求**三种流派各完成一次完整试玩**，当前状态：**待执行**（需可见 Unity 窗口人工操作；自动侧已由 EditMode/PlayMode 全覆盖）。每条流派（剑/拳套/暗器）各跑一遍下面的完整链路：

1. 主菜单选中该流派 → 确认预览恒为同一男主、武器小图为该流派 → 新游戏。
2. 出生在客栈门外 (7.5, 7.6)；先去河岸确认无水匪/荷包（阶段门关闭），进客栈向掌柜接取 MVP_01。
3. 回镇 → 河岸杀 2 水匪（感受该流派攻击距离/连击节奏/主动技差异）→ 拾荷包 → 回掌柜提交。
4. 击杀后金币即时入账，地面铜钱为短命视觉反馈（约 1.2 秒淡出），无卡脚碰撞；物品掉落可正常拾取。
5. 保存 → 退出到主菜单 → 继续游戏，恢复该流派与全部进度。

通用检查（任一流派跑一遍即可）：

- 烟柳镇 ↔ 客栈往返不卡门（落地不会立即被传回），等级/HP/MP/武学不重置。
- Demo 世界、HUD 和像素视口正确显示，场景切换无残影。
- WASD、J、Shift 和 ESC 可用；暂停时应显示默认暂停面板。
- v5 存档往返精确恢复外观、流派、位置、HP/MP、背包、装备、金钱、武学、活跃任务进度和已完成任务；v1–v4 旧档载入为男主+长剑。
- 读档不发初始物资、不覆盖位置；卸装后属性正确。
- 后续场景加载不会重复应用旧存档。

2026-08-13 已通过真实 Unity 相机离屏渲染验收主菜单与正式烟柳镇；主菜单→选择男拳师→Demo 外观保持由端到端 PlayMode 覆盖。三流派完整人工试玩（2026-08-20 复审 Spec-P1 提出）尚未执行，完成前不满足合并门禁。

## 9. 推送到 GitHub（尚未执行）

```bash
git remote add origin git@github.com:<用户名>/yuanHaiLu.git
git branch -M main
git push -u origin main
```

## 10. 外部 AI 分支约束

- `main` 是唯一权威基线；远程历史 `codex/*` 分支（外部 AI 美术产出）与 `main` 没有共同祖先，禁止直接 merge；如需取用素材，迁移到从最新 `main` 创建的新分支再搬运。
- 不得覆盖 `QuestTarget` 成功推进后才锁定的修复；不得夹带 `ProjectSettings` 平台噪声。
- 外部产出不得自行宣告合并就绪；必须提供固定提交、测试 XML/日志、视觉截图和人工 QA 证据，再独立复验。
