using UnityEngine;

namespace YuanHaiLu.GameSystem
{
    /// <summary>
    /// 任务阶段门（复审 P0 修复）：把一组场景对象的激活状态绑定到
    /// 顺序任务的当前目标，防止玩家在接任务或到达对应阶段之前
    /// 提前击杀敌人/拾取任务物品，导致 MVP_01 永久软锁。
    /// 仅适用于 sequentialObjectives 任务：门在"第一个未完成目标"
    /// 恰好为本目标时开启，其余情况（未接取/已完成/其他阶段）关闭。
    /// </summary>
    public class QuestStageGate : MonoBehaviour
    {
        [Header("门控任务与目标")]
        public string questId = "";
        public QuestObjective.ObjectiveType objectiveType = QuestObjective.ObjectiveType.KillEnemy;
        public string targetId = "";

        [Header("受控对象（未轮到阶段时整体失活）")]
        public GameObject[] targets = new GameObject[0];

        private QuestManager _subscribed;

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            if (_subscribed == null) return;
            _subscribed.OnQuestAccepted -= HandleQuestChanged;
            _subscribed.OnQuestUpdated -= HandleQuestChanged;
            _subscribed.OnObjectiveUpdated -= HandleObjectiveChanged;
            _subscribed.OnQuestCompleted -= HandleQuestDataChanged;
            _subscribed = null;
        }

        private void Start()
        {
            // QuestManager 若与本门同帧创建，OnEnable 时可能尚未就绪，这里补订阅。
            Subscribe();

            // 读档恢复发生在 sceneLoaded 回调（早于 Start），此时刷新即可覆盖存档中间状态。
            Refresh();
        }

        private void Subscribe()
        {
            if (_subscribed != null) return;
            var quests = QuestManager.Instance;
            if (quests == null) return;

            quests.OnQuestAccepted += HandleQuestChanged;
            quests.OnQuestUpdated += HandleQuestChanged;
            quests.OnObjectiveUpdated += HandleObjectiveChanged;
            quests.OnQuestCompleted += HandleQuestDataChanged;
            _subscribed = quests;

            Refresh();
        }

        private void HandleQuestChanged(ActiveQuest quest) => Refresh();

        private void HandleQuestDataChanged(QuestData quest) => Refresh();

        private void HandleObjectiveChanged(QuestObjective objective) => Refresh();

        private void Refresh()
        {
            bool active = ShouldBeActive();
            foreach (var target in targets)
            {
                if (target == null) continue;
                if (target.activeSelf != active)
                    target.SetActive(active);
            }
        }

        private bool ShouldBeActive()
        {
            var quests = QuestManager.Instance;
            if (quests == null || string.IsNullOrEmpty(questId)) return false;

            var quest = quests.GetActiveQuest(questId);
            if (quest?.data == null) return false;
            if (quest.state == ActiveQuest.QuestState.Completed ||
                quest.state == ActiveQuest.QuestState.Failed)
            {
                return false;
            }

            foreach (var objective in quest.Objectives)
            {
                if (objective.completed) continue;
                // 第一个未完成目标即顺序门：只有轮到本目标时才放行。
                return objective.type == objectiveType && objective.targetId == targetId;
            }
            return false;
        }
    }
}
