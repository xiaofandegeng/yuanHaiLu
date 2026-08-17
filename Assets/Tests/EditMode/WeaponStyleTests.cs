using System.Linq;
using NUnit.Framework;
using UnityEngine;
using YuanHaiLu.Art;
using YuanHaiLu.Character;
using YuanHaiLu.Core;
using YuanHaiLu.GameSystem;

namespace YuanHaiLu.Tests.EditMode
{
    public class WeaponStyleTests
    {
        [Test]
        public void ThreeStableStyleIdsRoundTrip()
        {
            Assert.That(WeaponStyle.All.Count, Is.EqualTo(3));
            foreach (var style in WeaponStyle.All)
            {
                Assert.That(WeaponStyle.TryParse(style.StyleId, out var parsed), Is.True);
                Assert.That(parsed, Is.EqualTo(style));
            }
        }

        [TestCase("sword")]
        [TestCase("gauntlets")]
        [TestCase("dart")]
        public void KnownStyleParses(string styleId)
        {
            Assert.That(WeaponStyle.ParseOrDefault(styleId).StyleId, Is.EqualTo(styleId));
        }

        [TestCase("")]
        [TestCase("sword2")]
        [TestCase("Railgun")]
        public void IllegalStyleFallsBackToSword(string styleId)
        {
            Assert.That(WeaponStyle.ParseOrDefault(styleId), Is.EqualTo(WeaponStyle.Default));
            Assert.That(WeaponStyle.Default.StyleId, Is.EqualTo("sword"));
        }

        [Test]
        public void MeleeProfilesDifferAcrossStyles()
        {
            var sword = WeaponStyle.ParseOrDefault("sword");
            var gauntlets = WeaponStyle.ParseOrDefault("gauntlets");
            var dart = WeaponStyle.ParseOrDefault("dart");

            // 拳套短距快连击；长剑中距均衡；飞镖近战最弱。
            Assert.That(gauntlets.MeleeRange, Is.LessThan(sword.MeleeRange));
            Assert.That(gauntlets.MaxCombo, Is.GreaterThan(sword.MaxCombo));
            Assert.That(gauntlets.AttackDuration, Is.LessThan(sword.AttackDuration));
            Assert.That(gauntlets.MeleeDamageMultiplier, Is.LessThan(sword.MeleeDamageMultiplier));
            Assert.That(dart.MeleeDamageMultiplier, Is.LessThan(sword.MeleeDamageMultiplier));
            Assert.That(dart.MeleeRange, Is.LessThan(sword.MeleeRange));
        }

        [Test]
        public void EachStyleHasOneActiveSkillInDatabase()
        {
            foreach (var style in WeaponStyle.All)
            {
                var skill = MartialSkillDatabase.Get(style.ActiveSkillId);
                Assert.That(skill, Is.Not.Null, $"missing active skill for {style.StyleId}");
                Assert.That(MartialSkillDatabase.GetStarterSkills(style.StyleId),
                    Is.EqualTo(new[] { style.ActiveSkillId }));
            }
        }

        [Test]
        public void EachStyleHasDistinctPersistentWeaponSprite()
        {
            // 复审 P1-c：武器小图必须是 Resources/Art/MVP 下的持久精灵，三种流派互不相同。
            var spriteIds = new System.Collections.Generic.HashSet<string>();
            foreach (var style in WeaponStyle.All)
            {
                Assert.That(style.WeaponSpriteId, Is.Not.Null.And.Not.Empty,
                    $"style {style.StyleId} must declare a weapon sprite id.");
                var sprite = MvpArtCatalog.Load(style.WeaponSpriteId);
                Assert.That(sprite, Is.Not.Null,
                    $"weapon sprite '{style.WeaponSpriteId}' must exist under Resources/Art/MVP.");
                spriteIds.Add(style.WeaponSpriteId);
            }
            Assert.That(spriteIds.Count, Is.EqualTo(3),
                "Each weapon style must reference its own weapon sprite.");
        }

        [Test]
        public void DartFanThrowSpreadsThreeProjectilesAndSwordQiIsASingleWave()
        {
            var dart = MartialSkillDatabase.Get("dart_fan_throw");
            Assert.That(dart.projectileCount, Is.EqualTo(3));
            Assert.That(dart.projectileSpreadDegrees, Is.GreaterThan(0f));

            var wave = MartialSkillDatabase.Get("sword_qi_wave");
            Assert.That(wave.projectileCount, Is.EqualTo(1));

            var punch = MartialSkillDatabase.Get("fist_dash_punch");
            Assert.That(punch.type, Is.EqualTo(SkillType.Dash));
            Assert.That(punch.baseDamage, Is.GreaterThan(0));
        }
    }
}
