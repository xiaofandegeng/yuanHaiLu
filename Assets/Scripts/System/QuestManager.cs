using UnityEngine;
using System;
using System.Collections.Generic;

namespace YuanHaiLu.GameSystem
{
    /// <summary>
    /// 任务数据定义（ScriptableObject）
    /// </summary>
    [CreateAssetMenu(fileName = "NewQuest", menuName = "渊海录/任务")]
    public class QuestData : ScriptableObject
    {
        public string questId;              // 唯一ID
        public string questName;            // 显示名称
        [TextArea(2, 4)] public string description; // 描述
        public Sprite questGiverPortrait;   // 发布者头像
        public QuestType type;              // 类型
        public QuestRarity rarity;          // 重要度

        [Header("前置条件")]
        public string[] prerequisiteQuests;  // 前置任务ID
        public int requiredLevel;           // 等级要求
        public string requiredChapter;      // 章节要求

        [Header("目标")]
        public QuestObjective[] objectives;  // 任务目标列表

        [Header("奖励")]
        public int rewardExp;               // 经验奖励
        public int rewardGold;              // 金钱奖励
        public string[] rewardItemIds;       // 物品奖励ID
        public string rewardSkillId;        // 武学奖励ID
        public string[] unlockQuestIds;     // 解锁的后续任务

        [Header("对话")]
        [TextArea(2, 4)] public string[] introDialogue;    // 接取对话
        [TextArea(2, 4)] public string[] progressDialogue;  // 进行中对话
        [TextArea(2, 4)] public string[] completeDialogue;  // 完成对话

        public enum QuestType
        {
            MainStory,      // 主线
            SideQuest,      // 支线
            Bounty,         // 悬赏
            Collection,     // 收集
            Escort,         // 护送
            Investigation   // 调查
        }

        public enum QuestRarity
        {
            Normal,
            Important,
            Critical
        }
    }

    /// <summary>
    /// 任务目标
    /// </summary>
    [System.Serializable]
    public class QuestObjective
    {
        public ObjectiveType type;
        public string targetId;             // 目标ID（敌人/物品/NPC的ID）
        public string targetName;           // 显示名
        public int requiredAmount = 1;      // 需要数量
        public int currentAmount;           // 当前进度
        public bool completed;              // 是否完成

        public enum ObjectiveType
        {
            KillEnemy,      // 击杀敌人
            CollectItem,    // 收集物品
            TalkToNPC,      // 与NPC对话
            ReachArea,      // 到达区域
            DefeatBoss,     // 击败BOSS
            LearnSkill,     // 学习武学
            CraftItem       // 制作物品
        }

        public float Progress => (float)currentAmount / requiredAmount;
        public string ProgressText => $"{currentAmount}/{requiredAmount}";
    }

    /// <summary>
    /// 运行时任务实例
    /// </summary>
    public class ActiveQuest
    {
        public QuestData data;
        public QuestState state;
        public System.DateTime acceptTime;

        public enum QuestState
        {
            Available,      // 可接取
            Active,         // 进行中
            Completable,    // 可提交
            Completed,      // 已完成
            Failed          // 已失败
        }

        public ActiveQuest(QuestData questData)
        {
            data = questData;
            state = QuestState.Active;
            acceptTime = System.DateTime.Now;
        }

        public bool AllObjectivesComplete()
        {
            foreach (var obj in data.objectives)
            {
                if (!obj.completed) return false;
            }
            return true;
        }

        public void CheckCompletion()
        {
            if (AllObjectivesComplete())
            {
                state = QuestState.Completable;
            }
        }
    }

    /// <summary>
    /// 任务管理器 — 任务接取、追踪、完成
    /// 挂载到 GameManager 下
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        [Header("当前任务")]
        [SerializeField] private List<ActiveQuest> activeQuests = new List<ActiveQuest>();
        [SerializeField] private int maxActiveQuests = 10;

        [Header("已完成任务")]
        [SerializeField] private List<string> completedQuestIds = new List<string>();

        // === 事件 ===
        public event System.Action<ActiveQuest> OnQuestAccepted;
        public event System.Action<ActiveQuest> OnQuestUpdated;
        public event System.Action<QuestData> OnQuestCompleted;
        public event System.Action<QuestObjective> OnObjectiveUpdated;

        public List<ActiveQuest> ActiveQuests => activeQuests;
        public List<string> CompletedQuestIds => completedQuestIds;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // === 接取任务 ===
        public bool AcceptQuest(QuestData quest)
        {
            if (quest == null) return false;

            // 检查是否已完成
            if (completedQuestIds.Contains(quest.questId))
            {
                Debug.Log($"[Quest] 任务已完成: {quest.questName}");
                return false;
            }

            // 检查是否已接取
            foreach (var aq in activeQuests)
            {
                if (aq.data.questId == quest.questId)
                {
                    Debug.Log($"[Quest] 任务已在进行中: {quest.questName}");
                    return false;
                }
            }

            // 检查任务上限
            if (activeQuests.Count >= maxActiveQuests)
            {
                Debug.LogWarning("[Quest] 活跃任务数量已达上限！");
                return false;
            }

            // 检查前置条件
            if (!CheckPrerequisites(quest))
            {
                Debug.Log($"[Quest] 前置条件未满足: {quest.questName}");
                return false;
            }

            var newQuest = new ActiveQuest(quest);
            activeQuests.Add(newQuest);

            OnQuestAccepted?.Invoke(newQuest);
            Debug.Log($"[Quest] 接取任务: {quest.questName}");
            return true;
        }

        /// <summary>
        /// 通过ID接取任务（用于对话系统触发）
        /// </summary>
        public bool AcceptQuestById(string questId)
        {
            // 尝试从预置任务模板中查找
            var questData = new QuestData { questId = questId, questName = questId };
            return AcceptQuest(questData);
        }

        // === 更新目标进度 ===
        public void UpdateObjective(QuestObjective.ObjectiveType type, string targetId, int amount = 1)
        {
            foreach (var quest in activeQuests)
            {
                if (quest.state != ActiveQuest.QuestState.Active) continue;

                foreach (var obj in quest.data.objectives)
                {
                    if (obj.type == type && obj.targetId == targetId && !obj.completed)
                    {
                        obj.currentAmount = Mathf.Min(obj.currentAmount + amount, obj.requiredAmount);
                        OnObjectiveUpdated?.Invoke(obj);

                        if (obj.currentAmount >= obj.requiredAmount)
                        {
                            obj.completed = true;
                            Debug.Log($"[Quest] 目标完成: {obj.targetName} ({obj.ProgressText})");
                        }

                        quest.CheckCompletion();
                        OnQuestUpdated?.Invoke(quest);
                    }
                }
            }
        }

        // === 完成任务 ===
        public void CompleteQuest(string questId)
        {
            ActiveQuest quest = activeQuests.Find(q => q.data.questId == questId);

            if (quest == null || quest.state != ActiveQuest.QuestState.Completable)
            {
                Debug.LogWarning($"[Quest] 任务无法完成: {questId}");
                return;
            }

            // 发放奖励
            GrantRewards(quest.data);

            // 标记完成
            quest.state = ActiveQuest.QuestState.Completed;
            activeQuests.Remove(quest);
            completedQuestIds.Add(questId);

            // 解锁后续任务
            if (quest.data.unlockQuestIds != null)
            {
                foreach (string unlockId in quest.data.unlockQuestIds)
                {
                    Debug.Log($"[Quest] 解锁后续任务: {unlockId}");
                }
            }

            OnQuestCompleted?.Invoke(quest.data);
            Debug.Log($"[Quest] 任务完成: {quest.data.questName}！");
        }

        // === 奖励发放 ===
        private void GrantRewards(QuestData quest)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            // 经验
            if (quest.rewardExp > 0)
            {
                var stats = player.GetComponent<Character.CharacterStats>();
                if (stats != null)
                {
                    stats.GainExp(quest.rewardExp);
                    Debug.Log($"[Quest] 获得 {quest.rewardExp} 经验");
                }
            }

            // 金钱
            if (quest.rewardGold > 0)
            {
                var inventory = InventoryManager.Instance;
                if (inventory != null)
                {
                    inventory.AddGold(quest.rewardGold);
                    Debug.Log($"[Quest] 获得 {quest.rewardGold} 文钱");
                }
            }

            // 物品
            if (quest.rewardItemIds != null)
            {
                var inventory = InventoryManager.Instance;
                foreach (string itemId in quest.rewardItemIds)
                {
                    inventory?.AddItem(itemId);
                }
            }

            // 武学
            if (!string.IsNullOrEmpty(quest.rewardSkillId))
            {
                Debug.Log($"[Quest] 获得武学: {quest.rewardSkillId}");
                // TODO: 通过 InventoryManager 学习武学
            }
        }

        // === 前置条件检查 ===
        private bool CheckPrerequisites(QuestData quest)
        {
            // 检查前置任务
            if (quest.prerequisiteQuests != null)
            {
                foreach (string preq in quest.prerequisiteQuests)
                {
                    if (!completedQuestIds.Contains(preq))
                        return false;
                }
            }

            // 检查等级
            if (quest.requiredLevel > 0)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    var stats = player.GetComponent<Character.CharacterStats>();
                    if (stats != null && stats.level < quest.requiredLevel)
                        return false;
                }
            }

            return true;
        }

        // === 查询 ===
        public ActiveQuest GetActiveQuest(string questId)
        {
            return activeQuests.Find(q => q.data.questId == questId);
        }

        public bool IsQuestActive(string questId)
        {
            return activeQuests.Exists(q => q.data.questId == questId);
        }

        public bool IsQuestCompleted(string questId)
        {
            return completedQuestIds.Contains(questId);
        }
    }
}
