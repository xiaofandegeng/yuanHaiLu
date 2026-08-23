using System;
using System.Collections.Generic;
using UnityEngine;

namespace YuanHaiLu.Art
{
    [Serializable]
    public sealed class EnvironmentArtEntry
    {
        [SerializeField] private string regionId;
        [SerializeField] private Texture2D tileset;
        [SerializeField] private Texture2D landmarks;
        [SerializeField] private Texture2D preview;
        [SerializeField] private string sceneConfigurationId;
        [SerializeField] private string kind;
        [SerializeField] private string sceneAssetPath;
        [SerializeField] private bool supportsDayNight;
        [SerializeField] private string weatherId;

        public string RegionId => regionId;
        public Texture2D Tileset => tileset;
        public Texture2D Landmarks => landmarks;
        public Texture2D Preview => preview;
        public string SceneConfigurationId => sceneConfigurationId;
        public string Kind => kind;
        public string SceneAssetPath => sceneAssetPath;
        public bool SupportsDayNight => supportsDayNight;
        public string WeatherId => weatherId;

        private EnvironmentArtEntry() { }

        public static EnvironmentArtEntry Create(
            string stableRegionId,
            Texture2D tilesetTexture,
            Texture2D landmarkTexture,
            Texture2D previewTexture,
            string configurationId,
            string environmentKind = "region",
            string scenePath = "Assets/Scenes/Regions/test.unity",
            bool hasDayNight = true,
            string formalWeatherId = "clear")
        {
            return new EnvironmentArtEntry
            {
                regionId = stableRegionId,
                tileset = tilesetTexture,
                landmarks = landmarkTexture,
                preview = previewTexture,
                sceneConfigurationId = configurationId,
                kind = environmentKind,
                sceneAssetPath = scenePath,
                supportsDayNight = hasDayNight,
                weatherId = formalWeatherId
            };
        }

        public static EnvironmentArtEntry ForTest(string stableRegionId)
        {
            return Create(
                stableRegionId,
                Texture2D.whiteTexture,
                Texture2D.whiteTexture,
                Texture2D.whiteTexture,
                stableRegionId + "_reference");
        }

        internal void Validate()
        {
            if (!ArtAssetId.IsValid(regionId))
                throw new InvalidOperationException(
                    $"Invalid environment art id '{regionId ?? "<null>"}'.");
            if (tileset == null)
                throw new InvalidOperationException($"Environment art '{regionId}' has no tileset texture.");
            if (landmarks == null)
                throw new InvalidOperationException($"Environment art '{regionId}' has no landmark texture.");
            if (preview == null)
                throw new InvalidOperationException($"Environment art '{regionId}' has no preview texture.");
            if (!ArtAssetId.IsValid(sceneConfigurationId))
                throw new InvalidOperationException(
                    $"Environment art '{regionId}' has invalid scene configuration id '{sceneConfigurationId}'.");
            if (kind != "region" && kind != "interior")
                throw new InvalidOperationException($"Environment art '{regionId}' has invalid kind '{kind}'.");
            if (string.IsNullOrWhiteSpace(sceneAssetPath) || !sceneAssetPath.EndsWith(".unity", StringComparison.Ordinal))
                throw new InvalidOperationException($"Environment art '{regionId}' has invalid scene path '{sceneAssetPath}'.");
            if (!ArtAssetId.IsValid(weatherId))
                throw new InvalidOperationException(
                    $"Environment art '{regionId}' has invalid weather id '{weatherId}'.");
        }
    }

    [CreateAssetMenu(fileName = "EnvironmentArtCatalog", menuName = "渊海录/美术/环境目录")]
    public sealed class EnvironmentArtCatalog : ScriptableObject
    {
        public const string DefaultResourcePath = "Art/EnvironmentArtCatalog";

        [SerializeField] private List<EnvironmentArtEntry> entries = new List<EnvironmentArtEntry>();

        [NonSerialized] private Dictionary<string, EnvironmentArtEntry> lookup;

        public IReadOnlyList<EnvironmentArtEntry> Entries => entries;

        public static EnvironmentArtCatalog LoadDefault()
        {
            var catalog = Resources.Load<EnvironmentArtCatalog>(DefaultResourcePath);
            if (catalog == null)
                throw new InvalidOperationException(
                    $"Missing Resources/{DefaultResourcePath}.asset formal environment catalog.");
            catalog.RebuildLookup();
            return catalog;
        }

        private void OnEnable()
        {
            // Serialized catalog migrations may temporarily contain entries from an
            // older schema. Rebuild explicitly after the editor builder has written
            // the current fields, or lazily from TryGet/LoadDefault.
            lookup = null;
        }

        public void SetEntriesForEditor(IEnumerable<EnvironmentArtEntry> values)
        {
            entries = values == null
                ? new List<EnvironmentArtEntry>()
                : new List<EnvironmentArtEntry>(values);
            lookup = null;
        }

        public void RebuildLookup()
        {
            lookup = new Dictionary<string, EnvironmentArtEntry>(StringComparer.Ordinal);
            if (entries == null)
                entries = new List<EnvironmentArtEntry>();

            foreach (var entry in entries)
            {
                if (entry == null)
                    throw new InvalidOperationException("Environment art catalog contains a null entry.");
                entry.Validate();
                if (!lookup.TryAdd(entry.RegionId, entry))
                    throw new InvalidOperationException($"Duplicate environment art id '{entry.RegionId}'.");
            }
        }

        public bool TryGet(string id, out EnvironmentArtEntry entry)
        {
            if (lookup == null)
                RebuildLookup();
            if (string.IsNullOrEmpty(id))
            {
                entry = null;
                return false;
            }
            return lookup.TryGetValue(id, out entry);
        }
    }
}
