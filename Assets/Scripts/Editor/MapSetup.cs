using UnityEditor;

namespace MmoGame.Editor
{
    /// <summary>
    /// Legacy entry point retained for muscle memory — delegates to
    /// SyntyCatalogScanner.Rebuild() which scans the entire Assets/Synty/
    /// tree instead of the old hard-coded 12-entry list.
    /// </summary>
    public static class MapSetup
    {
        [MenuItem("MmoGame/Setup Map Registry (legacy alias)")]
        public static void Run() => SyntyCatalogScanner.Rebuild();
    }
}
