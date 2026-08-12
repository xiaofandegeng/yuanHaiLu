using UnityEngine;
using UnityEngine.Tilemaps;

namespace YuanHaiLu.Map
{
    /// <summary>
    /// 瓦片地图管理器 — 负责地图加载、动态瓦片更新、碰撞设置
    /// 挂载到 Tilemap Grid 下
    /// </summary>
    public class TileMapManager : MonoBehaviour
    {
        [Header("瓦片地图引用")]
        [SerializeField] private Tilemap groundTilemap;      // 地面层（不可碰撞）
        [SerializeField] private Tilemap environmentTilemap;  // 环境层（可碰撞，如墙壁）
        [SerializeField] private Tilemap foregroundTilemap;   // 前景层（装饰）
        [SerializeField] private Tilemap collisionTilemap;    // 碰撞层（隐形）

        [Header("区域信息")]
        public string mapName = "烟柳镇";
        public int mapWidth = 30;     // 瓦片数
        public int mapHeight = 20;

        [Header("动态元素")]
        [SerializeField] private Transform dynamicObjectsParent; // 动态物体父节点

        // === 单例（场景级） ===
        public static TileMapManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// 世界坐标转瓦片坐标
        /// </summary>
        public Vector3Int WorldToTile(Vector2 worldPos)
        {
            return groundTilemap.WorldToCell(worldPos);
        }

        /// <summary>
        /// 瓦片坐标转世界坐标（中心点）
        /// </summary>
        public Vector2 TileToWorld(Vector3Int tilePos)
        {
            return groundTilemap.GetCellCenterWorld(tilePos);
        }

        /// <summary>
        /// 检查某位置是否可通行
        /// </summary>
        public bool IsWalkable(Vector2 worldPos)
        {
            Vector3Int tilePos = WorldToTile(worldPos);

            // 检查碰撞层
            if (collisionTilemap != null && collisionTilemap.HasTile(tilePos))
                return false;

            // 检查环境层（墙壁等）
            if (environmentTilemap != null && environmentTilemap.HasTile(tilePos))
            {
                var tile = environmentTilemap.GetTile<Tile>(tilePos);
                if (tile != null && tile.colliderType != Tile.ColliderType.None)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 设置指定位置的瓦片
        /// </summary>
        public void SetTile(Tilemap tilemap, Vector3Int position, TileBase tile)
        {
            if (tilemap != null)
            {
                tilemap.SetTile(position, tile);
            }
        }

        /// <summary>
        /// 获取地图边界
        /// </summary>
        public Bounds GetMapBounds()
        {
            groundTilemap.CompressBounds();
            return groundTilemap.localBounds;
        }
    }
}
