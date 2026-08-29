using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using YuanHaiLu.Character;
using YuanHaiLu.Map;
using YuanHaiLu.GameSystem;
using YuanHaiLu.Art;
using YuanHaiLu.Core;

namespace YuanHaiLu.Editor
{
    /// <summary>
    /// Demo场景一键生成器
    /// 菜单: Tools/渊海录/生成Demo场景
    /// 自动创建完整的可运行演示场景
    /// </summary>
    public static class DemoSceneGenerator
    {
        private const float MvpWidth = 30f;
        private const float MvpHeight = 16.875f;

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
            CreateMvpVisualStage();
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
                new Vector3(MvpWidth * 0.5f, MvpHeight * 0.5f, -10f),
                8.4375f,
                new Color(0.18f, 0.22f, 0.16f)); // 暗绿底色
            PlaySceneAssembler.ConfigureCameraBounds(
                _uiCamera, Vector2.zero, new Vector2(MvpWidth, MvpHeight));
            // [ScreenTransition] 画布先于相机创建，此处补绑同一逻辑展示面。
            PlaySceneAssembler.BindScreenTransitionToCamera(_uiCamera);
        }

        private static void CreateMvpVisualStage()
        {
            // docs/18 §6.B：密集小模块按 town.json 装配，替代三张 480×270 整屏层。
            MvpSceneModuleAssembler.Assemble(GameObject.Find("yanliu"), "town");
            PlaySceneAssembler.ConfigureDenseActorSprite("mvp_bandit_a");
            PlaySceneAssembler.ConfigureDenseActorSprite("mvp_bandit_b");
            PlaySceneAssembler.ConfigureDenseActorSprite("mvp_lost_pouch");
        }

        private static void CreateFormalColliders()
        {
            // MVP 背景是单屏构图，旧 formal collider 已随旧 Tile 渲染一起禁用。
            // 这里的边界、客栈建筑和水面阻挡与新画面保持一致。
            var root = new GameObject("FormalCollision");
            root.layer = LayerMask.NameToLayer("Environment");
            CreateInvisibleWall(root, "Boundary_West", new Vector2(-0.5f, MvpHeight * 0.5f), new Vector2(1f, MvpHeight + 1f));
            CreateInvisibleWall(root, "Boundary_East", new Vector2(MvpWidth + 0.5f, MvpHeight * 0.5f), new Vector2(1f, MvpHeight + 1f));
            CreateInvisibleWall(root, "Boundary_North", new Vector2(MvpWidth * 0.5f, MvpHeight + 0.5f), new Vector2(MvpWidth + 1f, 1f));
            CreateInvisibleWall(root, "Boundary_South", new Vector2(MvpWidth * 0.5f, -0.5f), new Vector2(MvpWidth + 1f, 1f));
            // 客栈正面必须给门保留真实缺口。此前单一 9 单位宽碰撞盒一直压到
            // 门前，出生点虽在建筑外却无法走到 AreaTrigger，MVP 首步被软锁。
            // 两段立面保持画面中的大体积建筑，同时为 x=7.5 的门洞留出 2.4 单位通道。
            CreateInvisibleWall(root, "Inn_Facade_West", new Vector2(3.45f, 13.4f), new Vector2(5.7f, 5.8f));
            CreateInvisibleWall(root, "Inn_Facade_East", new Vector2(9.15f, 13.4f), new Vector2(1.7f, 5.8f));
            CreateInvisibleWall(root, "Upper_River", new Vector2(22.7f, 13.3f), new Vector2(14.5f, 7.2f));
        }

        private static void CreateInvisibleWall(GameObject parent, string name, Vector2 position, Vector2 size)
        {
            var wall = new GameObject(name);
            wall.transform.SetParent(parent.transform);
            wall.transform.position = position;
            wall.layer = LayerMask.NameToLayer("Environment");
            wall.AddComponent<BoxCollider2D>().size = size;
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
            // MVP 镇场景只承载客栈入口和河岸战斗；掌柜在室内，其他剧情 NPC
            // 属冻结内容。去掉视口外的旧正式角色，避免它们与新单主角画面混搭。
            Debug.Log("[Demo] MVP 镇不创建额外 NPC");
        }

        // ========== 敌人 ==========
        private static void CreateEnemies()
        {
            // 复审 P1：Demo 只保留 MVP_01 的两名河岸水匪（docs/15“两个敌人、
            // 一个任务闭环”）。旧的山贼/路匪巡逻组与 BOSS 战事件已随收缩移除，
            // 不再引用其他冻结角色资产。
            CreateEnemy("河岸水匪甲", "mvp_bandit_a", new Vector2(14f, 3.2f), 22, 5,
                questTargetId: "river_bandit");
            CreateEnemy("河岸水匪乙", "mvp_bandit_b", new Vector2(17f, 2.6f), 22, 5,
                questTargetId: "river_bandit");

            Debug.Log("[Demo] 敌人创建完成");
        }

        private static void CreateEnemy(string name, string spriteId, Vector2 pos, int hp, int atk,
            string questTargetId = null)
        {
            var enemy = new GameObject($"Enemy_{name}");
            enemy.transform.position = pos;
            enemy.tag = "Enemy";
            enemy.layer = LayerMask.NameToLayer("Enemy");

            var sr = enemy.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Character";
            MvpStaticVisual.ApplyTo(enemy, spriteId);

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
            // 木箱属于旧正式场景填充，不在本轮 10–15 分钟 MVP 的任务闭环内。
            Debug.Log("[Demo] MVP 不创建额外可破坏物");
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
            MvpStaticVisual.ApplyTo(pouch, "mvp_lost_pouch");
            // MvpStaticVisual assigns normal actors to Character.  The pouch is an
            // environment-side pickup and must remain below the player instead.
            pouchSr.sortingLayerName = GameConfig.SORTING_ENVIRONMENT;
            pouchSr.sortingOrder = 5;
            // 密集荷包精灵为 16×16，1× 缩放即等于旧 32×32 半缩放的占地。
            pouch.transform.localScale = Vector3.one;
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
            innDoorTrigger.spawnPositionInTarget = new Vector2(15f, 2.5f);

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
