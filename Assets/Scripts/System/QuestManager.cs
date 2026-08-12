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

        public float Progress => (float)currentAmount / Mathf.Max(1, requiredAmount);
        public string ProgressText => $"{currentAmount}/{requiredAmount}";

        public QuestObjective CloneForRuntime()
        {
            return new QuestObjective
            {
                type = type,
                targetId = targetId,
                targetName = targetName,
                requiredAmount = Mathf.Max(1, requiredAmount),
                currentAmount = Mathf.Max(0, currentAmount),
                completed = completed
            };
        }
    }

    /// <summary>
    /// 运行时任务实例
    /// </summary>
    public class ActiveQuest
    {
        public QuestData data;
        public QuestState state;
        public System.DateTime acceptTime;
        public QuestObjective[] Objectives { get; }

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
            QuestObjective[] templates = questData?.objectives ?? Array.Empty<QuestObjective>();
            Objectives = new QuestObjective[templates.Length];
            for (int i = 0; i < templates.Length; i++)
            {
                Objectives[i] = templates[i]?.CloneForRuntime() ?? new QuestObjective();
            }

            CheckCompletion();
        }

        public bool AllObjectivesComplete()
        {
            foreach (var obj in Objectives)
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
            if (quest == null || string.IsNullOrEmpty(quest.questId)) return false;

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
            QuestData questData = QuestDatabase.Get(questId);
            if (questData == null)
            {
                Debug.LogWarning($"[Quest] 任务模板不存在: {questId}");
                return false;
            }

            return AcceptQuest(questData);
        }

        public bool CanAcceptQuestById(string questId)
        {
            QuestData quest = QuestDatabase.Get(questId);
            if (quest == null || completedQuestIds.Contains(questId)) return false;
            if (activeQuests.Exists(active => active.data?.questId == questId)) return false;
            if (activeQuests.Count >= maxActiveQuests) return false;
            return CheckPrerequisites(quest);
        }

        // === 更新目标进度 ===
        public void UpdateObjective(QuestObjective.ObjectiveType type, string targetId, int amount = 1)
        {
            foreach (var quest in activeQuests)
            {
                if (quest.state != ActiveQuest.QuestState.Active) continue;

                foreach (var obj in quest.Objectives)
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
        public bool CompleteQuest(string questId)
        {
            ActiveQuest quest = activeQuests.Find(q => q.data?.questId == questId);

            if (quest == null || quest.state != ActiveQuest.QuestState.Completable)
            {
                Debug.LogWarning($"[Quest] 任务无法完成: {questId}");
                return false;
            }

            // 先记录完成状态，防止奖励或事件回调重入后重复结算。
            quest.state = ActiveQuest.QuestState.Completed;
            activeQuests.Remove(quest);
            if (!completedQuestIds.Contains(questId))
                completedQuestIds.Add(questId);

            GrantRewards(quest.data);

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
            return true;
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
                var skill = MartialSkillDatabase.Get(quest.rewardSkillId);
                var martial = player.GetComponent<Character.MartialArtsSystem>();
                if (skill == null)
                {
                    Debug.LogWarning($"[Quest] 奖励武学不存在: {quest.rewardSkillId}");
                }
                else if (martial == null)
                {
                    Debug.LogWarning("[Quest] 玩家缺少 MartialArtsSystem，无法发放武学奖励");
                }
                else if (martial.LearnSkill(skill))
                {
                    Debug.Log($"[Quest] 获得武学: {skill.skillName}");
                }
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
            return activeQuests.Find(q => q.data?.questId == questId);
        }

        public bool IsQuestActive(string questId)
        {
            return activeQuests.Exists(q => q.data?.questId == questId);
        }

        public bool IsQuestCompleted(string questId)
        {
            return completedQuestIds.Contains(questId);
        }

        // === 存档支持 ===
        [System.Serializable]
        public class QuestObjectiveSaveData
        {
            public QuestObjective.ObjectiveType type;
            public string targetId;
            public int currentAmount;
        }

        [System.Serializable]
        public class ActiveQuestSaveData
        {
            public string questId;
            public ActiveQuest.QuestState state;
            public long acceptTimeBinary;
            public QuestObjectiveSaveData[] objectives;
        }

        [System.Serializable]
        public class QuestSaveData
        {
            public ActiveQuestSaveData[] activeQuests;
            public string[] completedQuestIds;
        }

        public QuestSaveData GetSaveData()
        {
            var savedActiveQuests = new List<ActiveQuestSaveData>();
            foreach (ActiveQuest activeQuest in activeQuests)
            {
                if (activeQuest?.data == null || string.IsNullOrEmpty(activeQuest.data.questId))
                    continue;

                var savedObjectives = new QuestObjectiveSaveData[activeQuest.Objectives.Length];
                for (int i = 0; i < activeQuest.Objectives.Length; i++)
                {
                    QuestObjective objective = activeQuest.Objectives[i];
                    savedObjectives[i] = new QuestObjectiveSaveData
                    {
                        type = objective.type,
                        targetId = objective.targetId,
                        currentAmount = objective.currentAmount
                    };
                }

                savedActiveQuests.Add(new ActiveQuestSaveData
                {
                    questId = activeQuest.data.questId,
                    state = activeQuest.state,
                    acceptTimeBinary = activeQuest.acceptTime.ToBinary(),
                    objectives = savedObjectives
                });
            }

            return new QuestSaveData
            {
                activeQuests = savedActiveQuests.ToArray(),
                completedQuestIds = completedQuestIds.ToArray()
            };
        }

        public void LoadSaveData(QuestSaveData data)
        {
            activeQuests.Clear();
            completedQuestIds.Clear();

            if (data == null) return;

            if (data.completedQuestIds != null)
            {
                foreach (string id in data.completedQuestIds)
                {
                    if (!string.IsNullOrEmpty(id) && !completedQuestIds.Contains(id))
                        completedQuestIds.Add(id);
                }
            }

            if (data.activeQuests == null) return;

            var restoredIds = new HashSet<string>();
            foreach (ActiveQuestSaveData savedQuest in data.activeQuests)
            {
                if (savedQuest == null || string.IsNullOrEmpty(savedQuest.questId)) continue;
                if (!restoredIds.Add(savedQuest.questId)) continue;

                if (savedQuest.state == ActiveQuest.QuestState.Completed)
                {
                    if (!completedQuestIds.Contains(savedQuest.questId))
                        completedQuestIds.Add(savedQuest.questId);
                    continue;
                }

                if (completedQuestIds.Contains(savedQuest.questId)) continue;

                QuestData template = QuestDatabase.Get(savedQuest.questId);
                if (template == null)
                {
                    Debug.LogWarning($"[Quest] 存档中的任务模板不存在，已跳过: {savedQuest.questId}");
                    continue;
                }

                var activeQuest = new ActiveQuest(template);
                if (savedQuest.acceptTimeBinary != 0)
                {
                    try
                    {
                        activeQuest.acceptTime = DateTime.FromBinary(savedQuest.acceptTimeBinary);
                    }
                    catch (ArgumentException)
                    {
                        activeQuest.acceptTime = DateTime.Now;
                    }
                }

                RestoreObjectiveProgress(activeQuest, savedQuest.objectives);
                activeQuest.state = savedQuest.state == ActiveQuest.QuestState.Failed
                    ? ActiveQuest.QuestState.Failed
                    : ActiveQuest.QuestState.Active;
                activeQuest.CheckCompletion();
                activeQuests.Add(activeQuest);
            }
        }

        private static void RestoreObjectiveProgress(
            ActiveQuest activeQuest,
            QuestObjectiveSaveData[] savedObjectives)
        {
            foreach (QuestObjective objective in activeQuest.Objectives)
            {
                objective.currentAmount = 0;
                objective.completed = false;
            }

            if (savedObjectives == null) return;

            foreach (QuestObjectiveSaveData savedObjective in savedObjectives)
            {
                if (savedObjective == null || string.IsNullOrEmpty(savedObjective.targetId)) continue;

                foreach (QuestObjective objective in activeQuest.Objectives)
                {
                    if (objective.type != savedObjective.type ||
                        objective.targetId != savedObjective.targetId)
                    {
                        continue;
                    }

                    objective.currentAmount = Mathf.Clamp(
                        savedObjective.currentAmount,
                        0,
                        objective.requiredAmount);
                    objective.completed = objective.currentAmount >= objective.requiredAmount;
                    break;
                }
            }
        }

        /// <summary>
        /// 导出已完成任务 ID 列表（供 SaveManager 保存）
        /// </summary>
        public string[] GetCompletedQuests()
        {
            return completedQuestIds.ToArray();
        }

        /// <summary>
        /// 从存档恢复已完成任务清单
        /// （注：活跃任务因缺任务数据库 ScriptableObject 暂不持久化）
        /// </summary>
        public void LoadCompletedQuests(string[] ids)
        {
            activeQuests.Clear();
            completedQuestIds.Clear();
            if (ids == null) return;
            foreach (var id in ids)
            {
                if (!string.IsNullOrEmpty(id) && !completedQuestIds.Contains(id))
                    completedQuestIds.Add(id);
            }
        }

        public void ResetForNewGame()
        {
            activeQuests.Clear();
            completedQuestIds.Clear();
        }
    }
}
