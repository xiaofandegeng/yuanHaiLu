using System.Reflection;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using YuanHaiLu.Art;
using YuanHaiLu.Editor;

namespace YuanHaiLu.Tests.EditMode
{
    public class CharacterArtTests
    {
        [OneTimeSetUp]
        public void RebuildFormalCharacterAssets()
        {
            CharacterAnimationBuilder.RebuildAll();
        }

        [Test]
        public void FormalCharacterCatalogContainsExactlyNinetySevenEntries()
        {
            var catalog = CharacterArtCatalog.LoadDefault();

            Assert.That(catalog.Entries.Count, Is.EqualTo(97));
            Assert.That(catalog.Entries.Select(entry => entry.Id).Distinct().Count(), Is.EqualTo(97));
            Assert.That(catalog.Entries.Count(entry => entry.Category == "player"), Is.EqualTo(12));
            Assert.That(catalog.Entries.Count(entry => entry.Category == "named"), Is.EqualTo(15));
            Assert.That(catalog.Entries.Count(entry => entry.Category == "npc"), Is.EqualTo(36));
            Assert.That(catalog.Entries.Count(entry => entry.Category == "enemy"), Is.EqualTo(24));
            Assert.That(catalog.Entries.Count(entry => entry.Category == "boss"), Is.EqualTo(10));
        }

        [TestCase("player_male_swordsman")]
        [TestCase("player_female_mystic")]
        [TestCase("shen_ruolan")]
        [TestCase("yanliu_merchant_01")]
        [TestCase("cangyue_cliff_wolf")]
        [TestCase("hanyuan_snow_beast")]
        public void FormalCharacterHasSheetControllerAndPrefab(string id)
        {
            var catalog = CharacterArtCatalog.LoadDefault();

            Assert.That(catalog.TryGet(id, out var entry), Is.True);
            Assert.That(entry.Sheet, Is.Not.Null);
            Assert.That(entry.Controller, Is.Not.Null);
            Assert.That(entry.Prefab, Is.Not.Null);
            Assert.That(AssetDatabase.Contains(entry.Sheet), Is.True);
            Assert.That(AssetDatabase.Contains(entry.Controller), Is.True);
            Assert.That(AssetDatabase.Contains(entry.Prefab), Is.True);
        }

        [Test]
        public void FixedMaleHeroCanBeRebuiltIndependentlyAtFortyEightPixels()
        {
            var rebuildOnly = typeof(CharacterAnimationBuilder).GetMethod(
                "RebuildOnly",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(rebuildOnly, Is.Not.Null,
                "The MVP hero must rebuild without regenerating the full character roster.");

            rebuildOnly.Invoke(null, new object[] { "player_male_swordsman" });
            var sprites = AssetDatabase.LoadAllAssetsAtPath(
                    "Assets/Art/Characters/Player/player_male_swordsman.png")
                .OfType<Sprite>()
                .ToArray();

            Assert.That(sprites.Length, Is.GreaterThan(0));
            Assert.That(sprites.All(sprite => sprite.rect.width == 48f && sprite.rect.height == 48f), Is.True);
            Assert.That(CharacterArtCatalog.LoadDefault().TryGet("player_male_swordsman", out var entry), Is.True);
            Assert.That(entry.Prefab, Is.Not.Null);
            Assert.That(entry.Controller, Is.Not.Null);
        }

        [Test]
        public void CharacterVisualRejectsUnknownFormalIdWithoutCreatingPixels()
        {
            var target = new GameObject("UnknownVisual");
            try
            {
                var visual = target.AddComponent<CharacterVisual>();
                Assert.That(() => visual.Apply("missing_actor"), Throws.InvalidOperationException);
                Assert.That(target.GetComponent<SpriteRenderer>(), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void CharacterVisualCreatesMissingRendererAndAnimatorForKnownFormalId()
        {
            var target = new GameObject("FreshVisual");
            try
            {
                var visual = CharacterVisual.ApplyTo(target, "innkeeper_zhao");

                Assert.That(visual.ArtId, Is.EqualTo("innkeeper_zhao"));
                Assert.That(target.GetComponent<SpriteRenderer>()?.sprite, Is.Not.Null);
                Assert.That(target.GetComponent<Animator>()?.runtimeAnimatorController, Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        [TestCase("attack1", "attack_1_left")]
        [TestCase("skill1", "skill_1_left")]
        [TestCase("death", "death_left")]
        public void ShowcaseActionMapsStableApiToAnimatorState(string actionId, string stateName)
        {
            var window = ScriptableObject.CreateInstance<CharacterShowcaseWindow>();
            try
            {
                Assert.That(window.AnimatorStateFor(actionId, 1), Is.EqualTo(stateName));
                Assert.That(() => window.AnimatorStateFor(actionId, 4),
                    Throws.TypeOf<System.ArgumentOutOfRangeException>());
                Assert.That(() => window.AnimatorStateFor("unknown", 0),
                    Throws.TypeOf<System.ArgumentOutOfRangeException>());
            }
            finally
            {
                Object.DestroyImmediate(window);
            }
        }

        [TestCase("player_female_swordsman")]
        [TestCase("cangyue_cliff_wolf")]
        [TestCase("hanyuan_snow_beast")]
        public void FormalControllerHasReachableDirectionalMovementAndAttackStates(string id)
        {
            var catalog = CharacterArtCatalog.LoadDefault();
            Assert.That(catalog.TryGet(id, out var entry), Is.True);
            var controller = entry.Controller as AnimatorController;
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.parameters.Any(parameter => parameter.name == "Facing"), Is.True);
            var states = controller.layers[0].stateMachine.states
                .Select(child => child.state.name)
                .ToArray();
            foreach (var direction in new[] { "down", "left", "right", "up" })
            {
                Assert.That(states, Does.Contain("idle_" + direction));
                Assert.That(states, Does.Contain("walk_" + direction));
                Assert.That(states, Does.Contain("attack_1_" + direction));
            }
            Assert.That(controller.layers[0].stateMachine.anyStateTransitions.Any(transition =>
                transition.conditions.Any(condition => condition.parameter == "IsAttacking")), Is.True);
        }

        [Test]
        public void CivilianControllerStillProvidesFourDirectionalIdleAndWalkStates()
        {
            var catalog = CharacterArtCatalog.LoadDefault();
            Assert.That(catalog.TryGet("yanliu_merchant_01", out var entry), Is.True);
            var controller = entry.Controller as AnimatorController;
            var states = controller.layers[0].stateMachine.states
                .Select(child => child.state.name)
                .ToArray();
            foreach (var direction in new[] { "down", "left", "right", "up" })
            {
                Assert.That(states, Does.Contain("idle_" + direction));
                Assert.That(states, Does.Contain("walk_" + direction));
            }
        }

        [Test]
        public void OnlyPlayerAttackClipsContainPlayerCombatAnimationEvents()
        {
            var catalog = CharacterArtCatalog.LoadDefault();
            Assert.That(catalog.TryGet("player_female_swordsman", out var player), Is.True);
            Assert.That(catalog.TryGet("cangyue_cliff_wolf", out var enemy), Is.True);

            var playerEvents = AnimationUtility.GetAnimationEvents(AttackClip(player.Controller, "attack_1_down"));
            var enemyEvents = AnimationUtility.GetAnimationEvents(AttackClip(enemy.Controller, "attack_1_down"));

            Assert.That(playerEvents.Select(animationEvent => animationEvent.functionName),
                Does.Contain("OnAttackHitFrame"));
            Assert.That(enemyEvents.Select(animationEvent => animationEvent.functionName),
                Has.None.EqualTo("OnAttackHitFrame"));
            Assert.That(enemyEvents.Select(animationEvent => animationEvent.functionName),
                Has.None.EqualTo("OnAttackAnimationEnd"));
        }

        private static AnimationClip AttackClip(RuntimeAnimatorController controller, string stateName)
        {
            var animatorController = controller as AnimatorController;
            Assert.That(animatorController, Is.Not.Null);
            var state = animatorController.layers[0].stateMachine.states
                .Single(child => child.state.name == stateName)
                .state;
            Assert.That(state.motion, Is.TypeOf<AnimationClip>());
            return state.motion as AnimationClip;
        }
    }
}
