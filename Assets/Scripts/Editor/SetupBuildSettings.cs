using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using YuanHaiLu.Art;

namespace YuanHaiLu.Editor
{
    public static class SetupBuildSettings
    {
        public static void Setup()
        {
            var scenes = CanonicalScenePaths();

            var buildScenes = new EditorBuildSettingsScene[scenes.Count];
            for (int i = 0; i < scenes.Count; i++)
            {
                buildScenes[i] = new EditorBuildSettingsScene(scenes[i], true);
            }

            EditorBuildSettings.scenes = buildScenes;
            Debug.Log($"[BuildSettings] 已添加 {scenes.Count} 个场景");
            foreach (var s in scenes) Debug.Log("  - " + s);
        }

        public static IReadOnlyList<string> CanonicalScenePaths()
        {
            var scenes = new List<string>
            {
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/Demo_YanLiuTown.unity"
            };
            var catalog = EnvironmentArtCatalog.LoadDefault();
            scenes.AddRange(catalog.Entries
                .OrderBy(entry => entry.Kind == "region" ? 0 : 1)
                .ThenBy(entry => entry.SceneAssetPath, StringComparer.Ordinal)
                .Select(entry => entry.SceneAssetPath));

            if (catalog.Entries.Count != 23 || scenes.Distinct(StringComparer.Ordinal).Count() != 25)
                throw new InvalidOperationException(
                    "Formal build catalog must resolve to 23 unique scenes plus MainMenu and Demo.");
            foreach (string scene in scenes)
            {
                if (!File.Exists(scene))
                    throw new FileNotFoundException(
                        "Canonical build scene is missing; regenerate formal scenes before setup.",
                        scene);
            }
            return scenes;
        }
    }
}
