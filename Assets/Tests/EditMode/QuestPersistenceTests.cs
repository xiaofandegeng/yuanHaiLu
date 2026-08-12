using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using YuanHaiLu.Core;
using YuanHaiLu.GameSystem;

namespace YuanHaiLu.Tests.EditMode
{
    public class QuestPersistenceTests
    {
        private const int TestSaveSlot = 97;

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey($"YuanHaiLu_SaveSlot_{TestSaveSlot}");
            TestSceneFactory.DestroyAll();
        }

        [Test]
        public void ActiveQuestJsonRoundTripPreservesProgressTimeAndCompletedIds()
        {
            QuestManager manager = CreateQuestManager();
            manager.LoadCompletedQuests(new[] { "completed_side_quest" });
            Assert.That(manager.AcceptQuestById("M01_01"), Is.True);
            manager.UpdateObjective(
                QuestObjective.ObjectiveType.ReachArea,
                "yanliu_inn");
            DateTime acceptedAt = manager.GetActiveQuest("M01_01").acceptTime;

            string json = JsonUtility.ToJson(manager.GetSaveData());
            manager.ResetForNewGame();
            manager.LoadSaveData(JsonUtility.FromJson<QuestManager.QuestSaveData>(json));

            ActiveQuest restored = manager.GetActiveQuest("M01_01");
            Assert.That(restored, Is.Not.Null);
            Assert.That(restored.Objectives[0].currentAmount, Is.EqualTo(1));
            Assert.That(restored.Objectives[1].currentAmount, Is.Zero);
            Assert.That(restored.acceptTime.ToBinary(), Is.EqualTo(acceptedAt.ToBinary()));
            Assert.That(manager.CompletedQuestIds, Is.EqualTo(new[] { "completed_side_quest" }));
        }

        [Test]
        public void RestoreDeduplicatesIdsAndSkipsUnknownTemplates()
        {
            QuestManager manager = CreateQuestManager();
            var active = new QuestManager.ActiveQuestSaveData
            {
                questId = "M01_01",
                state = ActiveQuest.QuestState.Active,
                acceptTimeBinary = DateTime.Now.ToBinary(),
                objectives = new QuestManager.QuestObjectiveSaveData[0]
            };
            var save = new QuestManager.QuestSaveData
            {
                activeQuests = new[]
                {
                    new QuestManager.ActiveQuestSaveData { questId = "missing_quest" },
                    active,
                    active
                },
                completedQuestIds = new[] { "done", "", "done", null }
            };
            LogAssert.Expect(LogType.Warning, "[Quest] 存档中的任务模板不存在，已跳过: missing_quest");

            manager.LoadSaveData(save);

            Assert.That(manager.ActiveQuests, Has.Count.EqualTo(1));
            Assert.That(manager.ActiveQuests[0].data.questId, Is.EqualTo("M01_01"));
            Assert.That(manager.CompletedQuestIds, Is.EqualTo(new[] { "done" }));
        }

        [Test]
        public void RestoreMatchesObjectivesByIdentityAndClampsProgress()
        {
            QuestManager manager = CreateQuestManager();
            manager.LoadSaveData(new QuestManager.QuestSaveData
            {
                completedQuestIds = new[] { "M01_03" },
                activeQuests = new[]
                {
                    new QuestManager.ActiveQuestSaveData
                    {
                        questId = "M01_04",
                        state = ActiveQuest.QuestState.Active,
                        acceptTimeBinary = DateTime.Now.ToBinary(),
                        objectives = new[]
                        {
                            Objective(QuestObjective.ObjectiveType.DefeatBoss, "boss_heifeng", 4),
                            Objective(QuestObjective.ObjectiveType.KillEnemy, "bandit", 99),
                            Objective(QuestObjective.ObjectiveType.ReachArea, "north_mountain", 8)
                        }
                    }
                }
            });

            ActiveQuest restored = manager.GetActiveQuest("M01_04");
            Assert.That(restored.Objectives[0].currentAmount, Is.EqualTo(1));
            Assert.That(restored.Objectives[1].currentAmount, Is.EqualTo(5));
            Assert.That(restored.Objectives[2].currentAmount, Is.EqualTo(1));
            Assert.That(restored.state, Is.EqualTo(ActiveQuest.QuestState.Completable));
        }

        [Test]
        public void VersionTwoLoadClearsActiveStateAndRestoresCompletedIds()
        {
            GameManager gameManager = TestSceneFactory.AddComponentWithAwake<GameManager>(
                TestSceneFactory.Create("GameManager"));
            gameManager.BeginSceneEntry(GameManager.SceneEntryMode.LoadGame);
            QuestManager quests = CreateQuestManager();
            Assert.That(quests.AcceptQuestById("M01_01"), Is.True);
            TestSceneFactory.CreatePlayer();
            SaveManager saveManager = TestSceneFactory.AddComponentWithAwake<SaveManager>(
                TestSceneFactory.Create("SaveManager"));

            saveManager.ApplySaveDataToLoadedScene(new SaveManager.SaveData
            {
                saveVersion = 2,
                playerName = "凌霜",
                level = 1,
                baseAttack = 15,
                baseDefense = 5,
                baseAgility = 10,
                baseMaxHp = 100,
                baseMaxMp = 50,
                currentHp = 100,
                currentMp = 50,
                chapterIndex = 1,
                completedQuests = new[] { "M01_03" }
            });

            Assert.That(quests.ActiveQuests, Is.Empty);
            Assert.That(quests.CompletedQuestIds, Is.EqualTo(new[] { "M01_03" }));
        }

        [Test]
        public void SaveGameWritesVersionThreeQuestPayload()
        {
            TestSceneFactory.AddComponentWithAwake<GameManager>(
                TestSceneFactory.Create("GameManager"));
            QuestManager quests = CreateQuestManager();
            Assert.That(quests.AcceptQuestById("M01_01"), Is.True);
            quests.UpdateObjective(
                QuestObjective.ObjectiveType.ReachArea,
                "yanliu_inn");
            TestSceneFactory.CreatePlayer();
            SaveManager saveManager = TestSceneFactory.AddComponentWithAwake<SaveManager>(
                TestSceneFactory.Create("SaveManager"));

            saveManager.SaveGame(TestSaveSlot);

            string json = PlayerPrefs.GetString($"YuanHaiLu_SaveSlot_{TestSaveSlot}");
            SaveManager.SaveData restored = JsonUtility.FromJson<SaveManager.SaveData>(json);
            Assert.That(restored.saveVersion, Is.EqualTo(3));
            Assert.That(restored.quests, Is.Not.Null);
            Assert.That(restored.quests.activeQuests, Has.Length.EqualTo(1));
            Assert.That(restored.quests.activeQuests[0].questId, Is.EqualTo("M01_01"));
            Assert.That(restored.quests.activeQuests[0].objectives[0].currentAmount, Is.EqualTo(1));
        }

        private static QuestManager CreateQuestManager()
        {
            return TestSceneFactory.AddComponentWithAwake<QuestManager>(
                TestSceneFactory.Create("QuestManager"));
        }

        private static QuestManager.QuestObjectiveSaveData Objective(
            QuestObjective.ObjectiveType type,
            string targetId,
            int amount)
        {
            return new QuestManager.QuestObjectiveSaveData
            {
                type = type,
                targetId = targetId,
                currentAmount = amount
            };
        }
    }
}
