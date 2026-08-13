using System;
using UnityEngine;

namespace YuanHaiLu.Art
{
    /// <summary>
    /// Binds one stable formal-art ID to a renderer and animator without creating pixels.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterVisual : MonoBehaviour
    {
        [SerializeField] private string artId;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;

        public string ArtId => artId;

        public static CharacterVisual ApplyTo(GameObject target, string stableArtId)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            var visual = target.GetComponent<CharacterVisual>();
            if (visual == null)
                visual = target.AddComponent<CharacterVisual>();
            visual.Apply(stableArtId);
            return visual;
        }

        public void Apply(string stableArtId)
        {
            var catalog = CharacterArtCatalog.LoadDefault();
            if (!catalog.TryGet(stableArtId, out var entry))
                throw new InvalidOperationException($"Unknown formal character art id '{stableArtId}'.");
            if (entry.Controller == null || entry.Prefab == null)
                throw new InvalidOperationException($"Formal character art '{stableArtId}' is not fully generated.");

            var prefabRenderer = entry.Prefab.GetComponent<SpriteRenderer>();
            if (prefabRenderer == null || prefabRenderer.sprite == null)
                throw new InvalidOperationException($"Formal character prefab '{stableArtId}' has no persistent sprite.");

            // UnityEngine.Object uses a native "fake null" state; do not use ?? here.
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            if (animator == null)
                animator = GetComponent<Animator>();
            if (animator == null)
                animator = gameObject.AddComponent<Animator>();
            spriteRenderer.sprite = prefabRenderer.sprite;
            animator.runtimeAnimatorController = entry.Controller;
            artId = stableArtId;
        }

        public void ConfigureForEditor(
            string stableArtId,
            SpriteRenderer renderer,
            Animator targetAnimator)
        {
            artId = stableArtId;
            spriteRenderer = renderer;
            animator = targetAnimator;
        }
    }
}
