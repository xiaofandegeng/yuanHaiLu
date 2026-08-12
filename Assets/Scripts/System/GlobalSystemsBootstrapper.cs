using UnityEngine;
using YuanHaiLu.Core;
using YuanHaiLu.Dialogue;

namespace YuanHaiLu.GameSystem
{
    /// <summary>
    /// 补齐跨场景存活的核心管理器。入口场景和游戏场景共用同一套创建规则。
    /// </summary>
    public static class GlobalSystemsBootstrapper
    {
        public static void EnsureRequiredSystems(GameManager owner)
        {
            if (owner == null)
            {
                Debug.LogError("[GlobalSystems] 缺少 GameManager，无法初始化全局系统！");
                return;
            }

            EnsureChild<SaveManager>(owner, "SaveManager", SaveManager.Instance);
            EnsureChild<InventoryManager>(owner, "InventoryManager", InventoryManager.Instance);
            EnsureChild<QuestManager>(owner, "QuestManager", QuestManager.Instance);
            EnsureChild<GameTimeManager>(owner, "GameTimeManager", GameTimeManager.Instance);
            EnsureChild<DialogueManager>(owner, "DialogueManager", DialogueManager.Instance);
        }

        private static void EnsureChild<T>(GameManager owner, string objectName, T instance)
            where T : Component
        {
            if (instance != null || owner.GetComponentInChildren<T>(true) != null)
                return;

            var child = new GameObject(objectName);
            child.transform.SetParent(owner.transform);
            child.AddComponent<T>();
        }
    }
}
