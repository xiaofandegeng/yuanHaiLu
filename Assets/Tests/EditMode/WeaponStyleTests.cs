using System.Linq;
using NUnit.Framework;
using UnityEngine;
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
