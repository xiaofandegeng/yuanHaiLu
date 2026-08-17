using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using YuanHaiLu.Character;
using YuanHaiLu.Core;

namespace YuanHaiLu.Tests.EditMode
{
    /// <summary>
    /// PlayerCombat 的流派档案应用：三种流派在同一副身体上产生可观测的判定差异。
    /// </summary>
    public class WeaponStyleCombatTests
    {
        [TearDown]
        public void TearDown()
        {
            TestSceneFactory.DestroyAll();
        }

        private static PlayerCombat CreateCombat()
        {
            var player = TestSceneFactory.CreatePlayer();
            return TestSceneFactory.AddComponentWithAwake<PlayerCombat>(player);
        }

        private static void AssertProfile(PlayerCombat combat, WeaponStyle style)
        {
            Assert.That(combat.WeaponStyleId, Is.EqualTo(style.StyleId));
            Assert.That(combat.CurrentAttackRange, Is.EqualTo(style.MeleeRange));
            Assert.That(combat.CurrentAttackBoxSize, Is.EqualTo(style.MeleeBoxSize));
            Assert.That(combat.CurrentMaxCombo, Is.EqualTo(style.MaxCombo));
            Assert.That(combat.CurrentAttackDuration, Is.EqualTo(style.AttackDuration));
            Assert.That(combat.CurrentMeleeDamageMultiplier, Is.EqualTo(style.MeleeDamageMultiplier));
            Assert.That(combat.slashColor, Is.EqualTo(style.SlashColor));
        }

        [TestCase("sword")]
        [TestCase("gauntlets")]
        [TestCase("dart")]
        public void ApplyWeaponStyleLoadsFullProfile(string styleId)
        {
            var combat = CreateCombat();
            combat.ApplyWeaponStyle(styleId);
            AssertProfile(combat, WeaponStyle.ParseOrDefault(styleId));
        }

        [Test]
        public void IllegalStyleFallsBackToSwordProfile()
        {
            var combat = CreateCombat();
            combat.ApplyWeaponStyle("katana");
            AssertProfile(combat, WeaponStyle.Default);
        }

        [Test]
        public void GameManagerStyleChangeReachesCombatThroughEvent()
        {
            var gameManager = TestSceneFactory.AddComponentWithAwake<GameManager>(
                TestSceneFactory.Create("GameManager"));
            var combat = CreateCombat();
            InvokePrivate(combat, "OnEnable");

            gameManager.SetWeaponStyle("dart");

            Assert.That(combat.WeaponStyleId, Is.EqualTo("dart"));
            Assert.That(combat.CurrentMaxCombo, Is.EqualTo(WeaponStyle.ParseOrDefault("dart").MaxCombo));
        }

        private static void InvokePrivate(object target, string methodName)
        {
            target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(target, null);
        }
    }
}
