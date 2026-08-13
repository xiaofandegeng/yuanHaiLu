using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using YuanHaiLu.Art;
using YuanHaiLu.Character;
using YuanHaiLu.Core;

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
                "cangyue_fallen_monk",
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
                    animator.SetFloat("MoveX", 0f);
                    animator.SetFloat("MoveY", -1f);
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
        public IEnumerator FormalPlayerAttackAnimationInvokesTheDamageHitFrame()
        {
            var gameManagerObject = new GameObject("AnimationTestGameManager");
            var player = new GameObject("AnimationTestPlayer");
            var enemy = new GameObject("AnimationTestEnemy");
            try
            {
                var gameManager = gameManagerObject.AddComponent<GameManager>();
                gameManager.SetState(GameManager.GameState.Exploration);

                player.tag = "Player";
                player.layer = LayerMask.NameToLayer("Player");
                player.AddComponent<SpriteRenderer>();
                var animator = player.AddComponent<Animator>();
                Assert.That(
                    CharacterArtCatalog.LoadDefault().TryGet(
                        "player_male_swordsman",
                        out var playerArt),
                    Is.True);
                animator.runtimeAnimatorController = playerArt.Controller;
                player.AddComponent<Rigidbody2D>();
                player.AddComponent<PlayerController>();
                player.AddComponent<CharacterStats>();
                player.AddComponent<PlayerCombat>();

                enemy.layer = LayerMask.NameToLayer("Enemy");
                enemy.transform.position = Vector3.down * 1.2f;
                enemy.AddComponent<CircleCollider2D>();
                var enemyStats = enemy.AddComponent<CharacterStats>();
                int hpBefore = enemyStats.currentHp;

                player.SendMessage("StartAttack", SendMessageOptions.RequireReceiver);
                yield return new WaitForSeconds(0.75f);

                Assert.That(enemyStats.currentHp, Is.LessThan(hpBefore),
                    "The formal attack clip must be reachable and invoke OnAttackHitFrame.");
            }
            finally
            {
                Object.Destroy(player);
                Object.Destroy(enemy);
                Object.Destroy(gameManagerObject);
            }
            yield return null;
        }
    }
}
