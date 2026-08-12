using UnityEngine;

namespace YuanHaiLu.Character
{
    /// <summary>
    /// NPC 基类 — 可交互NPC的通用行为
    /// 挂载到NPC预制体上，需要 Collider2D（Is Trigger）
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class NPCBase : MonoBehaviour, IInteractable
    {
        [Header("NPC信息")]
        public string npcName = "村民";
        public string npcTitle = "";           // 称号，如"铁匠"、"药铺掌柜"
        public Sprite portrait;                // 对话头像

        [Header("对话数据")]
        [TextArea(2, 5)]
        public string[] defaultDialogue;        // 默认对话文本
        public string[] questDialogue;          // 有任务时的对话
        public string[] postQuestDialogue;      // 任务完成后的对话

        [Header("移动设置")]
        public bool canWander = true;           // 是否随机走动
        public float wanderRadius = 3f;         // 走动范围
        public float wanderInterval = 3f;       // 走动间隔（秒）
        public float wanderSpeed = 1f;          // 走动速度

        [Header("状态")]
        public bool hasQuest = false;
        public bool questCompleted = false;
        public bool interactable = true;

        // === 内部状态 ===
        private Vector2 _originPosition;
        private Vector2 _targetPosition;
        private float _wanderTimer;
        private bool _isWandering = false;
        private SpriteRenderer _sprite;
        private Animator _anim;

        private static readonly int AnimSpeed = Animator.StringToHash("Speed");
        private static readonly int AnimMoveX = Animator.StringToHash("MoveX");
        private static readonly int AnimMoveY = Animator.StringToHash("MoveY");
        private bool CanAnimate => _anim != null && _anim.runtimeAnimatorController != null;

        private void Awake()
        {
            _sprite = GetComponent<SpriteRenderer>();
            _anim = GetComponent<Animator>();
            _originPosition = transform.position;
            _wanderTimer = Random.Range(0f, wanderInterval);
        }

        private void Update()
        {
            if (!canWander) return;

            _wanderTimer -= Time.deltaTime;

            if (_wanderTimer <= 0f && !_isWandering)
            {
                StartWander();
            }

            if (_isWandering)
            {
                MoveToTarget();
            }
        }

        private void StartWander()
        {
            // 在范围内随机选一个目标点
            Vector2 randomOffset = Random.insideUnitCircle * wanderRadius;
            _targetPosition = _originPosition + randomOffset;
            _isWandering = true;

            // 更新动画方向
            Vector2 dir = (_targetPosition - (Vector2)transform.position).normalized;
            if (CanAnimate)
            {
                _anim.SetFloat(AnimMoveX, dir.x);
                _anim.SetFloat(AnimMoveY, dir.y);
                _anim.SetFloat(AnimSpeed, 1f);
            }
        }

        private void MoveToTarget()
        {
            Vector2 current = transform.position;
            float dist = Vector2.Distance(current, _targetPosition);

            if (dist < 0.1f)
            {
                // 到达目标，停下来
                _isWandering = false;
                _wanderTimer = wanderInterval + Random.Range(-1f, 1f);

                if (CanAnimate)
                {
                    _anim.SetFloat(AnimSpeed, 0f);
                }
                return;
            }

            // 移动
            Vector2 direction = (_targetPosition - current).normalized;
            transform.position = Vector2.MoveTowards(current, _targetPosition, wanderSpeed * Time.deltaTime);
        }

        // === IInteractable 实现 ===
        public virtual void OnInteract(GameObject player)
        {
            if (!interactable) return;

            string[] dialogue = GetCurrentDialogue();
            if (dialogue == null || dialogue.Length == 0)
            {
                Debug.Log($"[{npcName}] 没有对话内容");
                return;
            }

            // 触发对话系统
            var dialogueManager = Dialogue.DialogueManager.Instance;
            if (dialogueManager != null)
            {
                dialogueManager.StartDialogue(npcName, dialogue);
            }
        }

        public virtual bool CanInteract()
        {
            return interactable;
        }

        protected virtual string[] GetCurrentDialogue()
        {
            if (questCompleted && postQuestDialogue != null && postQuestDialogue.Length > 0)
                return postQuestDialogue;
            if (hasQuest && questDialogue != null && questDialogue.Length > 0)
                return questDialogue;
            return defaultDialogue;
        }

        // === Y轴排序 ===
        private void LateUpdate()
        {
            if (_sprite != null)
            {
                _sprite.sortingOrder = -(int)(transform.position.y * 10);
            }
        }

        // === 编辑器 ===
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (canWander)
            {
                Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.3f);
                Gizmos.DrawWireSphere(_originPosition, wanderRadius);
            }
        }
#endif
    }

    /// <summary>
    /// 可交互接口
    /// </summary>
    public interface IInteractable
    {
        void OnInteract(GameObject player);
        bool CanInteract();
    }
}
