using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using YuanHaiLu.Art;

namespace YuanHaiLu.Editor
{
    [Serializable]
    internal sealed class LayoutAnchorJson
    {
        public string id;
        public string type;
        public int x;
        public int y;
    }

    [Serializable]
    internal sealed class LayoutJson
    {
        public string id;
        public string kind;
        public int width;
        public int height;
        public LayoutAnchorJson[] anchors;
        public string[] requiredLandmarks;
    }

    public static class RegionSceneBuilder
    {
        private static readonly string[] LayerNames =
        {
            "Ground", "Water", "Lower Environment", "Buildings",
            "Character", "Foreground", "Effects"
        };

        [MenuItem("Tools/渊海录/美术/生成全部正式环境场景")]
        public static void BuildAll()
        {
            EnvironmentTileBuilder.RebuildAll();
            foreach (var path in Directory.GetFiles(
                "Assets/ArtSource/Environment/Layouts",
                "*.json",
                SearchOption.AllDirectories).OrderBy(value => value, StringComparer.Ordinal))
            {
                var layout = ReadLayout(path.Replace('\\', '/'));
                Build(layout.id);
            }
            ArtCatalogBuilder.RebuildAll();
            Debug.Log("[RegionSceneBuilder] generated 23 formal environment scenes.");
        }

        public static void Build(string id)
        {
            var layout = ReadLayout(LayoutPath(id));
            var tiles = EnvironmentTileBuilder.LoadTiles(id);
            if (tiles.Count == 0)
                throw new InvalidOperationException($"No formal tiles were generated for '{id}'.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = id;
            var gridObject = new GameObject(id);
            var grid = gridObject.AddComponent<Grid>();
            grid.cellSize = Vector3.one;
            var definition = gridObject.AddComponent<RegionSceneDefinition>();
            definition.ConfigureForEditor(
                id,
                layout.kind,
                new Vector2Int(layout.width, layout.height),
                (layout.anchors ?? Array.Empty<LayoutAnchorJson>()).Select(
                    anchor => new SceneAnchorDefinition(
                        anchor.id,
                        anchor.type,
                        new Vector2Int(anchor.x, anchor.y))));

            var maps = new Dictionary<string, Tilemap>(StringComparer.Ordinal);
            for (var index = 0; index < LayerNames.Length; index++)
            {
                var child = new GameObject(LayerNames[index]);
                child.transform.SetParent(gridObject.transform);
                var tilemap = child.AddComponent<Tilemap>();
                var renderer = child.AddComponent<TilemapRenderer>();
                renderer.sortingOrder = index * 10;
                maps[LayerNames[index]] = tilemap;
            }

            var ground = FindRole(tiles, id, layout.kind == "interior" ? "floor" : "ground", 0);
            var groundVariants = FindRoleVariants(tiles, id, layout.kind == "interior" ? "floor" : "ground");
            for (var y = 0; y < layout.height; y++)
            {
                for (var x = 0; x < layout.width; x++)
                {
                    var tile = groundVariants[(x * 7 + y * 11) % groundVariants.Count];
                    maps["Ground"].SetTile(new Vector3Int(x, y, 0), tile);
                }
            }
            if (layout.kind == "region" && TryFindRole(tiles, id, "water", 0, out var water))
            {
                for (var y = 0; y < 4; y++)
                    for (var x = 0; x < layout.width; x++)
                        maps["Water"].SetTile(new Vector3Int(x, y, 0), water);
            }
            if (TryFindRole(tiles, id, "road", 0, out var road))
            {
                for (var y = 4; y < layout.height; y++)
                    for (var x = layout.width / 2 - 2; x <= layout.width / 2 + 1; x++)
                        maps["Ground"].SetTile(new Vector3Int(x, y, 0), road);
            }
            if (TryFindRole(tiles, id, layout.kind == "interior" ? "prop" : "decor", 0, out var decoration))
            {
                for (var index = 0; index < 12; index++)
                    maps["Lower Environment"].SetTile(
                        new Vector3Int(2 + (index * 7) % Math.Max(3, layout.width - 4), 3 + (index * 5) % Math.Max(3, layout.height - 6), 0),
                        decoration);
            }

            AddLandmarks(id, layout, gridObject.transform);
            AddAnchors(layout, gridObject.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            EnsureSceneFolder(layout.kind);
            EditorSceneManager.SaveScene(scene, ScenePath(id));
        }

        public static string ScenePath(string id)
        {
            return File.Exists($"Assets/ArtSource/Environment/Layouts/interiors/{id}.json")
                ? $"Assets/Scenes/Interiors/{id}.unity"
                : $"Assets/Scenes/Regions/{id}.unity";
        }

        private static string LayoutPath(string id)
        {
            var interior = $"Assets/ArtSource/Environment/Layouts/interiors/{id}.json";
            return File.Exists(interior)
                ? interior
                : $"Assets/ArtSource/Environment/Layouts/{id}.json";
        }

        private static LayoutJson ReadLayout(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Missing environment layout.", path);
            var layout = JsonUtility.FromJson<LayoutJson>(File.ReadAllText(path));
            if (layout == null || string.IsNullOrEmpty(layout.id) || layout.width <= 0 || layout.height <= 0)
                throw new InvalidDataException($"Invalid environment layout '{path}'.");
            return layout;
        }

        private static void AddLandmarks(string id, LayoutJson layout, Transform root)
        {
            var metadataPath = layout.kind == "interior"
                ? $"Assets/Art/Environment/Interiors/{id}/{id}_tileset.art.json"
                : $"Assets/Art/Environment/Regions/{id}/{id}_tileset.art.json";
            var metadata = ArtImportRules.ReadMetadataAtPath(metadataPath);
            var imagePath = Path.Combine(Path.GetDirectoryName(metadataPath) ?? string.Empty, metadata.landmarkImage).Replace('\\', '/');
            var sprites = AssetDatabase.LoadAllAssetsAtPath(imagePath).OfType<Sprite>()
                .ToDictionary(sprite => sprite.name, StringComparer.Ordinal);
            foreach (var landmarkId in layout.requiredLandmarks ?? Array.Empty<string>())
            {
                var spriteName = $"{id}__landmark__{landmarkId}";
                if (!sprites.TryGetValue(spriteName, out var sprite))
                    throw new InvalidOperationException($"Missing formal landmark '{spriteName}'.");
                var anchor = (layout.anchors ?? Array.Empty<LayoutAnchorJson>()).First(value => value.id == landmarkId);
                var instance = new GameObject("Landmark_" + landmarkId);
                instance.transform.SetParent(root);
                instance.transform.position = new Vector3(anchor.x, anchor.y, 0f);
                var renderer = instance.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = 35;
            }
        }

        private static void AddAnchors(LayoutJson layout, Transform root)
        {
            var anchorRoot = new GameObject("Anchors");
            anchorRoot.transform.SetParent(root);
            foreach (var anchor in layout.anchors ?? Array.Empty<LayoutAnchorJson>())
            {
                var instance = new GameObject(anchor.type + "_" + anchor.id);
                instance.transform.SetParent(anchorRoot.transform);
                instance.transform.position = new Vector3(anchor.x + 0.5f, anchor.y + 0.5f, 0f);
            }
        }

        private static List<Tile> FindRoleVariants(IReadOnlyDictionary<string, Tile> tiles, string id, string role)
        {
            var prefix = $"{id}__{role}__";
            var values = tiles.Where(pair => pair.Key.StartsWith(prefix, StringComparison.Ordinal))
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => pair.Value)
                .ToList();
            if (values.Count == 0)
                throw new InvalidOperationException($"'{id}' is missing required tile role '{role}'.");
            return values;
        }

        private static Tile FindRole(IReadOnlyDictionary<string, Tile> tiles, string id, string role, int variant)
        {
            if (!TryFindRole(tiles, id, role, variant, out var tile))
                throw new InvalidOperationException($"'{id}' is missing tile '{role}' variant {variant}.");
            return tile;
        }

        private static bool TryFindRole(IReadOnlyDictionary<string, Tile> tiles, string id, string role, int variant, out Tile tile)
        {
            return tiles.TryGetValue($"{id}__{role}__{variant}", out tile);
        }

        private static void EnsureSceneFolder(string kind)
        {
            var path = kind == "interior" ? "Assets/Scenes/Interiors" : "Assets/Scenes/Regions";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder("Assets/Scenes", kind == "interior" ? "Interiors" : "Regions");
        }
    }
}
