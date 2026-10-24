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
        [SerializeField] private float _interactionMagnetRadius = 4.0f;
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
            bool pointerDown = false;
            Vector2 screenPos = Vector2.zero;

            if (Input.GetMouseButtonDown(0))
            {
                pointerDown = true;
                screenPos = Input.mousePosition;
            }
            else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                pointerDown = true;
                screenPos = Input.GetTouch(0).position;
            }

            if (!pointerDown || _mainCamera == null) return;

            Ray ray = _mainCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, _groundLayers | _interactionLayers))
            {
                // Check for direct click on an Interactable or nearby magnetism
                Interactable target = hit.collider.GetComponentInParent<Interactable>();
                if (target == null)
                {
                    target = FindNearestInteractable(hit.point, _interactionMagnetRadius);
                }

                if (target != null)
                {
                    SetInteractionTarget(target);
                }
                else
                {
                    ClearPendingInteraction();
                    MoveToDestination(hit.point);
                }
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
                    _agent.SetDestination(navHit.position);
                }
            }
        }

        public void MoveToDestination(Vector3 destination)
        {
            if (NavMesh.SamplePosition(destination, out NavMeshHit navHit, 3.0f, NavMesh.AllAreas))
            {
                _agent.stoppingDistance = 0.2f;
                _agent.SetDestination(navHit.position);
                SpawnDestinationMarker(navHit.position);
            }
        }

        public void SetInteractionTarget(Interactable interactable)
        {
            _pendingInteractable = interactable;
            _agent.stoppingDistance = interactable.InteractionDistance * 0.85f;
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
