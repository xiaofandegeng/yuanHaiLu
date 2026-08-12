using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using YuanHaiLu.Character;

namespace YuanHaiLu.Tests.PlayMode
{
    public class RuntimePresentationPlayModeTests
    {
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
    }
}
