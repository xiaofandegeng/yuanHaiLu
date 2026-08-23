using System;
using System.Collections.Generic;
using UnityEngine;

namespace YuanHaiLu.Art
{
    [Serializable]
    public sealed class SceneAnchorDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private string type;
        [SerializeField] private Vector2Int cell;

        public string Id => id;
        public string Type => type;
        public Vector2Int Cell => cell;

        public SceneAnchorDefinition(string stableId, string anchorType, Vector2Int anchorCell)
        {
            id = stableId;
            type = anchorType;
            cell = anchorCell;
        }
    }

    public sealed class RegionSceneDefinition : MonoBehaviour
    {
        [SerializeField] private string sceneId;
        [SerializeField] private string kind;
        [SerializeField] private Vector2Int size;
        [SerializeField] private List<SceneAnchorDefinition> anchors = new List<SceneAnchorDefinition>();
        [SerializeField] private bool supportsDayNight;
        [SerializeField] private string weatherId;

        public string SceneId => sceneId;
        public string Kind => kind;
        public Vector2Int Size => size;
        public IReadOnlyList<SceneAnchorDefinition> Anchors => anchors;
        public bool SupportsDayNight => supportsDayNight;
        public string WeatherId => weatherId;

        public void ConfigureForEditor(
            string stableSceneId,
            string sceneKind,
            Vector2Int sceneSize,
            IEnumerable<SceneAnchorDefinition> sceneAnchors,
            bool hasDayNight = true,
            string formalWeatherId = "clear")
        {
            sceneId = stableSceneId;
            kind = sceneKind;
            size = sceneSize;
            anchors = new List<SceneAnchorDefinition>(sceneAnchors);
            supportsDayNight = hasDayNight;
            weatherId = formalWeatherId;
        }
    }
}
