using YuanHaiLu.GameSystem;
using UnityEngine;
using YuanHaiLu.Core;

namespace YuanHaiLu.Map
{
    /// <summary>
    /// 可破坏物体 — 木箱、花瓶等可被攻击破坏的物体
    /// 挂载到可破坏的地图物体上
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]
    public class Destructible : MonoBehaviour, Character.IInteractable
    {
        [Header("物体设置")]
        public string objectName = "木箱";
        public int hp = 3;
        public bool respawn = false;
        public float respawnTime = 30f;

        [Header("掉落")]
        [Tooltip("掉落物品ID列表（随机选一个，可空表示不掉落）")]
        public string[] dropItemIds;
        [Range(0f, 1f)] public float dropChance = 0.5f;
        [Tooltip("掉落金币范围")]
        public Vector2Int goldDropRange = new Vector2Int(0, 5);

        [Header("视觉效果")]
        public Sprite damagedSprite;           // 受损外观
        public GameObject destroyEffect;       // 破坏粒子特效
        public GameObject dropItemPrefab;      // 掉落物预制体

        [Header("音效")]
        public string hitSfx = "hit_wood";
        public string destroySfx = "break_wood";

        private SpriteRenderer _sprite;
        private Sprite _originalSprite;
        private Collider2D _col;
        private int _currentHp;

        public bool IsDestroyed => _currentHp <= 0;

        private void Awake()
        {
            _sprite = GetComponent<SpriteRenderer>();
            _col = GetComponent<Collider2D>();
            _originalSprite = _sprite.sprite;
            _currentHp = hp;
        }

        /// <summary>
        /// 受到攻击
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (_currentHp <= 0) return;

            _currentHp -= damage;

            // 受击音效
            if (GameSystem.AudioManager.Instance != null)
                GameSystem.AudioManager.Instance.PlaySFXRandomPitch(hitSfx);

            // 受损外观
            if (_currentHp <= hp / 2 && damagedSprite != null)
            {
                _sprite.sprite = damagedSprite;
            }

            // 摇晃效果
            StartCoroutine(ShakeObject());

            if (_currentHp <= 0)
            {
                Destroy();
            }
        }

        private void Destroy()
        {
            // 破坏音效
            if (GameSystem.AudioManager.Instance != null)
                GameSystem.AudioManager.Instance.PlaySFX(destroySfx);

            // 破坏特效
            if (destroyEffect != null)
            {
                Instantiate(destroyEffect, transform.position, Quaternion.identity);
            }

            // 掉落物品
            SpawnDrops();

            // 隐藏物体
            _sprite.enabled = false;
            _col.enabled = false;

            // 重生计时
            if (respawn)
            {
                StartCoroutine(RespawnAfterDelay());
            }
        }

        private void SpawnDrops()
        {
            // 金币掉落
            int gold = Random.Range(goldDropRange.x, goldDropRange.y + 1);
            if (gold > 0 && GameSystem.InventoryManager.Instance != null)
            {
                GameSystem.InventoryManager.Instance.AddGold(gold);
                Debug.Log($"[Destructible] {objectName} 掉落了 {gold} 文钱");
            }

            // 物品掉落
            if (dropItemIds != null && dropItemIds.Length > 0 && Random.value <= dropChance)
            {
                string itemId = dropItemIds[Random.Range(0, dropItemIds.Length)];
                if (!string.IsNullOrEmpty(itemId))
                {
                    if (dropItemPrefab != null)
                    {
                        // 生成掉落物
                        GameObject drop = Instantiate(dropItemPrefab, transform.position + Vector3.up * 0.3f, Quaternion.identity);
                        var pickup = drop.AddComponent<ItemPickup>();
                        pickup.itemId = itemId;
                    }
                    else
                    {
                        // 直接加入背包
                        if (GameSystem.InventoryManager.Instance != null)
                            GameSystem.InventoryManager.Instance.AddItem(itemId);
                    }
                    Debug.Log($"[Destructible] {objectName} 掉落了物品: {itemId}");
                }
            }
        }

        private System.Collections.IEnumerator ShakeObject()
        {
            Vector3 originalPos = transform.position;
            float duration = 0.15f;
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float offsetX = Random.Range(-0.05f, 0.05f);
                float offsetY = Random.Range(-0.05f, 0.05f);
                transform.position = originalPos + new Vector3(offsetX, offsetY, 0);
                yield return null;
            }

            transform.position = originalPos;
        }

        private System.Collections.IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(respawnTime);

            _currentHp = hp;
            _sprite.sprite = _originalSprite;
            _sprite.enabled = true;
            _col.enabled = true;
        }

        // IInteractable 接口
        public void OnInteract(GameObject player)
        {
            // 如果有交互键，也可以推开木箱等
            TakeDamage(1);
        }

        public bool CanInteract()
        {
            return !IsDestroyed;
        }
    }
}
