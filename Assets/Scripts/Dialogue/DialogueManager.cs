using YuanHaiLu.GameSystem;
using UnityEngine;
using YuanHaiLu.Character;
using System.Collections.Generic;
using YuanHaiLu.Core;

namespace YuanHaiLu.Dialogue
{
    /// <summary>
    /// 对话管理器 v2 — 支持打字机效果、选择分支、条件判断
    /// 单例，挂载到 DialogueManager 空物体上
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("打字机设置")]
        [SerializeField] private float typeSpeed = 0.04f;
        [SerializeField] private float punctuationPause = 0.15f;

        // 当前对话状态
        private List<DialogueNode> _currentNodes;
        private int _currentNodeIndex;
        private bool _isTyping = false;
        private bool _skipRequested = false;
        private string _currentFullText = "";

        // === 事件 ===
        public event System.Action<string, string> OnDialogueStart;     // (speaker, firstLine)
        public event System.Action<string> OnLineShown;                 // (currentLine)
        public event System.Action OnDialogueEnd;
        public event System.Action<string[]> OnChoicesPresented;        // (choiceTexts)
        public event System.Action<int> OnChoiceSelected;               // (choiceIndex)

        public bool IsInDialogue => _currentNodes != null && _currentNodes.Count > 0;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (!IsInDialogue) return;

            // Space/Enter/Click 推进对话
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.J))
            {
                if (_isTyping)
                {
                    _skipRequested = true;
                }
                else
                {
                    AdvanceDialogue();
                }
            }

            // 数字键选择分支
            if (_waitingForChoice)
            {
                for (int i = 0; i < _currentChoices.Length && i < 9; i++)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                    {
                        SelectChoice(i);
                    }
                }
            }
        }

        // === 启动对话（简单字符串数组，向后兼容） ===

        public void StartDialogue(string speaker, string[] lines)
        {
            var nodes = new List<DialogueNode>();
            foreach (var line in lines)
            {
                nodes.Add(new DialogueNode { speaker = speaker, text = line });
            }
            StartDialogue(nodes);
        }

        // === 启动对话（节点列表，支持分支） ===

        public void StartDialogue(List<DialogueNode> nodes)
        {
            if (IsInDialogue) return;

            _currentNodes = nodes;
            _currentNodeIndex = 0;

            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameManager.GameState.Dialogue);

            ShowCurrentNode();
        }

        // === 显示当前节点 ===

        private void ShowCurrentNode()
        {
            if (_currentNodeIndex >= _currentNodes.Count)
            {
                EndDialogue();
                return;
            }

            var node = _currentNodes[_currentNodeIndex];

            // 条件检查
            if (!CheckCondition(node.condition))
            {
                _currentNodeIndex++;
                ShowCurrentNode();
                return;
            }

            // 检查是否有选择分支
            if (node.choices != null && node.choices.Length > 0)
            {
                PresentChoices(node);
                return;
            }

            // 检查是否有动作
            if (!string.IsNullOrEmpty(node.action))
            {
                ExecuteAction(node.action);
            }

            // 显示文本（打字机）
            StartCoroutine(TypeLine(node.speaker, node.text));

            OnDialogueStart?.Invoke(node.speaker, node.text);
        }

        // === 打字机效果 ===

        private System.Collections.IEnumerator TypeLine(string speaker, string text)
        {
            _isTyping = true;
            _skipRequested = false;
            _currentFullText = text;

            string displayed = "";
            for (int i = 0; i < text.Length; i++)
            {
                if (_skipRequested)
                {
                    displayed = text;
                    break;
                }

                displayed += text[i];
                OnLineShown?.Invoke(displayed);

                // 标点停顿
                char c = text[i];
                if (c == '。' || c == '！' || c == '？' || c == '…' ||
                    c == '.' || c == '!' || c == '?')
                {
                    yield return new WaitForSecondsRealtime(punctuationPause);
                }
                else if (c == '，' || c == '、' || c == ',' || c == ';')
                {
                    yield return new WaitForSecondsRealtime(punctuationPause * 0.5f);
                }
                else
                {
                    yield return new WaitForSecondsRealtime(typeSpeed);
                }
            }

            OnLineShown?.Invoke(displayed);
            _isTyping = false;
        }

        // === 推进对话 ===

        private void AdvanceDialogue()
        {
            _currentNodeIndex++;

            // 检查跳转
            if (_currentNodeIndex > 0 && _currentNodeIndex <= _currentNodes.Count)
            {
                var prevNode = _currentNodes[_currentNodeIndex - 1];
                if (prevNode.jumpToIndex >= 0 && prevNode.jumpToIndex < _currentNodes.Count)
                {
                    _currentNodeIndex = prevNode.jumpToIndex;
                }
            }

            ShowCurrentNode();
        }

        // === 选择分支 ===

        private bool _waitingForChoice = false;
        private DialogueChoice[] _currentChoices;

        private void PresentChoices(DialogueNode node)
        {
            _waitingForChoice = true;
            _currentChoices = node.choices;

            string[] choiceTexts = new string[node.choices.Length];
            for (int i = 0; i < node.choices.Length; i++)
            {
                choiceTexts[i] = node.choices[i].text;
            }

            // 先显示节点文本
            StartCoroutine(TypeLine(node.speaker, node.text));

            // 等打字完成后显示选项
            StartCoroutine(WaitForTypingThenShowChoices(choiceTexts));
        }

        private System.Collections.IEnumerator WaitForTypingThenShowChoices(string[] choiceTexts)
        {
            while (_isTyping) yield return null;
            OnChoicesPresented?.Invoke(choiceTexts);
        }

        public void SelectChoice(int index)
        {
            if (!_waitingForChoice || index < 0 || index >= _currentChoices.Length) return;

            _waitingForChoice = false;
            var choice = _currentChoices[index];

            OnChoiceSelected?.Invoke(index);

            // 执行选择动作
            if (!string.IsNullOrEmpty(choice.action))
            {
                ExecuteAction(choice.action);
            }

            // 跳转到指定节点
            if (choice.jumpToIndex >= 0 && choice.jumpToIndex < _currentNodes.Count)
            {
                _currentNodeIndex = choice.jumpToIndex;
                ShowCurrentNode();
            }
            else
            {
                EndDialogue();
            }
        }

        // === 条件系统 ===

        private bool CheckCondition(string condition)
        {
            if (string.IsNullOrEmpty(condition)) return true;

            // 格式: "hasItem:herb_spirit" / "questComplete:q001" / "level>=5" / "gold>=100"
            string[] parts = condition.Split(':');
            if (parts.Length < 2) return true;

            switch (parts[0])
            {
                case "hasItem":
                    return GameSystem.InventoryManager.Instance != null
                        && GameSystem.InventoryManager.Instance.HasItem(parts[1]);

                case "questActive":
                    return GameSystem.QuestManager.Instance != null
                        && GameSystem.QuestManager.Instance.IsQuestActive(parts[1]);

                case "questComplete":
                    return GameSystem.QuestManager.Instance != null
                        && GameSystem.QuestManager.Instance.IsQuestCompleted(parts[1]);

                case "level>=":
                    var player = GameObject.FindGameObjectWithTag("Player");
                    if (player != null)
                    {
                        var stats = player.GetComponent<CharacterStats>();
                        return stats != null && stats.level >= int.Parse(parts[1]);
                    }
                    return false;

                case "gold>=":
                    return GameSystem.InventoryManager.Instance != null
                        && GameSystem.InventoryManager.Instance.Gold >= int.Parse(parts[1]);

                default:
                    return true;
            }
        }

        // === 动作系统 ===

        private void ExecuteAction(string action)
        {
            // 格式: "giveItem:herb_medicinal:2" / "startQuest:q001" / "completeQuest:q001" / "giveGold:50" / "heal:50"
            string[] parts = action.Split(':');

            switch (parts[0])
            {
                case "giveItem":
                    int qty = parts.Length > 2 ? int.Parse(parts[2]) : 1;
                    if (GameSystem.InventoryManager.Instance != null)
                        GameSystem.InventoryManager.Instance.AddItem(parts[1], qty);
                    Debug.Log($"[对话] 获得物品: {parts[1]} x{qty}");
                    break;

                case "startQuest":
                    if (GameSystem.QuestManager.Instance != null)
                        GameSystem.QuestManager.Instance.AcceptQuestById(parts[1]);
                    Debug.Log($"[对话] 开始任务: {parts[1]}");
                    break;

                case "completeQuest":
                    if (GameSystem.QuestManager.Instance != null)
                        GameSystem.QuestManager.Instance.CompleteQuest(parts[1]);
                    break;

                case "giveGold":
                    if (GameSystem.InventoryManager.Instance != null)
                        GameSystem.InventoryManager.Instance.AddGold(int.Parse(parts[1]));
                    Debug.Log($"[对话] 获得 {parts[1]} 文钱");
                    break;

                case "heal":
                    var p = GameObject.FindGameObjectWithTag("Player");
                    if (p != null)
                    {
                        var healStats = p.GetComponent<CharacterStats>();
                        if (healStats != null) healStats.Heal(int.Parse(parts[1]));
                    }
                    break;

                case "learnSkill":
                    Debug.Log($"[对话] 学会武学: {parts[1]}（待接入）");
                    break;

                case "setFlag":
                    Debug.Log($"[对话] 设置标记: {parts[1]}");
                    break;
            }
        }

        // === 结束对话 ===

        private void EndDialogue()
        {
            _currentNodes = null;
            _currentNodeIndex = 0;
            _waitingForChoice = false;

            if (GameManager.Instance != null &&
                GameManager.Instance.currentState == GameManager.GameState.Dialogue)
            {
                GameManager.Instance.SetState(GameManager.GameState.Exploration);
            }

            OnDialogueEnd?.Invoke();
        }

        /// <summary>
        /// 强制结束当前对话
        /// </summary>
        public void ForceEndDialogue()
        {
            if (IsInDialogue) EndDialogue();
        }
    }

    // ========== 数据结构 ==========

    /// <summary>
    /// 对话节点 — 一句话或一个选择分支
    /// </summary>
    [System.Serializable]
    public class DialogueNode
    {
        public string speaker = "";
        [TextArea(2, 4)] public string text = "";
        public string condition = "";              // 显示条件
        public string action = "";                 // 执行动作
        public int jumpToIndex = -1;               // 跳转到第几个节点（-1=顺序）
        public DialogueChoice[] choices;           // 选择分支（null=普通对话）
    }

    /// <summary>
    /// 对话选择项
    /// </summary>
    [System.Serializable]
    public class DialogueChoice
    {
        public string text = "";                   // 选项文本
        public string condition = "";              // 可选条件
        public string action = "";                 // 选择后的动作
        public int jumpToIndex = -1;               // 跳转到第几个节点
    }
}
