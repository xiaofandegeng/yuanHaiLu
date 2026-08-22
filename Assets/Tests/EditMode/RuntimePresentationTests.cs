using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using YuanHaiLu.Art;
using YuanHaiLu.Core;
using YuanHaiLu.GameSystem;
using YuanHaiLu.UI;

namespace YuanHaiLu.Tests.EditMode
{
    public class RuntimePresentationTests
    {
        [TearDown]
        public void TearDown()
        {
            TestSceneFactory.DestroyAll();
        }

        [Test]
        public void PixelPerfectCameraClearsTheFullScreenOutsideItsViewport()
        {
            var cameraObject = TestSceneFactory.Create("Main Camera");
            var mainCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<PixelPerfectCamera>();

            Camera clearCamera = cameraObject
                .GetComponentsInChildren<Camera>(true)
                .Single(camera => camera != mainCamera);

            Assert.That(clearCamera.rect, Is.EqualTo(new Rect(0f, 0f, 1f, 1f)));
            Assert.That(clearCamera.cullingMask, Is.Zero);
            Assert.That(clearCamera.clearFlags, Is.EqualTo(CameraClearFlags.SolidColor));
            Assert.That(clearCamera.depth, Is.LessThan(mainCamera.depth));
        }

        [Test]
        public void MvpLootDropSpritesArePersistentAssets()
        {
            // 复审 P1：敌人掉落必须用 Resources/Art/MVP 下的持久精灵，
            // 禁止运行时 Texture2D/Sprite.Create 生成掉落贴图。
            Assert.That(Art.MvpArtCatalog.Load("loot_gold"), Is.Not.Null,
                "loot_gold persistent sprite is missing from Resources/Art/MVP.");
            Assert.That(Art.MvpArtCatalog.Load("loot_item"), Is.Not.Null,
                "loot_item persistent sprite is missing from Resources/Art/MVP.");
        }

        [Test]
        public void GoldDropIsPureVisualFeedbackAndItemDropsRemainCollectable()
        {
            // 复审四轮 Spec-P2：金币击杀即时入账，地面铜钱必须是短命纯视觉反馈，
            // 不得再携带碰撞体或 ItemPickup 留下拾取不了的假掉落；
            // 物品掉落仍必须带 itemId/数量且可拾取（复审三轮 P1）。
            var lootTable = TestSceneFactory.Create("LootEnemy").AddComponent<LootTable>();

            lootTable.SpawnGoldFeedback(Vector2.zero);
            var goldDrop = GameObject.Find("Loot_Gold_Feedback");
            Assert.That(goldDrop, Is.Not.Null);
            Assert.That(goldDrop.GetComponent<Collider2D>(), Is.Null,
                "Gold is credited on kill; the coin feedback must not carry a collider.");
            Assert.That(goldDrop.GetComponent<Map.ItemPickup>(), Is.Null,
                "The coin feedback must not pose as a collectable pickup.");
            Assert.That(goldDrop.GetComponent<GoldFeedbackSprite>(), Is.Not.Null,
                "The coin must self-animate and self-destroy via GoldFeedbackSprite.");
            var goldRenderer = goldDrop.GetComponent<SpriteRenderer>();
            Assert.That(goldRenderer.sprite, Is.Not.Null);
            Assert.That(AssetDatabase.Contains(goldRenderer.sprite), Is.True,
                "The coin must use the persistent loot_gold sprite asset.");

            lootTable.SpawnItemDrop(Vector2.zero, "herb_medicinal", 2);
            var itemDrop = GameObject.Find("Loot_herb_medicinal");
            Assert.That(itemDrop, Is.Not.Null);
            var pickup = itemDrop.GetComponent<Map.ItemPickup>();
            Assert.That(pickup, Is.Not.Null);
            Assert.That(pickup.itemId, Is.EqualTo("herb_medicinal"));
            Assert.That(pickup.amount, Is.EqualTo(2));

            Object.DestroyImmediate(goldDrop);
            Object.DestroyImmediate(itemDrop);
        }

        [Test]
        public void PixelCameraKeepsConstantWorldCoverageAcrossWindowSizes()
        {
            // docs/16 C.1 契约：逻辑画面 480×270、PPU 16 → 世界正交尺寸恒为
            // 270/(2×16)=8.4375；窗口尺寸与整数倍率只决定 pixelRect，不得扩大世界覆盖。
            var method = typeof(PixelPerfectCamera).GetMethod(
                "UpdateCameraForScreen",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null,
                "PixelPerfectCamera 必须提供可注入窗口尺寸的刷新入口，" +
                "否则无法在测试中模拟不同外层屏幕尺寸（docs/16 C.1）。");

            var cameraObject = TestSceneFactory.Create("Main Camera");
            cameraObject.AddComponent<Camera>();
            var pixelCamera = cameraObject.AddComponent<PixelPerfectCamera>();
            var camera = cameraObject.GetComponent<Camera>();

            // 1440×900（3× 可用）：世界覆盖必须保持 480×270。
            method.Invoke(pixelCamera, new object[] { 1440, 900 });
            Assert.That(camera.orthographicSize, Is.EqualTo(8.4375f).Within(0.0001f),
                "1440×900 窗口下 orthographicSize 必须恒为 8.4375，" +
                "旧公式按屏幕高参与计算会把世界缩成缩略图（docs/16 P0）。");
            Assert.That(pixelCamera.GetCurrentScale(), Is.EqualTo(3));

            // 480×270（1× 恰好）：同一世界覆盖。
            method.Invoke(pixelCamera, new object[] { 480, 270 });
            Assert.That(camera.orthographicSize, Is.EqualTo(8.4375f).Within(0.0001f));
            Assert.That(pixelCamera.GetCurrentScale(), Is.EqualTo(1));

            // 不足一倍（安全降级）：世界覆盖仍不得改变。
            method.Invoke(pixelCamera, new object[] { 320, 200 });
            Assert.That(camera.orthographicSize, Is.EqualTo(8.4375f).Within(0.0001f),
                "小于逻辑画面的窗口只能裁剪显示，不得缩放世界。");
            Assert.That(pixelCamera.GetCurrentScale(), Is.EqualTo(1));

            // 居中整数倍视口矩形（纯函数；Camera.pixelRect 会被实际屏幕钳制，
            // 居中数学以 CalculateViewportRect 为准）。
            Assert.That(
                PixelPerfectCamera.CalculateViewportRect(1440, 900, 480, 270, 3),
                Is.EqualTo(new Rect(0f, 45f, 1440f, 810f)));
            Assert.That(
                PixelPerfectCamera.CalculateViewportRect(480, 270, 480, 270, 1),
                Is.EqualTo(new Rect(0f, 0f, 480f, 270f)));
            var centered = PixelPerfectCamera.CalculateViewportRect(
                1000, 600, 480, 270, 2);
            Assert.That(centered.x + centered.width / 2f, Is.EqualTo(500f).Within(0.5f),
                "pixelRect 必须水平居中");
            Assert.That(centered.y + centered.height / 2f, Is.EqualTo(300f).Within(0.5f),
                "pixelRect 必须垂直居中");
            Assert.That(centered.width, Is.LessThanOrEqualTo(1000f));
            Assert.That(centered.height, Is.LessThanOrEqualTo(600f));
        }

        [Test]
        public void CameraFollowClampsTheMvpViewAtTheMapEdge()
        {
            // docs/16：试玩相机保持 30×16.875 世界视野时，出生点靠近地图西南缘
            // 仍必须看到完整场景，而非引擎清屏色。移除 ConfigureBounds/SnapToTarget
            // 或回退为直接按玩家坐标取景时，此测试必须失败。
            var cameraObject = TestSceneFactory.Create("Main Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8.4375f;
            camera.aspect = 16f / 9f;
            var follow = TestSceneFactory.AddComponentWithAwake<CameraFollow>(cameraObject);
            var target = TestSceneFactory.Create("Player");
            target.transform.position = new Vector3(7.5f, 7.6f, 0f);

            follow.SetTarget(target.transform);
            follow.ConfigureBounds(Vector2.zero, new Vector2(40f, 24f));
            follow.SnapToTarget();

            Assert.That(cameraObject.transform.position.x, Is.EqualTo(15f).Within(0.001f));
            Assert.That(cameraObject.transform.position.y, Is.EqualTo(8.4375f).Within(0.001f));
        }

        [Test]
        public void MvpHudUsesCompactSafeAreasInsteadOfA_DebugSizedTray()
        {
            // docs/16 Gate R1：信息 UI 可见但不得把 480×270 试玩画面压成
            // 左侧条块+底部大黑槽。若恢复旧宽 40% 的技能栏或高 30% 的状态栏，
            // 锚点和尺寸都会让本测试失败。
            var canvasObject = TestSceneFactory.Create("[HUD Canvas]");
            canvasObject.AddComponent<Canvas>();
            canvasObject.AddComponent<CanvasScaler>();
            // AddComponent 在 EditMode 不保证调用 Awake；HUD 的画布节点正是在
            // Awake 中构建，测试必须按真实生命周期显式触发它。
            var hud = TestSceneFactory.AddComponentWithAwake<HUD>(canvasObject);

            var bars = hud.transform.Find("Bars").GetComponent<RectTransform>();
            Assert.That(bars.anchorMin, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(bars.anchorMax, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(bars.sizeDelta.x, Is.LessThanOrEqualTo(116f));
            Assert.That(bars.sizeDelta.y, Is.LessThanOrEqualTo(42f));

            var skills = hud.transform.Find("SkillBar").GetComponent<RectTransform>();
            Assert.That(skills.anchorMin, Is.EqualTo(new Vector2(0.5f, 0f)));
            Assert.That(skills.anchorMax, Is.EqualTo(new Vector2(0.5f, 0f)));
            Assert.That(skills.sizeDelta.x, Is.LessThanOrEqualTo(104f));
            Assert.That(skills.sizeDelta.y, Is.LessThanOrEqualTo(28f));
        }

        [Test]
        public void DirectPlayFallbackMakesTheInnImmediatelyInteractive()
        {
            // 试玩场景可被开发者直接按 Play。此前 GameManager.Start() 固定切到
            // MainMenu，而客栈没有 SceneDirector 补回 Exploration，导致角色、交互
            // 与任务全部被锁死。这个显式后备入口只能在 MainMenu 状态下介入，不能
            // 覆盖正常的读档或跨场景切换。
            // 其他测试留下的延迟销毁单例不应让本测试的 GameManager 失去 Instance。
            // 按游戏对象销毁生命周期先清场，随后创建唯一的可用管理器。
            foreach (var existing in Object.FindObjectsByType<GameManager>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
                Object.DestroyImmediate(existing.gameObject);
            var manager = TestSceneFactory.AddComponentWithAwake<GameManager>(
                TestSceneFactory.Create("[GameManager]"));
            manager.SetState(GameManager.GameState.MainMenu);
            manager.BeginSceneEntry(GameManager.SceneEntryMode.NewGame);
            var fallback = TestSceneFactory.Create("[MVP Inn Entry]")
                .AddComponent<MvpDirectPlayFallback>();

            fallback.ActivateIfDirectPlay();

            Assert.That(manager.currentState, Is.EqualTo(GameManager.GameState.Exploration));
            Assert.That(manager.CurrentSceneEntryMode,
                Is.EqualTo(GameManager.SceneEntryMode.Active));
        }

        [Test]
        public void CameraFollowDoesNotJumpOutsideUnconfiguredBounds()
        {
            var cameraObject = TestSceneFactory.Create("Main Camera");
            cameraObject.AddComponent<Camera>().orthographicSize = 5f;
            var follow = TestSceneFactory.AddComponentWithAwake<CameraFollow>(cameraObject);
            var target = TestSceneFactory.Create("Player");
            target.transform.position = Vector3.zero;
            follow.SetTarget(target.transform);

            InvokePrivate(follow, "LateUpdate");

            Assert.That(cameraObject.transform.position.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(cameraObject.transform.position.y, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void PauseMenuBuildsVisibleDefaultPanelWhenReferencesAreMissing()
        {
            var root = TestSceneFactory.Create("Pause Canvas");
            root.AddComponent<Canvas>();
            var pauseMenu = root.AddComponent<PauseMenu>();

            InvokePrivate(pauseMenu, "Start");
            pauseMenu.Pause();

            var panel = root.transform.Find("PausePanel");
            Assert.That(panel, Is.Not.Null, "Generated Demo scenes need a usable fallback pause panel.");
            Assert.That(panel.gameObject.activeSelf, Is.True);
            Assert.That(panel.GetComponentsInChildren<Button>(true).Length, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void MissingSfxWarningIsLoggedOnlyOncePerClip()
        {
            var audioManager = TestSceneFactory.Create("AudioManager").AddComponent<AudioManager>();
            int warningCount = 0;
            Application.LogCallback captureMissingAudio = (condition, _, type) =>
            {
                if (type == LogType.Warning && condition.Contains("missing_test_sfx"))
                    warningCount++;
            };

            Application.logMessageReceived += captureMissingAudio;
            try
            {
                audioManager.PlaySFX("missing_test_sfx");
                audioManager.PlaySFX("missing_test_sfx");
            }
            finally
            {
                Application.logMessageReceived -= captureMissingAudio;
            }

            Assert.That(warningCount, Is.EqualTo(1));
        }

        private static void InvokePrivate(object target, string methodName)
        {
            target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(target, null);
        }

    }
}
