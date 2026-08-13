using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YuanHaiLu.Editor
{
    public static class ArtReferencePreviewGenerator
    {
        public const string ScenePath = "Assets/Scenes/ArtReference.unity";

        [MenuItem("Tools/渊海录/美术/生成参考预览场景")]
        public static void Generate()
        {
            ArtCatalogBuilder.RebuildAll();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "ArtReference";

            var cameraObject = new GameObject("ReferenceCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8.4375f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(24, 31, 36, 255);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var environmentRoot = new GameObject("YanliuEnvironment");
            var previewPath = "Assets/Art/Environment/Regions/yanliu/yanliu_reference.png";
            var previewSprite = AssetDatabase.LoadAssetAtPath<Sprite>(previewPath);
            if (previewSprite == null)
                throw new InvalidOperationException($"Missing preview sprite '{previewPath}'.");
            var previewObject = new GameObject("YanliuReferencePreview");
            previewObject.transform.SetParent(environmentRoot.transform);
            var previewRenderer = previewObject.AddComponent<SpriteRenderer>();
            previewRenderer.sprite = previewSprite;
            previewRenderer.sortingOrder = 0;

            var characterRoot = new GameObject("ReferenceCharacters");
            AddCharacterSamples(
                characterRoot.transform,
                "Assets/Art/Characters/Player/player_male_swordsman.png",
                "player_male_swordsman",
                -5.5f);
            AddCharacterSamples(
                characterRoot.transform,
                "Assets/Art/Characters/Player/player_female_swordsman.png",
                "player_female_swordsman",
                -7.1f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ArtReferencePreviewGenerator] Saved {ScenePath}");
        }

        public static void GenerateFromCommandLine()
        {
            Generate();
        }

        private static void AddCharacterSamples(
            Transform parent,
            string sheetPath,
            string characterId,
            float y)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(sheetPath).OfType<Sprite>().ToArray();
            var actions = new[] { "idle", "walk", "dash", "attack_1", "attack_2", "attack_3", "skill_1", "skill_2", "hurt", "dodge", "down", "death" };
            for (var index = 0; index < actions.Length; index++)
            {
                var expectedName = $"{characterId}__{actions[index]}__down__0";
                var sprite = sprites.FirstOrDefault(candidate => candidate.name == expectedName);
                if (sprite == null)
                    throw new InvalidOperationException($"Missing reference sprite '{expectedName}'.");
                var sample = new GameObject(expectedName);
                sample.transform.SetParent(parent);
                sample.transform.position = new Vector3(-11f + index * 2f, y, 0f);
                var renderer = sample.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = 10;
            }
        }
    }
}
