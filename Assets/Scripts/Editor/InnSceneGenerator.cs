using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using YuanHaiLu.Art;
using YuanHaiLu.Character;
using YuanHaiLu.Core;
using YuanHaiLu.Map;
using YuanHaiLu.GameSystem;

namespace YuanHaiLu.Editor
{
    /// <summary>
    /// 客栈室内场景生成器（单主角 MVP，docs/15）。
    /// 以正式 inn 室内 Tilemap 为底稿，叠加玩家、掌柜老赵（MVP_01 任务发布者）、
    /// 回烟柳镇的出口与完整 UI。玩法场景保存为 Assets/Scenes/Demo_Inn.unity。
    /// 菜单: Tools/渊海录/生成客栈室内场景
    /// </summary>
    public static class InnSceneGenerator
    {
        private const float MvpWidth = 30f;
        private const float MvpHeight = 16.875f;
        private const string MvpInnBackdrop =
            "Assets/Art/Environment/MVP/mvp_inn_backdrop.png";

        // 玩法内容放在独立 Demo 路径，正式 Interiors/inn.unity 保持纯 Tilemap 基线，
        // 供 EnvironmentArtTests 反复重建（与 Demo_YanLiuTown / Regions/yanliu 同构）。
        private const string ScenePath = "Assets/Scenes/Demo_Inn.unity";

        [MenuItem("Tools/渊海录/生成客栈室内场景")]
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
            Debug.Log("  渊海录 客栈室内场景生成器");
            Debug.Log("========================================");

            RegionSceneBuilder.Build("inn");
            var formalScene = EditorSceneManager.OpenScene(
                RegionSceneBuilder.ScenePath("inn"),
                OpenSceneMode.Single);
            EditorSceneManager.SaveScene(formalScene, ScenePath, true);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            CreateGlobalManagers();
            new GameObject("[MVP Direct Play Fallback]").AddComponent<MvpDirectPlayFallback>();
            CreateMainCamera();
            CreateMvpVisualStage();
            CreatePlayer();
            CreateInnkeeper();
            CreateExitToTown();

            // 保存场景
            EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log($"========================================");
            Debug.Log($"  客栈室内场景生成完成！");
            Debug.Log($"  场景路径: {ScenePath}");
            Debug.Log($"========================================");

            if (showDialog)
            {
                EditorUtility.DisplayDialog("客栈室内场景生成完成",
                    "客栈室内场景已生成！\n\n" +
                    "包含掌柜老赵（河岸失物任务）、回烟柳镇出口与完整 UI。",
                    "太好了！");
            }
        }

        // ========== 全局管理器 ==========
        private static void CreateGlobalManagers()
        {
            // 复审 P1：统一走共享装配器，与烟柳镇 Demo 保持一致，防止漂移。
            PlaySceneAssembler.CreateGlobalManagers("Inn");
        }

        // ========== 摄像机 ==========
        private static Camera _uiCamera;

        private static void CreateMainCamera()
        {
            _uiCamera = PlaySceneAssembler.CreateMainCamera(
                "Inn",
                new Vector3(MvpWidth * 0.5f, MvpHeight * 0.5f, -10f),
                8.4375f,
                new Color(0.14f, 0.11f, 0.09f));
            PlaySceneAssembler.ConfigureCameraBounds(
                _uiCamera, Vector2.zero, new Vector2(MvpWidth, MvpHeight));
            // [ScreenTransition] 画布先于相机创建，此处补绑同一逻辑展示面。
            PlaySceneAssembler.BindScreenTransitionToCamera(_uiCamera);
        }

        private static void CreateMvpVisualStage()
        {
            PlaySceneAssembler.CreateMvpBackdrop(
                GameObject.Find("inn"),
                MvpInnBackdrop,
                new Vector2(MvpWidth * 0.5f, MvpHeight * 0.5f));

            var root = new GameObject("MvpInnCollision");
            root.layer = LayerMask.NameToLayer("Environment");
            CreateWall(root, "Wall_West", new Vector2(-0.5f, MvpHeight * 0.5f), new Vector2(1f, MvpHeight + 1f));
            CreateWall(root, "Wall_East", new Vector2(MvpWidth + 0.5f, MvpHeight * 0.5f), new Vector2(1f, MvpHeight + 1f));
            CreateWall(root, "Wall_North", new Vector2(MvpWidth * 0.5f, MvpHeight + 0.5f), new Vector2(MvpWidth + 1f, 1f));
            CreateWall(root, "Counter", new Vector2(15f, 11.2f), new Vector2(11f, 1.1f));
            CreateWall(root, "Kitchen", new Vector2(3.6f, 12.5f), new Vector2(6.2f, 6.2f));
            CreateWall(root, "Stair", new Vector2(25.3f, 12.6f), new Vector2(4.4f, 5.2f));
            CreateWall(root, "Table_Upper", new Vector2(23f, 8.5f), new Vector2(4.4f, 1.7f));
            CreateWall(root, "Table_Lower", new Vector2(22.5f, 5.1f), new Vector2(4.4f, 1.7f));
        }

        private static void CreateWall(GameObject root, string name, Vector2 position, Vector2 size)
        {
            var wall = new GameObject(name);
            wall.transform.SetParent(root.transform);
            wall.transform.position = position;
            wall.layer = LayerMask.NameToLayer("Environment");
            wall.AddComponent<BoxCollider2D>().size = size;
        }

        // ========== 玩家 ==========
        private static void CreatePlayer()
        {
            // 固定男主 + 全套组件统一走共享装配器（复审 P1）。
            // 出生点：室内南门入口（AreaTrigger 落地时会覆盖到指定入口坐标）。
            PlaySceneAssembler.CreatePlayer("Inn", new Vector3(15f, 2.5f, 0));

            // UI
            PlaySceneAssembler.CreateHudCanvas(_uiCamera);
            PlaySceneAssembler.CreateDialogueCanvas(_uiCamera);
            PlaySceneAssembler.CreatePauseCanvas(_uiCamera);
            PlaySceneAssembler.EnsureEventSystem();

            Debug.Log("[Inn] 玩家与UI创建完成");
        }

        // ========== 掌柜老赵（MVP_01 任务发布者） ==========
        private static void CreateInnkeeper()
        {
            var npc = new GameObject("NPC_掌柜老赵");
            // 柜台前沿的迎客位：视觉仍依托主柜台，但玩家可在 y=9 的通道内
            // 以默认 1.2 范围交互，不能把 MVP_01 的首步放到碰撞体后面。
            npc.transform.position = new Vector2(15f, 10f);
            npc.tag = "NPC";
            npc.layer = LayerMask.NameToLayer("NPC");

            var sr = npc.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Character";
            CharacterVisual.ApplyTo(npc, "innkeeper_zhao");

            var col = npc.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1f, 1.5f);
            col.offset = new Vector2(0f, 0.5f);

            var npcBase = npc.AddComponent<NPCBase>();
            npcBase.npcName = "掌柜老赵";
            npcBase.npcTitle = "烟柳客栈掌柜";
            npcBase.canWander = false;
            npcBase.defaultDialogue = new string[]
            {
                "客官里边请！烟柳客栈，锅盔管够，热水管烫。",
                "有什么事尽管吩咐。"
            };

            var questGiver = npc.AddComponent<QuestGiver>();
            questGiver.questId = "MVP_01";
            questGiver.interactionTargetId = "innkeeper_zhao";
            questGiver.canAcceptQuest = true;
            questGiver.canCompleteQuest = true;
            questGiver.completedDialogue = new string[]
            {
                "少侠又来啦！河岸那边可还太平？",
                "有了你，我这客栈的账银总算睡得安稳了。"
            };

            Debug.Log("[Inn] 掌柜老赵（MVP_01）创建完成");
        }

        // ========== 回烟柳镇出口 ==========
        private static void CreateExitToTown()
        {
            var exit = new GameObject("AreaTrigger_ExitToTown");
            exit.transform.position = new Vector3(15f, 1.8f, 0);
            var col = exit.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(3f, 1.2f);

            var trigger = exit.AddComponent<AreaTrigger>();
            trigger.areaName = "烟柳客栈";
            trigger.areaSubtitle = "掌柜老赵";
            trigger.triggersSceneChange = true;
            trigger.targetSceneName = "Demo_YanLiuTown";
            // 回镇落点必须低于客栈门触发盒下缘 9.2（玩家碰撞盒高 1.2），
            // 否则落地即与门重叠、立刻被传回客栈形成往返软锁。
            trigger.spawnPositionInTarget = new Vector2(7.5f, 7.6f);

            Debug.Log("[Inn] 回镇出口创建完成");
        }
    }
}
