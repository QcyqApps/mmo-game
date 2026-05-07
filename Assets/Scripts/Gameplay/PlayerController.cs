using FishNet.Object;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MmoGame.Gameplay
{
    /// <summary>
    /// Bare-bones owner-driven WASD movement. Server-authoritative version
    /// with input prediction lands in week 4 alongside combat — for now we
    /// just need two clients to see each other moving. Sync handled by
    /// the NetworkTransform component on the same prefab.
    /// </summary>
    public class PlayerController : NetworkBehaviour
    {
        [SerializeField] float speed = 5f;

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (IsOwner)
                Debug.Log($"[Player] Owned spawn at {transform.position} (id={ObjectId})");
        }

        void Update()
        {
            if (!IsOwner) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            float h = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
            float v = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
            if (h == 0f && v == 0f) return;

            var dir = new Vector3(h, 0f, v).normalized;
            transform.Translate(dir * (speed * Time.deltaTime), Space.World);
        }
    }
}
