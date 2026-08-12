using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using YuanHaiLu.Core;
using YuanHaiLu.Dialogue;
using YuanHaiLu.GameSystem;
using YuanHaiLu.UI;

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

        [Test]
        public void MainMenuStartPutsPersistentGameManagerBackInMenuState()
        {
            var gameManager = TestSceneFactory.AddComponentWithAwake<GameManager>(
                TestSceneFactory.Create("GameManager"));
            gameManager.SetState(GameManager.GameState.Exploration);
            var menu = TestSceneFactory.Create("MainMenu").AddComponent<MainMenu>();

            InvokePrivate(menu, "Start");

            Assert.That(gameManager.currentState, Is.EqualTo(GameManager.GameState.MainMenu));
        }

        [Test]
        public void InvalidNewGameSceneDoesNotResetCurrentSession()
        {
            var gameManager = TestSceneFactory.AddComponentWithAwake<GameManager>(
                TestSceneFactory.Create("GameManager"));
            gameManager.playerName = "当前角色";
            gameManager.chapterIndex = 3;
            gameManager.SetState(GameManager.GameState.Combat);
            var inventory = TestSceneFactory.AddComponentWithAwake<InventoryManager>(
                TestSceneFactory.Create("InventoryManager"));
            inventory.AddGold(50);
            TestSceneFactory.AddComponentWithAwake<QuestManager>(
                TestSceneFactory.Create("QuestManager"));
            var menu = TestSceneFactory.Create("MainMenu").AddComponent<MainMenu>();
            SetPrivateField(menu, "firstSceneName", "Missing_Scene_For_Test");

            LogAssert.Expect(
                LogType.Error,
                "[MainMenu] 场景不在 Build Settings 中: Missing_Scene_For_Test");
            menu.OnNewGame();

            Assert.That(inventory.Gold, Is.EqualTo(150));
            Assert.That(gameManager.playerName, Is.EqualTo("当前角色"));
            Assert.That(gameManager.chapterIndex, Is.EqualTo(3));
            Assert.That(gameManager.currentState, Is.EqualTo(GameManager.GameState.Combat));
        }

        private static void InvokePrivate(object target, string methodName)
        {
            target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(target, null);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(target, value);
        }
    }
}
