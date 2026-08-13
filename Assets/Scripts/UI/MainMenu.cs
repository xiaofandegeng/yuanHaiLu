using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YuanHaiLu.Core;
using YuanHaiLu.GameSystem;
using YuanHaiLu.Art;
using System;
using System.Linq;

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

        [Header("角色选择")]
        [SerializeField] private Image appearancePreview;
        [SerializeField] private Text appearanceLabel;
        [SerializeField] private GameObject appearancePanel;
        [SerializeField] private GameObject mainButtonContainer;

        [Header("场景名")]
        [SerializeField] private string firstSceneName = "Demo_YanLiuTown";

        public PlayerAppearance SelectedAppearance { get; private set; } = PlayerAppearance.Default;

        private void Start()
        {
            GameManager gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<GameManager>(FindObjectsInactive.Include);
                if (gameManager == null)
                    gameManager = new GameObject("[GameManager]").AddComponent<GameManager>();
            }

            GlobalSystemsBootstrapper.EnsureRequiredSystems(gameManager);
            gameManager.SetState(GameManager.GameState.MainMenu);
            SelectedAppearance = gameManager.PlayerAppearance;
            ResolveAppearanceUI();
            RefreshAppearanceUI();
            SetAppearanceSelectionVisible(false);
            BindMenuButtons();
        }

        private void Update()
        {
            if (appearancePanel != null && appearancePanel.activeSelf &&
                Input.GetButtonDown("Cancel"))
            {
                CancelAppearanceSelection();
            }
        }

        /// <summary>
        /// 开始新游戏
        /// </summary>
        public void OnNewGame()
        {
            var gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                Debug.LogError("[MainMenu] GameManager 缺失，无法选择新游戏角色！");
                return;
            }
            SelectedAppearance = gameManager.PlayerAppearance;
            ResolveAppearanceUI();
            RefreshAppearanceUI();
            SetAppearanceSelectionVisible(true);
        }

        public void ConfirmNewGame()
        {
            Debug.Log("[MainMenu] 确认角色并开始新游戏");

            var gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                Debug.LogError("[MainMenu] GameManager 缺失，无法开始新游戏！");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(firstSceneName))
            {
                Debug.LogError($"[MainMenu] 场景不在 Build Settings 中: {firstSceneName}");
                return;
            }

            GlobalSystemsBootstrapper.EnsureRequiredSystems(gameManager);
            gameManager.SetPlayerAppearance(SelectedAppearance.ArtId);
            InventoryManager.Instance?.ResetForNewGame();
            QuestManager.Instance?.ResetForNewGame();
            gameManager.playerName = "凌霜";
            gameManager.chapterIndex = 1;
            gameManager.BeginSceneEntry(GameManager.SceneEntryMode.NewGame);
            gameManager.SetState(GameManager.GameState.Exploration);

            SceneManager.LoadScene(firstSceneName);
        }

        public void CancelAppearanceSelection()
        {
            if (GameManager.Instance != null)
                SelectedAppearance = GameManager.Instance.PlayerAppearance;
            ResolveAppearanceUI();
            RefreshAppearanceUI();
            SetAppearanceSelectionVisible(false);
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
                        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
                            EventSystem.current.SetSelectedGameObject(button.gameObject);
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
                    case "Btn_确认角色":
                        Bind(button, ConfirmNewGame);
                        break;
                    case "Btn_取消角色":
                        Bind(button, CancelAppearanceSelection);
                        break;
                    default:
                        const string prefix = "Btn_角色_";
                        if (button.gameObject.name.StartsWith(prefix, StringComparison.Ordinal))
                        {
                            string artId = button.gameObject.name.Substring(prefix.Length);
                            BindAppearance(button, artId);
                        }
                        break;
                }
            }
        }

        public void SelectAppearance(string artId)
        {
            if (!PlayerAppearance.TryParse(artId, out var appearance))
                throw new ArgumentException($"Unknown formal player appearance '{artId}'.", nameof(artId));
            SelectedAppearance = appearance;
            ResolveAppearanceUI();
            RefreshAppearanceUI();
        }

        private void ResolveAppearanceUI()
        {
            if (appearancePreview == null)
                appearancePreview = GetComponentsInChildren<Image>(true)
                    .FirstOrDefault(value => value.name == "CharacterPreview");
            if (appearanceLabel == null)
                appearanceLabel = GetComponentsInChildren<Text>(true)
                    .FirstOrDefault(value => value.name == "CharacterSelectionLabel");
            if (appearancePanel == null)
                appearancePanel = transform.Find("CharacterSelector")?.gameObject ??
                    GetComponentsInChildren<Transform>(true)
                        .FirstOrDefault(value => value.name == "CharacterSelector")?.gameObject;
            if (mainButtonContainer == null)
                mainButtonContainer = GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(value => value.name == "ButtonContainer")?.gameObject;
        }

        private void SetAppearanceSelectionVisible(bool visible)
        {
            ResolveAppearanceUI();
            if (appearancePanel != null)
                appearancePanel.SetActive(visible);
            if (mainButtonContainer != null)
                mainButtonContainer.SetActive(!visible);

            var eventSystem = EventSystem.current;
            if (eventSystem == null) return;
            string targetName = visible
                ? "Btn_角色_" + SelectedAppearance.ArtId
                : "Btn_新游戏";
            var target = GetComponentsInChildren<Button>(true)
                .FirstOrDefault(button => button.name == targetName &&
                                          button.gameObject.activeInHierarchy);
            if (target == null && visible)
                target = GetComponentsInChildren<Button>(true)
                    .FirstOrDefault(button => button.name == "Btn_确认角色" &&
                                              button.gameObject.activeInHierarchy);
            eventSystem.SetSelectedGameObject(target != null ? target.gameObject : null);
        }

        private void RefreshAppearanceUI()
        {
            if (appearanceLabel != null)
                appearanceLabel.text = $"主角：{SelectedAppearance.DisplayName}";
            if (appearancePreview != null)
            {
                var catalog = CharacterArtCatalog.LoadDefault();
                if (!catalog.TryGet(SelectedAppearance.ArtId, out var entry) || entry.Prefab == null)
                    throw new InvalidOperationException(
                        $"Formal player preview is missing for '{SelectedAppearance.ArtId}'.");
                var prefabRenderer = entry.Prefab.GetComponent<SpriteRenderer>();
                if (prefabRenderer == null || prefabRenderer.sprite == null)
                    throw new InvalidOperationException(
                        $"Formal player prefab has no idle sprite for '{SelectedAppearance.ArtId}'.");
                appearancePreview.sprite = prefabRenderer.sprite;
                appearancePreview.preserveAspect = true;
            }

            const string prefix = "Btn_角色_";
            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                if (!button.name.StartsWith(prefix, StringComparison.Ordinal))
                    continue;
                bool selected = string.Equals(
                    button.name.Substring(prefix.Length),
                    SelectedAppearance.ArtId,
                    StringComparison.Ordinal);
                var colors = button.colors;
                colors.normalColor = selected
                    ? new Color(0.72f, 0.48f, 0.16f)
                    : new Color(0.15f, 0.12f, 0.2f);
                button.colors = colors;
            }
        }

        private void BindAppearance(Button button, string artId)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectAppearance(artId));
        }

        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button.onClick.GetPersistentEventCount() > 0)
                return;

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
