# AGENTS.md — 渊海录项目交接与记忆

> 本文件是接手本项目（开发者或 AI 助手）的首选入口。长期事实以本文件为准。
> 最后更新：2026-08-13

## 0. 30 秒速览

| 项 | 内容 |
|----|------|
| 项目 | 渊海录（YuanHaiLu）— Unity 6 像素武侠 RPG（俯视角 2D） |
| 引擎 | Unity `6000.4.10f1`（2D Core / 内置 2D，**不是 URP**） |
| 平台 | macOS Apple Silicon（可扩 PC/WebGL/移动） |
| 代码规模 | 68 个运行时/编辑器 C# 文件；另有 19 个测试/测试工具文件 |
| 状态 | 正式美术第一阶段完成：97 角色、10 户外、13 室内、烟柳镇可玩 Demo |
| 版本控制 | Git，默认分支 `main`；`.gitignore` 已配置 |
| 测试 | 81 EditMode + 6 PlayMode + 34 Python 全通过 |
| 设计/交接 | `docs/01-art-style-guide.md`、`docs/02-story-design.md`、`docs/03-art-production-handoff.md` |

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
2–11  Assets/Scenes/Regions/*.unity
12–24 Assets/Scenes/Interiors/*.unity
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
  GameConfig / GameManager / PlayerAppearance / SceneBootstrapper / CameraFollow / PixelPerfectCamera

YuanHaiLu.Art
  CharacterArtCatalog / EnvironmentArtCatalog / CharacterVisual
  PlayerAppearanceBinder / RegionSceneDefinition / ArtAssetId

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
  QuestDatabase / QuestManager / QuestGiver / QuestTarget
  MartialSkillDatabase / ShopManager / LootTable
  AudioManager / GameTimeManager / ScreenTransition / PlayerDeathHandler

YuanHaiLu.UI
  HUD / MainMenu / InventoryUI / QuestUI / PauseMenu / DialogueUI

YuanHaiLu.Editor
  DemoSceneGenerator / MainMenuSceneGenerator / PixelArtImporter
  ProjectInitializer / SetupBuildSettings / ArtImportRules / ArtAssetValidator
  CharacterAnimationBuilder / RegionSceneBuilder / EnvironmentTileBuilder
  CharacterShowcaseGenerator / EnvironmentShowcaseGenerator / FormalSceneCapture

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
- `CharacterArtCatalog` 和 `EnvironmentArtCatalog` 以稳定 snake_case ID 作为运行时唯一入口；正式场景不得创建运行时 Texture/Sprite 作为美术回退。
- `PlayerAppearance` 固定 2 性别 × 6 职业，`PlayerAppearanceBinder` 在场景加载后重新应用持久选择。
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
  → v4 恢复正式主角外观；v1–v3 缺失外观时迁移为 player_female_swordsman
  → 状态切到 Exploration
  → SceneEntryMode.Active
```

`SaveData.saveVersion == 4` 是当前格式；基础属性仍按 v2 语义恢复，任务按 v3 语义恢复，外观按 v4 语义恢复。不要降低版本号或改变既有字段含义。

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

- `QuestDatabase` 提供 `M01_01`–`M01_05` 五个稳定代码模板；`Resources/Quests` 下同 ID 的 `QuestData` 可覆盖代码模板。
- `ActiveQuest` 深复制模板目标，模板只提供显示和奖励数据，运行时进度不得写回模板。
- `QuestManager` 是接取、目标推进、提交、奖励与 v3 序列化的唯一权威；损坏进度会钳制并警告，未知模板/目标会跳过并警告。
- `QuestGiver` 与 `NPCBase` 同物体配置，但不实现 `IInteractable`；任务行为只在它启动的对话结束后结算。
- `QuestTarget`、`AreaTrigger`、`ItemPickup` 和 `MartialArtsSystem` 只在真实成功行为后上报进度；重复死亡、区域、拾取或学习不会重复计数。

### 3.7 正式美术流水线

- 可编辑源位于 `Assets/ArtSource/`；烘焙结果位于 `Assets/Art/`，两者都提交 Git。
- `tools/art_pipeline/` 只用确定性像素模块、清单和规范色板生成资源；当前范围为 97 个角色与 23 个环境配方。
- 每个输出带 `.art.json`、稳定帧名、pivot、SHA-256；`ArtImportRules` 精确切片，`ArtAssetValidator` 检查哈希/尺寸/持久资源。
- 角色分类固定：12 Player、15 Named、36 NPC、24 Enemies、10 Bosses。
- 环境固定：10 Regions（tianshu/cangyue/yanliu/chisha/youhuang/hanyuan/prologue_village/luoyuan/jueyun/zhenyue）和 13 Interiors（inn/residence/shop/pharmacy/academy/yamen/palace/temple/cave/tomb/dungeon/military_camp/ship_cabin）。
- `CharacterVisual.Apply` 中 UnityEngine.Object 的组件检查必须用显式两段 `== null`；禁止 `GetComponent<T>() ?? AddComponent<T>()`，Unity “fake null” 曾导致 `MissingComponentException`。
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

PlayMode 测试把 `-testPlatform` 改为 `PlayMode` 并使用独立结果文件。`-runTests` 时不要传 `-quit`，否则可能在结果写出前退出。当前全量基线为 EditMode 81/81、PlayMode 6/6、Python 34/34。

美术确定性验证：

```bash
python3 -m unittest discover -s tools/art_pipeline/tests -v
python3 -m tools.art_pipeline.build --all   # 当前应 built=0 skipped=120
python3 -m tools.art_pipeline.validate --all
```

若批处理日志出现 `Unsupported protocol version '1.18.1'` 或许可证连接挂起，先退出 Unity Hub，再运行批处理；测试结束后可重新打开 Hub。不要同时保留陈旧的 Unity 批处理进程。

## 5. 已知问题与未完成项

### P0

当前没有已知的运行时阻塞 P0。新增功能后仍需在 Unity Play 中做端到端验证。

### P1 — 数据与内容

- v4 已保存主角外观与活跃任务，但尚未保存敌人状态、唯一拾取物、一次性事件、区域标志和其他世界状态。
- `M01_01`–`M01_05` 运行时模板已完成，但烟柳镇现有场景尚未配置对应 `QuestGiver`、区域目标、敌人目标和任务物品；这是阶段二内容接入任务。
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
│   ├── Scripts/                 68 个运行时/编辑器 .cs
│   ├── Tests/EditMode/          81 个测试用例
│   ├── Tests/PlayMode/          6 个测试用例
│   ├── ArtSource/               稳定 PNG/JSON、模块、布局、清单
│   ├── Art/                     97 角色 + 23 环境输出和验收图
│   ├── Prefabs/Characters/      97 个正式 Prefab
│   ├── AnimatorControllers/     97 个正式 Controller
│   ├── Tilemaps/Formal/         23 套持久 Tile 资产
│   ├── Scenes/                  MainMenu + Demo + 10 Regions + 13 Interiors
│   └── Resources/Art/           两个正式目录资产
├── docs/
│   ├── 01-art-style-guide.md
│   ├── 02-story-design.md
│   └── 03-art-production-handoff.md
├── tools/art_pipeline/          确定性美术 baker/validator（34 测试）
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

## 8. 当前人工 QA 清单

自动测试不能替代 Play 验证。涉及本批改动时至少检查：

- 主菜单新游戏能进入 Demo。
- Demo 世界、HUD 和像素视口正确显示，场景切换无残影。
- K/E NPC 对话、手动事件和传送点可达。
- WASD、J、Shift 和 ESC 可用；暂停时应显示默认暂停面板。
- 自动事件不显示交互提示，一次性事件不会再次成为目标。
- v4 存档往返精确恢复外观、位置、HP/MP、背包、装备、金钱、武学、活跃任务进度和已完成任务。
- 读档不发初始物资、不覆盖位置；卸装后属性正确。
- 后续场景加载不会重复应用旧存档。

2026-08-13 已通过真实 Unity 相机离屏渲染验收主菜单与正式烟柳镇；主菜单→选择男拳师→Demo 外观保持由端到端 PlayMode 覆盖。因最终 GUI 控制时 Mac 处于锁屏，本轮没有补做可见 Unity 窗口鼠标点击；解锁后仍建议按上表做 2 分钟人工操作复核。

## 9. 推送到 GitHub（尚未执行）

```bash
git remote add origin git@github.com:<用户名>/yuanHaiLu.git
git branch -M main
git push -u origin main
```
