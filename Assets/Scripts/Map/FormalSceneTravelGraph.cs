using System;
using System.Collections.Generic;
using System.Linq;

namespace YuanHaiLu.Map
{
    public readonly struct FormalSceneLink
    {
        public string PortalId { get; }
        public string SourceSceneId { get; }
        public string SourceAnchorId { get; }
        public string TargetSceneId { get; }
        public string TargetAnchorId { get; }

        public FormalSceneLink(
            string portalId,
            string sourceSceneId,
            string sourceAnchorId,
            string targetSceneId,
            string targetAnchorId)
        {
            PortalId = portalId;
            SourceSceneId = sourceSceneId;
            SourceAnchorId = sourceAnchorId;
            TargetSceneId = targetSceneId;
            TargetAnchorId = targetAnchorId;
        }
    }

    /// <summary>
    /// 25 个 Build Settings 场景中 23 个正式区域/室内的确定性转场图。
    /// </summary>
    public static class FormalSceneTravelGraph
    {
        private static readonly string[] RegionOrder =
        {
            "prologue_village", "luoyuan", "tianshu", "yanliu", "cangyue",
            "jueyun", "chisha", "youhuang", "hanyuan", "zhenyue"
        };

        private static readonly IReadOnlyDictionary<string, string[]> InteriorsByRegion =
            new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["prologue_village"] = new[] { "residence" },
                ["luoyuan"] = new[] { "shop" },
                ["tianshu"] = new[] { "academy", "yamen", "palace" },
                ["yanliu"] = new[] { "inn", "pharmacy" },
                ["cangyue"] = new[] { "temple" },
                ["jueyun"] = new[] { "dungeon" },
                ["chisha"] = new[] { "military_camp" },
                ["youhuang"] = new[] { "cave" },
                ["hanyuan"] = new[] { "tomb" },
                ["zhenyue"] = new[] { "ship_cabin" }
            };

        private static readonly FormalSceneLink[] Links = BuildLinks();

        public static IReadOnlyList<FormalSceneLink> All => Links;

        public static IEnumerable<FormalSceneLink> Outgoing(string sceneId)
        {
            return Links.Where(link =>
                string.Equals(link.SourceSceneId, sceneId, StringComparison.Ordinal));
        }

        private static FormalSceneLink[] BuildLinks()
        {
            var links = new List<FormalSceneLink>();
            for (var index = 0; index < RegionOrder.Length; index++)
            {
                string region = RegionOrder[index];
                string previous = RegionOrder[(index - 1 + RegionOrder.Length) % RegionOrder.Length];
                string next = RegionOrder[(index + 1) % RegionOrder.Length];
                links.Add(new FormalSceneLink(
                    "previous_region", region, "entry", previous, "exit"));
                links.Add(new FormalSceneLink(
                    "next_region", region, "exit", next, "entry"));

                foreach (string interior in InteriorsByRegion[region])
                {
                    links.Add(new FormalSceneLink(
                        "enter_" + interior,
                        region,
                        "interior_entry",
                        interior,
                        "entry"));
                    links.Add(new FormalSceneLink(
                        "return_to_" + region,
                        interior,
                        "exit",
                        region,
                        "interior_entry"));
                }
            }
            return links.ToArray();
        }
    }
}
