using UnityEngine;
using YuanHaiLu.Character;
using YuanHaiLu.Core;

namespace YuanHaiLu.Character
{
    /// <summary>
    /// 敌人AI — 巡逻 + 追击 + 攻击
    /// 挂载到敌人预制体上
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CharacterStats))]
    public class EnemyAI : MonoBehaviour
    {
        public enum AIState
        {
            Idle,       // 待机
            Patrol,     // 巡逻
            Chase,      // 追击
            Attack,     // 攻击
            Return      // 返回巡逻点
        }

        [Header("AI 状态")]
        public AIState currentState = AIState.Idle;

        [Header("检测范围")]
        [SerializeField] private float detectRange = 5f;      // 侦测范围
        [SerializeField] private float attackRange = 1.0f;    // 攻击距离
        [SerializeField] private float loseSightRange = 8f;   // 脱离范围

        [Header("移动")]
        [SerializeField] private float patrolSpeed = 1f;
        [SerializeField] private float chaseSpeed = 2.5f;

        [Header("巡逻")]
        [SerializeField] private float patrolRadius = 3f;
        [SerializeField] private float waitTimeAtPoint = 2f;

        [Header("攻击")]
        [SerializeField] private float attackCooldown = 1.5f;
        [SerializeField] private float attackDuration = 0.5f;

        // === 组件 ===
        private Rigidbody2D _rb;
        private CharacterStats _stats;
        private Animator _anim;
        private SpriteRenderer _sprite;

        // === 状态变量 ===
        private Vector2 _originPosition;
        private Vector2 _patrolTarget;
        private Transform _player;
        private float _waitTimer = 0f;
        private float _attackCooldownTimer = 0f;
        private float _attackTimer = 0f;
        private Vector2 _lastDirection = Vector2.down;

        private static readonly int AnimSpeed = Animator.StringToHash("Speed");
        private static readonly int AnimMoveX = Animator.StringToHash("MoveX");
        private static readonly int AnimMoveY = Animator.StringToHash("MoveY");
        private static readonly int AnimIsAttacking = Animator.StringToHash("IsAttacking");
        private bool CanAnimate => _anim != null && _anim.runtimeAnimatorController != null;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _stats = GetComponent<CharacterStats>();
            _anim = GetComponent<Animator>();
            _sprite = GetComponent<SpriteRenderer>();

            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
        }

        private void Start()
        {
            _originPosition = transform.position;
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            _player = playerGo != null ? playerGo.transform : null;
            SetNewPatrolTarget();
        }

        private void Update()
        {
            if (!_stats.IsAlive) return;

            _attackCooldownTimer -= Time.deltaTime;

            UpdateState();
            ExecuteState();
            UpdateAnimator();
        }

        private void UpdateState()
        {
            if (_player == null) return;

            float distToPlayer = Vector2.Distance(transform.position, _player.position);

            switch (currentState)
            {
                case AIState.Idle:
                case AIState.Patrol:
                    if (distToPlayer <= detectRange)
                    {
                        currentState = AIState.Chase;
                    }
                    break;

                case AIState.Chase:
                    if (distToPlayer <= attackRange && _attackCooldownTimer <= 0f)
                    {
                        currentState = AIState.Attack;
                    }
                    else if (distToPlayer > loseSightRange)
                    {
                        currentState = AIState.Return;
                    }
                    break;

                case AIState.Attack:
                    if (_attackTimer <= 0f)
                    {
                        if (distToPlayer <= detectRange)
                            currentState = AIState.Chase;
                        else
                            currentState = AIState.Return;
                    }
                    break;

                case AIState.Return:
                    float distToOrigin = Vector2.Distance(transform.position, _originPosition);
                    if (distToOrigin < 0.5f)
                    {
                        currentState = AIState.Idle;
                        SetNewPatrolTarget();
                    }
                    break;
            }
        }

        private void ExecuteState()
        {
            switch (currentState)
            {
                case AIState.Idle:
                    _waitTimer -= Time.deltaTime;
                    _rb.linearVelocity = Vector2.zero;
                    if (_waitTimer <= 0f)
                    {
                        currentState = AIState.Patrol;
                    }
                    break;

                case AIState.Patrol:
                    MoveTowards(_patrolTarget, patrolSpeed);
                    float distToTarget = Vector2.Distance(transform.position, _patrolTarget);
                    if (distToTarget < 0.2f)
                    {
                        currentState = AIState.Idle;
                        _waitTimer = waitTimeAtPoint + Random.Range(-0.5f, 0.5f);
                    }
                    break;

                case AIState.Chase:
                    if (_player != null)
                    {
                        MoveTowards(_player.position, chaseSpeed);
                    }
                    break;

                case AIState.Attack:
                    PerformAttack();
                    break;

                case AIState.Return:
                    MoveTowards(_originPosition, patrolSpeed);
                    break;
            }
        }

        private void MoveTowards(Vector2 target, float speed)
        {
            Vector2 direction = (target - (Vector2)transform.position).normalized;
            _rb.linearVelocity = direction * speed;
            _lastDirection = direction;
        }

        private void PerformAttack()
        {
            _attackTimer -= Time.deltaTime;

            if (_attackTimer <= 0f && _attackCooldownTimer <= 0f)
            {
                // 执行攻击
                _rb.linearVelocity = Vector2.zero;
                _attackTimer = attackDuration;
                _attackCooldownTimer = attackCooldown;

                if (CanAnimate) _anim.SetBool(AnimIsAttacking, true);

                // 攻击判定
                Vector2 attackCenter = (Vector2)transform.position + _lastDirection * attackRange;
                Collider2D[] hits = Physics2D.OverlapCircleAll(attackCenter, attackRange * 0.6f,
                    LayerMask.GetMask("Player"));

                foreach (var hit in hits)
                {
                    var playerStats = hit.GetComponent<CharacterStats>();
                    if (playerStats != null && playerStats.IsAlive)
                    {
                        playerStats.TakeDamage(_stats.attack, _stats);
                    }
                }
            }
            else if (_attackTimer <= 0f)
            {
                if (CanAnimate) _anim.SetBool(AnimIsAttacking, false);
            }
        }

        private void SetNewPatrolTarget()
        {
            Vector2 randomOffset = Random.insideUnitCircle * patrolRadius;
            _patrolTarget = _originPosition + randomOffset;
        }

        private void UpdateAnimator()
        {
            if (!CanAnimate) return;
            _anim.SetFloat(AnimMoveX, _lastDirection.x);
            _anim.SetFloat(AnimMoveY, _lastDirection.y);
            _anim.SetFloat(AnimSpeed, _rb.linearVelocity.sqrMagnitude > 0.01f ? 1f : 0f);
        }

        // Y轴排序
        private void LateUpdate()
        {
            if (_sprite != null)
                _sprite.sortingOrder = -(int)(transform.position.y * 10);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // 侦测范围
            Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, detectRange);

            // 攻击范围
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // 巡逻范围
            Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(Application.isPlaying ? _originPosition : (Vector2)transform.position, patrolRadius);
        }
#endif
    }
}
