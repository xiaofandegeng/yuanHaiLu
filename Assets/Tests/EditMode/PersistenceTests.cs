using NUnit.Framework;
using UnityEngine;
using YuanHaiLu.Character;
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
    }
}
