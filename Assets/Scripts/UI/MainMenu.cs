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
    /// 主菜单控制器（单主角 MVP，docs/15）：
    /// 只保留 新游戏/继续游戏 与三个武器流派按钮；
    /// 预览永远是同一位男性主角，不再提供外观选择。
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [Header("武器流派选择")]
        [SerializeField] private Image weaponPreview;
        [SerializeField] private Image weaponIcon;
        [SerializeField] private Text weaponLabel;
        [SerializeField] private Text weaponHint;

        [Header("场景名")]
        [SerializeField] private string firstSceneName = "Demo_YanLiuTown";

        public WeaponStyle SelectedWeaponStyle { get; private set; } = WeaponStyle.Default;

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
            SelectedWeaponStyle = gameManager.WeaponStyle;
            ResolveWeaponStyleUI();
            RefreshWeaponStyleUI();
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

            if (!Application.CanStreamedLevelBeLoaded(firstSceneName))
            {
                Debug.LogError($"[MainMenu] 场景不在 Build Settings 中: {firstSceneName}");
                return;
            }

            GlobalSystemsBootstrapper.EnsureRequiredSystems(gameManager);
            var inventory = InventoryManager.Instance;
            if (inventory != null) inventory.ResetForNewGame();
            var quests = QuestManager.Instance;
            if (quests != null) quests.ResetForNewGame();
            gameManager.playerName = "凌霜";
            gameManager.chapterIndex = 1;
            // 单主角 MVP：固定男性主角身体；只应用玩家选择的武器流派。
            gameManager.SetPlayerAppearance(PlayerAppearance.Default.ArtId);
            gameManager.SetWeaponStyle(SelectedWeaponStyle.StyleId);
            gameManager.BeginSceneEntry(GameManager.SceneEntryMode.NewGame);
            gameManager.SetState(GameManager.GameState.Exploration);

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
                    default:
                        const string prefix = "Btn_流派_";
                        if (button.gameObject.name.StartsWith(prefix, StringComparison.Ordinal))
                        {
                            string styleId = button.gameObject.name.Substring(prefix.Length);
                            BindWeaponStyle(button, styleId);
                        }
                        break;
                }
            }
        }

        public void SelectWeaponStyle(string styleId)
        {
            if (!WeaponStyle.TryParse(styleId, out var style))
                throw new ArgumentException($"Unknown weapon style '{styleId}'.", nameof(styleId));
            var gameManager = GameManager.Instance;
            if (gameManager == null)
                throw new InvalidOperationException("GameManager is required to select a weapon style.");
            gameManager.SetWeaponStyle(style.StyleId);
            SelectedWeaponStyle = style;
            ResolveWeaponStyleUI();
            RefreshWeaponStyleUI();
        }

        private void ResolveWeaponStyleUI()
        {
            if (weaponPreview == null)
                weaponPreview = GetComponentsInChildren<Image>(true)
                    .FirstOrDefault(value => value.name == "CharacterPreview");
            if (weaponIcon == null)
                weaponIcon = GetComponentsInChildren<Image>(true)
                    .FirstOrDefault(value => value.name == "WeaponIcon");
            if (weaponLabel == null)
                weaponLabel = GetComponentsInChildren<Text>(true)
                    .FirstOrDefault(value => value.name == "StyleSelectionLabel");
            if (weaponHint == null)
                weaponHint = GetComponentsInChildren<Text>(true)
                    .FirstOrDefault(value => value.name == "StyleSelectionHint");
        }

        private void RefreshWeaponStyleUI()
        {
            if (weaponLabel != null)
                weaponLabel.text = $"武器：{SelectedWeaponStyle.DisplayName}";
            if (weaponHint != null)
                weaponHint.text = SelectedWeaponStyle.Description;
            if (weaponPreview != null)
            {
                var catalog = CharacterArtCatalog.LoadDefault();
                if (!catalog.TryGet(PlayerAppearance.Default.ArtId, out var entry) || entry.Prefab == null)
                    throw new InvalidOperationException(
                        $"Formal player preview is missing for '{PlayerAppearance.Default.ArtId}'.");
                var prefabRenderer = entry.Prefab.GetComponent<SpriteRenderer>();
                if (prefabRenderer == null || prefabRenderer.sprite == null)
                    throw new InvalidOperationException(
                        $"Formal player prefab has no idle sprite for '{PlayerAppearance.Default.ArtId}'.");
                weaponPreview.sprite = prefabRenderer.sprite;
                weaponPreview.preserveAspect = true;
            }

            // 武器小图（docs/15 复审）：大图标随所选流派切换持久精灵。
            if (weaponIcon != null)
            {
                var icon = MvpArtCatalog.Load(SelectedWeaponStyle.WeaponSpriteId);
                if (icon == null)
                    throw new InvalidOperationException(
                        $"Weapon icon sprite is missing for '{SelectedWeaponStyle.StyleId}'.");
                weaponIcon.sprite = icon;
                weaponIcon.preserveAspect = true;
            }

            const string prefix = "Btn_流派_";
            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                if (!button.name.StartsWith(prefix, StringComparison.Ordinal))
                    continue;
                string styleId = button.name.Substring(prefix.Length);
                bool selected = string.Equals(
                    styleId,
                    SelectedWeaponStyle.StyleId,
                    StringComparison.Ordinal);
                var colors = button.colors;
                colors.normalColor = selected
                    ? new Color(0.72f, 0.48f, 0.16f)
                    : new Color(0.15f, 0.12f, 0.2f);
                button.colors = colors;

                // 每个流派按钮的角标图标也换成对应武器小图。
                // 注意不能用 GetComponentInChildren：会命中按钮自身背景 Image。
                var buttonIcon = button.GetComponentsInChildren<Image>(true)
                    .FirstOrDefault(img => img.name == "Icon");
                if (buttonIcon != null)
                {
                    var sprite = MvpArtCatalog.Load("weapon_" + styleId);
                    if (sprite != null)
                        buttonIcon.sprite = sprite;
                }
            }
        }

        private void BindWeaponStyle(Button button, string styleId)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectWeaponStyle(styleId));
        }

        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button.onClick.GetPersistentEventCount() > 0)
                return;

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }
    }
}
