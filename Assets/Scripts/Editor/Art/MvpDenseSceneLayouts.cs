using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace YuanHaiLu.Editor
{
    /// <summary>town.json / inn.json 中的一条模块放置记录（docs/18 §2.2）。</summary>
    [Serializable]
    public sealed class MvpModulePlacement
    {
        public string asset;
        public float x;
        public float y;
        public string layer;
        public int sortingOrder;
        public string role;
    }

    /// <summary>反序列化后的密集像素场景布局。</summary>
    [Serializable]
    public sealed class MvpDenseSceneLayout
    {
        public string scene;
        public List<MvpModulePlacement> placements = new List<MvpModulePlacement>();

        public string SceneId => scene;
        public IReadOnlyList<MvpModulePlacement> Placements => placements;
    }

    /// <summary>
    /// docs/18 dense_pixel 布局 JSON 的唯一读取入口。
    /// 布局以世界坐标书写，保留两个 Demo 场景的既有游戏契约；
    /// 烘焙后的模块精灵位于 BakedEnvironmentRoot 之下。
    /// </summary>
    public static class MvpDenseSceneLayouts
    {
        public const string LayoutRoot = "Assets/ArtSource/MVP/dense_pixel/layouts/";
        public const string BakedEnvironmentRoot = "Assets/Art/MVP/dense_pixel/environment/";

        public static MvpDenseSceneLayout Load(string layoutId)
        {
            if (string.IsNullOrEmpty(layoutId))
                throw new ArgumentException("Layout id is required.", nameof(layoutId));

            var path = LayoutRoot + layoutId + ".json";
            var text = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (text == null)
                throw new InvalidOperationException("Missing dense pixel layout: " + path);

            var layout = JsonUtility.FromJson<MvpDenseSceneLayout>(text.text);
            if (layout == null || string.IsNullOrEmpty(layout.scene))
                throw new InvalidOperationException("Dense pixel layout has no scene id: " + path);
            if (layout.placements == null || layout.placements.Count == 0)
                throw new InvalidOperationException("Dense pixel layout has no placements: " + path);
            return layout;
        }
    }
}
