using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using YuanHaiLu.Art;
using YuanHaiLu.Core;
using YuanHaiLu.Editor;

namespace YuanHaiLu.Tests.EditMode
{
    public class ArtPipelineTests
    {
        private const string TestCharacterPath =
            "Assets/Art/Characters/Player/player_male_swordsman.png";
        private const string TestTilesetPath =
            "Assets/Art/Environment/Regions/yanliu/yanliu_tileset.png";

        private CharacterArtCatalog characterCatalog;
        private EnvironmentArtCatalog environmentCatalog;

        [TearDown]
        public void TearDown()
        {
            if (characterCatalog != null)
            {
                foreach (var entry in characterCatalog.Entries)
                {
                    if (entry?.Controller != null && !AssetDatabase.Contains(entry.Controller))
                        UnityEngine.Object.DestroyImmediate(entry.Controller);
                    if (entry?.Prefab != null && !AssetDatabase.Contains(entry.Prefab))
                        UnityEngine.Object.DestroyImmediate(entry.Prefab);
                }
                UnityEngine.Object.DestroyImmediate(characterCatalog);
            }
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
        public void CharacterCatalogRejectsMissingDerivedControllerOrPrefab()
        {
            characterCatalog = ScriptableObject.CreateInstance<CharacterArtCatalog>();
            characterCatalog.SetEntriesForEditor(new[]
            {
                CharacterArtEntry.Create(
                    "player_male_swordsman",
                    ArtAssetId.CharacterCategory.Player,
                    Texture2D.whiteTexture,
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

        [Test]
        public void FormalCharacterTextureUsesExactPixelImportSettings()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(TestCharacterPath);

            Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(16f));
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Multiple));
            Assert.That(
                AssetDatabase.LoadAllAssetsAtPath(TestCharacterPath).OfType<Sprite>().Count(),
                Is.EqualTo(244));
        }

        [Test]
        public void FormalTilesetUsesMetadataSpriteNames()
        {
            var names = AssetDatabase.LoadAllAssetsAtPath(TestTilesetPath)
                .OfType<Sprite>()
                .Select(sprite => sprite.name)
                .ToArray();

            Assert.That(names, Does.Contain("yanliu__ground__0"));
            Assert.That(names, Does.Contain("yanliu__water__0"));
            Assert.That(names, Does.Contain("yanliu__bridge__0"));
        }

        [Test]
        public void FormalArtValidatorReportsNoReferenceSliceErrors()
        {
            var report = ArtAssetValidator.ValidateAll();

            Assert.That(report.Errors, Is.Empty, report.ToString());
        }

        [Test]
        public void UndeclaredLegacyTextureIsOutsideFormalMetadataContract()
        {
            const string legacyPath = "Assets/Art/Tilesets/yanliu_town_demo.png";
            Assert.That(System.IO.File.Exists(System.IO.Path.ChangeExtension(legacyPath, ".art.json")),
                Is.False);
        }

        [Test]
        public void GeneratedDefaultCatalogsResolveTheReferenceSlice()
        {
            var characters = AssetDatabase.LoadAssetAtPath<CharacterArtCatalog>(
                ArtCatalogBuilder.CharacterCatalogPath);
            var environments = AssetDatabase.LoadAssetAtPath<EnvironmentArtCatalog>(
                ArtCatalogBuilder.EnvironmentCatalogPath);

            Assert.That(characters, Is.Not.Null);
            Assert.That(environments, Is.Not.Null);
            Assert.That(characters.TryGet("player_male_swordsman", out var male), Is.True);
            Assert.That(male.Sheet, Is.Not.Null);
            Assert.That(environments.TryGet("yanliu", out var yanliu), Is.True);
            Assert.That(yanliu.Landmarks, Is.Not.Null);
            Assert.That(yanliu.Preview, Is.Not.Null);
        }

        [Test]
        public void ArtReferenceSceneContainsOnlyPersistentSpriteAssets()
        {
            var scene = EditorSceneManager.OpenScene(
                ArtReferencePreviewGenerator.ScenePath,
                OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                Assert.That(roots.Select(root => root.name), Does.Contain("ReferenceCharacters"));
                Assert.That(roots.Select(root => root.name), Does.Contain("YanliuEnvironment"));
                Assert.That(
                    roots.SelectMany(root => root.GetComponentsInChildren<GameManager>(true)),
                    Is.Empty);
                var renderers = roots.SelectMany(
                    root => root.GetComponentsInChildren<SpriteRenderer>(true)).ToArray();
                Assert.That(renderers.Length, Is.EqualTo(25));
                Assert.That(renderers.All(renderer => renderer.sprite != null), Is.True);
                Assert.That(
                    renderers.All(renderer => AssetDatabase.Contains(renderer.sprite)),
                    Is.True,
                    "Reference scene must not contain runtime-created placeholder sprites.");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
