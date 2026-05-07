using UnityEngine;

namespace MmoGame.Gameplay
{
    /// <summary>
    /// Fixed-angle RO-style follow camera. World-aligned (does not rotate
    /// with the player), so WASD stays north/south/east/west. PlayerController
    /// assigns Target on its own spawn when IsOwner.
    /// </summary>
    public class PlayerCamera : MonoBehaviour
    {
        public static PlayerCamera Instance { get; private set; }

        [Tooltip("World-space offset from target. Default ~45° pitch, 12u distance.")]
        public Vector3 Offset = new Vector3(0f, 12f, -12f);

        [Tooltip("Lower = snappier follow, higher = lazier.")]
        public float SmoothTime = 0.12f;

        public Transform Target { get; private set; }

        Vector3 _velocity;

        void Awake()
        {
            Instance = this;
        }

        public void SetTarget(Transform target)
        {
            Target = target;
            if (target != null)
            {
                // Snap once on assignment so we don't slide across the map from origin.
                transform.position = target.position + Offset;
                transform.LookAt(target.position);
            }
        }

        void LateUpdate()
        {
            if (Target == null) return;
            var desired = Target.position + Offset;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, SmoothTime);
            transform.LookAt(Target.position);
        }
    }
}
