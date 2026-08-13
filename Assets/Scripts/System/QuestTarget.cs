using UnityEngine;
using YuanHaiLu.Character;

namespace YuanHaiLu.GameSystem
{
    /// <summary>
    /// 将敌人死亡转换为任务击杀目标进度。
    /// </summary>
    [RequireComponent(typeof(CharacterStats))]
    public class QuestTarget : MonoBehaviour
    {
        public QuestObjective.ObjectiveType objectiveType = QuestObjective.ObjectiveType.KillEnemy;
        public string targetId = "";
        public int amount = 1;

        private CharacterStats _stats;
        private bool _reported;

        private void Awake()
        {
            _stats = GetComponent<CharacterStats>();
            if (_stats != null)
                _stats.OnDeath += ReportDefeat;
        }

        private void OnDestroy()
        {
            if (_stats != null)
                _stats.OnDeath -= ReportDefeat;
        }

        public void ReportDefeat()
        {
            if (_reported || string.IsNullOrEmpty(targetId)) return;
            if (objectiveType != QuestObjective.ObjectiveType.KillEnemy &&
                objectiveType != QuestObjective.ObjectiveType.DefeatBoss)
            {
                return;
            }

            // 与 AreaTrigger.ReportAreaReached 一致：仅当真正匹配到活跃任务目标时才锁定。
            // 当前 CharacterStats.OnDeath 仅触发一次，故此改动功能上无变化，
            // 仅为模式统一，并防御未来若 OnDeath 被重复触发导致的重复计数。
            bool matched = QuestManager.Instance != null && QuestManager.Instance.UpdateObjective(
                objectiveType,
                targetId,
                Mathf.Max(1, amount));

            if (matched)
            {
                _reported = true;
            }
        }
    }
}
