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
            EnsureDefaultPausePanel();

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

        /// <summary>
        /// Demo 场景生成器只挂载 PauseMenu 组件；引用为空时在运行时补出可用的默认界面。
        /// </summary>
        private void EnsureDefaultPausePanel()
        {
            if (pausePanel != null) return;

            var canvas = GetComponent<Canvas>();
            if (canvas != null && GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            pausePanel = new GameObject(
                "PausePanel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            pausePanel.transform.SetParent(transform, false);

            var panelRect = pausePanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            pausePanel.GetComponent<Image>().color = new Color(0.03f, 0.03f, 0.08f, 0.92f);

            CreateLabel(pausePanel.transform, "Title", "游戏已暂停", new Vector2(0f, 80f), 32);
            CreateButton(pausePanel.transform, "Btn_继续游戏", "继续游戏", new Vector2(0f, 20f), Resume);
            CreateButton(pausePanel.transform, "Btn_保存游戏", "保存游戏", new Vector2(0f, -35f), OnSaveButton);
            CreateButton(pausePanel.transform, "Btn_返回主菜单", "返回主菜单", new Vector2(0f, -90f), OnQuitToMenuButton);
        }

        private static void CreateLabel(
            Transform parent,
            string objectName,
            string content,
            Vector2 anchoredPosition,
            int fontSize)
        {
            var labelObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            labelObject.transform.SetParent(parent, false);

            var rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(320f, 48f);

            var text = labelObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.95f, 0.85f, 0.55f);
        }

        private static void CreateButton(
            Transform parent,
            string objectName,
            string label,
            Vector2 anchoredPosition,
            UnityEngine.Events.UnityAction onClick)
        {
            var buttonObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(240f, 42f);

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.18f, 0.14f, 0.24f, 0.98f);

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            CreateLabel(buttonObject.transform, "Text", label, Vector2.zero, 20);
            var labelRect = buttonObject.transform.Find("Text").GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            labelRect.sizeDelta = Vector2.zero;
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
