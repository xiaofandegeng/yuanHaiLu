using UnityEngine;
using YuanHaiLu.Core;
using YuanHaiLu.GameSystem;
using YuanHaiLu.Character;
using YuanHaiLu.Dialogue;
using YuanHaiLu.UI;

namespace YuanHaiLu.Core
{
    /// <summary>
    /// 场景引导器 — 每个游戏场景的入口，负责初始化场景内的系统
    /// 挂载到场景根物体上
    /// </summary>
    public class SceneBootstrapper : MonoBehaviour
    {
        [Header("=== 场景配置 ===")]
        [SerializeField] private string sceneName = "烟柳镇";
        [SerializeField] private string sceneBGM = "bgm_yanliu_town";

        [Header("=== 必要引用 ===")]
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField] private Vector2 cameraMinBounds = new Vector2(-20, -15);
        [SerializeField] private Vector2 cameraMaxBounds = new Vector2(20, 15);

        [Header("=== 调试 ===")]
        [SerializeField] private bool debugMode = true;
        [SerializeField] private bool spawnTestNPCs = true;
        [SerializeField] private bool spawnTestEnemies = true;

        private void Start()
        {
            Debug.Log($"[SceneBootstrapper] 场景初始化: {sceneName}");

            InitializeSystems();
            SetupPlayer();
            SetupCamera();
            SetupAudio();

            if (debugMode)
            {
                SpawnDebugContent();
            }
        }

        /// <summary>
        /// 确保全局系统存在
        /// </summary>
        private void InitializeSystems()
        {
            // GameManager（全局单例）
            GameManager gameManager = GameManager.Instance;
            if (gameManager == null)
                gameManager = new GameObject("GameManager").AddComponent<GameManager>();

            GlobalSystemsBootstrapper.EnsureRequiredSystems(gameManager);

            // AudioManager
            if (AudioManager.Instance == null)
            {
                new GameObject("AudioManager").AddComponent<AudioManager>();
            }

            Debug.Log("[SceneBootstrapper] 全局系统初始化完成");
        }

        /// <summary>
        /// 初始化玩家
        /// </summary>
        private void SetupPlayer()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player == null)
            {
                // 场景中没有玩家，创建一个基础玩家对象
                player = new GameObject("Player");
                player.tag = "Player";

                // 必要组件
                var sr = player.AddComponent<SpriteRenderer>();
                sr.sortingLayerName = GameConfig.SORTING_CHARACTER;
                sr.sortingOrder = 0;

                var rb = player.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                rb.freezeRotation = true;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

                var col = player.AddComponent<BoxCollider2D>();
                col.size = new Vector2(0.8f, 1.2f);
                col.offset = new Vector2(0f, 0.6f);

                player.AddComponent<Animator>();
                player.AddComponent<PlayerController>();

                var stats = player.AddComponent<CharacterStats>();
                stats.characterName = GameManager.Instance.playerName;
                stats.maxHp = 100;
                stats.maxMp = 50;

                player.AddComponent<PlayerCombat>();
                player.AddComponent<CharacterAudio>();

                Debug.Log("[SceneBootstrapper] 创建玩家对象完成");
            }

            // 设置生成点
            if (playerSpawnPoint != null)
            {
                player.transform.position = playerSpawnPoint.position;
            }

            // 确保在正确的层级
            player.layer = LayerMask.NameToLayer("Player");
            PlayerInteraction.EnsureOn(player);
            if (player.GetComponent<MartialArtsSystem>() == null)
                player.AddComponent<MartialArtsSystem>();
        }

        /// <summary>
        /// 设置摄像机
        /// </summary>
        private void SetupCamera()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                var camObj = new GameObject("Main Camera");
                camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
                camObj.tag = "MainCamera";
                mainCam = camObj.GetComponent<Camera>();
            }

            // 像素完美摄像机
            var pixelCam = mainCam.GetComponent<PixelPerfectCamera>();
            if (pixelCam == null)
            {
                pixelCam = mainCam.gameObject.AddComponent<PixelPerfectCamera>();
            }

            // 摄像机跟随
            var camFollow = mainCam.GetComponent<CameraFollow>();
            if (camFollow == null)
            {
                camFollow = mainCam.gameObject.AddComponent<CameraFollow>();
            }

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                camFollow.SetTarget(player.transform);
            }

            // 设置边界
            camFollow.enabled = true; // useBounds 会在 Inspector 中配置

            Debug.Log("[SceneBootstrapper] 摄像机设置完成");
        }

        /// <summary>
        /// 播放场景BGM
        /// </summary>
        private void SetupAudio()
        {
            if (!string.IsNullOrEmpty(sceneBGM) && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBGM(sceneBGM);
            }
        }

        /// <summary>
        /// 调试模式下生成测试内容
        /// </summary>
        private void SpawnDebugContent()
        {
            if (spawnTestNPCs)
            {
                SpawnTestNPC("测试村民", new Vector2(3f, 0f),
                    new string[] {
                        "欢迎来到烟柳镇！",
                        "最近北山上的山贼越来越猖狂了……",
                        "你要小心啊，{player}。"
                    });
                SpawnTestNPC("测试铁匠", new Vector2(-4f, 2f),
                    new string[] {
                        "我是镇上的铁匠老王。",
                        "你要是想打造兵器，尽管来找我！"
                    });
            }

            if (spawnTestEnemies)
            {
                SpawnTestEnemy("测试山贼", new Vector2(8f, 5f), 5, 3);
                SpawnTestEnemy("测试山贼", new Vector2(10f, 3f), 5, 3);
            }

            Debug.Log("[SceneBootstrapper] 调试内容生成完成");
        }

        private void SpawnTestNPC(string name, Vector2 pos, string[] dialogue)
        {
            GameObject npc = new GameObject($"NPC_{name}");
            npc.transform.position = pos;
            npc.layer = LayerMask.NameToLayer("NPC");

            var sr = npc.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = GameConfig.SORTING_CHARACTER;

            var col = npc.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.8f, 1.2f);

            var npcBase = npc.AddComponent<NPCBase>();
            npcBase.npcName = name;
            npcBase.defaultDialogue = dialogue;
        }

        private void SpawnTestEnemy(string name, Vector2 pos, int hp, int atk)
        {
            GameObject enemy = new GameObject($"Enemy_{name}");
            enemy.transform.position = pos;
            enemy.layer = LayerMask.NameToLayer("Enemy");

            var sr = enemy.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = GameConfig.SORTING_CHARACTER;

            var rb = enemy.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            var col = enemy.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 1.2f);

            var stats = enemy.AddComponent<CharacterStats>();
            stats.characterName = name;
            stats.maxHp = hp;
            stats.currentHp = hp;
            stats.attack = atk;

            enemy.AddComponent<EnemyAI>();
        }
    }
}
