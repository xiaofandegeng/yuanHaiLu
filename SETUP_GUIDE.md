# 渊海录 — Unity 工程搭建指南

## 📋 前置条件

- **Unity 版本**: 2022.3 LTS（推荐） 或 2023.2+
- **模板**: 2D (URP) 或 2D Core
- **OS**: macOS (Apple Silicon)

---

## 🚀 Step 1: 安装 Unity

1. 访问 https://unity.com/download 下载 **Unity Hub**
2. 安装 Hub 后，打开它
3. 点击 **Installs** → **Install Editor**
4. 选择 **2022.3 LTS**（长期支持版）
5. 勾选模块:
   - ✅ WebGL Build Support（如需网页版）
   - ✅ iOS Build Support（如需手机版）
   - ✅ Android Build Support

---

## 🎮 Step 2: 创建项目

1. Unity Hub → **Projects** → **New Project**
2. 选择 **2D (URP)** 模板
3. 项目名: `YuanHaiLu`（渊海录）
4. 位置: 选择你喜欢的目录

---

## 📂 Step 3: 导入工程文件

将本目录下的文件复制到 Unity 项目中：

```bash
# 假设 Unity 项目路径为 ~/UnityProjects/YuanHaiLu
# 我们的工程文件在 wuxia-game/unity-project/

# 复制脚本
cp -r unity-project/Assets/Scripts/ ~/UnityProjects/YuanHaiLu/Assets/Scripts/

# 复制精灵表素材（之前 Python 生成的）
cp -r wuxia-game/assets/sprites/*.png ~/UnityProjects/YuanHaiLu/Assets/Sprites/Characters/Hero/
cp -r wuxia-game/assets/tilesets/*.png ~/UnityProjects/YuanHaiLu/Assets/Sprites/Tiles/

# 复制文档
cp -r wuxia-game/docs/ ~/UnityProjects/YuanHaiLu/Assets/Docs/
```

---

## ⚙️ Step 4: Unity 编辑器设置

### 4.1 像素完美设置

**Edit → Project Settings → Quality:**
- Anti Aliasing: **Disabled**
- VSync: **Don't Sync**
- Anisotropic Textures: **Disabled**

**Edit → Project Settings → Player:**
- Resolution → Default Screen Width: **1280**
- Resolution → Default Screen Height: **720**
- Resolution → Fullscreen Mode: **Windowed**
- Run In Background: ✅

### 4.2 物理设置（2D Top-Down）

**Edit → Project Settings → Physics 2D:**
- Gravity: X=0, Y=0（俯视角不需要重力）
- Velocity Iterations: 8
- Position Iterations: 3

### 4.3 输入设置

已配置在 `ProjectSettings/ProjectSettings.asset` 中：

| 动作 | 按键 | 备选 |
|------|------|------|
| 移动 | WASD / 方向键 | - |
| 攻击 | J | Left Ctrl |
| 交互 | K | E |
| 冲刺 | Left Shift | Space |
| 菜单 | Escape | Tab |

### 4.4 图层（Layers）

在 **Tags & Layers** 中添加：
```
- Ground
- Environment
- Character
- Interactable
- Enemy
- Player
```

### 4.5 排序层（Sorting Layers）

在 **Sorting Layers** 中按顺序添加（从后到前）：
```
1. Ground
2. Environment
3. Character
4. Foreground
5. UI
```

---

## 🏗️ Step 5: 场景搭建

### 5.1 主场景结构 (MainMenu)

```
 MainMenu (Scene)
 ├── Main Camera
 │   └── PixelPerfectCamera.cs
 ├── Canvas (Screen Space - Overlay)
 │   ├── Background
 │   ├── Title "渊海录"
 │   ├── NewGameButton
 │   ├── ContinueButton
 │   ├── SettingsButton
 │   └── QuitButton
 │   └── MainMenu.cs
 ├── GameManager (Empty)
 │   ├── GameManager.cs
 │   └── SaveManager.cs
 └── EventSystem
```

### 5.2 游戏场景结构 (YanLiuTown)

```
 YanLiuTown (Scene)
 ├── Main Camera
 │   ├── PixelPerfectCamera.cs
 │   └── Camera Follow Target
 ├── Grid
 │   ├── Ground Tilemap (Sorting: Ground)
 │   ├── Environment Tilemap (Sorting: Environment)
 │   ├── Collision Tilemap (invisible)
 │   └── Foreground Tilemap (Sorting: Foreground)
 │   └── TileMapManager.cs
 ├── Player
 │   ├── SpriteRenderer (Sorting: Character)
 │   ├── Rigidbody2D (Gravity=0, Freeze Rotation)
 │   ├── BoxCollider2D
 │   ├── Animator
 │   ├── PlayerController.cs
 │   ├── CharacterStats.cs
 │   └── PlayerCombat.cs
 ├── NPCs/
 │   ├── NPC_Villager_01
 │   │   ├── SpriteRenderer
 │   │   ├── BoxCollider2D (Is Trigger)
 │   │   └── NPCBase.cs
 │   └── ...
 ├── Enemies/
 │   ├── Bandit_01
 │   │   ├── SpriteRenderer
 │   │   ├── Rigidbody2D
 │   │   ├── BoxCollider2D
 │   │   └── EnemyAI.cs + CharacterStats.cs
 │   └── ...
 ├── AreaTriggers/
 │   ├── Exit_North → AreaTrigger.cs
 │   └── Exit_South → AreaTrigger.cs
 ├── Canvas (HUD)
 │   ├── HP Bar
 │   ├── MP Bar
 │   ├── Stamina Bar
 │   ├── Interact Prompt
 │   └── HUD.cs
 ├── DialogueCanvas
 │   ├── DialoguePanel
 │   │   ├── SpeakerName
 │   │   ├── Portrait
 │   │   └── DialogueText
 │   └── DialogueManager.cs
 ├── GameManager (DontDestroyOnLoad)
 └── EventSystem
```

---

## 🎨 Step 6: 精灵表导入设置

导入之前生成的精灵表 PNG 后，在 Inspector 中设置：

### 角色精灵表 (lingshuang_walk_v2.png 等)

| 设置 | 值 |
|------|-----|
| Texture Type | Sprite (2D and UI) |
| Sprite Mode | Multiple |
| Pixels Per Unit | 16 |
| Filter Mode | Point (no filter) |
| Compression | None |
| Max Size | 2048 |

点击 **Sprite Editor** → **Slice**:
- Type: **Grid By Cell Size**
- Pixel Size: **48 x 48**
- Pivot: **Bottom Center** (0.5, 0)
- 点击 Slice

### 瓦片集 (yanliu_town_tileset.png)

| 设置 | 值 |
|------|-----|
| Texture Type | Sprite (2D and UI) |
| Sprite Mode | Multiple |
| Pixels Per Unit | 16 |
| Filter Mode | Point (no filter) |

Sprite Editor → Slice:
- Type: **Grid By Cell Size**
- Pixel Size: **16 x 16**
- Pivot: **Center**

---

## 🎬 Step 7: Animator 设置

### 玩家角色 Animator

创建 Animator Controller: `Assets/AnimatorControllers/Hero.controller`

**参数:**
- Float: `MoveX` (-1 ~ 1)
- Float: `MoveY` (-1 ~ 1)
- Float: `Speed` (0 ~ 1)
- Bool: `IsDashing`
- Bool: `IsAttacking`
- Int: `AttackIndex`

**状态机结构:**
```
                  ┌─────────────────┐
                  │   Idle_Blend    │ ←── Speed < 0.01
                  │  (Blend Tree)   │
                  └────────┬────────┘
                           │ Speed > 0.01
                  ┌────────▼────────┐
                  │   Walk_Blend    │
                  │  (Blend Tree)   │
                  └────────┬────────┘
                           │ IsDashing
                  ┌────────▼────────┐
                  │    Dash_Blend   │
                  │  (Blend Tree)   │
                  └────────┬────────┘
                           │ IsAttacking
                  ┌────────▼────────┐
                  │    Attack_0/1/2 │ ←── AttackIndex
                  └─────────────────┘
```

每个 Blend Tree 的方向参数:
- (0, -1) → Down  (正面)
- (0, 1)  → Up    (背面)
- (-1, 0) → Left  (左)
- (1, 0)  → Right (右)

---

## 📦 Step 8: 必装 Package

在 **Window → Package Manager** 中安装:

| Package | 用途 |
|---------|------|
| 2D Tilemap Editor | 瓦片地图编辑 |
| 2D Pixel Perfect | 像素完美渲染（可选，我们自定义了） |
| 2D Animation | 骨骼动画（可选） |
| Cinemachine | 摄像机控制（可选） |
| TextMeshPro | UI文字渲染 |
| Input System (New) | 新输入系统（可选） |

---

## 🔧 快速验证清单

打开 Unity 后，按顺序验证：

- [ ] 创建空场景，添加 Camera + PixelPerfectCamera.cs
- [ ] 运行，确认画面是像素化的（无模糊）
- [ ] 导入角色精灵表，切分为 48x48 帧
- [ ] 创建 Player 物体，挂载 PlayerController + CharacterStats + PlayerCombat
- [ ] 运行，确认 WASD 移动正常
- [ ] 导入瓦片集，创建 Tilemap
- [ ] 画一个简单的测试地图
- [ ] 添加一个 NPC（NPCBase.cs），按 K 交互
- [ ] 添加一个敌人（EnemyAI.cs），靠近追击
- [ ] 全部通过 → 开始正式开发！

---

## 📁 最终目录结构

```
YuanHaiLu/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/                    # 核心系统
│   │   │   ├── GameConfig.cs        ← 全局常量
│   │   │   ├── GameManager.cs       ← 游戏管理器
│   │   │   └── PixelPerfectCamera.cs← 像素摄像机
│   │   ├── Character/               # 角色系统
│   │   │   ├── PlayerController.cs  ← 玩家控制
│   │   │   ├── PlayerCombat.cs      ← 战斗系统
│   │   │   ├── CharacterStats.cs    ← 属性系统
│   │   │   ├── NPCBase.cs           ← NPC基类
│   │   │   └── EnemyAI.cs           ← 敌人AI
│   │   ├── Dialogue/                # 对话系统
│   │   │   └── DialogueManager.cs   ← 对话管理
│   │   ├── Map/                     # 地图系统
│   │   │   ├── TileMapManager.cs    ← 瓦片管理
│   │   │   └── AreaTrigger.cs       ← 区域切换
│   │   ├── UI/                      # 界面
│   │   │   ├── HUD.cs               ← 游戏HUD
│   │   │   └── MainMenu.cs          ← 主菜单
│   │   └── System/                  # 系统
│   │       └── SaveManager.cs       ← 存档管理
│   ├── Sprites/                     # 精灵资源
│   │   ├── Characters/Hero/         ← 凌霜精灵表
│   │   ├── Characters/NPC/          ← NPC精灵
│   │   ├── Tiles/                   ← 瓦片集
│   │   └── UI/                      ← UI素材
│   ├── Tilemaps/                    ← 瓦片地图场景数据
│   ├── Prefabs/                     ← 预制体
│   ├── Scenes/                      ← 场景文件
│   ├── Animations/                  ← 动画剪辑
│   ├── AnimatorControllers/         ← 动画控制器
│   ├── Fonts/                       ← 字体（像素中文字体）
│   └── Art/                         ← 美术参考
├── ProjectSettings/
│   ├── ProjectSettings.asset        ← 输入+质量配置
│   └── GraphicsSettings.asset       ← 渲染配置
└── Packages/
```

---

## 💡 开发优先级建议

### Phase 0: 原型验证（1-2周）
1. ✅ 像素完美摄像机
2. ✅ 角色移动
3. ✅ 瓦片地图绘制
4. 碰撞检测
5. 简单NPC交互

### Phase 1: 核心系统（3-4周）
6. 战斗系统（连击）
7. 对话系统完善
8. 背包/物品系统
9. 任务系统
10. 存档系统

### Phase 2: 内容填充（6-8周）
11. 烟柳镇完整地图
12. 5-10个NPC + 对话
13. 3-5种敌人
14. 主线剧情第一章
15. 2-3个支线任务

### Phase 3: 打磨（4-6周）
16. 音效/BGM
17. 粒子特效
18. UI美化
19. 性能优化
20. Bug修复
