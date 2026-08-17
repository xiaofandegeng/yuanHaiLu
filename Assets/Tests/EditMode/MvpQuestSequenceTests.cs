using System.Linq;
using NUnit.Framework;
using UnityEngine;
using YuanHaiLu.GameSystem;

namespace YuanHaiLu.Tests.EditMode
{
    /// <summary>
    /// MVP_01 河岸失物：五步固定顺序，每步只在真实成功后推进，不重复计数。
    /// </summary>
    public class MvpQuestSequenceTests
    {
        [TearDown]
        public void TearDown()
        {
            TestSceneFactory.DestroyAll();
        }

        private static QuestManager CreateQuestManager()
        {
            return TestSceneFactory.AddComponentWithAwake<QuestManager>(
                TestSceneFactory.Create("QuestManager"));
        }

        [Test]
        public void TemplateDefinesFiveSequentialObjectivesInFixedOrder()
        {
            var quest = QuestDatabase.Get("MVP_01");

            Assert.That(quest, Is.Not.Null);
            Assert.That(quest.sequentialObjectives, Is.True);
            Assert.That(
                quest.objectives.Select(objective => (objective.type, objective.targetId)),
                Is.EqualTo(new[]
                {
                    (QuestObjective.ObjectiveType.TalkToNPC, "innkeeper_zhao"),
                    (QuestObjective.ObjectiveType.ReachArea, "yanliu_riverbank"),
                    (QuestObjective.ObjectiveType.KillEnemy, "river_bandit"),
                    (QuestObjective.ObjectiveType.CollectItem, "quest_lost_pouch"),
                    (QuestObjective.ObjectiveType.TalkToNPC, "innkeeper_zhao")
                }));
            Assert.That(
                quest.objectives.First(objective => objective.targetId == "river_bandit").requiredAmount,
                Is.EqualTo(2));
            Assert.That(ItemDatabase.Get("quest_lost_pouch"), Is.Not.Null);
        }

        [Test]
        public void M01TemplatesRemainFreelyOrderable()
        {
            Assert.That(QuestDatabase.Get("M01_01").sequentialObjectives, Is.False);
            Assert.That(QuestDatabase.Get("M01_05").sequentialObjectives, Is.False);
        }

        [Test]
        public void ObjectivesOnlyAdvanceInOrderAndNeverDoubleCount()
        {
            var questManager = CreateQuestManager();
            Assert.That(questManager.AcceptQuestById("MVP_01"), Is.True);

            // 接任务前的旧上报不推进任何目标。
            Assert.That(questManager.UpdateObjective(
                QuestObjective.ObjectiveType.ReachArea, "yanliu_riverbank"), Is.False);
            Assert.That(questManager.UpdateObjective(
                QuestObjective.ObjectiveType.KillEnemy, "river_bandit"), Is.False);

            // 第一步：与掌柜交谈。
            Assert.That(questManager.UpdateObjective(
                QuestObjective.ObjectiveType.TalkToNPC, "innkeeper_zhao"), Is.True);
            // 同一步重复上报不再计数。
            Assert.That(questManager.UpdateObjective(
                QuestObjective.ObjectiveType.TalkToNPC, "innkeeper_zhao"), Is.False);

            // 第二步：到达河岸。跳步的上报（拾取/击杀）仍被顺序门拦下。
            Assert.That(questManager.UpdateObjective(
                QuestObjective.ObjectiveType.CollectItem, "quest_lost_pouch"), Is.False);
            Assert.That(questManager.UpdateObjective(
                QuestObjective.ObjectiveType.ReachArea, "yanliu_riverbank"), Is.True);

            // 第三步：击败两名水匪（每次真实死亡上报 1）。
            Assert.That(questManager.UpdateObjective(
                QuestObjective.ObjectiveType.KillEnemy, "river_bandit"), Is.True);
            Assert.That(questManager.UpdateObjective(
                QuestObjective.ObjectiveType.KillEnemy, "river_bandit"), Is.True);
            // 击杀数不超出 requiredAmount。
            Assert.That(questManager.UpdateObjective(
                QuestObjective.ObjectiveType.KillEnemy, "river_bandit"), Is.False);

            // 第四步：拾回荷包。
            Assert.That(questManager.UpdateObjective(
                QuestObjective.ObjectiveType.CollectItem, "quest_lost_pouch"), Is.True);

            // 第五步：回掌柜处复命 → 任务进入可提交状态。
            Assert.That(questManager.UpdateObjective(
                QuestObjective.ObjectiveType.TalkToNPC, "innkeeper_zhao"), Is.True);

            var active = questManager.GetActiveQuest("MVP_01");
            Assert.That(active.state, Is.EqualTo(ActiveQuest.QuestState.Completable));
            Assert.That(active.AllObjectivesComplete(), Is.True);
            foreach (var objective in active.Objectives)
                Assert.That(objective.currentAmount, Is.EqualTo(objective.requiredAmount));
        }

        [Test]
        public void QuestSurvivesSaveRoundTripWithSequentialProgress()
        {
            var questManager = CreateQuestManager();
            questManager.AcceptQuestById("MVP_01");
            questManager.UpdateObjective(QuestObjective.ObjectiveType.TalkToNPC, "innkeeper_zhao");
            questManager.UpdateObjective(QuestObjective.ObjectiveType.ReachArea, "yanliu_riverbank");

            var json = JsonUtility.ToJson(questManager.GetSaveData());
            questManager.LoadSaveData(JsonUtility.FromJson<QuestManager.QuestSaveData>(json));

            var restored = questManager.GetActiveQuest("MVP_01");
            Assert.That(restored.data.sequentialObjectives, Is.True);
            Assert.That(restored.Objectives[0].completed, Is.True);
            Assert.That(restored.Objectives[1].completed, Is.True);
            Assert.That(restored.Objectives[2].completed, Is.False);

            // 恢复后顺序门仍然生效：不能跳过击杀直接拾取。
            Assert.That(questManager.UpdateObjective(
                QuestObjective.ObjectiveType.CollectItem, "quest_lost_pouch"), Is.False);
            Assert.That(questManager.UpdateObjective(
                QuestObjective.ObjectiveType.KillEnemy, "river_bandit"), Is.True);
        }
    }
}
