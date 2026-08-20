using YuanHaiLu.GameSystem;
using UnityEngine.Events;
using UnityEngine;
using UnityEngine.SceneManagement;
using YuanHaiLu.Core;

namespace YuanHaiLu.Map
{
    /// <summary>
    /// 区域触发器 v2 — 区域名称显示 + 场景切换
    /// 挂载到区域边界空物体上，需要 Collider2D（Is Trigger）
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class AreaTrigger : MonoBehaviour
    {
        [Header("区域信息")]
        public string areaName = "烟柳镇";
        public string areaSubtitle = "渊朝·江南道";
        public string questTargetId = "";

        [Header("场景切换（可选）")]
        public bool triggersSceneChange = false;
        public string targetSceneName = "";
        public Vector2 spawnPositionInTarget = Vector2.zero;

        public enum TransitionDirection { Up, Down, Left, Right, Custom }
        [Header("传送门方向")]
        public TransitionDirection transitionDir = TransitionDirection.Custom;

        [Header("设置")]
        public float fadeDuration = 0.5f;
        public bool showOnce = true;

        private bool _hasShown = false;
        private bool _questProgressReported = false;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            // 任务进度先于"只显示一次"门：玩家可能在接任务前就路过此地，
            // 接任务后再次进入时仍必须能上报 ReachArea（上报自身有成功锁定）。
            ReportAreaReached();

            if (showOnce && _hasShown) return;

            if (triggersSceneChange && !string.IsNullOrEmpty(targetSceneName))
            {
                StartCoroutine(TransitionToScene(other.gameObject));
            }
            else
            {
                ShowAreaName();
            }
        }

        private void ShowAreaName()
        {
            _hasShown = true;

            // 调用屏幕过渡系统显示地名
            var transition = GameSystem.ScreenTransition.Instance;
            if (transition != null)
            {
                transition.ShowAreaName(areaName, areaSubtitle, 2.5f);
            }
            else
            {
                Debug.Log($"[AreaTrigger] 进入区域: {areaName} — {areaSubtitle}");
            }
        }

        internal void ReportAreaReached()
        {
            if (string.IsNullOrEmpty(questTargetId)) return;
            if (_questProgressReported) return;

            QuestManager questManager = QuestManager.Instance;
            if (questManager != null && questManager.UpdateObjective(
                    QuestObjective.ObjectiveType.ReachArea,
                    questTargetId))
            {
                _questProgressReported = true;
            }
        }

        private System.Collections.IEnumerator TransitionToScene(GameObject player)
        {
            // 锁定玩家输入
            var controller = player.GetComponent<Character.PlayerController>();
            if (controller != null) controller.SetInputEnabled(false);

            // 淡出
            var transition = GameSystem.ScreenTransition.Instance;
            if (transition != null)
            {
                bool done = false;
                transition.FadeOut(() => done = true);
                yield return new WaitUntil(() => done);
            }

            // 自动存档
            var saveManager = GameSystem.SaveManager.Instance;
            if (saveManager != null) saveManager.SaveGame(-1);

            var gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                Debug.LogError("[AreaTrigger] GameManager 缺失，无法切换场景！");
                if (controller != null) controller.SetInputEnabled(true);
                yield break;
            }

            // HP/MP/基础属性/武学挂在场景本地玩家上，切换前捕获、落地后回放。
            gameManager.BeginTransitionCarry(player);

            // 加载目标场景
            Debug.Log($"[AreaTrigger] 切换场景: {targetSceneName}");

            // 标记为"场景过渡"而非新游戏：SceneDirector 将跳过出生点/初始物资覆盖。
            gameManager.BeginSceneEntry(GameManager.SceneEntryMode.SceneTransition);

            // 单次具名回调：新场景加载后把玩家放到指定入口，并回放过渡携带。
            // 闭包只捕获值类型与持久 GameManager 引用（不捕获 this），
            // 因为 Single 加载会销毁当前场景的 AreaTrigger，协程随之停止。
            Vector2 targetSpawn = spawnPositionInTarget;
            UnityAction<Scene, LoadSceneMode> onSceneLoaded = null;
            onSceneLoaded = (scene, mode) =>
            {
                SceneManager.sceneLoaded -= onSceneLoaded;
                var newPlayer = GameObject.FindGameObjectWithTag("Player");
                if (newPlayer != null)
                {
                    newPlayer.transform.position = targetSpawn;
                    gameManager.ApplyTransitionCarry(newPlayer);
                }
                gameManager.CompleteSceneEntry();
            };
            SceneManager.sceneLoaded += onSceneLoaded;

            SceneManager.LoadScene(targetSceneName);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                Gizmos.color = triggersSceneChange
                    ? new Color(0f, 0.5f, 1f, 0.3f)
                    : new Color(0.5f, 1f, 0.5f, 0.3f);
                Gizmos.DrawCube(transform.position, col.bounds.size);
            }
        }
#endif
    }
}
