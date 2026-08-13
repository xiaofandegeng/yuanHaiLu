using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using YuanHaiLu.Core;
using YuanHaiLu.Character;
using YuanHaiLu.Dialogue;
using YuanHaiLu.Map;
using YuanHaiLu.GameSystem;
using YuanHaiLu.Effects;
using YuanHaiLu.UI;
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
            directorObj.AddComponent<SceneDirector>()
                .ConfigureForEditor(new Vector2(20.5f, 7.5f));

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
            camObj.transform.position = new Vector3(20f, 12f, -10f);

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

        // ========== 玩家 ==========
        private static void CreatePlayer()
        {
            var player = new GameObject("Player");
            player.tag = "Player";
            player.layer = LayerMask.NameToLayer("Player");

            var sr = player.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Character";
            sr.sortingOrder = 0;

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
            CharacterVisual.ApplyTo(player, PlayerAppearance.Default.ArtId);
            player.AddComponent<PlayerAppearanceBinder>();

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

            // 交互系统（K 键与 NPC/木箱/传送点交互）
            PlayerInteraction.EnsureOn(player);

            player.transform.position = new Vector3(20.5f, 7.5f, 0);

            // 学会初始招式
            Debug.Log("[Demo] 玩家创建完成（含武学+升级系统）");
        }

        // ========== NPC ==========
        private static void CreateNPCs()
        {
            // 客栈掌柜
            CreateNPC("掌柜老赵", "innkeeper_zhao", new Vector2(6.5f, 9f),
                new string[] {
                    "客官您好！欢迎来到烟柳客栈。",
                    "最近北山上的山贼闹得厉害，商路都断了。",
                    "你要是身手好，不如去帮镇上除个害？",
                    "凌霜少侠，我看好你！"
                });

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
            // 第一组：镇外山贼
            CreateEnemy("山贼甲", "yanliu_river_bandit", new Vector2(33f, 18f), 25, 6);
            CreateEnemy("叛军水兵", "yanliu_rebel_marine", new Vector2(35f, 20f), 25, 6);

            // 第二组：路匪
            CreateEnemy("水匪", "yanliu_water_bandit", new Vector2(6f, 19f), 35, 8);

            Debug.Log("[Demo] 敌人创建完成");
        }

        private static void CreateEnemy(string name, string artId, Vector2 pos, int hp, int atk)
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
            CreateCrate(new Vector2(16f, 15f), new string[] { "herb_medicinal" });
            CreateCrate(new Vector2(18f, 16f), new string[] { "food_mantou", "herb_spirit" });
            CreateCrate(new Vector2(24f, 14f), new string[] { });

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
            // 北山山贼BOSS战触发器
            var triggerObj = new GameObject("Event_BossFight");
            triggerObj.transform.position = new Vector3(34f, 21f, 0);

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
                    artId = "yanliu_river_bandit",
                    count = 3,
                    enemyHp = 20,
                    enemyAtk = 5,
                    spawnRadius = 3f
                },
                new EventTrigger.WaveData
                {
                    enemyName = "山贼头目",
                    artId = "yanliu_rebel_gang_lord",
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
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esObj = new GameObject("[EventSystem]");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }
    }
}
