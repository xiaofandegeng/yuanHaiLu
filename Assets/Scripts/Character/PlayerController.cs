using UnityEngine;
using YuanHaiLu.Core;

namespace YuanHaiLu.Character
{
    /// <summary>
    /// 玩家控制器 — 8方向移动 + 冲刺 + 方向锁定
    /// 挂载到玩家角色上，需要 Rigidbody2D + Animator + SpriteRenderer
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerController : MonoBehaviour
    {
        [Header("移动参数")]
        [SerializeField] private float moveSpeed = GameConfig.MOVE_SPEED;
        [SerializeField] private float dashSpeed = GameConfig.DASH_SPEED;
        [SerializeField] private float dashDuration = GameConfig.DASH_DURATION;
        [SerializeField] private float dashCooldown = GameConfig.DASH_COOLDOWN;

        [Header("组件引用")]
        private Rigidbody2D _rb;
        private Animator _anim;
        private SpriteRenderer _sprite;

        // === 状态 ===
        private Vector2 _moveInput;
        private Vector2 _lastDirection = Vector2.down; // 默认面朝下（正面）
        private bool _isDashing = false;
        private bool _canDash = true;
        private float _dashTimer = 0f;
        private float _dashCooldownTimer = 0f;
        private bool _inputEnabled = true;

        // === 动画参数名 ===
        private static readonly int AnimMoveX = Animator.StringToHash("MoveX");
        private static readonly int AnimMoveY = Animator.StringToHash("MoveY");
        private static readonly int AnimSpeed = Animator.StringToHash("Speed");
        private static readonly int AnimIsDashing = Animator.StringToHash("IsDashing");
        private static readonly int AnimIsAttacking = Animator.StringToHash("IsAttacking");
        private static readonly int AnimAttackIndex = Animator.StringToHash("AttackIndex");
        private static readonly int AnimFacing = Animator.StringToHash("Facing");

        public Vector2 LastDirection => _lastDirection;
        public bool IsMoving => _moveInput.sqrMagnitude > 0.01f;
        public bool IsDashing => _isDashing;
        public bool IsInputEnabled => _inputEnabled;
        private bool CanAnimate => _anim != null && _anim.runtimeAnimatorController != null;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _anim = GetComponent<Animator>();
            _sprite = GetComponent<SpriteRenderer>();

            // 刚体设置（2D Top-Down）
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        private void Update()
        {
            if (!_inputEnabled) return;

            HandleInput();
            HandleDash();
            UpdateAnimator();
        }

        private void FixedUpdate()
        {
            HandleMovement();
        }

        // === 输入处理 ===
        private void HandleInput()
        {
            _moveInput = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")
            );

            // 归一化（防止斜向移动更快）
            if (_moveInput.sqrMagnitude > 1f)
            {
                _moveInput.Normalize();
            }

            // 记录最后朝向（仅在有输入时更新）
            if (_moveInput.sqrMagnitude > 0.01f)
            {
                _lastDirection = _moveInput;
            }

            // 冲刺输入
            if (Input.GetButtonDown("Dash") && _canDash && IsMoving && !_isDashing)
            {
                StartDash();
            }
        }

        // === 移动 ===
        private void HandleMovement()
        {
            if (_isDashing)
            {
                _rb.linearVelocity = _lastDirection * dashSpeed;
            }
            else
            {
                _rb.linearVelocity = _moveInput * moveSpeed;
            }
        }

        // === 冲刺 ===
        private void StartDash()
        {
            _isDashing = true;
            _canDash = false;
            _dashTimer = dashDuration;
        }

        private void HandleDash()
        {
            if (!_isDashing) return;

            _dashTimer -= Time.deltaTime;
            if (_dashTimer <= 0f)
            {
                _isDashing = false;
                _dashCooldownTimer = dashCooldown;
                Invoke(nameof(ResetDash), dashCooldown);
            }
        }

        private void ResetDash()
        {
            _canDash = true;
        }

        // === 动画更新 ===
        private void UpdateAnimator()
        {
            if (!CanAnimate) return;

            _anim.SetFloat(AnimMoveX, _lastDirection.x);
            _anim.SetFloat(AnimMoveY, _lastDirection.y);
            _anim.SetFloat(AnimSpeed, _moveInput.sqrMagnitude);
            _anim.SetBool(AnimIsDashing, _isDashing);
            _anim.SetInteger(AnimFacing, FacingIndex(_lastDirection));
        }

        // === 外部控制 ===
        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            if (!enabled)
            {
                _moveInput = Vector2.zero;
                _rb.linearVelocity = Vector2.zero;
                if (CanAnimate) _anim.SetFloat(AnimSpeed, 0f);
            }
        }

        public void FaceDirection(Vector2 direction)
        {
            _lastDirection = direction.normalized;
            if (CanAnimate)
            {
                _anim.SetFloat(AnimMoveX, _lastDirection.x);
                _anim.SetFloat(AnimMoveY, _lastDirection.y);
                _anim.SetInteger(AnimFacing, FacingIndex(_lastDirection));
            }
        }

        private static int FacingIndex(Vector2 direction)
        {
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
                return direction.x < 0f ? 1 : 2;
            return direction.y > 0f ? 3 : 0;
        }

        // === 碰撞排序（Y轴排序，让前方角色遮挡后方） ===
        private void LateUpdate()
        {
            // 修改 sorting order 基于 Y 坐标
            // Y 越小（越靠屏幕下方）→ 越前面 → order 越大
            _sprite.sortingOrder = -(int)(transform.position.y * 10);
        }
    }
}
