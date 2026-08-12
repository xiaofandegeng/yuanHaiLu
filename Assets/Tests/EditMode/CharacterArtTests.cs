using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using YuanHaiLu.Art;
using YuanHaiLu.Editor;

namespace YuanHaiLu.Tests.EditMode
{
    public class CharacterArtTests
    {
        [Test]
        public void FormalCharacterCatalogContainsExactlyNinetySevenEntries()
        {
            CharacterAnimationBuilder.RebuildAll();
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
            CharacterAnimationBuilder.RebuildAll();
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
    }
}
