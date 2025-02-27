using Unity.Cinemachine;
using UnityEditor.IMGUI.Controls;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterShooter : MonoBehaviour
{
    [SerializeField] private Transform aimTarget;
    [SerializeField] private Transform cam;
    [SerializeField] private InputActionReference shootAction;
    [SerializeField] private Transform gunTip;
    [SerializeField] private Transform orientation;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask enemiesLayer;
    [SerializeField] private float maxDistance = 150f;
    [SerializeField] private float cameraSafeDistance = 2;
    [SerializeField] private float lerpTime = 15;

    private void Awake()
    {
        if (cam == null)
            cam = FindAnyObjectByType<CinemachineBrain>().transform;
    }
    public void UpdateAim()
    {
        if (Physics.Raycast(cam.position, cam.forward * cameraSafeDistance, out RaycastHit hit, maxDistance))
        {
            if (hit.collider.gameObject.layer == playerLayer) return;

            if (Vector3.Distance(transform.position, hit.point) > 1 ||
                Vector3.Dot(orientation.forward, (transform.position - hit.point).normalized) < -.5f)
                if (hit.collider.gameObject.layer == enemiesLayer)
                    aimTarget.position = hit.point;
                else
                    aimTarget.position = Vector3.Lerp(aimTarget.position, hit.point, Time.deltaTime * lerpTime);
            else
                aimTarget.position = Vector3.Lerp(aimTarget.position, cam.forward * maxDistance, Time.deltaTime * lerpTime);
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
