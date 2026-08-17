using UnityEngine;
using System;
using System.Collections.Generic;

namespace YuanHaiLu.GameSystem
{
    // ========== 装备槽位（补充） ==========

    public enum EquipSlot
    {
        Weapon,
        Head,
        Body,
        Legs,
        Accessory
    }

    // ========== 预置物品数据库 ==========

    /// <summary>
    /// 代码生成预置物品（用于Demo，无需ScriptableObject文件）
    /// </summary>
    public static class ItemDatabase
    {
        private static Dictionary<string, ItemData> _items;

        public static Dictionary<string, ItemData> AllItems
        {
            get
            {
                if (_items == null) BuildDatabase();
                return _items;
            }
        }

        /// <summary>显式触发代码表构建（场景生成与测试入口使用，避免以废弃局部变量触发 getter）。</summary>
        public static void Initialize()
        {
            var _ = AllItems;
        }

        public static ItemData Get(string id)
        {
            return AllItems.TryGetValue(id, out var item) ? item : null;
        }

        private static void BuildDatabase()
        {
            _items = new Dictionary<string, ItemData>();

            // === 消耗品 ===
            Add("herb_medicinal", "草药", "路旁常见的草药，可恢复少量气血", ItemType.Consumable,
                healHp: 20, buyPrice: 5, sellPrice: 2);
            Add("herb_spirit", "灵草", "蕴含微弱灵气的草药，可恢复少量内力", ItemType.Consumable,
                healMp: 15, buyPrice: 8, sellPrice: 4);
            Add("pill_recovery", "回气丹", "药师炼制的丹药，恢复中量气血和内力", ItemType.Consumable,
                healHp: 50, healMp: 30, buyPrice: 25, sellPrice: 12, rarity: ItemRarity.Uncommon);
            Add("food_mantou", "馒头", "热腾腾的馒头，聊胜于无", ItemType.Consumable,
                healHp: 8, buyPrice: 2, sellPrice: 1);
            Add("wine_zhuyeqing", "竹叶青", "清冽的美酒，壮胆增力", ItemType.Consumable,
                healHp: 10, bonusAttack: 3, buyPrice: 15, sellPrice: 7, rarity: ItemRarity.Uncommon);

            // === 武器 ===
            Add("sword_iron", "铁剑", "普通的铁剑，锋利可靠", ItemType.Weapon,
                bonusAttack: 5, buyPrice: 50, sellPrice: 25, equippable: true, equipSlot: EquipSlot.Weapon);
            Add("sword_greensteel", "碧钢剑", "以碧钢锻造的长剑，寒光闪闪", ItemType.Weapon,
                bonusAttack: 12, bonusAgility: 2, buyPrice: 180, sellPrice: 90,
                equippable: true, equipSlot: EquipSlot.Weapon, rarity: ItemRarity.Rare);
            Add("sword_frost", "霜华剑", "剑身凝结冰霜的宝剑，传为北冥派至宝", ItemType.Weapon,
                bonusAttack: 22, bonusAgility: 5, bonusMp: 20, buyPrice: 500, sellPrice: 250,
                equippable: true, equipSlot: EquipSlot.Weapon, rarity: ItemRarity.Epic);

            // === 防具 ===
            Add("armor_cloth", "布衣", "普通的布制衣裳", ItemType.Armor,
                bonusDefense: 2, buyPrice: 20, sellPrice: 10, equippable: true, equipSlot: EquipSlot.Body);
            Add("armor_leather", "皮甲", "以牛皮制成的轻甲，兼顾防御和灵活", ItemType.Armor,
                bonusDefense: 6, bonusAgility: 1, buyPrice: 80, sellPrice: 40,
                equippable: true, equipSlot: EquipSlot.Body, rarity: ItemRarity.Uncommon);
            Add("armor_silk", "天蚕丝甲", "以天蚕丝编织的宝甲，刀枪不入", ItemType.Armor,
                bonusDefense: 14, bonusAgility: 3, bonusHp: 30, buyPrice: 400, sellPrice: 200,
                equippable: true, equipSlot: EquipSlot.Body, rarity: ItemRarity.Epic);

            // === 材料 ===
            Add("mat_iron", "铁矿石", "普通的铁矿石，可打造兵器", ItemType.Material,
                buyPrice: 5, sellPrice: 3);
            Add("mat_jade", "玉佩碎片", "刻有神秘铭文的玉佩碎片", ItemType.QuestItem,
                questItem: true, questId: "q_jade_pendant");
            Add("quest_lost_pouch", "掌柜的荷包", "老赵落在河岸的荷包，装着客栈的全部账银", ItemType.QuestItem,
                questItem: true, questId: "MVP_01");
            Add("mat_wolf_fang", "狼牙", "山中恶狼的獠牙", ItemType.Material,
                buyPrice: 8, sellPrice: 4);

            // === 书籍/秘籍 ===
            Add("book_basic_sword", "基础剑法", "入门剑法心要", ItemType.SkillBook,
                buyPrice: 30, sellPrice: 15);
            Add("book_wind_step", "疾风步法", "轻功秘籍，习得后可突进", ItemType.SkillBook,
                buyPrice: 200, sellPrice: 100, rarity: ItemRarity.Rare);
        }

        private static void Add(string id, string name, string desc, ItemType type,
            int healHp = 0, int healMp = 0, int bonusAttack = 0, int bonusDefense = 0,
            int bonusAgility = 0, int bonusHp = 0, int bonusMp = 0,
            int buyPrice = 0, int sellPrice = 0,
            bool equippable = false, EquipSlot equipSlot = EquipSlot.Weapon,
            ItemRarity rarity = ItemRarity.Common, bool questItem = false, string questId = "")
        {
            var item = ScriptableObject.CreateInstance<ItemData>();
            item.itemId = id;
            item.itemName = name;
            item.description = desc;
            item.type = type;
            item.rarity = rarity;
            item.buyPrice = buyPrice;
            item.sellPrice = sellPrice;
            item.healHp = healHp;
            item.healMp = healMp;
            item.bonusAttack = bonusAttack;
            item.bonusDefense = bonusDefense;
            item.bonusAgility = bonusAgility;
            item.bonusMaxHp = bonusHp;
            item.bonusMaxMp = bonusMp;
            item.equippable = equippable;
            item.equipSlot = equipSlot;
            item.questItem = questItem;
            item.questId = questId;
            item.usable = type == ItemType.Consumable;
            item.maxStack = type == ItemType.Consumable ? 99 : 1;

            _items[id] = item;
        }
    }
}
