using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using YuanHaiLu.Character;
using YuanHaiLu.GameSystem;

namespace YuanHaiLu.Tests.PlayMode
{
    /// <summary>
    /// 三种武器流派的主动技能实测（docs/15）：
    /// 长剑=前冲剑气单发；拳套=冲拳路径伤害；飞镖=扇形三镖。
    /// </summary>
    public class WeaponSkillPlayModeTests
    {
        [TearDown]
        public void TearDown()
        {
            foreach (var projectile in Object.FindObjectsByType<Projectile>(
                     FindObjectsInactive.Exclude,
                     FindObjectsSortMode.None))
                Object.Destroy(projectile.gameObject);
        }

        private static MartialArtsSystem CreatePlayerWithSkill(string skillId)
        {
            var player = new GameObject("Player");
            player.tag = "Player";
            player.AddComponent<CharacterStats>();
            var martial = player.AddComponent<MartialArtsSystem>();
            Assert.That(martial.LearnSkill(MartialSkillDatabase.Get(skillId)), Is.True);
            return martial;
        }

        [UnityTest]
        public IEnumerator SwordQiWaveFiresSingleForwardProjectile()
        {
            var martial = CreatePlayerWithSkill("sword_qi_wave");

            Assert.That(martial.UseSkill(0), Is.True);
            yield return null;

            var projectiles = Object.FindObjectsByType<Projectile>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            Assert.That(projectiles, Has.Length.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DartFanThrowFiresThreeSpreadProjectiles()
        {
            var martial = CreatePlayerWithSkill("dart_fan_throw");

            Assert.That(martial.UseSkill(0), Is.True);
            yield return null;

            var projectiles = Object.FindObjectsByType<Projectile>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            Assert.That(projectiles, Has.Length.EqualTo(3));
            // 扇形张角 24°：外缘两发速度方向相对前进轴约 ±12°。
            var forward = Vector2.right;
            var angles = projectiles
                .Select(projectile => Vector2.SignedAngle(
                    forward,
                    projectile.GetComponent<Rigidbody2D>().linearVelocity))
                .OrderBy(angle => angle)
                .ToArray();
            Assert.That(angles[0], Is.InRange(-16f, -8f));
            Assert.That(angles[^1], Is.InRange(8f, 16f));
            Assert.That(angles[1], Is.InRange(-2f, 2f));
        }

        [UnityTest]
        public IEnumerator FistDashPunchDamagesEnemyAlongDashPath()
        {
            var player = new GameObject("Player");
            player.tag = "Player";
            player.AddComponent<CharacterStats>();
            var martial = player.AddComponent<MartialArtsSystem>();
            martial.LearnSkill(MartialSkillDatabase.Get("fist_dash_punch"));

            var enemy = new GameObject("Enemy");
            enemy.layer = LayerMask.NameToLayer("Enemy");
            enemy.tag = "Enemy";
            enemy.transform.position = new Vector3(2f, 0f, 0f);
            var enemyCollider = enemy.AddComponent<BoxCollider2D>();
            enemyCollider.size = new Vector2(0.8f, 1.2f);
            var enemyStats = enemy.AddComponent<CharacterStats>();
            // CharacterStats.Awake 在 PlayMode 自动执行并把当前 HP 设为上限。
            enemyStats.agility = 0; // 消除闪避带来的偶然性
            int hpBeforeHit = enemyStats.currentHp;

            Assert.That(martial.UseSkill(0), Is.True);
            yield return null;

            Assert.That(enemyStats.currentHp, Is.LessThan(hpBeforeHit));
        }
    }
}
