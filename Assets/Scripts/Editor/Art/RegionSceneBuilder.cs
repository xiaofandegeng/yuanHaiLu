using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YuanHaiLu.Art;
using YuanHaiLu.Core;
using YuanHaiLu.Map;

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
        public Dictionary<string, JArray[]> layers;
        public int[][] collisions;
        public int[][] foregroundSpans;
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
            var metadata = ArtImportRules.ReadMetadataAtPath(MetadataPath(id, layout.kind));
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
                        new Vector2Int(anchor.x, anchor.y))),
                metadata.dayNight,
                metadata.weather);

            var maps = new Dictionary<string, Tilemap>(StringComparer.Ordinal);
            for (var index = 0; index < LayerNames.Length; index++)
            {
                var child = new GameObject(LayerNames[index]);
                child.transform.SetParent(gridObject.transform);
                var tilemap = child.AddComponent<Tilemap>();
                var renderer = child.AddComponent<TilemapRenderer>();
                renderer.sortingOrder = index * 10;
                renderer.sortingLayerName = SortingLayerFor(LayerNames[index]);
                maps[LayerNames[index]] = tilemap;
            }

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

            ApplyDeclaredTileSeeds(maps, tiles, id, layout);

            if (layout.kind == "interior")
                PaintInteriorStructure(maps, tiles, id, layout.width, layout.height);
            else
                PaintRegionStructure(maps, tiles, id, layout.width, layout.height);
            PaintDeclaredForeground(maps["Foreground"], tiles, id, layout);
            PaintWeather(maps["Effects"], tiles, id, layout);

            int groundCellCount = maps["Ground"].GetTilesBlock(
                    new BoundsInt(0, 0, 0, layout.width, layout.height, 1))
                .Count(tile => tile != null);
            if (groundCellCount != layout.width * layout.height)
                throw new InvalidOperationException(
                    $"'{id}' populated {groundCellCount} of {layout.width * layout.height} ground cells.");

            AddLandmarks(id, layout, metadata, gridObject.transform);
            var anchors = AddAnchors(layout, gridObject.transform);
            AddFormalCollision(maps, gridObject.transform, layout);
            AddRuntimeIntegration(id, layout, metadata, maps, anchors, gridObject);
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
            var layout = JsonConvert.DeserializeObject<LayoutJson>(File.ReadAllText(path));
            if (layout == null || string.IsNullOrEmpty(layout.id) || layout.width <= 0 || layout.height <= 0)
                throw new InvalidDataException($"Invalid environment layout '{path}'.");
            ValidateLayoutContract(layout, path);
            return layout;
        }

        private static void ValidateLayoutContract(LayoutJson layout, string path)
        {
            if (layout.layers == null || !layout.layers.ContainsKey("Ground") ||
                !layout.layers.ContainsKey("Buildings"))
                throw new InvalidDataException($"Layout '{path}' must declare Ground and Buildings layers.");
            if (layout.collisions == null || layout.collisions.Length == 0)
                throw new InvalidDataException($"Layout '{path}' must declare collision cells.");
            if (layout.foregroundSpans == null)
                throw new InvalidDataException($"Layout '{path}' must declare foreground spans.");
            foreach (int[] cell in layout.collisions)
            {
                if (cell == null || cell.Length != 2 || cell[0] < 0 || cell[0] >= layout.width ||
                    cell[1] < 0 || cell[1] >= layout.height)
                    throw new InvalidDataException($"Layout '{path}' contains an invalid collision cell.");
            }
            foreach (int[] span in layout.foregroundSpans)
            {
                if (span == null || span.Length != 4 || span[0] < 0 || span[0] >= layout.width ||
                    span[2] < 0 || span[2] >= layout.width || span[1] < 0 ||
                    span[1] >= layout.height || span[3] < 0 || span[3] >= layout.height)
                    throw new InvalidDataException($"Layout '{path}' contains an invalid foreground span.");
            }
        }

        private static string MetadataPath(string id, string kind)
        {
            return kind == "interior"
                ? $"Assets/Art/Environment/Interiors/{id}/{id}_tileset.art.json"
                : $"Assets/Art/Environment/Regions/{id}/{id}_tileset.art.json";
        }

        private static string SortingLayerFor(string layerName)
        {
            if (layerName == "Ground") return "Ground";
            if (layerName == "Character") return "Character";
            if (layerName == "Foreground" || layerName == "Effects") return "Foreground";
            return "Environment";
        }

        private static void AddLandmarks(
            string id,
            LayoutJson layout,
            ArtMetadata metadata,
            Transform root)
        {
            var metadataPath = MetadataPath(id, layout.kind);
            var imagePath = Path.Combine(Path.GetDirectoryName(metadataPath) ?? string.Empty, metadata.landmarkImage).Replace('\\', '/');
            var sprites = AssetDatabase.LoadAllAssetsAtPath(imagePath).OfType<Sprite>()
                .ToDictionary(sprite => sprite.name, StringComparer.Ordinal);
            foreach (var landmarkId in layout.requiredLandmarks ?? Array.Empty<string>())
            {
                var spriteName = $"{id}__landmark__{landmarkId}";
                if (!sprites.TryGetValue(spriteName, out var sprite))
                    throw new InvalidOperationException($"Missing formal landmark '{spriteName}'.");
                var marker = DeclaredLayerCells(layout, "Buildings")
                    .Single(value => string.Equals(
                        value.token,
                        "landmark_" + landmarkId,
                        StringComparison.Ordinal));
                var instance = new GameObject("Landmark_" + landmarkId);
                instance.transform.SetParent(root);
                instance.transform.position = new Vector3(marker.x, marker.y, 0f);
                instance.layer = LayerMask.NameToLayer("Environment");
                var renderer = instance.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingLayerName = "Environment";
                renderer.sortingOrder = 35;

                var landmarkMetadata = metadata.landmarks.First(value => value.name == spriteName);
                if (landmarkMetadata.collision != null &&
                    landmarkMetadata.collision.Length == 4 &&
                    landmarkMetadata.collision[2] > 0 &&
                    landmarkMetadata.collision[3] > 0)
                {
                    var collision = landmarkMetadata.collision;
                    var collider = instance.AddComponent<BoxCollider2D>();
                    collider.size = new Vector2(
                        collision[2] / (float)ArtImportRules.PixelsPerUnit,
                        collision[3] / (float)ArtImportRules.PixelsPerUnit);
                    collider.offset = new Vector2(
                        (collision[0] + collision[2] * 0.5f - sprite.rect.width * 0.5f) /
                        ArtImportRules.PixelsPerUnit,
                        (sprite.rect.height - collision[1] - collision[3] * 0.5f) /
                        ArtImportRules.PixelsPerUnit);
                }

                if (landmarkMetadata.foregroundCut <= 0) continue;
                string foregroundName = spriteName + "__foreground";
                if (!sprites.TryGetValue(foregroundName, out var foregroundSprite))
                    throw new InvalidOperationException(
                        $"Missing formal foreground slice '{foregroundName}'.");
                var foreground = new GameObject("Foreground_" + landmarkId);
                foreground.transform.SetParent(instance.transform);
                foreground.transform.localPosition = new Vector3(
                    0f,
                    (sprite.rect.height - landmarkMetadata.foregroundCut) /
                    ArtImportRules.PixelsPerUnit,
                    0f);
                var foregroundRenderer = foreground.AddComponent<SpriteRenderer>();
                foregroundRenderer.sprite = foregroundSprite;
                foregroundRenderer.sortingLayerName = "Foreground";
            }
        }

        private static Dictionary<string, Transform> AddAnchors(LayoutJson layout, Transform root)
        {
            var anchorRoot = new GameObject("Anchors");
            anchorRoot.transform.SetParent(root);
            var anchors = new Dictionary<string, Transform>(StringComparer.Ordinal);
            foreach (var anchor in layout.anchors ?? Array.Empty<LayoutAnchorJson>())
            {
                var instance = new GameObject(anchor.type + "_" + anchor.id);
                instance.transform.SetParent(anchorRoot.transform);
                instance.transform.position = new Vector3(anchor.x + 0.5f, anchor.y + 0.5f, 0f);
                anchors.Add(anchor.id, instance.transform);
            }
            return anchors;
        }

        private static void AddFormalCollision(
            IReadOnlyDictionary<string, Tilemap> maps,
            Transform root,
            LayoutJson layout)
        {
            var buildingCollider = maps["Buildings"].gameObject.AddComponent<TilemapCollider2D>();
            buildingCollider.gameObject.layer = LayerMask.NameToLayer("Environment");

            var collisionRoot = new GameObject("LayoutCollision");
            collisionRoot.transform.SetParent(root);
            int colliderIndex = 0;
            foreach (var row in layout.collisions
                         .GroupBy(cell => cell[1])
                         .OrderBy(group => group.Key))
            {
                int[] ordered = row.Select(cell => cell[0]).Distinct().OrderBy(x => x).ToArray();
                int runStart = ordered[0];
                int runEnd = runStart;
                for (var index = 1; index <= ordered.Length; index++)
                {
                    if (index < ordered.Length && ordered[index] == runEnd + 1)
                    {
                        runEnd = ordered[index];
                        continue;
                    }

                    var collision = new GameObject("Cells_" + colliderIndex++);
                    collision.transform.SetParent(collisionRoot.transform);
                    collision.transform.position = new Vector3(
                        (runStart + runEnd + 1) * 0.5f,
                        row.Key + 0.5f,
                        0f);
                    collision.layer = LayerMask.NameToLayer("Environment");
                    collision.AddComponent<BoxCollider2D>().size =
                        new Vector2(runEnd - runStart + 1, 1f);
                    if (index < ordered.Length)
                        runStart = runEnd = ordered[index];
                }
            }
        }

        private static void AddRuntimeIntegration(
            string id,
            LayoutJson layout,
            ArtMetadata metadata,
            IReadOnlyDictionary<string, Tilemap> maps,
            IReadOnlyDictionary<string, Transform> anchors,
            GameObject gridObject)
        {
            if (!anchors.TryGetValue("entry", out var defaultSpawn))
                throw new InvalidOperationException($"'{id}' has no entry anchor for runtime bootstrap.");

            var bootstrapObject = new GameObject("SceneBootstrapper");
            var bootstrapper = bootstrapObject.AddComponent<SceneBootstrapper>();
            bootstrapper.ConfigureForEditor(
                id,
                "bgm_" + id,
                defaultSpawn,
                Vector2.zero,
                new Vector2(layout.width, layout.height));

            var environment = gridObject.AddComponent<RegionEnvironmentController>();
            environment.ConfigureForEditor(metadata.dayNight, metadata.weather, maps["Effects"]);

            var portalRoot = new GameObject("TravelPortals");
            var outgoing = FormalSceneTravelGraph.Outgoing(id).ToArray();
            foreach (var group in outgoing.GroupBy(link => link.SourceAnchorId, StringComparer.Ordinal))
            {
                if (!anchors.TryGetValue(group.Key, out var sourceAnchor))
                    throw new InvalidOperationException(
                        $"Travel graph references missing source anchor '{id}/{group.Key}'.");
                var links = group.ToArray();
                for (var index = 0; index < links.Length; index++)
                {
                    var link = links[index];
                    var portal = new GameObject("Portal_" + link.PortalId);
                    portal.transform.SetParent(portalRoot.transform);
                    float offset = (index - (links.Length - 1) * 0.5f) * 1.4f;
                    portal.transform.position = sourceAnchor.position + Vector3.right * offset;
                    var collider = portal.AddComponent<BoxCollider2D>();
                    collider.size = new Vector2(1.1f, 1.1f);
                    collider.isTrigger = true;
                    var area = portal.AddComponent<AreaTrigger>();
                    area.areaName = link.TargetSceneId;
                    area.triggersSceneChange = true;
                    area.targetSceneName = link.TargetSceneId;
                    area.targetAnchorId = link.TargetAnchorId;
                    area.requireInteractForSceneChange = true;
                    area.showOnce = false;
                }
            }
        }

        private static void PaintWeather(
            Tilemap effects,
            IReadOnlyDictionary<string, Tile> tiles,
            string id,
            LayoutJson layout)
        {
            Tile weatherTile;
            if (!TryFindRole(tiles, id, layout.kind == "interior" ? "light" : "water", 0, out weatherTile) &&
                !TryFindRole(tiles, id, layout.kind == "interior" ? "prop" : "decor", 0, out weatherTile))
                return;
            var positions = new List<Vector3Int>();
            var values = new List<TileBase>();
            for (var index = 0; index < 12; index++)
            {
                positions.Add(new Vector3Int(
                    1 + (index * 5) % Math.Max(2, layout.width - 2),
                    1 + (index * 7) % Math.Max(2, layout.height - 2),
                    0));
                values.Add(weatherTile);
            }
            effects.SetTiles(positions.ToArray(), values.ToArray());
        }

        private static void ApplyDeclaredTileSeeds(
            IReadOnlyDictionary<string, Tilemap> maps,
            IReadOnlyDictionary<string, Tile> tiles,
            string id,
            LayoutJson layout)
        {
            foreach (string layerName in new[] { "Ground", "Water" })
            {
                if (!maps.TryGetValue(layerName, out var tilemap)) continue;
                var positions = new List<Vector3Int>();
                var values = new List<TileBase>();
                foreach (var marker in DeclaredLayerCells(layout, layerName))
                {
                    if (!TryParseRoleToken(marker.token, out string role, out int variant) ||
                        !TryFindRole(tiles, id, role, variant, out var tile))
                        throw new InvalidDataException(
                            $"Layout '{id}' references missing tile token '{marker.token}'.");
                    positions.Add(new Vector3Int(marker.x, marker.y, 0));
                    values.Add(tile);
                }
                if (positions.Count > 0)
                    tilemap.SetTiles(positions.ToArray(), values.ToArray());
            }
        }

        private static void PaintDeclaredForeground(
            Tilemap foreground,
            IReadOnlyDictionary<string, Tile> tiles,
            string id,
            LayoutJson layout)
        {
            if (!TryFindRole(tiles, id, layout.kind == "interior" ? "prop" : "roof", 0,
                    out var foregroundTile) &&
                !TryFindRole(tiles, id, "decor", 0, out foregroundTile))
                return;

            var positions = new List<Vector3Int>();
            var values = new List<TileBase>();
            foreach (int[] span in layout.foregroundSpans)
            {
                int dx = Math.Sign(span[2] - span[0]);
                int dy = Math.Sign(span[3] - span[1]);
                int length = Math.Max(Math.Abs(span[2] - span[0]), Math.Abs(span[3] - span[1]));
                for (var index = 0; index <= length; index++)
                {
                    int x = span[0] + dx * index;
                    int y = span[1] + dy * index;
                    positions.Add(new Vector3Int(x, y, 0));
                    values.Add(foregroundTile);
                }
            }
            if (positions.Count > 0)
                foreground.SetTiles(positions.ToArray(), values.ToArray());
        }

        private static IEnumerable<(int x, int y, string token)> DeclaredLayerCells(
            LayoutJson layout,
            string layerName)
        {
            if (layout.layers == null || !layout.layers.TryGetValue(layerName, out var cells) ||
                cells == null)
                yield break;
            foreach (JArray cell in cells)
            {
                if (cell == null || cell.Count != 3 || cell[0].Type != JTokenType.Integer ||
                    cell[1].Type != JTokenType.Integer || cell[2].Type != JTokenType.String)
                    throw new InvalidDataException(
                        $"Layout '{layout.id}' has an invalid {layerName} marker.");
                int x = cell[0].Value<int>();
                int y = cell[1].Value<int>();
                if (x < 0 || x >= layout.width || y < 0 || y >= layout.height)
                    throw new InvalidDataException(
                        $"Layout '{layout.id}' has an out-of-bounds {layerName} marker.");
                yield return (x, y, cell[2].Value<string>());
            }
        }

        private static bool TryParseRoleToken(string token, out string role, out int variant)
        {
            role = null;
            variant = 0;
            if (string.IsNullOrWhiteSpace(token)) return false;
            int separator = token.LastIndexOf('_');
            if (separator <= 0 || separator == token.Length - 1 ||
                !int.TryParse(token.Substring(separator + 1), out variant))
                return false;
            role = token.Substring(0, separator);
            return true;
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
