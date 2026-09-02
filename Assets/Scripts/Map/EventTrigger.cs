using UnityEngine;
using System.Collections.Generic;
using YuanHaiLu.Core;
using YuanHaiLu.Character;
using YuanHaiLu.Dialogue;
using YuanHaiLu.GameSystem;
using YuanHaiLu.Art;

namespace YuanHaiLu.Map
{
    /// <summary>
    /// 事件触发器 — 进入区域时触发剧情/战斗/对话/效果
    /// 用于制作固定事件点（如BOSS战、剧情演出）
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class EventTrigger : MonoBehaviour, IInteractable
    {
        public enum TriggerType
        {
            Dialogue,       // 触发对话
            Combat,         // 触发战斗（生成敌人波次）
            Cutscene,       // 触发过场
            QuestStart,     // 任务开始
            QuestComplete,  // 任务完成检查点
            Effect,         // 触发特效/环境变化
            Custom          // 自定义事件
        }

        [Header("触发设置")]
        public TriggerType triggerType = TriggerType.Dialogue;
        public bool triggerOnce = true;         // 只触发一次
        public bool requireInteract = false;     // 需要按交互键
        public float triggerDelay = 0f;          // 触发延迟
        public string requiredQuestId = "";      // 需要特定任务才触发
        public bool requiredQuestActive = true;  // true=任务进行中触发，false=任务未开始触发

        [Header("对话（TriggerType.Dialogue）")]
        public string speakerName = "";
        [TextArea(2, 5)] public string[] dialogueLines;

        [Header("战斗（TriggerType.Combat）")]
        public WaveData[] enemyWaves;            // 敌人波次
        public float waveInterval = 2f;          // 波次间隔

        [Header("任务（TriggerType.QuestStart/Complete）")]
        public string questId = "";
        public QuestData questData;       // 任务数据引用

        [Header("特效（TriggerType.Effect）")]
        public string effectId = "";
        public bool changeBGM = false;
        public string newBGM = "";

        [Header("状态")]
        public bool hasTriggered = false;

        private Collider2D _col;

        private void Awake()
        {
            _col = GetComponent<Collider2D>();
            _col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            if (requireInteract) return;

            TryTrigger(other.gameObject);
        }

        public void OnInteract(GameObject player)
        {
            TryTrigger(player);
        }

        /// <summary>
        /// 仅当需要按键触发时，才作为可交互目标被检测
        /// （自动触发型 requireInteract=false 仍走 OnTriggerEnter2D，不会被误提示）
        /// </summary>
        public bool CanInteract()
        {
            return requireInteract && !(hasTriggered && triggerOnce);
        }

        private void TryTrigger(GameObject player)
        {
            // 已触发过且只触发一次
            if (hasTriggered && triggerOnce) return;

            // 任务条件检查
            if (!string.IsNullOrEmpty(requiredQuestId))
            {
                var questMgr = GameSystem.QuestManager.Instance;
                if (questMgr == null) return;

                if (requiredQuestActive)
                {
                    if (!questMgr.IsQuestActive(requiredQuestId)) return;
                }
                else
                {
                    if (questMgr.IsQuestCompleted(requiredQuestId)) return;
                }
            }

            // 延迟触发
            if (triggerDelay > 0)
            {
                StartCoroutine(DelayedTrigger(player));
            }
            else
            {
                ExecuteTrigger(player);
            }
        }

        private System.Collections.IEnumerator DelayedTrigger(GameObject player)
        {
            yield return new WaitForSeconds(triggerDelay);
            ExecuteTrigger(player);
        }

        private void ExecuteTrigger(GameObject player)
        {
            hasTriggered = true;

            switch (triggerType)
            {
                case TriggerType.Dialogue:
                    TriggerDialogue();
                    break;
                case TriggerType.Combat:
                    StartCoroutine(TriggerCombat(player));
                    break;
                case TriggerType.QuestStart:
                    TriggerQuestStart();
                    break;
                case TriggerType.QuestComplete:
                    TriggerQuestComplete();
                    break;
                case TriggerType.Effect:
                    TriggerEffect();
                    break;
                case TriggerType.Cutscene:
                    TriggerCutscene();
                    break;
            }

            Debug.Log($"[EventTrigger] 触发事件: {triggerType} @ {name}");
        }

        // === 对话 ===
        private void TriggerDialogue()
        {
            if (dialogueLines == null || dialogueLines.Length == 0) return;

            var dlgMgr = DialogueManager.Instance;
            if (dlgMgr != null)
            {
                dlgMgr.StartDialogue(speakerName, dialogueLines);
            }
        }

        // === 战斗（波次） ===
        private System.Collections.IEnumerator TriggerCombat(GameObject player)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.EnterCombat();

            for (int w = 0; w < enemyWaves.Length; w++)
            {
                WaveData wave = enemyWaves[w];
                Debug.Log($"[EventTrigger] 第 {w + 1} 波！");

                for (int i = 0; i < wave.count; i++)
                {
                    Vector2 spawnPos = (Vector2)transform.position +
                        Random.insideUnitCircle * wave.spawnRadius;
                    SpawnEnemy(wave.enemyName, wave.artId, wave.enemyHp, wave.enemyAtk, spawnPos);

                    // 稍错开生成
                    if (i < wave.count - 1)
                        yield return new WaitForSeconds(0.3f);
                }

                // 等待这波敌人全部消灭
                yield return new WaitWhile(() => GameObject.FindGameObjectsWithTag("Enemy").Length > 0);

                // 波次间隔
                if (w < enemyWaves.Length - 1)
                    yield return new WaitForSeconds(waveInterval);
            }

            if (GameManager.Instance != null)
                GameManager.Instance.ExitCombat();

            Debug.Log("[EventTrigger] 战斗结束！");

            // 战斗后自动触发对话或任务完成
            if (!string.IsNullOrEmpty(questId))
            {
                TriggerQuestComplete();
            }
        }

        private void SpawnEnemy(string name, string artId, int hp, int atk, Vector2 pos)
        {
            GameObject enemy = new GameObject($"Enemy_{name}");
            enemy.transform.position = pos;
            enemy.tag = "Enemy";
            enemy.layer = LayerMask.NameToLayer("Enemy");

            var sr = enemy.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Character";
            CharacterVisual.ApplyTo(enemy,
                string.IsNullOrEmpty(artId) ? "yanliu_river_bandit" : artId);

            var rb = enemy.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            var col = enemy.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 1.2f);

            var stats = enemy.AddComponent<CharacterStats>();
            stats.characterName = name;
            stats.maxHp = hp;
            stats.currentHp = hp;
            stats.attack = atk;

            enemy.AddComponent<EnemyAI>();
        }

        // === 任务 ===
        private void TriggerQuestStart()
        {
            if (questData == null && string.IsNullOrEmpty(questId)) return;

            var questMgr = GameSystem.QuestManager.Instance;
            if (questMgr != null)
            {
                questMgr.AcceptQuest(questData);
            }
        }

        private void TriggerQuestComplete()
        {
            if (string.IsNullOrEmpty(questId)) return;

            var questMgr = GameSystem.QuestManager.Instance;
            if (questMgr != null)
            {
                questMgr.CompleteQuest(questId);
            }
        }

        // === 特效 ===
        private void TriggerEffect()
        {
            if (!string.IsNullOrEmpty(effectId))
            {
                Effects.EffectsManager.Instance?.PlayEffect(effectId, transform.position);
            }

            if (changeBGM && !string.IsNullOrEmpty(newBGM))
            {
                GameSystem.AudioManager.Instance?.PlayBGM(newBGM);
            }
        }

        // === 过场 ===
        private void TriggerCutscene()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameManager.GameState.Cutscene);

            Debug.Log("[EventTrigger] 过场动画播放中...");
        }

        // === 波次数据 ===
        [System.Serializable]
        public class WaveData
        {
            public string enemyName = "山贼";
            public string artId = "yanliu_river_bandit";
            public int count = 3;
            public int enemyHp = 20;
            public int enemyAtk = 5;
            public float spawnRadius = 3f;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Color color = triggerType switch
            {
                TriggerType.Dialogue => new Color(0.2f, 0.5f, 1f, 0.3f),
                TriggerType.Combat => new Color(1f, 0.2f, 0.2f, 0.3f),
                TriggerType.QuestStart => new Color(0.2f, 1f, 0.2f, 0.3f),
                TriggerType.QuestComplete => new Color(1f, 1f, 0.2f, 0.3f),
                _ => new Color(0.5f, 0.5f, 0.5f, 0.3f)
            };
            Gizmos.color = color;

            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                Gizmos.DrawCube(transform.position, col.bounds.size);
            }
            else
            {
                Gizmos.DrawWireSphere(transform.position, 1f);
            }
        }
#endif
    }
}
