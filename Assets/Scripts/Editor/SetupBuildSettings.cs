using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace YuanHaiLu.Editor
{
    public static class SetupBuildSettings
    {
        public static void Setup()
        {
            var scenes = new[]
            {
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/Demo_YanLiuTown.unity"
            };

            var buildScenes = new EditorBuildSettingsScene[scenes.Length];
            for (int i = 0; i < scenes.Length; i++)
            {
                buildScenes[i] = new EditorBuildSettingsScene(scenes[i], true);
            }

            EditorBuildSettings.scenes = buildScenes;
            Debug.Log($"[BuildSettings] 已添加 {scenes.Length} 个场景");
            foreach (var s in scenes) Debug.Log("  - " + s);
        }
    }
}
