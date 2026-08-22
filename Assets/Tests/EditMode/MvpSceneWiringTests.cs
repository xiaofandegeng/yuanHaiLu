using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using YuanHaiLu.Art;
using YuanHaiLu.Core;
using YuanHaiLu.Editor;
using YuanHaiLu.GameSystem;
using YuanHaiLu.Map;

namespace YuanHaiLu.Tests.EditMode
{
    /// <summary>
    /// MVP 场景接线（docs/15）：Demo 河岸子区 + 客栈门；Demo_Inn 室内掌柜/出口。
    /// 依赖 Unity 批处理先重生成 MainMenu / Demo_YanLiuTown / Demo_Inn 三个场景。
    /// </summary>
    public class MvpSceneWiringTests
    {
        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            typeof(ItemDatabase).GetField(
                "_items",
                BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, null);
        }

        [Test]
        public void DemoSceneWiresRiverbankCombatSubAreaAndInnDoor()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Demo_YanLiuTown.unity", OpenSceneMode.Single);

            // 河岸 ReachArea 触发器：非场景切换，先于一次性地名显示上报进度。
            var riverbank = Object.FindObjectsByType<AreaTrigger>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .FirstOrDefault(trigger => trigger.questTargetId == "yanliu_riverbank");
            Assert.That(riverbank, Is.Not.Null);
            Assert.That(riverbank.triggersSceneChange, Is.False);

            // 两名河岸水匪：死亡上报 KillEnemy river_bandit。
            var banditTargets = Object.FindObjectsByType<QuestTarget>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(target => target.targetId == "river_bandit")
                .ToArray();
            Assert.That(banditTargets, Has.Length.EqualTo(2));
            Assert.That(banditTargets.All(target =>
                target.objectiveType == QuestObjective.ObjectiveType.KillEnemy), Is.True);

            // 复审 P1-b：Demo 只保留这两名水匪；额外山贼/路匪与 BOSS 事件已随范围收缩移除。
            var allEnemies = Object.FindObjectsByType<Character.EnemyAI>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            Assert.That(allEnemies, Has.Length.EqualTo(2),
                "MVP scope: exactly two river bandits, no extra patrol groups.");
            Assert.That(GameObject.Find("Event_BossFight"), Is.Null,
                "MVP scope: boss fights are frozen content and must not appear in Demo.");

            // 掌柜的荷包拾取点。
            var pouch = Object.FindObjectsByType<ItemPickup>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .FirstOrDefault(pickup => pickup.itemId == "quest_lost_pouch");
            Assert.That(pouch, Is.Not.Null);

            // 客栈大门 → 客栈室内。
            var innDoor = Object.FindObjectsByType<AreaTrigger>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .FirstOrDefault(trigger => trigger.targetSceneName == "Demo_Inn");
            Assert.That(innDoor, Is.Not.Null);
            Assert.That(innDoor.triggersSceneChange, Is.True);
            Assert.That(innDoor.spawnPositionInTarget, Is.Not.EqualTo(Vector2.zero));

            // 任务阶段门（复审 P0）：水匪/荷包只在对应顺序步骤激活。
            var gates = Object.FindObjectsByType<QuestStageGate>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(gate => gate.questId == "MVP_01")
                .ToArray();
            var killGate = gates.FirstOrDefault(gate => gate.targetId == "river_bandit");
            Assert.That(killGate, Is.Not.Null);
            Assert.That(killGate.objectiveType,
                Is.EqualTo(QuestObjective.ObjectiveType.KillEnemy));
            Assert.That(killGate.targets, Is.Not.Empty);
            Assert.That(killGate.targets, Has.All.Not.Null);

            var collectGate = gates.FirstOrDefault(gate =>
                gate.targetId == "quest_lost_pouch");
            Assert.That(collectGate, Is.Not.Null);
            Assert.That(collectGate.objectiveType,
                Is.EqualTo(QuestObjective.ObjectiveType.CollectItem));
            Assert.That(collectGate.targets, Has.All.Not.Null);

            // 掌柜不再摆在镇上（已移入客栈室内）。
            Assert.That(GameObject.Find("NPC_掌柜老赵"), Is.Null);

            // 主角仍是固定男主。
            var player = GameObject.FindGameObjectWithTag("Player");
            Assert.That(player, Is.Not.Null);
            var playerVisual = player.GetComponent<CharacterVisual>();
            Assert.That(playerVisual, Is.Not.Null);
            Assert.That(playerVisual.ArtId, Is.EqualTo("player_male_swordsman"));
        }

        [Test]
        public void InnSceneContainsInnkeeperQuestGiverAndExitBackToDemo()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Demo_Inn.unity", OpenSceneMode.Single);

            var definition = Object.FindAnyObjectByType<RegionSceneDefinition>();
            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.SceneId, Is.EqualTo("inn"));

            var player = GameObject.FindGameObjectWithTag("Player");
            Assert.That(player, Is.Not.Null);
            var playerVisual = player.GetComponent<CharacterVisual>();
            Assert.That(playerVisual, Is.Not.Null);
            Assert.That(playerVisual.ArtId, Is.EqualTo("player_male_swordsman"));
            Assert.That(player.GetComponent<Character.PlayerCombat>(), Is.Not.Null);
            Assert.That(player.GetComponent<Character.MartialArtsSystem>(), Is.Not.Null);

            var innkeeper = GameObject.Find("NPC_掌柜老赵");
            Assert.That(innkeeper, Is.Not.Null);
            var questGiver = innkeeper.GetComponent<QuestGiver>();
            Assert.That(questGiver, Is.Not.Null);
            Assert.That(questGiver.questId, Is.EqualTo("MVP_01"));
            Assert.That(questGiver.interactionTargetId, Is.EqualTo("innkeeper_zhao"));
            Assert.That(innkeeper.GetComponent<Character.NPCBase>().canWander, Is.False);

            // 掌柜站在柜台后的画面构图不能牺牲任务入口：玩家在柜台前的可行走
            // 位置必须落在 1.2 世界单位交互圈内。旧布局将掌柜放在 y=11.9、
            // 碰撞柜台后方，玩家最近只能到 y≈9.4，首步“找掌柜”无法完成。
            Physics2D.SyncTransforms();
            var counterFront = new Vector2(15f, 9f);
            var playerFootprint = Physics2D.OverlapBox(
                counterFront + new Vector2(0f, 0.6f),
                new Vector2(0.9f, 1.3f),
                0f,
                LayerMask.GetMask("Environment"));
            Assert.That(playerFootprint, Is.Null,
                "柜台前必须留出玩家可站立的交互格");
            Assert.That(Vector2.Distance(counterFront, innkeeper.transform.position),
                Is.LessThanOrEqualTo(1.2f),
                "掌柜必须能从柜台前以默认交互距离触达");

            Assert.That(Object.FindAnyObjectByType<MvpDirectPlayFallback>(), Is.Not.Null,
                "Demo_Inn 直接按 Play 时必须补回 Exploration，不能锁死在 MainMenu。");

            var exit = Object.FindObjectsByType<AreaTrigger>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .FirstOrDefault(trigger => trigger.targetSceneName == "Demo_YanLiuTown");
            Assert.That(exit, Is.Not.Null);
            Assert.That(exit.triggersSceneChange, Is.True);

            Assert.That(Object.FindAnyObjectByType<UI.HUD>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<UI.DialogueUI>(), Is.Not.Null);
        }
        [Test]
        public void DemoSceneSpawnDoorAndRiverbankRouteAreWalkable()
        {
            // 先在客栈场景读回镇落点（出口触发器在 Demo_Inn 内）。
            EditorSceneManager.OpenScene("Assets/Scenes/Demo_Inn.unity", OpenSceneMode.Single);
            var exitToTown = Object.FindObjectsByType<AreaTrigger>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .FirstOrDefault(trigger => trigger.targetSceneName == "Demo_YanLiuTown");
            Assert.That(exitToTown, Is.Not.Null);
            var returnSpawn = exitToTown.spawnPositionInTarget;

            EditorSceneManager.OpenScene("Assets/Scenes/Demo_YanLiuTown.unity", OpenSceneMode.Single);
            Physics2D.SyncTransforms();

            // 出生点在地图内且位于可行走格（复审 P1：旧默认 (0,-5) 在地图外）。
            var director = Object.FindAnyObjectByType<SceneDirector>();
            Assert.That(director, Is.Not.Null);
            Assert.That(director.spawnPosition, Is.EqualTo(new Vector2(7.5f, 7.6f)));
            Assert.That(director.spawnPosition.x, Is.InRange(0f, 40f));
            Assert.That(director.spawnPosition.y, Is.InRange(5f, 24f));

            // 回镇落点落回本场景时，玩家碰撞盒不得与任何场景切换触发盒重叠，
            // 否则落地即被传回客栈形成往返软锁。
            var playerBox = new Bounds(
                new Vector3(returnSpawn.x, returnSpawn.y + 0.6f, 0f),
                new Vector3(0.8f, 1.2f, 0f));
            foreach (var trigger in Object.FindObjectsByType<AreaTrigger>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (!trigger.triggersSceneChange) continue;
                var col = trigger.GetComponent<Collider2D>();
                Assert.That(
                    playerBox.Intersects(col.bounds),
                    Is.False,
                    $"{trigger.name} 与回镇落点玩家碰撞盒重叠，会形成往返软锁");
            }

            // 出生点 → 客栈门 / 河岸触发器存在连续可行走路线（0.5 格 BFS）。
            Assert.That(IsWalkable(director.spawnPosition), Is.True, "出生点本身被阻挡");
            Assert.That(HasWalkableRoute(director.spawnPosition, new Vector2(7.5f, 9.9f)),
                Is.True, "出生点到客栈门不可达");
            Assert.That(HasWalkableRoute(director.spawnPosition, new Vector2(12f, 5.2f)),
                Is.True, "出生点到河岸子区不可达");
            Assert.That(HasWalkableRoute(new Vector2(12f, 5.2f), new Vector2(24f, 3f)),
                Is.True, "河岸到荷包拾取点不可达");
        }

        /// <summary>0.5 步长网格 BFS：以玩家半径膨胀环境阻挡后判定路线。</summary>
        private static bool HasWalkableRoute(Vector2 from, Vector2 to)
        {
            const float step = 0.5f;
            var start = new Vector2Int(
                Mathf.RoundToInt(from.x / step), Mathf.RoundToInt(from.y / step));
            var goal = new Vector2Int(
                Mathf.RoundToInt(to.x / step), Mathf.RoundToInt(to.y / step));

            var queue = new Queue<Vector2Int>();
            var visited = new HashSet<Vector2Int> { start };
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                if (cell == goal) return true;
                foreach (var delta in new[]
                         {
                             new Vector2Int(1, 0), new Vector2Int(-1, 0),
                             new Vector2Int(0, 1), new Vector2Int(0, -1),
                         })
                {
                    var next = cell + delta;
                    // 地图 40×24；0.5 格坐标下留 2 世界单位余量，防止 BFS 越界发散。
                    if (next.x < -4 || next.x > 84 || next.y < -4 || next.y > 52) continue;
                    if (!visited.Add(next)) continue;
                    if (!IsWalkable(new Vector2(next.x * step, next.y * step))) continue;
                    queue.Enqueue(next);
                }
            }
            return false;
        }

        private static bool IsWalkable(Vector2 worldPosition)
        {
            // 玩家碰撞盒 0.8×1.2；以中心点 + 0.45 半径近似膨胀判定。
            var hit = Physics2D.OverlapBox(
                worldPosition + new Vector2(0f, 0.6f),
                new Vector2(0.9f, 1.3f),
                0f,
                LayerMask.GetMask("Environment"));
            return hit == null;
        }

        [Test]
        public void DemoScenesShareThePixelCameraLogicalUiSurface()
        {
            // docs/16 C.2/F6：HUD、对话、暂停与过场画布必须与 480×270 世界
            // 共用同一逻辑展示面 —— Screen Space - Camera 绑定像素相机 +
            // 480×270 固定参考分辨率 scaler，禁止 Overlay 漂在 letterbox 上。
            foreach (var scenePath in new[]
                     {
                         "Assets/Scenes/Demo_YanLiuTown.unity",
                         "Assets/Scenes/Demo_Inn.unity",
                     })
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                var pixelCameras = Object.FindObjectsByType<PixelPerfectCamera>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
                Assert.That(pixelCameras.Length, Is.EqualTo(1),
                    $"{scenePath} 必须恰好一台像素相机");
                var gameCamera = pixelCameras[0].GetComponent<Camera>();
                Assert.That(gameCamera, Is.Not.Null);

                foreach (var canvasName in new[]
                         {
                             "[HUD Canvas]", "[Dialogue Canvas]", "[Pause Canvas]",
                         })
                {
                    AssertCanvasOnPixelSurface(scenePath, canvasName, gameCamera);
                }

                var transitions = Object.FindObjectsByType<ScreenTransition>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
                Assert.That(transitions.Length, Is.GreaterThanOrEqualTo(1),
                    $"{scenePath} 必须有过场画布");
                var transitionCanvas = transitions[0].GetComponent<Canvas>();
                Assert.That(transitionCanvas, Is.Not.Null);
                Assert.That(transitionCanvas.renderMode,
                    Is.EqualTo(RenderMode.ScreenSpaceCamera),
                    $"{scenePath} 过场画布必须在像素相机逻辑展示面上");
                Assert.That(transitionCanvas.worldCamera, Is.EqualTo(gameCamera),
                    $"{scenePath} 过场画布必须绑定游戏相机");
            }
        }

        [Test]
        public void DemoScenesHavePersistentLayeredMvpArtWithCharacterDepth()
        {
            // docs/17：MVP 必须是原生 480×270 像素层，不允许把整张高密度概念
            // 图置于所有游戏物体下面。角色在 Environment 与 Foreground 之间，
            // 所以门帘、屋檐、柜台等前景可形成真实遮挡。
            foreach (var scenePath in new[]
                     {
                         "Assets/Scenes/Demo_YanLiuTown.unity",
                         "Assets/Scenes/Demo_Inn.unity",
                     })
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                Assert.That(GameObject.Find("[MVP Backdrop]"), Is.Null,
                    $"{scenePath} 不能保留整张概念背景");

                var expectedLayers = new[]
                {
                    // Existing formal scenes use Unity's Default layer for their
                    // bottom tilemap.  The MVP keeps that convention instead of
                    // rewriting every frozen asset merely to rename Ground.
                    ("[MVP Ground]", "Default"),
                    ("[MVP Environment]", GameConfig.SORTING_ENVIRONMENT),
                    ("[MVP Foreground]", GameConfig.SORTING_FOREGROUND),
                };
                foreach (var (layerName, sortingLayer) in expectedLayers)
                {
                    var layer = GameObject.Find(layerName);
                    Assert.That(layer, Is.Not.Null, $"{scenePath} 缺少 {layerName}");
                    var renderer = layer.GetComponent<SpriteRenderer>();
                    Assert.That(renderer, Is.Not.Null);
                    Assert.That(renderer.sprite, Is.Not.Null);
                    Assert.That(AssetDatabase.Contains(renderer.sprite), Is.True,
                        $"{scenePath} 的 {layerName} 必须是持久资源");
                    Assert.That(renderer.sortingLayerName, Is.EqualTo(sortingLayer));
                    Assert.That(renderer.bounds.size.x, Is.EqualTo(30f).Within(0.01f));
                    Assert.That(renderer.bounds.size.y, Is.EqualTo(16.875f).Within(0.01f));
                }

                var player = GameObject.Find("Player");
                Assert.That(player, Is.Not.Null);
                var playerRenderer = player.GetComponent<SpriteRenderer>();
                Assert.That(playerRenderer, Is.Not.Null);
                Assert.That(playerRenderer.sortingLayerName,
                    Is.EqualTo(GameConfig.SORTING_CHARACTER));
            }
        }

        [Test]
        public void MvpOnlyActorsUsePersistentSpritesFromTheSharedPixelPalette()
        {
            foreach (var spriteId in new[]
                     {
                         "mvp_innkeeper", "mvp_bandit_a", "mvp_bandit_b", "mvp_lost_pouch",
                     })
            {
                var sprite = MvpArtCatalog.Load(spriteId);
                Assert.That(sprite, Is.Not.Null, $"MVP actor sprite '{spriteId}' is required");
                Assert.That(AssetDatabase.Contains(sprite), Is.True,
                    $"MVP actor sprite '{spriteId}' cannot be generated at runtime");
                Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(32f, 32f)));
            }

            EditorSceneManager.OpenScene("Assets/Scenes/Demo_YanLiuTown.unity", OpenSceneMode.Single);
            var bandits = Object.FindObjectsByType<Character.EnemyAI>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(bandits, Has.Length.EqualTo(2));
            Assert.That(bandits.All(bandit => bandit.GetComponent<MvpStaticVisual>() != null), Is.True);
            Assert.That(bandits.All(bandit => bandit.GetComponent<CharacterVisual>() == null), Is.True,
                "MVP bandits cannot mix frozen formal art into the new scene palette.");

            EditorSceneManager.OpenScene("Assets/Scenes/Demo_Inn.unity", OpenSceneMode.Single);
            var innkeeper = GameObject.Find("NPC_掌柜老赵");
            Assert.That(innkeeper, Is.Not.Null);
            Assert.That(innkeeper.GetComponent<MvpStaticVisual>(), Is.Not.Null);
            Assert.That(innkeeper.GetComponent<CharacterVisual>(), Is.Null);
        }

        [Test]
        public void GameplayCaptureIsolatesItsTargetSceneFromTheOpenEditorScene()
        {
            // CaptureMvpGameplay 过去以 Additive 打开目标场景但没有隐藏当前场景，
            // 因此从打开的客栈执行时，烟柳镇截图会被同层的客栈背景覆盖。两幅
            // 不同地点的 480×270 画面必须有实质像素差异，不能只是玩家位置不同。
            var directory = Path.Combine(Path.GetTempPath(), "yuanhailu-capture-isolation-test");
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
            try
            {
                EditorSceneManager.OpenScene("Assets/Scenes/Demo_Inn.unity", OpenSceneMode.Single);
                VisualRegressionCapture.CaptureMvpGameplay(directory);

                var town = Path.Combine(directory, "town-spawn-1x.png");
                var inn = Path.Combine(directory, "inn-counter-1x.png");
                Assert.That(File.Exists(town), Is.True);
                Assert.That(File.Exists(inn), Is.True);
                Assert.That(VisualRegressionCapture.ChangedPixelRatio(town, inn),
                    Is.GreaterThan(0.25f),
                    "Town capture must not be overdrawn by an already-open inn scene.");
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        [Test]
        public void GameplayCaptureUsesTheFullLogicalFrameWithoutSideClearBands()
        {
            // 截图必须与真实 480×270 逻辑画面同宽。此前先设置 pixelRect、后绑定
            // RenderTexture，Unity 会把 rect 按当前编辑器 Game View 钳制为 362px，
            // 两侧留下大块清屏色，正是用户看到“画面很小”的根因。
            var directory = Path.Combine(Path.GetTempPath(), "yuanhailu-capture-full-frame-test");
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
            try
            {
                EditorSceneManager.OpenScene("Assets/Scenes/Demo_YanLiuTown.unity", OpenSceneMode.Single);
                var reviewBandit = Object.FindObjectsByType<GameObject>(
                        FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Single(gameObject => gameObject.name == "Enemy_河岸水匪甲");
                var reviewPouch = Object.FindObjectsByType<GameObject>(
                        FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Single(gameObject => gameObject.name == "ItemPickup_LostPouch");
                var banditWasActive = reviewBandit.activeSelf;
                var pouchWasActive = reviewPouch.activeSelf;
                VisualRegressionCapture.CaptureMvpGameplay(directory);
                AssertCaptureReachesBothEdges(
                    Path.Combine(directory, "town-spawn-1x.png"),
                    new Color32(46, 56, 41, 255));
                AssertCaptureReachesBothEdges(
                    Path.Combine(directory, "inn-counter-1x.png"),
                    new Color32(36, 28, 23, 255));
                Assert.That(reviewBandit.activeSelf, Is.EqualTo(banditWasActive),
                    "Review capture must restore the kill gate after temporarily showing combat targets.");
                Assert.That(reviewPouch.activeSelf, Is.EqualTo(pouchWasActive),
                    "Review capture must restore the pouch gate after temporarily showing combat targets.");
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        private static void AssertCaptureReachesBothEdges(string imagePath, Color32 clearColor)
        {
            var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(ImageConversion.LoadImage(image, File.ReadAllBytes(imagePath)), Is.True);
                const int sampleY = 135;
                var firstContent = 0;
                while (firstContent < image.width &&
                       image.GetPixel(firstContent, sampleY) == (Color)clearColor)
                    firstContent++;
                var lastContent = image.width - 1;
                while (lastContent >= 0 &&
                       image.GetPixel(lastContent, sampleY) == (Color)clearColor)
                    lastContent--;

                Assert.That(firstContent, Is.LessThanOrEqualTo(2),
                    $"{Path.GetFileName(imagePath)} 左侧出现清屏色边带");
                Assert.That(lastContent, Is.GreaterThanOrEqualTo(image.width - 3),
                    $"{Path.GetFileName(imagePath)} 右侧出现清屏色边带");
            }
            finally
            {
                Object.DestroyImmediate(image);
            }
        }

        private static void AssertCanvasOnPixelSurface(
            string scenePath, string canvasName, Camera gameCamera)
        {
            var canvasObject = GameObject.Find(canvasName);
            Assert.That(canvasObject, Is.Not.Null,
                $"{scenePath} 缺少 {canvasName}");
            var canvas = canvasObject.GetComponent<Canvas>();
            Assert.That(canvas, Is.Not.Null);
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceCamera),
                $"{scenePath} {canvasName} 必须绑定像素相机（docs/16 C.2）");
            Assert.That(canvas.worldCamera, Is.EqualTo(gameCamera),
                $"{scenePath} {canvasName} 必须绑定游戏相机");
            var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            Assert.That(scaler, Is.Not.Null);
            Assert.That(scaler.uiScaleMode,
                Is.EqualTo(UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize),
                $"{scenePath} {canvasName} 缩放模式");
            Assert.That(scaler.referenceResolution,
                Is.EqualTo(new Vector2(480f, 270f)),
                $"{scenePath} {canvasName} 参考分辨率必须为 480×270");
        }
    }
}
