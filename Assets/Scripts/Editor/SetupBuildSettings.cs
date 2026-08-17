using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace YuanHaiLu.Editor
{
    public static class SetupBuildSettings
    {
        public static void Setup()
        {
            var scenes = new List<string>
            {
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/Demo_YanLiuTown.unity",
                "Assets/Scenes/Demo_Inn.unity"
            };

            scenes.AddRange(Directory.GetFiles("Assets/Scenes/Regions", "*.unity")
                .Select(path => path.Replace('\\', '/'))
                .OrderBy(path => path, System.StringComparer.Ordinal));
            scenes.AddRange(Directory.GetFiles("Assets/Scenes/Interiors", "*.unity")
                .Select(path => path.Replace('\\', '/'))
                .OrderBy(path => path, System.StringComparer.Ordinal));

            var buildScenes = new EditorBuildSettingsScene[scenes.Count];
            for (int i = 0; i < scenes.Count; i++)
            {
                buildScenes[i] = new EditorBuildSettingsScene(scenes[i], true);
            }

            EditorBuildSettings.scenes = buildScenes;
            Debug.Log($"[BuildSettings] 已添加 {scenes.Count} 个场景");
            foreach (var s in scenes) Debug.Log("  - " + s);
        }
    }
}
