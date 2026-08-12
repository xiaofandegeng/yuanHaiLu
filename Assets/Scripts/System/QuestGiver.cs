using UnityEngine;
using YuanHaiLu.Character;
using YuanHaiLu.Dialogue;

namespace YuanHaiLu.GameSystem
{
    /// <summary>
    /// 将 NPC 对话与任务接取、推进、提交串联起来。
    /// 任务状态只在本组件启动的对话正常结束后变更。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NPCBase))]
    public class QuestGiver : MonoBehaviour
    {
        [Header("任务")]
        public string questId;
        public string interactionTargetId;
        public bool canAcceptQuest = true;
        public bool canCompleteQuest = true;

        [Header("完成后的对话")]
        [TextArea(2, 4)]
        public string[] completedDialogue;

        private enum PendingAction
        {
            None,
            AcceptAndReportTalk,
            ReportTalk,
            CompleteQuest
        }

        private DialogueManager _subscribedDialogueManager;
        private PendingAction _pendingAction;

        /// <summary>
        /// 尝试按当前任务状态处理 NPC 交互。
        /// 返回 false 时，NPCBase 会继续播放普通对话。
        /// </summary>
        public bool TryHandleInteraction(GameObject player)
        {
            QuestManager questManager = QuestManager.Instance;
            DialogueManager dialogueManager = DialogueManager.Instance;
            if (questManager == null || dialogueManager == null ||
                dialogueManager.IsInDialogue || string.IsNullOrEmpty(questId))
            {
                return false;
            }

            QuestData quest = QuestDatabase.Get(questId);
            if (quest == null) return false;

            string[] dialogue;
            PendingAction action;
            ActiveQuest activeQuest = questManager.GetActiveQuest(questId);

            if (questManager.IsQuestCompleted(questId))
            {
                dialogue = completedDialogue;
                action = PendingAction.None;
            }
            else if (activeQuest?.state == ActiveQuest.QuestState.Completable && canCompleteQuest)
            {
                dialogue = quest.completeDialogue;
                action = PendingAction.CompleteQuest;
            }
            else if (activeQuest != null)
            {
                dialogue = quest.progressDialogue;
                action = PendingAction.ReportTalk;
            }
            else if (canAcceptQuest && questManager.CanAcceptQuestById(questId))
            {
                dialogue = quest.introDialogue;
                action = PendingAction.AcceptAndReportTalk;
            }
            else
            {
                return false;
            }

            if (dialogue == null || dialogue.Length == 0) return false;

            NPCBase npc = GetComponent<NPCBase>();
            dialogueManager.StartDialogue(npc != null ? npc.npcName : name, dialogue);
            if (!dialogueManager.IsInDialogue) return false;

            ClearPendingAction();
            _pendingAction = action;
            _subscribedDialogueManager = dialogueManager;
            _subscribedDialogueManager.OnDialogueEnd += HandleDialogueEnd;
            return true;
        }

        private void HandleDialogueEnd()
        {
            PendingAction action = _pendingAction;
            ClearPendingAction();

            QuestManager questManager = QuestManager.Instance;
            if (questManager == null) return;

            switch (action)
            {
                case PendingAction.AcceptAndReportTalk:
                    if (questManager.AcceptQuestById(questId))
                        ReportTalkObjective(questManager);
                    break;

                case PendingAction.ReportTalk:
                    ReportTalkObjective(questManager);
                    break;

                case PendingAction.CompleteQuest:
                    questManager.CompleteQuest(questId);
                    break;
            }
        }

        private void ReportTalkObjective(QuestManager questManager)
        {
            if (!string.IsNullOrEmpty(interactionTargetId))
            {
                questManager.UpdateObjective(
                    QuestObjective.ObjectiveType.TalkToNPC,
                    interactionTargetId);
            }
        }

        private void ClearPendingAction()
        {
            if (_subscribedDialogueManager != null)
                _subscribedDialogueManager.OnDialogueEnd -= HandleDialogueEnd;

            _subscribedDialogueManager = null;
            _pendingAction = PendingAction.None;
        }

        private void OnDisable()
        {
            ClearPendingAction();
        }
    }
}
