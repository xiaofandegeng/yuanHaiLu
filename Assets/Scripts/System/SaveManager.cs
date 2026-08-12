using UnityEngine;
using YuanHaiLu.Core;
using System;
using YuanHaiLu.Character;
using UnityEngine.SceneManagement;

namespace YuanHaiLu.GameSystem
{
    /// <summary>
    /// 存档管理器 — 保存/加载游戏进度
    /// 单例，挂载到 GameManager 子物体或独立物体上
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        private const string SAVE_KEY = "YuanHaiLu_SaveSlot_";
        private const string AUTO_SAVE_KEY = "YuanHaiLu_AutoSave";
        private const int BASE_STATS_SAVE_VERSION = 2;
        private const int CURRENT_SAVE_VERSION = 3;

        private SaveData _pendingLoadData;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (Application.isPlaying && transform.parent == null)
                DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoadedForLoad;
            if (Instance == this)
                Instance = null;
        }

        // === 存档数据结构 ===
        [System.Serializable]
        public class SaveData
        {
            public int saveVersion;

            // 玩家
            public string playerName;
            public int level;
            public int exp;
            public int currentHp;
            public int maxHp;
            public int currentMp;
            public int maxMp;
            public int attack;
            public int defense;
            public int agility;

            // v2 起保存基础值，避免装备加成被重复计入基础属性。
            public int baseAttack;
            public int baseDefense;
            public int baseAgility;
            public int baseMaxHp;
            public int baseMaxMp;

            // 位置
            public float positionX;
            public float positionY;
            public string sceneName;

            // 剧情
            public int chapterIndex;
            public string[] completedQuests;
            public string[] activeQuests;
            public string[] learnedSkills;

            // 背包/装备/金钱（嵌套可序列化结构）
            public InventoryManager.InventorySaveData inventory;
            // 武学（已学/已装备）
            public MartialArtsSystem.MartialArtsSaveData martialArts;
            // v3 起保存活跃任务、目标进度和已完成任务。
            public QuestManager.QuestSaveData quests;

            // 世界状态
            public string[] defeatedEnemies;
            public string[] collectedItems;
            public string[] unlockedAreas;

            // 时间戳
            public string saveTime;
        }

        /// <summary>
        /// 保存游戏
        /// </summary>
        public void SaveGame(int slot = 0)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogError("[SaveManager] 找不到玩家对象！");
                return;
            }

            var stats = player.GetComponent<CharacterStats>();
            if (stats == null || GameManager.Instance == null)
            {
                Debug.LogError("[SaveManager] 玩家属性或 GameManager 缺失，无法存档！");
                return;
            }

            var saveData = new SaveData
            {
                saveVersion = CURRENT_SAVE_VERSION,

                // 基本信息
                playerName = GameManager.Instance.playerName,
                level = stats.level,
                exp = stats.exp,
                currentHp = stats.currentHp,
                maxHp = stats.maxHp,
                currentMp = stats.currentMp,
                maxMp = stats.maxMp,
                attack = stats.attack,
                defense = stats.defense,
                agility = stats.agility,
                baseAttack = stats.BaseAttack,
                baseDefense = stats.BaseDefense,
                baseAgility = stats.BaseAgility,
                baseMaxHp = stats.BaseMaxHp,
                baseMaxMp = stats.BaseMaxMp,

                // 位置
                positionX = player.transform.position.x,
                positionY = player.transform.position.y,
                sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,

                // 剧情
                chapterIndex = GameManager.Instance.chapterIndex,

                // 时间
                saveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            // 背包/武学/已完成任务（单例可能在场景中，用 null 安全访问）
            saveData.inventory = InventoryManager.Instance?.GetSaveData();
            var martial = player.GetComponent<MartialArtsSystem>();
            if (martial != null)
            {
                saveData.martialArts = martial.GetSaveData();
            }
            saveData.completedQuests = QuestManager.Instance?.GetCompletedQuests();
            saveData.quests = QuestManager.Instance?.GetSaveData();

            string json = JsonUtility.ToJson(saveData, true);
            string key = slot == -1 ? AUTO_SAVE_KEY : SAVE_KEY + slot;
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();

            Debug.Log($"[SaveManager] 存档成功！槽位: {(slot == -1 ? "自动存档" : slot.ToString())} | " +
                      $"位置: {saveData.sceneName} ({saveData.positionX:F1}, {saveData.positionY:F1})");
        }

        /// <summary>
        /// 加载游戏
        /// </summary>
        public void LoadGame(int slot = 0)
        {
            string key = slot == -1 ? AUTO_SAVE_KEY : SAVE_KEY + slot;
            string json = PlayerPrefs.GetString(key, "");

            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning($"[SaveManager] 槽位 {slot} 无存档");
                return;
            }

            SaveData saveData;
            try
            {
                saveData = JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SaveManager] 存档 JSON 损坏: {exception.Message}");
                return;
            }

            if (saveData == null || string.IsNullOrEmpty(saveData.sceneName))
            {
                Debug.LogError("[SaveManager] 存档缺少场景名，无法读档！");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(saveData.sceneName))
            {
                Debug.LogError($"[SaveManager] 存档场景不在 Build Settings 中: {saveData.sceneName}");
                return;
            }

            if (GameManager.Instance == null)
            {
                Debug.LogError("[SaveManager] GameManager 缺失，无法读档！");
                return;
            }

            SceneManager.sceneLoaded -= OnSceneLoadedForLoad;
            _pendingLoadData = saveData;
            GameManager.Instance.BeginSceneEntry(GameManager.SceneEntryMode.LoadGame);
            SceneManager.sceneLoaded += OnSceneLoadedForLoad;

            try
            {
                SceneManager.LoadScene(saveData.sceneName, LoadSceneMode.Single);
            }
            catch (Exception exception)
            {
                SceneManager.sceneLoaded -= OnSceneLoadedForLoad;
                _pendingLoadData = null;
                GameManager.Instance.CompleteSceneEntry();
                Debug.LogError($"[SaveManager] 加载场景失败: {exception.Message}");
                return;
            }

            Debug.Log($"[SaveManager] 读档成功！存档时间: {saveData.saveTime}");
        }

        private void OnSceneLoadedForLoad(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnSceneLoadedForLoad;
            SaveData saveData = _pendingLoadData;
            _pendingLoadData = null;

            if (saveData != null)
                ApplySaveDataToLoadedScene(saveData);
        }

        internal void ApplySaveDataToLoadedScene(SaveData saveData)
        {
            if (saveData == null) return;

            var gameManager = GameManager.Instance;
            var player = GameObject.FindGameObjectWithTag("Player");
            CharacterStats stats = null;

            if (player == null)
            {
                Debug.LogError("[SaveManager] 场景加载后找不到玩家，无法恢复玩家数据！");
            }
            else
            {
                player.transform.position = new Vector2(saveData.positionX, saveData.positionY);

                stats = player.GetComponent<CharacterStats>();
                if (stats == null)
                {
                    Debug.LogError("[SaveManager] 玩家缺少 CharacterStats，无法恢复属性！");
                }
                else
                {
                    ResolveBaseStats(saveData,
                        out int baseAttack,
                        out int baseDefense,
                        out int baseAgility,
                        out int baseMaxHp,
                        out int baseMaxMp);

                    stats.characterName = saveData.playerName;
                    stats.level = Mathf.Max(1, saveData.level);
                    stats.exp = Mathf.Max(0, saveData.exp);
                    stats.SetBaseFromLoad(
                        baseAttack, baseDefense, baseAgility,
                        baseMaxHp, baseMaxMp,
                        saveData.currentHp, saveData.currentMp);
                }

                if (saveData.inventory != null)
                {
                    if (InventoryManager.Instance != null)
                        InventoryManager.Instance.LoadSaveData(saveData.inventory);
                    else
                        Debug.LogError("[SaveManager] InventoryManager 缺失，无法恢复背包！");
                }

                // SetBaseFromLoad 必须先于装备重算；装备恢复完成后再按最终上限
                // 写回当前资源，避免装备增加上限时把合法 HP/MP 提前裁剪掉。
                stats?.SetCurrentResourcesFromLoad(saveData.currentHp, saveData.currentMp);

                if (saveData.martialArts != null)
                {
                    var martial = player.GetComponent<MartialArtsSystem>();
                    if (martial != null)
                        martial.LoadSaveData(saveData.martialArts, MartialSkillDatabase.AllSkills);
                    else
                        Debug.LogError("[SaveManager] 玩家缺少 MartialArtsSystem，无法恢复武学！");
                }
            }

            if (QuestManager.Instance != null)
            {
                if (saveData.saveVersion >= CURRENT_SAVE_VERSION && saveData.quests != null)
                    QuestManager.Instance.LoadSaveData(saveData.quests);
                else
                    QuestManager.Instance.LoadCompletedQuests(saveData.completedQuests);
            }
            else if (saveData.quests != null || saveData.completedQuests != null)
            {
                Debug.LogError("[SaveManager] QuestManager 缺失，无法恢复任务！");
            }

            if (gameManager != null)
            {
                gameManager.chapterIndex = Mathf.Max(1, saveData.chapterIndex);
                if (!string.IsNullOrEmpty(saveData.playerName))
                    gameManager.playerName = saveData.playerName;
                gameManager.SetState(GameManager.GameState.Exploration);
                gameManager.CompleteSceneEntry();
            }
        }

        private static void ResolveBaseStats(
            SaveData saveData,
            out int baseAttack,
            out int baseDefense,
            out int baseAgility,
            out int baseMaxHp,
            out int baseMaxMp)
        {
            if (saveData.saveVersion >= BASE_STATS_SAVE_VERSION)
            {
                baseAttack = saveData.baseAttack;
                baseDefense = saveData.baseDefense;
                baseAgility = saveData.baseAgility;
                baseMaxHp = saveData.baseMaxHp;
                baseMaxMp = saveData.baseMaxMp;
                return;
            }

            baseAttack = saveData.attack;
            baseDefense = saveData.defense;
            baseAgility = saveData.agility;
            baseMaxHp = saveData.maxHp;
            baseMaxMp = saveData.maxMp;

            // 兼容本修复合入前产生的临时存档：旧字段保存的是含装备总值。
            var inventory = InventoryManager.Instance;
            if (saveData.inventory == null || inventory == null) return;

            foreach (string itemId in new[]
                     {
                         saveData.inventory.equippedWeapon,
                         saveData.inventory.equippedArmor,
                         saveData.inventory.equippedAccessory
                     })
            {
                if (string.IsNullOrEmpty(itemId)) continue;
                ItemData item = inventory.GetItemData(itemId);
                if (item == null) continue;

                baseAttack -= item.bonusAttack;
                baseDefense -= item.bonusDefense;
                baseAgility -= item.bonusAgility;
                baseMaxHp -= item.bonusMaxHp;
                baseMaxMp -= item.bonusMaxMp;
            }

            baseMaxHp = Mathf.Max(1, baseMaxHp);
            baseMaxMp = Mathf.Max(0, baseMaxMp);
        }

        /// <summary>
        /// 检查存档是否存在
        /// </summary>
        public bool HasSave(int slot = 0)
        {
            string key = slot == -1 ? AUTO_SAVE_KEY : SAVE_KEY + slot;
            return PlayerPrefs.HasKey(key);
        }

        /// <summary>
        /// 删除存档
        /// </summary>
        public void DeleteSave(int slot = 0)
        {
            string key = slot == -1 ? AUTO_SAVE_KEY : SAVE_KEY + slot;
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }
}
