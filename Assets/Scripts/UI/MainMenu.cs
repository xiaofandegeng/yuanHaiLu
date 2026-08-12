using YuanHaiLu.GameSystem;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
        [SerializeField] private string firstSceneName = "YanLiuTown";  // 烟柳镇

        /// <summary>
        /// 开始新游戏
        /// </summary>
        public void OnNewGame()
        {
            Debug.Log("[MainMenu] 开始新游戏");
            Core.GameManager.Instance.SetState(Core.GameManager.GameState.Exploration);
            SceneManager.LoadScene(firstSceneName);
        }

        /// <summary>
        /// 继续游戏（存档加载）
        /// </summary>
        public void OnContinue()
        {
            Debug.Log("[MainMenu] 继续游戏");
            // TODO: 从存档系统加载
            GameSystem.SaveManager.Instance?.LoadGame();
        }

        /// <summary>
        /// 打开设置
        /// </summary>
        public void OnSettings()
        {
            mainPanel.SetActive(false);
            settingsPanel.SetActive(true);
        }

        /// <summary>
        /// 关闭设置
        /// </summary>
        public void OnSettingsBack()
        {
            settingsPanel.SetActive(false);
            mainPanel.SetActive(true);
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
