using UnityEngine;
using YuanHaiLu.Core;
using System;
using YuanHaiLu.Core;
using System.Collections.Generic;
using YuanHaiLu.Character;

namespace YuanHaiLu.GameSystem
{
    /// <summary>
    /// 商店系统 — 买卖物品
    /// 挂载到商店NPC上或独立管理
    /// </summary>
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }

        [Header("商店设置")]
        [SerializeField] private float buyPriceMultiplier = 1.0f;   // 买入倍率
        [SerializeField] private float sellPriceMultiplier = 0.5f;  // 卖出倍率（半价回收）

        [Header("当前商店")]
        [SerializeField] private string currentShopName = "";
        [SerializeField] private List<ShopItem> currentShopItems = new List<ShopItem>();

        // === 事件 ===
        public event System.Action<List<ShopItem>> OnShopOpened;
        public event System.Action OnShopClosed;
        public event System.Action<string, int> OnItemBought;   // (itemId, price)
        public event System.Action<string, int> OnItemSold;

        public bool IsOpen { get; private set; }
        public List<ShopItem> CurrentItems => currentShopItems;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// 打开商店
        /// </summary>
        public void OpenShop(string shopName, ShopItem[] items)
        {
            currentShopName = shopName;
            currentShopItems = new List<ShopItem>(items);
            IsOpen = true;

            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameManager.GameState.Menu);

            AudioManager.Instance?.PlaySFX(AudioManager.SFX.UI_OPEN);
            OnShopOpened?.Invoke(currentShopItems);

            Debug.Log($"[Shop] 打开商店: {shopName}，商品 {items.Length} 种");
        }

        /// <summary>
        /// 关闭商店
        /// </summary>
        public void CloseShop()
        {
            IsOpen = false;
            currentShopItems.Clear();

            if (GameManager.Instance != null &&
                GameManager.Instance.currentState == GameManager.GameState.Menu)
            {
                GameManager.Instance.SetState(GameManager.GameState.Exploration);
            }

            AudioManager.Instance?.PlaySFX(AudioManager.SFX.UI_CLOSE);
            OnShopClosed?.Invoke();
        }

        /// <summary>
        /// 购买物品
        /// </summary>
        public bool BuyItem(int shopIndex, int amount = 1)
        {
            if (!IsOpen || shopIndex < 0 || shopIndex >= currentShopItems.Count) return false;

            var shopItem = currentShopItems[shopIndex];
            if (shopItem.itemData == null) return false;

            int totalPrice = Mathf.RoundToInt(shopItem.itemData.buyPrice * buyPriceMultiplier) * amount;

            // 检查金钱
            var inventory = InventoryManager.Instance;
            if (inventory == null || !inventory.SpendGold(totalPrice))
            {
                Debug.Log("[Shop] 金钱不足！");
                AudioManager.Instance?.PlaySFX(AudioManager.SFX.UI_ERROR);
                return false;
            }

            // 检查库存
            if (shopItem.stock > 0 && shopItem.stock < amount)
            {
                Debug.Log("[Shop] 库存不足！");
                return false;
            }

            // 加入背包
            inventory.AddItem(shopItem.itemData.itemId, amount);

            // 减少库存
            if (shopItem.stock > 0)
                shopItem.stock -= amount;

            AudioManager.Instance?.PlaySFX(AudioManager.SFX.PICKUP_ITEM);
            OnItemBought?.Invoke(shopItem.itemData.itemId, totalPrice);

            Debug.Log($"[Shop] 购买 {shopItem.itemData.itemName} x{amount}，花费 {totalPrice} 文");
            return true;
        }

        /// <summary>
        /// 出售物品（背包槽位索引）
        /// </summary>
        public bool SellItem(int inventorySlotIndex, int amount = 1)
        {
            var inventory = InventoryManager.Instance;
            if (inventory == null) return false;

            var slot = inventory.Slots[inventorySlotIndex];
            if (slot.IsEmpty) return false;

            // 任务物品不可出售
            if (slot.itemData.questItem)
            {
                Debug.Log("[Shop] 任务物品不可出售！");
                return false;
            }

            int totalPrice = Mathf.RoundToInt(slot.itemData.sellPrice * sellPriceMultiplier) * amount;

            inventory.RemoveItem(slot.itemId, amount);
            inventory.AddGold(totalPrice);

            AudioManager.Instance?.PlaySFX(AudioManager.SFX.PICKUP_ITEM);
            OnItemSold?.Invoke(slot.itemId, totalPrice);

            Debug.Log($"[Shop] 出售 {slot.itemData.itemName} x{amount}，获得 {totalPrice} 文");
            return true;
        }

        /// <summary>
        /// 获取物品买入价
        /// </summary>
        public int GetBuyPrice(ItemData item)
        {
            return Mathf.RoundToInt(item.buyPrice * buyPriceMultiplier);
        }

        /// <summary>
        /// 获取物品卖出价
        /// </summary>
        public int GetSellPrice(ItemData item)
        {
            return Mathf.RoundToInt(item.sellPrice * sellPriceMultiplier);
        }
    }

    /// <summary>
    /// 商店物品条目
    /// </summary>
    [System.Serializable]
    public class ShopItem
    {
        public ItemData itemData;       // 物品数据
        public int stock = -1;          // 库存（-1=无限）
        public int requiredLevel;       // 等级要求
        public string requiredQuest;    // 前置任务

        public bool IsAvailable
        {
            get
            {
                if (stock == 0) return false;

                // 等级检查
                if (requiredLevel > 0)
                {
                    var player = GameObject.FindGameObjectWithTag("Player");
                    if (player != null)
                    {
                        var stats = player.GetComponent<CharacterStats>();
                        if (stats != null && stats.level < requiredLevel) return false;
                    }
                }

                return true;
            }
        }
    }

    // ===== 预设商店数据 =====

    /// <summary>
    /// 预设商店 — 通过代码快速创建商店
    /// </summary>
    public static class ShopPresets
    {
        public static ShopItem[] GetGeneralStore()
        {
            return new ShopItem[]
            {
                new ShopItem { itemData = null, stock = -1 }, // 需要在Unity中设置ItemData引用
                // 占位：实际在Unity编辑器中配置ItemData后填充
            };
        }

        public static ShopItem[] GetBlacksmith()
        {
            return new ShopItem[]
            {
                // 铁匠铺：武器 + 防具
            };
        }

        public static ShopItem[] GetPharmacy()
        {
            return new ShopItem[]
            {
                // 药铺：草药 + 丹药
            };
        }
    }
}
