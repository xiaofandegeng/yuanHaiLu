using NUnit.Framework;
using YuanHaiLu.Core;
using YuanHaiLu.Map;

namespace YuanHaiLu.Tests.EditMode
{
    public class GameManagerEntryModeTests
    {
        [TearDown]
        public void TearDown()
        {
            TestSceneFactory.DestroyAll();
        }

        [Test]
        public void LoadGameEntrySkipsNewGameInitializationUntilCompleted()
        {
            var gameObject = TestSceneFactory.Create("GameManager");
            var manager = gameObject.AddComponent<GameManager>();

            manager.BeginSceneEntry(GameManager.SceneEntryMode.LoadGame);

            Assert.That(manager.ShouldInitializeNewGame, Is.False);
            manager.CompleteSceneEntry();
            Assert.That(
                manager.CurrentSceneEntryMode,
                Is.EqualTo(GameManager.SceneEntryMode.Active));
        }

        [Test]
        public void SceneDirectorCompletesDirectNewGameInExplorationState()
        {
            var gameObject = TestSceneFactory.Create("GameManager");
            var manager = gameObject.AddComponent<GameManager>();
            manager.SetState(GameManager.GameState.MainMenu);
            manager.BeginSceneEntry(GameManager.SceneEntryMode.NewGame);

            SceneDirector.CompleteNewGameInitialization(manager);

            Assert.That(manager.currentState, Is.EqualTo(GameManager.GameState.Exploration));
            Assert.That(
                manager.CurrentSceneEntryMode,
                Is.EqualTo(GameManager.SceneEntryMode.Active));
        }
    }
}
