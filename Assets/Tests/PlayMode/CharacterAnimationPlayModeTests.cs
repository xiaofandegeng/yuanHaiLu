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

        [UnityTest]
        public IEnumerator ShowcaseActionVocabularyMapsToRealAnimatorStates()
        {
            // 这组动作与 CharacterShowcaseWindow.SupportedActions 对齐（带下划线，
            // 与 Animator 状态命名 attack_1_down / skill_1_down 等一致）。
            var showcaseActions = new[]
            {
                "idle", "walk", "dash",
                "attack_1", "attack_2", "attack_3",
                "skill_1", "skill_2",
                "hurt", "death",
            };
            var catalog = CharacterArtCatalog.LoadDefault();
            Assert.That(catalog.TryGet("player_female_mystic", out var entry), Is.True);
            var instance = Object.Instantiate(entry.Prefab);
            try
            {
                var animator = instance.GetComponent<Animator>();
                Assert.That(animator, Is.Not.Null);
                Assert.That(animator.runtimeAnimatorController, Is.Not.Null);

                // player_female_mystic 声明了全部核心动作，因此总览词表里的每一项
                // 都必须映射到一个真实存在的 Animator 状态。
                foreach (var action in showcaseActions)
                {
                    string state = action + "_down";
                    Assert.That(
                        animator.HasState(0, Animator.StringToHash(state)),
                        Is.True,
                        "showcase action '" + action + "' must map to Animator state '" + state + "'.");
                }

                animator.Play("idle_down", 0, 0f);
                animator.Update(0f);
                yield return null;
                Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("idle_down"), Is.True);
            }
            finally
            {
                Object.Destroy(instance);
            }
        }
    }
}
