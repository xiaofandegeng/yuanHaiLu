# 正式美术生产与主流程交接

> 更新时间：2026-08-13  
> 对应规格：`docs/superpowers/specs/2026-08-12-full-art-production-design.md`  
> 对应计划：`docs/superpowers/plans/2026-08-12-full-character-art.md`、`2026-08-12-full-environment-art.md`、`2026-08-12-art-integration-qa.md`
> 当前完整交接：`docs/HANDOFF-art-production.md`（本文件保留为原计划入口）

## 交付结果

- 97 套正式角色：12 主角、15 剧情角色、36 NPC、24 敌人、10 BOSS。
- 主角固定 2 性别 × 6 职业：剑客、拳师、暗器、医者、儒生、术士。
- 每套角色有独立 PNG、`.art.json`、稳定帧名、完整四方向动作行、Animator Controller 和 Prefab。
- 10 个户外区域、13 个室内场景，共 23 套环境 PNG/JSON、持久 Tile 与 `.unity` 场景。
- 每个正式环境场景有 Ground、Water、Lower Environment、Buildings、Character、Foreground、Effects 七层 Tilemap，以及可达 entry/exit/interior 锚点。
- 主菜单支持 12 套外观选择和确认/取消；v4 存档持久化 `playerArtId`；菜单进入 Demo 后外观保持。
- 全部正式 Controller 已接通四方向 idle/walk/dash/三段攻击与动画命中帧。
- 全部 23 个场景已有 JSON 驱动碰撞/前景、昼夜/动态天气、运行时玩家引导和可达传送关系。
- `Demo_YanLiuTown` 使用正式烟柳镇 Tilemap、地标、桥、水岸、建筑和正式角色资源，不再由生成器创建临时像素块。

## 资源入口

| 目标 | 路径 |
|------|------|
| 可编辑角色源/清单 | `Assets/ArtSource/Characters/` |
| 可编辑环境源/布局/清单 | `Assets/ArtSource/Environment/` |
| 角色输出 | `Assets/Art/Characters/` |
| 环境输出 | `Assets/Art/Environment/` |
| 角色 Prefab | `Assets/Prefabs/Characters/` |
| 角色 Controller | `Assets/AnimatorControllers/Characters/` |
| 正式 Tile | `Assets/Tilemaps/Formal/` |
| 正式目录 | `Assets/Resources/Art/` |
| 23 个环境场景 | `Assets/Scenes/Regions/`、`Assets/Scenes/Interiors/` |
| 可玩 Demo | `Assets/Scenes/Demo_YanLiuTown.unity` |
| 验收图 | `Assets/Art/Characters/Player/previews/main-menu-character-selection.png`、`Assets/Art/Environment/previews/demo-yanliu-gameplay.png` |

## 重建顺序

```bash
python3 -m tools.art_pipeline.build --all
python3 -m tools.art_pipeline.validate --all
```

然后在 Unity 依次执行：

1. `Tools → 渊海录 → 美术 → 重建角色动画与Prefab`
2. `Tools → 渊海录 → 美术 → 生成全部正式环境场景`
3. `Tools → 渊海录 → 生成主菜单场景`
4. `Tools → 渊海录 → 生成Demo场景`
5. 有意接受视觉变化时执行 `Tools → 渊海录 → 美术 → 重建全部视觉基线`
6. `SetupBuildSettings.Setup()` 或运行任一生成器的命令行入口，确认 Build Settings 为 25 个场景。

自动生成文件不是手工编辑入口。需要修改画面时，应先改 `Assets/ArtSource` 或 `tools/art_pipeline`，再重建输出。

## 稳定契约

- 角色帧 32×32、瓦片 16×16、PPU 16、Point、无压缩、无 mipmap。
- 所有正式 ID 使用小写 snake_case；运行时代码只依赖目录稳定 ID，不依赖文件枚举顺序。
- `CharacterArtCatalog` 必须严格保持 97 条，分类计数 12/15/36/24/10。
- `EnvironmentArtCatalog` 必须严格保持 23 条，分类计数 10/13。
- 正式美术不得用运行时 `Texture2D`/`Sprite.Create` 作为缺失资源回退；缺资源应明确失败。
- Unity 场景批量生成使用 `Tilemap.SetTiles`，不要退回逐格 `SetTile`；后者在 Unity 6 批处理保存时曾产生空 Tilemap。
- UnityEngine.Object 不应使用 `GetComponent<T>() ?? AddComponent<T>()`，应显式两次 `== null` 检查。
- 布局生成器必须读取 `layers/collisions/foregroundSpans`；Build Settings 必须从 `EnvironmentArtCatalog` 获取规范场景路径。

## 测试基线

```text
Unity EditMode: 以 AGENTS.md 最新全量结果为准
Unity PlayMode: 以 AGENTS.md 最新全量结果为准
Python:         以 AGENTS.md 最新全量结果为准
Art build:      built=0 skipped=120
Art validator:  passed
```

完整 Unity 测试运行时不要添加 `-quit`。若出现旧许可通道 `Unsupported protocol version '1.18.1'`，终止陈旧的 `Unity.Licensing.Client` 后重试。

## 已知后续工作

- 当前美术是可复现的正式第一版，仍可人工精修面部辨识度、攻击动作、材质纹理和区域构图。
- Animator 已接通四方向基础移动、冲刺和三段攻击；仍可继续精修受击/倒地表现和动作节奏。
- 烟柳镇之外 22 个环境场景已有基础传送、碰撞、昼夜/天气与玩家引导，尚需剧情对象、任务和区域专属玩法内容。
- v4 仍未持久化敌人、唯一拾取物、一次性事件和世界标志。
- BGM/SFX、任务 SO、物品 SO、商店 UI、技能树、小地图和 BOSS 玩法仍待制作。

## 本轮发现并修复的缺陷

1. Unity fake-null + `??` 导致 NPC 无法补 Animator。
2. Unity 6 批处理逐格 SetTile 未持久化，造成实际截图只有地标没有地面。
3. 主菜单把整张动画预览表作为 RawImage，造成横向拉花。
4. RectTransform 修改锚点后未清 offset，造成标题和按钮背景拉伸。
5. AspectRatioFitter 覆盖固定预览尺寸，角色遮挡按钮。
6. 菜单按钮高度不足，文字在离屏渲染中被裁掉。
7. PlayerCombat 在 GameManager 引导前/销毁期空引用。
8. PlayMode 端到端测试的 DontDestroy/场景对象污染后续用例，已补完整 teardown。
9. 修复攻击状态不可达导致 J 攻击不命中，以及 EnemyAI 动画前立即扣血。
10. 修复 24 敌人名单偏离规格，并补精确目录范围校验。
11. 修复读档位置被默认出生点覆盖，补齐 23 场景旅行图与真实跨场景 E2E。
12. 新增 10 户外 + 主菜单视觉基线，防止构图、颜色或相机回归。
13. 修复 Buildings Tile 全部 `colliderType=None`、布局 JSON 未消费和天气静态平铺。
14. 修复正式场景入口状态/相机/跨场景玩家状态与输入恢复。
15. 修复选角面板键盘焦点、Demo 出生点越界和 Build Settings 目录枚举漂移。
