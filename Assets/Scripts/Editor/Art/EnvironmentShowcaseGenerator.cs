using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using YuanHaiLu.Art;

namespace YuanHaiLu.Editor
{
    public static class EnvironmentShowcaseGenerator
    {
        public const string ScenePath = "Assets/Scenes/EnvironmentShowcase.unity";

        [MenuItem("Tools/渊海录/美术/生成环境总览场景")]
        public static void Generate()
        {
            RegionSceneBuilder.BuildAll();
            var catalog = EnvironmentArtCatalog.LoadDefault();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "EnvironmentShowcase";
            var cameraObject = new GameObject("ShowcaseCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 18f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(24, 31, 36, 255);
            cameraObject.transform.position = new Vector3(12f, -8f, -10f);

            for (var index = 0; index < catalog.Entries.Count; index++)
            {
                var entry = catalog.Entries[index];
                var group = new GameObject(entry.RegionId);
                group.transform.position = new Vector3((index % 6) * 5f, -(index / 6) * 5f, 0f);
                var tileSprite = AssetDatabase.LoadAllAssetsAtPath(
                        AssetDatabase.GetAssetPath(entry.Tileset))
                    .OfType<Sprite>()
                    .FirstOrDefault();
                var landmarkSprite = AssetDatabase.LoadAllAssetsAtPath(
                        AssetDatabase.GetAssetPath(entry.Landmarks))
                    .OfType<Sprite>()
                    .FirstOrDefault();
                if (tileSprite == null || landmarkSprite == null)
                    throw new InvalidOperationException($"'{entry.RegionId}' has incomplete formal sprites.");
                var ground = new GameObject("GroundSample");
                ground.transform.SetParent(group.transform);
                ground.transform.localScale = new Vector3(4f, 4f, 1f);
                ground.AddComponent<SpriteRenderer>().sprite = tileSprite;
                var landmark = new GameObject("LandmarkSample");
                landmark.transform.SetParent(group.transform);
                landmark.transform.localPosition = new Vector3(0f, 0.6f, 0f);
                var renderer = landmark.AddComponent<SpriteRenderer>();
                renderer.sprite = landmarkSprite;
                renderer.sortingOrder = 2;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[EnvironmentShowcaseGenerator] entries={catalog.Entries.Count}");
        }

        public static void GenerateFromCommandLine()
        {
            Generate();
        }
    }
}
