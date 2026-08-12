using UnityEngine;

namespace YuanHaiLu.Core
{
    /// <summary>
    /// 摄像机跟随 — 像素对齐的平滑跟随
    /// 挂载到主摄像机上（与 PixelPerfectCamera 共存）
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("跟随目标")]
        [SerializeField] private Transform target;

        [Header("跟随参数")]
        [SerializeField] private float smoothSpeed = 5f;       // 平滑速度
        [SerializeField] private bool pixelSnap = true;         // 像素对齐
        [SerializeField] private float pixelsPerUnit = 16f;     // PPU（用于对齐）

        [Header("边界限制")]
        [SerializeField] private bool useBounds = true;
        [SerializeField] private Vector2 minBounds;             // 地图左下角
        [SerializeField] private Vector2 maxBounds;             // 地图右上角

        [Header("震动效果")]
        [SerializeField] private float shakeDecay = 5f;         // 震动衰减速度

        private float _shakeIntensity = 0f;
        private Vector3 _shakeOffset;
        private Camera _camera;

        public void SetTarget(Transform t) => target = t;

        /// <summary>
        /// 触发摄像机震动
        /// </summary>
        public void Shake(float intensity)
        {
            _shakeIntensity = Mathf.Max(_shakeIntensity, intensity);
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                // 自动寻找玩家
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) target = player.transform;
                return;
            }

            // 目标位置
            Vector3 targetPos = new Vector3(
                target.position.x,
                target.position.y,
                transform.position.z
            );

            // 平滑插值
            Vector3 smoothed = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);

            // 震动偏移
            UpdateShake();
            smoothed += _shakeOffset;

            // 像素对齐
            if (pixelSnap)
            {
                smoothed.x = Mathf.Round(smoothed.x * pixelsPerUnit) / pixelsPerUnit;
                smoothed.y = Mathf.Round(smoothed.y * pixelsPerUnit) / pixelsPerUnit;
            }

            // 边界限制
            if (useBounds)
            {
                float halfH = _camera.orthographicSize;
                float halfW = halfH * _camera.aspect;

                smoothed.x = ClampToBounds(smoothed.x, minBounds.x, maxBounds.x, halfW);
                smoothed.y = ClampToBounds(smoothed.y, minBounds.y, maxBounds.y, halfH);
            }

            transform.position = smoothed;
        }

        private static float ClampToBounds(float value, float min, float max, float halfViewSize)
        {
            // (0,0)–(0,0) 是未配置状态；此时不应把摄像机推到视野半径之外。
            if (max <= min) return value;

            float allowedMin = min + halfViewSize;
            float allowedMax = max - halfViewSize;

            // 地图小于当前视野时固定在地图中心，避免反向 Clamp。
            if (allowedMin > allowedMax) return (min + max) * 0.5f;

            return Mathf.Clamp(value, allowedMin, allowedMax);
        }

        private void UpdateShake()
        {
            if (_shakeIntensity > 0.01f)
            {
                _shakeOffset = Random.insideUnitSphere * _shakeIntensity;
                _shakeOffset.z = 0;
                _shakeIntensity = Mathf.Lerp(_shakeIntensity, 0f, shakeDecay * Time.deltaTime);
            }
            else
            {
                _shakeIntensity = 0f;
                _shakeOffset = Vector3.zero;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!useBounds) return;

            // 绘制摄像机边界
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Vector2 center = (minBounds + maxBounds) / 2f;
            Vector2 size = maxBounds - minBounds;
            Gizmos.DrawWireCube(center, size);
        }
#endif
    }
}
