using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using YuanHaiLu.Art;

namespace YuanHaiLu.Editor
{
    public sealed class CharacterShowcaseWindow : EditorWindow
    {
        public static readonly string[] SupportedActions =
        {
            "idle", "walk", "dash", "attack1", "attack2", "attack3", "skill1", "skill2", "hurt", "death"
        };

        public static readonly int[] SupportedScales = { 1, 4, 8 };

        private static readonly string[] DirectionNames = { "down", "left", "right", "up" };
        private static readonly IReadOnlyDictionary<string, string> ActionToState =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["idle"] = "idle",
                ["walk"] = "walk",
                ["dash"] = "dash",
                ["attack1"] = "attack_1",
                ["attack2"] = "attack_2",
                ["attack3"] = "attack_3",
                ["skill1"] = "skill_1",
                ["skill2"] = "skill_2",
                ["hurt"] = "hurt",
                ["death"] = "death"
            };

        [SerializeField] private string selectedId;
        [SerializeField] private string selectedAction = "idle";
        [SerializeField] private int selectedScale = 4;
        [SerializeField] private int selectedFacing;
        private GameObject previewInstance;

        [MenuItem("Tools/渊海录/美术/角色总览控制台")]
        public static void Open()
        {
            GetWindow<CharacterShowcaseWindow>("角色总览");
        }

        private void OnEnable()
        {
            selectedAction = string.IsNullOrEmpty(selectedAction) ? "idle" : selectedAction;
            selectedScale = SupportedScales.Contains(selectedScale) ? selectedScale : 4;
            EditorApplication.delayCall += EnsurePreview;
        }

        private void OnDisable()
        {
            EditorApplication.delayCall -= EnsurePreview;
            DestroyPreview();
        }

        private void OnGUI()
        {
            var catalog = CharacterArtCatalog.LoadDefault();
            var entries = catalog.Entries.OrderBy(entry => entry.Id, StringComparer.Ordinal).ToArray();
            if (entries.Length == 0)
            {
                EditorGUILayout.HelpBox("未找到正式角色目录。请先重建角色动画与 Prefab。", MessageType.Warning);
                return;
            }

            var ids = entries.Select(entry => entry.Id).ToArray();
            var selectedIndex = Math.Max(0, Array.IndexOf(ids, selectedId));
            EditorGUI.BeginChangeCheck();
            selectedIndex = EditorGUILayout.Popup("角色", selectedIndex, ids);
            var actionIndex = EditorGUILayout.Popup(
                "动作",
                Math.Max(0, Array.IndexOf(SupportedActions, selectedAction)),
                SupportedActions);
            selectedAction = SupportedActions[actionIndex];
            selectedFacing = EditorGUILayout.Popup("朝向", selectedFacing, DirectionNames);
            selectedScale = SupportedScales[EditorGUILayout.Popup(
                "缩放", Array.IndexOf(SupportedScales, selectedScale),
                SupportedScales.Select(scale => scale + "×").ToArray())];
            if (EditorGUI.EndChangeCheck())
            {
                selectedId = ids[selectedIndex];
                EnsurePreview();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                $"状态：{AnimatorStateFor(selectedAction, selectedFacing)}，缩放：{selectedScale}×",
                MessageType.Info);
            if (GUILayout.Button("打开并应用到 97 角色总览"))
            {
                OpenAndApplyToShowcase();
            }
        }

        public string AnimatorStateFor(string actionId, int facing)
        {
            if (facing < 0 || facing >= DirectionNames.Length)
                throw new ArgumentOutOfRangeException(nameof(facing));
            if (!ActionToState.TryGetValue(actionId, out var state))
                throw new ArgumentOutOfRangeException(nameof(actionId));
            return state + "_" + DirectionNames[facing];
        }

        private void EnsurePreview()
        {
            if (this == null)
                return;
            var catalog = CharacterArtCatalog.LoadDefault();
            var entry = catalog.Entries.OrderBy(item => item.Id, StringComparer.Ordinal)
                .FirstOrDefault(item => string.IsNullOrEmpty(selectedId) || item.Id == selectedId);
            if (entry == null || entry.Prefab == null)
                return;
            selectedId = entry.Id;
            DestroyPreview();
            previewInstance = (GameObject)PrefabUtility.InstantiatePrefab(entry.Prefab);
            previewInstance.name = "CharacterShowcasePreview_" + entry.Id;
            previewInstance.hideFlags = HideFlags.HideAndDontSave;
            previewInstance.transform.position = new Vector3(-10000f, -10000f, 0f);
            previewInstance.transform.localScale = Vector3.one * selectedScale;
            var animator = previewInstance.GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
                animator.Play(AnimatorStateFor(selectedAction, selectedFacing), 0, 0f);
        }

        private void DestroyPreview()
        {
            if (previewInstance != null)
                UnityEngine.Object.DestroyImmediate(previewInstance);
            previewInstance = null;
        }

        private void OpenAndApplyToShowcase()
        {
            DestroyPreview();
            if (!System.IO.File.Exists(CharacterShowcaseGenerator.ScenePath))
                CharacterShowcaseGenerator.Generate();
            EditorSceneManager.OpenScene(CharacterShowcaseGenerator.ScenePath, OpenSceneMode.Single);
            foreach (var visual in FindObjectsByType<CharacterVisual>(FindObjectsSortMode.None))
            {
                visual.transform.localScale = Vector3.one * selectedScale;
                var animator = visual.GetComponent<Animator>();
                if (animator != null && animator.runtimeAnimatorController != null)
                    animator.Play(AnimatorStateFor(selectedAction, selectedFacing), 0, 0f);
            }
        }
    }
}
