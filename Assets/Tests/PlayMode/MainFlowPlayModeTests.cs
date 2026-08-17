#if UNITY_EDITOR
using System.Collections;
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
