using UnityEngine;
using YuanHaiLu.Core;
using YuanHaiLu.UI;

namespace YuanHaiLu.Character
{
    /// <summary>
    /// 玩家交互控制器 — 检测附近可交互对象，按 K 触发
    /// 挂载到玩家角色上（需 Collider2D）
    /// 统一驱动 NPCBase / TeleportPoint / Destructible / EventTrigger(requireInteract)
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PlayerInteraction : MonoBehaviour
    {
        public static PlayerInteraction EnsureOn(GameObject player)
        {
            if (player == null)
                throw new System.ArgumentNullException(nameof(player));

            var interaction = player.GetComponent<PlayerInteraction>();
            return interaction != null ? interaction : player.AddComponent<PlayerInteraction>();
        }

        [Header("交互设置")]
        [SerializeField] private float interactRange = 1.2f;   // 检测半径（单位）
        [SerializeField] private float detectInterval = 0.15f; // 检测频率（秒），避免每帧 OverlapCircle

        private HUD _hud;
        private float _detectTimer;
        private IInteractable _candidate;   // 当前可交互目标

        private void Start()
        {
            _hud = FindAnyObjectByType<HUD>(FindObjectsInactive.Include);
        }

        private void Update()
        {
            // 对话/暂停/菜单/过场中不交互
            if (GameManager.Instance != null && !GameManager.Instance.CanPlayerAct())
            {
                ClearCandidate();
                return;
            }

            // 按间隔刷新附近可交互目标
            _detectTimer -= Time.deltaTime;
            if (_detectTimer <= 0f)
            {
                _detectTimer = detectInterval;
                RefreshCandidate();
            }

            // 按下交互键 → 触发当前目标
            if (_candidate != null && Input.GetButtonDown("Interact"))
            {
                _candidate.OnInteract(gameObject);
                // 触发后清空，等下一轮检测重新填充（避免同帧重复触发）
                _candidate = null;
                if (_hud != null) _hud.HideInteractPrompt();
            }
        }

        /// <summary>
        /// 扫描范围内最近且可交互的目标
        /// </summary>
        private void RefreshCandidate()
        {
            IInteractable best = null;
            float bestSqrDist = float.MaxValue;

            // 不限定 layer：靠 IInteractable 组件过滤，规避 NPC 层配置缺失的问题
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactRange);
            foreach (var hit in hits)
            {
                if (hit == null) continue;
                // 跳过玩家自身
                if (hit.gameObject == gameObject) continue;

                var interactable = hit.GetComponent<IInteractable>();
                if (interactable == null) continue;
                if (!interactable.CanInteract()) continue;

                float sqrDist = ((Vector2)(hit.transform.position - transform.position)).sqrMagnitude;
                if (sqrDist < bestSqrDist)
                {
                    bestSqrDist = sqrDist;
                    best = interactable;
                }
            }

            if (best != _candidate)
            {
                _candidate = best;
                if (_candidate != null)
                {
                    // 尝试用对象名作提示文案
                    string prompt = GetPromptText(_candidate);
                    if (_hud != null) _hud.ShowInteractPrompt(prompt);
                }
                else
                {
                    if (_hud != null) _hud.HideInteractPrompt();
                }
            }
        }

        private void ClearCandidate()
        {
            if (_candidate != null)
            {
                _candidate = null;
                if (_hud != null) _hud.HideInteractPrompt();
            }
        }

        /// <summary>
        /// 生成提示文案（优先取 NPC 名 / 传送点 prompt）
        /// </summary>
        private string GetPromptText(IInteractable target)
        {
            var targetBehaviour = target as MonoBehaviour;
            var go = targetBehaviour != null ? targetBehaviour.gameObject : null;
            if (go != null)
            {
                var npc = go.GetComponent<NPCBase>();
                if (npc != null && !string.IsNullOrEmpty(npc.npcName))
                    return $"[K/E] 与 {npc.npcName} 交谈";
            }
            return "[K/E] 交互";
        }

        private void OnDisable()
        {
            ClearCandidate();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
#endif
    }
}
