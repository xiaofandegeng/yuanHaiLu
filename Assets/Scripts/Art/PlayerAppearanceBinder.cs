using UnityEngine;
using YuanHaiLu.Core;

namespace YuanHaiLu.Art
{
    /// <summary>
    /// Re-applies the persistent player selection after each scene load.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterVisual))]
    public sealed class PlayerAppearanceBinder : MonoBehaviour
    {
        private void Start()
        {
            ApplyCurrentAppearance();
        }

        public void ApplyCurrentAppearance()
        {
            string artId = GameManager.Instance != null
                ? GameManager.Instance.PlayerArtId
                : PlayerAppearance.Default.ArtId;
            CharacterVisual.ApplyTo(gameObject, artId);
        }
    }
}
