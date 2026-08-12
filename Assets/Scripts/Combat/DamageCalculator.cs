using UnityEngine;
using System.Collections.Generic;
using YuanHaiLu.Core;
using YuanHaiLu.Character;
using YuanHaiLu.GameSystem;

namespace YuanHaiLu.Combat
{
    /// <summary>
    /// 伤害计算引擎 — 武侠风格公式
    /// 统一处理所有伤害/治疗/暴击/防御/属性克制
    /// </summary>
    public static class DamageCalculator
    {
        // === 伤害公式 ===

        /// <summary>
        /// 计算物理攻击伤害
        /// 公式: (攻击 × 技能倍率 - 防御 × 0.6) × 随机波动 × 暴击
        /// </summary>
        public static DamageResult CalculatePhysicalDamage(
            CharacterStats attacker, CharacterStats defender,
            float skillMultiplier = 1f, int bonusDamage = 0)
        {
            var result = new DamageResult();

            // 基础攻击
            float baseAtk = attacker.attack * skillMultiplier + bonusDamage;

            // 防御减免
            float defense = defender.defense * 0.6f;
            float afterDef = Mathf.Max(baseAtk - defense, 1f); // 至少1点伤害

            // 随机波动 (±10%)
            float variance = Random.Range(0.9f, 1.1f);
            result.damage = Mathf.RoundToInt(afterDef * variance);

            // 暴击判定
            result.isCrit = Random.Range(0, 100) < attacker.critRate;
            if (result.isCrit)
            {
                result.damage = Mathf.RoundToInt(result.damage * attacker.critMultiplier);
            }

            // 身法闪避判定
            result.isMiss = Random.Range(0, 100) < Mathf.FloorToInt(defender.agility * 0.3f);
            if (result.isMiss)
            {
                result.damage = 0;
            }

            return result;
        }

        /// <summary>
        /// 计算内功（技能）伤害
        /// 公式: (攻击 × 技能加成 + 技能基础) × 内力系数 - 防御 × 0.3
        /// </summary>
        public static DamageResult CalculateSkillDamage(
            CharacterStats attacker, CharacterStats defender,
            float skillScaling, int skillBaseDamage)
        {
            var result = new DamageResult();

            // 内力加成系数 (MP越多伤害越高)
            float mpRatio = (float)attacker.currentMp / attacker.maxMp;
            float mpBonus = 0.8f + mpRatio * 0.4f; // 0.8 ~ 1.2

            float baseDmg = (attacker.attack * skillScaling + skillBaseDamage) * mpBonus;
            float afterDef = Mathf.Max(baseDmg - defender.defense * 0.3f, 1f);

            float variance = Random.Range(0.9f, 1.1f);
            result.damage = Mathf.RoundToInt(afterDef * variance);

            // 内功暴击率略低但暴击倍率更高
            result.isCrit = Random.Range(0, 100) < (attacker.critRate * 0.7f);
            if (result.isCrit)
            {
                result.damage = Mathf.RoundToInt(result.damage * attacker.critMultiplier * 1.2f);
            }

            result.isMiss = false; // 内功不可闪避

            return result;
        }

        /// <summary>
        /// 计算治疗量
        /// </summary>
        public static int CalculateHealAmount(CharacterStats healer, int baseHeal)
        {
            float intBonus = 1f + healer.level * 0.05f;
            float variance = Random.Range(0.95f, 1.05f);
            return Mathf.RoundToInt(baseHeal * intBonus * variance);
        }

        /// <summary>
        /// 应用伤害到目标
        /// </summary>
        public static DamageResult ApplyDamage(
            CharacterStats attacker, CharacterStats defender,
            float skillMultiplier = 1f, int bonusDamage = 0)
        {
            var result = CalculatePhysicalDamage(attacker, defender, skillMultiplier, bonusDamage);

            if (!result.isMiss)
            {
                defender.TakeDamage(result.damage, attacker);

                // 命中特效
                if (result.isCrit)
                {
                    Effects.EffectsManager.CritEffect(defender.transform.position);
                }
            }

            return result;
        }
    }

    /// <summary>
    /// 伤害结果
    /// </summary>
    [System.Serializable]
    public struct DamageResult
    {
        public int damage;
        public bool isCrit;
        public bool isMiss;

        public string ToDisplayString()
        {
            if (isMiss) return "闪避!";
            if (isCrit) return $"暴击 {damage}!";
            return damage.ToString();
        }
    }
}
