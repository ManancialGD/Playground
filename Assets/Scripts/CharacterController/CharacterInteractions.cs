using System.Linq;
using JetBrains.Annotations;
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
        private void OnGUI()
        {
            var labelWidth = 400;
            var labelHeight = 60;
            var rect = new Rect(
                (Screen.width - labelWidth) / 2,
                (Screen.height - labelHeight) / 2 + 50,
                labelWidth,
                labelHeight
            );
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new(.75f, .75f, .75f) }
            };

            if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, interactDistance, interactablesLayer))
            {
                if (hit.collider.TryGetComponent<Interactable>(out var interactable))
                {
                    GUI.Label(rect, interactable.interactMessage, style);
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
                    GUI.Label(rect, closestInteractable.interactMessage, style);
                }
            }
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