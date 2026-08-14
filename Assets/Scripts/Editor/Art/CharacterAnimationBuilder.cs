using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using YuanHaiLu.Art;

namespace YuanHaiLu.Editor
{
    public static class CharacterAnimationBuilder
    {
        private const string ControllerRoot = "Assets/AnimatorControllers/Characters";
        private const string PrefabRoot = "Assets/Prefabs/Characters";
        private const string AnimationRoot = "Assets/Animations/Characters";
        private const string StampPath = AnimationRoot + "/formal-character-build.txt";
        private const string BuilderVersion = "character-builder-v4";

        [MenuItem("Tools/渊海录/美术/重建角色动画与Prefab")]
        public static void RebuildAll()
        {
            RebuildAll(false);
        }

        public static void RebuildAll(bool force)
        {
            var metadataPaths = ArtImportRules.EnumerateMetadataAssetPaths()
                .Where(path => string.Equals(
                    ArtImportRules.ReadMetadataAtPath(path).kind,
                    "character",
                    StringComparison.Ordinal))
                .ToArray();
            var stamp = BuildStamp(metadataPaths);
            if (!force && IsCurrent(stamp, metadataPaths.Length))
                return;

            ArtImportRules.ApplyAllFormal();
            var report = ArtAssetValidator.ValidateAll();
            if (!report.IsValid)
                throw new InvalidOperationException(report.ToString());

            if (AssetDatabase.IsValidFolder(ControllerRoot))
                AssetDatabase.DeleteAsset(ControllerRoot);
            if (AssetDatabase.IsValidFolder(PrefabRoot))
                AssetDatabase.DeleteAsset(PrefabRoot);
            EnsureFolder("Assets/AnimatorControllers");
            EnsureFolder(ControllerRoot);
            EnsureFolder("Assets/Prefabs");
            EnsureFolder(PrefabRoot);
            EnsureFolder("Assets/Animations");
            EnsureFolder(AnimationRoot);

            foreach (var metadataPath in metadataPaths)
            {
                var metadata = ArtImportRules.ReadMetadataAtPath(metadataPath);
                BuildCharacter(metadataPath, metadata);
            }

            File.WriteAllText(StampPath, stamp, Encoding.UTF8);
            AssetDatabase.ImportAsset(StampPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.SaveAssets();
            ArtCatalogBuilder.RebuildAll();
            Debug.Log($"[CharacterAnimationBuilder] generated={metadataPaths.Length}");
        }

        public static void RebuildFromCommandLine()
        {
            RebuildAll(true);
        }

        private static void BuildCharacter(string metadataPath, ArtMetadata metadata)
        {
            var directory = Path.GetDirectoryName(metadataPath) ?? string.Empty;
            var sheetPath = Path.Combine(directory, metadata.image).Replace('\\', '/');
            var sprites = AssetDatabase.LoadAllAssetsAtPath(sheetPath)
                .OfType<Sprite>()
                .ToDictionary(sprite => sprite.name, StringComparer.Ordinal);
            var folder = ArtCatalogBuilder.CharacterCategoryFolder(metadata.id);
            EnsureFolder(ControllerRoot + "/" + folder);
            EnsureFolder(PrefabRoot + "/" + folder);
            var controllerPath = ArtCatalogBuilder.CharacterControllerPath(metadata.id);
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
                AssetDatabase.DeleteAsset(controllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            AddParameters(controller);

            var states = new Dictionary<string, AnimatorState>(StringComparer.Ordinal);
            var stateMachine = controller.layers[0].stateMachine;
            foreach (var animation in metadata.animations)
            {
                var stateName = animation.name + "_" + animation.direction;
                var clip = BuildClip(metadata.id, animation, sprites);
                clip.name = stateName;
                AssetDatabase.AddObjectToAsset(clip, controller);
                var state = stateMachine.AddState(stateName);
                state.motion = clip;
                states[stateName] = state;
            }
            if (states.TryGetValue("idle_down", out var idle))
                stateMachine.defaultState = idle;
            AddBasicTransitions(stateMachine, states);
            EditorUtility.SetDirty(controller);

            var prefabObject = new GameObject(metadata.id);
            try
            {
                var renderer = prefabObject.AddComponent<SpriteRenderer>();
                var idleName = metadata.id + "__idle__down__0";
                if (!sprites.TryGetValue(idleName, out var idleSprite))
                    throw new InvalidOperationException($"Missing '{idleName}' in '{sheetPath}'.");
                renderer.sprite = idleSprite;
                renderer.sortingLayerName = "Character";
                var animator = prefabObject.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                var visual = prefabObject.AddComponent<CharacterVisual>();
                visual.ConfigureForEditor(metadata.id, renderer, animator);
                PrefabUtility.SaveAsPrefabAsset(
                    prefabObject,
                    ArtCatalogBuilder.CharacterPrefabPath(metadata.id));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefabObject);
            }
        }

        private static AnimationClip BuildClip(
            string artId,
            ArtAnimationMetadata animation,
            IReadOnlyDictionary<string, Sprite> sprites)
        {
            var clip = new AnimationClip { frameRate = animation.fps };
            var keys = new ObjectReferenceKeyframe[animation.frames.Length];
            for (var index = 0; index < animation.frames.Length; index++)
            {
                if (!sprites.TryGetValue(animation.frames[index], out var sprite))
                    throw new InvalidOperationException(
                        $"Missing sprite '{animation.frames[index]}' for '{artId}'.");
                keys[index] = new ObjectReferenceKeyframe
                {
                    time = index / (float)animation.fps,
                    value = sprite
                };
            }
            AnimationUtility.SetObjectReferenceCurve(
                clip,
                EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite"),
                keys);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = animation.loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            if (artId.StartsWith("player_", StringComparison.Ordinal) &&
                animation.name.StartsWith("attack_", StringComparison.Ordinal))
            {
                var events = new List<AnimationEvent>();
                foreach (var frame in animation.hitFrames ?? Array.Empty<int>())
                {
                    events.Add(new AnimationEvent
                    {
                        functionName = "OnAttackHitFrame",
                        time = frame / (float)animation.fps
                    });
                }
                events.Add(new AnimationEvent
                {
                    functionName = "OnAttackAnimationEnd",
                    time = Math.Max(0, animation.frames.Length - 1) / (float)animation.fps
                });
                AnimationUtility.SetAnimationEvents(clip, events.ToArray());
            }
            return clip;
        }

        private static void AddParameters(AnimatorController controller)
        {
            controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
            controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsDashing", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
            controller.AddParameter("AttackIndex", AnimatorControllerParameterType.Int);
            controller.AddParameter("Facing", AnimatorControllerParameterType.Int);
        }

        private static void AddBasicTransitions(
            AnimatorStateMachine stateMachine,
            IReadOnlyDictionary<string, AnimatorState> states)
        {
            var directions = new[] { "down", "left", "right", "up" };
            for (var facing = 0; facing < directions.Length; facing++)
            {
                var direction = directions[facing];
                if (states.TryGetValue("idle_" + direction, out var idle))
                {
                    var idleTransition = AddAnyStateTransition(stateMachine, idle);
                    idleTransition.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsAttacking");
                    idleTransition.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDashing");
                    idleTransition.AddCondition(AnimatorConditionMode.Less, 0.01f, "Speed");
                    idleTransition.AddCondition(AnimatorConditionMode.Equals, facing, "Facing");
                }

                if (states.TryGetValue("walk_" + direction, out var walk))
                {
                    var walkTransition = AddAnyStateTransition(stateMachine, walk);
                    walkTransition.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsAttacking");
                    walkTransition.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDashing");
                    walkTransition.AddCondition(AnimatorConditionMode.Greater, 0.01f, "Speed");
                    walkTransition.AddCondition(AnimatorConditionMode.Equals, facing, "Facing");
                }

                if (states.TryGetValue("dash_" + direction, out var dash))
                {
                    var dashTransition = AddAnyStateTransition(stateMachine, dash);
                    dashTransition.AddCondition(AnimatorConditionMode.If, 0f, "IsDashing");
                    dashTransition.AddCondition(AnimatorConditionMode.Equals, facing, "Facing");
                    if (states.TryGetValue("idle_" + direction, out idle))
                    {
                        var dashEnd = dash.AddTransition(idle);
                        dashEnd.hasExitTime = true;
                        dashEnd.exitTime = 1f;
                        dashEnd.duration = 0f;
                        dashEnd.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDashing");
                    }
                }

                for (var attackIndex = 0; attackIndex < 3; attackIndex++)
                {
                    var stateName = "attack_" + (attackIndex + 1) + "_" + direction;
                    if (!states.TryGetValue(stateName, out var attack))
                        continue;

                    var attackTransition = AddAnyStateTransition(stateMachine, attack);
                    attackTransition.AddCondition(AnimatorConditionMode.If, 0f, "IsAttacking");
                    attackTransition.AddCondition(AnimatorConditionMode.Equals, attackIndex, "AttackIndex");
                    attackTransition.AddCondition(AnimatorConditionMode.Equals, facing, "Facing");
                    if (states.TryGetValue("idle_" + direction, out idle))
                    {
                        var attackEnd = attack.AddTransition(idle);
                        attackEnd.hasExitTime = true;
                        attackEnd.exitTime = 1f;
                        attackEnd.duration = 0f;
                        attackEnd.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsAttacking");
                    }
                }
            }
        }

        private static AnimatorStateTransition AddAnyStateTransition(
            AnimatorStateMachine stateMachine,
            AnimatorState target)
        {
            var transition = stateMachine.AddAnyStateTransition(target);
            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.canTransitionToSelf = false;
            return transition;
        }

        private static string BuildStamp(IEnumerable<string> metadataPaths)
        {
            var builder = new StringBuilder(BuilderVersion).AppendLine();
            foreach (var path in metadataPaths.OrderBy(value => value, StringComparer.Ordinal))
            {
                var metadata = ArtImportRules.ReadMetadataAtPath(path);
                builder.Append(metadata.id).Append(':').Append(metadata.sha256).AppendLine();
            }
            return builder.ToString();
        }

        private static bool IsCurrent(string stamp, int expectedCount)
        {
            if (!File.Exists(StampPath) || File.ReadAllText(StampPath, Encoding.UTF8) != stamp)
                return false;
            var catalog = AssetDatabase.LoadAssetAtPath<CharacterArtCatalog>(ArtCatalogBuilder.CharacterCatalogPath);
            return catalog != null && catalog.Entries.Count == expectedCount &&
                catalog.Entries.All(entry => entry.Controller != null && entry.Prefab != null);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
