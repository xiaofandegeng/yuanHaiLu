# MVP 模块化像素美术：实施记录与交接

> 状态：实施中，尚未合并、未推送、未取得用户视觉验收。
>
> 工作分支：`codex/mvp-presentation-flow-rework`。
> 本文取代 `docs/17-mvp-art-integration-rework.md` 中「三个 480×270 静态图层」作为后续实施方案；`docs/15` 的单男主 MVP 范围与任务语义不变。

## 1. 为什么要再次替换旧方案

上一版虽然把背景分成 `Ground / Environment / Foreground` 三张 480×270 图片，但它们仍是整屏平铺：角色、门、桥、柜台和任务物像贴在画上，不能产生可维护的深度、遮挡或空间关系。

本轮采用原创的「高密度俯视像素江湖」方向：用明度、轮廓、少量暖光和场景功能组织画面。该方向参考的是品类层面的可读性原则，不复制《大侠立志传》或其他游戏的任何角色、场景、UI、像素簇或布局。

## 2. 已落地的资产契约

### 2.1 固定男主和武器

- `player_male_swordsman` 是唯一允许 `48×48` 帧的正式角色；其他 96 个正式角色仍为 `32×32`。
- `tools/art_pipeline/mvp_dense_art_builder.py` 以六张可编辑层写入男主全部既有动画行：深色发髻、靛蓝短披、纸白内衫、朱砂腰绦、腰侧剑鞘和落地阴影均在 1× 有独立轮廓。
- `weapon_sword`、`weapon_gauntlets`、`weapon_dart` 仍使用原有稳定资源 ID，避免破坏主菜单与 `PlayerCombat`。
- `CharacterAnimationBuilder.RebuildOnly("player_male_swordsman")` 是唯一的 Unity 局部重建入口；它不删除 97 个角色的 Controller/Prefab 根目录。

### 2.2 小模块场景源

新增的可编辑源和对应烘焙输出为：

```text
Assets/ArtSource/MVP/dense_pixel/
  layouts/town.json
  layouts/inn.json
  environment/town/*.png
  environment/inn/*.png
  actors/*.png

Assets/Art/MVP/dense_pixel/environment/{town,inn}/*.png
Assets/Resources/Art/MVP/dense_pixel/actors/*.png
```

所有模块均为 `16×16`、`32×32`、`48×48` 或 `64×64`，使用 Point 像素绘制；没有新的整屏运行时背景资源。

`town.json` 保留既有世界坐标和游戏契约：出生点 `(7.5, 7.6)`、客栈门 `(7.5, 9.9)`、河岸 `(12, 5.2)`、荷包 `(24, 3)`。它包含石路、水面/岸线、客栈立面、桥、船、系船柱、灯笼与边缘柳枝等角色。

`inn.json` 保留入口 `(15, 2.5)`、掌柜 `(15, 10)` 与北侧出口；它明确区分门厅、中央走道、柜台灯、后厨灶火、桌席、楼梯、北门和两条边缘前景梁柱。前景总面积受布局数据限制，不能遮挡中间交互路线。

## 3. 已清理的冗余实现

旧的 `character_source_builder.py` 曾内嵌一套只服务男主的 32px 绘制函数；在男主升级为 48px 后，它会在批量源生成时重新覆盖新版源表。

现已清理该重复职责：

1. 旧 32px 男主专用绘制函数和调色板已经删除。
2. 旧通用源生成器对 48px 男主会明确报错，不能静默生成错误素材。
3. `character_source_builder --all` 和 `--id player_male_swordsman` 会转交给 `mvp_dense_art_builder`，其他 96 名角色仍由旧工具处理。

仍待下一阶段清理的内容：

> 2026-08-23 补记：上一代 v1 产物已先行删除——`mvp_backdrop_builder.py`（含测试）、`Assets/Art/Environment/MVP/mvp_*_backdrop.png`、`Assets/ArtSource/Environment/MVP/*_concept_v1.png` 与 `docs/16-thumbnails/` 全目录（提交 `1e14b52`）。它们不在下方保护清单内：构建注册已在 `181fd79` 移除、场景/代码零引用。下列 v2 过渡回滚点仍按原约束保留。

- `tools/art_pipeline/mvp_scene_layer_builder.py`；
- `Assets/ArtSource/Environment/MVP/v2/` 与 `Assets/Art/Environment/MVP/v2/` 的三张整屏层；
- `PlaySceneAssembler.CreateMvpSceneLayers` 及两个生成器中的三层常量；
- 只断言整屏层的旧测试。

这些文件目前是**过渡回滚点**。在 `MvpWorldModule` 装配器、Demo 场景重建和 1× 截图测试全部转绿之前，禁止提前删除。

## 4. 当前验证记录

本轮资源构建后的最新离线结果：

```text
python3 -m unittest tools.art_pipeline.tests.test_mvp_dense_art_builder -v
  3 / 3 通过

python3 -m tools.art_pipeline.build --all
  built=0 skipped=219

python3 -m tools.art_pipeline.validate --all
  通过
```

Python 全量回归和 Unity 回归必须在最终代码提交后重新运行，不能以本节代替最终证据。

## 5. 当前阻塞：Unity 本机许可证

Unity 本机目前弹出“没有有效许可证”的系统提示；针对 `6000.4.10f1` 的批处理测试停在许可证阶段，尚未开始 NUnit 测试。因此以下 C# 改动处于“已写测试、等待编译验证”状态，不能宣称通过：

- `CharacterAnimationBuilder.RebuildOnly`；
- `FixedMaleHeroCanBeRebuiltIndependentlyAtFortyEightPixels` EditMode 测试；
- 后续 `MvpWorldModule` 场景装配器与 Demo/Inn 场景重建。

需要项目拥有者在 Unity Hub 中恢复该版本的有效许可证并完成编辑器要求的条款确认。该操作涉及软件条款，必须由用户本人完成。恢复后首先运行第 6 节的「恢复后第一轮验证」，不要直接删除旧图层。

## 6. 下一位开发者的严格执行顺序

### A. 恢复后第一轮验证

1. 在 Unity `6000.4.10f1` 重新导入当前分支。
2. 运行 `FixedMaleHeroCanBeRebuiltIndependentlyAtFortyEightPixels`，确认它会调用局部重建，并确认男主切片为 `48×48`、女剑客保持 `32×32`。
3. 运行全量 EditMode；按 `AGENTS.md` 还原测试引入的正式场景/角色重序列化噪声。

### B. 用模块装配器替代整屏图层

1. 先增加失败的 EditMode 结构测试：两个 Demo 场景必须存在 `MvpWorldModule`，不得再有 `[MVP Ground]`、`[MVP Environment]`、`[MVP Foreground]` 三个整屏对象；每个精灵都必须是持久资产且不大于 `64×64`。
2. 新建 `MvpWorldModule`、`MvpSceneModuleAssembler`、`MvpDenseSceneLayouts`。装配器按 `town.json` / `inn.json` 放置精灵，并明确映射 `Ground → Default/-100`、`Environment → Environment`、`Foreground → Foreground`。
3. 只在模块结构测试通过后修改 `DemoSceneGenerator`、`InnSceneGenerator` 和 `PlaySceneAssembler`。
4. 重建两个 Demo，运行路径 BFS、任务门控和场景往返测试，再看三张 480×270、1×、无标注截图。

### C. 删除过渡层并收口

只有 B 的全绿证据和用户对 1× 截图的批准都存在时，才删除第 3 节列出的整屏资产/构建器/测试，并从 `build.py` 移除旧 `mvp_scene_layer_builder` 调用。删除后必须再次运行全量 Python、EditMode、PlayMode 和三流派人工试玩。

## 7. 最终验证与交付门禁

```bash
rtk python3 -m unittest discover -s tools/art_pipeline/tests -v
rtk python3 -m tools.art_pipeline.build --all
rtk python3 -m tools.art_pipeline.validate --all

/Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath /Users/lhw/code/yuanHaiLu \
  -runTests -testPlatform EditMode \
  -testResults /private/tmp/yuanhailu-dense-editmode.xml \
  -logFile /private/tmp/yuanhailu-dense-editmode.log

/Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath /Users/lhw/code/yuanHaiLu \
  -runTests -testPlatform PlayMode \
  -testResults /private/tmp/yuanhailu-dense-playmode.xml \
  -logFile /private/tmp/yuanhailu-dense-playmode.log
```

在最终提交之后重新产生 XML/日志；恢复测试造成的无关 YAML/.meta 噪声后，执行：

```bash
rtk git diff --check main...HEAD
```

自动化不代替两道人工门禁：

- Gate R1：用户在不放大、无标注的三张 480×270 截图中确认人物、门、路线、河岸、柜台和掌柜都可立即辨认。
- Gate R2：剑、拳套、暗器各完整跑通一次「客栈接任务 → 河岸战斗 → 荷包 → 交付 → 保存/继续」。

在 Gate R1、Gate R2、全量验证和范围审计均通过前，不得合并 `main` 或推送远端。
