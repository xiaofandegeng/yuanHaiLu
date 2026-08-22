using System.Collections;
using UnityEngine;

namespace YuanHaiLu.Core
{
    /// <summary>
    /// Keeps a standalone MVP gameplay scene usable when an author presses Play
    /// directly in the Unity editor instead of entering through MainMenu.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MvpDirectPlayFallback : MonoBehaviour
    {
        private IEnumerator Start()
        {
            // GameManager.Start normally runs in the same frame and sets MainMenu.
            // Wait once so this fallback observes the settled startup state.
            yield return null;
            ActivateIfDirectPlay();
        }

        public void ActivateIfDirectPlay()
        {
            var gameManager = GameManager.Instance;
            if (gameManager == null ||
                gameManager.currentState != GameManager.GameState.MainMenu)
                return;

            gameManager.BeginSceneEntry(GameManager.SceneEntryMode.NewGame);
            gameManager.SetState(GameManager.GameState.Exploration);
            gameManager.CompleteSceneEntry();
            Debug.Log("[MvpDirectPlayFallback] 直接试玩场景已进入探索状态。");
        }
    }
}
