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
    internal sealed class LayoutCellJson
    {
        public int x;
        public int y;
        public string token;
    }

    [Serializable]
    internal sealed class LayoutLayerJson
    {
        public string name;
        public LayoutCellJson[] cells;
    }

    [Serializable]
    internal sealed class LayoutCollisionJson
    {
        public int x;
        public int y;
        public int width;
        public int height;
    }

    [Serializable]
    internal sealed class ForegroundSpanJson
    {
        public int fromX;
        public int fromY;
        public int toX;
        public int toY;
        public string token;
    }

    [Serializable]
    internal sealed class LayoutJson
    {
        public string id;
        public string kind;
        public int width;
        public int height;
        public LayoutLayerJson[] layers;
        public LayoutCollisionJson[] collisions;
        public ForegroundSpanJson[] foregroundSpans;
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

            foreach (var layerName in LayerNames)
                ApplyDeclaredLayer(maps[layerName], tiles, id, layout, layerName);

            int groundCellCount = maps["Ground"].GetTilesBlock(
                    new BoundsInt(0, 0, 0, layout.width, layout.height, 1))
                .Count(tile => tile != null);
            if (groundCellCount != DeclaredLayerCellCount(layout, "Ground"))
                throw new InvalidOperationException(
                    $"'{id}' populated {groundCellCount} declared ground cells.");

            AddLandmarks(id, layout, gridObject.transform);
            AddAnchors(layout, gridObject.transform);
            AddDeclaredColliders(layout, gridObject.transform);
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

        public static int DeclaredLayerCellCount(string id, string layerName)
        {
            return DeclaredLayerCellCount(ReadLayout(LayoutPath(id)), layerName);
        }

        public static int DeclaredCollisionRunCount(string id)
        {
            return (ReadLayout(LayoutPath(id)).collisions ?? Array.Empty<LayoutCollisionJson>()).Length;
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

        private static void ApplyDeclaredLayer(
            Tilemap tilemap,
            IReadOnlyDictionary<string, Tile> tiles,
            string id,
            LayoutJson layout,
            string layerName)
        {
            var positions = new List<Vector3Int>();
            var values = new List<TileBase>();
            var authored = FindLayer(layout, layerName);
            foreach (var cell in authored?.cells ?? Array.Empty<LayoutCellJson>())
            {
                ValidateCell(layout, cell.x, cell.y, layerName);
                positions.Add(new Vector3Int(cell.x, cell.y, 0));
                values.Add(ResolveDeclaredTile(tiles, id, cell.token));
            }
            if (string.Equals(layerName, "Foreground", StringComparison.Ordinal))
            {
                foreach (var span in layout.foregroundSpans ?? Array.Empty<ForegroundSpanJson>())
                {
                    foreach (var position in EnumerateSpan(layout, span))
                    {
                        positions.Add(position);
                        values.Add(ResolveDeclaredTile(tiles, id, span.token));
                    }
                }
            }
            if (positions.Count > 0)
                tilemap.SetTiles(positions.ToArray(), values.ToArray());
        }

        private static int DeclaredLayerCellCount(LayoutJson layout, string layerName)
        {
            var positions = new HashSet<Vector3Int>(
                (FindLayer(layout, layerName)?.cells ?? Array.Empty<LayoutCellJson>())
                .Select(cell => new Vector3Int(cell.x, cell.y, 0)));
            if (string.Equals(layerName, "Foreground", StringComparison.Ordinal))
                foreach (var span in layout.foregroundSpans ?? Array.Empty<ForegroundSpanJson>())
                    positions.UnionWith(EnumerateSpan(layout, span));
            return positions.Count;
        }

        private static LayoutLayerJson FindLayer(LayoutJson layout, string layerName)
        {
            return (layout.layers ?? Array.Empty<LayoutLayerJson>())
                .SingleOrDefault(layer => string.Equals(layer.name, layerName, StringComparison.Ordinal));
        }

        private static Tile ResolveDeclaredTile(IReadOnlyDictionary<string, Tile> tiles, string id, string token)
        {
            if (string.IsNullOrEmpty(token) || !tiles.TryGetValue($"{id}__{token}", out var tile))
                throw new InvalidOperationException($"'{id}' layout references missing tile '{token}'.");
            return tile;
        }

        private static void ValidateCell(LayoutJson layout, int x, int y, string layerName)
        {
            if (x < 0 || x >= layout.width || y < 0 || y >= layout.height)
                throw new InvalidDataException($"{layout.id} {layerName} cell ({x}, {y}) is out of bounds.");
        }

        private static IEnumerable<Vector3Int> EnumerateSpan(LayoutJson layout, ForegroundSpanJson span)
        {
            ValidateCell(layout, span.fromX, span.fromY, "Foreground span");
            ValidateCell(layout, span.toX, span.toY, "Foreground span");
            var x = span.fromX;
            var y = span.fromY;
            yield return new Vector3Int(x, y, 0);
            while (x != span.toX)
            {
                x += Math.Sign(span.toX - x);
                yield return new Vector3Int(x, y, 0);
            }
            while (y != span.toY)
            {
                y += Math.Sign(span.toY - y);
                yield return new Vector3Int(x, y, 0);
            }
        }

        private static void AddDeclaredColliders(LayoutJson layout, Transform root)
        {
            var collisionRoot = new GameObject("Layout Collisions");
            collisionRoot.transform.SetParent(root);
            var environmentLayer = LayerMask.NameToLayer("Environment");
            if (environmentLayer >= 0) collisionRoot.layer = environmentLayer;
            foreach (var run in layout.collisions ?? Array.Empty<LayoutCollisionJson>())
            {
                if (run.width <= 0 || run.height <= 0)
                    throw new InvalidDataException($"{layout.id} has an empty collision run.");
                ValidateCell(layout, run.x, run.y, "Collision");
                ValidateCell(layout, run.x + run.width - 1, run.y + run.height - 1, "Collision");
                var colliderObject = new GameObject($"Collision_{run.x}_{run.y}");
                colliderObject.transform.SetParent(collisionRoot.transform);
                colliderObject.transform.position = new Vector3(
                    run.x + run.width * 0.5f,
                    run.y + run.height * 0.5f,
                    0f);
                var collider = colliderObject.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(run.width, run.height);
            }
        }

        private static void EnsureSceneFolder(string kind)
        {
            var path = kind == "interior" ? "Assets/Scenes/Interiors" : "Assets/Scenes/Regions";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder("Assets/Scenes", kind == "interior" ? "Interiors" : "Regions");
        }
    }
}
