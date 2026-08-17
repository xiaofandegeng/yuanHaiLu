using UnityEngine;
using YuanHaiLu.Character;

namespace YuanHaiLu.Core
{
    /// <summary>
    /// 单例游戏管理器 — 负责游戏状态、场景切换、全局事件
    /// 挂载到 GameManager 空物体上
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public enum GameState
        {
            Boot,
            MainMenu,
            Exploration,    // 自由探索
            Dialogue,       // 对话中
            Combat,         // 战斗中
            Menu,           // 菜单打开
            Cutscene,       // 过场动画
            Paused
        }

        public enum SceneEntryMode
        {
            NewGame,
            LoadGame,
            SceneTransition,
            Active
        }

        [Header("当前状态")]
        public GameState currentState = GameState.Boot;

        [Header("游戏数据")]
        public string playerName = "凌霜";
        public int chapterIndex = 1;
        [SerializeField] private string playerArtId = PlayerAppearance.DefaultArtId;
        [SerializeField] private string weaponStyleId = WeaponStyle.DefaultStyleId;

        public PlayerAppearance PlayerAppearance =>
            YuanHaiLu.Core.PlayerAppearance.ParseOrDefault(playerArtId);
        public string PlayerArtId => PlayerAppearance.ArtId;
        public WeaponStyle WeaponStyle => Core.WeaponStyle.ParseOrDefault(weaponStyleId);
        public string WeaponStyleId => WeaponStyle.StyleId;

        public SceneEntryMode CurrentSceneEntryMode { get; private set; } = SceneEntryMode.NewGame;
        public bool ShouldInitializeNewGame => CurrentSceneEntryMode == SceneEntryMode.NewGame;

        // === 事件 ===
        public static event System.Action<GameState, GameState> OnStateChanged;
        public static event System.Action<string> OnWeaponStyleChanged;

        private void Awake()
        {
            // 单例模式
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            playerArtId = PlayerAppearance.ArtId;
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
                Application.targetFrameRate = 60;
                QualitySettings.vSyncCount = 0;
            }
        }

        private void Start()
        {
            SetState(GameState.MainMenu);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// 切换游戏状态
        /// </summary>
        public void SetState(GameState newState)
        {
            if (currentState == newState) return;

            GameState oldState = currentState;
            currentState = newState;

            Debug.Log($"[GameManager] 状态切换: {oldState} → {newState}");
            OnStateChanged?.Invoke(oldState, newState);

            HandleStateChange(oldState, newState);
        }

        private void HandleStateChange(GameState old, GameState now)
        {
            switch (now)
            {
                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;
                case GameState.Cutscene:
                    // 锁定玩家输入
                    break;
                default:
                    Time.timeScale = 1f;
                    break;
            }
        }

        public bool CanPlayerMove()
        {
            return currentState == GameState.Exploration;
        }

        public bool CanPlayerAct()
        {
            return currentState == GameState.Exploration || currentState == GameState.Combat;
        }

        public void BeginSceneEntry(SceneEntryMode mode)
        {
            CurrentSceneEntryMode = mode;
        }

        public void CompleteSceneEntry()
        {
            CurrentSceneEntryMode = SceneEntryMode.Active;
        }

        public void SetPlayerAppearance(string artId)
        {
            if (!YuanHaiLu.Core.PlayerAppearance.TryParse(artId, out var appearance))
                throw new System.ArgumentException(
                    $"Unknown formal player appearance '{artId}'.",
                    nameof(artId));
            playerArtId = appearance.ArtId;
        }

        /// <summary>
        /// 设置武器流派（docs/15）。非法 ID 直接抛错；
        /// 存档迁移侧请先用 WeaponStyle.ParseOrDefault 归一化。
        /// </summary>
        public void SetWeaponStyle(string styleId)
        {
            if (!Core.WeaponStyle.TryParse(styleId, out var style))
                throw new System.ArgumentException(
                    $"Unknown weapon style '{styleId}'.",
                    nameof(styleId));
            weaponStyleId = style.StyleId;
            OnWeaponStyleChanged?.Invoke(weaponStyleId);
        }

        // === 场景过渡携带（docs/15 MVP：烟柳镇 ↔ 客栈往返）===
        // HP/MP/基础属性/武学挂在场景本地玩家上，切换场景时由 AreaTrigger
        // 在离开前捕获、新场景落地后回放，避免除读档外的进度丢失。
        public sealed class TransitionCarry
        {
            public string playerName;
            public int level;
            public int exp;
            public int currentHp;
            public int currentMp;
            public int baseAttack;
            public int baseDefense;
            public int baseAgility;
            public int baseMaxHp;
            public int baseMaxMp;
            public MartialArtsSystem.MartialArtsSaveData martialArts;
        }

        private TransitionCarry _pendingTransitionCarry;

        public void BeginTransitionCarry(GameObject player)
        {
            _pendingTransitionCarry = null;
            if (player == null) return;

            var stats = player.GetComponent<CharacterStats>();
            if (stats == null) return;

            _pendingTransitionCarry = new TransitionCarry
            {
                playerName = stats.characterName,
                level = stats.level,
                exp = stats.exp,
                currentHp = stats.currentHp,
                currentMp = stats.currentMp,
                baseAttack = stats.BaseAttack,
                baseDefense = stats.BaseDefense,
                baseAgility = stats.BaseAgility,
                baseMaxHp = stats.BaseMaxHp,
                baseMaxMp = stats.BaseMaxMp,
                martialArts = player.GetComponent<MartialArtsSystem>()?.GetSaveData()
            };
        }

        public bool ApplyTransitionCarry(GameObject player)
        {
            var carry = _pendingTransitionCarry;
            _pendingTransitionCarry = null;
            if (carry == null || player == null) return false;

            var stats = player.GetComponent<CharacterStats>();
            if (stats == null) return false;

            stats.characterName = carry.playerName;
            stats.level = Mathf.Max(1, carry.level);
            stats.exp = Mathf.Max(0, carry.exp);
            stats.SetBaseFromLoad(
                carry.baseAttack, carry.baseDefense, carry.baseAgility,
                carry.baseMaxHp, carry.baseMaxMp,
                carry.currentHp, carry.currentMp);

            if (carry.martialArts != null)
                player.GetComponent<MartialArtsSystem>()?.LoadSaveData(
                    carry.martialArts,
                    GameSystem.MartialSkillDatabase.AllSkills);
            return true;
        }

        // === 快捷方法 ===
        public void Pause() => SetState(GameState.Paused);
        public void Resume() => SetState(GameState.Exploration);
        public void EnterDialogue() => SetState(GameState.Dialogue);
        public void ExitDialogue() => SetState(GameState.Exploration);
        public void EnterCombat() => SetState(GameState.Combat);
        public void ExitCombat() => SetState(GameState.Exploration);
    }
}
