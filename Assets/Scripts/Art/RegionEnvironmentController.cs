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

        /// <summary>
        /// 精确天气档：键必须与 23 个环境 manifest 中声明的 weather ID 完全一致。
        /// 未知 ID 不允许默认猜测，由 <see cref="ConfigureWeather"/> 抛出。
        /// </summary>
        private static readonly IReadOnlyDictionary<string, WeatherProfile> WeatherProfiles =
            new Dictionary<string, WeatherProfile>(StringComparer.Ordinal)
            {
                { "clear",          new WeatherProfile(new Color(1f, 1f, 1f, 0f),       Vector2.zero) },
                { "indoor_ambient", new WeatherProfile(new Color(0.95f, 0.82f, 0.58f, 0.12f), Vector2.zero) },
                { "river_rain",     new WeatherProfile(new Color(0.65f, 0.82f, 1f, 0.28f),  new Vector2(-0.35f, -1.75f)) },
                { "canal_rain",     new WeatherProfile(new Color(0.65f, 0.82f, 1f, 0.28f),  new Vector2(-0.35f, -1.75f)) },
                { "snowfall",       new WeatherProfile(new Color(0.88f, 0.95f, 1f, 0.42f), new Vector2(0.18f, -0.55f)) },
                { "sandstorm",      new WeatherProfile(new Color(0.95f, 0.72f, 0.38f, 0.32f), new Vector2(1.2f, 0.18f)) },
                { "ember_wind",     new WeatherProfile(new Color(0.95f, 0.72f, 0.38f, 0.32f), new Vector2(1.2f, 0.18f)) },
                { "poison_fog",     new WeatherProfile(new Color(0.55f, 0.8f, 0.48f, 0.3f),  new Vector2(0.28f, 0.04f)) },
                { "summit_cloud",   new WeatherProfile(new Color(0.65f, 0.82f, 1f, 0.28f),  new Vector2(0.28f, 0.04f)) },
                { "cloud_mist",     new WeatherProfile(new Color(0.65f, 0.82f, 1f, 0.28f),  new Vector2(0.28f, 0.04f)) },
                { "mountain_fog",   new WeatherProfile(new Color(0.65f, 0.82f, 1f, 0.28f),  new Vector2(0.28f, 0.04f)) },
                { "city_haze",      new WeatherProfile(new Color(0.65f, 0.82f, 1f, 0.28f),  new Vector2(0.28f, 0.04f)) },
            };

        public void ConfigureForEditor(
            bool hasDayNight,
            string formalWeatherId,
            Tilemap effects)
        {
            supportsDayNight = hasDayNight;
            weatherTilemap = effects;
            ConfigureWeather(formalWeatherId);
        }

        /// <summary>
        /// 用精确 manifest 天气 ID 配置天气表现。未知 ID 抛 <see cref="ArgumentException"/>，
        /// 不允许任何默认猜测。
        /// </summary>
        public void ConfigureWeather(string weatherId)
        {
            if (string.IsNullOrEmpty(weatherId) ||
                !WeatherProfiles.TryGetValue(weatherId, out var profile))
                throw new ArgumentException(
                    "Unknown weather id '" + weatherId +
                    "'; expected one of the weather ids declared across the 23 environment manifests.",
                    nameof(weatherId));
            this.weatherId = weatherId;
            weatherVelocity = profile.Velocity;
            ApplyWeatherTint(profile);
        }

        private void Start()
        {
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            tilemaps = GetComponentsInChildren<Tilemap>(true);
            weatherOrigin = weatherTilemap != null ? weatherTilemap.transform.localPosition : Vector3.zero;
            ConfigureWeather(weatherId);
            if (!TryBindTimeManager())
                ApplyPeriod(GameTimeManager.TimePeriod.Morning);
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

        private void ApplyWeatherTint(WeatherProfile profile)
        {
            if (weatherTilemap == null) return;
            weatherTilemap.color = profile.Tint;
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

        /// <summary>一个 manifest 天气 ID 对应的着色与漂移速度。</summary>
        private readonly struct WeatherProfile
        {
            public Color Tint { get; }
            public Vector2 Velocity { get; }

            public WeatherProfile(Color tint, Vector2 velocity)
            {
                Tint = tint;
                Velocity = velocity;
            }
        }
    }
}
