using System;
using UnityEngine;
using YuanHaiLu.Core;

namespace YuanHaiLu.Art
{
    /// <summary>
    /// Demo-only persistent sprite binding for the MVP innkeeper, bandits and quest pouch.
    /// It deliberately does not alter the frozen formal-character catalog or its controllers.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MvpStaticVisual : MonoBehaviour
    {
        [SerializeField] private string spriteId;

        public string SpriteId => spriteId;

        public static MvpStaticVisual ApplyTo(GameObject target, string persistentSpriteId)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            var visual = target.GetComponent<MvpStaticVisual>();
            if (visual == null)
                visual = target.AddComponent<MvpStaticVisual>();
            visual.Apply(persistentSpriteId);
            return visual;
        }

        public void Apply(string persistentSpriteId)
        {
            var sprite = MvpArtCatalog.Load(persistentSpriteId);
            if (sprite == null)
                throw new InvalidOperationException(
                    "Missing persistent MVP actor sprite '" + persistentSpriteId + "'.");

            var renderer = GetComponent<SpriteRenderer>();
            if (renderer == null)
                renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = GameConfig.SORTING_CHARACTER;
            renderer.sortingOrder = 0;

            // Static supporting actors must not be overwritten by one of the frozen
            // formal Animator controllers on the next frame.
            var targetAnimator = GetComponent<Animator>();
            if (targetAnimator != null)
                targetAnimator.runtimeAnimatorController = null;
            spriteId = persistentSpriteId;
        }
    }
}
