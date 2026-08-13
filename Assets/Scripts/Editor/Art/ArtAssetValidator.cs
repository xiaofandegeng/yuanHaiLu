using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace YuanHaiLu.Editor
{
    public sealed class ArtValidationReport
    {
        private readonly List<string> errors = new List<string>();

        public IReadOnlyList<string> Errors => errors;
        public bool IsValid => errors.Count == 0;

        internal void Add(string error)
        {
            errors.Add(error);
        }

        public override string ToString()
        {
            return IsValid ? "Formal art validation passed." : string.Join("\n", errors);
        }
    }

    public static class ArtAssetValidator
    {
        [Serializable]
        private sealed class ManifestEntry
        {
            public string id;
        }

        [Serializable]
        private sealed class FormalManifest
        {
            public string outputDirectory;
            public bool perRecipeDirectory;
            public ManifestEntry[] characters;
            public ManifestEntry[] environments;
        }

        private static readonly string[] FormalManifestPaths =
        {
            "Assets/ArtSource/Characters/Manifests/player-roster.json",
            "Assets/ArtSource/Characters/Manifests/named-roster.json",
            "Assets/ArtSource/Characters/Manifests/npc-roster.json",
            "Assets/ArtSource/Characters/Manifests/enemy-roster.json",
            "Assets/ArtSource/Characters/Manifests/boss-roster.json",
            "Assets/ArtSource/Environment/Manifests/regions.json",
            "Assets/ArtSource/Environment/Manifests/interiors.json"
        };

        [MenuItem("Tools/渊海录/美术/验证正式美术")]
        public static void ValidateFromMenu()
        {
            var report = ValidateAll();
            if (!report.IsValid)
                throw new InvalidOperationException(report.ToString());
            Debug.Log("[ArtAssetValidator] Formal art validation passed.");
        }

        public static ArtValidationReport ValidateAll()
        {
            var report = new ArtValidationReport();
            string[] metadataPaths = ArtImportRules.EnumerateMetadataAssetPaths();
            ValidateFormalScope(metadataPaths, report);
            foreach (var metadataPath in metadataPaths)
            {
                try
                {
                    ValidateMetadata(metadataPath, report);
                }
                catch (Exception exception)
                {
                    report.Add($"{metadataPath}: {exception.Message}");
                }
            }
            if (metadataPaths.Length == 0)
                report.Add("No formal .art.json metadata files were found under Assets/Art.");
            return report;
        }

        private static void ValidateFormalScope(
            IReadOnlyCollection<string> actualPaths,
            ArtValidationReport report)
        {
            var expected = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string manifestPath in FormalManifestPaths)
            {
                if (!File.Exists(manifestPath))
                {
                    report.Add($"Missing formal manifest '{manifestPath}'.");
                    continue;
                }
                var manifest = JsonUtility.FromJson<FormalManifest>(File.ReadAllText(manifestPath));
                if (manifest == null || string.IsNullOrEmpty(manifest.outputDirectory))
                {
                    report.Add($"Invalid formal manifest '{manifestPath}'.");
                    continue;
                }
                foreach (var entry in manifest.characters ?? Array.Empty<ManifestEntry>())
                    expected[$"{manifest.outputDirectory}/{entry.id}.art.json"] = entry.id;
                foreach (var entry in manifest.environments ?? Array.Empty<ManifestEntry>())
                {
                    string directory = manifest.perRecipeDirectory
                        ? $"{manifest.outputDirectory}/{entry.id}"
                        : manifest.outputDirectory;
                    expected[$"{directory}/{entry.id}_tileset.art.json"] = entry.id;
                }
            }

            var actual = new HashSet<string>(actualPaths, StringComparer.Ordinal);
            foreach (string missing in expected.Keys.Where(path => !actual.Contains(path)))
                report.Add($"Missing formal metadata '{missing}'.");
            foreach (string unexpected in actual.Where(path => !expected.ContainsKey(path)))
                report.Add($"Unexpected formal metadata '{unexpected}'.");
            foreach (var pair in expected.Where(pair => actual.Contains(pair.Key)))
            {
                var metadata = ArtImportRules.ReadMetadataAtPath(pair.Key);
                if (!string.Equals(metadata.id, pair.Value, StringComparison.Ordinal))
                    report.Add($"'{pair.Key}' declares id '{metadata.id}', expected '{pair.Value}'.");
            }
        }

        private static void ValidateMetadata(string metadataPath, ArtValidationReport report)
        {
            var metadata = ArtImportRules.ReadMetadataAtPath(metadataPath);
            var directory = Path.GetDirectoryName(metadataPath) ?? string.Empty;
            var imagePath = Path.Combine(directory, metadata.image).Replace('\\', '/');
            ValidateTexture(imagePath, metadata.sha256, metadata.sprites, report);

            if (!string.IsNullOrEmpty(metadata.landmarkImage))
            {
                var landmarkPath = Path.Combine(directory, metadata.landmarkImage).Replace('\\', '/');
                ValidateTexture(landmarkPath, metadata.landmarkSha256, metadata.landmarks, report);
            }

            if (string.Equals(metadata.kind, "environment", StringComparison.Ordinal))
            {
                var previewPath = Path.Combine(directory, metadata.id + "_reference.png").Replace('\\', '/');
                ValidatePreview(previewPath, report);
            }
        }

        private static void ValidateTexture(
            string assetPath,
            string expectedHash,
            IReadOnlyCollection<ArtSpriteMetadata> expectedSprites,
            ArtValidationReport report)
        {
            if (!File.Exists(assetPath))
            {
                report.Add($"Missing formal texture '{assetPath}'.");
                return;
            }
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                report.Add($"Missing TextureImporter for '{assetPath}'.");
                return;
            }
            if (importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Multiple ||
                importer.spritePixelsPerUnit != ArtImportRules.PixelsPerUnit ||
                importer.filterMode != FilterMode.Point ||
                importer.mipmapEnabled ||
                importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                report.Add($"'{assetPath}' violates formal pixel import settings.");
            }

            var actualNames = new HashSet<string>(
                AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().Select(sprite => sprite.name),
                StringComparer.Ordinal);
            foreach (var sprite in expectedSprites)
            {
                if (!actualNames.Contains(sprite.name))
                    report.Add($"'{assetPath}' is missing sprite '{sprite.name}'.");
            }

            var actualHash = ComputeRgbaHash(assetPath);
            if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
                report.Add($"'{assetPath}' hash mismatch: expected {expectedHash}, got {actualHash}.");
        }

        private static void ValidatePreview(string assetPath, ArtValidationReport report)
        {
            if (!File.Exists(assetPath))
            {
                report.Add($"Missing formal preview '{assetPath}'.");
                return;
            }
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null || importer.textureType != TextureImporterType.Sprite ||
                importer.filterMode != FilterMode.Point || importer.mipmapEnabled ||
                importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                report.Add($"'{assetPath}' violates preview pixel import settings.");
            }
        }

        private static string ComputeRgbaHash(string assetPath)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(assetPath), false))
                    throw new InvalidDataException("PNG decode failed.");
                var pixels = texture.GetPixels32();
                using (var hash = SHA256.Create())
                {
                    var header = Encoding.ASCII.GetBytes($"{texture.width}x{texture.height}:RGBA");
                    hash.TransformBlock(header, 0, header.Length, null, 0);
                    var row = new byte[texture.width * 4];
                    for (var y = texture.height - 1; y >= 0; y--)
                    {
                        for (var x = 0; x < texture.width; x++)
                        {
                            var pixel = pixels[y * texture.width + x];
                            var offset = x * 4;
                            row[offset] = pixel.r;
                            row[offset + 1] = pixel.g;
                            row[offset + 2] = pixel.b;
                            row[offset + 3] = pixel.a;
                        }
                        hash.TransformBlock(row, 0, row.Length, null, 0);
                    }
                    hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                    return BitConverter.ToString(hash.Hash).Replace("-", string.Empty).ToLowerInvariant();
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
