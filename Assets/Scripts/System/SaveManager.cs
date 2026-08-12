using UnityEngine;
using YuanHaiLu.Core;
using System;
using YuanHaiLu.Character;

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

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // === 存档数据结构 ===
        [System.Serializable]
        public class SaveData
        {
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

            // 位置
            public float positionX;
            public float positionY;
            public string sceneName;

            // 剧情
            public int chapterIndex;
            public string[] completedQuests;
            public string[] activeQuests;
            public string[] learnedSkills;

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
            var saveData = new SaveData
            {
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

                // 位置
                positionX = player.transform.position.x,
                positionY = player.transform.position.y,
                sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,

                // 剧情
                chapterIndex = GameManager.Instance.chapterIndex,

                // 时间
                saveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

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

            var saveData = JsonUtility.FromJson<SaveData>(json);

            // 加载场景
            UnityEngine.SceneManagement.SceneManager.LoadScene(saveData.sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);

            // 场景加载完成后恢复状态
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) =>
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    player.transform.position = new Vector2(saveData.positionX, saveData.positionY);

                    var stats = player.GetComponent<CharacterStats>();
                    if (stats != null)
                    {
                        stats.level = saveData.level;
                        stats.exp = saveData.exp;
                        // 用基础值方式恢复（读档视为无装备；
                        // 与"背包未接入存档"的现状一致）
                        stats.SetBaseFromLoad(
                            saveData.attack, saveData.defense, saveData.agility,
                            saveData.maxHp, saveData.maxMp,
                            saveData.currentHp, saveData.currentMp);
                    }
                }

                GameManager.Instance.chapterIndex = saveData.chapterIndex;
                GameManager.Instance.playerName = saveData.playerName;
                GameManager.Instance.SetState(GameManager.GameState.Exploration);
            };

            Debug.Log($"[SaveManager] 读档成功！存档时间: {saveData.saveTime}");
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
