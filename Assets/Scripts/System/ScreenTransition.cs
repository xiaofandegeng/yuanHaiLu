using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using YuanHaiLu.Core;

namespace YuanHaiLu.GameSystem
{
    /// <summary>
    /// 屏幕过渡效果 — 场景切换/死亡/复活时的淡入淡出
    /// 挂载到全局 Canvas 下
    /// </summary>
    public class ScreenTransition : MonoBehaviour
    {
        public static ScreenTransition Instance { get; private set; }

        [Header("过渡设置")]
        [SerializeField] private float defaultFadeDuration = 0.5f;
        [SerializeField] private Color fadeColor = Color.black;

        private UnityEngine.UI.Image _overlay;
        private bool _isTransitioning = false;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            CreateOverlay();
        }

        private void CreateOverlay()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 9998;
                gameObject.AddComponent<CanvasScaler>();
            }

            var overlayObj = new GameObject("TransitionOverlay");
            overlayObj.transform.SetParent(transform, false);

            var rt = overlayObj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _overlay = overlayObj.AddComponent<UnityEngine.UI.Image>();
            _overlay.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
            _overlay.raycastTarget = false;
        }

        /// <summary>
        /// 淡入（从黑到透明）
        /// </summary>
        public void FadeIn(System.Action onComplete = null)
        {
            if (_isTransitioning) return;
            StartCoroutine(DoFade(1f, 0f, defaultFadeDuration, onComplete));
        }

        /// <summary>
        /// 淡出（从透明到黑）
        /// </summary>
        public void FadeOut(System.Action onComplete = null)
        {
            if (_isTransitioning) return;
            StartCoroutine(DoFade(0f, 1f, defaultFadeDuration, onComplete));
        }

        /// <summary>
        /// 闪烁（快速淡出再淡入）
        /// </summary>
        public void Flash(float duration = 0.3f, System.Action onMidpoint = null)
        {
            StartCoroutine(DoFlash(duration, onMidpoint));
        }

        /// <summary>
        /// 渐变到指定颜色
        /// </summary>
        public void FadeToColor(Color targetColor, float duration, System.Action onComplete = null)
        {
            if (_isTransitioning) return;
            StartCoroutine(DoFadeToColor(targetColor, duration, onComplete));
        }

        /// <summary>
        /// 区域名称提示（淡入显示，停留，淡出）
        /// </summary>
        public void ShowAreaName(string areaName, string subtitle = "", float displayDuration = 2.5f)
        {
            StartCoroutine(DoShowAreaName(areaName, subtitle, displayDuration));
        }

        // === 内部实现 ===

        private IEnumerator DoFade(float fromAlpha, float toAlpha, float duration, System.Action onComplete)
        {
            _isTransitioning = true;
            _overlay.raycastTarget = true;

            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime; // 不受暂停影响
                float t = timer / duration;
                float alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
                _overlay.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
                yield return null;
            }

            _overlay.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, toAlpha);
            _overlay.raycastTarget = toAlpha > 0.5f;

            _isTransitioning = false;
            onComplete?.Invoke();
        }

        private IEnumerator DoFlash(float duration, System.Action onMidpoint)
        {
            _isTransitioning = true;

            float halfDur = duration * 0.5f;

            // 淡出
            yield return DoFade(0f, 1f, halfDur, onMidpoint);
            // 淡入
            yield return DoFade(1f, 0f, halfDur, null);

            _isTransitioning = false;
        }

        private IEnumerator DoFadeToColor(Color target, float duration, System.Action onComplete)
        {
            _isTransitioning = true;
            Color start = _overlay.color;

            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                float t = timer / duration;
                _overlay.color = Color.Lerp(start, target, t);
                yield return null;
            }

            _overlay.color = target;
            _isTransitioning = false;
            onComplete?.Invoke();
        }

        private IEnumerator DoShowAreaName(string areaName, string subtitle, float displayDuration)
        {
            // 创建区域名称UI
            var nameObj = new GameObject("AreaNameDisplay");
            nameObj.transform.SetParent(transform, false);

            // 背景
            var bgRT = nameObj.AddComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0.1f, 0.6f);
            bgRT.anchorMax = new Vector2(0.9f, 0.8f);
            var bg = nameObj.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(0, 0, 0, 0f);

            // 区域名
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(nameObj.transform, false);
            var titleRT = titleObj.AddComponent<RectTransform>();
            titleRT.anchorMin = Vector2.zero;
            titleRT.anchorMax = Vector2.one;
            titleRT.offsetMin = Vector2.zero;
            titleRT.offsetMax = Vector2.zero;
            var titleText = titleObj.AddComponent<UnityEngine.UI.Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 28;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(1f, 0.85f, 0.3f, 0f);
            titleText.text = areaName;

            // 副标题
            var subObj = new GameObject("Subtitle");
            subObj.transform.SetParent(nameObj.transform, false);
            var subRT = subObj.AddComponent<RectTransform>();
            subRT.anchorMin = new Vector2(0f, 0f);
            subRT.anchorMax = new Vector2(1f, 0.4f);
            subRT.offsetMin = Vector2.zero;
            subRT.offsetMax = Vector2.zero;
            var subText = subObj.AddComponent<UnityEngine.UI.Text>();
            subText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            subText.fontSize = 14;
            subText.alignment = TextAnchor.MiddleCenter;
            subText.color = new Color(0.8f, 0.8f, 0.8f, 0f);
            subText.text = subtitle;

            // 动画：淡入 → 停留 → 淡出
            float fadeIn = 0.8f;
            float fadeOut = 1f;

            // 淡入
            float t = 0f;
            while (t < fadeIn)
            {
                t += Time.unscaledDeltaTime;
                float a = t / fadeIn;
                bg.color = new Color(0, 0, 0, a * 0.5f);
                titleText.color = new Color(1f, 0.85f, 0.3f, a);
                subText.color = new Color(0.8f, 0.8f, 0.8f, a);
                yield return null;
            }

            // 停留
            yield return new WaitForSecondsRealtime(displayDuration);

            // 淡出
            t = 0f;
            while (t < fadeOut)
            {
                t += Time.unscaledDeltaTime;
                float a = 1f - t / fadeOut;
                bg.color = new Color(0, 0, 0, a * 0.5f);
                titleText.color = new Color(1f, 0.85f, 0.3f, a);
                subText.color = new Color(0.8f, 0.8f, 0.8f, a);
                yield return null;
            }

            Destroy(nameObj);
        }
    }
}
