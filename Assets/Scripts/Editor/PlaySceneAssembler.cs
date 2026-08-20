using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using YuanHaiLu.Art;
using YuanHaiLu.Core;
using YuanHaiLu.Character;
using YuanHaiLu.Effects;
using YuanHaiLu.GameSystem;
using YuanHaiLu.UI;

namespace YuanHaiLu.Editor
{
    /// <summary>
    /// 玩法场景共享装配器（复审 P1：消除 Demo/Inn 两个生成器各自手工
    /// 装配全局管理器、玩家与 UI 的重复，防止后续场景行为漂移）。
    /// 两个生成器只保留各自差异化的内容（摄像机参数、出生点、NPC、敌人、出口）。
    /// </summary>
    public static class PlaySceneAssembler
    {
        // ========== 全局管理器（两套玩法场景完全一致） ==========
        public static void CreateGlobalManagers(string logPrefix)
        {
            // 复审 S1：Save/Inventory/Quest/GameTime/Dialogue 五个核心管理器
            // 统一交给 GlobalSystemsBootstrapper.EnsureRequiredSystems 创建，
            // 与运行时补全规则共用同一入口，不再手工装配。
            var gmObj = new GameObject("[GameManager]");
            var gameManager = gmObj.AddComponent<GameManager>();
            GlobalSystemsBootstrapper.EnsureRequiredSystems(gameManager);

            // 以下场景级系统不在 EnsureRequiredSystems 契约内，仍由场景装配：
            var audioObj = new GameObject("[AudioManager]");
            audioObj.AddComponent<AudioManager>();

            var fxObj = new GameObject("[EffectsManager]");
            fxObj.AddComponent<EffectsManager>();

            var transObj = new GameObject("[ScreenTransition]");
            transObj.AddComponent<Canvas>();
            transObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            transObj.AddComponent<ScreenTransition>();

            ItemDatabase.EnsureInitialized();
            MartialSkillDatabase.EnsureInitialized();

            var deathObj = new GameObject("[PlayerDeathHandler]");
            deathObj.AddComponent<PlayerDeathHandler>();

            Debug.Log($"[{logPrefix}] 全局管理器创建完成");
        }

        // ========== 摄像机（参数由各场景决定） ==========
        public static Camera CreateMainCamera(string logPrefix, Vector3 position,
            float orthographicSize, Color backgroundColor)
        {
            var camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            camObj.transform.position = position;

            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = orthographicSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = backgroundColor;

            camObj.AddComponent<AudioListener>();
            camObj.AddComponent<PixelPerfectCamera>();
            camObj.AddComponent<CameraFollow>();

            Debug.Log($"[{logPrefix}] 摄像机创建完成");
            return cam;
        }

        // ========== 玩家（固定男主 + 全套战斗/交互组件，docs/15） ==========
        public static GameObject CreatePlayer(string logPrefix, Vector3 spawnPosition)
        {
            var player = new GameObject("Player");
            player.tag = "Player";
            player.layer = LayerMask.NameToLayer("Player");

            var sr = player.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Character";
            sr.sortingOrder = 0;

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
            player.AddComponent<MartialArtsSystem>();
            player.AddComponent<LevelSystem>();

            PlayerInteraction.EnsureOn(player);

            player.transform.position = spawnPosition;

            Debug.Log($"[{logPrefix}] 玩家创建完成（含武学+升级系统）");
            return player;
        }

        // ========== UI（HUD / 对话 / 暂停 / EventSystem） ==========
        public static void CreateHudCanvas()
        {
            var canvasObj = new GameObject("[HUD Canvas]");
            canvasObj.AddComponent<Canvas>();
            canvasObj.AddComponent<CanvasScaler>();
            // HUD v2 自动构建所有UI
            canvasObj.AddComponent<HUD>();
        }

        public static void CreateDialogueCanvas()
        {
            var dlgCanvas = new GameObject("[Dialogue Canvas]");
            dlgCanvas.AddComponent<Canvas>();
            dlgCanvas.AddComponent<CanvasScaler>();
            // DialogueUI v2 自动构建对话框+选择面板
            dlgCanvas.AddComponent<DialogueUI>();
        }

        public static void CreatePauseCanvas()
        {
            var pauseCanvas = new GameObject("[Pause Canvas]");
            var canvas = pauseCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;
            pauseCanvas.AddComponent<CanvasScaler>();
            pauseCanvas.AddComponent<PauseMenu>();
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            var esObj = new GameObject("[EventSystem]");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }
    }
}
