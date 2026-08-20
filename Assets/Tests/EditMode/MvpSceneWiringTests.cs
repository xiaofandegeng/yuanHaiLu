using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using YuanHaiLu.Art;
using YuanHaiLu.Core;
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
    }
}
