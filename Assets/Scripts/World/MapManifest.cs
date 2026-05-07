using System;

namespace MmoGame.World
{
    [Serializable]
    public class MapPiece
    {
        public string prefab;            // logical name → MapPrefabRegistry lookup
        public float[] position;         // world-space [x,y,z]
        public float[] rotation;         // euler degrees [x,y,z]
        public float[] scale;            // optional, defaults to [1,1,1]
        public string parent;            // optional group name (empty GO under map root)
        public string note;              // optional author comment, ignored at runtime
    }

    /// <summary>
    /// Declarative grid placement — replaces dozens of hand-written MapPiece
    /// entries with a single bounding box + step. Iteration is inclusive on
    /// both ends; non-positive steps on a given axis collapse that axis to
    /// the min value (single row/column/layer).
    /// </summary>
    [Serializable]
    public class MapTiling
    {
        public string prefab;
        public float[] min;              // [x,y,z] start corner (inclusive)
        public float[] max;              // [x,y,z] end corner (inclusive)
        public float[] step;             // [x,y,z] grid spacing per axis
        public float[] rotation;         // optional euler degrees applied to every instance
        public string parent;            // optional group name
        public string note;              // optional author comment
    }

    [Serializable]
    public class MapManifest
    {
        public string name;
        public MapPiece[] pieces;
        public MapTiling[] tilings;
    }
}
