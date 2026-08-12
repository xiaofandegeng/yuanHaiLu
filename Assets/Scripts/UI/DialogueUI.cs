using UnityEngine;
using UnityEngine.UI;
using YuanHaiLu.Core;
using YuanHaiLu.Dialogue;

namespace YuanHaiLu.UI
{
    /// <summary>
    /// 对话UI — 真正的屏幕对话框+选项面板
    /// 自动创建 Canvas，挂载到全局管理器下
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class DialogueUI : MonoBehaviour
    {
        [Header("样式")]
        [SerializeField] private Color speakerColor = new Color(1f, 0.85f, 0.3f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color choiceColor = new Color(0.7f, 0.9f, 1f);
        [SerializeField] private Color choiceHighlightColor = new Color(1f, 0.85f, 0.3f);
        [SerializeField] private int fontSize = 16;
        [SerializeField] private float boxHeight = 140f;

        // UI组件
        private Canvas _canvas;
        private GameObject _dialogueBox;
        private Text _speakerText;
        private Text _contentText;
        private GameObject _choicesPanel;
        private Text[] _choiceTexts;
        private Image _portraitFrame;

        // 打字机相关
        private string _displayedText = "";
        private int _choiceCount = 0;
        private int _selectedChoice = 0;

        private void Awake()
        {
            BuildUI();
            Hide();

            var dm = DialogueManager.Instance;
            if (dm != null)
            {
                dm.OnLineShown += OnLineShown;
                dm.OnChoicesPresented += OnChoicesPresented;
                dm.OnDialogueEnd += Hide;
                dm.OnDialogueStart += (s, t) => Show();
            }
        }

        private void OnDestroy()
        {
            var dm = DialogueManager.Instance;
            if (dm != null)
            {
                dm.OnLineShown -= OnLineShown;
                dm.OnChoicesPresented -= OnChoicesPresented;
                dm.OnDialogueEnd -= Hide;
            }
        }

        private void Update()
        {
            // 选择分支的上下键
            if (_choicesPanel != null && _choicesPanel.activeSelf && _choiceCount > 0)
            {
                if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
                {
                    _selectedChoice = (_selectedChoice - 1 + _choiceCount) % _choiceCount;
                    UpdateChoiceHighlight();
                }
                if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
                {
                    _selectedChoice = (_selectedChoice + 1) % _choiceCount;
                    UpdateChoiceHighlight();
                }
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.J))
                {
                    DialogueManager.Instance.SelectChoice(_selectedChoice);
                }
            }
        }

        // === 构建UI ===

        private void BuildUI()
        {
            _canvas = GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 500;

            if (GetComponent<CanvasScaler>() == null)
                gameObject.AddComponent<CanvasScaler>();

            // 对话框容器
            _dialogueBox = new GameObject("DialogueBox");
            _dialogueBox.transform.SetParent(transform, false);
            var boxRT = _dialogueBox.AddComponent<RectTransform>();
            boxRT.anchorMin = new Vector2(0.05f, 0f);
            boxRT.anchorMax = new Vector2(0.95f, 0f);
            boxRT.offsetMin = new Vector2(0, 10);
            boxRT.offsetMax = new Vector2(0, 10 + boxHeight);

            // 背景
            var bg = _dialogueBox.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.1f, 0.92f);
            var bgOutline = _dialogueBox.AddComponent<Outline>();
            bgOutline.effectColor = new Color(0.3f, 0.2f, 0.1f, 0.8f);
            bgOutline.effectDistance = new Vector2(2, 2);

            // 说话人名字
            var speakerObj = new GameObject("Speaker");
            speakerObj.transform.SetParent(_dialogueBox.transform, false);
            var spRT = speakerObj.AddComponent<RectTransform>();
            spRT.anchorMin = new Vector2(0f, 0.7f);
            spRT.anchorMax = new Vector2(0.3f, 1f);
            spRT.offsetMin = new Vector2(15, 0);
            spRT.offsetMax = new Vector2(0, -5);
            _speakerText = speakerObj.AddComponent<Text>();
            _speakerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _speakerText.fontSize = 14;
            _speakerText.color = speakerColor;
            _speakerText.alignment = TextAnchor.MiddleLeft;

            // 继续提示（右下角▼）
            var contObj = new GameObject("ContinueHint");
            contObj.transform.SetParent(_dialogueBox.transform, false);
            var contRT = contObj.AddComponent<RectTransform>();
            contRT.anchorMin = new Vector2(0.9f, 0f);
            contRT.anchorMax = new Vector2(1f, 0.2f);
            var contText = contObj.AddComponent<Text>();
            contText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            contText.fontSize = 14;
            contText.color = new Color(0.6f, 0.6f, 0.6f);
            contText.alignment = TextAnchor.MiddleRight;
            contText.text = "▼ K";
            // 闪烁动画
            contObj.AddComponent<ContinueHintBlink>();

            // 内容文本
            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(_dialogueBox.transform, false);
            var ctRT = contentObj.AddComponent<RectTransform>();
            ctRT.anchorMin = new Vector2(0f, 0.05f);
            ctRT.anchorMax = new Vector2(1f, 0.7f);
            ctRT.offsetMin = new Vector2(15, 0);
            ctRT.offsetMax = new Vector2(-15, -5);
            _contentText = contentObj.AddComponent<Text>();
            _contentText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _contentText.fontSize = fontSize;
            _contentText.color = textColor;
            _contentText.alignment = TextAnchor.UpperLeft;
            _contentText.lineSpacing = 1.3f;

            // 选择面板
            _choicesPanel = new GameObject("ChoicesPanel");
            _choicesPanel.transform.SetParent(_dialogueBox.transform, false);
            var cpRT = _choicesPanel.AddComponent<RectTransform>();
            cpRT.anchorMin = new Vector2(0.1f, 0.05f);
            cpRT.anchorMax = new Vector2(0.9f, 0.7f);
            cpRT.offsetMin = new Vector2(15, 0);
            cpRT.offsetMax = new Vector2(-15, -5);
            _choicesPanel.SetActive(false);
        }

        // === 显示/隐藏 ===

        public void Show()
        {
            _dialogueBox.SetActive(true);
        }

        public void Hide()
        {
            _dialogueBox.SetActive(false);
            _choicesPanel.SetActive(false);
        }

        // === 回调 ===

        private void OnLineShown(string text)
        {
            _displayedText = text;
            _contentText.text = text;

            // 解析说话人（如果格式是 "【名字】内容" 或 "名字：内容"）
            if (text.StartsWith("【"))
            {
                int end = text.IndexOf("】");
                if (end > 0)
                {
                    _speakerText.text = text.Substring(1, end - 1);
                    _contentText.text = text.Substring(end + 1);
                    return;
                }
            }

            // 默认：使用 DialogueManager 当前节点的 speaker
            _speakerText.text = ""; // 将在 OnDialogueStart 中设置
        }

        private void OnChoicesPresented(string[] choices)
        {
            // 清除旧选择
            foreach (Transform child in _choicesPanel.transform)
                Destroy(child.gameObject);

            _choiceCount = choices.Length;
            _selectedChoice = 0;
            _choiceTexts = new Text[choices.Length];

            float heightPerChoice = 1f / choices.Length;

            for (int i = 0; i < choices.Length; i++)
            {
                var choiceObj = new GameObject($"Choice_{i}");
                choiceObj.transform.SetParent(_choicesPanel.transform, false);

                var cRT = choiceObj.AddComponent<RectTransform>();
                cRT.anchorMin = new Vector2(0f, 1f - (i + 1) * heightPerChoice);
                cRT.anchorMax = new Vector2(1f, 1f - i * heightPerChoice);
                cRT.offsetMin = new Vector2(30, 2);
                cRT.offsetMax = new Vector2(0, -2);

                var cText = choiceObj.AddComponent<Text>();
                cText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                cText.fontSize = fontSize;
                cText.color = choiceColor;
                cText.alignment = TextAnchor.MiddleLeft;
                cText.text = $"  {i + 1}. {choices[i]}";

                _choiceTexts[i] = cText;
            }

            _choicesPanel.SetActive(true);
            _contentText.gameObject.SetActive(false);
            UpdateChoiceHighlight();
        }

        private void UpdateChoiceHighlight()
        {
            for (int i = 0; i < _choiceTexts.Length; i++)
            {
                if (_choiceTexts[i] != null)
                {
                    _choiceTexts[i].color = (i == _selectedChoice) ? choiceHighlightColor : choiceColor;
                    string prefix = (i == _selectedChoice) ? " ▶" : "  ";
                    // 保持原始文本但更换前缀
                    string originalText = _choiceTexts[i].text;
                    int dotIndex = originalText.IndexOf('.');
                    if (dotIndex > 0)
                    {
                        _choiceTexts[i].text = $"{prefix}{originalText.Substring(dotIndex + 1)}";
                    }
                }
            }
        }

        // === 内部组件 ===

        /// <summary>
        /// 继续提示闪烁
        /// </summary>
        private class ContinueHintBlink : MonoBehaviour
        {
            private Text _text;
            private float _timer;

            private void Awake()
            {
                _text = GetComponent<Text>();
            }

            private void Update()
            {
                _timer += Time.unscaledDeltaTime;
                float alpha = (Mathf.Sin(_timer * 3f) + 1f) / 2f;
                if (_text != null)
                {
                    var c = _text.color;
                    _text.color = new Color(c.r, c.g, c.b, 0.3f + alpha * 0.7f);
                }
            }
        }
    }
}
