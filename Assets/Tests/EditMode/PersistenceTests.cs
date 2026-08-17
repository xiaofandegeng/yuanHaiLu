using NUnit.Framework;
using UnityEngine;
using YuanHaiLu.Character;
using YuanHaiLu.Core;
using YuanHaiLu.GameSystem;

namespace YuanHaiLu.Tests.EditMode
{
    public class PersistenceTests
    {
        [TearDown]
        public void TearDown()
        {
            TestSceneFactory.DestroyAll();
        }

        [Test]
        public void InventoryLoadRestoresEquipmentWithoutHealingSavedResources()
        {
            var player = TestSceneFactory.CreatePlayer();
            var stats = player.GetComponent<CharacterStats>();
            stats.SetBaseFromLoad(15, 5, 10, 100, 50, 40, 20);
            var inventory = TestSceneFactory.AddComponentWithAwake<InventoryManager>(
                TestSceneFactory.Create("Inventory"));
            var data = new InventoryManager.InventorySaveData
            {
                slotItemIds = new[] { "herb_medicinal" },
                slotAmounts = new[] { 2 },
                equippedWeapon = "sword_iron",
                equippedArmor = "",
                equippedAccessory = "",
                gold = 77
            };

            inventory.LoadSaveData(data);

            Assert.That(inventory.GetItemData("sword_iron"), Is.Not.Null);
            Assert.That(stats.attack, Is.EqualTo(20));
            Assert.That(stats.currentHp, Is.EqualTo(40));
            Assert.That(inventory.Gold, Is.EqualTo(77));
        }

        [Test]
        public void InventoryLoadClearsSlotsMissingFromShorterSaveArrays()
        {
            TestSceneFactory.CreatePlayer();
            var inventory = TestSceneFactory.AddComponentWithAwake<InventoryManager>(
                TestSceneFactory.Create("Inventory"));
            inventory.Slots[1].itemId = "food_mantou";
            inventory.Slots[1].itemData = ItemDatabase.Get("food_mantou");
            inventory.Slots[1].amount = 3;

            inventory.LoadSaveData(new InventoryManager.InventorySaveData
            {
                slotItemIds = new[] { "herb_medicinal" },
                slotAmounts = new[] { 1 },
                equippedWeapon = "",
                equippedArmor = "",
                equippedAccessory = "",
                gold = 10
            });

            Assert.That(inventory.Slots[0].itemId, Is.EqualTo("herb_medicinal"));
            Assert.That(inventory.Slots[1].IsEmpty, Is.True);
        }

        [Test]
        public void MartialArtsLoadReplacesOldSlotsAndIgnoresMissingData()
        {
            var player = TestSceneFactory.CreatePlayer();
            var martial = TestSceneFactory.AddComponentWithAwake<MartialArtsSystem>(player);
            martial.LoadSaveData(new MartialArtsSystem.MartialArtsSaveData
            {
                learnedSkillIds = new[] { "basic_slash" },
                equippedSkillIds = new[] { "basic_slash", "", "", "" }
            }, MartialSkillDatabase.AllSkills);
            Assert.That(martial.EquippedSkills[0], Is.Not.Null);

            martial.LoadSaveData(new MartialArtsSystem.MartialArtsSaveData
            {
                learnedSkillIds = null,
                equippedSkillIds = null
            }, MartialSkillDatabase.AllSkills);

            Assert.That(martial.LearnedSkills, Is.Empty);
            Assert.That(martial.EquippedSkills, Is.All.Null);

            Assert.DoesNotThrow(() => martial.LoadSaveData(
                new MartialArtsSystem.MartialArtsSaveData
                {
                    learnedSkillIds = new[] { "missing_skill" },
                    equippedSkillIds = new[] { "missing_skill" }
                },
                MartialSkillDatabase.AllSkills));
            Assert.That(martial.LearnedSkills, Is.Empty);
        }

        [Test]
        public void QuestLoadDeduplicatesIdsAndNewGameResetClearsAllProgress()
        {
            var questManager = TestSceneFactory.AddComponentWithAwake<QuestManager>(
                TestSceneFactory.Create("QuestManager"));
            var activeQuest = TestSceneFactory.CreateScriptableObject<QuestData>();
            activeQuest.questId = "q_active";
            activeQuest.questName = "进行中的任务";
            activeQuest.objectives = new QuestObjective[0];
            Assert.That(questManager.AcceptQuest(activeQuest), Is.True);

            questManager.LoadCompletedQuests(
                new[] { "q_main_01", "", "q_main_01", null, "q_side_02" });

            CollectionAssert.AreEqual(
                new[] { "q_main_01", "q_side_02" },
                questManager.GetCompletedQuests());

            questManager.ResetForNewGame();
            Assert.That(questManager.ActiveQuests, Is.Empty);
            Assert.That(questManager.CompletedQuestIds, Is.Empty);
        }

        [Test]
        public void VersionTwoSaveRestoresBaseStatsEquipmentResourcesAndPosition()
        {
            var gameManager = TestSceneFactory.AddComponentWithAwake<GameManager>(
                TestSceneFactory.Create("GameManager"));
            gameManager.BeginSceneEntry(GameManager.SceneEntryMode.LoadGame);
            var inventory = TestSceneFactory.AddComponentWithAwake<InventoryManager>(
                TestSceneFactory.Create("InventoryManager"));
            TestSceneFactory.AddComponentWithAwake<QuestManager>(
                TestSceneFactory.Create("QuestManager"));
            var player = TestSceneFactory.CreatePlayer();
            TestSceneFactory.AddComponentWithAwake<MartialArtsSystem>(player);
            var saveManager = TestSceneFactory.AddComponentWithAwake<SaveManager>(
                TestSceneFactory.Create("SaveManager"));
            var data = new SaveManager.SaveData
            {
                saveVersion = 2,
                playerName = "凌霜",
                level = 3,
                exp = 25,
                currentHp = 120,
                currentMp = 60,
                baseAttack = 15,
                baseDefense = 5,
                baseAgility = 10,
                baseMaxHp = 100,
                baseMaxMp = 50,
                positionX = 3f,
                positionY = -2f,
                chapterIndex = 2,
                inventory = new InventoryManager.InventorySaveData
                {
                    slotItemIds = new[] { "herb_medicinal" },
                    slotAmounts = new[] { 2 },
                    equippedWeapon = "sword_frost",
                    equippedArmor = "armor_silk",
                    equippedAccessory = "",
                    gold = 77
                },
                martialArts = new MartialArtsSystem.MartialArtsSaveData
                {
                    learnedSkillIds = new[] { "basic_slash" },
                    equippedSkillIds = new[] { "basic_slash", "", "", "" }
                },
                completedQuests = new[] { "q_main_01" }
            };

            saveManager.ApplySaveDataToLoadedScene(data);

            var stats = player.GetComponent<CharacterStats>();
            Assert.That(stats.BaseAttack, Is.EqualTo(15));
            Assert.That(stats.attack, Is.EqualTo(37));
            Assert.That(stats.maxHp, Is.EqualTo(130));
            Assert.That(stats.maxMp, Is.EqualTo(70));
            Assert.That(stats.currentHp, Is.EqualTo(120));
            Assert.That(stats.currentMp, Is.EqualTo(60));
            Assert.That(player.transform.position, Is.EqualTo(new Vector3(3f, -2f, 0f)));
            Assert.That(inventory.Gold, Is.EqualTo(77));
            Assert.That(gameManager.chapterIndex, Is.EqualTo(2));
            Assert.That(
                gameManager.CurrentSceneEntryMode,
                Is.EqualTo(GameManager.SceneEntryMode.Active));
        }

        [Test]
        public void VersionTwoSaveDataSurvivesJsonRoundTrip()
        {
            var source = new SaveManager.SaveData
            {
                saveVersion = 2,
                baseAttack = 18,
                inventory = new InventoryManager.InventorySaveData
                {
                    slotItemIds = new[] { "food_mantou" },
                    slotAmounts = new[] { 5 },
                    gold = 66
                },
                martialArts = new MartialArtsSystem.MartialArtsSaveData
                {
                    learnedSkillIds = new[] { "basic_slash" },
                    equippedSkillIds = new[] { "basic_slash" }
                }
            };

            string json = JsonUtility.ToJson(source);
            var restored = JsonUtility.FromJson<SaveManager.SaveData>(json);

            Assert.That(restored.saveVersion, Is.EqualTo(2));
            Assert.That(restored.baseAttack, Is.EqualTo(18));
            Assert.That(restored.inventory.gold, Is.EqualTo(66));
            Assert.That(restored.martialArts.learnedSkillIds, Is.EqualTo(new[] { "basic_slash" }));
        }

        [Test]
        public void LegacySaveWithInventoryMigratesEquipmentOutOfTotalStats()
        {
            var gameManager = TestSceneFactory.AddComponentWithAwake<GameManager>(
                TestSceneFactory.Create("GameManager"));
            gameManager.BeginSceneEntry(GameManager.SceneEntryMode.LoadGame);
            TestSceneFactory.AddComponentWithAwake<InventoryManager>(
                TestSceneFactory.Create("InventoryManager"));
            var player = TestSceneFactory.CreatePlayer();
            var saveManager = TestSceneFactory.AddComponentWithAwake<SaveManager>(
                TestSceneFactory.Create("SaveManager"));
            var legacyData = new SaveManager.SaveData
            {
                attack = 20,
                defense = 5,
                agility = 10,
                maxHp = 100,
                maxMp = 50,
                currentHp = 40,
                currentMp = 20,
                inventory = new InventoryManager.InventorySaveData
                {
                    slotItemIds = new string[0],
                    slotAmounts = new int[0],
                    equippedWeapon = "sword_iron",
                    equippedArmor = "",
                    equippedAccessory = "",
                    gold = 50
                }
            };

            saveManager.ApplySaveDataToLoadedScene(legacyData);

            var stats = player.GetComponent<CharacterStats>();
            Assert.That(stats.BaseAttack, Is.EqualTo(15));
            Assert.That(stats.attack, Is.EqualTo(20));
            Assert.That(stats.currentHp, Is.EqualTo(40));
        }

        [Test]
        public void VersionFourSaveMigratesToFixedMaleHeroWithSwordStyle()
        {
            var gameManager = TestSceneFactory.AddComponentWithAwake<GameManager>(
                TestSceneFactory.Create("GameManager"));
            gameManager.BeginSceneEntry(GameManager.SceneEntryMode.LoadGame);
            var player = TestSceneFactory.CreatePlayer();
            TestSceneFactory.AddComponentWithAwake<PlayerCombat>(player);
            var saveManager = TestSceneFactory.AddComponentWithAwake<SaveManager>(
                TestSceneFactory.Create("SaveManager"));

            saveManager.ApplySaveDataToLoadedScene(new SaveManager.SaveData
            {
                saveVersion = 4,
                playerName = "凌霜",
                // v4 曾保存过的女性外观必须迁移为固定男主（docs/15）。
                playerArtId = "player_female_swordsman",
                level = 1,
                currentHp = 100,
                currentMp = 50,
                baseAttack = 15,
                baseDefense = 5,
                baseAgility = 10,
                baseMaxHp = 100,
                baseMaxMp = 50,
                chapterIndex = 1
            });

            Assert.That(gameManager.PlayerAppearance.ArtId, Is.EqualTo("player_male_swordsman"));
            Assert.That(player.GetComponent<YuanHaiLu.Art.CharacterVisual>().ArtId,
                Is.EqualTo("player_male_swordsman"));
            Assert.That(gameManager.WeaponStyleId, Is.EqualTo("sword"));
            Assert.That(player.GetComponent<PlayerCombat>().WeaponStyleId, Is.EqualTo("sword"));
        }

        [Test]
        public void OlderSaveMigratesToFixedMaleHeroWithSwordStyle()
        {
            var gameManager = TestSceneFactory.AddComponentWithAwake<GameManager>(
                TestSceneFactory.Create("GameManager"));
            gameManager.SetPlayerAppearance("player_male_mystic");
            gameManager.SetWeaponStyle("dart");
            gameManager.BeginSceneEntry(GameManager.SceneEntryMode.LoadGame);
            var player = TestSceneFactory.CreatePlayer();
            TestSceneFactory.AddComponentWithAwake<PlayerCombat>(player);
            var saveManager = TestSceneFactory.AddComponentWithAwake<SaveManager>(
                TestSceneFactory.Create("SaveManager"));

            saveManager.ApplySaveDataToLoadedScene(new SaveManager.SaveData
            {
                saveVersion = 3,
                playerName = "旧档",
                level = 1,
                currentHp = 80,
                currentMp = 30,
                baseAttack = 15,
                baseDefense = 5,
                baseAgility = 10,
                baseMaxHp = 100,
                baseMaxMp = 50,
                chapterIndex = 1
            });

            Assert.That(gameManager.PlayerAppearance, Is.EqualTo(PlayerAppearance.Default));
            Assert.That(player.GetComponent<YuanHaiLu.Art.CharacterVisual>().ArtId,
                Is.EqualTo(PlayerAppearance.Default.ArtId));
            Assert.That(gameManager.WeaponStyleId, Is.EqualTo("sword"));
            Assert.That(player.GetComponent<PlayerCombat>().WeaponStyleId, Is.EqualTo("sword"));
        }

        [Test]
        public void VersionFiveSaveRestoresWeaponStyleAndIllegalStyleFallsBackToSword()
        {
            var gameManager = TestSceneFactory.AddComponentWithAwake<GameManager>(
                TestSceneFactory.Create("GameManager"));
            gameManager.BeginSceneEntry(GameManager.SceneEntryMode.LoadGame);
            var player = TestSceneFactory.CreatePlayer();
            TestSceneFactory.AddComponentWithAwake<PlayerCombat>(player);
            var saveManager = TestSceneFactory.AddComponentWithAwake<SaveManager>(
                TestSceneFactory.Create("SaveManager"));

            saveManager.ApplySaveDataToLoadedScene(new SaveManager.SaveData
            {
                saveVersion = 5,
                playerName = "凌霜",
                playerArtId = "player_male_swordsman",
                weaponStyleId = "dart",
                level = 2,
                currentHp = 90,
                currentMp = 40,
                baseAttack = 15,
                baseDefense = 5,
                baseAgility = 10,
                baseMaxHp = 100,
                baseMaxMp = 50,
                chapterIndex = 1
            });

            Assert.That(gameManager.WeaponStyleId, Is.EqualTo("dart"));
            Assert.That(player.GetComponent<PlayerCombat>().WeaponStyleId, Is.EqualTo("dart"));

            saveManager.ApplySaveDataToLoadedScene(new SaveManager.SaveData
            {
                saveVersion = 5,
                playerName = "凌霜",
                weaponStyleId = "railgun",
                level = 2,
                currentHp = 90,
                currentMp = 40,
                baseAttack = 15,
                baseDefense = 5,
                baseAgility = 10,
                baseMaxHp = 100,
                baseMaxMp = 50,
                chapterIndex = 1
            });

            Assert.That(gameManager.WeaponStyleId, Is.EqualTo("sword"));
            Assert.That(player.GetComponent<PlayerCombat>().WeaponStyleId, Is.EqualTo("sword"));
        }

        [Test]
        public void VersionFiveSaveDataSurvivesJsonRoundTrip()
        {
            var source = new SaveManager.SaveData
            {
                saveVersion = 5,
                playerArtId = "player_male_swordsman",
                weaponStyleId = "gauntlets",
                baseAttack = 18
            };

            string json = JsonUtility.ToJson(source);
            var restored = JsonUtility.FromJson<SaveManager.SaveData>(json);

            Assert.That(restored.saveVersion, Is.EqualTo(5));
            Assert.That(restored.playerArtId, Is.EqualTo("player_male_swordsman"));
            Assert.That(restored.weaponStyleId, Is.EqualTo("gauntlets"));
            Assert.That(restored.baseAttack, Is.EqualTo(18));
        }
    }
}
