using YuanHaiLu.GameSystem;
using UnityEngine;
using YuanHaiLu.Character;
using YuanHaiLu.Core;

namespace YuanHaiLu.Map
{
    /// <summary>
    /// 传送点 — 场景内传送（如进入建筑、上下楼梯）
    /// 与 AreaTrigger（跨场景）不同，这个是同场景内的瞬移
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class TeleportPoint : MonoBehaviour, IInteractable
    {
        [Header("传送设置")]
        public Transform destination;          // 目标位置
        public bool requireInteract = true;     // 需要按交互键（false=走过即触发）
        public float cooldown = 1f;             // 冷却时间（防止来回传送）

        [Header("效果")]
        public GameObject teleportEffect;       // 传送特效
        public string teleportSfx = "teleport";

        [Header("提示文字")]
        public string promptText = "进入";
        public string targetName = "";          // 如"客栈一楼"

        private Collider2D _col;
        private float _lastTeleportTime;

        private void Awake()
        {
            _col = GetComponent<Collider2D>();
            _col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            if (requireInteract) return;

            TryTeleport(other.gameObject);
        }

        public void OnInteract(GameObject player)
        {
            TryTeleport(player);
        }

        public bool CanInteract()
        {
            return destination != null;
        }

        private void TryTeleport(GameObject player)
        {
            if (destination == null)
            {
                Debug.LogWarning($"[TeleportPoint] 没有设置目标位置: {name}");
                return;
            }

            // 冷却检查
            if (Time.time - _lastTeleportTime < cooldown) return;
            _lastTeleportTime = Time.time;

            // 特效（出发地）
            if (teleportEffect != null)
                Instantiate(teleportEffect, player.transform.position, Quaternion.identity);

            // 音效
            var audioMgr = GameSystem.AudioManager.Instance;
            if (audioMgr != null) audioMgr.PlaySFX(teleportSfx);

            // 瞬移
            player.transform.position = destination.position;

            // 更新摄像机位置（消除跟随延迟）
            var mainCam = Camera.main;
            var camFollow = mainCam != null ? mainCam.GetComponent<CameraFollow>() : null;
            if (camFollow != null)
            {
                camFollow.transform.position = new Vector3(
                    destination.position.x,
                    destination.position.y,
                    camFollow.transform.position.z
                );
            }

            // 特效（目的地）
            if (teleportEffect != null)
                Instantiate(teleportEffect, destination.position, Quaternion.identity);

            Debug.Log($"[TeleportPoint] 传送到 {targetName}");
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (destination != null)
            {
                Gizmos.color = new Color(0.5f, 0f, 1f, 0.5f);
                Gizmos.DrawLine(transform.position, destination.position);
                Gizmos.DrawWireSphere(destination.position, 0.3f);
            }

            // 自身标记
            Gizmos.color = new Color(0.5f, 0f, 1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
#endif
    }
}
