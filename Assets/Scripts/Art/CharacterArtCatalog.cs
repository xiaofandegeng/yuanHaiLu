using System;
using System.Collections.Generic;
using UnityEngine;

namespace YuanHaiLu.Art
{
    [Serializable]
    public sealed class CharacterArtEntry
    {
        [SerializeField] private string id;
        [SerializeField] private string category;
        [SerializeField] private Texture2D sheet;
        [SerializeField] private RuntimeAnimatorController controller;
        [SerializeField] private GameObject prefab;
        [SerializeField] private Texture2D preview;

        public string Id => id;
        public string Category => category;
        public Texture2D Sheet => sheet;
        public RuntimeAnimatorController Controller => controller;
        public GameObject Prefab => prefab;
        public Texture2D Preview => preview;

        private CharacterArtEntry() { }

        public static CharacterArtEntry Create(
            string stableId,
            string artCategory,
            Texture2D spriteSheet,
            RuntimeAnimatorController animatorController,
            GameObject characterPrefab,
            Texture2D previewTexture)
        {
            return new CharacterArtEntry
            {
                id = stableId,
                category = artCategory,
                sheet = spriteSheet,
                controller = animatorController,
                prefab = characterPrefab,
                preview = previewTexture
            };
        }

        public static CharacterArtEntry ForTest(string stableId)
        {
            return Create(
                stableId,
                "test",
                Texture2D.whiteTexture,
                null,
                null,
                Texture2D.whiteTexture);
        }

        internal void Validate()
        {
            if (!ArtAssetId.IsValid(id))
                throw new InvalidOperationException($"Invalid character art id '{id ?? "<null>"}'.");
            if (string.IsNullOrWhiteSpace(category))
                throw new InvalidOperationException($"Character art '{id}' has no category.");
            if (sheet == null)
                throw new InvalidOperationException($"Character art '{id}' has no formal sheet texture.");
            if (preview == null)
                throw new InvalidOperationException($"Character art '{id}' has no preview texture.");
        }
    }

    [CreateAssetMenu(fileName = "CharacterArtCatalog", menuName = "渊海录/美术/角色目录")]
    public sealed class CharacterArtCatalog : ScriptableObject
    {
        public const string DefaultResourcePath = "Art/CharacterArtCatalog";

        [SerializeField] private List<CharacterArtEntry> entries = new List<CharacterArtEntry>();

        [NonSerialized] private Dictionary<string, CharacterArtEntry> lookup;

        public IReadOnlyList<CharacterArtEntry> Entries => entries;

        public static CharacterArtCatalog LoadDefault()
        {
            var catalog = Resources.Load<CharacterArtCatalog>(DefaultResourcePath);
            if (catalog == null)
                throw new InvalidOperationException(
                    $"Missing Resources/{DefaultResourcePath}.asset formal character catalog.");
            catalog.RebuildLookup();
            return catalog;
        }

        private void OnEnable()
        {
            RebuildLookup();
        }

        public void SetEntriesForEditor(IEnumerable<CharacterArtEntry> values)
        {
            entries = values == null
                ? new List<CharacterArtEntry>()
                : new List<CharacterArtEntry>(values);
            lookup = null;
        }

        public void RebuildLookup()
        {
            lookup = new Dictionary<string, CharacterArtEntry>(StringComparer.Ordinal);
            if (entries == null)
                entries = new List<CharacterArtEntry>();

            foreach (var entry in entries)
            {
                if (entry == null)
                    throw new InvalidOperationException("Character art catalog contains a null entry.");
                entry.Validate();
                if (!lookup.TryAdd(entry.Id, entry))
                    throw new InvalidOperationException($"Duplicate character art id '{entry.Id}'.");
            }
        }

        public bool TryGet(string id, out CharacterArtEntry entry)
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
