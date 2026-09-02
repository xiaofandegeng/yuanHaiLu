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
        private int _initialGold;

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
            _initialGold = gold;

            // 初始化槽位
            EnsureSlotCount();

            // 加载物品数据库
            LoadItemDatabase();
        }

        /// <summary>
        /// 从 Resources/Items 加载所有物品定义
        /// </summary>
        private void LoadItemDatabase()
        {
            _itemDatabase.Clear();

            // Demo 代码数据库是默认数据源，正式 Resources 资源可覆盖同 ID。
            foreach (var pair in ItemDatabase.AllItems)
                _itemDatabase[pair.Key] = pair.Value;

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
            if (amount <= 0) return false;

            ItemData itemData = GetItemData(itemId);
            if (itemData == null)
            {
                Debug.LogWarning($"[Inventory] 物品不存在: {itemId}");
                return false;
            }

            int requestedAmount = amount;
            int availableCapacity = 0;
            foreach (InventorySlot slot in slots)
            {
                if (slot.IsEmpty)
                {
                    availableCapacity += itemData.maxStack;
                }
                else if (slot.itemId == itemId && itemData.maxStack > 1)
                {
                    availableCapacity += Mathf.Max(0, itemData.maxStack - slot.amount);
                }
            }

            if (availableCapacity < requestedAmount)
            {
                Debug.LogWarning("[Inventory] 背包空间不足！");
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
                            OnItemAdded?.Invoke(itemId, requestedAmount);
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
                    Debug.LogWarning("[Inventory] 背包空间不足！");
                    OnInventoryChanged?.Invoke();
                    return false;
                }

                int addAmount = Mathf.Min(amount, itemData.maxStack);
                slots[emptyIndex].itemId = itemId;
                slots[emptyIndex].itemData = itemData;
                slots[emptyIndex].amount = addAmount;
                amount -= addAmount;
            }

            OnItemAdded?.Invoke(itemId, requestedAmount);
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

        private void ApplyEquipmentStats(bool adjustCurrentResources = true)
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

            stats.SetEquipmentBonus(atk, def, agi, hp, mp, adjustCurrentResources);
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
            if (data == null) return;

            ResetSlots();

            int itemIdCount = data.slotItemIds?.Length ?? 0;
            int amountCount = data.slotAmounts?.Length ?? 0;
            int savedSlotCount = Mathf.Min(maxSlots, Mathf.Min(itemIdCount, amountCount));

            for (int i = 0; i < savedSlotCount; i++)
            {
                string itemId = data.slotItemIds[i];
                int amount = data.slotAmounts[i];
                if (string.IsNullOrEmpty(itemId) || amount <= 0) continue;

                ItemData itemData = GetItemData(itemId);
                if (itemData == null)
                {
                    Debug.LogWarning($"[Inventory] 存档中的物品不存在，已跳过: {itemId}");
                    continue;
                }

                slots[i].itemId = itemId;
                slots[i].amount = amount;
                slots[i].itemData = itemData;
            }

            equippedWeaponId = NormalizeEquipmentId(data.equippedWeapon);
            equippedArmorId = NormalizeEquipmentId(data.equippedArmor);
            equippedAccessoryId = NormalizeEquipmentId(data.equippedAccessory);
            gold = data.gold;

            ApplyEquipmentStats(false);
            OnGoldChanged?.Invoke(gold);
            OnInventoryChanged?.Invoke();
        }

        public void ResetForNewGame()
        {
            ResetSlots();
            equippedWeaponId = "";
            equippedArmorId = "";
            equippedAccessoryId = "";
            gold = _initialGold;

            ApplyEquipmentStats(false);
            OnGoldChanged?.Invoke(gold);
            OnInventoryChanged?.Invoke();
        }

        private void EnsureSlotCount()
        {
            if (slots == null)
                slots = new List<InventorySlot>();

            while (slots.Count < maxSlots)
                slots.Add(new InventorySlot());

            if (slots.Count > maxSlots)
                slots.RemoveRange(maxSlots, slots.Count - maxSlots);
        }

        private void ResetSlots()
        {
            slots = new List<InventorySlot>(maxSlots);
            for (int i = 0; i < maxSlots; i++)
                slots.Add(new InventorySlot());
        }

        private string NormalizeEquipmentId(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return "";
            if (GetItemData(itemId) != null) return itemId;

            Debug.LogWarning($"[Inventory] 存档中的装备不存在，已跳过: {itemId}");
            return "";
        }
    }
}
