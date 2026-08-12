using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using YuanHaiLu.Character;
using YuanHaiLu.Dialogue;
using YuanHaiLu.GameSystem;

namespace YuanHaiLu.Tests.PlayMode
{
    public class QuestFlowPlayModeTests
    {
        private readonly List<GameObject> _createdObjects = new List<GameObject>();

        [UnityTest]
        public IEnumerator QuestNpcAcceptsAndCompletesThroughDialogueEndEvents()
        {
            QuestManager questManager = CreateObject("QuestManager").AddComponent<QuestManager>();
            DialogueManager dialogueManager = CreateObject("DialogueManager").AddComponent<DialogueManager>();
            InventoryManager inventory = CreateObject("InventoryManager").AddComponent<InventoryManager>();

            GameObject player = CreateObject("Player");
            player.tag = "Player";
            CharacterStats stats = player.AddComponent<CharacterStats>();

            GameObject npcObject = CreateObject("Innkeeper");
            npcObject.AddComponent<BoxCollider2D>();
            NPCBase npc = npcObject.AddComponent<NPCBase>();
            npc.npcName = "赵掌柜";
            npc.canWander = false;
            QuestGiver questGiver = npcObject.AddComponent<QuestGiver>();
            questGiver.questId = "M01_01";
            questGiver.interactionTargetId = "innkeeper_zhao";
            int startingGold = inventory.Gold;

            npc.OnInteract(player);
            yield return null;

            Assert.That(dialogueManager.IsInDialogue, Is.True);
            Assert.That(questManager.IsQuestActive("M01_01"), Is.False);
            dialogueManager.ForceEndDialogue();

            ActiveQuest activeQuest = questManager.GetActiveQuest("M01_01");
            Assert.That(activeQuest, Is.Not.Null);
            Assert.That(activeQuest.Objectives[1].currentAmount, Is.EqualTo(1));

            questManager.UpdateObjective(
                QuestObjective.ObjectiveType.ReachArea,
                "yanliu_inn");
            questManager.UpdateObjective(
                QuestObjective.ObjectiveType.TalkToNPC,
                "drunk_old_man");
            Assert.That(activeQuest.state, Is.EqualTo(ActiveQuest.QuestState.Completable));

            npc.OnInteract(player);
            yield return null;
            dialogueManager.ForceEndDialogue();
            yield return null;

            Assert.That(questManager.IsQuestCompleted("M01_01"), Is.True);
            Assert.That(inventory.Gold, Is.EqualTo(startingGold + 20));
            Assert.That(stats.exp, Is.EqualTo(50));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                    Object.Destroy(_createdObjects[i]);
            }

            _createdObjects.Clear();
            yield return null;
        }

        private GameObject CreateObject(string name)
        {
            var gameObject = new GameObject(name);
            _createdObjects.Add(gameObject);
            return gameObject;
        }
    }
}
