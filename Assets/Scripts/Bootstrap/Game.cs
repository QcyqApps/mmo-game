using UnityEngine;

namespace MmoGame.Bootstrap
{
    /// <summary>
    /// Application entry point. Wired into the bootstrap scene; kicks off
    /// initialization sequence (config load → backend connect → game state).
    /// Intentionally minimal in week 1 — fills out as networking + backend
    /// land in week 2.
    /// </summary>
    public class Game : MonoBehaviour
    {
        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Debug.Log($"[Game] Bootstrap on {Application.platform}, version {Application.version}");
        }
    }
}
