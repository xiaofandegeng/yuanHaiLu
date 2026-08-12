using UnityEngine;
using System.Collections;

namespace YuanHaiLu.GameSystem
{
    /// <summary>
    /// 音频管理器 — BGM + 音效播放
    /// 单例，挂载到 AudioManager 空物体上
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("音频源")]
        [SerializeField] private AudioSource bgmSource;      // 背景音乐
        [SerializeField] private AudioSource sfxSource;      // 音效（单通道）
        [SerializeField] private int sfxPoolSize = 5;        // 音效池大小

        [Header("音量")]
        [Range(0f, 1f)] [SerializeField] private float bgmVolume = 0.6f;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.8f;

        [Header("淡入淡出")]
        [SerializeField] private float bgmFadeDuration = 1.0f;

        // 音效池
        private AudioSource[] _sfxPool;
        private int _sfxPoolIndex = 0;

        // 当前BGM
        private string _currentBgmId = "";

        // 预加载的音频
        private System.Collections.Generic.Dictionary<string, AudioClip> _bgmCache = new();
        private System.Collections.Generic.Dictionary<string, AudioClip> _sfxCache = new();

        // === 事件 ===
        public event System.Action<string> OnBgmChanged;
        public event System.Action<string> OnSfxPlayed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 初始化音频源
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
            }
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.volume = bgmVolume;

            // 创建音效池
            _sfxPool = new AudioSource[sfxPoolSize];
            for (int i = 0; i < sfxPoolSize; i++)
            {
                GameObject sfxObj = new GameObject($"SFX_{i}");
                sfxObj.transform.SetParent(transform);
                _sfxPool[i] = sfxObj.AddComponent<AudioSource>();
                _sfxPool[i].playOnAwake = false;
                _sfxPool[i].volume = sfxVolume;
            }
        }

        // === BGM ===

        /// <summary>
        /// 播放背景音乐（带淡入淡出）
        /// </summary>
        public void PlayBGM(string bgmId, float fadeTime = -1f)
        {
            if (bgmId == _currentBgmId) return;

            float fade = fadeTime < 0 ? bgmFadeDuration : fadeTime;

            if (bgmSource.isPlaying && fade > 0)
            {
                StartCoroutine(CrossFadeBGM(bgmId, fade));
            }
            else
            {
                AudioClip clip = LoadBGM(bgmId);
                if (clip != null)
                {
                    bgmSource.clip = clip;
                    bgmSource.volume = bgmVolume;
                    bgmSource.Play();
                    _currentBgmId = bgmId;
                    OnBgmChanged?.Invoke(bgmId);
                }
            }
        }

        private IEnumerator CrossFadeBGM(string newBgmId, float duration)
        {
            AudioClip newClip = LoadBGM(newBgmId);
            if (newClip == null) yield break;

            float halfDuration = duration / 2f;

            // 淡出当前BGM
            float startVol = bgmSource.volume;
            float timer = 0f;
            while (timer < halfDuration)
            {
                timer += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(startVol, 0f, timer / halfDuration);
                yield return null;
            }

            // 切换
            bgmSource.Stop();
            bgmSource.clip = newClip;
            bgmSource.Play();
            _currentBgmId = newBgmId;

            // 淡入新BGM
            timer = 0f;
            while (timer < halfDuration)
            {
                timer += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(0f, bgmVolume, timer / halfDuration);
                yield return null;
            }

            bgmSource.volume = bgmVolume;
            OnBgmChanged?.Invoke(newBgmId);
        }

        public void StopBGM(float fadeTime = 1f)
        {
            if (!bgmSource.isPlaying) return;
            StartCoroutine(FadeOutBGM(fadeTime));
        }

        private IEnumerator FadeOutBGM(float duration)
        {
            float startVol = bgmSource.volume;
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(startVol, 0f, timer / duration);
                yield return null;
            }
            bgmSource.Stop();
            _currentBgmId = "";
        }

        // === 音效 ===

        /// <summary>
        /// 播放音效
        /// </summary>
        public void PlaySFX(string sfxId, float volumeScale = 1f, float pitch = 1f)
        {
            AudioClip clip = LoadSFX(sfxId);
            if (clip == null) return;

            // 从池中取一个空闲的AudioSource
            AudioSource source = _sfxPool[_sfxPoolIndex];
            _sfxPoolIndex = (_sfxPoolIndex + 1) % _sfxPool.Length;

            source.pitch = pitch;
            source.PlayOneShot(clip, sfxVolume * volumeScale);
            OnSfxPlayed?.Invoke(sfxId);
        }

        /// <summary>
        /// 播放带随机音高的音效（避免重复感）
        /// </summary>
        public void PlaySFXRandomPitch(string sfxId, float minPitch = 0.9f, float maxPitch = 1.1f)
        {
            PlaySFX(sfxId, 1f, Random.Range(minPitch, maxPitch));
        }

        // === 音量控制 ===

        public void SetBGMVolume(float vol)
        {
            bgmVolume = Mathf.Clamp01(vol);
            bgmSource.volume = bgmVolume;
        }

        public void SetSFXVolume(float vol)
        {
            sfxVolume = Mathf.Clamp01(vol);
            foreach (var src in _sfxPool)
            {
                src.volume = sfxVolume;
            }
        }

        public float GetBGMVolume() => bgmVolume;
        public float GetSFXVolume() => sfxVolume;

        // === 资源加载 ===

        private AudioClip LoadBGM(string bgmId)
        {
            if (_bgmCache.TryGetValue(bgmId, out AudioClip clip))
                return clip;

            clip = Resources.Load<AudioClip>($"Audio/BGM/{bgmId}");
            if (clip != null)
            {
                _bgmCache[bgmId] = clip;
            }
            else
            {
                // 缓存缺失结果，避免移动脚步等高频调用每次都刷同一条警告。
                _bgmCache[bgmId] = null;
                Debug.LogWarning($"[Audio] BGM未找到: {bgmId}");
            }
            return clip;
        }

        private AudioClip LoadSFX(string sfxId)
        {
            if (_sfxCache.TryGetValue(sfxId, out AudioClip clip))
                return clip;

            clip = Resources.Load<AudioClip>($"Audio/SFX/{sfxId}");
            if (clip != null)
            {
                _sfxCache[sfxId] = clip;
            }
            else
            {
                // 缓存缺失结果，避免移动脚步等高频调用每次都刷同一条警告。
                _sfxCache[sfxId] = null;
                Debug.LogWarning($"[Audio] 音效未找到: {sfxId}");
            }
            return clip;
        }

        // === 区域音乐配置 ===
        // 可以在 AreaTrigger 中调用

        /// <summary>
        /// 区域BGM映射（可序列化配置）
        /// </summary>
        [System.Serializable]
        public class AreaBGMEntry
        {
            public string areaName;
            public string bgmId;
        }

        [SerializeField] private AreaBGMEntry[] areaBgmMappings;

        public void PlayAreaBGM(string areaName)
        {
            foreach (var entry in areaBgmMappings)
            {
                if (entry.areaName == areaName)
                {
                    PlayBGM(entry.bgmId);
                    return;
                }
            }
        }

        // === 音效ID常量（方便代码调用） ===
        public static class SFX
        {
            // 战斗
            public const string SWORD_SLASH = "sword_slash";
            public const string SWORD_HIT = "sword_hit";
            public const string CRIT_HIT = "crit_hit";
            public const string DODGE = "dodge";
            public const string DASH = "dash";
            public const string ENEMY_DEATH = "enemy_death";
            public const string PLAYER_HURT = "player_hurt";
            public const string BLOCK = "block";

            // UI
            public const string UI_CLICK = "ui_click";
            public const string UI_OPEN = "ui_open";
            public const string UI_CLOSE = "ui_close";
            public const string UI_SELECT = "ui_select";
            public const string UI_ERROR = "ui_error";

            // 交互
            public const string PICKUP_ITEM = "pickup_item";
            public const string EQUIP = "equip";
            public const string LEVEL_UP = "level_up";
            public const string QUEST_ACCEPT = "quest_accept";
            public const string QUEST_COMPLETE = "quest_complete";

            // 环境
            public const string FOOTSTEP_GRASS = "footstep_grass";
            public const string FOOTSTEP_STONE = "footstep_stone";
            public const string FOOTSTEP_WATER = "footstep_water";
            public const string DOOR_OPEN = "door_open";
            public const string CHEST_OPEN = "chest_open";

            // 天气
            public const string RAIN = "rain";
            public const string WIND = "wind";
            public const string THUNDER = "thunder";
        }
    }
}
