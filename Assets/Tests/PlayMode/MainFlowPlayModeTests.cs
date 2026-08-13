#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using YuanHaiLu.Art;
using YuanHaiLu.Character;
using YuanHaiLu.Core;
using YuanHaiLu.Dialogue;
using YuanHaiLu.GameSystem;
using YuanHaiLu.Map;
using YuanHaiLu.UI;
using UnityEngine.TestTools.Utils;

namespace YuanHaiLu.Tests.PlayMode
{
    public class MainFlowPlayModeTests
    {
        [UnityTest]
        public IEnumerator AppearanceSelectorMovesKeyboardFocusIntoAndBackOutOfThePanel()
        {
            var load = EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Scenes/MainMenu.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!load.isDone)
                yield return null;
            yield return null;

            var menu = Object.FindAnyObjectByType<MainMenu>();
            menu.OnNewGame();
            yield return null;
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.name,
                Is.EqualTo("Btn_角色_" + PlayerAppearance.Default.ArtId));

            menu.CancelAppearanceSelection();
            yield return null;
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.name,
                Is.EqualTo("Btn_新游戏"));
        }

        [UnityTest]
        public IEnumerator SelectedAppearanceSurvivesMainMenuToFormalDemo()
        {
            var load = EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Scenes/MainMenu.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!load.isDone)
                yield return null;
            yield return null;

            var menu = Object.FindAnyObjectByType<MainMenu>();
            Assert.That(menu, Is.Not.Null);
            menu.OnNewGame();
            menu.SelectAppearance("player_male_boxer");
            menu.ConfirmNewGame();

            yield return null;
            yield return null;

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Demo_YanLiuTown"));
            Assert.That(Object.FindAnyObjectByType<RegionSceneDefinition>()?.SceneId,
                Is.EqualTo("yanliu"));
            Assert.That(GameManager.Instance.PlayerArtId, Is.EqualTo("player_male_boxer"));
            var player = GameObject.FindGameObjectWithTag("Player");
            Assert.That(player, Is.Not.Null);
            Assert.That(player.GetComponent<CharacterVisual>()?.ArtId,
                Is.EqualTo("player_male_boxer"));
        }

        [UnityTest]
        public IEnumerator FormalMainFlowCoversMovementCombatDialoguePauseSaveLoadAndTravel()
        {
            const int saveSlot = 98;
            PlayerPrefs.DeleteKey("YuanHaiLu_SaveSlot_" + saveSlot);
            var load = EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Scenes/MainMenu.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!load.isDone)
                yield return null;
            yield return null;

            var menu = Object.FindAnyObjectByType<MainMenu>();
            menu.OnNewGame();
            menu.SelectAppearance("player_female_mystic");
            menu.ConfirmNewGame();
            float initializationDeadline = Time.realtimeSinceStartup + 4f;
            while (GameManager.Instance.CurrentSceneEntryMode !=
                       GameManager.SceneEntryMode.Active &&
                   Time.realtimeSinceStartup < initializationDeadline)
            {
                yield return null;
            }
            Assert.That(GameManager.Instance.CurrentSceneEntryMode,
                Is.EqualTo(GameManager.SceneEntryMode.Active),
                "Demo SceneDirector must complete real new-game initialization.");
            var player = GameObject.FindGameObjectWithTag("Player");
            Assert.That((Vector2)player.transform.position,
                Is.EqualTo(new Vector2(20.5f, 7.5f))
                    .Using(Vector2ComparerWithEqualsOperator.Instance),
                "Demo intro must spawn the player inside the formal Yanliu bounds.");
            var controller = player.GetComponent<PlayerController>();
            var body = player.GetComponent<Rigidbody2D>();
            Vector2 movementStart = player.transform.position;
            controller.enabled = false;
            typeof(PlayerController).GetField(
                    "_moveInput",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(controller, Vector2.right);
            typeof(PlayerController).GetMethod(
                    "HandleMovement",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(controller, null);
            yield return new WaitForFixedUpdate();
            body.linearVelocity = Vector2.zero;
            controller.enabled = true;
            Assert.That((Vector2)player.transform.position, Is.Not.EqualTo(movementStart));

            controller.FaceDirection(Vector2.down);
            var enemy = Object.FindObjectsByType<EnemyAI>(FindObjectsInactive.Include).First();
            enemy.enabled = false;
            enemy.transform.position = player.transform.position + Vector3.down * 1.2f;
            var enemyStats = enemy.GetComponent<CharacterStats>();
            enemyStats.agility = 0;
            enemyStats.isInvincible = false;
            int hpBefore = enemyStats.currentHp;
            player.SendMessage("StartAttack", SendMessageOptions.RequireReceiver);
            yield return new WaitForSeconds(0.75f);
            Assert.That(enemyStats.currentHp, Is.LessThan(hpBefore));

            var innkeeper = Object.FindObjectsByType<CharacterVisual>(FindObjectsInactive.Include)
                .First(value => value.ArtId == "innkeeper_zhao");
            innkeeper.GetComponent<NPCBase>().OnInteract(player);
            Assert.That(DialogueManager.Instance?.IsInDialogue, Is.True);
            DialogueManager.Instance.ForceEndDialogue();

            var pauseMenu = Object.FindAnyObjectByType<PauseMenu>();
            pauseMenu.Pause();
            Assert.That(pauseMenu.IsPaused, Is.True);
            pauseMenu.Resume();
            Assert.That(pauseMenu.IsPaused, Is.False);

            Vector2 savedPosition = player.transform.position;
            SaveManager.Instance.SaveGame(saveSlot);
            int originalPlayerId = player.GetInstanceID();
            SaveManager.Instance.LoadGame(saveSlot);
            float loadDeadline = Time.realtimeSinceStartup + 5f;
            do
            {
                yield return null;
                player = GameObject.FindGameObjectWithTag("Player");
            } while ((player == null || player.GetInstanceID() == originalPlayerId) &&
                     Time.realtimeSinceStartup < loadDeadline);
            Assert.That(player, Is.Not.Null);
            Assert.That((Vector2)player.transform.position,
                Is.EqualTo(savedPosition).Using(Vector2ComparerWithEqualsOperator.Instance));
            Assert.That(GameManager.Instance.PlayerArtId, Is.EqualTo("player_female_mystic"));

            var portal = Object.FindObjectsByType<AreaTrigger>(FindObjectsInactive.Include)
                .First(value => value.targetSceneName == "inn");
            portal.OnInteract(player);
            float travelDeadline = Time.realtimeSinceStartup + 5f;
            while (SceneManager.GetActiveScene().name != "inn" &&
                   Time.realtimeSinceStartup < travelDeadline)
            {
                yield return null;
            }
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("inn"));
            Assert.That(GameObject.FindGameObjectWithTag("Player")
                .GetComponent<CharacterVisual>().ArtId, Is.EqualTo("player_female_mystic"));
            PlayerPrefs.DeleteKey("YuanHaiLu_SaveSlot_" + saveSlot);
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
            PlayerPrefs.DeleteKey("YuanHaiLu_SaveSlot_98");
            yield return null;
            yield return null;
        }
    }
}
#endif
