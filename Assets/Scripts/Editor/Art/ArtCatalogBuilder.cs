using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using YuanHaiLu.Art;

namespace YuanHaiLu.Editor
{
    public static class ArtCatalogBuilder
    {
        public const string CharacterCatalogPath = "Assets/Resources/Art/CharacterArtCatalog.asset";
        public const string EnvironmentCatalogPath = "Assets/Resources/Art/EnvironmentArtCatalog.asset";

        [MenuItem("Tools/渊海录/美术/重建正式美术目录")]
        public static void RebuildAll()
        {
            ArtImportRules.ApplyAllFormal();
            var report = ArtAssetValidator.ValidateAll();
            if (!report.IsValid)
                throw new InvalidOperationException(report.ToString());

            var characterEntries = new List<CharacterArtEntry>();
            var environmentEntries = new List<EnvironmentArtEntry>();
            foreach (var metadataPath in ArtImportRules.EnumerateMetadataAssetPaths())
            {
                var metadata = ArtImportRules.ReadMetadataAtPath(metadataPath);
                var directory = Path.GetDirectoryName(metadataPath) ?? string.Empty;
                if (string.Equals(metadata.kind, "character", StringComparison.Ordinal))
                {
                    var sheetPath = Path.Combine(directory, metadata.image).Replace('\\', '/');
                    var sheet = AssetDatabase.LoadAssetAtPath<Texture2D>(sheetPath);
                    characterEntries.Add(CharacterArtEntry.Create(
                        metadata.id,
                        InferCategory(metadata.id),
                        sheet,
                        null,
                        null,
                        sheet));
                }
                else if (string.Equals(metadata.kind, "environment", StringComparison.Ordinal))
                {
                    var tilesetPath = Path.Combine(directory, metadata.image).Replace('\\', '/');
                    var landmarkPath = Path.Combine(directory, metadata.landmarkImage).Replace('\\', '/');
                    var previewPath = Path.Combine(directory, metadata.id + "_reference.png").Replace('\\', '/');
                    environmentEntries.Add(EnvironmentArtEntry.Create(
                        metadata.id,
                        AssetDatabase.LoadAssetAtPath<Texture2D>(tilesetPath),
                        AssetDatabase.LoadAssetAtPath<Texture2D>(landmarkPath),
                        AssetDatabase.LoadAssetAtPath<Texture2D>(previewPath),
                        metadata.id + "_reference"));
                }
            }

            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/Art");
            var characterCatalog = LoadOrCreate<CharacterArtCatalog>(CharacterCatalogPath);
            characterCatalog.SetEntriesForEditor(characterEntries);
            characterCatalog.RebuildLookup();
            EditorUtility.SetDirty(characterCatalog);

            var environmentCatalog = LoadOrCreate<EnvironmentArtCatalog>(EnvironmentCatalogPath);
            environmentCatalog.SetEntriesForEditor(environmentEntries);
            environmentCatalog.RebuildLookup();
            EditorUtility.SetDirty(environmentCatalog);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ArtCatalogBuilder] characters={characterEntries.Count}, environments={environmentEntries.Count}");
        }

        public static void RebuildFromCommandLine()
        {
            RebuildAll();
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static string InferCategory(string id)
        {
            if (id.StartsWith("player_", StringComparison.Ordinal)) return ArtAssetId.CharacterCategory.Player;
            if (id.StartsWith("boss_", StringComparison.Ordinal)) return ArtAssetId.CharacterCategory.Boss;
            if (id.StartsWith("enemy_", StringComparison.Ordinal)) return ArtAssetId.CharacterCategory.Enemy;
            if (id.StartsWith("npc_", StringComparison.Ordinal)) return ArtAssetId.CharacterCategory.Npc;
            return ArtAssetId.CharacterCategory.Named;
        }
    }
}
