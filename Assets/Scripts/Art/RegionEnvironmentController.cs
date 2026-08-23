using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using YuanHaiLu.GameSystem;

namespace YuanHaiLu.Art
{
    /// <summary>
    /// 正式场景的昼夜与天气表现。只给已导入的正式 Sprite/Tilemap 调色，
    /// 不在运行时创建纹理或降级色块。
    /// </summary>
    public sealed class RegionEnvironmentController : MonoBehaviour
    {
        [SerializeField] private bool supportsDayNight = true;
        [SerializeField] private string weatherId = "clear";
        [SerializeField] private Tilemap weatherTilemap;

        private SpriteRenderer[] spriteRenderers = Array.Empty<SpriteRenderer>();
        private Tilemap[] tilemaps = Array.Empty<Tilemap>();
        private GameTimeManager observedTimeManager;
        private Vector3 weatherOrigin;
        [SerializeField] private Vector2 weatherVelocity;
        private float weatherPhase;

        public bool SupportsDayNight => supportsDayNight;
        public string WeatherId => weatherId;
        public bool IsWeatherAnimated => weatherTilemap != null && weatherVelocity.sqrMagnitude > 0f;

        public void ConfigureForEditor(
            bool hasDayNight,
            string formalWeatherId,
            Tilemap effects)
        {
            supportsDayNight = hasDayNight;
            weatherId = formalWeatherId;
            weatherTilemap = effects;
            ConfigureWeatherMotion();
            ApplyWeatherTint();
        }

        private void Start()
        {
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            tilemaps = GetComponentsInChildren<Tilemap>(true);
            weatherOrigin = weatherTilemap != null ? weatherTilemap.transform.localPosition : Vector3.zero;
            ConfigureWeatherMotion();
            if (!TryBindTimeManager())
                ApplyPeriod(GameTimeManager.TimePeriod.Morning);
            ApplyWeatherTint();
        }

        private void Update()
        {
            if (observedTimeManager == null)
                TryBindTimeManager();
            AnimateWeather(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (observedTimeManager != null)
                observedTimeManager.OnPeriodChanged -= ApplyPeriod;
        }

        private bool TryBindTimeManager()
        {
            var manager = GameTimeManager.Instance;
            if (manager == null)
                return false;
            observedTimeManager = manager;
            observedTimeManager.OnPeriodChanged += ApplyPeriod;
            ApplyPeriod(observedTimeManager.CurrentPeriod);
            return true;
        }

        internal void ApplyPeriod(GameTimeManager.TimePeriod period)
        {
            Color tint = supportsDayNight ? PeriodTint(period) : Color.white;
            foreach (var renderer in spriteRenderers)
            {
                if (renderer == null || renderer.sortingLayerName == "Foreground")
                    continue;
                renderer.color = tint;
            }
            foreach (var tilemap in tilemaps)
            {
                if (tilemap == null || tilemap == weatherTilemap || tilemap.name == "Foreground")
                    continue;
                tilemap.color = tint;
            }
        }

        private void ApplyWeatherTint()
        {
            if (weatherTilemap == null) return;
            Color tint = weatherId.Contains("snow")
                ? new Color(0.88f, 0.95f, 1f, 0.42f)
                : weatherId.Contains("sand") || weatherId.Contains("ember")
                    ? new Color(0.95f, 0.72f, 0.38f, 0.32f)
                    : weatherId.Contains("poison")
                        ? new Color(0.55f, 0.8f, 0.48f, 0.3f)
                        : weatherId.Contains("indoor")
                            ? new Color(0.95f, 0.82f, 0.58f, 0.12f)
                            : new Color(0.65f, 0.82f, 1f, 0.28f);
            weatherTilemap.color = tint;
        }

        private void ConfigureWeatherMotion()
        {
            if (weatherTilemap == null || weatherId.Contains("indoor"))
            {
                weatherVelocity = Vector2.zero;
                return;
            }
            if (weatherId.Contains("rain"))
                weatherVelocity = new Vector2(-0.35f, -1.75f);
            else if (weatherId.Contains("snow"))
                weatherVelocity = new Vector2(0.18f, -0.55f);
            else if (weatherId.Contains("sand") || weatherId.Contains("ember"))
                weatherVelocity = new Vector2(1.2f, 0.18f);
            else
                weatherVelocity = new Vector2(0.28f, 0.04f);
        }

        private void AnimateWeather(float deltaTime)
        {
            if (!IsWeatherAnimated || deltaTime <= 0f) return;
            weatherPhase = Mathf.Repeat(weatherPhase + deltaTime, 4f);
            Vector2 offset = weatherVelocity * weatherPhase;
            // 整个正式天气图层缓慢循环，避免用一排静态水面/装饰冒充天气。
            weatherTilemap.transform.localPosition = weatherOrigin + new Vector3(
                Mathf.Repeat(offset.x + 1f, 2f) - 1f,
                Mathf.Repeat(offset.y + 1f, 2f) - 1f,
                0f);
        }

        private static Color PeriodTint(GameTimeManager.TimePeriod period)
        {
            switch (period)
            {
                case GameTimeManager.TimePeriod.Dawn:
                    return new Color(0.92f, 0.76f, 0.7f, 1f);
                case GameTimeManager.TimePeriod.Dusk:
                    return new Color(0.88f, 0.67f, 0.54f, 1f);
                case GameTimeManager.TimePeriod.Night:
                    return new Color(0.48f, 0.56f, 0.78f, 1f);
                default:
                    return Color.white;
            }
        }
    }
}
