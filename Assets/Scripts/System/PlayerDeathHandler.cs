using UnityEngine;
using System;
using System.Collections;
using YuanHaiLu.Core;
using YuanHaiLu.Character;

namespace YuanHaiLu.GameSystem
{
    /// <summary>
    /// 玩家死亡/复活管理
    /// 监听玩家死亡事件，显示死亡界面，处理复活
    /// </summary>
    public class PlayerDeathHandler : MonoBehaviour
    {
        public static PlayerDeathHandler Instance { get; private set; }

        [Header("死亡设置")]
        [SerializeField] private float deathFadeDelay = 1f;
        [SerializeField] private int hpLostOnDeathPercent = 20; // 死亡损失气血上限百分比
        [SerializeField] private int goldLostPercent = 10;       // 死亡损失金币百分比

        [Header("复活点")]
        [SerializeField] private Vector2 defaultRespawnPos = new Vector2(0, 0);

        private CharacterStats _playerStats;
        private bool _isDead = false;

        // === 事件 ===
        public event System.Action OnPlayerDeath;
        public event System.Action OnPlayerRespawn;

        public bool IsDead => _isDead;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _playerStats = player.GetComponent<CharacterStats>();
                if (_playerStats != null)
                {
                    _playerStats.OnDeath += HandleDeath;
                }
            }
        }

        private void OnDestroy()
        {
            if (_playerStats != null)
            {
                _playerStats.OnDeath -= HandleDeath;
            }
        }

        private void HandleDeath()
        {
            if (_isDead) return;
            _isDead = true;

            OnPlayerDeath?.Invoke();

            // 延迟后执行复活流程
            StartCoroutine(DeathSequence());
        }

        private IEnumerator DeathSequence()
        {
            // 死亡画面停留
            yield return new WaitForSecondsRealtime(deathFadeDelay);

            // 淡出
            var transition = ScreenTransition.Instance;
            if (transition != null)
            {
                bool fadeDone = false;
                transition.FadeOut(() => fadeDone = true);
                yield return new WaitUntil(() => fadeDone);
            }

            // 死亡惩罚
            ApplyDeathPenalty();

            // 复活
            RespawnPlayer();

            // 淡入
            if (transition != null)
            {
                bool fadeDone = false;
                transition.FadeIn(() => fadeDone = true);
                yield return new WaitUntil(() => fadeDone);
            }

            // 显示提示
            if (transition != null)
                transition.ShowAreaName("烟柳客栈", "已在此处复活", 2f);

            _isDead = false;
        }

        private void ApplyDeathPenalty()
        {
            if (_playerStats == null) return;

            // 气血上限暂时降低
            int hpLost = Mathf.RoundToInt(_playerStats.maxHp * hpLostOnDeathPercent / 100f);
            _playerStats.maxHp = Mathf.Max(10, _playerStats.maxHp - hpLost);

            // 金币损失
            var inv = InventoryManager.Instance;
            if (inv != null)
            {
                int goldLost = Mathf.RoundToInt(inv.Gold * goldLostPercent / 100f);
                inv.SpendGold(goldLost);
                Debug.Log($"[死亡] 损失 {goldLost} 文钱");
            }

            Debug.Log($"[死亡] 气血上限 -{hpLost}（恢复后将复原）");
        }

        private void RespawnPlayer()
        {
            var player = _playerStats.gameObject;

            // 移动到复活点
            player.transform.position = defaultRespawnPos;

            // 恢复一半气血
            _playerStats.currentHp = Mathf.Max(1, _playerStats.maxHp / 2);
            _playerStats.currentMp = _playerStats.maxMp / 2;

            // 清除状态
            _playerStats.isPoisoned = false;
            _playerStats.isBleeding = false;
            _playerStats.isStunned = false;
            _playerStats.isInvincible = true; // 复活无敌

            // 重新启用组件
            var col = player.GetComponent<Collider2D>();
            if (col != null) col.enabled = true;

            var controller = player.GetComponent<PlayerController>();
            if (controller != null) controller.SetInputEnabled(true);

            // 复活无敌 3 秒
            StartCoroutine(RespawnInvincibility(3f));

            OnPlayerRespawn?.Invoke();

            Debug.Log("[复活] 在烟柳客栈复活，气血恢复50%，3秒无敌");
        }

        private IEnumerator RespawnInvincibility(float duration)
        {
            // 无敌闪烁效果
            var sr = _playerStats.GetComponent<SpriteRenderer>();
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                // 闪烁
                if (sr != null)
                {
                    sr.enabled = !sr.enabled;
                }

                yield return new WaitForSeconds(0.1f);
            }

            // 恢复正常
            if (sr != null) sr.enabled = true;
            _playerStats.isInvincible = false;
        }

        /// <summary>
        /// 设置复活点
        /// </summary>
        public void SetRespawnPoint(Vector2 pos)
        {
            defaultRespawnPos = pos;
            Debug.Log($"[复活] 复活点已更新: {pos}");
        }
    }
}
