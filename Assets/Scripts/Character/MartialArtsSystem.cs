using UnityEngine;
using System.Collections.Generic;
using YuanHaiLu.Core;

namespace YuanHaiLu.Character
{
    /// <summary>
    /// 武学技能系统 — 武侠游戏的灵魂
    /// 管理已学招式、释放、冷却
    /// 挂载到玩家角色上
    /// </summary>
    public class MartialArtsSystem : MonoBehaviour
    {
        [Header("武学设置")]
        [SerializeField] private int maxSkillSlots = 4;         // 同时装备的招式数
        [SerializeField] private float globalCooldown = 0.5f;   // 全局冷却

        // 已学会的全部招式
        private Dictionary<string, MartialSkill> _learnedSkills = new Dictionary<string, MartialSkill>();
        // 当前装备的招式（快捷键绑定）
        private MartialSkill[] _equippedSkills;
        // 冷却计时
        private Dictionary<string, float> _cooldowns = new Dictionary<string, float>();

        private CharacterStats _stats;
        private float _globalCooldownTimer;

        // === 事件 ===
        public event System.Action<MartialSkill> OnSkillLearned;
        public event System.Action<MartialSkill, int> OnSkillEquipped; // (skill, slotIndex)
        public event System.Action<MartialSkill> OnSkillUsed;
        public event System.Action<string, float> OnSkillCooldownUpdate; // (skillId, remaining)

        public MartialSkill[] EquippedSkills => _equippedSkills;
        public Dictionary<string, MartialSkill> LearnedSkills => _learnedSkills;

        private void Awake()
        {
            _stats = GetComponent<CharacterStats>();
            _equippedSkills = new MartialSkill[maxSkillSlots];
        }

        private void Update()
        {
            // 更新冷却
            List<string> expiredCooldowns = new List<string>();
            // 先拷贝键集合再遍历：Unity Mono 下迭代中用索引器写回会使枚举器失效。
            var cooldownKeys = new List<string>(_cooldowns.Keys);
            foreach (var skillId in cooldownKeys)
            {
                if (_cooldowns[skillId] > 0)
                {
                    _cooldowns[skillId] -= Time.deltaTime;
                    OnSkillCooldownUpdate?.Invoke(skillId, _cooldowns[skillId]);
                }
                else
                {
                    expiredCooldowns.Add(skillId);
                }
            }
            foreach (var key in expiredCooldowns)
                _cooldowns.Remove(key);

            if (_globalCooldownTimer > 0)
                _globalCooldownTimer -= Time.deltaTime;

            // 仅在可行动状态（探索/战斗）读取技能快捷键，
            // 避免与对话系统的数字键（1-9 选择分支）/暂停菜单冲突
            if (GameManager.Instance == null || !GameManager.Instance.CanPlayerAct())
                return;

            // 技能快捷键
            HandleSkillInput();
        }

        private void HandleSkillInput()
        {
            // 数字键 1-4 释放装备的技能
            if (_globalCooldownTimer > 0) return;

            for (int i = 0; i < maxSkillSlots; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    UseSkill(i);
                }
            }
        }

        /// <summary>
        /// 学习新招式
        /// </summary>
        public bool LearnSkill(MartialSkill skill)
        {
            if (_learnedSkills.ContainsKey(skill.skillId))
            {
                Debug.Log($"[武学] 已学会 {skill.skillName}");
                return false;
            }

            _learnedSkills[skill.skillId] = skill;
            OnSkillLearned?.Invoke(skill);
            var questManagerOnLearn = GameSystem.QuestManager.Instance;
            if (questManagerOnLearn != null)
                questManagerOnLearn.UpdateObjective(
                    GameSystem.QuestObjective.ObjectiveType.LearnSkill,
                    skill.skillId);

            Debug.Log($"[武学] 学会新招式：{skill.skillName}（{skill.school}）");

            // 自动装备到空槽位
            for (int i = 0; i < _equippedSkills.Length; i++)
            {
                if (_equippedSkills[i] == null)
                {
                    EquipSkill(skill, i);
                    break;
                }
            }

            return true;
        }

        /// <summary>
        /// 装备招式到快捷栏
        /// </summary>
        public void EquipSkill(MartialSkill skill, int slot)
        {
            if (slot < 0 || slot >= maxSkillSlots) return;
            if (!_learnedSkills.ContainsKey(skill.skillId)) return;

            // 如果这个技能已在其他槽位，先卸下
            for (int i = 0; i < _equippedSkills.Length; i++)
            {
                if (_equippedSkills[i]?.skillId == skill.skillId)
                    _equippedSkills[i] = null;
            }

            _equippedSkills[slot] = skill;
            OnSkillEquipped?.Invoke(skill, slot);

            Debug.Log($"[武学] 装备 {skill.skillName} → 快捷键 {slot + 1}");
        }

        /// <summary>
        /// 释放技能
        /// </summary>
        public bool UseSkill(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _equippedSkills.Length) return false;

            var skill = _equippedSkills[slotIndex];
            if (skill == null) return false;

            return UseSkill(skill);
        }

        public bool UseSkill(MartialSkill skill)
        {
            // 冷却检查
            if (_cooldowns.ContainsKey(skill.skillId) && _cooldowns[skill.skillId] > 0)
            {
                Debug.Log($"[武学] {skill.skillName} 冷却中（{_cooldowns[skill.skillId]:F1}s）");
                return false;
            }

            // 内力检查
            if (_stats.currentMp < skill.mpCost)
            {
                Debug.Log($"[武学] 内力不足！需要 {skill.mpCost}，当前 {_stats.currentMp}");
                return false;
            }

            // 消耗内力
            _stats.currentMp -= skill.mpCost;

            // 进入冷却
            _cooldowns[skill.skillId] = skill.cooldown;
            _globalCooldownTimer = globalCooldown;

            // 执行技能效果
            ExecuteSkill(skill);

            OnSkillUsed?.Invoke(skill);
            return true;
        }

        private void ExecuteSkill(MartialSkill skill)
        {
            Debug.Log($"[武学] 释放 {skill.skillName}！消耗内力 {skill.mpCost}");

            var dir = GetComponent<PlayerController>()?.LastDirection ?? Vector2.right;

            switch (skill.type)
            {
                case SkillType.Melee:
                    ExecuteMeleeSkill(skill, dir);
                    break;
                case SkillType.Ranged:
                    ExecuteRangedSkill(skill, dir);
                    break;
                case SkillType.Buff:
                    ExecuteBuffSkill(skill);
                    break;
                case SkillType.Heal:
                    ExecuteHealSkill(skill);
                    break;
                case SkillType.Dash:
                    ExecuteDashSkill(skill, dir);
                    break;
                case SkillType.AoE:
                    ExecuteAoESkill(skill);
                    break;
            }
        }

        // === 近战招式 ===
        private void ExecuteMeleeSkill(MartialSkill skill, Vector2 dir)
        {
            // 扩大攻击范围 + 加伤害
            Vector2 center = (Vector2)transform.position + dir * skill.range;
            float radius = skill.aoeRadius > 0 ? skill.aoeRadius : 1.2f;

            var hits = Physics2D.OverlapCircleAll(center, radius, LayerMask.GetMask("Enemy"));
            foreach (var hit in hits)
            {
                var target = hit.GetComponent<CharacterStats>();
                if (target != null && target.IsAlive)
                {
                    int damage = CalculateSkillDamage(skill);
                    target.TakeDamage(damage, _stats);

                    Effects.EffectsManager.HitSpark(hit.transform.position, dir);
                    Effects.EffectsManager.DamageNumber(hit.transform.position, damage, false);
                }
            }

            // 剑气特效（显式 == null，规避 Unity fake-null）
            var slashEnd = (Vector2)transform.position + dir * skill.range;
            var slashEffects = Effects.EffectsManager.Instance;
            if (slashEffects != null)
            {
                slashEffects.PlaySlashTrail(
                    (Vector2)transform.position + dir * 0.5f,
                    slashEnd,
                    skill.elementColor,
                    0.4f
                );
            }
        }

        // === 远程招式 ===
        private void ExecuteRangedSkill(MartialSkill skill, Vector2 dir)
        {
            int count = Mathf.Max(1, skill.projectileCount);
            float spread = Mathf.Max(0f, skill.projectileSpreadDegrees);

            for (int i = 0; i < count; i++)
            {
                // 扇形齐发：总张角均匀分布，单发保持直线。
                float t = count == 1 ? 0f : (i / (float)(count - 1) - 0.5f);
                float angle = spread * t;
                Vector2 shotDir = Rotate(dir, angle);

                CreateProjectile(skill, shotDir);
            }

            // 发射特效（显式 == null，规避 Unity fake-null）
            var castEffects = Effects.EffectsManager.Instance;
            if (castEffects != null)
                castEffects.PlayEffect("cast_burst", transform.position, 0.5f);
        }

        private void CreateProjectile(MartialSkill skill, Vector2 dir)
        {
            // 弹体必须使用持久精灵（Resources/Art/MVP，复审 S3），
            // 禁止运行时 Texture2D/Sprite.Create；元素差异经 SpriteRenderer.color 染色。
            var sprite = Art.MvpArtCatalog.Load(skill.projectileSpriteId);
            if (sprite == null) return;

            // 创建飞行物
            GameObject projectile = new GameObject($"Projectile_{skill.skillName}");
            projectile.transform.position = transform.position;

            var sr = projectile.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Foreground";
            sr.sortingOrder = 50;
            sr.sprite = sprite;
            sr.color = skill.elementColor;

            var rb = projectile.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.linearVelocity = dir * skill.projectileSpeed;

            // 触发碰撞所需：飞行物必须有 Trigger 碰撞体，OnTriggerEnter2D 才会触发
            var col = projectile.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.3f;

            var proj = projectile.AddComponent<Projectile>();
            proj.damage = CalculateSkillDamage(skill);
            proj.sourceStats = _stats;
            proj.lifetime = 3f;
            proj.skill = skill;
        }

        private static Vector2 Rotate(Vector2 vector, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(
                vector.x * cos - vector.y * sin,
                vector.x * sin + vector.y * cos);
        }

        // === 增益招式 ===
        private void ExecuteBuffSkill(MartialSkill skill)
        {
            // 临时提升属性
            StartCoroutine(BuffCoroutine(skill));
            var buffEffects = Effects.EffectsManager.Instance;
            if (buffEffects != null)
                buffEffects.PlayEffect("buff_ring", transform.position, 1f);
        }

        private System.Collections.IEnumerator BuffCoroutine(MartialSkill skill)
        {
            int bonusAtk = Mathf.RoundToInt(_stats.attack * skill.buffMultiplier);
            _stats.attack += bonusAtk;

            Debug.Log($"[武学] 增益生效：攻击 +{bonusAtk}，持续 {skill.duration}s");

            yield return new WaitForSeconds(skill.duration);

            _stats.attack -= bonusAtk;
            Debug.Log($"[武学] 增益消失：攻击 -{bonusAtk}");
        }

        // === 治疗招式 ===
        private void ExecuteHealSkill(MartialSkill skill)
        {
            int healAmount = skill.baseDamage + Mathf.RoundToInt(_stats.level * 2);
            _stats.Heal(healAmount);

            Effects.EffectsManager.HealEffect(transform.position);
            Effects.EffectsManager.DamageNumber(transform.position, healAmount, false);

            Debug.Log($"[武学] 治疗 {healAmount} 气血");
        }

        // === 突进招式 ===
        private void ExecuteDashSkill(MartialSkill skill, Vector2 dir)
        {
            // 冲拳类突进：对突进路径上的敌人结算一次伤害（docs/15 拳套主动技）。
            if (skill.baseDamage > 0 || skill.attackScaling > 0f)
            {
                Vector2 start = transform.position;
                Vector2 end = start + dir * skill.range;
                Vector2 min = Vector2.Min(start, end) - new Vector2(0.6f, 0.6f);
                Vector2 max = Vector2.Max(start, end) + new Vector2(0.6f, 0.6f);
                var hits = Physics2D.OverlapAreaAll(min, max, LayerMask.GetMask("Enemy"));
                foreach (var hit in hits)
                {
                    var target = hit.GetComponent<CharacterStats>();
                    if (target != null && target.IsAlive)
                    {
                        int damage = CalculateSkillDamage(skill);
                        target.TakeDamage(damage, _stats);
                        Effects.EffectsManager.HitSpark(hit.transform.position, dir);
                        Effects.EffectsManager.DamageNumber(hit.transform.position, damage, false);
                    }
                }
            }

            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = dir * skill.dashSpeed;
                StartCoroutine(StopDashAfterDelay(rb, 0.2f));
            }

            // 残影效果（显式 == null，规避 Unity fake-null）
            var effects = Effects.EffectsManager.Instance;
            if (effects != null)
            {
                effects.PlaySlashTrail(
                    transform.position,
                    (Vector2)transform.position + dir * skill.range,
                    new Color(0.5f, 0.5f, 1f, 0.5f),
                    0.5f
                );
            }
        }

        private System.Collections.IEnumerator StopDashAfterDelay(Rigidbody2D rb, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        // === 范围招式 ===
        private void ExecuteAoESkill(MartialSkill skill)
        {
            float radius = skill.aoeRadius > 0 ? skill.aoeRadius : 3f;
            var hits = Physics2D.OverlapCircleAll(transform.position, radius, LayerMask.GetMask("Enemy"));

            foreach (var hit in hits)
            {
                var target = hit.GetComponent<CharacterStats>();
                if (target != null && target.IsAlive)
                {
                    int damage = CalculateSkillDamage(skill);
                    target.TakeDamage(damage, _stats);
                    Effects.EffectsManager.DamageNumber(hit.transform.position, damage, false);
                }
            }

            // 范围特效（显式 == null，规避 Unity fake-null）
            var aoeEffects = Effects.EffectsManager.Instance;
            if (aoeEffects != null)
            {
                aoeEffects.PlayEffect("aoe_burst", transform.position, 1f);
                aoeEffects.ScreenFlash(skill.elementColor, 0.2f);
            }

            Debug.Log($"[武学] 范围技 {skill.skillName}，命中 {hits.Length} 个目标");
        }

        // === 伤害计算 ===
        private int CalculateSkillDamage(MartialSkill skill)
        {
            float baseDmg = skill.baseDamage + _stats.attack * skill.attackScaling;
            // 暴击
            if (Random.Range(0, 100) < _stats.critRate)
            {
                baseDmg *= _stats.critMultiplier;
            }
            return Mathf.RoundToInt(baseDmg);
        }

        // === 辅助 ===
        // （弹体精灵已改为 MvpArtCatalog 持久资产，CreateBulletSprite 随复审 S3 移除。）

        // === 存档 ===
        [System.Serializable]
        public class MartialArtsSaveData
        {
            public string[] learnedSkillIds;
            public string[] equippedSkillIds;
        }

        public MartialArtsSaveData GetSaveData()
        {
            var data = new MartialArtsSaveData();
            var learned = new List<string>(_learnedSkills.Keys);
            data.learnedSkillIds = learned.ToArray();

            var equipped = new List<string>();
            foreach (var skill in _equippedSkills)
                equipped.Add(skill?.skillId ?? "");
            data.equippedSkillIds = equipped.ToArray();

            return data;
        }

        public void LoadSaveData(MartialArtsSaveData data, Dictionary<string, MartialSkill> allSkills)
        {
            _learnedSkills.Clear();
            System.Array.Clear(_equippedSkills, 0, _equippedSkills.Length);

            if (data == null || allSkills == null) return;

            if (data.learnedSkillIds != null)
            {
                foreach (var id in data.learnedSkillIds)
                {
                    if (!string.IsNullOrEmpty(id) && allSkills.TryGetValue(id, out var skill))
                        _learnedSkills[id] = skill;
                }
            }

            if (data.equippedSkillIds == null) return;

            for (int i = 0; i < data.equippedSkillIds.Length && i < _equippedSkills.Length; i++)
            {
                if (!string.IsNullOrEmpty(data.equippedSkillIds[i]) &&
                    _learnedSkills.TryGetValue(data.equippedSkillIds[i], out var skill))
                {
                    _equippedSkills[i] = skill;
                }
            }
        }
    }

    // ========== 数据定义 ==========

    /// <summary>技能类型</summary>
    public enum SkillType
    {
        Melee,      // 近战（剑招/拳法）
        Ranged,     // 远程（暗器/剑气）
        Buff,       // 增益（内功心法）
        Heal,       // 治疗
        Dash,       // 突进（轻功）
        AoE         // 范围（绝招）
    }

    /// <summary>
    /// 武学招式数据
    /// </summary>
    [CreateAssetMenu(fileName = "MartialSkill", menuName = "渊海录/武学招式")]
    public class MartialSkill : ScriptableObject
    {
        [Header("基础信息")]
        public string skillId;
        public string skillName;
        [TextArea(1, 3)] public string description;
        public string school;              // 所属门派/流派
        public SkillType type;

        [Header("数值")]
        public int mpCost = 10;            // 内力消耗
        public float cooldown = 3f;        // 冷却时间
        public int baseDamage = 20;        // 基础伤害
        public float attackScaling = 0.5f; // 攻击力加成比例
        public float range = 2f;           // 射程
        public float aoeRadius = 0f;       // 范围半径（0=单体）
        public float duration = 5f;        // 增益/效果持续时间
        public float buffMultiplier = 0.3f;// 增益倍率
        public float projectileSpeed = 8f; // 飞行物速度
        public float dashSpeed = 15f;      // 突进速度
        public int projectileCount = 1;    // 飞行物数量（扇形齐发，如飞镖三连）
        public float projectileSpreadDegrees = 0f; // 扇形总张角（度，0=单发直线）

        [Header("视觉")]
        public Color elementColor = new Color(0.7f, 0.9f, 1f); // 元素颜色
        public string castAnimation = "Cast";
        public string vfxId = "";
        public string projectileSpriteId = "proj_qi"; // 弹体持久精灵（MvpArtCatalog）

        [Header("学习条件")]
        public int requiredLevel = 1;
        public string requiredQuest = "";
        public string prerequisiteSkill = "";
    }

    /// <summary>
    /// 飞行物组件
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        public int damage;
        public CharacterStats sourceStats;
        public float lifetime = 3f;
        public MartialSkill skill;

        private void Start()
        {
            Destroy(gameObject, lifetime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Enemy"))
            {
                var target = other.GetComponent<CharacterStats>();
                if (target != null && target.IsAlive)
                {
                    target.TakeDamage(damage, sourceStats);
                    Effects.EffectsManager.HitSpark(transform.position, transform.right);
                    Effects.EffectsManager.DamageNumber(transform.position, damage, false);
                }
                Destroy(gameObject);
            }
            else if (other.gameObject.layer == LayerMask.NameToLayer("Environment"))
            {
                var wallEffects = Effects.EffectsManager.Instance;
                if (wallEffects != null)
                    wallEffects.PlayEffect("hit_wall", transform.position, 0.3f);
                Destroy(gameObject);
            }
        }
    }
}
