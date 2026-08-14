# 渊海录 — Unity 工程搭建与验证指南

## 1. 环境要求

| 项目 | 要求 |
|------|------|
| Unity | `6000.4.10f1` |
| 模板/渲染 | 2D Core / 内置 2D，**不是 URP** |
| 推荐平台 | macOS Apple Silicon |
| 输入 | Legacy Input Manager |
| 测试 | Unity Test Framework `1.6.0` |

本仓库已经是完整 Unity 项目，不需要新建工程或复制脚本。用 Unity Hub 直接打开仓库根目录即可。

## 2. 首次打开

1. Unity Hub → **Open** → 选择 `yuanHaiLu` 根目录。
2. 确认编辑器为 `6000.4.10f1`。
3. 等待 `Library/` 首次导入和 C# 编译完成。
4. Console 中不应存在编译错误。

如果修改过 `ProjectSettings/InputManager.asset`、`TagManager.asset` 或其他 `ProjectSettings/*.asset`，请关闭并重新打开 Unity，确保配置被重新加载。

## 3. 场景与运行

仓库维护 25 个 Build Settings 场景：

```text
Assets/Scenes/MainMenu.unity
Assets/Scenes/Demo_YanLiuTown.unity
Assets/Scenes/Regions/*.unity（10 个）
Assets/Scenes/Interiors/*.unity（13 个）
```

需要重建时使用：

```text
Tools → 渊海录 → 生成主菜单场景
Tools → 渊海录 → 生成Demo场景
```

在 **File → Build Profiles**（部分界面仍显示 Build Settings）中按顺序加入：

```text
0  MainMenu
1  Demo_YanLiuTown
2–11  Regions
12–24 Interiors
```

日常运行可从 `MainMenu` 开始；需要快速调试场景时也可直接打开 `Demo_YanLiuTown`，它会按新游戏入口初始化。

## 4. 项目设置基线

### 4.1 物理 Layer

`ProjectSettings/TagManager.asset` 中固定为：

```text
6  Player
7  Enemy
8  NPC
9  Environment
```

不要新增或改名为 `Interactable`：交互通过 `IInteractable` 组件过滤，NPC 使用 `NPC` 层。

### 4.2 Sorting Layer

从后到前必须为：

```text
Ground → Environment → Character → Foreground → UI
```

这些名字与 `GameConfig.SORTING_*` 常量直接对应，拼写不一致会让渲染静默落到默认层。

### 4.3 像素规格

| 设置 | 值 |
|------|-----|
| 内部分辨率 | `480 × 270` |
| Pixels Per Unit | `16` |
| 瓦片 | `16 × 16` |
| 正式角色帧 | `32 × 32` |
| Filter Mode | Point |
| Compression | None |
| Anti Aliasing | Disabled |
| VSync | Don't Sync |

可用 `Tools → 渊海录 → 配置所有精灵为像素模式` 批量修正导入设置。

### 4.4 输入轴

实际配置位于 `ProjectSettings/InputManager.asset`：

| 动作 | 主键 | 备用键 |
|------|------|--------|
| Horizontal / Vertical | 方向键 | WASD |
| Attack | J | Left Ctrl |
| Dash | Left Shift | Space |
| Interact | K | E |
| Submit | Return / Enter | Space |
| Cancel | Escape | 手柄 Button 1 |

背包 Tab、任务 Q、武学 1–4 等功能直接读取 `KeyCode`。对话/暂停状态由 `GameManager` 输入门阻止战斗和武学误触。

## 5. 运行时对象约定

主菜单和游戏场景都通过 `GlobalSystemsBootstrapper` 补齐以下持久化管理器：

```text
GameManager
├── SaveManager
├── InventoryManager
├── QuestManager
├── GameTimeManager
└── DialogueManager
```

`AudioManager` 由场景入口单独保障。不要在新场景中实现另一套管理器创建逻辑。

玩家对象至少需要：

```text
SpriteRenderer
Animator
Rigidbody2D
BoxCollider2D
PlayerController
CharacterStats
PlayerCombat
PlayerInteraction
CharacterVisual
PlayerAppearanceBinder
MartialArtsSystem（武学功能需要）
```

`PlayerInteraction.EnsureOn` 是幂等入口；场景生成器、`SceneBootstrapper` 和 `SceneDirector` 都会调用它，因此既有 Demo 场景无需重新生成即可获得 K/E 交互。

### 5.1 任务组件约定

- `QuestDatabase` 已内置 `M01_01`–`M01_05` 稳定模板；`Resources/Quests` 下同 ID 的 `QuestData` 可覆盖代码模板。
- 给任务 NPC 在 `NPCBase` 同物体添加 `QuestGiver`，配置 `questId` 与需要推进的 `interactionTargetId`。不要让 `QuestGiver` 实现 `IInteractable`。
- 给普通敌人/Boss 添加 `QuestTarget`，配置 `objectiveType` 和稳定 `targetId`。
- 区域目标配置 `AreaTrigger.questTargetId`；任务未接取时不会提前消耗一次性上报机会。
- `ItemPickup` 和 `MartialArtsSystem.LearnSkill` 已自动在完整成功拾取/首次学习后上报。
- v4 存档保存正式主角外观、活跃任务与目标进度；v1–v3 缺失外观时迁移为女剑客。

## 6. Animator 约定

正式角色 Controller 已生成，并沿用下列参数：

```text
MoveX       Float
MoveY       Float
Speed       Float
IsDashing   Bool
IsAttacking Bool
AttackIndex Int
```

创建控制器时必须沿用这些名字；攻击判定由动画事件调用 `PlayerCombat.OnAttackHitFrame()`。

## 7. 自动测试

当前基线：100 个 EditMode、7 个 PlayMode、45 个 Python 测试。

正式美术验证：

```bash
python3 -m unittest discover -s tools/art_pipeline/tests -v
python3 -m tools.art_pipeline.build --all
python3 -m tools.art_pipeline.validate --all
```

在 macOS 上运行全部 EditMode 测试：

```bash
/Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath /absolute/path/to/yuanHaiLu \
  -runTests \
  -testPlatform EditMode \
  -testResults /tmp/yuanHaiLu-editmode.xml \
  -logFile /tmp/yuanHaiLu-editmode.log
```

运行 PlayMode 测试时使用相同命令，并把平台和输出文件改为：

```bash
-testPlatform PlayMode \
-testResults /tmp/yuanHaiLu-playmode.xml \
-logFile /tmp/yuanHaiLu-playmode.log
```

注意：`-runTests` 命令不要附加 `-quit`。仅做编译/导入检查时才使用：

```bash
/Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath /absolute/path/to/yuanHaiLu \
  -quit \
  -logFile /tmp/yuanHaiLu-compile.log
```

若日志出现 `Unsupported protocol version '1.18.1'` 或许可证连接长时间挂起，先完全退出 Unity Hub 并终止陈旧的 Unity 批处理进程，再重跑命令；GUI 验证时再重新打开 Hub。

## 8. Play 验证清单

重启 Unity 后至少验证：

- [ ] 主菜单可见 12 个角色选择；切换外观后 idle 预览和金色选中态正确。
- [ ] 主菜单“新游戏”进入 `Demo_YanLiuTown`，选择的外观保持。
- [ ] Demo 正式地面、水岸、道路、桥、建筑、玩家和 NPC 可见，场景切换后视口外无残影。
- [ ] 序章村庄 normal / burned 状态仅替换环境画面；入口、出口、地标锚点和墙体碰撞保持一致。
- [ ] NPC 附近出现提示，K 与 E 都能开始对话。
- [ ] 自动事件不显示交互提示；按键事件可以触发且一次性事件不会重复出现。
- [ ] J 攻击、Shift 冲刺、数字键武学在探索状态可用。
- [ ] ESC 显示暂停面板，再按 ESC 可继续游戏。
- [ ] v4 存档后修改外观、位置、HP/MP、背包、装备、金钱、武学、活跃任务和已完成任务，读档可精确恢复。
- [ ] 读档不追加初始物资、不覆盖出生点；卸下装备后属性正确。
- [ ] 再次加载其他场景不会重复应用旧存档。

敌人、唯一拾取物、一次性事件、区域标志等世界状态目前不在存档范围，不能把它们列为通过项。五条主线模板尚未接入现有烟柳镇场景，场景端任务闭环留待阶段二。

## 9. 常见问题

- **找不到场景**：确认 Build Profiles 中名字为 `Demo_YanLiuTown`，不是 `YanLiuTown`。
- **K/E 无响应**：重启 Unity，并检查 `InputManager.asset` 中只有一个 `Interact` 轴。
- **NPC 无法检测**：确认物理层为 `NPC`，Collider2D 可进入交互半径，组件实现 `IInteractable`。
- **物品 ID 找不到**：`InventoryManager` 先加载 `ItemDatabase` 代码表，再用 `Resources/Items` 下同 ID 的 SO 覆盖。
- **读档后属性叠加**：v4 仍沿用 v2 的基础属性语义；装备加成由背包恢复后统一重算。
- **任务读档警告**：未知任务模板/目标会跳过并警告，越界进度会钳制并警告；不要把这些警告静默删除。
- **大量命名空间错误**：系统代码必须使用 `YuanHaiLu.GameSystem`，不要使用 `YuanHaiLu.System`。
- **只有地标、没有 Tilemap 地面**：运行 `RegionSceneBuilder` 重建；生成器必须批量调用 `Tilemap.SetTiles`，不能改回逐格 `SetTile`。
- **只有 HUD、没有地图**：确认 Demo 主摄像机位于 Z=-10，并从正式烟柳镇场景重新生成 Demo。
- **需要审查美术画面**：执行 `Tools → 渊海录 → 美术 → 截取临时正式美术验收图`；输出在 `/private/tmp/yuanhailu-art-review/`，不会改写仓库基线。
- **启动提示 Packages with Errors**：项目已移除未使用且停止支持的 IAP 4.15；若旧 Library 缓存仍显示，等待 Package Manager 完成刷新后重启 Unity。
