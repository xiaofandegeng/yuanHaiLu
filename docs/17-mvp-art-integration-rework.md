# MVP 美术整合返工（历史方案）

> 状态：已被后续模块化方案取代。本文记录 480×270 三静态层阶段的历史实现，不再是后续开发或验收依据。请改读 [docs/18-dense-pixel-mvp-implementation-handoff.md](18-dense-pixel-mvp-implementation-handoff.md)。

## 问题定性

上一版把高密度概念图缩放成一张 480×270 背景，再把旧的 32×32 角色资源置于其上。背景、角色和交互物的像素密度、俯视角、调色板、光源与前景遮挡均不一致；看上去像一张画被角色贴在表面，而不是可行走场景。

## 采用方案

只重做 MVP 的两处场景，且遵守项目既定纯 2D 像素规格。

1. 场景使用原生 480×270、16px 网格的确定性像素源，拆为 `Ground`、`Environment` 和透明的 `Foreground` 三层。运行时角色在环境层与前景层之间，屋檐、柳枝和柜台前沿会形成有限遮挡；不再直接使用 AI 概念图作游戏背景。
2. 烟柳镇画面只服务“客栈门—河岸”路线：灰青石路、深青水道、石拱桥、暖灯客栈和河岸战斗空地分别用独立明度组表达。环境轮廓和不可走碰撞一一对应。
3. 客栈画面只服务“入口—掌柜—出口”：入口石阶、长柜台、掌柜、灶火、桌席和楼梯各自为可读轮廓；柜台主灯是唯一强暖焦点，前景门帘只压画面边缘。
4. 男主仍为 32×32、四方向、可动画的 `player_male_swordsman`，但重画成深发、靛蓝短披、米白内衫、朱砂腰绦和钢剑五个稳定识别点，剪影至少占帧宽 26px、高 30px。
5. 掌柜与两名水匪只在 Demo 内换用 32×32 的 MVP 持久精灵，不修改正式 97 角色、Prefab、Controller 或角色目录。掌柜为赭褐/暖灰，水匪为暗红/青灰，和男主、场景共用墨青／宣纸／赭石／朱砂调色板。

## 接线与验证

- `PlaySceneAssembler` 从三个持久图层创建 `Ground → Environment → Foreground`，替代单张 `MvpBackdrop`；禁止运行时生成 Texture 或 Sprite。
- MVP 的底图使用项目既有的 `Default` 底层，随后依次进入 `Environment → Character → Foreground`；这保证真实遮挡，同时不为了改名重写冻结的正式场景与角色资源。
- 两个 Demo 生成器只创建本次坐标体系中的碰撞和任务对象；去掉视口外、与 MVP 无关的镇 NPC。
- 新增 Python 测试：三层尺寸、透明前景、共享色板、男主有效剪影、MVP NPC/敌人持久精灵。
- 新增 Unity 接线测试：两个 Demo 都有三层、掌柜/水匪不再引用正式角色美术、角色排序位于环境与前景之间、关键路线仍可达。
- 以真实 Game View 重新输出三张 480×270 1× 实拍；自动测试只证明接线和流程，视觉是否批准仍由用户判定。

## 不变边界

- 不新增区域、角色职业、任务或系统；保留 `MVP_01`、`QuestStageGate`、存档 v5、三武器流派和场景往返。
- 不改 `ProjectSettings/`、正式区域/室内基线、11 套未使用主角、正式 NPC/敌人/Boss 资产。
- 所有新图放入 `Assets/ArtSource/Environment/MVP/v2/`、`Assets/Art/Environment/MVP/v2/` 或 `Assets/Resources/Art/MVP/`；均有 `.meta` 且 Point/PPU 16/无压缩。

## 完成证据（2026-08-22）

- Python 48/48；`python3 -m tools.art_pipeline.build --all` 为 `built=0 skipped=131`；`validate --all` 通过。
- Unity EditMode 138/138、PlayMode 14/14 通过；结果分别在 `/private/tmp/yuanhailu-mvp-native-editmode-full.xml` 与 `/private/tmp/yuanhailu-mvp-native-playmode-full.xml`。
- 三张真实 480×270 游戏画面在 `/private/tmp/yuanhailu-mvp-rework-review/`：`town-spawn-1x.png`、`town-riverbank-1x.png`、`inn-counter-1x.png`。它们是技术与流程复核输入，不代替用户的视觉验收。
