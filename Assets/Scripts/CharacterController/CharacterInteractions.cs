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
        private float radius = 2.0f;

        [SerializeField]
        private LayerMask interactablesLayer;

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
            RaycastHit[] hits = Physics.SphereCastAll(transform.position, radius, transform.forward, 0f, interactablesLayer);

            if (hits.Length > 0)
            {
                RaycastHit closestHit = hits.OrderBy(h => h.distance).First();
                Interactable interactableObject = closestHit.collider.GetComponent<Interactable>();

                if (interactableObject != null)
                {
                    interactableObject.Interact();
                }
            }
        }
    }
}