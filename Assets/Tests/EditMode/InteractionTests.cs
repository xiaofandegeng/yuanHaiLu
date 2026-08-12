using NUnit.Framework;
using YuanHaiLu.Character;
using YuanHaiLu.Map;

namespace YuanHaiLu.Tests.EditMode
{
    public class InteractionTests
    {
        [TearDown]
        public void TearDown()
        {
            TestSceneFactory.DestroyAll();
        }

        [Test]
        public void EnsureOnAddsExactlyOneInteractionComponent()
        {
            var player = TestSceneFactory.CreatePlayer();

            var first = PlayerInteraction.EnsureOn(player);
            var second = PlayerInteraction.EnsureOn(player);

            Assert.That(second, Is.SameAs(first));
            Assert.That(player.GetComponents<PlayerInteraction>(), Has.Length.EqualTo(1));
        }

        [Test]
        public void OneShotInteractiveEventStopsBeingCandidateAfterTrigger()
        {
            var eventObject = TestSceneFactory.Create("Event");
            eventObject.AddComponent<UnityEngine.BoxCollider2D>();
            var trigger = eventObject.AddComponent<EventTrigger>();
            trigger.requireInteract = true;
            trigger.triggerOnce = true;
            trigger.hasTriggered = true;

            Assert.That(trigger.CanInteract(), Is.False);
        }
    }
}
