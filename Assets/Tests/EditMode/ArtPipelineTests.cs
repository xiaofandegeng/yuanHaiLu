using System;
using NUnit.Framework;
using UnityEngine;
using YuanHaiLu.Art;

namespace YuanHaiLu.Tests.EditMode
{
    public class ArtPipelineTests
    {
        private CharacterArtCatalog characterCatalog;
        private EnvironmentArtCatalog environmentCatalog;

        [TearDown]
        public void TearDown()
        {
            if (characterCatalog != null)
                UnityEngine.Object.DestroyImmediate(characterCatalog);
            if (environmentCatalog != null)
                UnityEngine.Object.DestroyImmediate(environmentCatalog);
        }

        [Test]
        public void CharacterCatalogRejectsDuplicateStableIds()
        {
            characterCatalog = ScriptableObject.CreateInstance<CharacterArtCatalog>();
            characterCatalog.SetEntriesForEditor(new[]
            {
                CharacterArtEntry.ForTest("player_male_swordsman"),
                CharacterArtEntry.ForTest("player_male_swordsman")
            });

            Assert.That(() => characterCatalog.RebuildLookup(), Throws.InvalidOperationException);
        }

        [Test]
        public void CharacterCatalogRejectsMalformedIds()
        {
            characterCatalog = ScriptableObject.CreateInstance<CharacterArtCatalog>();
            characterCatalog.SetEntriesForEditor(new[]
            {
                CharacterArtEntry.ForTest("Player Swordsman")
            });

            Assert.That(() => characterCatalog.RebuildLookup(), Throws.InvalidOperationException);
        }

        [Test]
        public void CharacterCatalogLooksUpAValidatedEntry()
        {
            characterCatalog = ScriptableObject.CreateInstance<CharacterArtCatalog>();
            var entry = CharacterArtEntry.ForTest("player_female_swordsman");
            characterCatalog.SetEntriesForEditor(new[] { entry });

            characterCatalog.RebuildLookup();

            Assert.That(characterCatalog.TryGet("player_female_swordsman", out var result), Is.True);
            Assert.That(result, Is.SameAs(entry));
        }

        [Test]
        public void CharacterCatalogRejectsMissingFormalSheet()
        {
            characterCatalog = ScriptableObject.CreateInstance<CharacterArtCatalog>();
            characterCatalog.SetEntriesForEditor(new[]
            {
                CharacterArtEntry.Create(
                    "player_male_swordsman",
                    ArtAssetId.CharacterCategory.Player,
                    null,
                    null,
                    null,
                    Texture2D.whiteTexture)
            });

            Assert.That(() => characterCatalog.RebuildLookup(), Throws.InvalidOperationException);
        }

        [Test]
        public void EnvironmentCatalogRejectsDuplicateRegionIds()
        {
            environmentCatalog = ScriptableObject.CreateInstance<EnvironmentArtCatalog>();
            environmentCatalog.SetEntriesForEditor(new[]
            {
                EnvironmentArtEntry.ForTest("yanliu"),
                EnvironmentArtEntry.ForTest("yanliu")
            });

            Assert.That(() => environmentCatalog.RebuildLookup(), Throws.InvalidOperationException);
        }

        [Test]
        public void StableArtIdsUseTheSharedSnakeCaseContract()
        {
            Assert.That(ArtAssetId.IsValid("boss_cangyue_01"), Is.True);
            Assert.That(ArtAssetId.IsValid("Boss-CangYue"), Is.False);
            Assert.That(ArtAssetId.IsValid(string.Empty), Is.False);
        }
    }
}
