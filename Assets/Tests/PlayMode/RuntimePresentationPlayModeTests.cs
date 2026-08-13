using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using YuanHaiLu.Art;
using YuanHaiLu.Character;
using YuanHaiLu.GameSystem;

namespace YuanHaiLu.Tests.PlayMode
{
    public class RuntimePresentationPlayModeTests
    {
        [UnityTest]
        public IEnumerator PlayerCombatWaitsSafelyWhenGameManagerIsNotBootstrapped()
        {
            var player = new GameObject("UnbootstrappedPlayer");
            player.AddComponent<SpriteRenderer>();
            player.AddComponent<Animator>();
            player.AddComponent<Rigidbody2D>();
            player.AddComponent<PlayerController>();
            player.AddComponent<CharacterStats>();
            player.AddComponent<PlayerCombat>();

            yield return null;

            Assert.That(player.GetComponent<PlayerCombat>(), Is.Not.Null);
            Object.Destroy(player);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerControllerWithoutAnimatorControllerDoesNotEmitWarnings()
        {
            int warningCount = 0;
            Application.LogCallback captureAnimatorWarning = (condition, _, type) =>
            {
                if (type == LogType.Warning &&
                    condition.Contains("Animator is not playing an AnimatorController"))
                {
                    warningCount++;
                }
            };

            Application.logMessageReceived += captureAnimatorWarning;

            var player = new GameObject("Player");
            player.AddComponent<SpriteRenderer>();
            player.AddComponent<Animator>();
            player.AddComponent<Rigidbody2D>();
            player.AddComponent<PlayerController>();

            yield return null;

            Application.logMessageReceived -= captureAnimatorWarning;
            Object.Destroy(player);
            yield return null;

            Assert.That(
                warningCount,
                Is.Zero,
                "PlayerController must tolerate placeholder Animators without a controller.");
        }

        [UnityTest]
        public IEnumerator EnvironmentBindsWhenGameTimeManagerStartsAfterItsSceneController()
        {
            var root = new GameObject("DelayedTimeEnvironment");
            var landmark = new GameObject("Landmark");
            landmark.transform.SetParent(root.transform);
            var renderer = landmark.AddComponent<SpriteRenderer>();
            var effects = new GameObject("Effects");
            effects.transform.SetParent(root.transform);
            var effectsTilemap = effects.AddComponent<Tilemap>();
            effects.AddComponent<TilemapRenderer>();
            var environment = root.AddComponent<RegionEnvironmentController>();
            environment.ConfigureForEditor(true, "clear", effectsTilemap);

            yield return null;

            var timeObject = new GameObject("DelayedGameTimeManager");
            var time = timeObject.AddComponent<GameTimeManager>();
            time.SetTime(22, 0);
            yield return null;

            Assert.That(renderer.color,
                Is.EqualTo(new Color(0.48f, 0.56f, 0.78f, 1f)));

            Object.Destroy(root);
            Object.Destroy(timeObject);
            yield return null;
        }
    }
}
