using FishNet.Object;
using UnityEngine;

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

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            if (h == 0f && v == 0f) return;

            var dir = new Vector3(h, 0f, v).normalized;
            transform.Translate(dir * (speed * Time.deltaTime), Space.World);
        }
    }
}
