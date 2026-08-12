using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using YuanHaiLu.Core;
using YuanHaiLu.Character;
using YuanHaiLu.Dialogue;
using YuanHaiLu.Map;
using YuanHaiLu.GameSystem;
using YuanHaiLu.Effects;
using YuanHaiLu.UI;

namespace YuanHaiLu.Editor
{
    /// <summary>
    /// Demo场景一键生成器
    /// 菜单: Tools/渊海录/生成Demo场景
    /// 自动创建完整的可运行演示场景
    /// </summary>
    public static class DemoSceneGenerator
    {
        [MenuItem("Tools/渊海录/生成Demo场景")]
        public static void Generate()
        {
            Debug.Log("========================================");
            Debug.Log("  渊海录 Demo 场景生成器");
            Debug.Log("========================================");

            // 创建新场景
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 按顺序构建
            CreateGlobalManagers();
            CreateMainCamera();
            CreateMapGround();
            CreateMapWalls();
            CreateMapDecorations();
            CreatePlayer();
            CreateNPCs();
            CreateEnemies();
            CreateDestructibles();
            CreateEventTriggers();
            CreateAreaExits();
            CreateHUD();
            CreateDialogueUI();
            CreatePauseMenu();
            CreateCanvasSettings();

            // 场景引导脚本
            var directorObj = new GameObject("[SceneDirector]");
            directorObj.AddComponent<SceneDirector>();

            // 保存场景
            string scenePath = "Assets/Scenes/Demo_YanLiuTown.unity";
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, scenePath);

            Debug.Log($"========================================");
            Debug.Log($"  Demo场景生成完成！");
            Debug.Log($"  场景路径: {scenePath}");
            Debug.Log($"  按 Play 即可运行！");
            Debug.Log($"========================================");

            EditorUtility.DisplayDialog("Demo场景生成完成",
                "烟柳镇 Demo 场景已生成！\n\n" +
                "场景包含：\n" +
                "• 玩家角色（WASD移动，J攻击，K交互）\n" +
                "• 3个NPC（可对话）\n" +
                "• 2组敌人（会追击攻击）\n" +
                "• 可破坏木箱（掉落物品）\n" +
                "• 事件触发器（北山山贼战斗）\n" +
                "• HUD + 暂停菜单\n\n" +
                "按 Play 运行即可体验！",
                "太好了！");
        }

        // ========== 全局管理器 ==========
        private static void CreateGlobalManagers()
        {
            // GameManager
            var gmObj = new GameObject("[GameManager]");
            gmObj.AddComponent<GameManager>();

            var saveObj = new GameObject("SaveManager");
            saveObj.transform.SetParent(gmObj.transform);
            saveObj.AddComponent<SaveManager>();

            var invObj = new GameObject("InventoryManager");
            invObj.transform.SetParent(gmObj.transform);
            invObj.AddComponent<InventoryManager>();

            var questObj = new GameObject("QuestManager");
            questObj.transform.SetParent(gmObj.transform);
            questObj.AddComponent<QuestManager>();

            var timeObj = new GameObject("GameTimeManager");
            timeObj.transform.SetParent(gmObj.transform);
            timeObj.AddComponent<GameTimeManager>();

            // AudioManager
            var audioObj = new GameObject("[AudioManager]");
            audioObj.AddComponent<AudioManager>();

            // DialogueManager
            var dlgObj = new GameObject("[DialogueManager]");
            dlgObj.transform.SetParent(gmObj.transform);
            dlgObj.AddComponent<DialogueManager>();

            // EffectsManager
            var fxObj = new GameObject("[EffectsManager]");
            fxObj.AddComponent<EffectsManager>();

            // ScreenTransition
            var transObj = new GameObject("[ScreenTransition]");
            transObj.AddComponent<Canvas>();
            transObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            transObj.AddComponent<ScreenTransition>();

            // 物品数据库初始化
            var _ = ItemDatabase.AllItems; // 触发 BuildDatabase
            var __ = MartialSkillDatabase.AllSkills;

            // PlayerDeathHandler
            var deathObj = new GameObject("[PlayerDeathHandler]");
            deathObj.AddComponent<PlayerDeathHandler>();

            Debug.Log("[Demo] 全局管理器创建完成");
        }

        // ========== 摄像机 ==========
        private static void CreateMainCamera()
        {
            var camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";

            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.18f, 0.22f, 0.16f); // 暗绿底色

            camObj.AddComponent<AudioListener>();
            camObj.AddComponent<PixelPerfectCamera>();
            camObj.AddComponent<CameraFollow>();

            Debug.Log("[Demo] 摄像机创建完成");
        }

        // ========== 地面 ==========
        private static void CreateMapGround()
        {
            var gridObj = new GameObject("Grid");
            gridObj.AddComponent<Grid>();

            // 地面瓦片地图
            var groundObj = new GameObject("Ground");
            groundObj.transform.SetParent(gridObj.transform);
            var groundTm = groundObj.AddComponent<Tilemap>();
            var groundTr = groundObj.AddComponent<TilemapRenderer>();
            groundTr.sortingLayerName = "Ground";
            groundTr.sortingOrder = 0;

            // 碰撞层
            var collisionObj = new GameObject("Collision");
            collisionObj.transform.SetParent(gridObj.transform);
            collisionObj.AddComponent<Tilemap>();
            collisionObj.AddComponent<TilemapCollider2D>();

            // 环境（墙壁等）
            var envObj = new GameObject("Environment");
            envObj.transform.SetParent(gridObj.transform);
            var envTm = envObj.AddComponent<Tilemap>();
            var envTr = envObj.AddComponent<TilemapRenderer>();
            envTr.sortingLayerName = "Environment";
            envTr.sortingOrder = 1;

            // 前景
            var fgObj = new GameObject("Foreground");
            fgObj.transform.SetParent(gridObj.transform);
            var fgTr = fgObj.AddComponent<TilemapRenderer>();
            fgTr.sortingLayerName = "Foreground";
            fgTr.sortingOrder = 10;

            // TileMapManager
            gridObj.AddComponent<TileMapManager>();

            // 用代码绘制彩色地面方块（临时占位）
            DrawGroundTiles(groundTm);

            Debug.Log("[Demo] 地面创建完成");
        }

        private static void DrawGroundTiles(Tilemap tm)
        {
            // 创建临时瓦片
            var tile = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
            var tex = new Texture2D(16, 16);
            Color32[] colors = new Color32[16 * 16];
            // 草地绿色
            Color32 grassColor = new Color32(120, 160, 80, 255);
            for (int i = 0; i < colors.Length; i++) colors[i] = grassColor;
            tex.SetPixels32(colors);
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            tile.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);

            // 铺设 30x20 的地面
            for (int x = -15; x < 15; x++)
            {
                for (int y = -10; y < 10; y++)
                {
                    tm.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }
        }

        // ========== 墙壁 ==========
        private static void CreateMapWalls()
        {
            // 用简单的 BoxCollider2D 充当墙壁
            var wallsObj = new GameObject("Walls");
            wallsObj.layer = LayerMask.NameToLayer("Environment");

            // 创建四面墙
            CreateWall(wallsObj, "Wall_North", new Vector2(0, 10.5f), new Vector2(30f, 1f));
            CreateWall(wallsObj, "Wall_South", new Vector2(0, -10.5f), new Vector2(30f, 1f));
            CreateWall(wallsObj, "Wall_West", new Vector2(-15.5f, 0), new Vector2(1f, 22f));
            CreateWall(wallsObj, "Wall_East", new Vector2(15.5f, 0), new Vector2(1f, 22f));

            // 内部障碍物（模拟房屋）
            CreateWall(wallsObj, "House_Inn", new Vector2(-8f, 4f), new Vector2(4f, 3f));
            CreateWall(wallsObj, "House_Shop", new Vector2(8f, 4f), new Vector2(4f, 3f));
            CreateWall(wallsObj, "House_Pharmacy", new Vector2(-8f, -6f), new Vector2(4f, 3f));

            // 装饰墙壁为棕色
            foreach (Transform wall in wallsObj.transform)
            {
                var sr = wall.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = new Color(0.6f, 0.4f, 0.2f, 0.8f);
            }

            Debug.Log("[Demo] 墙壁创建完成");
        }

        private static void CreateWall(GameObject parent, string name, Vector2 pos, Vector2 size)
        {
            var wall = new GameObject(name);
            wall.transform.SetParent(parent.transform);
            wall.transform.position = pos;
            wall.layer = LayerMask.NameToLayer("Environment");

            var sr = wall.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Environment";
            sr.drawMode = SpriteDrawMode.Sliced;

            // 创建方块Sprite
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 16);
            sr.size = size;

            var col = wall.AddComponent<BoxCollider2D>();
            col.size = size;
        }

        // ========== 装饰 ==========
        private static void CreateMapDecorations()
        {
            var decoObj = new GameObject("Decorations");

            // 树木（绿色圆圈）
            CreateTree(decoObj, new Vector2(-5f, 7f));
            CreateTree(decoObj, new Vector2(-3f, 8f));
            CreateTree(decoObj, new Vector2(5f, 7f));
            CreateTree(decoObj, new Vector2(12f, 2f));
            CreateTree(decoObj, new Vector2(-12f, -3f));

            // 水井
            CreateWell(decoObj, new Vector2(2f, -2f));

            // 指示牌
            CreateSign(decoObj, new Vector2(-2f, 0f), "烟柳镇中心");

            Debug.Log("[Demo] 装饰物创建完成");
        }

        private static void CreateTree(GameObject parent, Vector2 pos)
        {
            var tree = new GameObject("Tree");
            tree.transform.SetParent(parent.transform);
            tree.transform.position = pos;
            tree.layer = LayerMask.NameToLayer("Environment");

            var sr = tree.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Environment";
            sr.sortingOrder = 5;

            // 临时绿色圆形
            var tex = new Texture2D(32, 32);
            for (int x = 0; x < 32; x++)
                for (int y = 0; y < 32; y++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(16, 16));
                    if (dist < 14)
                    {
                        float shade = Random.Range(0.7f, 1f);
                        tex.SetPixel(x, y, new Color(0.2f * shade, 0.6f * shade, 0.15f * shade));
                    }
                    else
                        tex.SetPixel(x, y, Color.clear);
                }
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.3f), 16);

            var col = tree.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.6f, 0.4f);
            col.offset = new Vector2(0, -0.5f);
        }

        private static void CreateWell(GameObject parent, Vector2 pos)
        {
            var well = new GameObject("Well");
            well.transform.SetParent(parent.transform);
            well.transform.position = pos;

            var sr = well.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Environment";
            sr.color = new Color(0.5f, 0.5f, 0.5f);

            var tex = new Texture2D(16, 16);
            Color32[] px = new Color32[256];
            for (int i = 0; i < 256; i++) px[i] = new Color32(100, 100, 100, 255);
            tex.SetPixels32(px);
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
        }

        private static void CreateSign(GameObject parent, Vector2 pos, string text)
        {
            var sign = new GameObject("Sign");
            sign.transform.SetParent(parent.transform);
            sign.transform.position = pos;

            var sr = sign.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Environment";
            sr.color = new Color(0.8f, 0.6f, 0.3f);

            var tex = new Texture2D(16, 16);
            Color32[] px = new Color32[256];
            for (int i = 0; i < 256; i++) px[i] = new Color32(200, 150, 80, 255);
            tex.SetPixels32(px);
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
        }

        // ========== 玩家 ==========
        private static void CreatePlayer()
        {
            var player = new GameObject("Player");
            player.tag = "Player";
            player.layer = LayerMask.NameToLayer("Player");

            // 精灵（临时蓝色方块）
            var sr = player.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Character";
            sr.sortingOrder = 0;
            sr.color = new Color(0.3f, 0.5f, 0.9f);

            var tex = new Texture2D(48, 48);
            for (int x = 0; x < 48; x++)
                for (int y = 0; y < 48; y++)
                {
                    // 简易像素人物：头+身体+腿
                    bool isHead = y > 30 && y < 44 && x > 14 && x < 34;
                    bool isBody = y > 14 && y <= 30 && x > 10 && x < 38;
                    bool isLegs = y > 2 && y <= 14 && x > 14 && x < 22;
                    bool isLegs2 = y > 2 && y <= 14 && x > 26 && x < 34;

                    if (isHead)
                        tex.SetPixel(x, y, new Color(0.95f, 0.85f, 0.75f)); // 肤色
                    else if (isBody)
                        tex.SetPixel(x, y, new Color(0.2f, 0.3f, 0.6f));   // 蓝衣
                    else if (isLegs || isLegs2)
                        tex.SetPixel(x, y, new Color(0.3f, 0.25f, 0.2f));  // 裤子
                    else
                        tex.SetPixel(x, y, Color.clear);
                }
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 48, 48), new Vector2(0.5f, 0f / 48f), 16);

            // 物理
            var rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var col = player.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 1.2f);
            col.offset = new Vector2(0f, 0.6f);

            player.AddComponent<Animator>();

            // 组件
            player.AddComponent<PlayerController>();
            var stats = player.AddComponent<CharacterStats>();
            stats.characterName = "凌霜";
            stats.maxHp = 100; stats.currentHp = 100;
            stats.maxMp = 50; stats.currentMp = 50;
            stats.attack = 15;
            stats.defense = 5;
            stats.agility = 10;
            player.AddComponent<PlayerCombat>();
            player.AddComponent<CharacterAudio>();

            // 武学系统
            var martial = player.AddComponent<MartialArtsSystem>();

            // 升级系统
            player.AddComponent<LevelSystem>();

            player.transform.position = new Vector3(0, 0, 0);

            // 学会初始招式
            Debug.Log("[Demo] 玩家创建完成（含武学+升级系统）");
        }

        // ========== NPC ==========
        private static void CreateNPCs()
        {
            // 客栈掌柜
            CreateNPC("掌柜老赵", new Vector2(-6f, 2f), new Color(0.8f, 0.6f, 0.3f),
                new string[] {
                    "客官您好！欢迎来到烟柳客栈。",
                    "最近北山上的山贼闹得厉害，商路都断了。",
                    "你要是身手好，不如去帮镇上除个害？",
                    "凌霜少侠，我看好你！"
                });

            // 苏婉清（药铺）
            CreateNPC("苏婉清", new Vector2(-6f, -7f), new Color(0.6f, 0.3f, 0.5f),
                new string[] {
                    "我是柳家药铺的苏婉清。",
                    "我父亲留下的这枚玉佩碎片……上面刻着奇怪的铭文。",
                    "你能帮我去找镇东的陈先生看看吗？",
                    "这可能是渊朝皇室的东西……"
                });

            // 钓鱼老翁
            CreateNPC("钓鱼翁", new Vector2(10f, -5f), new Color(0.5f, 0.5f, 0.3f),
                new string[] {
                    "嗬……今天的鱼不太上钩啊。",
                    "年轻人，你也来钓鱼？",
                    "我年轻的时候啊，江湖上的人都叫我'疾风剑'。",
                    "不过那都是过去的事了……"
                });

            Debug.Log("[Demo] NPC创建完成");
        }

        private static void CreateNPC(string name, Vector2 pos, Color color, string[] dialogue)
        {
            var npc = new GameObject($"NPC_{name}");
            npc.transform.position = pos;
            npc.tag = "NPC";
            npc.layer = LayerMask.NameToLayer("Interactable");

            var sr = npc.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Character";
            sr.color = color;

            // 临时NPC精灵
            var tex = new Texture2D(32, 32);
            for (int x = 0; x < 32; x++)
                for (int y = 0; y < 32; y++)
                {
                    bool isHead = y > 20 && y < 30 && x > 10 && x < 22;
                    bool isBody = y > 6 && y <= 20 && x > 6 && x < 26;
                    if (isHead)
                        tex.SetPixel(x, y, new Color(0.95f, 0.85f, 0.75f));
                    else if (isBody)
                        tex.SetPixel(x, y, color);
                    else
                        tex.SetPixel(x, y, Color.clear);
                }
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0f / 32f), 16);

            var col = npc.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1f, 1.5f);
            col.offset = new Vector2(0f, 0.5f);

            var npcBase = npc.AddComponent<NPCBase>();
            npcBase.npcName = name;
            npcBase.defaultDialogue = dialogue;
        }

        // ========== 敌人 ==========
        private static void CreateEnemies()
        {
            // 第一组：镇外山贼
            CreateEnemy("山贼甲", new Vector2(12f, 6f), 25, 6, new Color(0.6f, 0.2f, 0.2f));
            CreateEnemy("山贼乙", new Vector2(13f, 8f), 25, 6, new Color(0.6f, 0.2f, 0.2f));

            // 第二组：路匪
            CreateEnemy("路匪", new Vector2(-12f, 6f), 35, 8, new Color(0.4f, 0.2f, 0.4f));

            Debug.Log("[Demo] 敌人创建完成");
        }

        private static void CreateEnemy(string name, Vector2 pos, int hp, int atk, Color color)
        {
            var enemy = new GameObject($"Enemy_{name}");
            enemy.transform.position = pos;
            enemy.tag = "Enemy";
            enemy.layer = LayerMask.NameToLayer("Enemy");

            var sr = enemy.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Character";
            sr.color = color;

            var tex = new Texture2D(32, 32);
            for (int x = 0; x < 32; x++)
                for (int y = 0; y < 32; y++)
                {
                    bool isHead = y > 20 && y < 30 && x > 10 && x < 22;
                    bool isBody = y > 6 && y <= 20 && x > 6 && x < 26;
                    if (isHead)
                        tex.SetPixel(x, y, new Color(0.95f, 0.85f, 0.75f));
                    else if (isBody)
                        tex.SetPixel(x, y, color);
                    else
                        tex.SetPixel(x, y, Color.clear);
                }
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0f / 32f), 16);

            var rb = enemy.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            var col = enemy.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 1.2f);
            col.offset = new Vector2(0f, 0.5f);

            var stats = enemy.AddComponent<CharacterStats>();
            stats.characterName = name;
            stats.maxHp = hp; stats.currentHp = hp;
            stats.attack = atk;

            enemy.AddComponent<EnemyAI>();

            // 掉落表
            var loot = enemy.AddComponent<LootTable>();
            loot.minGold = 3;
            loot.maxGold = 12;
            loot.expReward = 10 + hp / 5;
            loot.lootItems = name.Contains("路匪") ? EnemyLootPresets.BanditLoot : EnemyLootPresets.WolfLoot;
        }

        // ========== 可破坏物体 ==========
        private static void CreateDestructibles()
        {
            CreateCrate(new Vector2(3f, 4f), new string[] { "herb_medicinal" });
            CreateCrate(new Vector2(5f, 3f), new string[] { "food_mantou", "herb_spirit" });
            CreateCrate(new Vector2(-3f, -4f), new string[] { });

            Debug.Log("[Demo] 可破坏物体创建完成");
        }

        private static void CreateCrate(Vector2 pos, string[] drops)
        {
            var crate = new GameObject("Crate");
            crate.transform.position = pos;
            crate.layer = LayerMask.NameToLayer("Environment");

            var sr = crate.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Environment";
            sr.sortingOrder = 2;
            sr.color = new Color(0.7f, 0.5f, 0.2f);

            var tex = new Texture2D(16, 16);
            Color32 brown = new Color32(180, 130, 60, 255);
            Color32 darkBrown = new Color32(120, 80, 30, 255);
            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                {
                    bool border = x == 0 || x == 15 || y == 0 || y == 15 || x == 7 || y == 7;
                    tex.SetPixel(x, y, border ? darkBrown : brown);
                }
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);

            var col = crate.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.9f, 0.9f);

            var dest = crate.AddComponent<Destructible>();
            dest.objectName = "木箱";
            dest.hp = 2;
            dest.dropItemIds = drops;
            dest.dropChance = 0.7f;
            dest.goldDropRange = new Vector2Int(1, 8);
        }

        // ========== 事件触发器 ==========
        private static void CreateEventTriggers()
        {
            // 北山山贼BOSS战触发器
            var triggerObj = new GameObject("Event_BossFight");
            triggerObj.transform.position = new Vector3(13f, 10f, 0);

            var col = triggerObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(3f, 2f);

            var evt = triggerObj.AddComponent<EventTrigger>();
            evt.triggerType = EventTrigger.TriggerType.Combat;
            evt.triggerOnce = true;
            evt.enemyWaves = new EventTrigger.WaveData[]
            {
                new EventTrigger.WaveData
                {
                    enemyName = "山贼",
                    count = 3,
                    enemyHp = 20,
                    enemyAtk = 5,
                    spawnRadius = 3f
                },
                new EventTrigger.WaveData
                {
                    enemyName = "山贼头目",
                    count = 1,
                    enemyHp = 60,
                    enemyAtk = 12,
                    spawnRadius = 2f
                }
            };

            Debug.Log("[Demo] 事件触发器创建完成");
        }

        // ========== 区域出口 ==========
        private static void CreateAreaExits()
        {
            // 北出口（通往北山）
            CreateExit("Exit_North", new Vector3(0, 10f, 0), new Vector2(4f, 1f), "北山山道");

            // 南出口
            CreateExit("Exit_South", new Vector3(0, -10f, 0), new Vector2(4f, 1f), "官道");

            // 区域入口提示（进入烟柳镇时显示地名）
            var areaEntry = new GameObject("AreaEntry_YanLiuTown");
            areaEntry.transform.position = new Vector3(0, -8f, 0);
            var entryCol = areaEntry.AddComponent<BoxCollider2D>();
            entryCol.isTrigger = true;
            entryCol.size = new Vector2(20f, 2f);
            var areaTrigger = areaEntry.AddComponent<AreaTrigger>();
            areaTrigger.areaName = "烟柳镇";
            areaTrigger.areaSubtitle = "柳暗花明又一村";

            Debug.Log("[Demo] 区域出口创建完成");
        }

        private static void CreateExit(string name, Vector3 pos, Vector2 size, string targetArea)
        {
            var exitObj = new GameObject(name);
            exitObj.transform.position = pos;

            var col = exitObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = size;

            var trigger = exitObj.AddComponent<EventTrigger>();
            trigger.triggerType = EventTrigger.TriggerType.Dialogue;
            trigger.triggerOnce = false;
            trigger.speakerName = "系统";
            trigger.dialogueLines = new string[]
            {
                $"前方是「{targetArea}」。",
                "（Demo版本，前方暂未开放）"
            };
        }

        // ========== HUD ==========
        private static void CreateHUD()
        {
            var canvasObj = new GameObject("[HUD Canvas]");
            canvasObj.AddComponent<Canvas>();
            canvasObj.AddComponent<CanvasScaler>();

            // HUD v2 自动构建所有UI
            canvasObj.AddComponent<HUD>();

            Debug.Log("[Demo] HUD创建完成");
        }

        // ========== 对话UI ==========
        private static void CreateDialogueUI()
        {
            var dlgCanvas = new GameObject("[Dialogue Canvas]");
            dlgCanvas.AddComponent<Canvas>();
            dlgCanvas.AddComponent<CanvasScaler>();

            // DialogueUI v2 自动构建对话框+选择面板
            dlgCanvas.AddComponent<DialogueUI>();

            Debug.Log("[Demo] 对话UI创建完成");
        }

        // ========== 暂停菜单 ==========
        private static void CreatePauseMenu()
        {
            var pauseCanvas = new GameObject("[Pause Canvas]");
            var canvas = pauseCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;
            pauseCanvas.AddComponent<CanvasScaler>();
            pauseCanvas.AddComponent<PauseMenu>();

            Debug.Log("[Demo] 暂停菜单创建完成");
        }

        // ========== Canvas设置 ==========
        private static void CreateCanvasSettings()
        {
            // 确保EventSystem存在
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esObj = new GameObject("[EventSystem]");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }
    }
}
