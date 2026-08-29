using System.Collections.Generic;
using UnityEngine;

namespace YuanHaiLu.Art
{
    /// <summary>
    /// 单主角 MVP 专属持久精灵的唯一运行时入口（docs/15）。
    /// 精灵资产位于 Assets/Resources/Art/MVP/，提前烘焙提交，
    /// 运行时禁止再以 Texture2D/Sprite.Create 生成任何弹体/武器图。
    /// </summary>
    public static class MvpArtCatalog
    {
        // docs/18：掌柜/水匪/荷包优先使用 dense_pixel/actors 下的 48px/16px
        // 密集调色板演员；掉落物、武器与弹体小图仍在旧根目录，回退加载。
        private static readonly string[] ResourceRoots =
        {
            "Art/MVP/dense_pixel/actors/",
            "Art/MVP/",
        };

        private static readonly Dictionary<string, Sprite> Cache =
            new Dictionary<string, Sprite>();

        /// <summary>按稳定 ID 加载持久精灵；缺失时记错误并返回 null（调用方自行跳过视觉）。</summary>
        public static Sprite Load(string spriteId)
        {
            if (string.IsNullOrEmpty(spriteId)) return null;
            if (Cache.TryGetValue(spriteId, out var cached)) return cached;

            Sprite sprite = null;
            foreach (var root in ResourceRoots)
            {
                sprite = Resources.Load<Sprite>(root + spriteId);
                if (sprite != null) break;
            }
            if (sprite == null)
                Debug.LogError($"[MvpArt] 缺少持久精灵（已查找 dense_pixel/actors 与根目录）: {spriteId}");
            Cache[spriteId] = sprite;
            return sprite;
        }
    }
}
