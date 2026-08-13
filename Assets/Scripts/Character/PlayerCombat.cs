using UnityEngine;
using YuanHaiLu.Core;
using YuanHaiLu.Effects;

namespace YuanHaiLu.Character
{
    /// <summary>
    /// 玩家战斗控制器 — 管理攻击输入、连击系统、攻击判定
    /// 挂载到玩家角色上
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(CharacterStats))]
    [RequireComponent(typeof(Animator))]
    public class PlayerCombat : MonoBehaviour
    {
        [Header("攻击设置")]
        [SerializeField] private float attackDuration = GameConfig.ATTACK_DURATION;
        [SerializeField] private float comboWindow = GameConfig.ATTACK_COMBO_WINDOW;
        [SerializeField] private int maxCombo = GameConfig.MAX_COMBO;
        [SerializeField] private float attackRange = 1.2f;
        [SerializeField] private Vector2 attackBoxSize = new Vector2(1.0f, 0.8f);

        [Header("剑气颜色")]
        public Color slashColor = new Color(0.7f, 0.9f, 1f, 0.8f);  // 冰蓝色剑气

        [Header("组件")]
        private PlayerController _controller;
        private CharacterStats _stats;
        private Animator _anim;

        // === 状态 ===
        private bool _isAttacking = false;
        private int _comboIndex = 0;
        private bool _comboQueued = false;
        private float _attackTimer = 0f;
        private float _comboTimer = 0f;

        // === 动画参数 ===
        private static readonly int AnimIsAttacking = Animator.StringToHash("IsAttacking");
        private static readonly int AnimAttackIndex = Animator.StringToHash("AttackIndex");
        private bool CanAnimate => _anim != null && _anim.runtimeAnimatorController != null;

        // === 事件 ===
        public event System.Action<int> OnAttackHit;     // (damage dealt)
        public event System.Action OnComboFinished;

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _stats = GetComponent<CharacterStats>();
            _anim = GetComponent<Animator>();
        }

        private void Update()
        {
            var gameManager = GameManager.Instance;
            if (gameManager == null || !gameManager.CanPlayerAct()) return;

            HandleAttackInput();
            HandleAttackTimers();
        }

        private void HandleAttackInput()
        {
            // 攻击输入
            if (Input.GetButtonDown("Attack"))
            {
                if (!_isAttacking)
                {
                    StartAttack();
                }
                else if (_isAttacking && _comboTimer > 0f)
                {
                    // 连击预输入
                    _comboQueued = true;
                }
            }
        }

        private void StartAttack()
        {
            _isAttacking = true;
            _attackTimer = attackDuration;

            if (CanAnimate)
            {
                _anim.SetBool(AnimIsAttacking, true);
                _anim.SetInteger(AnimAttackIndex, _comboIndex);
            }

            // 禁用移动（攻击期间不能移动）
            _controller.SetInputEnabled(false);

            Debug.Log($"[PlayerCombat] 攻击！连击数: {_comboIndex + 1}");
        }

        /// <summary>
        /// 由动画事件调用 — 在攻击动画的判定帧触发
        /// </summary>
        public void OnAttackHitFrame()
        {
            // 计算攻击判定区域
            Vector2 attackCenter = (Vector2)transform.position + _controller.LastDirection * attackRange;

            // === 剑气轨迹 ===
            Vector2 slashStart = (Vector2)transform.position + _controller.LastDirection * 0.3f;
            Vector2 slashEnd = attackCenter;
            // 垂直方向偏移（模拟横向挥剑）
            Vector2 perp = Vector2.Perpendicular(_controller.LastDirection) * 0.5f;
            EffectsManager.Instance?.PlaySlashTrail(slashStart + perp, slashEnd - perp, slashColor, 0.2f);

            // 检测范围内的敌人
            Collider2D[] hits = Physics2D.OverlapBoxAll(attackCenter, attackBoxSize, 0f,
                LayerMask.GetMask("Enemy"));

            bool anyHit = false;
            foreach (var hit in hits)
            {
                var enemyStats = hit.GetComponent<CharacterStats>();
                if (enemyStats != null && enemyStats.IsAlive)
                {
                    // 计算伤害（连击加成）
                    float comboMultiplier = 1f + (_comboIndex * 0.15f);
                    bool isCrit = Random.Range(0, 100) < _stats.critRate;
                    if (isCrit) comboMultiplier *= _stats.critMultiplier;
                    int damage = Mathf.RoundToInt(_stats.attack * comboMultiplier);

                    enemyStats.TakeDamage(damage, _stats);
                    OnAttackHit?.Invoke(damage);

                    // === 视觉反馈 ===
                    EffectsManager.HitSpark(hit.transform.position, _controller.LastDirection);
                    EffectsManager.DamageNumber(hit.transform.position, damage, isCrit);

                    if (isCrit)
                    {
                        EffectsManager.CritEffect(hit.transform.position);
                    }

                    // 摄像机震动（连击越高震动越大）
                    var camFollow = Camera.main?.GetComponent<CameraFollow>();
                    if (camFollow != null) camFollow.Shake(0.05f + _comboIndex * 0.03f);

                    anyHit = true;
                    Debug.Log($"[PlayerCombat] 命中 {enemyStats.characterName}，" +
                              $"造成 {damage} 伤害（连击 x{_comboIndex + 1}）{(isCrit ? " 暴击!" : "")}");
                }
            }

            // 检测可破坏物体
            if (!anyHit)
            {
                Collider2D[] destructibles = Physics2D.OverlapBoxAll(attackCenter, attackBoxSize, 0f,
                    LayerMask.GetMask("Environment"));
                foreach (var hit in destructibles)
                {
                    var destructible = hit.GetComponent<Map.Destructible>();
                    if (destructible != null && destructible.CanInteract())
                    {
                        destructible.TakeDamage(1);
                    }
                }
            }
        }

        /// <summary>
        /// 由动画事件调用 — 攻击动画结束
        /// </summary>
        public void OnAttackAnimationEnd()
        {
            _isAttacking = false;
            if (CanAnimate) _anim.SetBool(AnimIsAttacking, false);

            if (_comboQueued && _comboIndex < maxCombo - 1)
            {
                // 进入下一连击
                _comboIndex++;
                _comboQueued = false;
                StartAttack();
            }
            else
            {
                // 连击结束
                EndCombo();
            }
        }

        private void EndCombo()
        {
            _comboIndex = 0;
            _comboQueued = false;
            _comboTimer = 0f;
            _controller.SetInputEnabled(true);
            OnComboFinished?.Invoke();
        }

        private void HandleAttackTimers()
        {
            if (!_isAttacking) return;

            _attackTimer -= Time.deltaTime;

            // 连击窗口计时（攻击后半段可以预输入）
            if (_attackTimer < attackDuration * 0.5f)
            {
                _comboTimer = comboWindow;
            }
            else
            {
                _comboTimer -= Time.deltaTime;
            }

            // 安全超时（动画事件未触发时自动结束）
            if (_attackTimer <= 0f)
            {
                OnAttackAnimationEnd();
            }
        }

        // === 编辑器辅助 ===
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_controller == null) return;

            // 绘制攻击判定区域
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.4f);
            Vector2 center = (Vector2)transform.position + _controller.LastDirection * attackRange;
            Gizmos.DrawWireCube(center, attackBoxSize);
        }
#endif
    }
}
