using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using YuanHaiLu.Character;
using YuanHaiLu.Map;
using YuanHaiLu.GameSystem;
using YuanHaiLu.Art;

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
            GenerateInternal(true);
        }

        public static void GenerateFromCommandLine()
        {
            GenerateInternal(false);
            SetupBuildSettings.Setup();
        }

        private static void GenerateInternal(bool showDialog)
        {
            Debug.Log("========================================");
            Debug.Log("  渊海录 Demo 场景生成器");
            Debug.Log("========================================");

            // 以正式烟柳镇 Tilemap 场景为底稿，不再重画占位方块。
            RegionSceneBuilder.Build("yanliu");
            string scenePath = "Assets/Scenes/Demo_YanLiuTown.unity";
            var formalScene = EditorSceneManager.OpenScene(
                RegionSceneBuilder.ScenePath("yanliu"),
                OpenSceneMode.Single);
            EditorSceneManager.SaveScene(formalScene, scenePath, true);
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // 按顺序构建
            CreateGlobalManagers();
            CreateMainCamera();
            CreateFormalColliders();
            CreatePlayer();
            CreateNPCs();
            CreateEnemies();
            CreateDestructibles();
            CreateEventTriggers();
            CreateAreaExits();
            CreateMvpQuestObjects();
            CreateHUD();
            CreateDialogueUI();
            CreatePauseMenu();
            CreateCanvasSettings();

            // 场景引导脚本
            var directorObj = new GameObject("[SceneDirector]");
            var director = directorObj.AddComponent<SceneDirector>();
            // 复审 P1：出生点改到客栈门外可行走格（旧默认 (0,-5) 在地图外）。
            director.spawnPosition = new Vector2(7.5f, 7.6f);

            // 保存场景
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, scenePath);

            Debug.Log($"========================================");
            Debug.Log($"  Demo场景生成完成！");
            Debug.Log($"  场景路径: {scenePath}");
            Debug.Log($"  按 Play 即可运行！");
            Debug.Log($"========================================");

            if (showDialog)
            {
                EditorUtility.DisplayDialog("Demo场景生成完成",
                    "正式烟柳镇 Demo 场景已生成！\n\n" +
                    "包含正式 Tilemap、地标、主角、NPC、敌人、战斗、交互与 UI。\n" +
                    "按 Play 运行即可体验。",
                    "太好了！");
            }
        }

        // ========== 全局管理器 ==========
        private static void CreateGlobalManagers()
        {
            // 复审 P1：统一走共享装配器，与客栈室内场景保持一致，防止漂移。
            PlaySceneAssembler.CreateGlobalManagers("Demo");
        }

        // ========== 摄像机 ==========
        private static Camera _uiCamera;

        private static void CreateMainCamera()
        {
            _uiCamera = PlaySceneAssembler.CreateMainCamera(
                "Demo",
                new Vector3(20f, 12f, -10f),
                8.4375f,
                new Color(0.18f, 0.22f, 0.16f)); // 暗绿底色
            // [ScreenTransition] 画布先于相机创建，此处补绑同一逻辑展示面。
            PlaySceneAssembler.BindScreenTransitionToCamera(_uiCamera);
        }

        private static void CreateFormalColliders()
        {
            // 建筑、岸线、柳树与边界的阻挡全部由 yanliu.json 声明并在
            // RegionSceneBuilder 生成的 Layout Colliders 中落地；这里只补
            // 场外缓冲，不再覆盖水面（水道可经双桥穿越）。
            var root = new GameObject("FormalCollision");
            root.layer = LayerMask.NameToLayer("Environment");
            CreateInvisibleWall(root, "Boundary_West", new Vector2(-0.5f, 12f), new Vector2(1f, 25f));
            CreateInvisibleWall(root, "Boundary_East", new Vector2(40.5f, 12f), new Vector2(1f, 25f));
            CreateInvisibleWall(root, "Boundary_North", new Vector2(20f, 24.5f), new Vector2(42f, 1f));
        }

        private static void CreateInvisibleWall(GameObject parent, string name, Vector2 position, Vector2 size)
        {
            var wall = new GameObject(name);
            wall.transform.SetParent(parent.transform);
            wall.transform.position = position;
            wall.layer = LayerMask.NameToLayer("Environment");
            wall.AddComponent<BoxCollider2D>().size = size;
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
            // 固定男主 + 全套组件统一走共享装配器（复审 P1）。
            PlaySceneAssembler.CreatePlayer("Demo", new Vector3(20.5f, 7.5f, 0));
        }

        // ========== NPC ==========
        private static void CreateNPCs()
        {
            // 掌柜老赵移入客栈室内场景（InnSceneGenerator 负责），镇上不再摆放。

            // 苏婉清（药铺）
            CreateNPC("苏婉清", "su_wanqing", new Vector2(30.5f, 9f),
                new string[] {
                    "我是柳家药铺的苏婉清。",
                    "我父亲留下的这枚玉佩碎片……上面刻着奇怪的铭文。",
                    "你能帮我去找镇东的陈先生看看吗？",
                    "这可能是渊朝皇室的东西……"
                });

            // 钓鱼老翁
            CreateNPC("钓鱼翁", "fishing_elder", new Vector2(34f, 5.5f),
                new string[] {
                    "嗬……今天的鱼不太上钩啊。",
                    "年轻人，你也来钓鱼？",
                    "我年轻的时候啊，江湖上的人都叫我'疾风剑'。",
                    "不过那都是过去的事了……"
                });

            Debug.Log("[Demo] NPC创建完成");
        }

        private static void CreateNPC(string name, string artId, Vector2 pos, string[] dialogue)
        {
            var npc = new GameObject($"NPC_{name}");
            npc.transform.position = pos;
            npc.tag = "NPC";
            npc.layer = LayerMask.NameToLayer("NPC");

            var sr = npc.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Character";
            CharacterVisual.ApplyTo(npc, artId);

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
            // 复审 P1：Demo 只保留 MVP_01 的两名河岸水匪（docs/15“两个敌人、
            // 一个任务闭环”）。旧的山贼/路匪巡逻组与 BOSS 战事件已随收缩移除，
            // 不再引用其他冻结角色资产。
            CreateEnemy("河岸水匪甲", "yanliu_river_bandit", new Vector2(14f, 3.2f), 22, 5,
                questTargetId: "river_bandit");
            CreateEnemy("河岸水匪乙", "yanliu_marsh_raider", new Vector2(17f, 2.6f), 22, 5,
                questTargetId: "river_bandit");

            Debug.Log("[Demo] 敌人创建完成");
        }

        private static void CreateEnemy(string name, string artId, Vector2 pos, int hp, int atk,
            string questTargetId = null)
        {
            var enemy = new GameObject($"Enemy_{name}");
            enemy.transform.position = pos;
            enemy.tag = "Enemy";
            enemy.layer = LayerMask.NameToLayer("Enemy");

            var sr = enemy.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Character";
            CharacterVisual.ApplyTo(enemy, artId);

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

            // 任务击杀目标（可选）：死亡成功上报后锁定，不重复计数。
            if (!string.IsNullOrEmpty(questTargetId))
            {
                var questTarget = enemy.AddComponent<QuestTarget>();
                questTarget.objectiveType = QuestObjective.ObjectiveType.KillEnemy;
                questTarget.targetId = questTargetId;
                questTarget.amount = 1;
            }

            // 掉落表
            var loot = enemy.AddComponent<LootTable>();
            loot.minGold = 3;
            loot.maxGold = 12;
            loot.expReward = 10 + hp / 5;
            loot.lootItems = name.Contains("水匪") || name.Contains("路匪")
                ? EnemyLootPresets.BanditLoot
                : EnemyLootPresets.WolfLoot;
        }

        // ========== 可破坏物体 ==========
        private static void CreateDestructibles()
        {
            CreateCrate(new Vector2(16f, 15f), new string[] { "herb_medicinal" });
            CreateCrate(new Vector2(18f, 16f), new string[] { "food_mantou", "herb_spirit" });
            CreateCrate(new Vector2(24f, 15f), new string[] { });

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
            var tiles = EnvironmentTileBuilder.LoadTiles("yanliu");
            sr.sprite = tiles["yanliu__decor__0"].sprite;

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
            // 复审 P1：BOSS 战事件（Event_BossFight）超出 docs/15 试玩范围，
            // 且引用冻结 Boss 资产，已移除。Demo 事件由 MVP 任务对象承担。
            Debug.Log("[Demo] 事件触发器创建完成（MVP 收缩后无附加事件）");
        }

        // ========== MVP 河岸失物任务对象（docs/15） ==========
        private static void CreateMvpQuestObjects()
        {
            // 河岸子区域：进入即上报 MVP_01 的 ReachArea 目标（上报先于一次性地名显示）。
            var riverbank = new GameObject("AreaTrigger_Riverbank");
            riverbank.transform.position = new Vector3(12f, 5.2f, 0);
            var riverbankCol = riverbank.AddComponent<BoxCollider2D>();
            riverbankCol.isTrigger = true;
            riverbankCol.size = new Vector2(8f, 1.6f);
            var riverbankTrigger = riverbank.AddComponent<AreaTrigger>();
            riverbankTrigger.areaName = "烟柳河岸";
            riverbankTrigger.areaSubtitle = "水匪出没之地";
            riverbankTrigger.questTargetId = "yanliu_riverbank";
            riverbankTrigger.showOnce = true;

            // 掌柜的荷包：走过即拾取，成功入包后上报 CollectItem。
            var pouch = new GameObject("ItemPickup_LostPouch");
            pouch.transform.position = new Vector3(24f, 3f, 0);
            var pouchSr = pouch.AddComponent<SpriteRenderer>();
            pouchSr.sortingLayerName = "Environment";
            pouchSr.sortingOrder = 5;
            var pouchTiles = EnvironmentTileBuilder.LoadTiles("yanliu");
            pouchSr.sprite = pouchTiles["yanliu__decor__0"].sprite;
            var pouchCol = pouch.AddComponent<BoxCollider2D>();
            pouchCol.isTrigger = true;
            pouchCol.size = new Vector2(0.8f, 0.8f);
            var pickup = pouch.AddComponent<ItemPickup>();
            pickup.itemId = "quest_lost_pouch";
            pickup.amount = 1;
            pickup.bobAmplitude = 0.08f;
            pickup.magnetRange = 1.2f;
            pickup.lifetime = 0f;

            // 客栈大门：进入切换到客栈室内（掌柜老赵与 MVP_01 接取/提交都在室内）。
            var innDoor = new GameObject("AreaTrigger_InnDoor");
            innDoor.transform.position = new Vector3(7.5f, 9.9f, 0);
            var innDoorCol = innDoor.AddComponent<BoxCollider2D>();
            innDoorCol.isTrigger = true;
            innDoorCol.size = new Vector2(1.8f, 1.4f);
            var innDoorTrigger = innDoor.AddComponent<AreaTrigger>();
            innDoorTrigger.areaName = "烟柳客栈";
            innDoorTrigger.areaSubtitle = "掌柜老赵";
            innDoorTrigger.triggersSceneChange = true;
            innDoorTrigger.targetSceneName = "Demo_Inn";
            innDoorTrigger.spawnPositionInTarget = new Vector2(11.5f, 2.5f);

            CreateMvpQuestStageGates(pouch);

            Debug.Log("[Demo] MVP 河岸失物任务对象创建完成");
        }

        // ========== MVP 任务阶段门（复审 P0：防接任务前消耗目标） ==========
        private static void CreateMvpQuestStageGates(GameObject pouch)
        {
            var banditA = GameObject.Find("Enemy_河岸水匪甲");
            var banditB = GameObject.Find("Enemy_河岸水匪乙");
            if (banditA == null || banditB == null || pouch == null)
            {
                Debug.LogError("[Demo] MVP 阶段门缺少受控对象，任务可能软锁！");
                return;
            }

            // 第三步（杀水匪）前：水匪保持失活，无法被提前击杀。
            var killGate = new GameObject("QuestGate_MVP01_KillBandits");
            var kill = killGate.AddComponent<QuestStageGate>();
            kill.questId = "MVP_01";
            kill.objectiveType = QuestObjective.ObjectiveType.KillEnemy;
            kill.targetId = "river_bandit";
            kill.targets = new[] { banditA, banditB };

            // 第四步（拾荷包）前：荷包保持失活，无法被提前拾走。
            var collectGate = new GameObject("QuestGate_MVP01_CollectPouch");
            var collect = collectGate.AddComponent<QuestStageGate>();
            collect.questId = "MVP_01";
            collect.objectiveType = QuestObjective.ObjectiveType.CollectItem;
            collect.targetId = "quest_lost_pouch";
            collect.targets = new[] { pouch };
        }

        // ========== 区域出口 ==========
        private static void CreateAreaExits()
        {
            // 北出口（通往北山）
            CreateExit("Exit_North", new Vector3(20f, 23f, 0), new Vector2(4f, 1f), "北山山道");

            // 南出口
            CreateExit("Exit_South", new Vector3(20f, 4.5f, 0), new Vector2(4f, 1f), "官道");

            // 区域入口提示（进入烟柳镇时显示地名）
            var areaEntry = new GameObject("AreaEntry_YanLiuTown");
            areaEntry.transform.position = new Vector3(20f, 6f, 0);
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
            PlaySceneAssembler.CreateHudCanvas(_uiCamera);
        }

        // ========== 对话UI ==========
        private static void CreateDialogueUI()
        {
            PlaySceneAssembler.CreateDialogueCanvas(_uiCamera);
        }

        // ========== 暂停菜单 ==========
        private static void CreatePauseMenu()
        {
            PlaySceneAssembler.CreatePauseCanvas(_uiCamera);
        }

        // ========== Canvas设置 ==========
        private static void CreateCanvasSettings()
        {
            PlaySceneAssembler.EnsureEventSystem();
        }
    }
}
