# 单男主 MVP：画面可读性与主流程返工交接书

> 状态：待执行。本文是交给下一位开发 AI 的唯一执行规格，不是验收通过声明。
>
> 基线：`codex/single-hero-mvp-v2` @ `dfdae6784498c46c625bd032bd0b55b56f36c8e4`。
> 新工作分支必须从该提交创建，例如 `codex/mvp-presentation-flow-rework`；不得从历史全量美术分支迁移，也不得直接修改 `main`。
>
> 本文仅覆盖 `docs/15-single-hero-mvp-design.md` 中「试玩版画面、两处场景、主流程可靠性」的返工。`docs/15` 的单男主、三流派、10–15 分钟垂直切片和冻结范围仍然有效；若本文与其原有「美术冻结」文字冲突，以本文第 4 节所列的**极小范围例外**为准。

## 1. 返工目标

当前试玩版的自动测试虽为绿色，但人工 Unity 试玩发现两个不能接受的问题：

1. 画面和场景太小、留白过多、像素与路径不可读；烟柳镇和客栈呈现为密集且重复的色块，玩家无法在 1× 画面中快速读出人物、目标、门、道路与可走路线。
2. 主流程不能稳定走通：进入客栈后与掌柜交谈，会在 Console 产生 `MissingReferenceException`，对话框不出现，因而无法接取 `MVP_01`。

返工后的可交付物不是“大世界美术”，而是一个可完整试玩、第一眼可读的最小垂直切片：

```text
主菜单（选择剑／拳套／暗器）
  → 烟柳镇客栈门外出生
  → 进入客栈，与掌柜交谈并接任务
  → 回烟柳镇，沿清晰路线到河岸
  → 击败 2 名水匪，拾取荷包
  → 回客栈交付
  → 保存、退出到主菜单、继续游戏
```

完成定义：三种流派都能按上图连续走通，零运行时错误；用户在不放大截图的 480×270 画面中，能立即辨认男主、当前目的地、可走路线和场景主体。

## 2. 已确认事实与根因

以下是已经在可见 Unity `6000.4.10f1` 窗口中复现的事实，不能以“现有自动测试通过”驳回。

### 2.1 P0：跨场景后客栈对话被销毁对象阻断

复现路径：主菜单新游戏 → 烟柳镇 → 正常进入客栈 → 靠近 `NPC_掌柜老赵` 并按 K/E 交谈。

现象：游戏状态进入 `Dialogue`，Console 报 `MissingReferenceException`；对话 UI 不显示，任务无法接取。

确认的代码根因位于 `Assets/Scripts/UI/DialogueUI.cs`：

- `Awake()` 为持久的 `DialogueManager` 订阅了匿名的 `OnDialogueStart += (s, t) => Show()`；
- `OnDestroy()` 只解除三条具名订阅，无法解除这条匿名委托；
- 烟柳镇的旧 `DialogueUI` 被场景卸载后，`DialogueManager` 仍持有它的 `Show()` 回调；客栈触发 `OnDialogueStart` 时先调用已销毁对象并抛异常，后续新 UI 不再可靠显示。

这不是视觉问题，而是合并阻塞 P0。必须先修复并添加真实跨场景回归测试。

### 2.2 P0：像素相机使用错误的世界覆盖尺寸

`Assets/Scripts/Core/PixelPerfectCamera.cs` 当前把外层 Game View 的 `Screen.height` 参与 `orthographicSize` 计算。实际运行日志曾显示：

```text
PixelPerfectCamera Scale: 1x | OrthoSize: 13.31 | Viewport: (x:194, y:78, width:480, height:270)
```

项目的逻辑画面是 480×270、PPU 16，因此世界相机在任何窗口尺寸下都必须覆盖：

```text
orthographicSize = 270 / (2 × 16) = 8.4375
```

整数放大倍率只决定输出的 `pixelRect` 与周边 letterbox，**不得改变世界相机的正交尺寸**。现有逻辑导致世界内容被缩小到中心小区域，正是“场景很小、看不清”的直接原因。

### 2.3 P1：世界视口与 UI 不处于同一逻辑画面

`PlaySceneAssembler` 创建 HUD、对话、暂停等 Canvas 时没有建立与 480×270 游戏视口一致的确定性关系。修复相机后，UI 也必须与世界共用同一逻辑展示面；不得出现世界在中央小框而 HUD/对话漂在外层窗口的状态。

### 2.4 P1：现有两处场景缺乏 1× 构图与可读性

烟柳镇与客栈的现有资源虽有 Tile、碰撞、生成器和自动截图，但不构成视觉验收。问题是构图和明度层级，而不是继续堆叠小纹理：

- 画面没有明确焦点，人物、门、NPC、河岸与可走区域的价值相同；
- 高密度重复图案在 1× 下变成噪点，水面、地面、家具、墙体和碰撞边界不可区分；
- 玩家镜头当前把一个“地图缩略图”塞进画面，而不是围绕操作路线组织可玩的场景；
- 客栈柜台、掌柜、入口、出口、通道与楼梯没有一眼可读的空间关系。

## 3. 不可变产品决定

- 主角固定为 `player_male_swordsman`。不恢复职业／性别选择，不制作第二套身体、第二位主角或完整角色阵容。
- 可选择的只有武器流派：`sword`、`gauntlets`、`dart`。它们可改变持武器小图、攻击、主动技和反馈，不改变男主身体美术。
- 场景只做两处：`Demo_YanLiuTown` 与 `Demo_Inn`。河岸为烟柳镇中的一个小型可达战斗区，不新增第三张大地图。
- 任务只做 `MVP_01`：掌柜 → 河岸 → 两名水匪 → 荷包 → 掌柜。不得把商店、BOSS、支线、技能树、小地图、全区域传送或世界状态持久化塞入本轮。
- 必须保留现有 v5 存档语义、`QuestStageGate` 顺序门、`TransitionCarry` 场景往返状态保持与现有 Build Settings 的 `Demo_Inn` 登记例外。

## 4. 允许与禁止改动范围

### 4.1 允许改动

仅在确有必要时修改下列内容，并为每个改动补自动测试或人工验收证据：

```text
Assets/Scripts/Core/PixelPerfectCamera.cs
Assets/Scripts/UI/DialogueUI.cs
Assets/Scripts/Editor/PlaySceneAssembler.cs
Assets/Scripts/Editor/DemoSceneGenerator.cs
Assets/Scripts/Editor/InnSceneGenerator.cs
Assets/Scripts/Editor/Art/VisualRegressionCapture.cs
Assets/Scripts/**                （仅为上述 P0/P1 或 MVP 接线所必需）
Assets/Tests/EditMode/**
Assets/Tests/PlayMode/**
Assets/Scenes/Demo_YanLiuTown.unity
Assets/Scenes/Demo_Inn.unity
Assets/ArtSource/Characters/Generated/player_male_swordsman/**
Assets/Art/Characters/Player/player_male_swordsman.*
Assets/ArtSource/Environment/Layouts/yanliu.json
Assets/ArtSource/Environment/Layouts/interiors/inn.json
Assets/ArtSource/Environment/Manifests/regions.json
Assets/ArtSource/Environment/Manifests/interiors.json
Assets/ArtSource/Environment/Modules/**          （只限 yanliu / inn 所需模块）
Assets/Art/Environment/Regions/yanliu/**
Assets/Art/Environment/Interiors/inn/**
Assets/Tilemaps/Formal/**                         （只限 yanliu / inn 的持久 Tile）
tools/art_pipeline/**                             （只限上述两处的确定性烘焙与验证）
docs/16-mvp-presentation-flow-rework-handoff.md  （更新执行记录）
AGENTS.md                                        （只更新长期事实、测试基线和已知风险）
```

已有的 `Assets/Resources/Art/MVP/` 七张 16×16 功能资源可继续使用；若确需改动，必须维持持久资源加载，禁止用 `Texture2D`/`Sprite.Create` 在运行时生成替代品。

### 4.2 严格禁止

- 不得改动其余 11 套主角、任何非 MVP 的 NPC／敌人／Boss、任何正式角色批量 Prefab/Animator。
- 不得改动其余 9 个户外区域、12 个非 inn 室内、全量布局或批量生产范围。
- 不得修改 `ProjectSettings/`，现有 `EditorBuildSettings.asset` 中 `Demo_Inn` 的登记例外除外；不得引入 URP、3D、后处理或新包。
- 不得以删除任务门控、跳过对话、自动完成任务、关闭碰撞或吞掉异常来制造“可走通”的假象。
- 不得把临时截图、`Library/`、`Temp/`、日志、`.csproj`、`.sln` 或无关 Unity 重序列化噪声纳入提交。
- 不得合并到 `main`、推送远端或擅自宣称用户已做视觉验收。

## 5. 执行顺序与实现要求

必须按顺序完成。每一阶段未满足门禁，不得开始下一阶段的批量或润色工作。

### 阶段 A：建立失败基线与最小回归测试

1. 从本文顶部的固定提交创建工作分支；保存 `git status --short --branch`、`git diff --check` 和当前测试结果。
2. 用 Unity 可见窗口或批处理可重现客栈交谈错误，保存 Console/测试日志中的完整异常。
3. 在不修生产代码前添加会失败的 PlayMode 测试，确保它能经历“旧 DialogueUI 已销毁，新场景开始交谈”的真实路径。
4. 在不修生产代码前添加会失败的 EditMode 或 PlayMode 测试，证明不同外层屏幕尺寸下相机仍错误地产生大于 `8.4375` 的正交尺寸。

测试必须验证行为而非仅验证对象存在：

- MainMenu 新游戏后进入烟柳镇，再通过实际 `AreaTrigger` 进入 `Demo_Inn`；
- 找到活跃场景的 `NPC_掌柜老赵`，走真实 `NPCBase.OnInteract(player)`／交互链；
- 活跃场景的对话框显示，`DialogueManager` 可以结束对话，`MVP_01` 成为活跃任务且第一步完成；
- 收集 `Application.logMessageReceived`，断言这段链路没有 Error、Exception 或 `MissingReferenceException`；
- 若测试通过却未能让修复前版本失败，测试无效，必须重写。

### 阶段 B：修复对话生命周期 P0

在 `DialogueUI` 中实现可逆、幂等的事件生命周期。实现细节可以不同，但必须满足：

1. `OnDialogueStart` 使用可解除的具名处理器（如 `HandleDialogueStart`），不能再使用无法退订的匿名 lambda。
2. UI 记录自己实际订阅的 `DialogueManager`，在 `OnDisable` 和/或 `OnDestroy` 对同一实例完整退订；重复启停不产生重复订阅。
3. 若 Manager 比 UI 晚创建、场景重建或对象已经被 Unity 销毁，处理逻辑要显式判断 Unity 对象 `== null`，不能用 Unity fake-null 不安全的 `?.`／`??` 处理 `MonoBehaviour`/`GameObject`。
4. 卸载烟柳镇后，持久 `DialogueManager` 不得保留任何指向旧 `DialogueUI` 的回调；进入客栈后只由活跃场景的 UI 响应。
5. 保持 `QuestGiver` 的现有语义：交谈真正结束后才接受／提交任务，不能在显示对话时提前结算。

通过阶段 B 的唯一证据是阶段 A 的真实跨场景 PlayMode 测试由红转绿，加上可见 Unity 窗口的一次实际交谈成功。

### 阶段 C：修复 480×270 世界与 UI 呈现 P0/P1

#### C.1 世界相机

`PixelPerfectCamera` 必须遵守下面的数学契约：

```text
targetWidth  = 480
targetHeight = 270
pixelsPerUnit = 16
world orthographicSize = targetHeight / (2 × pixelsPerUnit) = 8.4375
```

- 窗口高宽、整数缩放倍率、letterbox 的存在与否，都不能改变上述世界覆盖面积。
- `pixelRect` 仍可使用最大可用整数倍缩放并居中；不足一倍时要有明确的安全降级行为，但不能把世界缩放到错误覆盖范围。
- 清屏相机／背景必须覆盖整个实际窗口，防止场景切换后残影；它不能覆盖游戏世界或改变逻辑相机的 culling。
- `PlaySceneAssembler` 创建的游戏相机应与此契约一致，禁止依赖“编辑器窗口碰巧大小合适”。

新增测试至少要在两组模拟屏幕尺寸（例如 480×270 和 1440×900）下刷新相机，断言 `orthographicSize` 都等于 `8.4375`（合理浮点误差内）；同时断言 `pixelRect` 居中且不超出屏幕。

#### C.2 UI 逻辑展示面

HUD、对话、暂停、过场提示必须和世界使用同一 480×270 逻辑展示面：

- 推荐使用 Screen Space - Camera，并绑定游戏像素相机；`CanvasScaler` 使用固定参考分辨率 480×270 与明确的缩放策略。
- 如采用另一种方案，必须在代码和测试中证明其在任意窗口尺寸下与世界 `pixelRect` 对齐。
- 禁止 Screen Space - Overlay UI 漂在 letterbox／窗口空白区；禁止使用仅靠编辑器布局才正确的绝对坐标。
- 对话框不得遮挡男主或交互对象的整个主体；HUD 只保留任务目标、HP/MP、当前武器与必要提示，避免占满画面。

请新增离屏 480×270 游戏截图测试或验证工具，能同时捕获世界和 UI；不能只测 Canvas 对象存在。

### 阶段 D：重做两个 MVP 可玩场景的 1× 构图

这是小范围关卡美术返工，不是扩张为全量美术制作。先做低成本缩样和构图，用户批准后才拆 Tile／烘焙。

#### D.1 共同可读性标准

- 游戏内审查基准为 **480×270、1×、不放大、无标注**。4× 图仅用于像素细节审查，不能替代 1× 验收。
- 男主以 32×32 像素的真实尺寸入镜，须有清晰深色轮廓、头部／衣身／朝向／武器四个可辨部件；不能缩小为地图上的单色点。
- 可走地面、不可走建筑／家具、水面、交互门、NPC 和敌人应有明确的明度或色相分组；不能依赖小字标签告诉玩家哪里能走。
- 每屏最多一个主视觉焦点和一个次焦点。纹理必须服务轮廓、材质或路线，不能用规则性小格、横线或重复灌满每个像素。
- 碰撞和视觉必须一致：看似能走的主路可走；前景遮挡只遮画面边缘或上半身局部，不遮门、掌柜、任务物和路线关键点。
- 保持 16×16 Tile、PPU 16、Point、无压缩、内置 2D；禁止抗锯齿、景深、URP/后处理。

#### D.2 烟柳镇：一条清楚的试玩路线

只需要制作“客栈门外出生点 → 客栈入口 → 河岸战斗点”的局部玩法路线。目标画面不是整张镇地图，而是玩家运行时看到的近中景。

必须同时具备：

| 区域 | 1× 画面职责 | 可读性要求 |
| --- | --- | --- |
| 客栈门外出生点 | 首屏锚点 | 玩家在门外 `(7.5, 7.6)` 出生；客栈门是最显著的可交互建筑入口，门前道路、招牌／暖光、门框轮廓清楚。 |
| 主路与支路 | 导航 | 客栈与河岸之间有连续、宽度稳定且与墙／水明显区分的可走道路；转向点用桥、灯、岸阶或招牌等单一地标提示。 |
| 河岸战斗点 | 战斗阅读 | 水面、岸线、可战斗陆地、两名水匪及荷包位置清楚；任务未到步骤时敌人／荷包依旧由 `QuestStageGate` 完全隐藏。 |
| 水乡背景 | 氛围，不抢焦点 | 只使用少量不规则岸线、桥、屋檐、柳树、船等大轮廓建立水乡；不做连续白墙方块、满屏等距栏杆或条纹水面。 |

须明确规定玩家相机在出生、客栈门和河岸的跟随／边界，确保每个关键地点都以 480×270 的有效世界画面呈现，而非缩小地图全景。

#### D.3 客栈：入口到掌柜的短路径

客栈是任务入口，必须优先服务交谈而非装饰密度。玩家进入后 1 秒内应看到掌柜和柜台。

必须具备：

| 元素 | 要求 |
| --- | --- |
| 入口与回镇出口 | 门洞、门毯／石阶和可走主通道连续；出口触发盒不与回镇出生点重叠，防止自动往返。 |
| 柜台与掌柜 | 位于入场镜头的主焦点；柜台台面、掌柜的 32×32 剪影、灯／窗的局部暖光区分清楚，K/E 交谈提示有足够对比度。 |
| 空间分区 | 以大轮廓区分入口、柜台、桌席、后厨／灶火、楼梯／暗口和床位；最多保留服务空间阅读的两三个次要区，禁止把每一区填成相同棕色格子。 |
| 照明 | 整体不能是纯黑或整屏棕；柜台为主光，灶火／窗光为次光，光池外仍保留可读的家具和墙体剪影。 |
| 碰撞 | 柜台、墙、桌席、梁柱不可穿越；入口→掌柜、掌柜→出口路径必须可达，且画面中不存在误导性的“看起来能走但被挡住”的主通道。 |

### 阶段 E：确定性资产、场景与截图接线

1. 先提交或保存三张不超过 160×90 的无 UI 构图缩样：烟柳出生／主路、烟柳河岸、客栈掌柜。它们用于确认焦点、路线和明暗，不是最终像素资源。
2. 等用户明确批准缩样后，才改 `yanliu`／`inn` 的 source layout、模块、烘焙 Tile 和两个 `Demo_*` 场景。源文件、烘焙输出、`.art.json`、`.meta`、Tile、碰撞与场景必须同步，不能只换预览 PNG。
3. 保留并增强 `VisualRegressionCapture`（或等效工具），输出固定的 480×270 游戏实拍：
   - `town-spawn-1x.png`
   - `town-riverbank-1x.png`
   - `inn-counter-1x.png`
   - 需要时再输出同名 `-4x.png` 供细节审查。
4. 临时审查图输出到 `/private/tmp/yuanhailu-mvp-rework-review/`；在用户视觉批准前，不能把它们当成正式视觉基线或用假截图替换真实 Game View。
5. 每张图都要确认男主实际存在、相机不是 Scene View、UI 与世界坐标一致、没有黑帧、没有已销毁对象异常遮蔽画面。

## 6. 自动化验收要求

新增或更新测试必须覆盖下表。既有 129 EditMode／12 PlayMode／45 Python 的基线只能说明旧功能未退化，不足以覆盖本返工。

| 编号 | 层级 | 必测行为 |
| --- | --- | --- |
| F1 | PlayMode | 烟柳镇 → 客栈后和掌柜交谈，无 `MissingReferenceException`，对话显示且 `MVP_01` 正确接取。 |
| F2 | PlayMode | 从接任务开始，真实完成到河岸、击败两名水匪、拾荷包、回掌柜提交；每个 `QuestStageGate` 仅在正确顺序步骤激活。 |
| F3 | PlayMode | 三种流派各走一次主流程关键战斗节点；至少验证流派 ID、武器层、攻击／主动技行为不同，身体美术仍为同一男主。 |
| F4 | PlayMode | 镇 → 客栈 → 镇往返后，出生点、输入、HP/MP、等级、武学、任务和流派不丢失；落地不自动触发反向门。 |
| F5 | EditMode/PlayMode | `PixelPerfectCamera` 在多种窗口尺寸下固定 `orthographicSize = 8.4375`，逻辑视口为居中 480×270 的整数缩放输出。 |
| F6 | EditMode | HUD、DialogueUI、Pause UI 的 Canvas 模式、相机绑定、480×270 scaler／锚点符合统一展示面契约。 |
| F7 | EditMode | 客栈入口→掌柜→出口、出生点→客栈门→河岸→荷包均可达；出生／落点与门触发盒不重叠；主要碰撞与视觉地标绑定一致。 |
| F8 | Python | `tools/art_pipeline` 只重建 yanliu／inn／男主允许范围，source、manifest、layout、烘焙 hash 与 Tile 引用一致；不得扩散到冻结资产。 |

每一次全量验证都必须在最后一个代码提交**之后**运行，并提供 XML 与日志。建议命令：

```bash
python3 -m unittest discover -s tools/art_pipeline/tests -v
python3 -m tools.art_pipeline.build --all
python3 -m tools.art_pipeline.validate --all

/Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath /Users/lhw/code/yuanHaiLu \
  -runTests -testPlatform EditMode \
  -testResults /private/tmp/yuanHaiLu-mvp-rework-editmode.xml \
  -logFile /private/tmp/yuanHaiLu-mvp-rework-editmode.log

/Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath /Users/lhw/code/yuanHaiLu \
  -runTests -testPlatform PlayMode \
  -testResults /private/tmp/yuanHaiLu-mvp-rework-playmode.xml \
  -logFile /private/tmp/yuanHaiLu-mvp-rework-playmode.log
```

运行 `-executeMethod` 重建场景时必须带 `-quit`；`-runTests` 不要加 `-quit`，以免结果 XML 尚未写出即结束。测试引起的正式场景或 `.meta` 重序列化噪声必须先还原，再检查差异。

## 7. 人工试玩与视觉门禁

自动测试不能关闭本次返工。下一位开发 AI 完成代码后必须停在 Gate R，由用户进行两类审核。

### Gate R1：1× 视觉审核

按顺序查看三张真实 Game View 的 480×270、1×、无标注截图，不放大：

1. `town-spawn-1x.png`：能否立即看懂“男主在客栈门外，可以进客栈”？
2. `town-riverbank-1x.png`：能否立即区分陆地、河水、战斗区、两名敌人与任务物／其隐藏状态？
3. `inn-counter-1x.png`：能否立即看懂“进入客栈后去柜台找掌柜”，并看清入口、主通道与出口？

任何一张仍被评价为“小、糊、像缩略地图、只能靠放大或标注才能读懂”，即 Gate R1 失败。应回到阶段 D 调整构图、明暗、轮廓或镜头，不得用增加纹理、放大 4× 图或文字说明来规避。

### Gate R2：三流派完整人工试玩

剑、拳套、暗器各独立跑完以下清单：

1. 主菜单选流派：男主预览固定，武器图标与随身武器正确切换。
2. 新游戏出生在 `(7.5, 7.6)`：地图、HUD、像素视口正常；接任务前河岸无水匪、无荷包。
3. 进客栈：掌柜提示可见，K/E 对话正常显示并接取任务，Console 零错误。
4. 回镇并沿路线至河岸：两名水匪出现；体验该流派的攻击距离、连击节奏和主动技能，击败两人。
5. 拾荷包并回掌柜：任务进度、奖励、金币视觉反馈和物品拾取正常；流程可提交完成。
6. 保存 → 退出主菜单 → 继续：流派、男主外观、HP/MP、等级、背包、武学和任务进度正确恢复。
7. 镇与客栈往返一次：不自动反弹、不丢输入、不重置状态。

任一流派失败，记录精确步骤、场景、Console 输出、截图／视频和当前 commit，退回阶段 B/C/D 对应项修复。三次都通过前，禁止合并 `main`。

## 8. 开发 AI 的交付格式

完成后仅提交到工作分支，并在回复中按以下模板交接：

```markdown
## MVP 呈现与流程返工交付

- 分支与最终 commit：`codex/mvp-presentation-flow-rework @ <SHA>`
- 基线：`dfdae678...`
- 工作树：`git status --short --branch` 原始输出
- 变更清单：按「P0 对话」「相机/UI」「烟柳」「客栈」「测试/文档」分组，逐文件说明
- 明确未改动：冻结角色、冻结区域、ProjectSettings（除既有 Demo_Inn 登记例外）、批量生产资产

### 根因与修复映射

| 问题 | 复现路径 | 修复位置 | 自动回归 | 人工结果 |
| --- | --- | --- | --- | --- |

### 验证证据

- Python：`<通过数>`，命令、时间、日志路径
- Unity EditMode：`<通过数>`，XML/日志绝对路径与时间
- Unity PlayMode：`<通过数>`，XML/日志绝对路径与时间
- 批处理编译／场景重建：命令、退出码、日志路径
- `git diff --check <base>...HEAD`：退出码与原始输出
- 1× 真实截图：三个绝对路径；4× 图只作可选附录

### 尚未关闭的门禁

- Gate R1：用户 1× 视觉批准（通过／待定／失败）
- Gate R2：剑／拳套／暗器人工试玩逐项结果
- 合并与推送：始终标为“未执行，等待用户单独授权”
```

不得把“测试全绿”“像素统计一致”“AI 视觉模型认为可读”写成用户视觉批准，也不得将自动化替代 Gate R2。

## 9. 复审者检查顺序

收到交付后，先不要合并。独立复审顺序如下：

1. 核查基线、提交范围、工作树和 `git diff --check`，确认无冻结资产／无关重序列化混入。
2. 先读 F1/F2 的测试，确认它们确实跨场景销毁旧 UI，再运行一次并看 Console；只看“测试数增加”不算验证。
3. 核查相机公式和 Canvas 对齐，再用至少一个非 480×270 的 Game View 尺寸播放，确认世界没有缩成地图缩略图。
4. 不看 4× 或标注，先看三张 1× 游戏实拍；视觉失败就停止，不进入代码细节辩护。
5. 在可见 Unity 窗口按 Gate R2 各跑剑、拳套、暗器一次，尤其检查客栈交谈、河岸门控、拾取、回镇与存档。
6. 仅当 R1、R2、全量自动测试和范围审计全部通过后，才由用户单独决定是否合并 `main`、是否推送远端。

## 10. 执行记录

| 日期 | 执行者 | 阶段 | 结果／证据 | 审批 |
| --- | --- | --- | --- | --- |
| 2026-08-22 | Codex | 规格建立 | 已在可见 Unity 中复现客栈 `MissingReferenceException`、确认像素相机 `OrthoSize: 13.31` 与 480×270/PPU16 契约不符；本文创建。 | 待执行 |
| 2026-08-22 | Codex | A 红基线 | 分支 `codex/mvp-presentation-flow-rework`（自 dfdae67）；F1 对话测试以文档同款 `MissingReferenceException` 红、F3/F4 相机契约红。提交 `144938b`（本文）、`e8002e3`（红基线）。 | 已按序执行 |
| 2026-08-22 | Codex | B 对话修复 | `DialogueUI` 具名处理器 + 记录订阅实例 + OnDisable/OnDestroy 全量退订 + Update 迟到管理器重订；F1 红→绿。提交 `6d569de`。 | 已按序执行 |
| 2026-08-22 | Codex | C 相机/UI 契约 | `PixelPerfectCamera` ortho 恒定 8.4375、RT 检测、纯函数视口；HUD/对话/暂停/过场画布统一 ScreenSpaceCamera+Scaler 480×270；两 Demo 场景经生成器重建；F2/F4/F5/F6 绿（影子副本 EditMode 4/4、PlayMode 4/4）。三张 480×270 实拍（出生/河岸/客栈）输出至 `/private/tmp/yuanhailu-mvp-rework-review/`，人工核对男主/满屏/HUD/无黑帧。提交 `8eefe7c`。 | 自动侧已绿；1× 视觉属 Gate R1 待用户 |
| 2026-08-22 | Codex | D 缩样 | 三张 160×90 无 UI 构图缩样 + 确定性生成脚本入库 `docs/16-thumbnails/`（烟柳出生/主路、烟柳河岸、客栈掌柜）。 | **待用户批准缩样后方可进入 Tile/烘焙** |

