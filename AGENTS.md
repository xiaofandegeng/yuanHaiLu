# AGENTS.md — 渊海录 项目交接与记忆

> 本文件是接手本项目（开发者或 AI 助手）的**首选入口**。读完这一篇即可快速进入工作状态。
> 最后更新：2026-08-12

---

## 0. 30 秒速览

| 项 | 内容 |
|----|------|
| **项目** | 渊海录（YuanHaiLu）— Unity 6 像素武侠 RPG（俯视角 2D） |
| **引擎** | Unity `6000.4.10f1`（2D 模板，**不是** URP） |
| **平台** | macOS Apple Silicon（可扩 PC/WebGL/移动） |
| **代码规模** | 44 个 C# 脚本 ≈ 10000 行 |
| **状态** | Demo 可运行（烟柳镇场景）；框架完整，内容待填充 |
| **版本控制** | Git（分支 `main`），`.gitignore` 已配置 |
| **测试** | ⚠️ **无自动化测试套件**，只能手动在 Unity 中 Play 验证 |
| **设计文档** | `docs/01-art-style-guide.md`、`docs/02-story-design.md`、`README.md`、`SETUP_GUIDE.md` |

---

## 1. 如何运行

### 1.1 首次打开
1. Unity Hub → Open → 选择本目录。
2. 首次打开会触发导入与编译（1–3 分钟，生成 `Library/`）。
3. 若 Console 出现编译错误，先确认用的是 `6000.4.10f1`。

### 1.2 生成/重置场景
场景由编辑器工具**程序化生成**，不在 Git 里维护"手工摆放"。如需重建：
```
菜单栏 → Tools → 渊海录 → 生成主菜单场景
菜单栏 → Tools → 渊海录 → 生成Demo场景
```
（实现：`Assets/Scripts/Editor/DemoSceneGenerator.cs` / `MainMenuSceneGenerator.cs`）

### 1.3 运行
- 双击 `Assets/Scenes/Demo_YanLiuTown.unity` → 按 **Play**。

### 1.4 ⚠️ 关键操作守则
- **修改 `ProjectSettings/` 下的 `.asset`（InputManager / TagManager 等）后，必须重启 Unity 编辑器**，改动才会被重新加载。
- 精灵导入设置可用 `Tools → 渊海录 → 配置所有精灵为像素模式` 批量处理。

### 1.5 操作键
| 按键 | 功能 |
|------|------|
| WASD / 方向键 | 移动 |
| **J** | 攻击（3 连击，可暴击） |
| **Left Shift** | 冲刺 |
| 数字键 **1–4** | 释放已装备武学（需先学会） |
| Space / Enter | 对话推进；数字键 1–9 选对话分支 |
| ESC | 暂停菜单 |
| Tab / Q | 背包 / 任务日志（UI 已建） |

> 输入轴定义在 `ProjectSettings/InputManager.asset`：`Horizontal/Vertical/Attack(J)/Dash(Shift)/Jump/Submit/Cancel`。**无 `Interact` 轴**（见 §5 已知问题）。

---

## 2. 架构总览

### 2.1 命名空间分层
```
YuanHaiLu.Core          核心层（无依赖）
  ├─ GameConfig         全局常量（分辨率/速度/战斗/图层/排序层/标签名）
  ├─ GameManager        单例 + 游戏状态机（DontDestroyOnLoad）
  ├─ PixelPerfectCamera 像素完美渲染
  ├─ CameraFollow       摄像机跟随 + 震屏
  └─ SceneBootstrapper  场景引导（运行时校正图层/摄像机）

YuanHaiLu.Character     角色（依赖 Core/Effects）
  ├─ PlayerController   8 方向移动 + 冲刺 + Y 轴排序
  ├─ PlayerCombat       连击/暴击/剑气判定（动画事件驱动判定帧）
  ├─ CharacterStats     属性系统（基础值+装备加成分离，HP/MP/Stamina，升级）
  ├─ EnemyAI            5 状态机：Idle/Patrol/Chase/Attack/Return
  ├─ NPCBase            NPC 基类
  ├─ MartialArtsSystem  武学系统（学习/装备/冷却/6 种招式执行）★武侠核心
  ├─ MartialSkill       武学数据 ScriptableObject（+ Projectile 飞行物）
  ├─ LevelSystem        等级成长
  └─ CharacterAudio     角色音效

YuanHaiLu.Effects       特效池（剑气轨迹/伤害数字/屏闪/命中火花）

YuanHaiLu.Map           地图（TileMapManager/AreaTrigger/TeleportPoint/Destructible/ItemPickup/EventTrigger/SceneDirector）

YuanHaiLu.Dialogue      DialogueManager（打字机+分支选择+条件+动作脚本）

YuanHaiLu.GameSystem    系统层（依赖 Character/Core）
  ├─ InventoryManager   背包/装备/金钱（+ ItemData SO 定义）
  ├─ ItemDatabase       代码预置物品表（Demo 用，无需 SO 文件）
  ├─ QuestManager       任务（+ QuestData SO / ActiveQuest / 多目标）
  ├─ SaveManager        存档（PlayerPrefs + JSON）
  ├─ MartialSkillDatabase 代码预置 11 个武学招式表
  ├─ ShopManager / LootTable / AudioManager / GameTimeManager（昼夜）
  ├─ ScreenTransition   屏幕转场
  └─ PlayerDeathHandler 玩家死亡处理

YuanHaiLu.UI            HUD / MainMenu / InventoryUI / QuestUI / PauseMenu / DialogueUI

YuanHaiLu.Editor        编辑器工具（DemoSceneGenerator / MainMenuSceneGenerator / PixelArtImporter / ProjectInitializer / SetupBuildSettings）

YuanHaiLu.Combat        仅 DamageCalculator（预留）
```

### 2.2 核心设计模式
- **单例管理器**：`GameManager` / `InventoryManager` / `QuestManager` / `DialogueManager` / `EffectsManager` / `AudioManager` 均 `Instance` + `DontDestroyOnLoad`。
- **状态机**：`GameManager` 八态（Boot/MainMenu/Exploration/Dialogue/Combat/Menu/Cutscene/Paused），`CanPlayerMove()`/`CanPlayerAct()` 控制输入门；`EnemyAI` 五态。
- **事件驱动**：大量 `event System.Action` 解耦（`OnHpChanged`、`OnQuestCompleted`、`OnDialogueEnd`、`OnSkillUsed` 等）。新增功能优先订阅事件，而非轮询。
- **程序化场景生成**：`DemoSceneGenerator` 一键造出完整可玩场景（玩家/NPC/敌人/木箱/HUD/对话/暂停），是快速迭代的基础设施。
- **动画事件驱动战斗判定**：`PlayerCombat.OnAttackHitFrame()` 由 Animator 的事件帧回调，而非定时器。

### 2.3 数据流向（典型）
```
玩家按 J → PlayerCombat.HandleAttackInput()
  → 动画事件 OnAttackHitFrame() → OverlapBox 检测敌人
  → CharacterStats.TakeDamage() → OnHpChanged/OnDamaged 事件
  → HUD 更新血条；EffectsManager 弹伤害数字；CameraFollow 震屏
  → HP≤0 → OnDeath → 禁用碰撞/输入
```

---

## 3. 关键约定（改动前必读）

### 3.1 像素规格（`GameConfig`）
- 内部分辨率 `480×270`，`PPU=16`，瓦片 `16×16`，角色 `48×48`。
- 精灵导入：Filter Mode = Point，Compression = None，无抗锯齿。

### 3.2 SortingLayer（渲染层，从后到前）
**必须与代码常量 `GameConfig.SORTING_*` 一致：**
```
Ground → Environment → Character → Foreground → UI
```
定义在 `ProjectSettings/TagManager.asset`。代码运行时按名字引用，名字写错会静默落到默认层。

### 3.3 物理 Layer
```
6:Player  7:Enemy  8:NPC  9:Environment
```
`LayerMask.GetMask("Enemy")` 等依赖这些。在 `ProjectSettings/TagManager.asset` 的 `layers:` 段。

### 3.4 新增脚本的位置
| 功能类型 | 目录 |
|---------|------|
| 运行时脚本 | `Assets/Scripts/<子系统>/`，命名空间 `YuanHaiLu.<子系统>` |
| 编辑器工具 | `Assets/Scripts/Editor/`，命名空间 `YuanHaiLu.Editor` |
| 数据 SO | `Assets/Scripts/System/`（定义），`Assets/Resources/Items|Quests/`（实例） |

> ⚠️ 系统（System）脚本统一用命名空间 **`YuanHaiLu.GameSystem`**（不要用 `YuanHaiLu.System`，会与 .NET 的 `System` 冲突，曾导致 345 个编译错误）。

---

## 4. 开发流程

1. 改代码 → 切回 Unity 触发编译 → 看 Console。
2. 改场景 → 手动保存（Cmd+S）。
3. 改 `ProjectSettings/*.asset` → **重启 Unity**。
4. 验证靠 Play；重要改动后在 `docs/` 或本文件记录。
5. 提交前：确认没把 `Library/`、日志、`.csproj` 提交（`.gitignore` 已兜底）。

### 编辑器菜单速查
| 菜单 | 用途 |
|------|------|
| Tools/渊海录/生成Demo场景 | 一键造烟柳镇 |
| Tools/渊海录/生成主菜单场景 | 一键造主菜单 |
| Tools/渊海录/初始化项目设置 | 配置 Tags/Layers/SortingLayers |
| Tools/渊海录/配置所有精灵为像素模式 | 批量精灵导入设置 |
| Tools/渊海录/切分角色精灵表(48×48) | 精灵切片 |
| Tools/渊海录/切分瓦片集(16×16) | 瓦片切片 |

---

## 5. 已知问题与未完成项（按优先级）

### 🔴 P0 — 系统性缺口
- **交互键系统缺失**：`Input.GetButtonDown("Interact")` 从未被读取，无玩家交互控制器。`NPCBase.OnInteract` / `EventTrigger.OnInteract` / `TeleportPoint.OnInteract`（`requireInteract=true` 默认）**无任何调用方** → 需手动按键的 NPC 对话/传送当前不可达。需新增 `PlayerInteraction` 组件：检测附近 `IInteractable` + 按 K 调用 `OnInteract`，并在 InputManager 加 `Interact(K)` 轴。
- **存档系统不完整**：`SaveManager` 只存玩家属性+位置+章节。**背包/装备/任务/武学/世界标志均未存档**（`InventoryManager.GetSaveData`/`LoadSaveData` 已写但未被调用）。读档后视为"无装备"。

### 🟡 P1 — 内容/打磨
- **占位美术**：`Assets/Sprites/Generated/` 是 Python 生成的程序图（`tools/generate_sprite.py`）。需替换为真实像素精灵表。
- **无 Animator 状态机**：玩家/敌人/NPC 的 Animator 参数（MoveX/MoveY/Speed/IsDashing/IsAttacking/AttackIndex）已在代码里 Set，但 `.controller` 资源与动画剪辑未建。当前动作靠代码逻辑，动画不播放。
- **物品/任务仅代码预置**：`ItemDatabase`/`MartialSkillDatabase` 是代码表；`Assets/Resources/Items|Quests/` 里只有 `.md` 设计稿，无 `.asset` 实例。设计稿见 `item_database.md`/`quest_database.md`。
- **Obsolete 字段**：`CharacterStats.learnedSkills`（`MartialSkillLegacy` 列表）已废弃、无读取方，保留仅为向后兼容，可安全删除。

### 🟢 P2 — 增强
- 天枢城等其余 5 个区域尚未开始（剧情见 `docs/02-story-design.md`）。
- 商店 UI、武学技能树 UI、小地图、BOSS 战。
- BGM/SFX 占位（`Assets/Audio/` 目录已建）。

---

## 6. 文件地图

```
yuanHaiLu/
├── Assets/
│   ├── Scripts/                 44 个 .cs（见 §2.1 分层）
│   ├── Scenes/                  MainMenu.unity + Demo_YanLiuTown.unity
│   ├── Sprites/Generated/       占位像素图（待替换）
│   ├── Resources/
│   │   ├── Items/item_database.md       物品设计表（待做成 .asset）
│   │   └── Quests/quest_database.md     任务设计表（待做成 .asset）
│   ├── Prefabs/ Animations/ AnimatorControllers/ Audio/ Fonts/ Art/  （多为空/待填充）
├── docs/                        美术风格 + 剧情大纲
├── tools/                       Python 精灵/瓦片/地图生成脚本
├── ProjectSettings/             ⚠️ 改动后需重启 Unity
├── Packages/manifest.json       依赖清单（2D + 标准 module）
├── README.md  SETUP_GUIDE.md
├── AGENTS.md                    ← 本文件
└── .gitignore
```

---

## 7. 修复历史（本次会话 2026-08-12）

### 第一批：让 Demo 能正常运行（4 项运行时阻塞）
1. **InputManager 缺轴** → 在 `InputManager.asset` 追加 `Attack(J)` / `Dash(Left Shift)` 轴（原 18→20 轴）。
2. **SortingLayer 名字不一致** → `TagManager.asset` 重写为 `Ground/Environment/Character/Foreground/UI`（对齐 `GameConfig.SORTING_*`）。
3. **`CharacterStats.RestoreMp` 公式错误**（恒为满蓝）→ 改为 `Mathf.Min(currentMp + amount, maxMp)`。
4. **远程招式飞行物无碰撞体** → `MartialArtsSystem.ExecuteRangedSkill` 为飞行物加 `CircleCollider2D{isTrigger=true, radius=0.3}`。

### 第二批：功能完善 + 工程化（4 项）
5. **重复 using** → 删除 `DialogueManager.cs`、`SaveManager.cs` 各一处重复引用。
6. **武学快捷键与对话数字键冲突** → `MartialArtsSystem.Update()` 加 `CanPlayerAct()` 守卫，对话/暂停时不读技能键。
7. **武学学习链路接通** → `InventoryManager.LearnSkill` 改为 `MartialSkillDatabase.Get` → `MartialArtsSystem.LearnSkill`（移除对废弃字段的引用）。
8. **装备属性加成生效** → `CharacterStats` 引入"基础值+装备加成"分离（`RecomputeDerived`/`SetEquipmentBonus`/`SetBaseFromLoad`，`LevelUp` 改修基础值）；`InventoryManager.ApplyEquipmentStats` 累加三件装备加成；`SaveManager.LoadGame` 改用 `SetBaseFromLoad`。

### 工程化
- 新增 `.gitignore`（Unity 标准，忽略 Library/Temp/Logs/csproj/sln 等，保留 .meta 与 .vscode）。
- 清理根目录 14 个 `unity_*.log` 与 `.DS_Store`。
- `git init`（分支 `main`）+ 初始提交。

---

## 8. 推送到 GitHub（待执行）

```bash
# 在 GitHub 新建空仓库 yuanHaiLu（不要勾选 README/license，避免冲突）
git remote add origin git@github.com:<你的用户名>/yuanHaiLu.git
git branch -M main
git push -u origin main
```
