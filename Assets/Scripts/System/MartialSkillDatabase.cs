using UnityEngine;
using System;
using System.Collections.Generic;
using YuanHaiLu.Character;

namespace YuanHaiLu.GameSystem
{
    /// <summary>
    /// 武学招式数据库 — Demo预置招式
    /// </summary>
    public static class MartialSkillDatabase
    {
        private static Dictionary<string, MartialSkill> _skills;

        public static Dictionary<string, MartialSkill> AllSkills
        {
            get
            {
                if (_skills == null) BuildDatabase();
                return _skills;
            }
        }

        public static MartialSkill Get(string id)
        {
            return AllSkills.TryGetValue(id, out var skill) ? skill : null;
        }

        private static void BuildDatabase()
        {
            _skills = new Dictionary<string, MartialSkill>();

            // === 初始招式（凌霜自带） ===
            Add("basic_slash", "横剑式", "最基本的剑招，横剑一斩", "无流派",
                SkillType.Melee, mpCost: 5, cooldown: 1.5f, baseDamage: 15,
                attackScaling: 0.8f, range: 1.5f,
                color: new Color(0.7f, 0.9f, 1f));

            // === 剑法 ===
            Add("sword_frost_slash", "霜华斩", "剑凝冰霜，一剑生寒", "霜华剑派",
                SkillType.Melee, mpCost: 15, cooldown: 4f, baseDamage: 30,
                attackScaling: 1.2f, range: 2f,
                color: new Color(0.5f, 0.8f, 1f),
                reqLevel: 3);

            Add("sword_flying_snow", "飞雪连天", "剑化飞雪，漫天寒芒", "霜华剑派",
                SkillType.AoE, mpCost: 30, cooldown: 10f, baseDamage: 25,
                attackScaling: 0.7f, aoeRadius: 4f,
                color: new Color(0.7f, 0.9f, 1f),
                reqLevel: 8, reqSkill: "sword_frost_slash");

            Add("sword_sky_pierce", "天外一剑", "蓄力一剑，贯穿天际", "霜华剑派",
                SkillType.Ranged, mpCost: 25, cooldown: 6f, baseDamage: 40,
                attackScaling: 1.5f, range: 8f, projSpeed: 12f,
                color: new Color(0.3f, 0.6f, 1f),
                reqLevel: 12, reqSkill: "sword_flying_snow");

            // === 拳法 ===
            Add("fist_tiger", "猛虎下山拳", "拳如猛虎，势不可挡", "少林外功",
                SkillType.Melee, mpCost: 10, cooldown: 3f, baseDamage: 20,
                attackScaling: 1f, range: 1.2f,
                color: new Color(1f, 0.7f, 0.2f),
                reqLevel: 2);

            Add("fist_dragon", "降龙掌", "掌出如龙，气吞山河", "丐帮",
                SkillType.AoE, mpCost: 35, cooldown: 12f, baseDamage: 35,
                attackScaling: 1f, aoeRadius: 3.5f,
                color: new Color(1f, 0.85f, 0.1f),
                reqLevel: 10, reqSkill: "fist_tiger");

            // === 轻功 ===
            Add("dash_wind_step", "疾风步", "身化疾风，瞬间位移", "武当轻功",
                SkillType.Dash, mpCost: 12, cooldown: 5f, baseDamage: 0,
                range: 4f, dashSpeed: 18f,
                color: new Color(0.5f, 1f, 0.5f),
                reqLevel: 4);

            Add("dash_shadow", "无影步", "步法如幻，残影迷踪", "唐门",
                SkillType.Dash, mpCost: 20, cooldown: 8f, baseDamage: 10,
                range: 6f, dashSpeed: 25f,
                color: new Color(0.4f, 0.2f, 0.6f),
                reqLevel: 10, reqSkill: "dash_wind_step");

            // === 内功 ===
            Add("buff_iron_body", "金钟罩", "内力护体，防御大增", "少林内功",
                SkillType.Buff, mpCost: 20, cooldown: 15f, baseDamage: 0,
                duration: 8f, buffMultiplier: 0.5f,
                color: new Color(1f, 0.85f, 0.3f),
                reqLevel: 6);

            Add("heal_pure_spring", "清心诀", "内息运转，恢复气血", "武当内功",
                SkillType.Heal, mpCost: 15, cooldown: 8f, baseDamage: 30,
                color: new Color(0.3f, 1f, 0.5f),
                reqLevel: 3);

            // === 绝招（大招） ===
            Add("ultimate_sword_dance", "一剑霜寒十四州", "霜华剑派终极奥义，剑意化霜天", "霜华剑派",
                SkillType.AoE, mpCost: 60, cooldown: 30f, baseDamage: 80,
                attackScaling: 2f, aoeRadius: 6f,
                color: new Color(0.2f, 0.5f, 1f),
                reqLevel: 20, reqSkill: "sword_sky_pierce");
        }

        private static void Add(string id, string name, string desc, string school,
            SkillType type, int mpCost, float cooldown, int baseDamage,
            float attackScaling = 0f, float range = 0f, float aoeRadius = 0f,
            float duration = 0f, float buffMultiplier = 0f,
            float projSpeed = 0f, float dashSpeed = 0f,
            Color color = default,
            int reqLevel = 1, string reqQuest = "", string reqSkill = "")
        {
            var skill = ScriptableObject.CreateInstance<MartialSkill>();
            skill.skillId = id;
            skill.skillName = name;
            skill.description = desc;
            skill.school = school;
            skill.type = type;
            skill.mpCost = mpCost;
            skill.cooldown = cooldown;
            skill.baseDamage = baseDamage;
            skill.attackScaling = attackScaling;
            skill.range = range;
            skill.aoeRadius = aoeRadius;
            skill.duration = duration;
            skill.buffMultiplier = buffMultiplier;
            skill.projectileSpeed = projSpeed;
            skill.dashSpeed = dashSpeed;
            skill.elementColor = color == default ? Color.white : color;
            skill.requiredLevel = reqLevel;
            skill.requiredQuest = reqQuest;
            skill.prerequisiteSkill = reqSkill;

            _skills[id] = skill;
        }

        /// <summary>
        /// 获取初始招式（新建角色使用）
        /// </summary>
        public static string[] GetStarterSkills()
        {
            return new string[] { "basic_slash" };
        }
    }
}
