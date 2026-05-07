using FishNet.Object;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace MmoGame.Gameplay
{
    /// <summary>
    /// RO-style click-to-move via NavMeshAgent. Owner-only input. Sync handled
    /// by NetworkTransform on the same prefab. Server-authoritative version
    /// with rollback lands in week 4 alongside combat — for now position is
    /// trusted from the owner.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class PlayerController : NetworkBehaviour
    {
        [SerializeField] float speed = 5f;
        [SerializeField] float angularSpeed = 360f;
        [SerializeField] float stoppingDistance = 0.1f;
        [SerializeField] float navSampleRadius = 2f;

        static readonly Plane GroundPlane = new Plane(Vector3.up, Vector3.zero);

        NavMeshAgent _agent;
        Camera _cam;

        void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.speed = speed;
            _agent.angularSpeed = angularSpeed;
            _agent.stoppingDistance = stoppingDistance;
            _agent.acceleration = 30f;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!IsOwner) return;

            Debug.Log($"[Player] Owned spawn at {transform.position} (id={ObjectId})");

            if (PlayerCamera.Instance != null)
                PlayerCamera.Instance.SetTarget(transform);
            _cam = Camera.main;
        }

        void Update()
        {
            if (!IsOwner) return;
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            var screen = mouse.position.ReadValue();
            var ray = _cam.ScreenPointToRay(screen);
            if (!GroundPlane.Raycast(ray, out var enter)) return;

            var worldPoint = ray.GetPoint(enter);
            if (NavMesh.SamplePosition(worldPoint, out var navHit, navSampleRadius, NavMesh.AllAreas))
                _agent.SetDestination(navHit.position);
        }
    }
}
