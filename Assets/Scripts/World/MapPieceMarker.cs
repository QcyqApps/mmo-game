using UnityEngine;

namespace MmoGame.World
{
    public enum MapMarkerKind { Piece, Tiling }

    /// <summary>
    /// Stamped onto every instance MapLoader spawns so the Map Editor can
    /// resolve a scene GameObject back to its entry in the JSON manifest.
    /// Without it, round-trip editing (move in scene → save to JSON) has
    /// no way to know which piece index a given instance belongs to.
    /// </summary>
    [DisallowMultipleComponent]
    public class MapPieceMarker : MonoBehaviour
    {
        public MapMarkerKind kind;
        public int pieceIndex = -1;
        public int tilingIndex = -1;
        public int tilingIx, tilingIy, tilingIz;
        public string prefabName;
        public string groupName;
    }
}
