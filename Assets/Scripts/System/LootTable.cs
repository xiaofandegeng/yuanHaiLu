using UnityEngine;
using YuanHaiLu.Map;
using System.Collections;
using YuanHaiLu.Core;
using YuanHaiLu.Character;

namespace YuanHaiLu.GameSystem
{
    [System.Serializable]
    public class LootEntry
    {
        public string itemId;
        public int minAmount = 1;
        public int maxAmount = 1;
        [Range(0f, 1f)] public float dropChance = 0.5f;
        public bool guaranteed = false;
    }

    /// <summary>
    /// 掉落系统 — 敌人死亡时掉落金币/经验/物品
    /// 挂载到敌人预制体上配置掉落表
    /// </summary>
    public class LootTable : MonoBehaviour
    {

        [Header("掉落设置")]
        public int minGold = 5;
        public int maxGold = 20;
        public int expReward = 15;
        public LootEntry[] lootItems;

        [Header("掉落物设置")]
        public float dropSpread = 1f;        // 掉落散布范围
        public float dropDuration = 30f;     // 掉落物存在时间

        private CharacterStats _stats;

        private void Awake()
        {
            _stats = GetComponent<CharacterStats>();
            if (_stats != null)
            {
                _stats.OnDeath += OnEnemyDeath;
            }
        }

        private void OnDestroy()
        {
            if (_stats != null)
            {
                _stats.OnDeath -= OnEnemyDeath;
            }
        }

        private void OnEnemyDeath()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            var levelSys = player.GetComponent<LevelSystem>();

            // === 经验 ===
            if (levelSys != null && expReward > 0)
            {
                int exp = expReward + Random.Range(-3, 5);
                levelSys.GainExp(Mathf.Max(1, exp));
            }

            // === 金币 ===
            // 金币即时入账；地面铜钱只是短命纯视觉反馈，无碰撞、无拾取组件（复审四轮 Spec-P2）。
            int gold = Random.Range(minGold, maxGold + 1);
            if (gold > 0)
            {
                var inventory = InventoryManager.Instance;
                if (inventory != null) inventory.AddGold(gold);
                SpawnGoldFeedback(transform.position);
            }

            // === 物品掉落 ===
            foreach (var entry in lootItems)
            {
                bool shouldDrop = entry.guaranteed || Random.Range(0f, 1f) <= entry.dropChance;
                if (!shouldDrop) continue;

                int amount = Random.Range(entry.minAmount, entry.maxAmount + 1);

                // 在附近随机位置生成掉落物
                Vector2 dropPos = (Vector2)transform.position +
                    Random.insideUnitCircle * dropSpread;
                SpawnItemDrop(dropPos, entry.itemId, amount);
            }

            string enemyName = _stats != null ? _stats.characterName : "敌人";
            Debug.Log($"[掉落] {enemyName} 击杀奖励: {gold}文, {expReward}经验");
        }

        internal void SpawnGoldFeedback(Vector2 position)
        {
            // 金币已在 OnEnemyDeath 直接入账，这里只弹出一枚短命铜钱作反馈；
            // 无碰撞体、无 ItemPickup，不留拾取不了的假掉落（复审四轮 Spec-P2）。
            var sprite = Art.MvpArtCatalog.Load("loot_gold");
            if (sprite == null) return;

            var coin = new GameObject("Loot_Gold_Feedback");
            coin.transform.position = position;

            var sr = coin.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Foreground";
            sr.sortingOrder = 45;
            sr.sprite = sprite;
            coin.AddComponent<GoldFeedbackSprite>();
        }

        internal void SpawnItemDrop(Vector2 position, string itemId, int amount)
        {
            // 物品掉落必须使用持久精灵（Resources/Art/MVP，复审三轮 P1）：
            // loot_item 按物品类型染色；禁止运行时 Texture2D/Sprite.Create。
            var sprite = Art.MvpArtCatalog.Load("loot_item");
            if (sprite == null) return;

            // 创建掉落物
            var dropObj = new GameObject($"Loot_{itemId}");
            dropObj.transform.position = position;

            // 精灵渲染器
            var sr = dropObj.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Foreground";
            sr.sortingOrder = 45;
            sr.sprite = sprite;
            sr.color = GetItemTint(itemId);

            // 碰撞体
            var col = dropObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.8f, 0.8f);

            // 掉落物组件：必须携带物品 ID 与数量，否则永远无法拾取（复审三轮 P1）。
            var pickup = dropObj.AddComponent<ItemPickup>();
            pickup.itemId = itemId;
            pickup.amount = Mathf.Max(1, amount);

            // 弹出动画
            dropObj.AddComponent<Rigidbody2D>().gravityScale = 0f;
            if (Application.isPlaying)
            {
                StartCoroutine(PopAnimation(dropObj, position));
                Destroy(dropObj, dropDuration);
            }
        }

        /// <summary>物品掉落按类型染色的色板（只改颜色，不再运行时生成贴图）。</summary>
        private static Color GetItemTint(string itemId)
        {
            var itemData = ItemDatabase.Get(itemId);
            if (itemData == null) return Color.white;
            return itemData.type switch
            {
                ItemType.Consumable => new Color(0.55f, 0.95f, 0.55f),
                ItemType.Weapon => new Color(0.78f, 0.85f, 1f),
                ItemType.Armor => new Color(0.9f, 0.82f, 0.68f),
                ItemType.Material => new Color(0.85f, 0.85f, 0.85f),
                ItemType.SkillBook => new Color(0.95f, 0.75f, 1f),
                _ => Color.white
            };
        }

        private IEnumerator PopAnimation(GameObject obj, Vector2 targetPos)
        {
            // 从当前位置弹出然后落下
            Vector2 startPos = targetPos + Vector2.up * 1.5f;
            float duration = 0.3f;
            float t = 0f;

            while (t < duration)
            {
                t += Time.deltaTime;
                float progress = t / duration;
                // 抛物线
                float height = Mathf.Sin(progress * Mathf.PI) * 1.5f;
                obj.transform.position = targetPos + Vector2.up * height;
                yield return null;
            }

            obj.transform.position = targetPos;
        }
    }

    /// <summary>击杀金币入账后弹出的地面铜钱：纯视觉反馈，自带弹出+淡出并自毁；
    /// 无碰撞、无拾取组件，也不依赖敌人对象存活（复审四轮 Spec-P2）。</summary>
    public class GoldFeedbackSprite : MonoBehaviour
    {
        private const float PopSeconds = 0.3f;
        private const float FadeSeconds = 0.9f;

        private Vector2 _origin;
        private SpriteRenderer _renderer;
        private float _age;

        private void Awake()
        {
            _origin = transform.position;
            _renderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_age < PopSeconds)
            {
                float progress = _age / PopSeconds;
                transform.position = _origin + Vector2.up * (Mathf.Sin(progress * Mathf.PI) * 1.5f);
                return;
            }

            float fade = (_age - PopSeconds) / FadeSeconds;
            if (fade >= 1f || _renderer == null)
            {
                Destroy(gameObject);
                return;
            }

            var color = _renderer.color;
            color.a = 1f - fade;
            _renderer.color = color;
            transform.position = _origin + Vector2.up * (fade * 0.5f);
        }
    }

    // ========== 预置敌人掉落表 ==========

    /// <summary>
    /// Demo 敌人掉落配置（代码生成用）
    /// </summary>
    public static class EnemyLootPresets
    {
        public static LootEntry[] BanditLoot => new LootEntry[]
        {
            new LootEntry { itemId = "herb_medicinal", minAmount = 1, maxAmount = 2, dropChance = 0.3f },
            new LootEntry { itemId = "food_mantou", minAmount = 1, maxAmount = 3, dropChance = 0.5f },
            new LootEntry { itemId = "mat_iron", minAmount = 1, maxAmount = 1, dropChance = 0.15f },
        };

        public static LootEntry[] WolfLoot => new LootEntry[]
        {
            new LootEntry { itemId = "mat_wolf_fang", minAmount = 1, maxAmount = 2, dropChance = 0.4f },
            new LootEntry { itemId = "herb_medicinal", minAmount = 1, maxAmount = 1, dropChance = 0.2f },
        };

        public static LootEntry[] BossLoot => new LootEntry[]
        {
            new LootEntry { itemId = "pill_recovery", minAmount = 2, maxAmount = 3, dropChance = 0.8f },
            new LootEntry { itemId = "sword_iron", minAmount = 1, maxAmount = 1, dropChance = 0.3f, guaranteed = false },
            new LootEntry { itemId = "wine_zhuyeqing", minAmount = 1, maxAmount = 1, dropChance = 0.5f },
        };
    }
}
