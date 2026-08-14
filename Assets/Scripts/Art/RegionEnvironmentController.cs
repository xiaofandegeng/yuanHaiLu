using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace YuanHaiLu.Art
{
    [Serializable]
    public sealed class EnvironmentTileSwap
    {
        public TileBase normal;
        public TileBase burned;
    }

    [Serializable]
    public sealed class EnvironmentLandmarkSwap
    {
        public SpriteRenderer renderer;
        public Sprite normal;
        public Sprite burned;
    }

    /// <summary>
    /// Replaces only persisted visual assets for a formal region state.  It
    /// deliberately does not recreate the region definition, anchors, or
    /// layout collision objects, so save and travel coordinates remain stable.
    /// </summary>
    public sealed class RegionEnvironmentController : MonoBehaviour
    {
        [SerializeField] private string currentEnvironmentState = "normal";
        [SerializeField] private string currentWeatherId = "clear";
        [SerializeField] private string normalWeatherId = "clear";
        [SerializeField] private string burnedWeatherId = "ember_wind";
        [SerializeField] private Tilemap[] tilemaps = Array.Empty<Tilemap>();
        [SerializeField] private EnvironmentTileSwap[] tileSwaps = Array.Empty<EnvironmentTileSwap>();
        [SerializeField] private EnvironmentLandmarkSwap[] landmarkSwaps = Array.Empty<EnvironmentLandmarkSwap>();

        public string CurrentEnvironmentState => currentEnvironmentState;
        public string CurrentWeatherId => currentWeatherId;

        public void ConfigureForEditor(
            IEnumerable<Tilemap> maps,
            IEnumerable<EnvironmentTileSwap> tiles,
            IEnumerable<EnvironmentLandmarkSwap> landmarks,
            string normalWeather,
            string burnedWeather)
        {
            tilemaps = maps.Where(map => map != null).ToArray();
            // The editor builder has already resolved both sides of every pair.
            // Keep that exact list here: filtering UnityEngine.Object references while
            // serialising a freshly-created scene can discard valid Tile assets before
            // the AssetDatabase finishes its import refresh.
            tileSwaps = tiles.ToArray();
            landmarkSwaps = landmarks.ToArray();
            normalWeatherId = normalWeather;
            burnedWeatherId = burnedWeather;
            currentEnvironmentState = "normal";
            currentWeatherId = normalWeatherId;
        }

        public void SetEnvironmentState(string stateId)
        {
            if (stateId != "normal" && stateId != "burned")
                throw new ArgumentException("Expected normal or burned.", nameof(stateId));
            if (stateId == currentEnvironmentState)
                return;

            var from = stateId == "burned"
                ? tileSwaps.ToDictionary(swap => swap.normal, swap => swap.burned)
                : tileSwaps.ToDictionary(swap => swap.burned, swap => swap.normal);
            foreach (var map in tilemaps)
            {
                if (map == null) continue;
                foreach (var position in map.cellBounds.allPositionsWithin)
                    if (map.GetTile(position) is TileBase tile && from.TryGetValue(tile, out var replacement))
                        map.SetTile(position, replacement);
            }
            foreach (var swap in landmarkSwaps)
                if (swap.renderer != null)
                    swap.renderer.sprite = stateId == "burned" ? swap.burned : swap.normal;

            currentEnvironmentState = stateId;
            currentWeatherId = stateId == "burned" ? burnedWeatherId : normalWeatherId;
        }
    }
}
