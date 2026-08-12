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
            var groundPositions = new List<Vector3Int>(layout.width * layout.height);
            var groundTiles = new List<TileBase>(layout.width * layout.height);
            for (var y = 0; y < layout.height; y++)
            {
                for (var x = 0; x < layout.width; x++)
                {
                    var tile = groundVariants[(x * 7 + y * 11) % groundVariants.Count];
                    groundPositions.Add(new Vector3Int(x, y, 0));
                    groundTiles.Add(tile);
                }
            }
            maps["Ground"].SetTiles(groundPositions.ToArray(), groundTiles.ToArray());
            if (layout.kind == "region" && TryFindRole(tiles, id, "water", 0, out var water))
            {
                var waterPositions = new List<Vector3Int>(layout.width * 4);
                var waterTiles = new List<TileBase>(layout.width * 4);
                for (var y = 0; y < 4; y++)
                    for (var x = 0; x < layout.width; x++)
                    {
                        waterPositions.Add(new Vector3Int(x, y, 0));
                        waterTiles.Add(water);
                    }
                maps["Water"].SetTiles(waterPositions.ToArray(), waterTiles.ToArray());
            }
            if (TryFindRole(tiles, id, "road", 0, out var road))
            {
                var roadPositions = new List<Vector3Int>();
                var roadTiles = new List<TileBase>();
                for (var y = 4; y < layout.height; y++)
                    for (var x = layout.width / 2 - 2; x <= layout.width / 2 + 1; x++)
                    {
                        roadPositions.Add(new Vector3Int(x, y, 0));
                        roadTiles.Add(road);
                    }
                maps["Ground"].SetTiles(roadPositions.ToArray(), roadTiles.ToArray());
            }
            if (TryFindRole(tiles, id, layout.kind == "interior" ? "prop" : "decor", 0, out var decoration))
            {
                var decorationPositions = new List<Vector3Int>(12);
                var decorationTiles = new List<TileBase>(12);
                for (var index = 0; index < 12; index++)
                {
                    decorationPositions.Add(new Vector3Int(
                        2 + (index * 7) % Math.Max(3, layout.width - 4),
                        3 + (index * 5) % Math.Max(3, layout.height - 6),
                        0));
                    decorationTiles.Add(decoration);
                }
                maps["Lower Environment"].SetTiles(
                    decorationPositions.ToArray(),
                    decorationTiles.ToArray());
            }

            if (layout.kind == "interior")
                PaintInteriorStructure(maps, tiles, id, layout.width, layout.height);
            else
                PaintRegionStructure(maps, tiles, id, layout.width, layout.height);

            int groundCellCount = maps["Ground"].GetTilesBlock(
                    new BoundsInt(0, 0, 0, layout.width, layout.height, 1))
                .Count(tile => tile != null);
            if (groundCellCount != layout.width * layout.height)
                throw new InvalidOperationException(
                    $"'{id}' populated {groundCellCount} of {layout.width * layout.height} ground cells.");

            AddLandmarks(id, layout, gridObject.transform);
            AddAnchors(layout, gridObject.transform);
            foreach (var tilemap in maps.Values)
            {
                tilemap.CompressBounds();
                tilemap.RefreshAllTiles();
                EditorUtility.SetDirty(tilemap);
                EditorUtility.SetDirty(tilemap.GetComponent<TilemapRenderer>());
            }
            EditorUtility.SetDirty(gridObject);
            EditorUtility.SetDirty(definition);
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

        private static void PaintRegionStructure(
            IReadOnlyDictionary<string, Tilemap> maps,
            IReadOnlyDictionary<string, Tile> tiles,
            string id,
            int width,
            int height)
        {
            var shore = FindRole(tiles, id, "shore", 0);
            var shorePositions = Enumerable.Range(0, width)
                .Select(x => new Vector3Int(x, 4, 0))
                .ToArray();
            maps["Lower Environment"].SetTiles(
                shorePositions,
                Enumerable.Repeat<TileBase>(shore, shorePositions.Length).ToArray());

            var wall = FindRole(tiles, id, "wall", 0);
            var wallAlt = FindRole(tiles, id, "wall", 1);
            var roof = FindRole(tiles, id, "roof", 0);
            var roofAlt = FindRole(tiles, id, "roof", 1);
            var door = FindRole(tiles, id, "door", 0);
            var window = FindRole(tiles, id, "window", 0);
            PaintHouse(maps["Buildings"], 3, Math.Max(7, height - 7), wall, wallAlt, roof, roofAlt, door, window);
            PaintHouse(maps["Buildings"], Math.Max(12, width - 10), Math.Max(7, height - 7),
                wallAlt, wall, roofAlt, roof, door, window);
        }

        private static void PaintHouse(
            Tilemap tilemap,
            int left,
            int bottom,
            Tile wall,
            Tile wallAlt,
            Tile roof,
            Tile roofAlt,
            Tile door,
            Tile window)
        {
            var positions = new List<Vector3Int>();
            var values = new List<TileBase>();
            for (var x = 0; x < 7; x++)
            {
                positions.Add(new Vector3Int(left + x, bottom + 2, 0));
                values.Add(x % 2 == 0 ? roof : roofAlt);
                positions.Add(new Vector3Int(left + x, bottom + 1, 0));
                values.Add(x % 2 == 0 ? wall : wallAlt);
                positions.Add(new Vector3Int(left + x, bottom, 0));
                values.Add(x == 3 ? door : (x == 1 || x == 5 ? window : wall));
            }
            tilemap.SetTiles(positions.ToArray(), values.ToArray());
        }

        private static void PaintInteriorStructure(
            IReadOnlyDictionary<string, Tilemap> maps,
            IReadOnlyDictionary<string, Tile> tiles,
            string id,
            int width,
            int height)
        {
            var walls = FindRoleVariants(tiles, id, "wall");
            var wallPositions = new List<Vector3Int>();
            var wallTiles = new List<TileBase>();
            for (var x = 0; x < width; x++)
            {
                AddWallCell(x, 0);
                AddWallCell(x, height - 1);
            }
            for (var y = 1; y < height - 1; y++)
            {
                AddWallCell(0, y);
                AddWallCell(width - 1, y);
            }
            maps["Buildings"].SetTiles(wallPositions.ToArray(), wallTiles.ToArray());

            var entry = FindRole(tiles, id, "entry", 0);
            var exit = FindRole(tiles, id, "exit", 0);
            maps["Buildings"].SetTiles(
                new[]
                {
                    new Vector3Int(width / 2, 0, 0),
                    new Vector3Int(width / 2, height - 1, 0)
                },
                new TileBase[] { entry, exit });

            if (TryFindRole(tiles, id, "light", 0, out var light))
            {
                maps["Effects"].SetTiles(
                    new[]
                    {
                        new Vector3Int(2, height - 3, 0),
                        new Vector3Int(width - 3, height - 3, 0)
                    },
                    new TileBase[] { light, light });
            }

            void AddWallCell(int x, int y)
            {
                wallPositions.Add(new Vector3Int(x, y, 0));
                wallTiles.Add(walls[(x * 3 + y * 5) % walls.Count]);
            }
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
