using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using YuanHaiLu.Character;
using YuanHaiLu.GameSystem;
using YuanHaiLu.Map;

namespace YuanHaiLu.Tests.PlayMode
{
    /// <summary>
    /// 复审 P0 回归：接任务/到达对应阶段前，河岸水匪与荷包由
    /// QuestStageGate 整体失活，玩家无法提前击杀或拾取导致
    /// MVP_01 永久软锁；按序推进后逐一激活，任务始终可完成。
    /// </summary>
    public class QuestStageGatePlayModeTests
    {
        private readonly List<GameObject> _createdObjects = new List<GameObject>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                    Object.Destroy(_createdObjects[i]);
                yield return null;
            }
            _createdObjects.Clear();
        }

        [UnityTest]
        public IEnumerator RiverBanditsAndPouchStayLockedUntilQuestStageReached()
        {
            var quests = CreateObject("QuestManager").AddComponent<QuestManager>();
            CreateObject("InventoryManager").AddComponent<InventoryManager>();
            yield return null; // 等待 Awake 初始化数据库

            var player = CreateObject("Player");
            player.tag = "Player";
            var rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            var playerCol = player.AddComponent<BoxCollider2D>();
            playerCol.size = new Vector2(0.8f, 1.2f);
            var playerStats = player.AddComponent<CharacterStats>();
            playerStats.agility = 0; // 排除闪避随机性

            var banditA = CreateBandit("BanditA");
            var banditB = CreateBandit("BanditB");
            var pouch = CreatePouch();

            var killGate = CreateObject("KillGate").AddComponent<QuestStageGate>();
            killGate.questId = "MVP_01";
            killGate.objectiveType = QuestObjective.ObjectiveType.KillEnemy;
            killGate.targetId = "river_bandit";
            killGate.targets = new[] { banditA, banditB };

            var collectGate = CreateObject("CollectGate").AddComponent<QuestStageGate>();
            collectGate.questId = "MVP_01";
            collectGate.objectiveType = QuestObjective.ObjectiveType.CollectItem;
            collectGate.targetId = "quest_lost_pouch";
            collectGate.targets = new[] { pouch };

            yield return null; // 门控 Start → 初次 Refresh

            // 接任务前：受控对象全部失活，玩家不可能提前消耗（P0 软锁根源）。
            Assert.That(banditA.activeSelf, Is.False);
            Assert.That(banditB.activeSelf, Is.False);
            Assert.That(pouch.activeSelf, Is.False);

            // 接任务后仍停在第一步（找掌柜），击杀门保持关闭。
            Assert.That(quests.AcceptQuestById("MVP_01"), Is.True);
            yield return null;
            Assert.That(banditA.activeSelf, Is.False);
            Assert.That(pouch.activeSelf, Is.False);

            // 第一、二步按序推进；到第三步（杀水匪）击杀门才开。
            Assert.That(quests.UpdateObjective(
                QuestObjective.ObjectiveType.TalkToNPC, "innkeeper_zhao"), Is.True);
            Assert.That(quests.UpdateObjective(
                QuestObjective.ObjectiveType.ReachArea, "yanliu_riverbank"), Is.True);
            yield return null;
            Assert.That(banditA.activeSelf, Is.True);
            Assert.That(banditB.activeSelf, Is.True);
            Assert.That(pouch.activeSelf, Is.False);

            // 真实死亡上报链路击杀两名水匪 → 第三步完成，拾取门接棒开启。
            Kill(banditA, playerStats);
            Kill(banditB, playerStats);
            var quest = quests.GetActiveQuest("MVP_01");
            Assert.That(quest.Objectives[2].completed, Is.True);
            yield return null;
            Assert.That(pouch.activeSelf, Is.True);

            // 玩家走到荷包位置真实拾取（物理触发 → 入包 → 上报 → 销毁）。
            player.transform.position = pouch.transform.position - new Vector3(0f, 1.5f, 0f);
            var deadline = Time.time + 5f;
            while (pouch != null && Time.time < deadline)
            {
                rb.linearVelocity = Vector2.up * 3f;
                yield return new WaitForFixedUpdate();
            }
            Assert.That(pouch == null, Is.True, "荷包应被玩家真实拾取并销毁");
            quest = quests.GetActiveQuest("MVP_01");
            Assert.That(quest.Objectives[3].completed, Is.True);

            // 第五步复命 → 可提交，任务链完整可完成，无软锁。
            Assert.That(quests.UpdateObjective(
                QuestObjective.ObjectiveType.TalkToNPC, "innkeeper_zhao"), Is.True);
            Assert.That(
                quests.GetActiveQuest("MVP_01").state,
                Is.EqualTo(ActiveQuest.QuestState.Completable));
        }

        private GameObject CreateObject(string name)
        {
            var obj = new GameObject("GateTest_" + name);
            _createdObjects.Add(obj);
            return obj;
        }

        private GameObject CreateBandit(string name)
        {
            var enemy = CreateObject(name);
            enemy.tag = "Enemy";
            var stats = enemy.AddComponent<CharacterStats>();
            stats.maxHp = 22;
            stats.currentHp = 22;
            stats.agility = 0;
            var target = enemy.AddComponent<QuestTarget>();
            target.objectiveType = QuestObjective.ObjectiveType.KillEnemy;
            target.targetId = "river_bandit";
            target.amount = 1;
            return enemy;
        }

        private GameObject CreatePouch()
        {
            var pouch = CreateObject("Pouch");
            pouch.transform.position = new Vector3(50f, 50f, 0f); // 远离其他测试对象
            var col = pouch.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.8f, 0.8f);
            var pickup = pouch.AddComponent<ItemPickup>();
            pickup.itemId = "quest_lost_pouch";
            pickup.pickupDelay = 0f;
            pickup.bobAmplitude = 0f;
            pickup.magnetRange = 0f;
            return pouch;
        }

        private static void Kill(GameObject enemy, CharacterStats attacker)
        {
            var stats = enemy.GetComponent<CharacterStats>();
            stats.TakeDamage(9999, attacker);
        }
    }
}
