using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterShooter : MonoBehaviour
{
    [SerializeField] private Transform aimTarget;
    [SerializeField] private Transform cam;
    [SerializeField] private InputActionReference shootAction;
    [SerializeField] private Transform gunTip;

    [SerializeField] private float maxDistance = 150f;

    public void UpdateAim()
    {
        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, maxDistance))
        {
            aimTarget.position = hit.point;
        }
        else
            aimTarget.position = cam.forward * maxDistance;
    }

    private void OnEnable()
    {
        shootAction.action.performed += OnShootAction;
    }

    private void OnDisable()
    {
        shootAction.action.performed -= OnShootAction;
    }

    private void OnShootAction(InputAction.CallbackContext context)
    {
        
    }
}
