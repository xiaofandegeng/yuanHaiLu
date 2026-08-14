using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace YuanHaiLu.Editor
{
    public static class EnvironmentTileBuilder
    {
        public const string TileRoot = "Assets/Tilemaps/Formal";

        [MenuItem("Tools/渊海录/美术/重建正式环境Tile")]
        public static void RebuildAll()
        {
            ArtImportRules.ApplyAllFormal();
            EnsureFolder("Assets/Tilemaps");
            EnsureFolder(TileRoot);
            foreach (var metadataPath in ArtImportRules.EnumerateMetadataAssetPaths())
            {
                var metadata = ArtImportRules.ReadMetadataAtPath(metadataPath);
                if (!string.Equals(metadata.kind, "environment", StringComparison.Ordinal))
                    continue;
                var directory = Path.GetDirectoryName(metadataPath) ?? string.Empty;
                var texturePath = Path.Combine(directory, metadata.image).Replace('\\', '/');
                var sprites = AssetDatabase.LoadAllAssetsAtPath(texturePath).OfType<Sprite>();
                var rolesBySpriteName = metadata.sprites.ToDictionary(
                    sprite => sprite.name,
                    sprite => sprite.role,
                    StringComparer.Ordinal);
                var blockingRoles = new HashSet<string>(
                    metadata.blockingTileRoles,
                    StringComparer.Ordinal);
                EnsureFolder(TileRoot + "/" + metadata.id);
                foreach (var sprite in sprites)
                {
                    var path = TilePath(metadata.id, sprite.name);
                    var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
                    if (tile == null)
                    {
                        tile = ScriptableObject.CreateInstance<Tile>();
                        AssetDatabase.CreateAsset(tile, path);
                    }
                    tile.sprite = sprite;
                    if (!rolesBySpriteName.TryGetValue(sprite.name, out var role))
                        throw new InvalidOperationException(
                            $"'{metadata.id}' metadata is missing a role for tile sprite '{sprite.name}'.");
                    tile.colliderType = blockingRoles.Contains(role)
                        ? Tile.ColliderType.Grid
                        : Tile.ColliderType.None;
                    EditorUtility.SetDirty(tile);
                }
            }
            AssetDatabase.SaveAssets();
            ArtCatalogBuilder.RebuildAll();
        }

        public static IReadOnlyDictionary<string, Tile> LoadTiles(string id)
        {
            var result = new Dictionary<string, Tile>(StringComparer.Ordinal);
            foreach (var guid in AssetDatabase.FindAssets("t:Tile", new[] { TileRoot + "/" + id }))
            {
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(AssetDatabase.GUIDToAssetPath(guid));
                if (tile != null && tile.sprite != null)
                    result[tile.sprite.name] = tile;
            }
            return result;
        }

        private static string TilePath(string id, string spriteName)
        {
            return $"{TileRoot}/{id}/{spriteName}.asset";
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
