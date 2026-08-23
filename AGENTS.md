# AGENTS.md — 渊海录项目交接与记忆

> 本文件是接手本项目（开发者或 AI 助手）的首选入口。长期事实以本文件为准。
> 最后更新：2026-08-13

## 0. 30 秒速览

| 项 | 内容 |
|----|------|
| 项目 | 渊海录（YuanHaiLu）— Unity 6 像素武侠 RPG（俯视角 2D） |
| 引擎 | Unity `6000.4.10f1`（2D Core / 内置 2D，**不是 URP**） |
| 平台 | macOS Apple Silicon（可扩 PC/WebGL/移动） |
| 代码规模 | 71 个运行时/编辑器 C# 文件；另有 20 个测试/测试工具文件 |
| 状态 | 正式美术第一阶段完成：97 角色、23 场景、完整旅行图、烟柳镇可玩 Demo |
| 版本控制 | Git，默认分支 `main`；`.gitignore` 已配置 |
| 测试 | 92 EditMode + 10 PlayMode + 35 Python 全量基线 |
| 设计/交接 | `docs/01-art-style-guide.md`、`docs/02-story-design.md`、`docs/HANDOFF-art-production.md`、`docs/03-art-production-handoff.md`、`docs/superpowers/` |

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
  PlayerAppearanceBinder / RegionSceneDefinition / RegionEnvironmentController / ArtAssetId

YuanHaiLu.Character
  PlayerController / PlayerCombat / PlayerInteraction / CharacterStats
  EnemyAI / NPCBase / MartialArtsSystem / MartialSkill / LevelSystem / CharacterAudio

YuanHaiLu.Effects
  EffectsManager（特效池、命中火花、伤害数字、剑气、屏闪）

YuanHaiLu.Map
  TileMapManager / AreaTrigger / TeleportPoint / Destructible
  ItemPickup / EventTrigger / SceneDirector / FormalSceneTravelGraph

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
- 玩家与敌人战斗判定帧分别由 Animator 事件调用 `PlayerCombat.OnAttackHitFrame()` 和 `EnemyAI.OnAttackHitFrame()`；正式 Controller 已接通四方向 idle/walk/dash/三段攻击。
- `PlayerInteraction` 扫描最近的 `IInteractable`，受 `GameManager.CanPlayerAct()` 控制。
- `CharacterArtCatalog` 和 `EnvironmentArtCatalog` 以稳定 snake_case ID 作为运行时唯一入口；正式场景不得创建运行时 Texture/Sprite 作为美术回退。
- `PlayerAppearance` 固定 2 性别 × 6 职业，`PlayerAppearanceBinder` 在场景加载后重新应用持久选择。
- 正式环境由 `RegionSceneBuilder` 生成 7 层 Tilemap；必须用批量 `SetTiles` 后保存，逐格 `SetTile` 在 Unity 6 批处理路径曾出现未序列化问题。
- `FormalSceneTravelGraph` 维护 23 个正式场景的稳定旅行关系；`SceneBootstrapper` 根据 `NewGame`/`LoadGame`/`SceneTransition` 决定默认出生、保留存档位置或使用传送锚点。

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
  → v4 恢复正式主角外观；v1–v3 缺失外观时迁移为 player_male_swordsman
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
- 24 个普通敌人必须与 `tools/art_pipeline/character_roster.py` 和 `Assets/ArtSource/Characters/Manifests/enemy-roster.json` 的确认名单完全一致；Python 与 Unity validator 都会拒绝缺失、多余或错位 ID。
- 每个正式场景必须消费布局 JSON 的 `layers/collisions/foregroundSpans`，保留 Buildings 真实碰撞形状、Foreground 遮挡、昼夜标记、动态 weather ID 和 entry/exit/interior 锚点。
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

PlayMode 测试把 `-testPlatform` 改为 `PlayMode` 并使用独立结果文件。`-runTests` 时不要传 `-quit`，否则可能在结果写出前退出。当前全量基线为 EditMode 92/92、PlayMode 10/10、Python 35/35。

美术确定性验证：

```bash
python3 -m unittest discover -s tools/art_pipeline/tests -v
python3 -m tools.art_pipeline.build --all   # 当前应 built=0 skipped=120
python3 -m tools.art_pipeline.validate --all
```

视觉回归位于 `Assets/Tests/VisualBaselines/`；有意接受画面变化时运行 `Tools → 渊海录 → 美术 → 重建全部视觉基线`。macOS 捕获依赖 Metal，不要传 `-nographics`。

若批处理日志出现 `Unsupported protocol version '1.18.1'` 或许可证连接挂起，先退出 Unity Hub，再运行批处理；测试结束后可重新打开 Hub。不要同时保留陈旧的 Unity 批处理进程。

## 5. 已知问题与未完成项

### P0

当前没有已知的运行时阻塞 P0。新增功能后仍需在 Unity Play 中做端到端验证。

### P1 — 数据与内容

- v4 已保存主角外观与活跃任务，但尚未保存敌人状态、唯一拾取物、一次性事件、区域标志和其他世界状态。
- `M01_01`–`M01_05` 运行时模板已完成，但烟柳镇现有场景尚未配置对应 `QuestGiver`、区域目标、敌人目标和任务物品；这是阶段二内容接入任务。
- 正式 97 角色和 23 环境已完成确定性第一版，但仍需要人工精修表情、攻击动作节奏、地标细节和区域独特构图。
- 角色 Controller/动画资源已生成并接通四方向移动、冲刺与三段攻击；受击/倒地表现和动作节奏仍可继续深化。
- 物品/任务主要由代码表和 Markdown 设计稿提供，正式 `.asset` 资源仍待制作。

### P2 — 增强

- 10 个户外区域和 13 个室内场景已生成并接通基础旅行、碰撞、昼夜/天气与玩家引导，但烟柳镇之外仍需填入玩法内容和剧情对象。
- 商店 UI、武学技能树、小地图、BOSS 战待实现。
- BGM/SFX 仍为空或占位；同一缺失资源只警告一次，避免脚步音刷屏。

## 6. 文件地图

```text
yuanHaiLu/
├── Assets/
│   ├── Scripts/                 71 个运行时/编辑器 .cs
│   ├── Tests/EditMode/          92 个测试用例
│   ├── Tests/PlayMode/          10 个测试用例
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
│   ├── HANDOFF-art-production.md
│   ├── 03-art-production-handoff.md
│   └── superpowers/specs|plans/ 本次规格与实施计划
├── tools/art_pipeline/          确定性美术 baker/validator（35 测试）
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
41. 存档升级 v4，保存 `playerArtId`；该批最初使用女剑客回退，后由第 52 项按确认规格统一为 `player_male_swordsman`。
42. 修复 Unity 6 批处理场景中逐格 `Tilemap.SetTile` 未序列化：改用批量 `SetTiles`，增加地面数量和 Buildings 结构层回归断言。
43. 修复 `CharacterVisual` 使用 `??` 遇到 Unity fake-null 时无法创建 Animator 的 `MissingComponentException`。
44. 修复 `PlayerCombat.Update()` 在 GameManager 引导前/销毁期空引用；无管理器时安全等待。
45. 增加主菜单与烟柳镇离屏实际渲染验收图，并由截图发现/修复动画整表拉花、RectTransform 偏移、AspectRatioFitter 覆盖尺寸、菜单文字裁剪等视觉问题。
46. 最终验证：81/81 EditMode、6/6 PlayMode、34/34 Python；`build --all` 为 `built=0 skipped=120`，全资产校验通过。

### 第七批：规格补齐、可玩旅行与视觉回归（2026-08-13）

47. 修复正式 Controller 只有 down idle/walk 可达：补齐四方向 idle/walk/dash/attack_1/2/3 过渡，并新增真实攻击命中 PlayMode 回归。
48. EnemyAI 改为由 `OnAttackHitFrame()` 动画事件结算伤害，不再在动画开始前立即扣血。
49. 普通敌人重建为规格确认的精确 24 人名单；Python/Unity validator 强制 97/23 全目录范围，测试改为只读，不再用重建自愈失败。
50. 23 个正式场景补齐 Buildings/边界碰撞、Foreground 遮挡、昼夜色调、区域天气和运行时玩家/相机引导。
51. 新增 `FormalSceneTravelGraph` 与稳定锚点传送；全部正式场景均有可解析后继，真实烟柳镇→客栈 PlayMode 通过。
52. 修复菜单选择立即污染外观：新游戏先显示选择器，确认才提交，取消恢复；默认和 v1–v3 迁移统一为 `player_male_swordsman`。
53. 修复 LoadGame 位置被 `SceneBootstrapper` 默认出生点覆盖；只有 NewGame 使用默认出生，SceneTransition 优先使用待处理锚点。
54. 新增主菜单与 10 户外区域 480×270 Metal 视觉基线，以及颜色/尺寸/确定性/像素差异回归。
55. 新增主菜单→选角→移动→攻击→NPC 对话→暂停→存读档→传送的完整 PlayMode E2E。
56. 删除 Demo 生成器遗留几何占位地图路径，更新美术规范、记忆文档和 `docs/HANDOFF-art-production.md`。
57. 本批最终验证结果见 §4；完整交接与不可破坏契约见 `docs/HANDOFF-art-production.md`。
58. 修复默认 08:00 被错误初始化为 Dawn，以及环境控制器先于 `GameTimeManager` 启动时永不订阅昼夜事件；补初始化顺序回归。
59. 修复正式场景运行时新建相机停在 z=0、整张像素世界被 near clip 裁空；统一放置到 z=-10 并补真实场景回归。
60. 移除 `GameManager.Start()` 无条件回到 MainMenu 的状态竞争；入口状态现由 `MainMenu` 或 `SceneBootstrapper` 明确决定，正式场景直开保持可操作。
61. 正式传送在加载窗口临时保留 Player，并于目标锚点落位后重新归属目标场景，避免 HP、等级、武学等运行时组件状态被重建清空。
62. 正式场景无 `SceneDirector` 时由 `SceneBootstrapper` 完成入口生命周期；SceneTransition 在消费锚点后恢复同一 Player 的输入。
63. 环境生成器开始实际消费布局 JSON 的层标记、碰撞格和前景跨度；wall/roof/window Tile 使用 Grid collider，布局碰撞按连续格合并为 BoxCollider。
64. 户外 Effects 图层按 rain/snow/sand/ember/fog 等 weather ID 动态循环，室内环境光保持静态；默认 08:00 昼夜状态和延迟订阅均有回归覆盖。
65. 选角面板显式切换 EventSystem 焦点，Build Settings 改从环境 Catalog 生成，全 97 角色强制验证 Controller/Prefab，主菜单加入真实捕获像素差异测试。
66. Demo 开场出生点修正到正式烟柳镇内部 `(20.5, 7.5)`，主流程 E2E 等待 SceneDirector 完成真实新游戏初始化，不再关闭协程掩盖问题。

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
