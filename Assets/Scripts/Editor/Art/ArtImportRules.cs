using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace YuanHaiLu.Editor
{
    [Serializable]
    internal sealed class ArtSpriteMetadata
    {
        public string name;
        public int[] rect;
        public float[] pivot;
    }

    [Serializable]
    internal sealed class ArtMetadata
    {
        public int schemaVersion;
        public string kind;
        public string id;
        public string image;
        public string sha256;
        public string landmarkImage;
        public string landmarkSha256;
        public int frameSize;
        public int tileSize;
        public ArtSpriteMetadata[] sprites;
        public ArtSpriteMetadata[] landmarks;
    }

    /// <summary>
    /// Applies the exact pixel and slicing contract emitted by tools/art_pipeline.
    /// </summary>
    public static class ArtImportRules
    {
        public const int PixelsPerUnit = 16;
        public const string FormalRoot = "Assets/Art";

        public static void Apply(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath) || !assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("A PNG asset path is required.", nameof(assetPath));

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException($"No TextureImporter exists for '{assetPath}'.");

            var metadata = LoadMetadataForImage(assetPath);
            var rects = SelectSpriteMetadata(assetPath, metadata);
            ConfigureTexture(importer, rects != null && rects.Length > 0);
            importer.SaveAndReimport();

            if (rects != null && rects.Length > 0)
                ApplySpriteRects(assetPath, rects);
        }

        public static void ApplyAllFormal()
        {
            var declaredPaths = new HashSet<string>(StringComparer.Ordinal);
            foreach (var metadataPath in EnumerateMetadataAssetPaths())
            {
                var metadata = ReadMetadataAtPath(metadataPath);
                var directory = Path.GetDirectoryName(metadataPath) ?? string.Empty;
                declaredPaths.Add(Path.Combine(directory, metadata.image).Replace('\\', '/'));
                if (!string.IsNullOrEmpty(metadata.landmarkImage))
                    declaredPaths.Add(Path.Combine(directory, metadata.landmarkImage).Replace('\\', '/'));
                if (string.Equals(metadata.kind, "environment", StringComparison.Ordinal))
                    declaredPaths.Add(Path.Combine(directory, metadata.id + "_reference.png").Replace('\\', '/'));
            }
            var paths = declaredPaths
                .Where(File.Exists)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            foreach (var path in paths)
                Apply(path);
            AssetDatabase.Refresh();
        }

        public static string[] EnumerateMetadataAssetPaths()
        {
            if (!Directory.Exists(FormalRoot))
                return Array.Empty<string>();
            return Directory.GetFiles(FormalRoot, "*.art.json", SearchOption.AllDirectories)
                .Select(path => path.Replace('\\', '/'))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
        }

        internal static ArtMetadata ReadMetadataAtPath(string metadataPath)
        {
            if (!File.Exists(metadataPath))
                throw new FileNotFoundException("Missing art metadata.", metadataPath);
            var metadata = JsonUtility.FromJson<ArtMetadata>(File.ReadAllText(metadataPath));
            if (metadata == null || metadata.schemaVersion != 1 || string.IsNullOrEmpty(metadata.id))
                throw new InvalidDataException($"Invalid art metadata '{metadataPath}'.");
            metadata.sprites = metadata.sprites ?? Array.Empty<ArtSpriteMetadata>();
            metadata.landmarks = metadata.landmarks ?? Array.Empty<ArtSpriteMetadata>();
            return metadata;
        }

        internal static string MetadataPathForImage(string assetPath)
        {
            if (assetPath.EndsWith("_landmarks.png", StringComparison.Ordinal))
            {
                var stem = assetPath.Substring(0, assetPath.Length - "_landmarks.png".Length);
                return stem + "_tileset.art.json";
            }
            return Path.ChangeExtension(assetPath, ".art.json").Replace('\\', '/');
        }

        private static ArtMetadata LoadMetadataForImage(string assetPath)
        {
            var metadataPath = MetadataPathForImage(assetPath);
            return File.Exists(metadataPath) ? ReadMetadataAtPath(metadataPath) : null;
        }

        private static ArtSpriteMetadata[] SelectSpriteMetadata(string assetPath, ArtMetadata metadata)
        {
            if (metadata == null)
                return null;
            if (string.Equals(Path.GetFileName(assetPath), metadata.image, StringComparison.Ordinal))
                return metadata.sprites;
            if (string.Equals(Path.GetFileName(assetPath), metadata.landmarkImage, StringComparison.Ordinal))
                return metadata.landmarks;
            return null;
        }

        private static void ConfigureTexture(TextureImporter importer, bool multiple)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = multiple ? SpriteImportMode.Multiple : SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 8192;
            if (!multiple)
            {
                importer.spritePivot = new Vector2(0.5f, 0.5f);
            }
        }

        private static void ApplySpriteRects(string assetPath, IReadOnlyList<ArtSpriteMetadata> metadataRects)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture == null)
                throw new InvalidOperationException($"Texture '{assetPath}' failed to import.");

            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(texture);
            provider.InitSpriteEditorDataProvider();
            var existingIds = provider.GetSpriteRects()
                .Where(rect => !string.IsNullOrEmpty(rect.name))
                .GroupBy(rect => rect.name, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First().spriteID, StringComparer.Ordinal);

            var spriteRects = new SpriteRect[metadataRects.Count];
            for (var index = 0; index < metadataRects.Count; index++)
            {
                var item = metadataRects[index];
                if (item.rect == null || item.rect.Length != 4 || item.pivot == null || item.pivot.Length != 2)
                    throw new InvalidDataException($"Sprite '{item.name}' has invalid rect or pivot metadata.");
                var unityY = texture.height - item.rect[1] - item.rect[3];
                spriteRects[index] = new SpriteRect
                {
                    name = item.name,
                    rect = new Rect(item.rect[0], unityY, item.rect[2], item.rect[3]),
                    pivot = new Vector2(item.pivot[0], item.pivot[1]),
                    alignment = SpriteAlignment.Custom,
                    spriteID = existingIds.TryGetValue(item.name, out var existingId)
                        ? existingId
                        : GUID.Generate()
                };
            }

            provider.SetSpriteRects(spriteRects);
            var nameProvider = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            nameProvider?.SetNameFileIdPairs(spriteRects.Select(
                rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
            provider.Apply();

            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.SaveAndReimport();
        }
    }
}
