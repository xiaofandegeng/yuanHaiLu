using UnityEngine;
using System;
using System.Collections.Generic;

namespace YuanHaiLu.GameSystem
{
    /// <summary>
    /// 物品数据定义
    /// </summary>
    [CreateAssetMenu(fileName = "NewItem", menuName = "渊海录/物品")]
    public class ItemData : ScriptableObject
    {
        public string itemId;           // 唯一ID，如 "herb_medicinal"
        public string itemName;         // 显示名称，如 "疗伤草"
        public string description;      // 描述
        public Sprite icon;             // 图标
        public ItemType type;           // 类型
        public ItemRarity rarity;       // 稀有度
        public int maxStack = 99;       // 最大堆叠
        public int buyPrice;            // 买入价格
        public int sellPrice;           // 卖出价格
        public bool usable;             // 是否可使用
        public bool equippable;         // 是否可装备
        public bool questItem;          // 是否任务物品（不可丢弃）

        // 使用效果
        public int healHp;              // 恢复气血
        public int healMp;              // 恢复内力
        public int healStamina;         // 恢复体力

        // 装备属性加成
        public int bonusAttack;
        public int bonusDefense;
        public int bonusAgility;
        public int bonusMaxHp;
        public int bonusMaxMp;

        // 装备槽位
        public EquipSlot equipSlot;

        // 武学秘籍
        public string teachSkillId;     // 学习后获得的武学ID

        // 任务相关
        public string questId;          // 关联任务ID
    }

    // 枚举提升到命名空间级别（避免嵌套引用问题）
    public enum ItemType
    {
        Consumable,     // 消耗品（药草、食物）
        Weapon,         // 武器
        Armor,          // 防具
        Accessory,      // 饰品
        SkillBook,      // 武学秘籍
        Material,       // 材料
        QuestItem,      // 任务物品
        Special         // 特殊物品
    }

    public enum ItemRarity
    {
        Common,         // 普通（白色）
        Uncommon,       // 优良（绿色）
        Rare,           // 稀有（蓝色）
        Epic,           // 史诗（紫色）
        Legendary       // 传说（金色）
    }

    // ItemData 扩展方法
    public static class ItemDataExtensions
    {
        public static Color GetRarityColor(this ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Common => Color.white,
                ItemRarity.Uncommon => new Color(0.3f, 1f, 0.3f),    // 绿
                ItemRarity.Rare => new Color(0.3f, 0.6f, 1f),         // 蓝
                ItemRarity.Epic => new Color(0.7f, 0.3f, 1f),         // 紫
                ItemRarity.Legendary => new Color(1f, 0.85f, 0.2f),   // 金
                _ => Color.white
            };
        }
    }

    /// <summary>
    /// 背包槽位
    /// </summary>
    [System.Serializable]
    public class InventorySlot
    {
        public string itemId;
        public ItemData itemData;
        public int amount;

        public bool IsEmpty => itemData == null || amount <= 0;

        public InventorySlot()
        {
            itemId = "";
            itemData = null;
            amount = 0;
        }
    }

    /// <summary>
    /// 背包管理器 — 物品存取、使用、装备
    /// 挂载到 GameManager 下
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        [Header("背包设置")]
        [SerializeField] private int maxSlots = 40;
        [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();

        [Header("装备栏")]
        [SerializeField] private string equippedWeaponId = "";
        [SerializeField] private string equippedArmorId = "";
        [SerializeField] private string equippedAccessoryId = "";

        [Header("金钱")]
        [SerializeField] private int gold = 100;

        // === 事件 ===
        public event System.Action OnInventoryChanged;
        public event System.Action<string, int> OnItemAdded;       // (itemId, amount)
        public event System.Action<string, int> OnItemRemoved;
        public event System.Action<string> OnItemUsed;
        public event System.Action<int> OnGoldChanged;

        // === 物品数据库 ===
        private Dictionary<string, ItemData> _itemDatabase = new Dictionary<string, ItemData>();

        public int Gold => gold;
        public int MaxSlots => maxSlots;
        public List<InventorySlot> Slots => slots;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // 初始化槽位
            for (int i = 0; i < maxSlots; i++)
            {
                slots.Add(new InventorySlot());
            }

            // 加载物品数据库
            LoadItemDatabase();
        }

        /// <summary>
        /// 从 Resources/Items 加载所有物品定义
        /// </summary>
        private void LoadItemDatabase()
        {
            ItemData[] items = Resources.LoadAll<ItemData>("Items");
            foreach (var item in items)
            {
                _itemDatabase[item.itemId] = item;
            }
            Debug.Log($"[Inventory] 物品数据库加载完成，共 {_itemDatabase.Count} 种物品");
        }

        /// <summary>
        /// 获取物品定义
        /// </summary>
        public ItemData GetItemData(string itemId)
        {
            return _itemDatabase.GetValueOrDefault(itemId);
        }

        // === 添加物品 ===
        public bool AddItem(string itemId, int amount = 1)
        {
            ItemData itemData = GetItemData(itemId);
            if (itemData == null)
            {
                Debug.LogWarning($"[Inventory] 物品不存在: {itemId}");
                return false;
            }

            // 先尝试堆叠到已有槽位
            if (itemData.maxStack > 1)
            {
                foreach (var slot in slots)
                {
                    if (slot.itemId == itemId && slot.amount < itemData.maxStack)
                    {
                        int canAdd = Mathf.Min(amount, itemData.maxStack - slot.amount);
                        slot.amount += canAdd;
                        amount -= canAdd;
                        if (amount <= 0)
                        {
                            OnItemAdded?.Invoke(itemId, slot.amount);
                            OnInventoryChanged?.Invoke();
                            return true;
                        }
                    }
                }
            }

            // 放入新槽位
            while (amount > 0)
            {
                int emptyIndex = FindEmptySlot();
                if (emptyIndex == -1)
                {
                    Debug.LogWarning("[Inventory] 背包已满！");
                    OnInventoryChanged?.Invoke();
                    return false;
                }

                int addAmount = Mathf.Min(amount, itemData.maxStack);
                slots[emptyIndex].itemId = itemId;
                slots[emptyIndex].itemData = itemData;
                slots[emptyIndex].amount = addAmount;
                amount -= addAmount;
            }

            OnItemAdded?.Invoke(itemId, amount);
            OnInventoryChanged?.Invoke();
            return true;
        }

        // === 移除物品 ===
        public bool RemoveItem(string itemId, int amount = 1)
        {
            int remaining = amount;

            for (int i = slots.Count - 1; i >= 0; i--)
            {
                if (slots[i].itemId == itemId)
                {
                    int remove = Mathf.Min(remaining, slots[i].amount);
                    slots[i].amount -= remove;
                    remaining -= remove;

                    if (slots[i].amount <= 0)
                    {
                        slots[i] = new InventorySlot();
                    }

                    if (remaining <= 0) break;
                }
            }

            if (remaining > 0)
            {
                Debug.LogWarning($"[Inventory] 物品不足: {itemId}，缺少 {remaining}");
                return false;
            }

            OnItemRemoved?.Invoke(itemId, amount);
            OnInventoryChanged?.Invoke();
            return true;
        }

        // === 使用物品 ===
        public void UseItem(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count) return;
            var slot = slots[slotIndex];
            if (slot.IsEmpty) return;

            var item = slot.itemData;

            if (item.type == ItemType.SkillBook && !string.IsNullOrEmpty(item.teachSkillId))
            {
                // 学习武学
                LearnSkill(item);
                RemoveItem(slot.itemId, 1);
                return;
            }

            if (item.usable)
            {
                // 使用消耗品
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    var stats = player.GetComponent<Character.CharacterStats>();
                    if (stats != null)
                    {
                        if (item.healHp > 0) stats.Heal(item.healHp);
                        if (item.healMp > 0) stats.RestoreMp(item.healMp);
                    }
                }

                if (!item.questItem)
                {
                    RemoveItem(slot.itemId, 1);
                }

                OnItemUsed?.Invoke(slot.itemId);
                Debug.Log($"[Inventory] 使用了 {item.itemName}");
            }
        }

        // === 装备 ===
        public void EquipItem(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count) return;
            var slot = slots[slotIndex];
            if (slot.IsEmpty || !slot.itemData.equippable) return;

            var item = slot.itemData;

            // 先卸下当前装备
            switch (item.type)
            {
                case ItemType.Weapon:
                    if (!string.IsNullOrEmpty(equippedWeaponId))
                        AddItem(equippedWeaponId);
                    equippedWeaponId = slot.itemId;
                    break;
                case ItemType.Armor:
                    if (!string.IsNullOrEmpty(equippedArmorId))
                        AddItem(equippedArmorId);
                    equippedArmorId = slot.itemId;
                    break;
                case ItemType.Accessory:
                    if (!string.IsNullOrEmpty(equippedAccessoryId))
                        AddItem(equippedAccessoryId);
                    equippedAccessoryId = slot.itemId;
                    break;
            }

            // 从背包移除
            RemoveItem(slot.itemId, 1);

            // 应用属性加成
            ApplyEquipmentStats();

            Debug.Log($"[Inventory] 装备了 {item.itemName}");
            OnInventoryChanged?.Invoke();
        }

        public void UnequipWeapon()
        {
            if (string.IsNullOrEmpty(equippedWeaponId)) return;
            AddItem(equippedWeaponId);
            equippedWeaponId = "";
            ApplyEquipmentStats();
            OnInventoryChanged?.Invoke();
        }

        private void ApplyEquipmentStats()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
            var stats = player.GetComponent<Character.CharacterStats>();
            if (stats == null) return;

            // 累加三件已装备物品的属性加成
            int atk = 0, def = 0, agi = 0, hp = 0, mp = 0;
            foreach (string id in new[] { equippedWeaponId, equippedArmorId, equippedAccessoryId })
            {
                if (string.IsNullOrEmpty(id)) continue;
                var item = GetItemData(id);
                if (item == null) continue;
                atk += item.bonusAttack;
                def += item.bonusDefense;
                agi += item.bonusAgility;
                hp += item.bonusMaxHp;
                mp += item.bonusMaxMp;
            }

            stats.SetEquipmentBonus(atk, def, agi, hp, mp);
        }

        // === 学习武学 ===
        private void LearnSkill(ItemData book)
        {
            if (string.IsNullOrEmpty(book.teachSkillId))
            {
                Debug.LogWarning($"[Inventory] 秘籍 {book.itemName} 未配置 teachSkillId");
                return;
            }

            // 从武学数据库取招式定义
            var skill = MartialSkillDatabase.Get(book.teachSkillId);
            if (skill == null)
            {
                Debug.LogWarning($"[Inventory] 武学数据库中找不到: {book.teachSkillId}");
                return;
            }

            // 交给 MartialArtsSystem 学习（内部含"已学"检查 + 自动装备到空槽）
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
            var martial = player.GetComponent<Character.MartialArtsSystem>();
            if (martial == null)
            {
                Debug.LogWarning("[Inventory] 玩家身上没有 MartialArtsSystem 组件");
                return;
            }

            martial.LearnSkill(skill);
        }

        // === 金钱 ===
        public void AddGold(int amount)
        {
            gold += amount;
            OnGoldChanged?.Invoke(gold);
        }

        public bool SpendGold(int amount)
        {
            if (gold < amount) return false;
            gold -= amount;
            OnGoldChanged?.Invoke(gold);
            return true;
        }

        // === 查询 ===
        public int GetItemCount(string itemId)
        {
            int count = 0;
            foreach (var slot in slots)
            {
                if (slot.itemId == itemId) count += slot.amount;
            }
            return count;
        }

        public bool HasItem(string itemId, int amount = 1)
        {
            return GetItemCount(itemId) >= amount;
        }

        private int FindEmptySlot()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].IsEmpty) return i;
            }
            return -1;
        }

        // === 存档支持 ===
        [System.Serializable]
        public class InventorySaveData
        {
            public string[] slotItemIds;
            public int[] slotAmounts;
            public string equippedWeapon;
            public string equippedArmor;
            public string equippedAccessory;
            public int gold;
        }

        public InventorySaveData GetSaveData()
        {
            var data = new InventorySaveData
            {
                slotItemIds = new string[maxSlots],
                slotAmounts = new int[maxSlots],
                equippedWeapon = equippedWeaponId,
                equippedArmor = equippedArmorId,
                equippedAccessory = equippedAccessoryId,
                gold = gold
            };

            for (int i = 0; i < maxSlots; i++)
            {
                data.slotItemIds[i] = slots[i].itemId;
                data.slotAmounts[i] = slots[i].amount;
            }

            return data;
        }

        public void LoadSaveData(InventorySaveData data)
        {
            for (int i = 0; i < maxSlots && i < data.slotItemIds.Length; i++)
            {
                slots[i].itemId = data.slotItemIds[i];
                slots[i].amount = data.slotAmounts[i];
                slots[i].itemData = GetItemData(data.slotItemIds[i]);
            }
            equippedWeaponId = data.equippedWeapon;
            equippedArmorId = data.equippedArmor;
            equippedAccessoryId = data.equippedAccessory;
            gold = data.gold;
            OnInventoryChanged?.Invoke();
        }
    }
}
