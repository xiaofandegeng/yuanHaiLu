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
        private const string ResourceRoot = "Art/MVP/";

        private static readonly Dictionary<string, Sprite> Cache =
            new Dictionary<string, Sprite>();

        /// <summary>按稳定 ID 加载持久精灵；缺失时记错误并返回 null（调用方自行跳过视觉）。</summary>
        public static Sprite Load(string spriteId)
        {
            if (string.IsNullOrEmpty(spriteId)) return null;
            if (Cache.TryGetValue(spriteId, out var cached)) return cached;

            var sprite = Resources.Load<Sprite>(ResourceRoot + spriteId);
            if (sprite == null)
                Debug.LogError($"[MvpArt] 缺少持久精灵 {ResourceRoot}{spriteId}");
            Cache[spriteId] = sprite;
            return sprite;
        }
    }
}
