using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using YuanHaiLu.Character;
using YuanHaiLu.GameSystem;
using YuanHaiLu.Map;

namespace YuanHaiLu.Tests.EditMode
{
    public class QuestIntegrationTests
    {
        [TearDown]
        public void TearDown()
        {
            TestSceneFactory.DestroyAll();
        }

        [Test]
        public void CompletingQuestGrantsEveryRewardOnlyOnce()
        {
            QuestManager manager = CreateQuestManager();
            GameObject player = TestSceneFactory.CreatePlayer();
            CharacterStats stats = player.GetComponent<CharacterStats>();
            InventoryManager inventory = TestSceneFactory.AddComponentWithAwake<InventoryManager>(
                TestSceneFactory.Create("InventoryManager"));
            MartialArtsSystem martial = TestSceneFactory.AddComponentWithAwake<MartialArtsSystem>(player);
            QuestData quest = CreateQuest("reward_quest");
            quest.rewardExp = 20;
            quest.rewardGold = 15;
            quest.rewardItemIds = new[] { "herb_medicinal" };
            quest.rewardSkillId = "dash_wind_step";
            int startingGold = inventory.Gold;

            Assert.That(manager.AcceptQuest(quest), Is.True);
            Assert.That(manager.CompleteQuest("reward_quest"), Is.True);
            Assert.That(manager.CompleteQuest("reward_quest"), Is.False);

            Assert.That(stats.exp, Is.EqualTo(20));
            Assert.That(inventory.Gold, Is.EqualTo(startingGold + 15));
            Assert.That(inventory.HasItem("herb_medicinal"), Is.True);
            Assert.That(martial.LearnedSkills.ContainsKey("dash_wind_step"), Is.True);
        }

        [Test]
        public void LearningSkillReportsObjectiveOnlyOnFirstLearn()
        {
            QuestManager manager = CreateQuestManager();
            QuestData quest = CreateQuest(
                "learn_quest",
                Objective(QuestObjective.ObjectiveType.LearnSkill, "dash_wind_step", 1));
            Assert.That(manager.AcceptQuest(quest), Is.True);
            GameObject player = TestSceneFactory.CreatePlayer();
            MartialArtsSystem martial = TestSceneFactory.AddComponentWithAwake<MartialArtsSystem>(player);
            MartialSkill skill = MartialSkillDatabase.Get("dash_wind_step");

            Assert.That(martial.LearnSkill(skill), Is.True);
            Assert.That(martial.LearnSkill(skill), Is.False);

            ActiveQuest active = manager.GetActiveQuest("learn_quest");
            Assert.That(active.Objectives[0].currentAmount, Is.EqualTo(1));
            Assert.That(active.state, Is.EqualTo(ActiveQuest.QuestState.Completable));
        }

        [Test]
        public void QuestTargetReportsEnemyDeathOnlyOnce()
        {
            QuestManager manager = CreateQuestManager();
            QuestData quest = CreateQuest(
                "kill_quest",
                Objective(QuestObjective.ObjectiveType.KillEnemy, "bandit", 2));
            Assert.That(manager.AcceptQuest(quest), Is.True);
            GameObject enemy = TestSceneFactory.Create("Bandit");
            CharacterStats stats = enemy.AddComponent<CharacterStats>();
            stats.defense = 0;
            stats.agility = 0;
            stats.maxHp = 1;
            stats.currentHp = 1;
            QuestTarget target = enemy.AddComponent<QuestTarget>();
            target.objectiveType = QuestObjective.ObjectiveType.KillEnemy;
            target.targetId = "bandit";

            stats.TakeDamage(10);
            target.ReportDefeat();

            Assert.That(
                manager.GetActiveQuest("kill_quest").Objectives[0].currentAmount,
                Is.EqualTo(1));
        }

        [Test]
        public void AreaTargetReportsOnlyOnceWhenConfiguredShowOnce()
        {
            QuestManager manager = CreateQuestManager();
            QuestData quest = CreateQuest(
                "area_quest",
                Objective(QuestObjective.ObjectiveType.ReachArea, "yanliu_inn", 2));
            Assert.That(manager.AcceptQuest(quest), Is.True);
            GameObject areaObject = TestSceneFactory.Create("Area");
            areaObject.AddComponent<BoxCollider2D>();
            AreaTrigger area = areaObject.AddComponent<AreaTrigger>();
            area.questTargetId = "yanliu_inn";
            area.showOnce = true;

            area.ReportAreaReached();
            area.ReportAreaReached();

            Assert.That(
                manager.GetActiveQuest("area_quest").Objectives[0].currentAmount,
                Is.EqualTo(1));
        }

        [Test]
        public void InventoryAddIsAtomicAndPublishesRequestedAmount()
        {
            TestSceneFactory.CreatePlayer();
            InventoryManager inventory = TestSceneFactory.AddComponentWithAwake<InventoryManager>(
                TestSceneFactory.Create("InventoryManager"));
            FillInventory(inventory);
            inventory.Slots[0].itemId = "herb_medicinal";
            inventory.Slots[0].itemData = inventory.GetItemData("herb_medicinal");
            inventory.Slots[0].amount = 98;
            LogAssert.Expect(LogType.Warning, "[Inventory] 背包空间不足！");

            Assert.That(inventory.AddItem("herb_medicinal", 2), Is.False);
            Assert.That(inventory.Slots[0].amount, Is.EqualTo(98));

            inventory.Slots[1] = new InventorySlot();
            int publishedAmount = 0;
            inventory.OnItemAdded += (_, amount) => publishedAmount = amount;
            Assert.That(inventory.AddItem("herb_medicinal", 2), Is.True);
            Assert.That(publishedAmount, Is.EqualTo(2));
            Assert.That(inventory.Slots[0].amount, Is.EqualTo(99));
            Assert.That(inventory.Slots[1].amount, Is.EqualTo(1));
        }

        [Test]
        public void PickupReportsCollectionOnlyAfterInventoryAcceptsFullAmount()
        {
            QuestManager manager = CreateQuestManager();
            QuestData quest = CreateQuest(
                "collect_quest",
                Objective(QuestObjective.ObjectiveType.CollectItem, "herb_medicinal", 1));
            Assert.That(manager.AcceptQuest(quest), Is.True);
            TestSceneFactory.CreatePlayer();
            InventoryManager inventory = TestSceneFactory.AddComponentWithAwake<InventoryManager>(
                TestSceneFactory.Create("InventoryManager"));
            FillInventory(inventory);
            LogAssert.Expect(LogType.Warning, "[Inventory] 背包空间不足！");

            Assert.That(ItemPickup.TryAddToInventory("herb_medicinal", 1), Is.False);
            Assert.That(manager.GetActiveQuest("collect_quest").Objectives[0].currentAmount, Is.Zero);

            inventory.Slots[0] = new InventorySlot();
            Assert.That(ItemPickup.TryAddToInventory("herb_medicinal", 1), Is.True);
            Assert.That(manager.GetActiveQuest("collect_quest").Objectives[0].currentAmount, Is.EqualTo(1));
        }

        private static QuestManager CreateQuestManager()
        {
            return TestSceneFactory.AddComponentWithAwake<QuestManager>(
                TestSceneFactory.Create("QuestManager"));
        }

        private static QuestData CreateQuest(string id, params QuestObjective[] objectives)
        {
            QuestData quest = TestSceneFactory.CreateScriptableObject<QuestData>();
            quest.questId = id;
            quest.questName = id;
            quest.objectives = objectives;
            return quest;
        }

        private static QuestObjective Objective(
            QuestObjective.ObjectiveType type,
            string targetId,
            int requiredAmount)
        {
            return new QuestObjective
            {
                type = type,
                targetId = targetId,
                targetName = targetId,
                requiredAmount = requiredAmount
            };
        }

        private static void FillInventory(InventoryManager inventory)
        {
            ItemData filler = inventory.GetItemData("sword_iron");
            foreach (InventorySlot slot in inventory.Slots)
            {
                slot.itemId = filler.itemId;
                slot.itemData = filler;
                slot.amount = 1;
            }
        }
    }
}
