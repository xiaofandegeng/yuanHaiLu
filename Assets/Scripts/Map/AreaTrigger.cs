using YuanHaiLu.GameSystem;
using UnityEngine;
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
            if (showOnce && _hasShown) return;

            ReportAreaReached();

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

            // 加载目标场景
            Debug.Log($"[AreaTrigger] 切换场景: {targetSceneName}");
            UnityEngine.SceneManagement.SceneManager.LoadScene(targetSceneName);
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
