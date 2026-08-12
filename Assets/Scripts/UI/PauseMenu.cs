using YuanHaiLu.GameSystem;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using YuanHaiLu.Core;

namespace YuanHaiLu.UI
{
    /// <summary>
    /// 暂停菜单 — ESC 暂停，包含继续/设置/回到主菜单
    /// 挂载到 PauseCanvas 下
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [Header("面板")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject confirmQuitPanel;

        [Header("设置项")]
        [SerializeField] private UnityEngine.UI.Slider bgmSlider;
        [SerializeField] private UnityEngine.UI.Slider sfxSlider;

        private bool _isPaused = false;

        public bool IsPaused => _isPaused;

        private void Start()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (confirmQuitPanel != null) confirmQuitPanel.SetActive(false);

            // 初始化音量滑块
            if (bgmSlider != null)
            {
                bgmSlider.value = GameSystem.AudioManager.Instance?.GetBGMVolume() ?? 0.6f;
                bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
            }
            if (sfxSlider != null)
            {
                sfxSlider.value = GameSystem.AudioManager.Instance?.GetSFXVolume() ?? 0.8f;
                sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (confirmQuitPanel != null && confirmQuitPanel.activeSelf)
                {
                    // 关闭确认对话框
                    confirmQuitPanel.SetActive(false);
                    return;
                }

                if (settingsPanel != null && settingsPanel.activeSelf)
                {
                    // 关闭设置
                    OnSettingsBack();
                    return;
                }

                if (_isPaused) Resume();
                else Pause();
            }
        }

        public void Pause()
        {
            _isPaused = true;
            if (pausePanel != null) pausePanel.SetActive(true);

            if (GameManager.Instance != null)
                GameManager.Instance.Pause();

            GameSystem.AudioManager.Instance?.PlaySFX(GameSystem.AudioManager.SFX.UI_OPEN);
        }

        public void Resume()
        {
            _isPaused = false;
            if (pausePanel != null) pausePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (confirmQuitPanel != null) confirmQuitPanel.SetActive(false);

            if (GameManager.Instance != null)
                GameManager.Instance.Resume();

            GameSystem.AudioManager.Instance?.PlaySFX(GameSystem.AudioManager.SFX.UI_CLOSE);
        }

        // === 按钮回调 ===

        public void OnResumeButton()
        {
            Resume();
        }

        public void OnSaveButton()
        {
            GameSystem.SaveManager.Instance?.SaveGame(0);
        }

        public void OnSettingsButton()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        public void OnSettingsBack()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(true);
        }

        public void OnQuitToMenuButton()
        {
            if (confirmQuitPanel != null)
            {
                confirmQuitPanel.SetActive(true);
            }
            else
            {
                QuitToMenu();
            }
        }

        public void OnConfirmQuit()
        {
            QuitToMenu();
        }

        public void OnCancelQuit()
        {
            if (confirmQuitPanel != null)
                confirmQuitPanel.SetActive(false);
        }

        private void QuitToMenu()
        {
            // 自动存档
            GameSystem.SaveManager.Instance?.SaveGame(-1);

            Resume();

            // 加载主菜单场景
            SceneManager.LoadScene("MainMenu");

            Debug.Log("[PauseMenu] 返回主菜单");
        }

        // === 音量控制 ===

        private void OnBGMVolumeChanged(float value)
        {
            GameSystem.AudioManager.Instance?.SetBGMVolume(value);
        }

        private void OnSFXVolumeChanged(float value)
        {
            GameSystem.AudioManager.Instance?.SetSFXVolume(value);
        }
    }
}
