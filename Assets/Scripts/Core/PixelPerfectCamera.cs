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
        private Camera _clearCamera;
        private int _currentScale;
        private int _lastScreenWidth;
        private int _lastScreenHeight;
        private RenderTexture _lastTargetTexture;

        private const string ClearCameraName = "[PixelPerfectBackground]";

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.orthographic = true;
            EnsureClearCamera();
            UpdateCamera();
        }

        private void OnEnable()
        {
            if (_camera == null) _camera = GetComponent<Camera>();
            EnsureClearCamera();
        }

        private void OnDisable()
        {
            DestroyClearCamera();
        }

        private void Update()
        {
            if (!autoScale) return;

            // 屏幕或渲染目标（离屏 RT/截图工具）变化时重新计算
            var target = _camera != null ? _camera.targetTexture : null;
            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight ||
                target != _lastTargetTexture)
            {
                UpdateCamera();
            }
        }

        private void UpdateCamera()
        {
            var target = _camera.targetTexture;
            _lastTargetTexture = target;
            // 渲染到离屏 RT 时，逻辑尺寸取 RT 的像素尺寸。
            UpdateCameraForScreen(
                target != null ? target.width : Screen.width,
                target != null ? target.height : Screen.height);
        }

        /// <summary>
        /// 以注入的窗口尺寸刷新相机（docs/16 C.1 契约与测试缝）。
        /// 世界正交尺寸只由逻辑画面决定：targetHeight / (2 × PPU) = 8.4375；
        /// 窗口尺寸与整数倍率只决定 pixelRect 的整数缩放与居中，
        /// 不得扩大或缩小世界覆盖。窗口不足 1× 时保持 1×（安全降级=裁剪显示）。
        /// </summary>
        public void UpdateCameraForScreen(int screenWidth, int screenHeight)
        {
            if (_camera == null) _camera = GetComponent<Camera>();
            ConfigureClearCamera();

            _lastScreenWidth = screenWidth;
            _lastScreenHeight = screenHeight;

            // 计算最大整数缩放倍数
            int scaleX = _lastScreenWidth / targetWidth;
            int scaleY = _lastScreenHeight / targetHeight;
            _currentScale = Mathf.Max(minScale, Mathf.Min(scaleX, scaleY));

            // 世界正交尺寸恒定（docs/16 C.1：旧公式用屏幕高参与计算，
            // 大窗口下 OrthoSize 膨胀到 13.31，世界被缩成地图缩略图）。
            float orthoSize = targetHeight / (2f * pixelsPerUnit);
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

        /// <summary>
        /// 像素摄像机只渲染居中的整数倍视口；先用一个无内容摄像机清空整个屏幕，
        /// 避免切换场景后视口外残留上一帧画面。
        /// </summary>
        private void EnsureClearCamera()
        {
            if (_camera == null) return;

            if (_clearCamera == null)
            {
                Transform existing = transform.Find(ClearCameraName);
                if (existing != null)
                {
                    _clearCamera = existing.GetComponent<Camera>();
                }
                else
                {
                    var clearObject = new GameObject(ClearCameraName)
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    };
                    clearObject.transform.SetParent(transform, false);
                    _clearCamera = clearObject.AddComponent<Camera>();
                }
            }

            ConfigureClearCamera();
        }

        private void ConfigureClearCamera()
        {
            if (_camera == null || _clearCamera == null) return;

            _clearCamera.clearFlags = CameraClearFlags.SolidColor;
            _clearCamera.backgroundColor = _camera.backgroundColor;
            _clearCamera.cullingMask = 0;
            _clearCamera.depth = _camera.depth - 1f;
            _clearCamera.rect = new Rect(0f, 0f, 1f, 1f);
            _clearCamera.targetDisplay = _camera.targetDisplay;
            _clearCamera.allowHDR = false;
            _clearCamera.allowMSAA = false;
            _clearCamera.useOcclusionCulling = false;
        }

        private void DestroyClearCamera()
        {
            if (_clearCamera == null) return;

            GameObject clearObject = _clearCamera.gameObject;
            _clearCamera = null;
            if (Application.isPlaying)
                Destroy(clearObject);
            else
                DestroyImmediate(clearObject);
        }

        private Rect CalculatePixelRect()
        {
            return CalculateViewportRect(
                _lastScreenWidth, _lastScreenHeight, targetWidth, targetHeight, _currentScale);
        }

        /// <summary>
        /// 居中的整数倍视口矩形（纯函数，docs/16 C.1 测试缝）。
        /// 注意 Camera.pixelRect 赋值会被实际屏幕钳制，居中数学以本函数为准。
        /// </summary>
        public static Rect CalculateViewportRect(
            int screenWidth, int screenHeight, int targetWidth, int targetHeight, int scale)
        {
            int viewportWidth = targetWidth * scale;
            int viewportHeight = targetHeight * scale;

            // 居中
            int x = (screenWidth - viewportWidth) / 2;
            int y = (screenHeight - viewportHeight) / 2;

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
