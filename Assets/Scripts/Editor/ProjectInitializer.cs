using UnityEngine;
using UnityEditor;
using YuanHaiLu.Core;

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

            // 1-3. 设置 Tags / Layers / Sorting Layers
            ConfigureProjectSettings();

            // 4. 生成主菜单场景
            MainMenuSceneGenerator.Generate();

            // 5. 生成 Demo 场景
            DemoSceneGenerator.Generate();

            // 6. 保存
            AssetDatabase.SaveAssets();
            Debug.Log("=== 初始化完成！请打开 Demo_YanLiuTown 场景按 Play ===");
        }

        internal static void ConfigureProjectSettings()
        {
            SetupTags();
            SetupLayers();
            SetupSortingLayers();
            AssetDatabase.SaveAssets();
        }

        private static void SetupTags()
        {
            var tags = new[] { "Player", "Enemy", "NPC", "Item", "Environment", "Trigger" };
            var serializedObject = new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/TagManager.asset"));
            var tagsProp = serializedObject.FindProperty("tags");

            foreach (string tag in tags)
            {
                bool exists = false;
                for (int i = 0; i < tagsProp.arraySize; i++)
                {
                    if (tagsProp.GetArrayElementAtIndex(i).stringValue != tag) continue;
                    exists = true;
                    break;
                }

                if (exists) continue;
                int index = tagsProp.arraySize;
                tagsProp.InsertArrayElementAtIndex(index);
                tagsProp.GetArrayElementAtIndex(index).stringValue = tag;
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
                (GameConfig.SORTING_GROUND, 0),
                (GameConfig.SORTING_ENVIRONMENT, 1),
                (GameConfig.SORTING_CHARACTER, 2),
                (GameConfig.SORTING_FOREGROUND, 3),
                (GameConfig.SORTING_UI, 4),
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
