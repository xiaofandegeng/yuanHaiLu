using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
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

        /// <summary>为固定 480×270 试玩镜头配置可玩画面的真实边界。</summary>
        public static void ConfigureCameraBounds(Camera camera, Vector2 minimum, Vector2 maximum)
        {
            if (camera == null) return;
            var follow = camera.GetComponent<CameraFollow>();
            if (follow == null) return;
            follow.ConfigureBounds(minimum, maximum);
        }

        /// <summary>
        /// 用三张同格、持久的原生像素层搭建 MVP 场景。角色处在 Environment 与
        /// Foreground 之间，因此柳枝、门帘和柜台边缘是真实的层级关系，而不是把
        /// 高分辨率概念图直接铺在所有游戏物体下面。
        /// </summary>
        public static void CreateMvpSceneLayers(
            GameObject formalSceneRoot,
            string groundAssetPath,
            string environmentAssetPath,
            string foregroundAssetPath,
            Vector2 center)
        {
            if (formalSceneRoot != null)
            {
                foreach (var tilemapRenderer in formalSceneRoot.GetComponentsInChildren<TilemapRenderer>(true))
                    tilemapRenderer.enabled = false;
                foreach (var formalRenderer in formalSceneRoot.GetComponentsInChildren<SpriteRenderer>(true))
                    formalRenderer.enabled = false;
                foreach (var collider in formalSceneRoot.GetComponentsInChildren<Collider2D>(true))
                    collider.enabled = false;
            }

            CreateMvpSceneLayer("[MVP Ground]", groundAssetPath,
                // The legacy project reserves Default for its bottom-most tile
                // layer. Keep the MVP ground there: it is below Environment,
                // Character and Foreground without migrating every frozen scene.
                "Default", -100, center);
            CreateMvpSceneLayer("[MVP Environment]", environmentAssetPath,
                GameConfig.SORTING_ENVIRONMENT, -100, center);
            CreateMvpSceneLayer("[MVP Foreground]", foregroundAssetPath,
                GameConfig.SORTING_FOREGROUND, 0, center);
        }

        private static void CreateMvpSceneLayer(
            string objectName,
            string assetPath,
            string sortingLayer,
            int sortingOrder,
            Vector2 center)
        {
            ConfigureMvpSpriteImporter(assetPath);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
                throw new System.InvalidOperationException(
                    "MVP scene layer is missing or not importable: " + assetPath);

            var layer = new GameObject(objectName);
            var renderer = layer.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = sortingOrder;
            layer.transform.position = new Vector3(center.x, center.y, 0f);
        }

        private static void ConfigureMvpSpriteImporter(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                throw new System.InvalidOperationException("Missing TextureImporter: " + assetPath);
            var changed = importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single
                || importer.spritePixelsPerUnit != GameConfig.PIXELS_PER_UNIT
                || importer.filterMode != FilterMode.Point
                || importer.textureCompression != TextureImporterCompression.Uncompressed
                || importer.mipmapEnabled;
            if (!changed) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = GameConfig.PIXELS_PER_UNIT;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        /// <summary>
        /// Static MVP actors share the same persistent, point-filtered import
        /// contract as the three environment layers. This is editor-only setup;
        /// gameplay still loads the baked Sprite via Resources.
        /// </summary>
        public static void ConfigureMvpActorSprite(string spriteId)
        {
            ConfigureMvpSpriteImporter("Assets/Resources/Art/MVP/" + spriteId + ".png");
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

        // ========== UI（HUD / 对话 / 暂停 / 过场 / EventSystem） ==========
        // docs/16 C.2：玩法 UI 与世界共用同一 480×270 逻辑展示面 ——
        // Screen Space - Camera 绑定像素相机 + 固定参考分辨率 scaler，
        // 画布随相机 pixelRect 整数缩放居中，禁止漂在外层窗口/letterbox 上。

        public static void ConfigureCanvasForPixelSurface(
            GameObject canvasObject, Camera uiCamera, int sortingOrder)
        {
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = uiCamera;
            canvas.planeDistance = 1f;
            canvas.sortingOrder = sortingOrder;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(
                GameConfig.NATIVE_WIDTH, GameConfig.NATIVE_HEIGHT);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        /// <summary>[ScreenTransition] 画布先于相机创建；相机就绪后补绑定同一逻辑展示面。</summary>
        public static void BindScreenTransitionToCamera(Camera uiCamera)
        {
            var transitions = Object.FindObjectsByType<ScreenTransition>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (transitions.Length == 0) return;

            ConfigureCanvasForPixelSurface(transitions[0].gameObject, uiCamera, 9998);
        }

        public static void CreateHudCanvas(Camera uiCamera)
        {
            var canvasObj = new GameObject("[HUD Canvas]");
            canvasObj.AddComponent<Canvas>();
            canvasObj.AddComponent<CanvasScaler>();
            ConfigureCanvasForPixelSurface(canvasObj, uiCamera, 400);
            // HUD v2 自动构建所有UI
            canvasObj.AddComponent<HUD>();
        }

        public static void CreateDialogueCanvas(Camera uiCamera)
        {
            var dlgCanvas = new GameObject("[Dialogue Canvas]");
            dlgCanvas.AddComponent<Canvas>();
            dlgCanvas.AddComponent<CanvasScaler>();
            ConfigureCanvasForPixelSurface(dlgCanvas, uiCamera, 500);
            // DialogueUI v2 自动构建对话框+选择面板
            dlgCanvas.AddComponent<DialogueUI>();
        }

        public static void CreatePauseCanvas(Camera uiCamera)
        {
            var pauseCanvas = new GameObject("[Pause Canvas]");
            pauseCanvas.AddComponent<Canvas>();
            pauseCanvas.AddComponent<CanvasScaler>();
            ConfigureCanvasForPixelSurface(pauseCanvas, uiCamera, 300);
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
