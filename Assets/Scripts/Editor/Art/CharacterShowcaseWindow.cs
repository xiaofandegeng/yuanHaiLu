using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using YuanHaiLu.Art;

namespace YuanHaiLu.Editor
{
    /// <summary>
    /// 角色总览交互窗口：在一个固定的动作词表和缩放词表上预览正式角色 Prefab。
    /// 动作名与正式角色 Animator 状态前缀一一对应（attack_1/skill_1 等带下划线），
    /// 因此 <see cref="PreviewAction"/> 可直接 <c>Animator.Play</c> 到对应方向状态，
    /// 不受仅服务于运行时玩法的参数过渡限制。
    /// 未知动作或缩放一律抛 <see cref="ArgumentException"/>，不做隐式回退。
    /// </summary>
    public sealed class CharacterShowcaseWindow : EditorWindow
    {
        // 与 23 个环境之外的正式角色 manifest 声明的核心动作对齐；
        // 必须保持 attack_1/skill_1 带下划线，以匹配 Animator 状态名（attack_1_down 等）。
        private static readonly HashSet<string> SupportedActionSet =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "idle", "walk", "dash",
                "attack_1", "attack_2", "attack_3",
                "skill_1", "skill_2",
                "hurt", "death",
            };

        private static readonly HashSet<int> SupportedScaleSet = new HashSet<int> { 1, 4, 8 };

        /// <summary>总览窗口支持预览的动作集合（稳定契约，供测试断言）。</summary>
        public static IReadOnlyCollection<string> SupportedActions => SupportedActionSet;

        /// <summary>总览窗口支持的预览缩放集合（稳定契约，供测试断言）。</summary>
        public static IReadOnlyCollection<int> SupportedScales => SupportedScaleSet;

        private const string DefaultDirection = "down";

        private CharacterArtCatalog catalog;
        private string selectedId;
        private GameObject previewInstance;
        private Animator previewAnimator;
        private int previewScale = 1;
        private string currentAction = "idle";

        /// <summary>当前预览目标的 Animator；测试可直接赋值以驱动 <see cref="PreviewAction"/>。</summary>
        public Animator PreviewAnimator
        {
            get => previewAnimator;
            set => previewAnimator = value;
        }

        /// <summary>当前生效的预览缩放（已校验）。</summary>
        public int PreviewScale => previewScale;

        /// <summary>最近一次预览的动作（已校验）。</summary>
        public string CurrentAction => currentAction;

        [MenuItem("Tools/渊海录/美术/角色总览窗口")]
        public static void Open()
        {
            GetWindow<CharacterShowcaseWindow>(false, "角色总览", true);
        }

        /// <summary>
        /// 预览某个支持的动作。未知动作抛 <see cref="ArgumentException"/>；
        /// 已绑定 <see cref="PreviewAnimator"/> 时直接跳到当前方向的对应状态。
        /// </summary>
        public void PreviewAction(string action)
        {
            if (!SupportedActionSet.Contains(action))
                throw new ArgumentException(
                    "Unknown showcase action '" + action + "'. Supported: " +
                    string.Join(", ", SupportedActionSet.OrderBy(value => value, StringComparer.Ordinal)) + ".",
                    nameof(action));
            currentAction = action;
            if (previewAnimator == null)
                return;
            previewAnimator.Play(action + "_" + DefaultDirection, 0, 0f);
        }

        /// <summary>切换预览缩放。未知缩放抛 <see cref="ArgumentException"/>。</summary>
        public void SetPreviewScale(int scale)
        {
            if (!SupportedScaleSet.Contains(scale))
                throw new ArgumentException(
                    "Unknown showcase scale " + scale + ". Supported: " +
                    string.Join(", ", SupportedScaleSet.OrderBy(value => value)) + ".",
                    nameof(scale));
            previewScale = scale;
            ApplyScale();
        }

        private void ApplyScale()
        {
            if (previewInstance == null)
                return;
            previewInstance.transform.localScale = Vector3.one * previewScale;
        }

        private void OnEnable()
        {
            try
            {
                catalog = CharacterArtCatalog.LoadDefault();
            }
            catch (InvalidOperationException)
            {
                catalog = null;
            }
            selectedId = catalog != null && catalog.Entries.Count > 0 ? catalog.Entries[0].Id : null;
        }

        private void OnDisable()
        {
            DestroyPreviewInstance();
        }

        private void OnGUI()
        {
            if (catalog == null)
            {
                EditorGUILayout.HelpBox("未加载到正式角色目录。请先运行 Tools/渊海录/美术/重建角色动画与Prefab。", MessageType.Warning);
                return;
            }

            var ids = catalog.Entries.Select(entry => entry.Id).ToArray();
            int currentIndex = ids.Length == 0 ? -1 : Array.IndexOf(ids, selectedId);
            int newIndex = EditorGUILayout.Popup("角色", currentIndex, ids);
            if (newIndex >= 0 && newIndex < ids.Length && ids[newIndex] != selectedId)
                SelectEntry(ids[newIndex]);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("动作", EditorStyles.boldLabel);
            foreach (var action in SupportedActionSet.OrderBy(value => value, StringComparer.Ordinal))
            {
                string label = action == currentAction ? "» " + action : action;
                if (GUILayout.Button(label))
                    PreviewAction(action);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("缩放", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (var scale in SupportedScaleSet.OrderBy(value => value))
                {
                    string label = scale == previewScale ? "» x" + scale : "x" + scale;
                    if (GUILayout.Button(label))
                        SetPreviewScale(scale);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "选中角色会在当前场景生成一个临时预览实例 __CharacterShowcasePreview，" +
                "关闭窗口或切换角色时自动清理。",
                MessageType.Info);
        }

        private void SelectEntry(string id)
        {
            selectedId = id;
            DestroyPreviewInstance();
            if (catalog.TryGet(id, out var entry) && entry.Prefab != null)
            {
                previewInstance = (GameObject)PrefabUtility.InstantiatePrefab(entry.Prefab);
                previewInstance.name = "__CharacterShowcasePreview";
                previewInstance.transform.position = Vector3.zero;
                previewAnimator = previewInstance.GetComponent<Animator>();
                ApplyScale();
                PreviewAction(currentAction);
            }
        }

        private void DestroyPreviewInstance()
        {
            if (previewInstance == null)
                return;
            UnityEngine.Object.DestroyImmediate(previewInstance);
            previewInstance = null;
            previewAnimator = null;
        }
    }
}
