#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using YuanHaiLu.Character;
using YuanHaiLu.Core;
using YuanHaiLu.Dialogue;
using YuanHaiLu.GameSystem;
using YuanHaiLu.UI;

namespace YuanHaiLu.Tests.PlayMode
{
    /// <summary>
    /// docs/16 阶段 A/B：跨场景后客栈交谈必须由活跃场景的 DialogueUI 响应。
    /// 旧实现的匿名 OnDialogueStart 订阅无法退订，烟柳镇 UI 被卸载后仍留在
    /// 持久 DialogueManager 的调用链里，客栈交谈会先调用已销毁 UI 抛
    /// MissingReferenceException，后续活跃 UI 不再显示（P0）。
    /// </summary>
    public class DialogueLifecyclePlayModeTests
    {
        [UnityTest]
        public IEnumerator InnkeeperDialogueWorksAfterTownDialogueUiIsUnloaded()
        {
            // 收集整条链路的错误日志：MissingReferenceException 必须让测试变红。
            var errors = new List<string>();
            void HandleLog(string condition, string stackTrace, LogType type)
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                    errors.Add($"{type}: {condition}");
            }
            Application.logMessageReceived += HandleLog;
            try
            {
                // —— 主菜单 → 新游戏 → 烟柳镇：镇上 DialogueUI 订阅持久 DialogueManager ——
                var load = EditorSceneManager.LoadSceneAsyncInPlayMode(
                    "Assets/Scenes/MainMenu.unity",
                    new LoadSceneParameters(LoadSceneMode.Single));
                while (!load.isDone) yield return null;
                yield return null;

                var menu = Object.FindAnyObjectByType<MainMenu>();
                Assert.That(menu, Is.Not.Null);
                menu.OnNewGame();
                yield return WaitForActiveScene("Demo_YanLiuTown");

                var townUi = Object.FindAnyObjectByType<DialogueUI>();
                Assert.That(townUi, Is.Not.Null, "sanity: 烟柳镇必须带 DialogueUI");

                // —— 真实 AreaTrigger 转场进客栈：镇上 UI 随场景卸载销毁 ——
                var innDoor = Object.FindObjectsByType<Map.AreaTrigger>(
                        FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                    .First(trigger => trigger.targetSceneName == "Demo_Inn");
                GameObject.FindGameObjectWithTag("Player").transform.position =
                    innDoor.transform.position;
                yield return WaitForActiveScene("Demo_Inn");
                yield return null;

                Assert.That(townUi == null, Is.True,
                    "sanity: 旧场景的 DialogueUI 必须已随场景卸载销毁");

                // —— 活跃场景走真实交互链：掌柜接取 MVP_01 ——
                var innkeeper = GameObject.Find("NPC_掌柜老赵");
                Assert.That(innkeeper, Is.Not.Null, "sanity: 客栈必须有掌柜");
                var player = GameObject.FindGameObjectWithTag("Player");
                Assert.That(player, Is.Not.Null);
                innkeeper.GetComponent<NPCBase>().OnInteract(player);

                var innUi = Object.FindAnyObjectByType<DialogueUI>();
                Assert.That(innUi, Is.Not.Null, "sanity: 客栈必须有自己的 DialogueUI");
                var dialogueBox = innUi.transform.Find("DialogueBox");
                Assert.That(dialogueBox, Is.Not.Null);
                Assert.That(dialogueBox.gameObject.activeSelf, Is.True,
                    "交谈后活跃场景的对话框必须显示（P0 症状：对话框不出现）");

                // —— 正常结束对话 → QuestGiver 才结算接取（保持既有语义） ——
                var dialogueManager = DialogueManager.Instance;
                Assert.That(dialogueManager.IsInDialogue, Is.True);
                dialogueManager.ForceEndDialogue();
                yield return null;

                Assert.That(QuestManager.Instance.IsQuestActive("MVP_01"), Is.True,
                    "对话正常结束后必须接取 MVP_01");

                Assert.That(errors, Is.Empty,
                    "跨场景交谈链路不应产生任何错误日志: " + string.Join(" | ", errors));
            }
            finally
            {
                Application.logMessageReceived -= HandleLog;
            }
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
