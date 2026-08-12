using UnityEngine;
using YuanHaiLu.Core;
using UnityEngine.UI;
using System.Collections.Generic;
using YuanHaiLu.GameSystem;

namespace YuanHaiLu.UI
{
    /// <summary>
    /// 背包界面 — 显示物品列表、装备栏、物品详情
    /// 挂载到 InventoryCanvas 下
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        [Header("面板")]
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private bool startClosed = true;

        [Header("槽位")]
        [SerializeField] private Transform slotContainer;
        [SerializeField] private GameObject slotPrefab;

        [Header("详情面板")]
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private Text itemNameText;
        [SerializeField] private Text itemDescText;
        [SerializeField] private Image itemIcon;
        [SerializeField] private Text itemStatsText;
        [SerializeField] private GameObject useButton;
        [SerializeField] private GameObject equipButton;
        [SerializeField] private GameObject dropButton;

        [Header("装备栏")]
        [SerializeField] private Image weaponSlot;
        [SerializeField] private Image armorSlot;
        [SerializeField] private Image accessorySlot;

        [Header("金钱")]
        [SerializeField] private Text goldText;

        [Header("分页")]
        [SerializeField] private int currentTab = 0; // 0=全部 1=消耗品 2=装备 3=材料 4=任务
        [SerializeField] private Text[] tabLabels;

        private InventorySlotUI[] _slotUIs;
        private int _selectedSlot = -1;
        private bool _isOpen = false;

        private void Start()
        {
            if (startClosed) Close();

            // 监听背包变化
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnInventoryChanged += Refresh;
                InventoryManager.Instance.OnGoldChanged += UpdateGold;
            }

            CreateSlots();
        }

        private void Update()
        {
            // Tab 键开关背包
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (_isOpen) Close();
                else Open();
            }

            // ESC 关闭
            if (Input.GetKeyDown(KeyCode.Escape) && _isOpen)
            {
                Close();
            }
        }

        private void CreateSlots()
        {
            if (slotContainer == null || slotPrefab == null) return;

            var inventory = InventoryManager.Instance;
            if (inventory == null) return;

            int totalSlots = inventory.MaxSlots;
            _slotUIs = new InventorySlotUI[totalSlots];

            for (int i = 0; i < totalSlots; i++)
            {
                GameObject slotObj = Instantiate(slotPrefab, slotContainer);
                var slotUI = slotObj.AddComponent<InventorySlotUI>();
                slotUI.Initialize(i, this);
                _slotUIs[i] = slotUI;
            }
        }

        public void Open()
        {
            _isOpen = true;
            inventoryPanel.SetActive(true);

            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameManager.GameState.Menu);

            Refresh();
        }

        public void Close()
        {
            _isOpen = false;
            inventoryPanel.SetActive(false);
            _selectedSlot = -1;

            if (detailPanel != null) detailPanel.SetActive(false);

            if (GameManager.Instance != null &&
                GameManager.Instance.currentState == GameManager.GameState.Menu)
            {
                GameManager.Instance.SetState(GameManager.GameState.Exploration);
            }
        }

        public void Refresh()
        {
            if (InventoryManager.Instance == null) return;

            var slots = InventoryManager.Instance.Slots;

            for (int i = 0; i < _slotUIs?.Length && i < slots.Count; i++)
            {
                _slotUIs[i].UpdateSlot(slots[i]);
            }

            UpdateGold(InventoryManager.Instance.Gold);
            UpdateEquipmentSlots();
        }

        public void SelectSlot(int index)
        {
            _selectedSlot = index;

            if (detailPanel != null) detailPanel.SetActive(true);

            var inventory = InventoryManager.Instance;
            if (inventory == null) return;

            var slot = inventory.Slots[index];
            if (slot.IsEmpty)
            {
                if (detailPanel != null) detailPanel.SetActive(false);
                return;
            }

            var item = slot.itemData;

            // 更新详情
            if (itemNameText != null)
            {
                itemNameText.text = item.itemName;
                itemNameText.color = item.rarity.GetRarityColor();
            }
            if (itemDescText != null)
                itemDescText.text = item.description;
            if (itemIcon != null && item.icon != null)
                itemIcon.sprite = item.icon;

            // 属性显示
            if (itemStatsText != null)
            {
                string stats = "";
                if (item.healHp > 0) stats += $"气血 +{item.healHp}\n";
                if (item.healMp > 0) stats += $"内力 +{item.healMp}\n";
                if (item.bonusAttack > 0) stats += $"攻击 +{item.bonusAttack}\n";
                if (item.bonusDefense > 0) stats += $"防御 +{item.bonusDefense}\n";
                if (item.bonusAgility > 0) stats += $"身法 +{item.bonusAgility}\n";
                if (item.sellPrice > 0) stats += $"\n售价: {item.sellPrice} 文";
                itemStatsText.text = stats;
            }

            // 按钮显示
            if (useButton != null) useButton.SetActive(item.usable);
            if (equipButton != null) equipButton.SetActive(item.equippable);
            if (dropButton != null) dropButton.SetActive(!item.questItem);
        }

        // === 按钮回调 ===

        public void OnUseButton()
        {
            if (_selectedSlot < 0) return;
            InventoryManager.Instance?.UseItem(_selectedSlot);
            Refresh();
        }

        public void OnEquipButton()
        {
            if (_selectedSlot < 0) return;
            InventoryManager.Instance?.EquipItem(_selectedSlot);
            Refresh();
        }

        public void OnDropButton()
        {
            if (_selectedSlot < 0) return;
            var inventory = InventoryManager.Instance;
            if (inventory == null) return;

            var slot = inventory.Slots[_selectedSlot];
            if (!slot.IsEmpty && !slot.itemData.questItem)
            {
                inventory.RemoveItem(slot.itemId, 1);
                Refresh();
            }
        }

        public void OnTabButton(int tab)
        {
            currentTab = tab;
            Refresh();
        }

        private void UpdateGold(int amount)
        {
            if (goldText != null)
                goldText.text = $"{amount} 文";
        }

        private void UpdateEquipmentSlots()
        {
            // TODO: 显示已装备物品的图标
        }

        private void OnDestroy()
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnInventoryChanged -= Refresh;
                InventoryManager.Instance.OnGoldChanged -= UpdateGold;
            }
        }
    }

    /// <summary>
    /// 背包槽位UI组件
    /// </summary>
    public class InventorySlotUI : MonoBehaviour
    {
        private int _index;
        private InventoryUI _parent;
        private Image _icon;
        private Text _amountText;
        private GameObject _highlight;

        public void Initialize(int index, InventoryUI parent)
        {
            _index = index;
            _parent = parent;

            _icon = transform.Find("Icon")?.GetComponent<Image>();
            _amountText = transform.Find("Amount")?.GetComponent<Text>();
            _highlight = transform.Find("Highlight")?.gameObject;

            var button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => _parent.SelectSlot(_index));
            }

            if (_highlight != null) _highlight.SetActive(false);
        }

        public void UpdateSlot(InventorySlot slot)
        {
            if (slot.IsEmpty)
            {
                if (_icon != null) _icon.enabled = false;
                if (_amountText != null) _amountText.text = "";
            }
            else
            {
                if (_icon != null && slot.itemData?.icon != null)
                {
                    _icon.sprite = slot.itemData.icon;
                    _icon.enabled = true;
                }
                if (_amountText != null)
                {
                    _amountText.text = slot.amount > 1 ? slot.amount.ToString() : "";
                }
            }
        }
    }
}
