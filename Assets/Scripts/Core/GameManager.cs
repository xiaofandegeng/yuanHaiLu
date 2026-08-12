using UnityEngine;

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

        public SceneEntryMode CurrentSceneEntryMode { get; private set; } = SceneEntryMode.NewGame;
        public bool ShouldInitializeNewGame => CurrentSceneEntryMode == SceneEntryMode.NewGame;

        // === 事件 ===
        public static event System.Action<GameState, GameState> OnStateChanged;

        private void Awake()
        {
            // 单例模式
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (Application.isPlaying)
                DontDestroyOnLoad(gameObject);

            // 初始化子系统
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
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

        // === 快捷方法 ===
        public void Pause() => SetState(GameState.Paused);
        public void Resume() => SetState(GameState.Exploration);
        public void EnterDialogue() => SetState(GameState.Dialogue);
        public void ExitDialogue() => SetState(GameState.Exploration);
        public void EnterCombat() => SetState(GameState.Combat);
        public void ExitCombat() => SetState(GameState.Exploration);
    }
}
