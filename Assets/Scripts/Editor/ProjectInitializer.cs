using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace YuanHaiLu.Editor
{
    /// <summary>
    /// 项目初始化器 — 通过命令行执行
    /// 设置标签、Layer、Sorting Layer，然后生成场景
    /// </summary>
    public static class ProjectInitializer
    {
        public static void Init()
        {
            Debug.Log("=== 渊海录 项目初始化 ===");

            // 1. 设置 Tags
            SetupTags();

            // 2. 设置 Layers
            SetupLayers();

            // 3. 设置 Sorting Layers
            SetupSortingLayers();

            // 4. 生成主菜单场景
            MainMenuSceneGenerator.Generate();

            // 5. 生成 Demo 场景
            DemoSceneGenerator.Generate();

            // 6. 保存
            AssetDatabase.SaveAssets();
            Debug.Log("=== 初始化完成！请打开 Demo_YanLiuTown 场景按 Play ===");
        }

        private static void SetupTags()
        {
            var tags = new string[] { "Player", "Enemy", "NPC", "Item", "Environment", "Trigger" };
            var serializedObject = new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/TagManager.asset"));
            var tagsProp = serializedObject.FindProperty("tags");

            tagsProp.ClearArray();
            for (int i = 0; i < tags.Length; i++)
            {
                tagsProp.InsertArrayElementAtIndex(i);
                tagsProp.GetArrayElementAtIndex(i).stringValue = tags[i];
            }

            serializedObject.ApplyModifiedProperties();
            Debug.Log("[Tags] 设置完成: " + string.Join(", ", tags));
        }

        private static void SetupLayers()
        {
            var serializedObject = new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/TagManager.asset"));
            var layersProp = serializedObject.FindProperty("layers");

            // Layer 6 = Player, 7 = Enemy, 8 = NPC, 9 = Environment
            SetLayer(layersProp, 6, "Player");
            SetLayer(layersProp, 7, "Enemy");
            SetLayer(layersProp, 8, "NPC");
            SetLayer(layersProp, 9, "Environment");

            serializedObject.ApplyModifiedProperties();
            Debug.Log("[Layers] 设置完成");
        }

        private static void SetLayer(SerializedProperty layersProp, int index, string name)
        {
            var layerProp = layersProp.GetArrayElementAtIndex(index);
            layerProp.stringValue = name;
        }

        private static void SetupSortingLayers()
        {
            var tagManager = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/TagManager.asset");
            var serializedObject = new SerializedObject(tagManager);
            var sortingLayers = serializedObject.FindProperty("m_SortingLayers");

            sortingLayers.ClearArray();

            var layers = new (string name, int id)[]
            {
                ("Background", 0),
                ("Terrain", 1),
                ("Decoration", 2),
                ("Character", 3),
                ("Foreground", 4),
                ("UI", 5),
            };

            for (int i = 0; i < layers.Length; i++)
            {
                sortingLayers.InsertArrayElementAtIndex(i);
                var layer = sortingLayers.GetArrayElementAtIndex(i);
                layer.FindPropertyRelative("name").stringValue = layers[i].name;
                layer.FindPropertyRelative("uniqueID").intValue = layers[i].id;
                layer.FindPropertyRelative("locked").boolValue = false;
            }

            serializedObject.ApplyModifiedProperties();
            Debug.Log("[SortingLayers] 设置完成: " + string.Join(", ", System.Array.ConvertAll(layers, l => l.name)));
        }
    }
}
