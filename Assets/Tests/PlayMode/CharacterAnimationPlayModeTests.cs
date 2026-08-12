using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using YuanHaiLu.Art;

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
    }
}
