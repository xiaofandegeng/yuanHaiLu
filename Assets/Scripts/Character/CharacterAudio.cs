using UnityEngine;
using YuanHaiLu.GameSystem;

namespace YuanHaiLu.Character
{
    /// <summary>
    /// 角色音效控制器 — 脚步声、受击音效等
    /// 挂载到角色上
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class CharacterAudio : MonoBehaviour
    {
        [Header("脚步声设置")]
        [SerializeField] private float footstepInterval = 0.3f;  // 步伐间隔
        [SerializeField] private string defaultFootstep = AudioManager.SFX.FOOTSTEP_GRASS;

        [Header("受击/死亡")]
        [SerializeField] private string hurtSfx = AudioManager.SFX.PLAYER_HURT;
        [SerializeField] private string deathSfx = AudioManager.SFX.ENEMY_DEATH;

        private PlayerController _controller;
        private CharacterStats _stats;
        private float _footstepTimer;

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _stats = GetComponent<CharacterStats>();
        }

        private void Start()
        {
            if (_stats != null)
            {
                _stats.OnDamaged += OnDamaged;
                _stats.OnDeath += OnDeath;
            }
        }

        private void Update()
        {
            if (!_controller.IsMoving) return;

            _footstepTimer -= Time.deltaTime;
            if (_footstepTimer <= 0f)
            {
                string footstep = GetTerrainFootstep();
                AudioManager.Instance?.PlaySFXRandomPitch(footstep, 0.85f, 1.15f);

                // 冲刺时步伐更快
                float interval = _controller.IsDashing ? footstepInterval * 0.6f : footstepInterval;
                _footstepTimer = interval;
            }
        }

        /// <summary>
        /// 根据脚下地形返回不同脚步声
        /// </summary>
        private string GetTerrainFootstep()
        {
            // 向下射线检测地形类型
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 0.5f,
                LayerMask.GetMask("Ground"));

            if (hit.collider != null)
            {
                string tag = hit.collider.tag;
                return tag switch
                {
                    "StonePath" => AudioManager.SFX.FOOTSTEP_STONE,
                    "Water" => AudioManager.SFX.FOOTSTEP_WATER,
                    _ => defaultFootstep
                };
            }

            return defaultFootstep;
        }

        private void OnDamaged(int damage)
        {
            AudioManager.Instance?.PlaySFXRandomPitch(hurtSfx, 0.9f, 1.1f);
        }

        private void OnDeath()
        {
            AudioManager.Instance?.PlaySFX(deathSfx);
        }

        private void OnDestroy()
        {
            if (_stats != null)
            {
                _stats.OnDamaged -= OnDamaged;
                _stats.OnDeath -= OnDeath;
            }
        }
    }
}
