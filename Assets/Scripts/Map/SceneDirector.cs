using UnityEngine;
using System.Collections;
using YuanHaiLu.Core;
using YuanHaiLu.Character;
using YuanHaiLu.GameSystem;
using YuanHaiLu.Dialogue;

namespace YuanHaiLu.Map
{
    /// <summary>
    /// 场景引导 — Demo开场序列
    /// 控制开场动画、玩家出生、初始化流程
    /// 挂载到场景中的空物体上
    /// </summary>
    public class SceneDirector : MonoBehaviour
    {
        [Header("开场设置")]
        [SerializeField] private bool playIntro = true;
        [SerializeField] private float introDelay = 1f;

        [Header("玩家出生设置")]
        [SerializeField] private Vector2 spawnPosition = new Vector2(0, -5);
        [SerializeField] private bool teachControls = true;

        [Header("对话序列")]
        [SerializeField] private DialogueSequence introSequence;

        private bool _introPlayed = false;

        public void ConfigureForEditor(Vector2 playerSpawnPosition)
        {
            spawnPosition = playerSpawnPosition;
        }

        private void Start()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                PlayerInteraction.EnsureOn(player);

            bool shouldInitializeNewGame = GameManager.Instance == null ||
                                           GameManager.Instance.ShouldInitializeNewGame;
            if (!_introPlayed && shouldInitializeNewGame)
            {
                StartCoroutine(PlayIntroSequence());
            }
        }

        private IEnumerator PlayIntroSequence()
        {
            _introPlayed = true;

            // 等待全局管理器初始化
            yield return new WaitForSeconds(introDelay);

            // 初始化玩家
            yield return InitPlayer();

            // 初始化武学
            yield return InitMartialArts();

            CompleteNewGameInitialization(GameManager.Instance);

            // playIntro 只控制演出；出生、属性、武学和初始物资始终需要初始化。
            if (!playIntro)
                yield break;

            // 显示区域名
            var transition = ScreenTransition.Instance;
            if (transition != null)
            {
                transition.ShowAreaName("烟柳镇", "渊朝·江南道·烟柳镇", 3f);
            }

            yield return new WaitForSeconds(4f);

            // 开场对话
            if (introSequence != null && introSequence.nodes.Length > 0)
            {
                var nodes = new System.Collections.Generic.List<DialogueNode>();
                nodes.AddRange(introSequence.nodes);
                DialogueManager.Instance?.StartDialogue(nodes);
                yield break; // 对话结束后由 DialogueManager 接管
            }
            else
            {
                // 默认开场对话
                var dm = DialogueManager.Instance;
                if (dm != null)
                {
                    dm.StartDialogue("凌霜（内心）", new string[]
                    {
                        "……这里是哪里？",
                        "烟柳镇……名字倒是雅致。",
                        "身上只有一柄铁剑和一些碎银子。",
                        "先四处看看吧。"
                    });
                }
            }

            // 教程提示
            if (teachControls)
            {
                yield return new WaitForSeconds(6f);
                ShowControlTips();
            }
        }

        private IEnumerator InitPlayer()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = spawnPosition;

                var stats = player.GetComponent<CharacterStats>();
                if (stats != null)
                {
                    stats.characterName = "凌霜";
                    stats.level = 1;
                    stats.exp = 0;
                    stats.SetBaseFromLoad(15, 5, 10, 100, 50, 100, 50);
                }
            }

            yield return null;
        }

        private IEnumerator InitMartialArts()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) yield break;

            var martialSys = player.GetComponent<MartialArtsSystem>();
            if (martialSys != null)
            {
                // 学习初始招式
                var starterSkills = MartialSkillDatabase.GetStarterSkills();
                foreach (var skillId in starterSkills)
                {
                    var skill = MartialSkillDatabase.Get(skillId);
                    if (skill != null)
                        martialSys.LearnSkill(skill);
                }
            }

            // 装备初始物品
            var inv = InventoryManager.Instance;
            if (inv != null)
            {
                inv.AddItem("herb_medicinal", 3);
                inv.AddItem("food_mantou", 5);
                inv.AddGold(50);
            }

            Debug.Log("[引导] 初始武学和物品已装备");
            yield return null;
        }

        internal static void CompleteNewGameInitialization(GameManager gameManager)
        {
            if (gameManager == null) return;

            gameManager.SetState(GameManager.GameState.Exploration);
            gameManager.CompleteSceneEntry();
        }

        private void ShowControlTips()
        {
            // 显示操作提示（通过对话系统）
            var dm = DialogueManager.Instance;
            if (dm != null && !dm.IsInDialogue)
            {
                dm.StartDialogue("系统", new string[]
                {
                    "【操作提示】",
                    "WASD 移动 | J 攻击（3连击）| K/E 交互",
                    "Shift 冲刺 | Tab 背包 | Q 任务 | ESC 暂停",
                    "数字键 1-4 释放武学招式",
                    "祝你旅途愉快，少侠！"
                });
            }
        }
    }

    // ========== 对话序列数据 ==========

    [CreateAssetMenu(fileName = "DialogueSequence", menuName = "渊海录/对话序列")]
    [System.Serializable]
    public class DialogueSequence : ScriptableObject
    {
        public DialogueNode[] nodes;
    }
}
