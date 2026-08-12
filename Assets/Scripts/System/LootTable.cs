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
        public GameObject lootDropPrefab;    // 掉落物预制体（null则自动创建）
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

            var playerStats = player.GetComponent<CharacterStats>();
            var levelSys = player.GetComponent<LevelSystem>();

            // === 经验 ===
            if (levelSys != null && expReward > 0)
            {
                int exp = expReward + Random.Range(-3, 5);
                levelSys.GainExp(Mathf.Max(1, exp));
            }

            // === 金币 ===
            int gold = Random.Range(minGold, maxGold + 1);
            if (gold > 0)
            {
                SpawnLootDrop(transform.position, null, gold);
                InventoryManager.Instance?.AddGold(gold);
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
                SpawnLootDrop(dropPos, entry.itemId, 0);
            }

            Debug.Log($"[掉落] {_stats?.characterName ?? "敌人"} 击杀奖励: {gold}文, {expReward}经验");
        }

        private void SpawnLootDrop(Vector2 position, string itemId, int gold)
        {
            // 创建掉落物
            var dropObj = new GameObject(itemId != null ? $"Loot_{itemId}" : "Loot_Gold");
            dropObj.transform.position = position;

            // 精灵渲染器
            var sr = dropObj.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Foreground";
            sr.sortingOrder = 45;
            sr.sprite = CreateLootSprite(itemId, gold > 0);

            // 碰撞体
            var col = dropObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.8f, 0.8f);

            // 掉落物组件
            var pickup = dropObj.AddComponent<ItemPickup>();

            // 弹出动画
            dropObj.AddComponent<Rigidbody2D>().gravityScale = 0f;
            StartCoroutine(PopAnimation(dropObj, position));

            // 自动消失
            Destroy(dropObj, dropDuration);
        }

        private Sprite CreateLootSprite(string itemId, bool isGold)
        {
            var tex = new Texture2D(16, 16);
            tex.filterMode = FilterMode.Point;

            if (isGold)
            {
                // 金币：黄色圆形
                for (int x = 0; x < 16; x++)
                    for (int y = 0; y < 16; y++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), new Vector2(7.5f, 7.5f));
                        if (d < 6f)
                            tex.SetPixel(x, y, d < 4f ? new Color(1f, 0.85f, 0.1f) : new Color(0.9f, 0.7f, 0f));
                        else
                            tex.SetPixel(x, y, Color.clear);
                    }
            }
            else
            {
                // 物品：按类型配色
                Color itemColor = Color.white;
                var itemData = ItemDatabase.Get(itemId);
                if (itemData != null)
                {
                    itemColor = itemData.type switch
                    {
                        ItemType.Consumable => new Color(0.3f, 0.9f, 0.3f),
                        ItemType.Weapon => new Color(0.7f, 0.8f, 1f),
                        ItemType.Armor => new Color(0.8f, 0.7f, 0.5f),
                        ItemType.Material => new Color(0.7f, 0.7f, 0.7f),
                        ItemType.SkillBook => new Color(0.9f, 0.6f, 1f),
                        _ => Color.white
                    };
                }

                for (int x = 0; x < 16; x++)
                    for (int y = 0; y < 16; y++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), new Vector2(7.5f, 7.5f));
                        if (d < 6f)
                            tex.SetPixel(x, y, itemColor);
                        else
                            tex.SetPixel(x, y, Color.clear);
                    }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
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
