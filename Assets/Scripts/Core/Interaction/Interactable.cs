using System;
using UnityEngine;
using UnityEngine.Events;

namespace InkAndAsh.Core.Interaction
{
    public class Interactable : MonoBehaviour
    {
        [Header("Interaction Configuration")]
        [SerializeField] private string _promptText = "Interact";
        [SerializeField] private float _interactionDistance = 2.5f;
        [SerializeField] private float _magnetismRadius = 4.0f;
        [SerializeField] private Transform _interactionPoint;

        [Header("Events")]
        public UnityEvent onInteracted;

        public string PromptText => _promptText;
        public float InteractionDistance => _interactionDistance;
        public float MagnetismRadius => _magnetismRadius;
        public Vector3 InteractionPosition => _interactionPoint != null ? _interactionPoint.position : transform.position;

        public void TriggerInteraction()
        {
            onInteracted?.Invoke();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(InteractionPosition, _interactionDistance);

            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
            Gizmos.DrawWireSphere(InteractionPosition, _magnetismRadius);
        }
    }
}
