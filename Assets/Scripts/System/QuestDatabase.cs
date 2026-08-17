using System.Collections.Generic;
using UnityEngine;

namespace YuanHaiLu.GameSystem
{
    /// <summary>
    /// 代码预置任务数据库。Resources/Quests 中同 ID 的 QuestData 会覆盖代码模板。
    /// </summary>
    public static class QuestDatabase
    {
        private static Dictionary<string, QuestData> _quests;

        public static IReadOnlyDictionary<string, QuestData> AllQuests
        {
            get
            {
                EnsureBuilt();
                return _quests;
            }
        }

        public static QuestData Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            EnsureBuilt();
            return _quests.TryGetValue(id, out QuestData quest) ? quest : null;
        }

        private static void EnsureBuilt()
        {
            if (_quests != null) return;

            _quests = new Dictionary<string, QuestData>();
            AddM01Quests();
            AddMvpQuests();

            foreach (QuestData quest in Resources.LoadAll<QuestData>("Quests"))
            {
                if (quest != null && !string.IsNullOrEmpty(quest.questId))
                    _quests[quest.questId] = quest;
            }
        }

        private static void AddM01Quests()
        {
            Add(Create(
                "M01_01",
                "初到烟柳镇",
                "凌霜来到烟柳镇，人生地不熟。先在镇上打探消息。",
                new[]
                {
                    Objective(QuestObjective.ObjectiveType.ReachArea, "yanliu_inn", "进入烟柳镇客栈"),
                    Objective(QuestObjective.ObjectiveType.TalkToNPC, "innkeeper_zhao", "与客栈掌柜交谈"),
                    Objective(QuestObjective.ObjectiveType.TalkToNPC, "drunk_old_man", "与疯老头交谈")
                },
                rewardExp: 50,
                rewardGold: 20,
                unlockQuestIds: new[] { "M01_02" },
                introDialogue: new[] { "初来烟柳镇？先在镇上四处看看吧。" },
                progressDialogue: new[] { "客栈里人多眼杂，也最容易打听到消息。" },
                completeDialogue: new[] { "若想继续找人，可以去镇北的柳家药铺问问。" }));

            Add(Create(
                "M01_02",
                "柳家药铺",
                "掌柜提到柳家药铺的苏婉清可能知道一些线索。",
                new[]
                {
                    Objective(QuestObjective.ObjectiveType.ReachArea, "liu_apothecary", "前往柳家药铺"),
                    Objective(QuestObjective.ObjectiveType.TalkToNPC, "su_wanqing", "与苏婉清交谈"),
                    Objective(QuestObjective.ObjectiveType.CollectItem, "herb_medicinal", "采集疗伤草", 3)
                },
                prerequisiteQuests: new[] { "M01_01" },
                rewardExp: 100,
                rewardGold: 50,
                unlockQuestIds: new[] { "M01_03" },
                introDialogue: new[] { "药铺库存告急，能否帮我采三份疗伤草？" },
                progressDialogue: new[] { "疗伤草常生在镇外湿润的草地上。" },
                completeDialogue: new[] { "多谢相助。父亲曾留下过一枚刻有铭文的玉佩碎片……" }));

            Add(Create(
                "M01_03",
                "玉佩之谜",
                "苏婉清父亲遗留的玉佩碎片刻着奇怪铭文，需要请陈先生解读。",
                new[]
                {
                    Objective(QuestObjective.ObjectiveType.TalkToNPC, "teacher_chen_intro", "请教陈先生"),
                    Objective(QuestObjective.ObjectiveType.CollectItem, "quest_old_book", "找回废弃书塾中的古书"),
                    Objective(QuestObjective.ObjectiveType.TalkToNPC, "teacher_chen_return", "返回陈先生处解读铭文")
                },
                prerequisiteQuests: new[] { "M01_02" },
                rewardExp: 200,
                rewardGold: 100,
                rewardItemIds: new[] { "book_basic_sword" },
                unlockQuestIds: new[] { "M01_04" },
                introDialogue: new[] { "这铭文残缺不全，若能找回书塾古书，或许可以辨认。" },
                progressDialogue: new[] { "古书应当还在镇西废弃书塾。" },
                completeDialogue: new[] { "这是渊朝皇室之物。你从哪里得来的？" }));

            Add(Create(
                "M01_04",
                "北山山贼",
                "北山山贼下山劫掠，镇民被困，必须前去救援。",
                new[]
                {
                    Objective(QuestObjective.ObjectiveType.ReachArea, "north_mountain", "前往北山山道"),
                    Objective(QuestObjective.ObjectiveType.KillEnemy, "bandit", "击败山贼", 5),
                    Objective(QuestObjective.ObjectiveType.DefeatBoss, "boss_heifeng", "击败山贼头目黑风")
                },
                prerequisiteQuests: new[] { "M01_03" },
                rewardExp: 300,
                rewardGold: 200,
                rewardItemIds: new[] { "sword_frost" },
                unlockQuestIds: new[] { "M01_05" },
                introDialogue: new[] { "北山山贼又来劫掠，请少侠救救被困的镇民！" },
                progressDialogue: new[] { "山贼头目黑风就在北山深处。" },
                completeDialogue: new[] { "黑风临死前提到了北冥的人，这绝非普通匪患。" }));

            Add(Create(
                "M01_05",
                "暗流涌动",
                "北冥的线索指向天枢城。向镇上的朋友告别，然后继续上路。",
                new[]
                {
                    Objective(QuestObjective.ObjectiveType.TalkToNPC, "su_wanqing_farewell", "与苏婉清告别"),
                    Objective(QuestObjective.ObjectiveType.TalkToNPC, "innkeeper_route", "向掌柜打听去天枢城的路"),
                    Objective(QuestObjective.ObjectiveType.ReachArea, "yanliu_south_exit", "走出烟柳镇南门")
                },
                prerequisiteQuests: new[] { "M01_04" },
                rewardExp: 500,
                rewardGold: 300,
                introDialogue: new[] { "北冥一事牵连甚广，你该去天枢城寻找答案。" },
                progressDialogue: new[] { "临行前，别忘了向帮助过你的人告别。" },
                completeDialogue: new[] { "烟柳镇渐渐远去，前路通向渊朝的心脏——天枢城。" }));
        }

        /// <summary>
        /// 单主角 MVP 主线（docs/15）：河岸失物。
        /// 五个目标严格按序推进，每步只在真实成功行为后上报。
        /// </summary>
        private static void AddMvpQuests()
        {
            Add(Create(
                "MVP_01",
                "河岸失物",
                "掌柜老赵运货途中在河岸被水匪劫走了荷包。去河岸夺回来。",
                new[]
                {
                    Objective(QuestObjective.ObjectiveType.TalkToNPC, "innkeeper_zhao", "与掌柜老赵交谈"),
                    Objective(QuestObjective.ObjectiveType.ReachArea, "yanliu_riverbank", "前往烟柳镇河岸"),
                    Objective(QuestObjective.ObjectiveType.KillEnemy, "river_bandit", "击败河岸水匪", 2),
                    Objective(QuestObjective.ObjectiveType.CollectItem, "quest_lost_pouch", "寻回掌柜的荷包"),
                    Objective(QuestObjective.ObjectiveType.TalkToNPC, "innkeeper_zhao", "回客栈向掌柜复命")
                },
                rewardExp: 80,
                rewardGold: 60,
                rewardItemIds: new[] { "herb_medicinal" },
                introDialogue: new[]
                {
                    "客官来得正好！",
                    "我前日运货，在镇南河岸被两个水匪劫了，荷包也丢了。",
                    "荷包里有全客栈的账银，务请少侠帮我取回来。",
                    "出客栈往南走，过了河岸就是。"
                },
                progressDialogue: new[]
                {
                    "河岸就在镇子南边，水匪还在那一带游荡。",
                    "荷包找回来后，回这里告诉我一声。"
                },
                completeDialogue: new[]
                {
                    "荷包……账银一文不少！少侠真是雪中送炭！",
                    "这点薄礼不成敬意，往后客栈的房间随你住。",
                    "若还想在镇上走走，南边的河岸风景其实不错。"
                },
                sequential: true));
        }

        private static QuestData Create(
            string id,
            string name,
            string description,
            QuestObjective[] objectives,
            string[] prerequisiteQuests = null,
            int rewardExp = 0,
            int rewardGold = 0,
            string[] rewardItemIds = null,
            string rewardSkillId = "",
            string[] unlockQuestIds = null,
            string[] introDialogue = null,
            string[] progressDialogue = null,
            string[] completeDialogue = null,
            bool sequential = false)
        {
            QuestData quest = ScriptableObject.CreateInstance<QuestData>();
            quest.hideFlags = HideFlags.HideAndDontSave;
            quest.questId = id;
            quest.questName = name;
            quest.description = description;
            quest.type = QuestData.QuestType.MainStory;
            quest.rarity = QuestData.QuestRarity.Critical;
            quest.objectives = objectives;
            quest.sequentialObjectives = sequential;
            quest.prerequisiteQuests = prerequisiteQuests;
            quest.rewardExp = rewardExp;
            quest.rewardGold = rewardGold;
            quest.rewardItemIds = rewardItemIds;
            quest.rewardSkillId = rewardSkillId;
            quest.unlockQuestIds = unlockQuestIds;
            quest.introDialogue = introDialogue;
            quest.progressDialogue = progressDialogue;
            quest.completeDialogue = completeDialogue;
            return quest;
        }

        private static QuestObjective Objective(
            QuestObjective.ObjectiveType type,
            string targetId,
            string targetName,
            int requiredAmount = 1)
        {
            return new QuestObjective
            {
                type = type,
                targetId = targetId,
                targetName = targetName,
                requiredAmount = Mathf.Max(1, requiredAmount)
            };
        }

        private static void Add(QuestData quest)
        {
            _quests[quest.questId] = quest;
        }
    }
}
