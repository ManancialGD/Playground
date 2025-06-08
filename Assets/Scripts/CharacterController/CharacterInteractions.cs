using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace CharacterController
{
    public class CharacterInteractions : MonoBehaviour
    {
        [SerializeField]
        private InputActionReference interactActionReference;

        [SerializeField]
        private float radius = 1.0f;
        [SerializeField]
        private float interactDistance = 2.25f;

        [SerializeField]
        private LayerMask interactablesLayer;

        [SerializeField]
        private Transform cameraTransform;

        private void OnEnable()
        {
            if (interactActionReference != null)
            {
                interactActionReference.action.performed += OnInteractPerformed;
            }
        }

        private void OnDisable()
        {
            if (interactActionReference != null)
            {
                interactActionReference.action.performed -= OnInteractPerformed;
            }
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            Interact();
        }

        public void Interact()
        {
            if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, interactDistance, interactablesLayer))
            {
                if (hit.collider.TryGetComponent<Interactable>(out var interactable))
                {
                    interactable.Interact();
                    return;
                }
            }

            RaycastHit[] hits = Physics.SphereCastAll(transform.position, radius, cameraTransform.forward, 0f, interactablesLayer);

            if (hits.Length > 0)
            {
                var interactables = hits.Select(h => h.collider.GetComponent<Interactable>())
                    .Where(i => i != null)
                    .ToList();

                Interactable closestInteractable = interactables
                    .OrderBy(i => Vector3.Distance(cameraTransform.position, i.transform.position))
                    .First();

                if (closestInteractable != null)
                {
                    closestInteractable.Interact();
                }
            }
        }
    }
}