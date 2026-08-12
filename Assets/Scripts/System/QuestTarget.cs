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

            _reported = true;
            QuestManager.Instance?.UpdateObjective(
                objectiveType,
                targetId,
                Mathf.Max(1, amount));
        }
    }
}
