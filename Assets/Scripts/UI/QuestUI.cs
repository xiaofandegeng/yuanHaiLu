using UnityEngine;
using YuanHaiLu.Core;
using UnityEngine.UI;
using YuanHaiLu.Core;
using System.Collections.Generic;
using YuanHaiLu.GameSystem;

namespace YuanHaiLu.UI
{
    /// <summary>
    /// 任务日志界面 — 显示当前任务列表和详情
    /// 挂载到 QuestCanvas 下
    /// </summary>
    public class QuestUI : MonoBehaviour
    {
        [Header("面板")]
        [SerializeField] private GameObject questPanel;
        [SerializeField] private bool startClosed = true;

        [Header("任务列表")]
        [SerializeField] private Transform questListContainer;
        [SerializeField] private GameObject questEntryPrefab;

        [Header("任务详情")]
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private Text questNameText;
        [SerializeField] private Text questTypeText;
        [SerializeField] private Text questDescText;
        [SerializeField] private Transform objectivesContainer;
        [SerializeField] private GameObject objectiveEntryPrefab;
        [SerializeField] private Text rewardText;

        [Header("选项按钮")]
        [SerializeField] private GameObject abandonButton;
        [SerializeField] private GameObject completeButton;

        [Header("筛选")]
        [SerializeField] private int currentFilter = 0; // 0=全部 1=主线 2=支线

        private bool _isOpen = false;
        private ActiveQuest _selectedQuest;
        private List<GameObject> _entryObjects = new List<GameObject>();

        private void Start()
        {
            if (startClosed) Close();

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestAccepted += OnQuestChanged;
                QuestManager.Instance.OnQuestUpdated += OnQuestChanged;
                QuestManager.Instance.OnQuestCompleted += OnQuestCompleted;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                if (_isOpen) Close();
                else Open();
            }

            if (Input.GetKeyDown(KeyCode.Escape) && _isOpen)
            {
                Close();
            }
        }

        public void Open()
        {
            _isOpen = true;
            questPanel.SetActive(true);

            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameManager.GameState.Menu);

            RefreshList();
        }

        public void Close()
        {
            _isOpen = false;
            questPanel.SetActive(false);
            _selectedQuest = null;

            if (GameManager.Instance != null &&
                GameManager.Instance.currentState == GameManager.GameState.Menu)
            {
                GameManager.Instance.SetState(GameManager.GameState.Exploration);
            }
        }

        public void RefreshList()
        {
            // 清除旧条目
            foreach (var obj in _entryObjects)
            {
                Destroy(obj);
            }
            _entryObjects.Clear();

            if (QuestManager.Instance == null) return;

            foreach (var quest in QuestManager.Instance.ActiveQuests)
            {
                // 筛选
                if (currentFilter == 1 && quest.data.type != QuestData.QuestType.MainStory) continue;
                if (currentFilter == 2 && quest.data.type == QuestData.QuestType.MainStory) continue;

                GameObject entry = CreateQuestEntry(quest);
                _entryObjects.Add(entry);
            }
        }

        private GameObject CreateQuestEntry(ActiveQuest quest)
        {
            GameObject entry = questEntryPrefab != null
                ? Instantiate(questEntryPrefab, questListContainer)
                : new GameObject("QuestEntry", typeof(RectTransform));

            entry.transform.SetParent(questListContainer, false);

            // 尝试设置文本
            var nameText = entry.GetComponentInChildren<Text>();
            if (nameText != null)
            {
                string prefix = quest.data.type == QuestData.QuestType.MainStory ? "【主线】" :
                                quest.data.type == QuestData.QuestType.SideQuest ? "【支线】" : "【其他】";
                string status = quest.state == ActiveQuest.QuestState.Completable ? " ✅" : "";
                nameText.text = $"{prefix}{quest.data.questName}{status}";
            }

            // 点击选择
            var button = entry.GetComponent<Button>();
            if (button == null) button = entry.AddComponent<Button>();
            button.onClick.AddListener(() => SelectQuest(quest));

            return entry;
        }

        public void SelectQuest(ActiveQuest quest)
        {
            _selectedQuest = quest;

            if (detailPanel != null) detailPanel.SetActive(true);

            // 任务名称
            if (questNameText != null) questNameText.text = quest.data.questName;

            // 类型
            if (questTypeText != null)
            {
                questTypeText.text = quest.data.type switch
                {
                    QuestData.QuestType.MainStory => "主线任务",
                    QuestData.QuestType.SideQuest => "支线任务",
                    QuestData.QuestType.Bounty => "悬赏任务",
                    QuestData.QuestType.Collection => "收集任务",
                    QuestData.QuestType.Escort => "护送任务",
                    QuestData.QuestType.Investigation => "调查任务",
                    _ => "任务"
                };
            }

            // 描述
            if (questDescText != null)
                questDescText.text = quest.data.description;

            // 目标列表
            RefreshObjectives(quest);

            // 奖励
            if (rewardText != null)
            {
                string rewards = "奖励：";
                if (quest.data.rewardExp > 0) rewards += $" {quest.data.rewardExp}经验";
                if (quest.data.rewardGold > 0) rewards += $" {quest.data.rewardGold}文钱";
                if (quest.data.rewardItemIds != null && quest.data.rewardItemIds.Length > 0)
                    rewards += " 及物品";
                rewardText.text = rewards;
            }

            // 按钮
            if (abandonButton != null)
                abandonButton.SetActive(quest.data.type != QuestData.QuestType.MainStory);
            if (completeButton != null)
                completeButton.SetActive(quest.state == ActiveQuest.QuestState.Completable);
        }

        private void RefreshObjectives(ActiveQuest quest)
        {
            if (objectivesContainer == null) return;

            // 清除旧目标
            foreach (Transform child in objectivesContainer)
            {
                Destroy(child.gameObject);
            }

            if (quest.data.objectives == null) return;

            foreach (var obj in quest.data.objectives)
            {
                GameObject objEntry = objectiveEntryPrefab != null
                    ? Instantiate(objectiveEntryPrefab, objectivesContainer)
                    : new GameObject("ObjEntry", typeof(RectTransform));

                objEntry.transform.SetParent(objectivesContainer, false);

                var text = objEntry.GetComponentInChildren<Text>();
                if (text != null)
                {
                    string check = obj.completed ? "✅" : "⬜";
                    text.text = $"{check} {obj.targetName} ({obj.ProgressText})";
                    text.color = obj.completed ? new Color(0.5f, 0.5f, 0.5f) : Color.white;
                }
            }
        }

        // === 按钮回调 ===

        public void OnAbandonButton()
        {
            if (_selectedQuest == null) return;

            QuestManager.Instance.ActiveQuests.Remove(_selectedQuest);
            _selectedQuest = null;
            RefreshList();

            if (detailPanel != null) detailPanel.SetActive(false);
        }

        public void OnCompleteButton()
        {
            if (_selectedQuest == null) return;

            QuestManager.Instance.CompleteQuest(_selectedQuest.data.questId);
            _selectedQuest = null;
            RefreshList();

            if (detailPanel != null) detailPanel.SetActive(false);
        }

        // === 事件 ===

        private void OnQuestChanged(ActiveQuest quest)
        {
            if (_isOpen) RefreshList();
            if (_selectedQuest?.data.questId == quest.data.questId)
                SelectQuest(quest);
        }

        private void OnQuestCompleted(QuestData quest)
        {
            if (_isOpen) RefreshList();

            // 完成特效
            Effects.EffectsManager.LevelUpEffect(Vector3.zero);
        }

        public void OnFilterButton(int filter)
        {
            currentFilter = filter;
            RefreshList();
        }

        private void OnDestroy()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestAccepted -= OnQuestChanged;
                QuestManager.Instance.OnQuestUpdated -= OnQuestChanged;
                QuestManager.Instance.OnQuestCompleted -= OnQuestCompleted;
            }
        }
    }
}
