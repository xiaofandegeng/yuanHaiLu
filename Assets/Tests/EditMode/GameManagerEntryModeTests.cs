using NUnit.Framework;
using YuanHaiLu.Core;

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
    }
}
