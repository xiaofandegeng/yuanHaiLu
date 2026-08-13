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
    public static class CharacterShowcaseGenerator
    {
        public const string ScenePath = "Assets/Scenes/CharacterShowcase.unity";

        [MenuItem("Tools/渊海录/美术/生成角色总览场景")]
        public static void Generate()
        {
            CharacterAnimationBuilder.RebuildAll();
            var catalog = CharacterArtCatalog.LoadDefault();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "CharacterShowcase";

            var cameraObject = new GameObject("ShowcaseCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 18f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(24, 31, 36, 255);
            cameraObject.transform.position = new Vector3(10f, -12f, -10f);

            var categoryOrder = new[] { "player", "named", "npc", "enemy", "boss" };
            var roots = new Dictionary<string, Transform>(StringComparer.Ordinal);
            foreach (var category in categoryOrder)
                roots[category] = new GameObject(category.ToUpperInvariant()).transform;

            var y = 4f;
            foreach (var category in categoryOrder)
            {
                var entries = catalog.Entries
                    .Where(entry => entry.Category == category)
                    .OrderBy(entry => entry.Id, StringComparer.Ordinal)
                    .ToArray();
                for (var index = 0; index < entries.Length; index++)
                {
                    var entry = entries[index];
                    if (entry.Prefab == null)
                        throw new InvalidOperationException($"'{entry.Id}' has no formal prefab.");
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(entry.Prefab, scene);
                    instance.name = entry.Id;
                    instance.transform.SetParent(roots[category]);
                    instance.transform.position = new Vector3((index % 12) * 2f, y - (index / 12) * 2.4f, 0f);
                    CreateLabel(instance.transform, entry.Id);
                }
                y -= ((entries.Length + 11) / 12) * 2.4f + 1.5f;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[CharacterShowcaseGenerator] entries={catalog.Entries.Count}, scene={ScenePath}");
        }

        public static void GenerateFromCommandLine()
        {
            Generate();
        }

        /// <summary>
        /// 在每个角色 Prefab 上方生成一个显示稳定 ID 的 <see cref="TextMesh"/>，
        /// SortingLayer=UI 以确保总览里标签始终盖在角色之上，便于人工核对外观与 ID。
        /// </summary>
        private static void CreateLabel(Transform parent, string stableId)
        {
            var labelObject = new GameObject(stableId + "__label");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            var text = labelObject.AddComponent<TextMesh>();
            text.text = stableId;
            text.fontSize = 24;
            text.characterSize = 0.28f;
            text.anchor = TextAnchor.LowerCenter;
            text.alignment = TextAlignment.Center;
            text.color = new Color(1f, 1f, 1f, 0.92f);
            var labelRenderer = labelObject.GetComponent<MeshRenderer>();
            labelRenderer.sortingLayerName = "UI";
            labelRenderer.sortingOrder = 10;
        }
    }
}
