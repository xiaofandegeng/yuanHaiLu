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
