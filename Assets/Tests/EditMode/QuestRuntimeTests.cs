using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using YuanHaiLu.GameSystem;

namespace YuanHaiLu.Tests.EditMode
{
    public class QuestRuntimeTests
    {
        [TearDown]
        public void TearDown()
        {
            TestSceneFactory.DestroyAll();
        }

        [Test]
        public void QuestDatabaseResolvesCompleteM01Template()
        {
            QuestData quest = QuestDatabase.Get("M01_01");

            Assert.That(quest, Is.Not.Null);
            Assert.That(quest.questName, Is.EqualTo("初到烟柳镇"));
            Assert.That(quest.objectives, Has.Length.EqualTo(3));
            Assert.That(quest.unlockQuestIds, Is.EqualTo(new[] { "M01_02" }));
        }

        [Test]
        public void AcceptedQuestOwnsProgressWithoutMutatingTemplate()
        {
            QuestManager manager = CreateQuestManager();

            Assert.That(manager.AcceptQuestById("M01_01"), Is.True);
            manager.UpdateObjective(
                QuestObjective.ObjectiveType.ReachArea,
                "yanliu_inn");

            Assert.That(
                manager.GetActiveQuest("M01_01").Objectives[0].currentAmount,
                Is.EqualTo(1));
            Assert.That(
                QuestDatabase.Get("M01_01").objectives[0].currentAmount,
                Is.Zero);
        }

        [Test]
        public void UnknownQuestIdIsRejectedWithoutCreatingAnEmptyQuest()
        {
            QuestManager manager = CreateQuestManager();
            LogAssert.Expect(LogType.Warning, "[Quest] 任务模板不存在: missing_quest");

            bool accepted = manager.AcceptQuestById("missing_quest");

            Assert.That(accepted, Is.False);
            Assert.That(manager.ActiveQuests, Is.Empty);
        }

        [Test]
        public void ObjectiveProgressClampsAndMakesQuestCompletable()
        {
            QuestManager manager = CreateQuestManager();
            manager.LoadCompletedQuests(new[] { "M01_03" });
            Assert.That(manager.AcceptQuestById("M01_04"), Is.True);

            manager.UpdateObjective(
                QuestObjective.ObjectiveType.ReachArea,
                "north_mountain",
                10);
            manager.UpdateObjective(
                QuestObjective.ObjectiveType.KillEnemy,
                "bandit",
                20);
            manager.UpdateObjective(
                QuestObjective.ObjectiveType.DefeatBoss,
                "boss_heifeng",
                2);

            ActiveQuest active = manager.GetActiveQuest("M01_04");
            Assert.That(active.Objectives[0].currentAmount, Is.EqualTo(1));
            Assert.That(active.Objectives[1].currentAmount, Is.EqualTo(5));
            Assert.That(active.Objectives[2].currentAmount, Is.EqualTo(1));
            Assert.That(active.state, Is.EqualTo(ActiveQuest.QuestState.Completable));
        }

        [Test]
        public void QuestWithoutObjectivesIsImmediatelyCompletable()
        {
            QuestManager manager = CreateQuestManager();
            QuestData quest = TestSceneFactory.CreateScriptableObject<QuestData>();
            quest.questId = "empty_quest";
            quest.questName = "空目标任务";
            quest.objectives = new QuestObjective[0];

            Assert.That(manager.AcceptQuest(quest), Is.True);
            Assert.That(
                manager.GetActiveQuest("empty_quest").state,
                Is.EqualTo(ActiveQuest.QuestState.Completable));
        }

        private static QuestManager CreateQuestManager()
        {
            return TestSceneFactory.AddComponentWithAwake<QuestManager>(
                TestSceneFactory.Create("QuestManager"));
        }
    }
}
