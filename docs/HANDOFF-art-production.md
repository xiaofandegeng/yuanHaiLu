# 正式美术生产交接

> 状态日期：2026-08-13  
> Unity：`6000.4.10f1`，2D Core / 内置 2D（非 URP）  
> 规格：`docs/superpowers/specs/2026-08-12-full-art-production-design.md`

## 已交付范围

- 97 套正式角色：12 主角、15 剧情角色、36 通用 NPC、24 普通敌人、10 BOSS。
- 主角固定为 2 性别 × 6 职业；新游戏默认 `player_male_swordsman`，只有点击“确认选择”才写入本次新游戏。
- 24 个普通敌人严格按六大区域各 4 个的规格清单生成；目录校验会拒绝缺项、多余项和 ID 错位。
- 每个角色具有 32×32 四方向动作表、稳定帧名、`.art.json`、Animator Controller 和 Prefab。
- 角色 Controller 已接通四方向 idle/walk/dash/attack_1/attack_2/attack_3；攻击命中由动画事件调用 `PlayerCombat.OnAttackHitFrame()` 或 `EnemyAI.OnAttackHitFrame()`。
- 23 套正式环境：10 个户外区域、13 个室内场景；每套均有确定性 PNG/JSON、持久 Tile 和 `.unity` 场景。
- 正式场景具有 7 层 Tilemap、地标、Buildings/布局碰撞、Foreground 遮挡切片、昼夜色调、动态区域天气和稳定 entry/exit/interior 锚点；生成器会实际消费布局 JSON 的 `layers/collisions/foregroundSpans`。
- `FormalSceneTravelGraph` 为全部 23 个场景建立可达传送关系；运行时 `SceneBootstrapper` 在场景中补齐玩家和相机，并按传送锚点落位。
- 烟柳镇 Demo 使用同一套正式环境和角色目录，叠加剧情 NPC、敌人、对话、战斗、暂停、保存/读取与传送流程。
- v4 存档持久化 `playerArtId`；v1–v3 因没有外观字段会静默迁移为 `player_male_swordsman`，v4 非法 ID 会告警后回退到同一默认值。
- 10 个户外区域与主菜单均有 480×270 实际相机视觉基线；EditMode 会重新捕获全部 11 个画面并要求像素差异不超过 0.5%。

## 资源与代码入口

| 目标 | 路径 |
|------|------|
| 可编辑角色源/清单 | `Assets/ArtSource/Characters/` |
| 可编辑环境源/布局/清单 | `Assets/ArtSource/Environment/` |
| 角色与环境烘焙输出 | `Assets/Art/Characters/`、`Assets/Art/Environment/` |
| 24 敌人标注总览 | `Assets/Art/Characters/Enemies/previews/enemy-roster.png` |
| 角色 Controller/Prefab | `Assets/AnimatorControllers/Characters/`、`Assets/Prefabs/Characters/` |
| 正式 Tile/目录资产 | `Assets/Tilemaps/Formal/`、`Assets/Resources/Art/` |
| 正式场景 | `Assets/Scenes/Regions/`、`Assets/Scenes/Interiors/` |
| 可玩入口 | `Assets/Scenes/MainMenu.unity`、`Assets/Scenes/Demo_YanLiuTown.unity` |
| 视觉回归基线 | `Assets/Tests/VisualBaselines/` |
| Python 流水线 | `tools/art_pipeline/` |
| Unity 导入/生成器 | `Assets/Scripts/Editor/Art/` |
| 运行时环境/传送 | `Assets/Scripts/Art/RegionEnvironmentController.cs`、`Assets/Scripts/Map/FormalSceneTravelGraph.cs` |

## 正确重建顺序

先在仓库根目录运行：

```bash
python3 -m tools.art_pipeline.build --all
python3 -m tools.art_pipeline.validate --all
```

再在 Unity 依次执行：

1. `Tools → 渊海录 → 美术 → 重建角色动画与Prefab`
2. `Tools → 渊海录 → 美术 → 生成全部正式环境场景`
3. `Tools → 渊海录 → 生成主菜单场景`
4. `Tools → 渊海录 → 生成Demo场景`
5. 需要有意接受新画面时，执行 `Tools → 渊海录 → 美术 → 重建全部视觉基线`

视觉基线捕获依赖真实图形设备；macOS 批处理时不要传 `-nographics`。生成文件不是手工编辑入口：角色或场景改动应先修改 `Assets/ArtSource` 或 `tools/art_pipeline`，再按上述顺序重建。

## 不可破坏的契约

- 内部分辨率 480×270，瓦片 16×16，角色帧 32×32，PPU 16；Point、无压缩、无 mipmap。
- 正式 ID 全部为小写 snake_case；运行时只通过稳定 ID 和 Catalog 取资源。
- `CharacterArtCatalog` 必须恰好为 97 条，分类计数固定为 12/15/36/24/10。
- `EnvironmentArtCatalog` 必须恰好为 23 条，分类计数固定为 10/13。
- `CharacterArtCatalog` 每一条都必须同时具有 Sheet、Controller、Prefab、Preview；不能只抽样验证代表角色。
- Build Settings 的 23 个正式路径只能来自 `EnvironmentArtCatalog`，缺失、重复或目录里多出的场景都不能被静默接纳。
- `ArtAssetValidator` 和 Python validator 都会校验规范清单的完整集合，测试不得通过调用生成器“自愈”缺失产物。
- 正式路径禁止运行时 `Texture2D`/`Sprite.Create` 回退；缺失正式资源必须明确失败。
- 场景生成必须使用 `Tilemap.SetTiles` 批量写入；Unity 6 批处理下逐格 `SetTile` 曾保存为空地图。
- 环境布局 JSON 是 Ground/Water 标记、地标位置、碰撞格、前景跨度和锚点的规范输入；wall/roof/window Tile 必须拥有 Collider shape。
- UnityEngine.Object 的补组件逻辑必须显式检查 `== null`，不可用 `GetComponent<T>() ?? AddComponent<T>()`。
- Player 与 Enemy 的攻击伤害依赖 Animator 命中帧事件；新增攻击剪辑必须保留对应事件。
- `SceneBootstrapper` 只在 `NewGame` 默认出生点落位；LoadGame 不得覆盖存档位置，SceneTransition 必须优先使用待处理锚点。

## 验证门槛

提交前必须运行：

```bash
python3 -m unittest discover -s tools/art_pipeline/tests -v
python3 -m tools.art_pipeline.build --all
python3 -m tools.art_pipeline.validate --all
```

Unity EditMode、PlayMode 需分别全量运行；命令不要同时传 `-runTests` 与 `-quit`。视觉回归使用 Metal 图形设备。重点覆盖：

- 97/23 精确目录、切片、Prefab、Controller 与 Animator 攻击命中；
- 23 场景旅行图和锚点可解析，真实烟柳镇→客栈传送；
- 主菜单选择必须确认，取消不污染当前外观，旧存档迁移稳定；
- 主菜单→选角→移动→攻击→NPC 对话→暂停→存读档→跨场景传送的完整 PlayMode E2E；
- 10 户外视觉基线和主菜单基线的 480×270 像素回归。

测试执行后的最终数字记录在 `AGENTS.md` 与 `README.md`，两处必须同步。

## 已知后续内容

- 交付的是可复现的正式第一版像素资源，不再是几何色块占位；仍可继续人工精修表情、剪影、攻击节奏、材质纹理和区域构图。
- 23 场景的基础旅行、碰撞、昼夜、天气和前景已接通；烟柳镇之外仍需填充剧情对象、任务、敌人配置和区域专属玩法。
- v4 尚未保存敌人、唯一拾取物、一次性事件和世界标志。
- 物品/任务正式 SO、商店 UI、武学树、小地图、BOSS 专属机制、BGM/SFX 仍是下一阶段工作。

## 本轮关键缺陷修复

1. 修复正式 Controller 只有 down idle/walk 可达，导致攻击状态和命中事件永远不执行。
2. 修复 EnemyAI 在攻击动画播放前立即扣血，改为命中帧事件判定。
3. 修复 24 敌人名单与确认规格不一致，并让双端 validator 强制精确范围。
4. 修复正式场景只有展示骨架、没有玩家落位和可玩传送链。
5. 补齐 Buildings/边界碰撞、前景遮挡、昼夜与区域天气。
6. 修复新游戏外观未经确认就被立即写入，以及旧档默认角色不一致。
7. 修复读档位置被场景默认出生点覆盖。
8. 移除生成器中遗留的地面、墙、树、井、路牌与角色几何占位路径。
9. 改为只读测试，避免测试重建资源掩盖缺失产物。
10. 增加视觉像素回归和贯穿菜单、战斗、对话、暂停、存档、传送的真实主流程 E2E。
11. 修复默认 08:00 被初始化为黎明，以及场景控制器先启动时错过昼夜事件订阅。
12. 修复正式场景运行时创建的相机停在 z=0，导致世界精灵被近裁剪面裁空。
13. 修复正式场景直开时 `GameManager.Start()` 把 Exploration 覆盖回 MainMenu、导致输入失效。
14. 修复正式场景传送时重建 Player，导致 HP、等级、武学等运行时状态丢失。
15. 修复正式场景入口模式停留在 NewGame，以及保留 Player 后跨场景输入未恢复。
16. 修复正式场景运行时创建的相机位于 z=0，以及 GameManager 启动顺序把 Exploration 覆盖成 MainMenu。
17. 环境生成器改为读取 `layers/collisions/foregroundSpans`，并让 Buildings TilemapCollider 取得真实结构形状。
18. 户外天气改为按 weather ID 选择速度的动态 Effects 图层，室内保持静态环境光。
19. 主菜单选角面板打开/关闭时显式移动 EventSystem 键盘焦点；Build Settings 改由 Catalog 规范化生成。
20. Demo SceneDirector 出生点改为正式烟柳镇内部 `(20.5, 7.5)`，E2E 不再停掉真实初始化协程。
