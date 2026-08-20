#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using YuanHaiLu.Art;
using YuanHaiLu.Core;
using YuanHaiLu.UI;

namespace YuanHaiLu.Tests.PlayMode
{
    public class MainFlowPlayModeTests
    {
        [UnityTest]
        public IEnumerator SelectedWeaponStyleSurvivesMainMenuToFormalDemo()
        {
            var load = EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Scenes/MainMenu.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!load.isDone)
                yield return null;
            yield return null;

            var menu = Object.FindAnyObjectByType<MainMenu>();
            Assert.That(menu, Is.Not.Null);
            // 模拟旧会话残留外观：新游戏必须重置为固定男主（docs/15）。
            GameManager.Instance.SetPlayerAppearance("player_female_swordsman");
            menu.SelectWeaponStyle("gauntlets");
            menu.OnNewGame();

            yield return null;
            yield return null;

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Demo_YanLiuTown"));
            Assert.That(Object.FindAnyObjectByType<RegionSceneDefinition>()?.SceneId,
                Is.EqualTo("yanliu"));
            Assert.That(GameManager.Instance.PlayerArtId, Is.EqualTo("player_male_swordsman"));
            Assert.That(GameManager.Instance.WeaponStyleId, Is.EqualTo("gauntlets"));
            var player = GameObject.FindGameObjectWithTag("Player");
            Assert.That(player, Is.Not.Null);
            Assert.That(player.GetComponent<CharacterVisual>()?.ArtId,
                Is.EqualTo("player_male_swordsman"));
            Assert.That(player.GetComponent<Character.PlayerCombat>()?.WeaponStyleId,
                Is.EqualTo("gauntlets"));

            // 复审 P1：开场引导（约 1 秒延迟）后，新游戏玩家必须被放到
            // 客栈门外的出生点，而不是旧默认 (0,-5) 的地图外位置。
            var startTime = Time.time;
            while (Time.time - startTime < 2.5f)
                yield return null;
            Assert.That(player.transform.position.x, Is.InRange(0f, 40f),
                $"出生点 X 越界: {player.transform.position.x}");
            Assert.That(player.transform.position.y, Is.InRange(5f, 24f),
                $"出生点 Y 越界: {player.transform.position.y}");
        }

        /// <summary>
        /// 复审 P1（docs/15“场景往返”）：真实物理触发 镇→客栈→镇 全程往返。
        /// 断言两个方向的场景、落点、输入恢复，以及 HP/MP/等级/武学/流派
        /// 经 TransitionCarry 回放后全部保持。
        /// </summary>
        [UnityTest]
        public IEnumerator TownInnTownRoundTripPreservesSpawnAndPlayerState()
        {
            // —— 主菜单 → 飞镖流派 → 烟柳镇 ——
            var load = EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Scenes/MainMenu.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!load.isDone) yield return null;
            yield return null;

            var menu = Object.FindAnyObjectByType<MainMenu>();
            Assert.That(menu, Is.Not.Null);
            menu.SelectWeaponStyle("dart");
            menu.OnNewGame();
            yield return null;
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Demo_YanLiuTown"));

            // 等开场引导把玩家放到客栈门外出生点。
            var waitStart = Time.time;
            while (Time.time - waitStart < 2.5f) yield return null;

            var player = GameObject.FindGameObjectWithTag("Player");
            Assert.That(player, Is.Not.Null);
            var stats = player.GetComponent<Character.CharacterStats>();
            var martialArts = player.GetComponent<Character.MartialArtsSystem>();
            Assert.That(stats, Is.Not.Null);
            Assert.That(martialArts, Is.Not.Null);

            // 制造可观察状态：掉血、扣蓝、升级、学一个非初始武学。
            stats.currentHp = 63;
            stats.currentMp = 21;
            stats.level = 3;
            stats.exp = 20;
            Assert.That(
                martialArts.LearnSkill(GameSystem.MartialSkillDatabase.Get("basic_slash")),
                Is.True, "sanity: basic_slash should be learnable before the round trip");

            // —— 镇 → 客栈：把玩家放进客栈大门触发器，走真实物理转场 ——
            var innDoor = Object.FindObjectsByType<Map.AreaTrigger>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .First(trigger => trigger.targetSceneName == "Demo_Inn");
            var expectedInnSpawn = innDoor.spawnPositionInTarget;
            player.transform.position = innDoor.transform.position;
            yield return WaitForActiveScene("Demo_Inn");

            player = GameObject.FindGameObjectWithTag("Player");
            Assert.That(player, Is.Not.Null, "客栈场景必须存在玩家");
            AssertRoundTripState(player, expectedInnSpawn);

            // —— 客栈 → 镇：把玩家放进客栈出口触发器，真实物理转场回镇 ——
            var exitDoor = Object.FindObjectsByType<Map.AreaTrigger>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .First(trigger => trigger.targetSceneName == "Demo_YanLiuTown");
            var expectedReturnSpawn = exitDoor.spawnPositionInTarget;
            player.transform.position = exitDoor.transform.position;
            yield return WaitForActiveScene("Demo_YanLiuTown");

            player = GameObject.FindGameObjectWithTag("Player");
            Assert.That(player, Is.Not.Null, "回镇后必须存在玩家");
            AssertRoundTripState(player, expectedReturnSpawn);
        }

        private static IEnumerator WaitForActiveScene(string sceneName)
        {
            var start = Time.realtimeSinceStartup;
            while (SceneManager.GetActiveScene().name != sceneName)
            {
                if (Time.realtimeSinceStartup - start > 20f)
                {
                    Assert.Fail($"场景切换超时: 期望 {sceneName}, " +
                        $"当前 {SceneManager.GetActiveScene().name}");
                    yield break;
                }
                yield return null;
            }
            // sceneLoaded 回调（落点与状态回放）在场景激活后执行。
            yield return null;
            yield return null;
        }

        private static void AssertRoundTripState(GameObject player, Vector2 expectedSpawn)
        {
            Assert.That(player.transform.position.x,
                Is.EqualTo(expectedSpawn.x).Within(0.05f), "转场落点 X");
            Assert.That(player.transform.position.y,
                Is.EqualTo(expectedSpawn.y).Within(0.05f), "转场落点 Y");

            var stats = player.GetComponent<Character.CharacterStats>();
            var martialArts = player.GetComponent<Character.MartialArtsSystem>();
            Assert.That(stats.currentHp, Is.EqualTo(63), "HP 必须经转场携带保持");
            Assert.That(stats.currentMp, Is.EqualTo(21), "MP 必须经转场携带保持");
            Assert.That(stats.level, Is.EqualTo(3), "等级必须经转场携带保持");
            Assert.That(stats.exp, Is.EqualTo(20), "经验必须经转场携带保持");
            Assert.That(martialArts.GetSaveData().learnedSkillIds,
                Does.Contain("basic_slash"), "已学武学必须经转场携带保持");

            Assert.That(GameManager.Instance.WeaponStyleId, Is.EqualTo("dart"),
                "武器流派必须跨场景保持");
            Assert.That(player.GetComponent<Character.PlayerCombat>().WeaponStyleId,
                Is.EqualTo("dart"), "战斗组件流派必须随场景玩家重建");
            Assert.That(player.GetComponent<CharacterVisual>().ArtId,
                Is.EqualTo("player_male_swordsman"), "固定男主外观必须保持");
            Assert.That(GameManager.Instance.CanPlayerAct(), Is.True,
                "转场后玩家必须重新可控");
            Assert.That(player.GetComponent<Character.PlayerController>().IsInputEnabled,
                Is.True, "转场后输入必须恢复");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid())
            {
                foreach (var root in activeScene.GetRootGameObjects())
                    Object.Destroy(root);
            }
            if (GameManager.Instance != null)
                Object.Destroy(GameManager.Instance.gameObject);
            yield return null;
            yield return null;
        }
    }
}
#endif
