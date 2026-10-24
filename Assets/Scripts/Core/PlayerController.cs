using System;
using UnityEngine;
using UnityEngine.AI;
using InkAndAsh.Core.Interaction;

namespace InkAndAsh.Core
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float _walkSpeed = 4.5f;
        [SerializeField] private float _runSpeed = 6.5f;
        [SerializeField] private float _rotationSpeed = 720f;
        [SerializeField] private LayerMask _groundLayers = ~0;

        [Header("Interaction Magnetism")]
        [SerializeField] private float _interactionMagnetRadius = 1.2f;
        [SerializeField] private LayerMask _interactionLayers = ~0;

        [Header("Visual Feedback")]
        [SerializeField] private GameObject _destinationMarkerPrefab;

        private NavMeshAgent _agent;
        private Camera _mainCamera;
        private Interactable _pendingInteractable;
        private GameObject _activeMarker;
        private bool _isInteracting;

        public bool CanMove { get; set; } = true;
        public bool IsMoving => _agent != null && _agent.hasPath && _agent.remainingDistance > _agent.stoppingDistance;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.speed = _walkSpeed;
            _agent.angularSpeed = _rotationSpeed;
            _agent.updateRotation = true;
            _mainCamera = Camera.main;
        }

        private void Update()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }

            if (!CanMove)
            {
                if (_agent.hasPath) _agent.ResetPath();
                return;
            }

            HandlePointerInput();
            HandleDirectionalInput();
            CheckPendingInteraction();
        }

        private void HandlePointerInput()
        {
            if (UnityEngine.EventSystems.EventSystem.current != null && 
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            bool pointerDown = false;
            Vector2 screenPos = Vector2.zero;

            if (Input.GetMouseButtonDown(0))
            {
                pointerDown = true;
                screenPos = Input.mousePosition;
            }
            else if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    if (UnityEngine.EventSystems.EventSystem.current != null && 
                        UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    {
                        return;
                    }
                    pointerDown = true;
                    screenPos = touch.position;
                }
            }

            if (!pointerDown || _mainCamera == null) return;

            Ray ray = _mainCamera.ScreenPointToRay(screenPos);
            RaycastHit[] hits = Physics.RaycastAll(ray, 150f, _groundLayers | _interactionLayers);
            if (hits == null || hits.Length == 0) return;

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            RaycastHit validHit = default;
            bool foundValidHit = false;

            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i];
                // Ignore the player's own colliders
                if (h.collider.transform.root == transform || h.collider.gameObject == gameObject)
                {
                    continue;
                }

                validHit = h;
                foundValidHit = true;
                break;
            }

            if (!foundValidHit) return;

            // Check for direct click on an Interactable or nearby magnetism
            Interactable target = validHit.collider.GetComponentInParent<Interactable>();
            if (target == null)
            {
                target = FindNearestInteractable(validHit.point, _interactionMagnetRadius);
            }

            if (target != null)
            {
                SetInteractionTarget(target);
            }
            else
            {
                ClearPendingInteraction();
                MoveToDestination(validHit.point);
            }
        }

        private void HandleDirectionalInput()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;
            if (inputDir.sqrMagnitude > 0.01f && _mainCamera != null)
            {
                ClearPendingInteraction();

                Vector3 cameraForward = Vector3.ProjectOnPlane(_mainCamera.transform.forward, Vector3.up).normalized;
                Vector3 cameraRight = Vector3.ProjectOnPlane(_mainCamera.transform.right, Vector3.up).normalized;
                Vector3 moveDir = (cameraForward * inputDir.z + cameraRight * inputDir.x).normalized;

                Vector3 targetPoint = transform.position + moveDir * 2.0f;
                if (NavMesh.SamplePosition(targetPoint, out NavMeshHit navHit, 2.0f, NavMesh.AllAreas))
                {
                    _agent.isStopped = false;
                    _agent.SetDestination(navHit.position);
                }
            }
        }

        public void MoveToDestination(Vector3 destination)
        {
            if (NavMesh.SamplePosition(destination, out NavMeshHit navHit, 5.0f, NavMesh.AllAreas))
            {
                _agent.stoppingDistance = 0.2f;
                _agent.isStopped = false;
                _agent.SetDestination(navHit.position);
                SpawnDestinationMarker(navHit.position);
            }
        }

        public void SetInteractionTarget(Interactable interactable)
        {
            _pendingInteractable = interactable;
            _agent.stoppingDistance = interactable.InteractionDistance * 0.85f;
            _agent.isStopped = false;
            _agent.SetDestination(interactable.InteractionPosition);
            SpawnDestinationMarker(interactable.InteractionPosition);
        }

        public void ClearPendingInteraction()
        {
            _pendingInteractable = null;
        }

        private void CheckPendingInteraction()
        {
            if (_pendingInteractable == null) return;

            float distance = Vector3.Distance(transform.position, _pendingInteractable.InteractionPosition);
            if (distance <= _pendingInteractable.InteractionDistance)
            {
                Interactable target = _pendingInteractable;
                _pendingInteractable = null;
                _agent.ResetPath();

                Vector3 lookTarget = target.transform.position;
                lookTarget.y = transform.position.y;
                transform.LookAt(lookTarget);

                target.TriggerInteraction();
            }
        }

        private Interactable FindNearestInteractable(Vector3 point, float radius)
        {
            Collider[] colliders = Physics.OverlapSphere(point, radius);
            Interactable nearest = null;
            float minDistance = float.MaxValue;

            foreach (var col in colliders)
            {
                Interactable candidate = col.GetComponentInParent<Interactable>();
                if (candidate != null)
                {
                    float dist = Vector3.Distance(point, candidate.InteractionPosition);
                    if (dist < minDistance && dist <= candidate.MagnetismRadius)
                    {
                        minDistance = dist;
                        nearest = candidate;
                    }
                }
            }

            return nearest;
        }

        private void SpawnDestinationMarker(Vector3 position)
        {
            if (_destinationMarkerPrefab == null) return;

            if (_activeMarker != null)
            {
                Destroy(_activeMarker);
            }

            _activeMarker = Instantiate(_destinationMarkerPrefab, position + Vector3.up * 0.05f, Quaternion.identity);
            Destroy(_activeMarker, 1.5f);
        }
    }
}
