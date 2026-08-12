using UnityEngine;
using UnityEngine.UI;
using YuanHaiLu.Character;
using YuanHaiLu.GameSystem;

namespace YuanHaiLu.UI
{
    /// <summary>
    /// HUD v2 — 完整游戏主界面
    /// HP/MP条、经验条、技能快捷栏、金币、等级
    /// 自动创建Canvas和所有UI元素
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class HUD : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private CharacterStats playerStats;

        // UI元素
        private Image _hpBarFill;
        private Image _mpBarFill;
        private Image _expBarFill;
        private Text _hpText;
        private Text _mpText;
        private Text _levelText;
        private Text _goldText;
        private Image[] _skillSlots = new Image[4];
        private Text[] _skillKeyLabels = new Text[4];
        private Image[] _skillCooldowns = new Image[4];
        private GameObject _interactPrompt;
        private Text _interactText;
        private GameObject _levelUpBanner;
        private Text _levelUpText;

        // 技能图标颜色（临时用颜色代替精灵）
        private Color[] _skillSlotColors = new Color[4]
        {
            Color.clear, Color.clear, Color.clear, Color.clear
        };

        private void Awake()
        {
            BuildUI();
        }

        private void Start()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null && playerStats == null)
            {
                playerStats = player.GetComponent<CharacterStats>();
            }

            if (playerStats != null)
            {
                playerStats.OnHpChanged += UpdateHP;
                playerStats.OnHealed += (_) => UpdateHP(playerStats.currentHp, playerStats.maxHp);
                playerStats.OnDamaged += (_) => UpdateHP(playerStats.currentHp, playerStats.maxHp);
                playerStats.OnLevelUp += OnLevelUp;
            }

            // 监听武学系统
            var martialSys = player?.GetComponent<MartialArtsSystem>();
            if (martialSys != null)
            {
                martialSys.OnSkillEquipped += OnSkillEquipped;
            }

            // 监听升级系统
            var levelSys = player?.GetComponent<LevelSystem>();
            if (levelSys != null)
            {
                levelSys.OnExpGained += UpdateExp;
            }

            UpdateHP(playerStats?.currentHp ?? 100, playerStats?.maxHp ?? 100);
            UpdateLevel(playerStats?.level ?? 1);
        }

        // === 构建UI ===

        private void BuildUI()
        {
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 400;
            if (GetComponent<CanvasScaler>() == null)
                gameObject.AddComponent<CanvasScaler>();

            // --- 左上角：HP/MP/EXP 条 ---
            var barsPanel = CreatePanel("Bars", transform,
                new Vector2(0f, 0.7f), new Vector2(0.22f, 1f),
                new Vector2(8, -8), new Vector2(-8, -50));

            // HP条
            CreateLabel("HP_Label", barsPanel.transform,
                new Vector2(0f, 0.72f), new Vector2(0.2f, 1f),
                "气血", 12, new Color(1f, 0.4f, 0.4f));

            _hpBarFill = CreateBar("HP_Bar", barsPanel.transform,
                new Vector2(0.22f, 0.72f), new Vector2(1f, 1f),
                new Color(0.8f, 0.15f, 0.15f));

            _hpText = CreateLabel("HP_Text", barsPanel.transform,
                new Vector2(0.22f, 0.72f), new Vector2(1f, 1f),
                "", 11, Color.white, TextAnchor.MiddleCenter);

            // MP条
            CreateLabel("MP_Label", barsPanel.transform,
                new Vector2(0f, 0.38f), new Vector2(0.2f, 0.68f),
                "内力", 12, new Color(0.4f, 0.6f, 1f));

            _mpBarFill = CreateBar("MP_Bar", barsPanel.transform,
                new Vector2(0.22f, 0.38f), new Vector2(1f, 0.68f),
                new Color(0.1f, 0.25f, 0.75f));

            _mpText = CreateLabel("MP_Text", barsPanel.transform,
                new Vector2(0.22f, 0.38f), new Vector2(1f, 0.68f),
                "", 11, Color.white, TextAnchor.MiddleCenter);

            // 经验条（细条）
            _expBarFill = CreateBar("EXP_Bar", barsPanel.transform,
                new Vector2(0f, 0.05f), new Vector2(1f, 0.3f),
                new Color(0.6f, 0.4f, 0.9f), height: 6);

            // 等级
            _levelText = CreateLabel("Level", barsPanel.transform,
                new Vector2(0f, 0.3f), new Vector2(0.35f, 0.6f),
                "Lv.1", 12, new Color(1f, 0.85f, 0.3f));

            // --- 左下角：技能快捷栏 ---
            var skillPanel = CreatePanel("SkillBar", transform,
                new Vector2(0.3f, 0f), new Vector2(0.7f, 0f),
                new Vector2(0, 8), new Vector2(0, 48));

            // 背景
            var spBg = skillPanel.AddComponent<Image>();
            spBg.color = new Color(0.05f, 0.05f, 0.1f, 0.75f);

            for (int i = 0; i < 4; i++)
            {
                float left = i * 0.25f;
                float right = (i + 1) * 0.25f;

                var slot = CreatePanel($"Skill_{i}", skillPanel.transform,
                    new Vector2(left, 0f), new Vector2(right, 1f),
                    new Vector2(3, 3), new Vector2(-3, -3));

                var slotBg = slot.AddComponent<Image>();
                slotBg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

                _skillSlots[i] = slotBg;

                // 技能图标（用颜色方块代替）
                var iconObj = CreatePanel($"SkillIcon_{i}", slot.transform,
                    new Vector2(0.15f, 0.1f), new Vector2(0.85f, 0.9f),
                    Vector2.zero, Vector2.zero);
                var iconImg = iconObj.AddComponent<Image>();
                iconImg.color = Color.clear;

                // 冷却遮罩
                var cdObj = CreatePanel($"CD_{i}", slot.transform,
                    new Vector2(0.15f, 0.1f), new Vector2(0.85f, 0.9f),
                    Vector2.zero, Vector2.zero);
                var cdImg = cdObj.AddComponent<Image>();
                cdImg.color = new Color(0, 0, 0, 0.5f);
                cdImg.fillMethod = Image.FillMethod.Radial360;
                cdImg.fillAmount = 0f;
                _skillCooldowns[i] = cdImg;

                // 快捷键标签
                var keyLabel = CreateLabel($"Key_{i}", slot.transform,
                    new Vector2(0f, 0.6f), new Vector2(0.4f, 1f),
                    (i + 1).ToString(), 10, new Color(0.6f, 0.6f, 0.6f));
                _skillKeyLabels[i] = keyLabel;
            }

            // --- 右上角：金币 ---
            var goldPanel = CreatePanel("Gold", transform,
                new Vector2(0.78f, 0.92f), new Vector2(0.98f, 1f),
                Vector2.zero, new Vector2(-8, -8));
            goldPanel.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.7f);

            var goldIcon = CreateLabel("GoldIcon", goldPanel.transform,
                new Vector2(0f, 0f), new Vector2(0.3f, 1f),
                "💰", 14, Color.white);
            _goldText = CreateLabel("GoldAmount", goldPanel.transform,
                new Vector2(0.3f, 0f), new Vector2(1f, 1f),
                "0", 13, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleLeft);

            // --- 交互提示（中央底部） ---
            _interactPrompt = CreatePanel("InteractPrompt", transform,
                new Vector2(0.35f, 0.08f), new Vector2(0.65f, 0.13f),
                Vector2.zero, Vector2.zero);
            _interactPrompt.AddComponent<Image>().color = new Color(0, 0, 0, 0.6f);
            _interactText = CreateLabel("InteractText", _interactPrompt.transform,
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                "[K] 交互", 14, Color.white, TextAnchor.MiddleCenter);
            _interactPrompt.SetActive(false);

            // --- 升级横幅 ---
            _levelUpBanner = CreatePanel("LevelUpBanner", transform,
                new Vector2(0.2f, 0.55f), new Vector2(0.8f, 0.65f),
                Vector2.zero, Vector2.zero);
            _levelUpBanner.AddComponent<Image>().color = new Color(0.1f, 0.05f, 0.2f, 0.85f);
            _levelUpText = CreateLabel("LevelUpText", _levelUpBanner.transform,
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                "等级提升！", 20, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleCenter);
            _levelUpBanner.SetActive(false);
        }

        // === 更新方法 ===

        public void UpdateHP(int current, int max)
        {
            if (_hpBarFill != null) _hpBarFill.fillAmount = (float)current / max;
            if (_hpText != null) _hpText.text = $"{current}/{max}";
        }

        public void UpdateMP(int current, int max)
        {
            if (playerStats != null)
            {
                if (_mpBarFill != null) _mpBarFill.fillAmount = (float)playerStats.currentMp / playerStats.maxMp;
                if (_mpText != null) _mpText.text = $"{playerStats.currentMp}/{playerStats.maxMp}";
            }
        }

        public void UpdateExp(int current, int required)
        {
            if (_expBarFill != null) _expBarFill.fillAmount = (float)current / required;
        }

        public void UpdateLevel(int level)
        {
            if (_levelText != null) _levelText.text = $"Lv.{level}";
        }

        public void UpdateGold(int gold)
        {
            if (_goldText != null) _goldText.text = gold.ToString();
        }

        public void ShowInteractPrompt(string text = "[K] 交互")
        {
            if (_interactPrompt != null)
            {
                _interactPrompt.SetActive(true);
                if (_interactText != null) _interactText.text = text;
            }
        }

        public void HideInteractPrompt()
        {
            if (_interactPrompt != null) _interactPrompt.SetActive(false);
        }

        private void OnLevelUp(int newLevel)
        {
            UpdateLevel(newLevel);
            UpdateHP(playerStats.currentHp, playerStats.maxHp);
            StartCoroutine(ShowLevelUpBanner(newLevel));
        }

        private void OnSkillEquipped(MartialSkill skill, int slot)
        {
            if (slot >= 0 && slot < _skillSlots.Length)
            {
                _skillSlotColors[slot] = skill.elementColor;
                _skillSlots[slot].color = new Color(
                    skill.elementColor.r * 0.6f,
                    skill.elementColor.g * 0.6f,
                    skill.elementColor.b * 0.6f,
                    0.8f
                );
            }
        }

        private System.Collections.IEnumerator ShowLevelUpBanner(int level)
        {
            if (_levelUpBanner == null) yield break;
            _levelUpBanner.SetActive(true);
            _levelUpText.text = $"✦ 等级提升！Lv.{level} ✦";
            yield return new WaitForSecondsRealtime(2.5f);
            _levelUpBanner.SetActive(false);
        }

        private void LateUpdate()
        {
            // 更新金币
            var inv = InventoryManager.Instance;
            if (inv != null && _goldText != null)
            {
                _goldText.text = inv.Gold.ToString();
            }

            // 更新MP
            if (playerStats != null)
            {
                UpdateMP(playerStats.currentMp, playerStats.maxMp);
            }

            // 更新技能冷却
            var martial = playerStats?.GetComponent<MartialArtsSystem>();
            if (martial != null)
            {
                var equipped = martial.EquippedSkills;
                for (int i = 0; i < _skillCooldowns.Length && i < equipped.Length; i++)
                {
                    if (equipped[i] != null && _skillCooldowns[i] != null)
                    {
                        // 简化：无法直接获取冷却剩余，设为0
                        _skillCooldowns[i].fillAmount = 0f;
                    }
                }
            }
        }

        // === UI构建辅助 ===

        private static GameObject CreatePanel(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return obj;
        }

        private static Image CreateBar(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Color fillColor, int height = 0)
        {
            // 背景
            var bgObj = CreatePanel(name + "_Bg", parent, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.1f, 0.12f, 0.8f);

            // 填充
            var fillObj = CreatePanel(name + "_Fill", bgObj.transform,
                new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            var fillImg = fillObj.AddComponent<Image>();
            fillImg.color = fillColor;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 1f;

            return fillImg;
        }

        private static Text CreateLabel(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax,
            string text, int fontSize, Color color,
            TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var obj = CreatePanel(name, parent, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            var txt = obj.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.color = color;
            txt.text = text;
            txt.alignment = alignment;
            return txt;
        }
    }
}
