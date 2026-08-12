using UnityEngine;

namespace YuanHaiLu.Core
{
    /// <summary>
    /// 像素完美摄像机 — 确保像素不模糊、不撕裂
    /// 挂载到主摄像机上
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class PixelPerfectCamera : MonoBehaviour
    {
        [Header("像素设置")]
        [Tooltip("目标内部分辨率宽度")]
        public int targetWidth = GameConfig.NATIVE_WIDTH;   // 480
        [Tooltip("目标内部分辨率高度")]
        public int targetHeight = GameConfig.NATIVE_HEIGHT;  // 270
        [Tooltip("每单位像素数")]
        public int pixelsPerUnit = GameConfig.PIXELS_PER_UNIT; // 16

        [Header("缩放设置")]
        [Tooltip("是否自动适配屏幕")]
        public bool autoScale = true;
        [Tooltip("最小整数缩放倍数")]
        public int minScale = 1;

        private Camera _camera;
        private int _currentScale;
        private int _lastScreenWidth;
        private int _lastScreenHeight;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.orthographic = true;
            UpdateCamera();
        }

        private void Update()
        {
            if (!autoScale) return;

            // 屏幕尺寸变化时重新计算
            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
            {
                UpdateCamera();
            }
        }

        private void UpdateCamera()
        {
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;

            // 计算最大整数缩放倍数
            int scaleX = _lastScreenWidth / targetWidth;
            int scaleY = _lastScreenHeight / targetHeight;
            _currentScale = Mathf.Max(minScale, Mathf.Min(scaleX, scaleY));

            // 设置正交摄像机大小
            // orthographicSize = (屏幕高度 / 2) / (缩放倍数 * PPU)
            float orthoSize = (_lastScreenHeight / 2f) / (_currentScale * (float)pixelsPerUnit);
            _camera.orthographicSize = orthoSize;

            // 关闭抗锯齿（像素游戏不需要）
            QualitySettings.antiAliasing = 0;

            // 设置过滤模式为 Point（最近邻，保持像素锐利）
            // 注意：这需要在纹理导入设置中配置
            // 这里设置摄像器的像素完美 snapping
            _camera.pixelRect = CalculatePixelRect();

            Debug.Log($"[PixelPerfectCamera] Scale: {_currentScale}x | " +
                      $"OrthoSize: {orthoSize:F2} | " +
                      $"Viewport: {_camera.pixelRect}");
        }

        private Rect CalculatePixelRect()
        {
            int viewportWidth = targetWidth * _currentScale;
            int viewportHeight = targetHeight * _currentScale;

            // 居中
            int x = (_lastScreenWidth - viewportWidth) / 2;
            int y = (_lastScreenHeight - viewportHeight) / 2;

            return new Rect(x, y, viewportWidth, viewportHeight);
        }

        /// <summary>
        /// 获取当前缩放倍数
        /// </summary>
        public int GetCurrentScale() => _currentScale;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_camera == null) _camera = GetComponent<Camera>();
            UpdateCamera();
        }
#endif
    }
}
