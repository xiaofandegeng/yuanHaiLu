using System;
using System.Collections.Generic;
using UnityEngine;

namespace YuanHaiLu.Art
{
    /// <summary>
    /// 模块化 MVP 场景的运行时契约（docs/18 §6.B）：标记场景中的密集像素世界根物体，
    /// 并保有布局 ID 与实际放置的持久模块清单。装配只发生在编辑器侧
    /// （MvpSceneModuleAssembler 按 town.json / inn.json 放置），运行时只读。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MvpWorldModule : MonoBehaviour
    {
        [SerializeField] private string layoutId;
        [SerializeField] private List<string> moduleAssets = new List<string>();

        public string LayoutId => layoutId;
        public IReadOnlyList<string> ModuleAssets => moduleAssets;

        /// <summary>编辑器装配器写入契约数据；运行时禁止改动。</summary>
        public void Configure(string worldLayoutId, IEnumerable<string> placedAssets)
        {
            if (string.IsNullOrEmpty(worldLayoutId))
                throw new ArgumentException("Layout id is required.", nameof(worldLayoutId));
            if (placedAssets == null)
                throw new ArgumentNullException(nameof(placedAssets));

            layoutId = worldLayoutId;
            moduleAssets = new List<string>(placedAssets);
        }
    }
}
