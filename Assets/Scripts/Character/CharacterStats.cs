using UnityEngine;

namespace YuanHaiLu.Character
{
    /// <summary>
    /// 角色属性系统 — 气血、内力、攻击力、防御力等
    /// 同时挂载到玩家、NPC、敌人上
    /// </summary>
    public class CharacterStats : MonoBehaviour
    {
        [Header("=== 基础属性 ===")]
        public string characterName = "未命名";

        [Tooltip("气血（HP）")]
        public int maxHp = 100;
        public int currentHp;

        [Tooltip("内力（MP）")]
        public int maxMp = 50;
        public int currentMp;

        [Tooltip("体力（Stamina）")]
        public int maxStamina = 100;
        public int currentStamina;

        [Header("=== 战斗属性 ===")]
        [Tooltip("攻击力")]
        public int attack = 15;

        [Tooltip("防御力")]
        public int defense = 5;

        [Tooltip("身法（影响闪避率）")]
        public int agility = 10;

        [Tooltip("暴击率（0-100）")]
        [Range(0, 100)]
        public int critRate = 10;

        [Tooltip("暴击伤害倍率")]
        public float critMultiplier = 1.5f;

        [Header("=== 经验/等级 ===")]
        public int level = 1;
        public int exp = 0;
        public int expToNextLevel = 100;

        [Header("=== 状态效果 ===")]
        public bool isPoisoned = false;
        public bool isBleeding = false;
        public bool isStunned = false;
        public bool isInvincible = false;

        // === 事件 ===
        public event System.Action<int, int> OnHpChanged;      // (currentHp, maxHp)
        public event System.Action OnDeath;
        public event System.Action<int> OnDamaged;              // damage amount
        public event System.Action<int> OnHealed;
        public event System.Action<int> OnLevelUp;

        // === 基础值 / 装备加成分离 ===
        // 序列化字段（attack/defense/maxHp 等）作为"基础值"在 Awake 时捕获；
        // 运行时展示的 attack 等于 _base + _eq（装备加成）。
        private int _baseAttack, _baseDefense, _baseAgility, _baseMaxHp, _baseMaxMp;
        private int _eqAttack, _eqDefense, _eqAgility, _eqMaxHp, _eqMaxMp;

        public int BaseAttack => _baseAttack;
        public int BaseDefense => _baseDefense;
        public int BaseAgility => _baseAgility;
        public int BaseMaxHp => _baseMaxHp;
        public int BaseMaxMp => _baseMaxMp;

        private void Awake()
        {
            // 捕获基础值（来自 Inspector 配置）
            _baseAttack = attack;
            _baseDefense = defense;
            _baseAgility = agility;
            _baseMaxHp = maxHp;
            _baseMaxMp = maxMp;

            RecomputeDerived();

            currentHp = maxHp;
            currentMp = maxMp;
            currentStamina = maxStamina;
        }

        /// <summary>
        /// 由基础值 + 装备加成重算派生属性
        /// </summary>
        private void RecomputeDerived()
        {
            attack = _baseAttack + _eqAttack;
            defense = _baseDefense + _eqDefense;
            agility = _baseAgility + _eqAgility;
            maxHp = _baseMaxHp + _eqMaxHp;
            maxMp = _baseMaxMp + _eqMaxMp;
        }

        /// <summary>
        /// 设置装备加成（由 InventoryManager 在装备/卸下时调用）
        /// maxHp/maxMp 的变化会同步调整当前值
        /// </summary>
        public void SetEquipmentBonus(
            int attackBonus,
            int defenseBonus,
            int agilityBonus,
            int maxHpBonus,
            int maxMpBonus,
            bool adjustCurrentResources = true)
        {
            int hpDelta = maxHpBonus - _eqMaxHp;
            int mpDelta = maxMpBonus - _eqMaxMp;

            _eqAttack = attackBonus;
            _eqDefense = defenseBonus;
            _eqAgility = agilityBonus;
            _eqMaxHp = maxHpBonus;
            _eqMaxMp = maxMpBonus;

            RecomputeDerived();

            if (adjustCurrentResources)
            {
                // 正常装备时，上限提升会同步增加当前资源。
                currentHp = Mathf.Clamp(currentHp + Mathf.Max(0, hpDelta), 0, maxHp);
                currentMp = Mathf.Clamp(currentMp + Mathf.Max(0, mpDelta), 0, maxMp);
            }
            else
            {
                // 读档时只恢复上限，不能把装备加成当成治疗。
                currentHp = Mathf.Clamp(currentHp, 0, maxHp);
                currentMp = Mathf.Clamp(currentMp, 0, maxMp);
            }
        }

        /// <summary>
        /// 读档时设置基础值；装备加成由 InventoryManager 随后恢复。
        /// </summary>
        public void SetBaseFromLoad(int atk, int def, int agi, int hp, int mp, int curHp, int curMp)
        {
            _baseAttack = atk;
            _baseDefense = def;
            _baseAgility = agi;
            _baseMaxHp = hp;
            _baseMaxMp = mp;
            _eqAttack = _eqDefense = _eqAgility = _eqMaxHp = _eqMaxMp = 0;

            RecomputeDerived();

            currentHp = Mathf.Clamp(curHp, 0, maxHp);
            currentMp = Mathf.Clamp(curMp, 0, maxMp);
        }

        /// <summary>
        /// 装备上限恢复完成后，再按最终上限精确恢复当前资源。
        /// </summary>
        internal void SetCurrentResourcesFromLoad(int hp, int mp)
        {
            currentHp = Mathf.Clamp(hp, 0, maxHp);
            currentMp = Mathf.Clamp(mp, 0, maxMp);
            OnHpChanged?.Invoke(currentHp, maxHp);
        }

        // === 受伤 ===
        public void TakeDamage(int rawDamage, CharacterStats attacker = null)
        {
            if (isInvincible || currentHp <= 0) return;

            // 闪避判定
            int dodgeChance = agility / 3;
            if (Random.Range(0, 100) < dodgeChance)
            {
                Debug.Log($"[{characterName}] 闪避了攻击！");
                return;
            }

            // 防御减伤
            int finalDamage = Mathf.Max(1, rawDamage - defense);

            // 暴击判定（攻击者）
            if (attacker != null && Random.Range(0, 100) < attacker.critRate)
            {
                finalDamage = Mathf.RoundToInt(finalDamage * attacker.critMultiplier);
                Debug.Log($"[{attacker.characterName}] 暴击！造成 {finalDamage} 伤害");
            }

            currentHp = Mathf.Max(0, currentHp - finalDamage);
            OnDamaged?.Invoke(finalDamage);
            OnHpChanged?.Invoke(currentHp, maxHp);

            Debug.Log($"[{characterName}] 受到 {finalDamage} 伤害，剩余 HP: {currentHp}/{maxHp}");

            if (currentHp <= 0)
            {
                Die();
            }
        }

        // === 治疗 ===
        public void Heal(int amount)
        {
            if (currentHp <= 0) return;

            int healed = Mathf.Min(amount, maxHp - currentHp);
            currentHp += healed;
            OnHealed?.Invoke(healed);
            OnHpChanged?.Invoke(currentHp, maxHp);

            Debug.Log($"[{characterName}] 恢复 {healed} 气血，当前 HP: {currentHp}/{maxHp}");
        }

        // === 内力消耗/恢复 ===
        public bool ConsumeMp(int amount)
        {
            if (currentMp < amount) return false;
            currentMp -= amount;
            return true;
        }

        public void RestoreMp(int amount)
        {
            if (currentHp <= 0) return;
            currentMp = Mathf.Min(currentMp + amount, maxMp);
        }

        // === 体力消耗/恢复 ===
        public bool ConsumeStamina(int amount)
        {
            if (currentStamina < amount) return false;
            currentStamina -= amount;
            return true;
        }

        // === 死亡 ===
        private void Die()
        {
            Debug.Log($"[{characterName}] 已倒下！");
            OnDeath?.Invoke();

            // 简单处理：禁用碰撞和控制器
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            var controller = GetComponent<PlayerController>();
            if (controller != null) controller.SetInputEnabled(false);
        }

        // === 升级 ===
        public void GainExp(int amount)
        {
            exp += amount;
            while (exp >= expToNextLevel)
            {
                exp -= expToNextLevel;
                LevelUp();
            }
        }

        private void LevelUp()
        {
            level++;
            expToNextLevel = Mathf.RoundToInt(expToNextLevel * 1.3f);

            // 基础属性成长（装备加成由 RecomputeDerived 自动叠加）
            _baseMaxHp += 10;
            _baseMaxMp += 5;
            _baseAttack += 3;
            _baseDefense += 2;
            _baseAgility += 1;

            RecomputeDerived();

            currentHp = maxHp;
            currentMp = maxMp;

            OnLevelUp?.Invoke(level);
            Debug.Log($"[{characterName}] 升级！当前等级: {level}");
        }

        /// <summary>
        /// 是否存活
        /// </summary>
        public bool IsAlive => currentHp > 0;
    }
}
