using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YuanHaiLu.Core;
using YuanHaiLu.GameSystem;

namespace YuanHaiLu.UI
{
    /// <summary>
    /// 主菜单控制器
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [Header("菜单面板")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject loadPanel;

        [Header("场景名")]
        [SerializeField] private string firstSceneName = "Demo_YanLiuTown";

        private void Start()
        {
            GameManager gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                gameManager = FindFirstObjectByType<GameManager>();
                if (gameManager == null)
                    gameManager = new GameObject("[GameManager]").AddComponent<GameManager>();
            }

            GlobalSystemsBootstrapper.EnsureRequiredSystems(gameManager);
            BindMenuButtons();
        }

        /// <summary>
        /// 开始新游戏
        /// </summary>
        public void OnNewGame()
        {
            Debug.Log("[MainMenu] 开始新游戏");

            var gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                Debug.LogError("[MainMenu] GameManager 缺失，无法开始新游戏！");
                return;
            }

            GlobalSystemsBootstrapper.EnsureRequiredSystems(gameManager);
            InventoryManager.Instance?.ResetForNewGame();
            QuestManager.Instance?.ResetForNewGame();
            gameManager.playerName = "凌霜";
            gameManager.chapterIndex = 1;
            gameManager.BeginSceneEntry(GameManager.SceneEntryMode.NewGame);
            gameManager.SetState(GameManager.GameState.Exploration);

            if (!Application.CanStreamedLevelBeLoaded(firstSceneName))
            {
                Debug.LogError($"[MainMenu] 场景不在 Build Settings 中: {firstSceneName}");
                return;
            }

            SceneManager.LoadScene(firstSceneName);
        }

        /// <summary>
        /// 继续游戏（存档加载）
        /// </summary>
        public void OnContinue()
        {
            Debug.Log("[MainMenu] 继续游戏");
            if (SaveManager.Instance == null)
            {
                Debug.LogError("[MainMenu] SaveManager 缺失，无法继续游戏！");
                return;
            }

            SaveManager.Instance.LoadGame();
        }

        /// <summary>
        /// 打开设置
        /// </summary>
        public void OnSettings()
        {
            if (mainPanel == null || settingsPanel == null)
            {
                Debug.LogWarning("[MainMenu] 设置面板引用未配置。");
                return;
            }

            mainPanel.SetActive(false);
            settingsPanel.SetActive(true);
        }

        /// <summary>
        /// 关闭设置
        /// </summary>
        public void OnSettingsBack()
        {
            if (mainPanel == null || settingsPanel == null)
            {
                Debug.LogWarning("[MainMenu] 设置面板引用未配置。");
                return;
            }

            settingsPanel.SetActive(false);
            mainPanel.SetActive(true);
        }

        private void BindMenuButtons()
        {
            foreach (Button button in GetComponentsInChildren<Button>(true))
            {
                switch (button.gameObject.name)
                {
                    case "Btn_新游戏":
                        Bind(button, OnNewGame);
                        break;
                    case "Btn_继续游戏":
                        Bind(button, OnContinue);
                        break;
                    case "Btn_设置":
                        Bind(button, OnSettings);
                        break;
                    case "Btn_退出":
                        Bind(button, OnQuit);
                        break;
                }
            }
        }

        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        /// <summary>
        /// 退出游戏
        /// </summary>
        public void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
