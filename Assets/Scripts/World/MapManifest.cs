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
    }

    [Serializable]
    public class MapManifest
    {
        public string name;
        public MapPiece[] pieces;
    }
}
