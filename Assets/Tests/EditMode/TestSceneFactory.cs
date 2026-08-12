using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using YuanHaiLu.Character;
using YuanHaiLu.Core;
using YuanHaiLu.Dialogue;
using YuanHaiLu.Effects;
using YuanHaiLu.GameSystem;

namespace YuanHaiLu.Tests.EditMode
{
    internal static class TestSceneFactory
    {
        private static readonly List<GameObject> Roots = new List<GameObject>();
        private static readonly List<Object> TrackedObjects = new List<Object>();

        internal static GameObject Create(string name)
        {
            var gameObject = new GameObject(name);
            Roots.Add(gameObject);
            return gameObject;
        }

        internal static GameObject CreatePlayer()
        {
            var player = Create("Player");
            player.tag = "Player";
            player.AddComponent<SpriteRenderer>();
            player.AddComponent<Animator>();
            player.AddComponent<Rigidbody2D>();
            player.AddComponent<BoxCollider2D>();
            player.AddComponent<CharacterStats>();
            return player;
        }

        internal static T AddComponentWithAwake<T>(GameObject gameObject) where T : MonoBehaviour
        {
            var component = gameObject.AddComponent<T>();
            typeof(T).GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(component, null);
            return component;
        }

        internal static T CreateScriptableObject<T>() where T : ScriptableObject
        {
            var instance = ScriptableObject.CreateInstance<T>();
            TrackedObjects.Add(instance);
            return instance;
        }

        internal static void DestroyAll()
        {
            for (int i = Roots.Count - 1; i >= 0; i--)
            {
                if (Roots[i] != null)
                    Object.DestroyImmediate(Roots[i]);
            }

            Roots.Clear();

            for (int i = TrackedObjects.Count - 1; i >= 0; i--)
            {
                if (TrackedObjects[i] != null)
                    Object.DestroyImmediate(TrackedObjects[i]);
            }

            TrackedObjects.Clear();
            ResetSingleton<GameManager>();
            ResetSingleton<SaveManager>();
            ResetSingleton<InventoryManager>();
            ResetSingleton<QuestManager>();
            ResetSingleton<GameTimeManager>();
            ResetSingleton<DialogueManager>();
            ResetSingleton<AudioManager>();
            ResetSingleton<EffectsManager>();
        }

        private static void ResetSingleton<T>()
        {
            typeof(T).GetField(
                "<Instance>k__BackingField",
                BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, null);
        }
    }
}
