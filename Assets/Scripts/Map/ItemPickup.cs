using UnityEngine;
using YuanHaiLu.GameSystem;

namespace YuanHaiLu.Map
{
    /// <summary>
    /// 地面掉落物品 — 场景中可拾取的物品
    /// 挂载到掉落物预制体上，需要 Collider2D (Is Trigger)
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]
    public class ItemPickup : MonoBehaviour
    {
        [Header("物品信息")]
        public string itemId = "";
        public int amount = 1;
        public Sprite itemSprite;

        [Header("行为")]
        public bool autoPickup = false;         // 走过自动拾取
        public float lifetime = 60f;            // 存在时间（秒），0=永久
        public float pickupDelay = 0.5f;        // 掉落后延迟可拾取
        public float bobAmplitude = 0.1f;       // 浮动幅度
        public float bobSpeed = 2f;             // 浮动速度
        public float magnetRange = 1.5f;        // 磁吸范围（靠近自动飞向玩家）

        [Header("音效")]
        public string pickupSfx = AudioManager.SFX.PICKUP_ITEM;

        private SpriteRenderer _sprite;
        private float _spawnTime;
        private bool _canPickup = false;
        private Vector3 _basePosition;

        private void Awake()
        {
            _sprite = GetComponent<SpriteRenderer>();
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;

            _spawnTime = Time.time;
            _basePosition = transform.position;

            // 设置图标
            if (itemSprite != null)
            {
                _sprite.sprite = itemSprite;
            }

            // 随机弹出效果
            StartCoroutine(PopIn());
        }

        private void Start()
        {
            // 延迟可拾取
            StartCoroutine(EnablePickupAfterDelay());
        }

        private void Update()
        {
            // 浮动动画
            if (bobAmplitude > 0)
            {
                float offsetY = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
                transform.position = _basePosition + new Vector3(0, offsetY, 0);
            }

            // 磁吸效果
            if (_canPickup && magnetRange > 0)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    float dist = Vector2.Distance(transform.position, player.transform.position);
                    if (dist < magnetRange)
                    {
                        transform.position = Vector2.MoveTowards(
                            transform.position,
                            player.transform.position,
                            5f * Time.deltaTime
                        );
                    }
                }
            }

            // 过期消失
            if (lifetime > 0 && Time.time - _spawnTime > lifetime)
            {
                StartCoroutine(FadeOutAndDestroy());
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_canPickup) return;
            if (!other.CompareTag("Player")) return;

            Pickup(other.gameObject);
        }

        private void Pickup(GameObject player)
        {
            if (!TryAddToInventory(itemId, amount)) return;

            // 音效
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFXRandomPitch(pickupSfx);

            // 小弹出文字效果（可选）
            Debug.Log($"[ItemPickup] 拾取了 {itemId} x{amount}");

            Destroy(gameObject);
        }

        internal static bool TryAddToInventory(string targetItemId, int targetAmount)
        {
            if (InventoryManager.Instance == null ||
                string.IsNullOrEmpty(targetItemId) ||
                targetAmount <= 0)
            {
                return false;
            }

            if (!InventoryManager.Instance.AddItem(targetItemId, targetAmount))
                return false;

            QuestManager.Instance?.UpdateObjective(
                QuestObjective.ObjectiveType.CollectItem,
                targetItemId,
                targetAmount);
            return true;
        }

        private System.Collections.IEnumerator PopIn()
        {
            // 从0缩放到1
            float duration = 0.2f;
            float timer = 0f;
            Vector3 targetScale = transform.localScale;
            transform.localScale = Vector3.zero;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;
                // 弹性效果
                float scale = Mathf.Sin(t * Mathf.PI * 0.5f) * 1.1f;
                transform.localScale = targetScale * scale;
                yield return null;
            }

            transform.localScale = targetScale;
        }

        private System.Collections.IEnumerator EnablePickupAfterDelay()
        {
            yield return new WaitForSeconds(pickupDelay);
            _canPickup = true;
        }

        private System.Collections.IEnumerator FadeOutAndDestroy()
        {
            float duration = 0.5f;
            float timer = 0f;
            Color color = _sprite.color;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                color.a = 1f - (timer / duration);
                _sprite.color = color;
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
