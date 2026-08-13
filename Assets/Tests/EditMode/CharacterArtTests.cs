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
            var catalog = CharacterArtCatalog.LoadDefault();

            Assert.That(catalog.Entries.Count, Is.EqualTo(97));
            Assert.That(catalog.Entries.Select(entry => entry.Id).Distinct().Count(), Is.EqualTo(97));
            Assert.That(catalog.Entries.Count(entry => entry.Category == "player"), Is.EqualTo(12));
            Assert.That(catalog.Entries.Count(entry => entry.Category == "named"), Is.EqualTo(15));
            Assert.That(catalog.Entries.Count(entry => entry.Category == "npc"), Is.EqualTo(36));
            Assert.That(catalog.Entries.Count(entry => entry.Category == "enemy"), Is.EqualTo(24));
            Assert.That(catalog.Entries.Count(entry => entry.Category == "boss"), Is.EqualTo(10));
        }

        [Test]
        public void EveryFormalCharacterHasSheetControllerAndPrefab()
        {
            var catalog = CharacterArtCatalog.LoadDefault();

            foreach (var entry in catalog.Entries)
            {
                Assert.That(entry.Sheet, Is.Not.Null, entry.Id);
                Assert.That(entry.Controller, Is.Not.Null, entry.Id);
                Assert.That(entry.Prefab, Is.Not.Null, entry.Id);
                Assert.That(AssetDatabase.Contains(entry.Sheet), Is.True, entry.Id);
                Assert.That(AssetDatabase.Contains(entry.Controller), Is.True, entry.Id);
                Assert.That(AssetDatabase.Contains(entry.Prefab), Is.True, entry.Id);
            }
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

        [Test]
        public void CharacterShowcaseWindowExposesFixedActionAndScaleVocabulary()
        {
            // 动作词表必须与正式角色 Animator 状态前缀一致（attack_1/skill_1 带下划线）。
            Assert.That(CharacterShowcaseWindow.SupportedActions, Is.EquivalentTo(new[]
            {
                "idle", "walk", "dash",
                "attack_1", "attack_2", "attack_3",
                "skill_1", "skill_2",
                "hurt", "death",
            }));
            Assert.That(CharacterShowcaseWindow.SupportedScales, Is.EquivalentTo(new[] { 1, 4, 8 }));
        }

        [Test]
        public void CharacterShowcaseWindowRejectsUnknownActionsAndScales()
        {
            var window = ScriptableObject.CreateInstance<CharacterShowcaseWindow>();
            try
            {
                Assert.Multiple(() =>
                {
                    Assert.That(() => window.PreviewAction("fly"), Throws.ArgumentException);
                    Assert.That(() => window.PreviewAction(string.Empty), Throws.ArgumentException);
                    Assert.That(() => window.SetPreviewScale(2), Throws.ArgumentException);
                    Assert.That(() => window.SetPreviewScale(0), Throws.ArgumentException);
                });

                // 已知值不得抛异常；未绑定 Animator 时为安全 no-op。
                Assert.That(() => window.PreviewAction("attack_1"), Throws.Nothing);
                Assert.That(() => window.SetPreviewScale(8), Throws.Nothing);
                Assert.That(window.PreviewScale, Is.EqualTo(8));
                Assert.That(window.CurrentAction, Is.EqualTo("attack_1"));
            }
            finally
            {
                Object.DestroyImmediate(window);
            }
        }
    }
}
