using UnityEngine;
using System;
using YuanHaiLu.Core;

namespace YuanHaiLu.GameSystem
{
    /// <summary>
    /// 游戏时间系统 — 控制昼夜循环、时间流逝
    /// 挂载到 GameManager 下
    /// </summary>
    public class GameTimeManager : MonoBehaviour
    {
        public static GameTimeManager Instance { get; private set; }

        [Header("时间设置")]
        [Tooltip("游戏内1分钟 = 现实多少秒")]
        [SerializeField] private float timeScale = 10f;     // 10秒=游戏内1分钟
        [SerializeField] private bool autoAdvance = true;    // 自动推进时间
        [SerializeField] private bool pauseInDialogue = true;

        [Header("当前时间")]
        [Range(0, 23)] public int hour = 8;      // 默认早上8点开始
        [Range(0, 59)] public int minute = 0;
        public int day = 1;
        public int month = 1;                    // 月份（1-12）
        public int year = 1;                     // 年份

        [Header("昼夜")]
        [SerializeField] private Color dayColor = new Color(1f, 0.95f, 0.85f);        // 白天
        [SerializeField] private Color sunsetColor = new Color(1f, 0.6f, 0.3f);       // 黄昏
        [SerializeField] private Color nightColor = new Color(0.15f, 0.15f, 0.35f);   // 夜晚
        [SerializeField] private Color dawnColor = new Color(0.9f, 0.7f, 0.5f);       // 黎明
        [SerializeField] private float colorTransitionSpeed = 2f;

        [Header("环境光")]
        [SerializeField] private Light2D globalLight;     // 全局2D光照（如果有）
        [SerializeField] private SpriteRenderer overlaySprite; // 全屏覆盖层（简易昼夜）

        public enum TimePeriod
        {
            Dawn,       // 黎明 5-7
            Morning,    // 上午 8-11
            Afternoon,  // 下午 12-16
            Dusk,       // 黄昏 17-19
            Night       // 夜晚 20-4
        }

        // === 事件 ===
        public event System.Action<TimePeriod> OnPeriodChanged;
        public event System.Action<int, int> OnTimeChanged;    // (hour, minute)
        public event System.Action<int> OnNewDay;              // day number

        private TimePeriod _currentPeriod;
        private float _timeAccumulator;
        private Color _targetColor;
        private bool _isPaused = false;

        public TimePeriod CurrentPeriod => _currentPeriod;
        public string TimeString => $"{hour:D2}:{minute:D2}";
        public string DateString => $"第{year}年{month}月第{day}日";

        // 武侠风日期名
        public string DateStringWuxia
        {
            get
            {
                string[] months = { "正月", "二月", "三月", "四月", "五月", "六月",
                                    "七月", "八月", "九月", "十月", "冬月", "腊月" };
                string monthName = month <= 12 ? months[month - 1] : $"{month}月";
                return $"{monthName}{day}日";
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializePeriod();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (!autoAdvance || _isPaused) return;

            // 对话中暂停时间
            if (pauseInDialogue && GameManager.Instance != null &&
                GameManager.Instance.currentState == GameManager.GameState.Dialogue)
            {
                return;
            }

            _timeAccumulator += Time.deltaTime * timeScale;

            if (_timeAccumulator >= 60f)
            {
                _timeAccumulator -= 60f;
                AdvanceMinute();
            }

            // 平滑过渡环境颜色
            UpdateEnvironmentColor();
        }

        private void AdvanceMinute()
        {
            minute++;
            if (minute >= 60)
            {
                minute = 0;
                hour++;
                if (hour >= 24)
                {
                    hour = 0;
                    AdvanceDay();
                }
            }

            CheckPeriodChange();
            OnTimeChanged?.Invoke(hour, minute);
        }

        private void AdvanceDay()
        {
            day++;
            if (day > 30) // 简化：每月30天
            {
                day = 1;
                month++;
                if (month > 12)
                {
                    month = 1;
                    year++;
                }
            }

            OnNewDay?.Invoke(day);
            Debug.Log($"[GameTime] 新的一天：{DateStringWuxia}");
        }

        private void CheckPeriodChange()
        {
            TimePeriod newPeriod = GetPeriod(hour);

            if (newPeriod != _currentPeriod)
            {
                _currentPeriod = newPeriod;
                OnPeriodChanged?.Invoke(_currentPeriod);

                // 更新目标颜色
                _targetColor = ColorForPeriod(newPeriod);

                Debug.Log($"[GameTime] 时段变化: {_currentPeriod} ({TimeString})");
            }
        }

        private void InitializePeriod()
        {
            _currentPeriod = GetPeriod(hour);
            _targetColor = ColorForPeriod(_currentPeriod);
        }

        private Color ColorForPeriod(TimePeriod period)
        {
            return period switch
            {
                TimePeriod.Dawn => dawnColor,
                TimePeriod.Morning => dayColor,
                TimePeriod.Afternoon => dayColor,
                TimePeriod.Dusk => sunsetColor,
                TimePeriod.Night => nightColor,
                _ => dayColor
            };
        }

        public static TimePeriod GetPeriod(int h)
        {
            return h switch
            {
                >= 5 and < 8 => TimePeriod.Dawn,
                >= 8 and < 12 => TimePeriod.Morning,
                >= 12 and < 17 => TimePeriod.Afternoon,
                >= 17 and < 20 => TimePeriod.Dusk,
                _ => TimePeriod.Night
            };
        }

        private void UpdateEnvironmentColor()
        {
            if (overlaySprite != null)
            {
                Color current = overlaySprite.color;
                Color target = new Color(_targetColor.r, _targetColor.g, _targetColor.b, 0f);

                // 夜晚和黄昏时覆盖层更明显
                if (_currentPeriod == TimePeriod.Night)
                    target.a = 0.4f;
                else if (_currentPeriod == TimePeriod.Dusk)
                    target.a = 0.2f;
                else if (_currentPeriod == TimePeriod.Dawn)
                    target.a = 0.15f;
                else
                    target.a = 0f;

                overlaySprite.color = Color.Lerp(current, target, colorTransitionSpeed * Time.deltaTime);
            }
        }

        // === 外部控制 ===

        /// <summary>
        /// 设置时间（跳转）
        /// </summary>
        public void SetTime(int h, int m)
        {
            hour = h;
            minute = m;
            CheckPeriodChange();
            OnTimeChanged?.Invoke(hour, minute);
        }

        /// <summary>
        /// 推进指定小时
        /// </summary>
        public void AdvanceHours(int hours)
        {
            for (int i = 0; i < hours * 60; i++)
            {
                AdvanceMinute();
            }
        }

        /// <summary>
        /// 暂停/恢复时间流逝
        /// </summary>
        public void SetPaused(bool paused) => _isPaused = paused;

        /// <summary>
        /// 等待到指定时段（用于任务系统）
        /// </summary>
        public void WaitUntil(TimePeriod period)
        {
            while (_currentPeriod != period)
            {
                AdvanceMinute();
            }
        }

        // === 存档 ===
        [System.Serializable]
        public class TimeSaveData
        {
            public int hour, minute, day, month, year;
        }

        public TimeSaveData GetSaveData()
        {
            return new TimeSaveData { hour = hour, minute = minute, day = day, month = month, year = year };
        }

        public void LoadSaveData(TimeSaveData data)
        {
            hour = data.hour;
            minute = data.minute;
            day = data.day;
            month = data.month;
            year = data.year;
            CheckPeriodChange();
        }
    }

    /// <summary>
    /// 简易 2D 光照组件（如果没有 URP 2D Lights）
    /// </summary>
    public class Light2D : MonoBehaviour
    {
        public Color color = Color.white;
        public float intensity = 1f;
        public float radius = 5f;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(color.r, color.g, color.b, 0.2f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
