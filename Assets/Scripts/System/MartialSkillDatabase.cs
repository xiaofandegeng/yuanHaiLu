using UnityEngine;
using System.Collections.Generic;
using YuanHaiLu.Character;

namespace YuanHaiLu.GameSystem
{
    /// <summary>
    /// 武学招式数据库 — Demo预置招式
    /// 复审 P2：Add 收配置对象（SkillSpec），不再使用长位置参数列表，
    /// 新增字段只改 SkillSpec 与拷贝处，调用点按名赋值不易错位。
    /// </summary>
    public static class MartialSkillDatabase
    {
        /// <summary>单条招式配置（Add 的入参对象）。</summary>
        private struct SkillSpec
        {
            public string id;
            public string name;
            public string desc;
            public string school;
            public SkillType type;
            public int mpCost;
            public float cooldown;
            public int baseDamage;
            public float attackScaling;
            public float range;
            public float aoeRadius;
            public float duration;
            public float buffMultiplier;
            public float projSpeed;
            public float dashSpeed;
            public Color color;
            public int reqLevel;
            public string reqQuest;
            public string reqSkill;
            public int projCount;
            public float projSpreadDegrees;
            public string projSpriteId;
        }

        private static Dictionary<string, MartialSkill> _skills;

        public static Dictionary<string, MartialSkill> AllSkills
        {
            get
            {
                if (_skills == null) BuildDatabase();
                return _skills;
            }
        }

        /// <summary>显式确保代码表已构建（场景生成与测试入口使用，复审 P2）。</summary>
        public static void EnsureInitialized()
        {
            if (_skills == null) BuildDatabase();
        }

        public static MartialSkill Get(string id)
        {
            return AllSkills.TryGetValue(id, out var skill) ? skill : null;
        }

        private static void BuildDatabase()
        {
            _skills = new Dictionary<string, MartialSkill>();

            // === 单主角 MVP 流派主动技（docs/15） ===
            Add(new SkillSpec
            {
                id = "sword_qi_wave",
                name = "剑气斩",
                desc = "长剑流派：剑凝真气，前冲一道剑气破敌",
                school = "无流派",
                type = SkillType.Ranged,
                mpCost = 10,
                cooldown = 3f,
                baseDamage = 20,
                attackScaling = 0.9f,
                range = 7f,
                projSpeed = 10f,
                color = new Color(0.7f, 0.9f, 1f),
            });

            Add(new SkillSpec
            {
                id = "fist_dash_punch",
                name = "冲拳",
                desc = "拳套流派：短距突进，一记重拳撞开前方之敌",
                school = "无流派",
                type = SkillType.Dash,
                mpCost = 8,
                cooldown = 4f,
                baseDamage = 18,
                attackScaling = 0.8f,
                range = 3.5f,
                dashSpeed = 16f,
                color = new Color(1f, 0.7f, 0.3f),
            });

            Add(new SkillSpec
            {
                id = "dart_fan_throw",
                name = "回风三镖",
                desc = "飞镖流派：袖中三镖齐发，扇形罩住去路",
                school = "无流派",
                type = SkillType.Ranged,
                mpCost = 10,
                cooldown = 3.5f,
                baseDamage = 12,
                attackScaling = 0.6f,
                range = 8f,
                projSpeed = 12f,
                color = new Color(0.85f, 0.8f, 1f),
                projCount = 3,
                projSpreadDegrees = 24f,
                projSpriteId = "proj_dart",
            });

            // === 初始招式（凌霜自带） ===
            Add(new SkillSpec
            {
                id = "basic_slash",
                name = "横剑式",
                desc = "最基本的剑招，横剑一斩",
                school = "无流派",
                type = SkillType.Melee,
                mpCost = 5,
                cooldown = 1.5f,
                baseDamage = 15,
                attackScaling = 0.8f,
                range = 1.5f,
                color = new Color(0.7f, 0.9f, 1f),
            });

            // === 剑法 ===
            Add(new SkillSpec
            {
                id = "sword_frost_slash",
                name = "霜华斩",
                desc = "剑凝冰霜，一剑生寒",
                school = "霜华剑派",
                type = SkillType.Melee,
                mpCost = 15,
                cooldown = 4f,
                baseDamage = 30,
                attackScaling = 1.2f,
                range = 2f,
                color = new Color(0.5f, 0.8f, 1f),
                reqLevel = 3,
            });

            Add(new SkillSpec
            {
                id = "sword_flying_snow",
                name = "飞雪连天",
                desc = "剑化飞雪，漫天寒芒",
                school = "霜华剑派",
                type = SkillType.AoE,
                mpCost = 30,
                cooldown = 10f,
                baseDamage = 25,
                attackScaling = 0.7f,
                aoeRadius = 4f,
                color = new Color(0.7f, 0.9f, 1f),
                reqLevel = 8,
                reqSkill = "sword_frost_slash",
            });

            Add(new SkillSpec
            {
                id = "sword_sky_pierce",
                name = "天外一剑",
                desc = "蓄力一剑，贯穿天际",
                school = "霜华剑派",
                type = SkillType.Ranged,
                mpCost = 25,
                cooldown = 6f,
                baseDamage = 40,
                attackScaling = 1.5f,
                range = 8f,
                projSpeed = 12f,
                color = new Color(0.3f, 0.6f, 1f),
                reqLevel = 12,
                reqSkill = "sword_flying_snow",
            });

            // === 拳法 ===
            Add(new SkillSpec
            {
                id = "fist_tiger",
                name = "猛虎下山拳",
                desc = "拳如猛虎，势不可挡",
                school = "少林外功",
                type = SkillType.Melee,
                mpCost = 10,
                cooldown = 3f,
                baseDamage = 20,
                attackScaling = 1f,
                range = 1.2f,
                color = new Color(1f, 0.7f, 0.2f),
                reqLevel = 2,
            });

            Add(new SkillSpec
            {
                id = "fist_dragon",
                name = "降龙掌",
                desc = "掌出如龙，气吞山河",
                school = "丐帮",
                type = SkillType.AoE,
                mpCost = 35,
                cooldown = 12f,
                baseDamage = 35,
                attackScaling = 1f,
                aoeRadius = 3.5f,
                color = new Color(1f, 0.85f, 0.1f),
                reqLevel = 10,
                reqSkill = "fist_tiger",
            });

            // === 轻功 ===
            Add(new SkillSpec
            {
                id = "dash_wind_step",
                name = "疾风步",
                desc = "身化疾风，瞬间位移",
                school = "武当轻功",
                type = SkillType.Dash,
                mpCost = 12,
                cooldown = 5f,
                baseDamage = 0,
                range = 4f,
                dashSpeed = 18f,
                color = new Color(0.5f, 1f, 0.5f),
                reqLevel = 4,
            });

            Add(new SkillSpec
            {
                id = "dash_shadow",
                name = "无影步",
                desc = "步法如幻，残影迷踪",
                school = "唐门",
                type = SkillType.Dash,
                mpCost = 20,
                cooldown = 8f,
                baseDamage = 10,
                range = 6f,
                dashSpeed = 25f,
                color = new Color(0.4f, 0.2f, 0.6f),
                reqLevel = 10,
                reqSkill = "dash_wind_step",
            });

            // === 内功 ===
            Add(new SkillSpec
            {
                id = "buff_iron_body",
                name = "金钟罩",
                desc = "内力护体，防御大增",
                school = "少林内功",
                type = SkillType.Buff,
                mpCost = 20,
                cooldown = 15f,
                baseDamage = 0,
                duration = 8f,
                buffMultiplier = 0.5f,
                color = new Color(1f, 0.85f, 0.3f),
                reqLevel = 6,
            });

            Add(new SkillSpec
            {
                id = "heal_pure_spring",
                name = "清心诀",
                desc = "内息运转，恢复气血",
                school = "武当内功",
                type = SkillType.Heal,
                mpCost = 15,
                cooldown = 8f,
                baseDamage = 30,
                color = new Color(0.3f, 1f, 0.5f),
                reqLevel = 3,
            });

            // === 绝招（大招） ===
            Add(new SkillSpec
            {
                id = "ultimate_sword_dance",
                name = "一剑霜寒十四州",
                desc = "霜华剑派终极奥义，剑意化霜天",
                school = "霜华剑派",
                type = SkillType.AoE,
                mpCost = 60,
                cooldown = 30f,
                baseDamage = 80,
                attackScaling = 2f,
                aoeRadius = 6f,
                color = new Color(0.2f, 0.5f, 1f),
                reqLevel = 20,
                reqSkill = "sword_sky_pierce",
            });
        }

        private static void Add(SkillSpec spec)
        {
            var skill = ScriptableObject.CreateInstance<MartialSkill>();
            skill.skillId = spec.id;
            skill.skillName = spec.name;
            skill.description = spec.desc;
            skill.school = spec.school;
            skill.type = spec.type;
            skill.mpCost = spec.mpCost;
            skill.cooldown = spec.cooldown;
            skill.baseDamage = spec.baseDamage;
            skill.attackScaling = spec.attackScaling;
            skill.range = spec.range;
            skill.aoeRadius = spec.aoeRadius;
            skill.duration = spec.duration;
            skill.buffMultiplier = spec.buffMultiplier;
            skill.projectileSpeed = spec.projSpeed;
            skill.dashSpeed = spec.dashSpeed;
            skill.elementColor = spec.color == default ? Color.white : spec.color;
            skill.requiredLevel = spec.reqLevel == 0 ? 1 : spec.reqLevel;
            skill.requiredQuest = spec.reqQuest ?? "";
            skill.prerequisiteSkill = spec.reqSkill ?? "";
            skill.projectileCount = spec.projCount == 0 ? 1 : spec.projCount;
            skill.projectileSpreadDegrees = spec.projSpreadDegrees;
            skill.projectileSpriteId = string.IsNullOrEmpty(spec.projSpriteId)
                ? "proj_qi"
                : spec.projSpriteId;
            skill.hideFlags = HideFlags.HideAndDontSave;

            _skills[spec.id] = skill;
        }

        /// <summary>
        /// 获取初始招式（新建角色使用）
        /// </summary>
        public static string[] GetStarterSkills()
        {
            return new string[] { "basic_slash" };
        }

        /// <summary>
        /// 单主角 MVP（docs/15）：新游戏只随流派学会一个主动技能。
        /// </summary>
        public static string[] GetStarterSkills(string weaponStyleId)
        {
            var style = Core.WeaponStyle.ParseOrDefault(weaponStyleId);
            return new string[] { style.ActiveSkillId };
        }
    }
}
