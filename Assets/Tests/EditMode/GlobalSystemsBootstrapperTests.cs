using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
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
                Object.FindObjectsByType<SaveManager>(),
                Has.Length.EqualTo(1));
            Assert.That(
                Object.FindObjectsByType<InventoryManager>(),
                Has.Length.EqualTo(1));
            Assert.That(
                Object.FindObjectsByType<QuestManager>(),
                Has.Length.EqualTo(1));
            Assert.That(
                Object.FindObjectsByType<GameTimeManager>(),
                Has.Length.EqualTo(1));
            Assert.That(
                Object.FindObjectsByType<DialogueManager>(),
                Has.Length.EqualTo(1));
        }

        [Test]
        public void GameTimeManagerInitializesPeriodFromItsConfiguredHour()
        {
            var manager = TestSceneFactory.AddComponentWithAwake<GameTimeManager>(
                TestSceneFactory.Create("GameTimeManager"));

            Assert.That(manager.hour, Is.EqualTo(8));
            Assert.That(manager.CurrentPeriod, Is.EqualTo(GameTimeManager.TimePeriod.Morning));
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
        public void MainMenuStartSelectsNewGameButtonForKeyboardNavigation()
        {
            TestSceneFactory.AddComponentWithAwake<GameManager>(
                TestSceneFactory.Create("GameManager"));
            var eventSystemObject = TestSceneFactory.Create("EventSystem");
            var eventSystem = eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
            InvokePrivate(eventSystem, "OnEnable");
            var menuObject = TestSceneFactory.Create("MainMenu");
            var menu = menuObject.AddComponent<MainMenu>();
            var buttonObject = TestSceneFactory.Create("Btn_新游戏");
            buttonObject.transform.SetParent(menuObject.transform);
            buttonObject.AddComponent<Image>();
            buttonObject.AddComponent<Button>();

            InvokePrivate(menu, "Start");

            Assert.That(eventSystem.currentSelectedGameObject, Is.SameAs(buttonObject));
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
            menu.ConfirmNewGame();

            Assert.That(inventory.Gold, Is.EqualTo(150));
            Assert.That(gameManager.playerName, Is.EqualTo("当前角色"));
            Assert.That(gameManager.chapterIndex, Is.EqualTo(3));
            Assert.That(gameManager.currentState, Is.EqualTo(GameManager.GameState.Combat));
        }

        [Test]
        public void MainMenuAppearanceSelectionIsPendingUntilConfirmation()
        {
            var gameManager = TestSceneFactory.AddComponentWithAwake<GameManager>(
                TestSceneFactory.Create("GameManager"));
            var menu = TestSceneFactory.Create("MainMenu").AddComponent<MainMenu>();

            menu.SelectAppearance("player_male_hidden_weapon");

            Assert.That(menu.SelectedAppearance.ArtId, Is.EqualTo("player_male_hidden_weapon"));
            Assert.That(gameManager.PlayerArtId, Is.EqualTo(PlayerAppearance.Default.ArtId));

            menu.CancelAppearanceSelection();

            Assert.That(menu.SelectedAppearance, Is.EqualTo(PlayerAppearance.Default));
            Assert.That(gameManager.PlayerArtId, Is.EqualTo(PlayerAppearance.Default.ArtId));
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
