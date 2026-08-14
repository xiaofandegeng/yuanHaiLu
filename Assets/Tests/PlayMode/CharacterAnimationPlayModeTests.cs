using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using YuanHaiLu.Art;
using YuanHaiLu.Character;

namespace YuanHaiLu.Tests.PlayMode
{
    public class CharacterAnimationPlayModeTests
    {
        [UnityTest]
        public IEnumerator RepresentativeFormalPrefabsTransitionFromIdleToWalk()
        {
            var catalog = CharacterArtCatalog.LoadDefault();
            var ids = new[]
            {
                "player_female_mystic",
                "yanliu_merchant_01",
                "cangyue_cliff_wolf",
                "hanyuan_snow_beast"
            };

            foreach (var id in ids)
            {
                Assert.That(catalog.TryGet(id, out var entry), Is.True, id);
                var instance = Object.Instantiate(entry.Prefab);
                try
                {
                    var animator = instance.GetComponent<Animator>();
                    Assert.That(animator, Is.Not.Null, id);
                    Assert.That(animator.runtimeAnimatorController, Is.Not.Null, id);
                    animator.Play("idle_down", 0, 0f);
                    animator.Update(0f);
                    Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("idle_down"), Is.True, id);
                    animator.SetFloat("Speed", 1f);
                    animator.Update(0.1f);
                    yield return null;
                    Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("walk_down"), Is.True, id);
                    Assert.That(instance.GetComponent<CharacterVisual>().ArtId, Is.EqualTo(id));
                }
                finally
                {
                    Object.Destroy(instance);
                }
            }
        }

        [UnityTest]
        public IEnumerator FormalPlayerAttackTransitionsIntoThreeDirectionalAttackStates()
        {
            var catalog = CharacterArtCatalog.LoadDefault();
            Assert.That(catalog.TryGet("player_female_swordsman", out var entry), Is.True);
            var instance = Object.Instantiate(entry.Prefab);
            try
            {
                instance.AddComponent<Rigidbody2D>();
                instance.AddComponent<PlayerController>();
                instance.AddComponent<CharacterStats>();
                instance.AddComponent<PlayerCombat>();
                var animator = instance.GetComponent<Animator>();
                animator.Play("idle_down", 0, 0f);
                animator.Update(0f);
                for (var attackIndex = 0; attackIndex < 3; attackIndex++)
                {
                    animator.SetInteger("Facing", 0);
                    animator.SetInteger("AttackIndex", attackIndex);
                    animator.SetBool("IsAttacking", true);
                    animator.Update(0.05f);
                    yield return null;
                    Assert.That(animator.GetCurrentAnimatorStateInfo(0)
                        .IsName("attack_" + (attackIndex + 1) + "_down"), Is.True);
                    animator.SetBool("IsAttacking", false);
                    animator.Update(0.05f);
                }
            }
            finally
            {
                Object.Destroy(instance);
            }
        }
    }
}
