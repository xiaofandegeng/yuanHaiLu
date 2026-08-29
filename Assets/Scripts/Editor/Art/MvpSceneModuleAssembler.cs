using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using YuanHaiLu.Art;
using YuanHaiLu.Core;

namespace YuanHaiLu.Editor
{
    /// <summary>
    /// docs/18 §6.B 模块装配器：按 town.json / inn.json 把 ≤64×64 的持久小模块
    /// 放置进 Demo 场景，替代 docs/17 的三张 480×270 整屏层。每个放置项生成一个
    /// 持久精灵子物体，排序映射固定为 Ground → Default、Environment →
    /// Environment、Foreground → Foreground，角色层仍在后两者之间。
    /// </summary>
    public static class MvpSceneModuleAssembler
    {
        public const string ModuleRootName = "[MVP World]";

        public static MvpWorldModule Assemble(GameObject formalSceneRoot, string layoutId)
        {
            // 冻结正式底稿只作克隆来源，不参与渲染与碰撞（与旧整屏层同约定）。
            if (formalSceneRoot != null)
            {
                foreach (var tilemapRenderer in formalSceneRoot.GetComponentsInChildren<TilemapRenderer>(true))
                    tilemapRenderer.enabled = false;
                foreach (var formalRenderer in formalSceneRoot.GetComponentsInChildren<SpriteRenderer>(true))
                    formalRenderer.enabled = false;
                foreach (var collider in formalSceneRoot.GetComponentsInChildren<Collider2D>(true))
                    collider.enabled = false;
            }

            // 场景重建是整体重生成：先移除上一次装配的世界根，保证幂等。
            var stale = GameObject.Find(ModuleRootName);
            if (stale != null)
                Object.DestroyImmediate(stale);

            var layout = MvpDenseSceneLayouts.Load(layoutId);
            var root = new GameObject(ModuleRootName);
            var module = root.AddComponent<MvpWorldModule>();

            var placedAssets = new List<string>();
            foreach (var placement in layout.Placements)
            {
                var assetPath = MvpDenseSceneLayouts.BakedEnvironmentRoot + placement.asset;
                ConfigureDenseSpriteImporter(assetPath);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (sprite == null)
                    throw new System.InvalidOperationException(
                        "Dense pixel module is missing or not importable: " + assetPath);

                var child = new GameObject($"{placement.role}_{sprite.name}");
                child.transform.SetParent(root.transform);
                child.transform.position = new Vector3(placement.x, placement.y, 0f);
                var renderer = child.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingLayerName = SortingLayerFor(placement.layer);
                renderer.sortingOrder = placement.sortingOrder;
                placedAssets.Add(placement.asset);
            }

            module.Configure(layoutId, placedAssets);
            Debug.Log($"[MvpWorldModule] {layoutId} 装配完成：{placedAssets.Count} 个模块");
            return module;
        }

        /// <summary>布局层名 → 项目 SortingLayer；未知层名直接抛错，不做静默回退。</summary>
        public static string SortingLayerFor(string layoutLayer)
        {
            switch (layoutLayer)
            {
                case "Ground":
                    // 正式场景的底层瓦片历来住在 Default 层（docs/16 起的约定），
                    // 位于 Environment/Character/Foreground 之下，无需迁移冻结场景。
                    return "Default";
                case "Environment":
                    return GameConfig.SORTING_ENVIRONMENT;
                case "Foreground":
                    return GameConfig.SORTING_FOREGROUND;
                default:
                    throw new System.InvalidOperationException(
                        "Unknown dense pixel layout layer: " + layoutLayer);
            }
        }

        /// <summary>密集模块与旧整屏层共用同一像素导入契约（PPU 16、Point、无压缩）。</summary>
        public static void ConfigureDenseSpriteImporter(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                throw new System.InvalidOperationException("Missing TextureImporter: " + assetPath);
            var changed = importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single
                || importer.spritePixelsPerUnit != GameConfig.PIXELS_PER_UNIT
                || importer.filterMode != FilterMode.Point
                || importer.textureCompression != TextureImporterCompression.Uncompressed
                || importer.mipmapEnabled;
            if (!changed) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = GameConfig.PIXELS_PER_UNIT;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
    }
}
