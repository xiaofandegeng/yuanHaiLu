using YuanHaiLu.GameSystem;
using UnityEngine;
using System.Collections.Generic;
using YuanHaiLu.Core;

namespace YuanHaiLu.Character
{
    /// <summary>
    /// 角色升级系统 — 经验获取、升级、属性成长
    /// 挂载到玩家角色上
    /// </summary>
    public class LevelSystem : MonoBehaviour
    {
        [Header("等级设置")]
        [SerializeField] private int maxLevel = 50;
        [SerializeField] private int baseExpRequired = 30;       // 1→2 级所需经验
        [SerializeField] private float expGrowthRate = 1.25f;    // 每级经验增长倍率

        [Header("属性成长（每级）")]
        [SerializeField] private int hpPerLevel = 12;
        [SerializeField] private int mpPerLevel = 5;
        [SerializeField] private int atkPerLevel = 2;
        [SerializeField] private int defPerLevel = 1;
        [SerializeField] private int agiPerLevel = 1;

        [Header("升级奖励")]
        [SerializeField] private int goldPerLevel = 20;

        private CharacterStats _stats;

        // === 事件 ===
        public event System.Action<int> OnLevelUp;               // 新等级
        public event System.Action<int, int> OnExpGained;        // (当前经验, 升级所需)
        public event System.Action<int> OnPendingPoints;         // 可分配点数

        public int PendingPoints { get; private set; } = 0;

        private void Awake()
        {
            _stats = GetComponent<CharacterStats>();
        }

        /// <summary>
        /// 获取当前等级升级所需经验
        /// </summary>
        public int GetExpRequired(int level)
        {
            return Mathf.RoundToInt(baseExpRequired * Mathf.Pow(expGrowthRate, level - 1));
        }

        /// <summary>
        /// 获得经验值
        /// </summary>
        public void GainExp(int amount)
        {
            if (_stats.level >= maxLevel) return;

            _stats.exp += amount;
            OnExpGained?.Invoke(_stats.exp, GetExpRequired(_stats.level));

            Debug.Log($"[升级] 获得 {amount} 经验（{_stats.exp}/{GetExpRequired(_stats.level)}）");

            // 检查升级
            while (_stats.exp >= GetExpRequired(_stats.level) && _stats.level < maxLevel)
            {
                _stats.exp -= GetExpRequired(_stats.level);
                LevelUp();
            }
        }

        /// <summary>
        /// 升级
        /// </summary>
        private void LevelUp()
        {
            _stats.level++;

            // 属性成长
            _stats.maxHp += hpPerLevel;
            _stats.maxMp += mpPerLevel;
            _stats.attack += atkPerLevel;
            _stats.defense += defPerLevel;
            _stats.agility += agiPerLevel;

            // 满血满蓝
            _stats.currentHp = _stats.maxHp;
            _stats.currentMp = _stats.maxMp;

            // 可分配点数
            PendingPoints += 3;

            // 升级特效
            Effects.EffectsManager.LevelUpEffect(transform.position);
            GameSystem.AudioManager.Instance?.PlaySFX(GameSystem.AudioManager.SFX.LEVEL_UP);

            // 金钱奖励
            var inv = GameSystem.InventoryManager.Instance;
            if (inv != null) inv.AddGold(goldPerLevel);

            OnLevelUp?.Invoke(_stats.level);
            OnPendingPoints?.Invoke(PendingPoints);

            Debug.Log($"[升级] 升到 {_stats.level} 级！" +
                      $"HP+{hpPerLevel} MP+{mpPerLevel} ATK+{atkPerLevel} DEF+{defPerLevel} AGI+{agiPerLevel}" +
                      $" 金币+{goldPerLevel}");
        }

        /// <summary>
        /// 分配属性点
        /// </summary>
        public bool AllocatePoint(string statName)
        {
            if (PendingPoints <= 0) return false;

            switch (statName.ToLower())
            {
                case "hp":
                case "maxhp":
                    _stats.maxHp += 5;
                    _stats.currentHp += 5;
                    break;
                case "mp":
                case "maxmp":
                    _stats.maxMp += 3;
                    _stats.currentMp += 3;
                    break;
                case "attack":
                case "atk":
                    _stats.attack += 2;
                    break;
                case "defense":
                case "def":
                    _stats.defense += 2;
                    break;
                case "agility":
                case "agi":
                    _stats.agility += 2;
                    break;
                default:
                    Debug.Log($"[升级] 未知属性: {statName}");
                    return false;
            }

            PendingPoints--;
            OnPendingPoints?.Invoke(PendingPoints);

            Debug.Log($"[升级] 分配 1 点到 {statName}，剩余 {PendingPoints} 点");
            return true;
        }

        // === 存档 ===
        [System.Serializable]
        public class LevelSaveData
        {
            public int pendingPoints;
        }

        public LevelSaveData GetSaveData() => new LevelSaveData { pendingPoints = PendingPoints };

        public void LoadSaveData(LevelSaveData data) => PendingPoints = data.pendingPoints;
    }

}
