using NUnit.Framework;
using UnityEngine;
using YuanHaiLu.Core;
using YuanHaiLu.Dialogue;
using YuanHaiLu.GameSystem;

namespace YuanHaiLu.Tests.EditMode
{
    public class GlobalSystemsBootstrapperTests
    {
        [TearDown]
        public void TearDown()
        {
            TestSceneFactory.DestroyAll();
        }

        [Test]
        public void EnsureRequiredSystemsCreatesOneOfEveryPersistentManager()
        {
            var root = TestSceneFactory.Create("GameManager");
            var gameManager = root.AddComponent<GameManager>();

            GlobalSystemsBootstrapper.EnsureRequiredSystems(gameManager);
            GlobalSystemsBootstrapper.EnsureRequiredSystems(gameManager);

            Assert.That(
                Object.FindObjectsByType<SaveManager>(FindObjectsSortMode.None),
                Has.Length.EqualTo(1));
            Assert.That(
                Object.FindObjectsByType<InventoryManager>(FindObjectsSortMode.None),
                Has.Length.EqualTo(1));
            Assert.That(
                Object.FindObjectsByType<QuestManager>(FindObjectsSortMode.None),
                Has.Length.EqualTo(1));
            Assert.That(
                Object.FindObjectsByType<GameTimeManager>(FindObjectsSortMode.None),
                Has.Length.EqualTo(1));
            Assert.That(
                Object.FindObjectsByType<DialogueManager>(FindObjectsSortMode.None),
                Has.Length.EqualTo(1));
        }
    }
}
