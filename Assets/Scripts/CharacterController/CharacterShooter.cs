using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterShooter : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float maxDistance = 25f;
    [SerializeField] private float cameraSafeDistance = 2;
    [SerializeField] private float lerpTime = 15;
    [SerializeField] private float shootRate = .15f;
    [SerializeField] private float bulletSpeed = 250;

    [Header("References")]
    [SerializeField] private Transform aimTarget;
    [SerializeField] private Transform cam;
    [SerializeField] private InputActionReference shootAction;
    [SerializeField] private Transform gunTip;
    [SerializeField] private Transform orientation;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask enemiesLayer;
    [SerializeField] private ObjectPool bulletPool;

    private bool canShoot = true;
    public event Action ShootEvent;
    private Coroutine shootCoroutine;

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
        shootAction.action.performed += OnShootActionPerformed;
        shootAction.action.canceled += OnShootActionCanceled;
    }

    private void OnDisable()
    {
        shootAction.action.performed -= OnShootActionPerformed;
        shootAction.action.canceled -= OnShootActionCanceled;
    }

    private void OnShootActionPerformed(InputAction.CallbackContext context)
    {
        shootCoroutine = StartCoroutine(ShootCoroutine());
    }

    private void OnShootActionCanceled(InputAction.CallbackContext context)
    {
        if (shootCoroutine != null)
            StopCoroutine(shootCoroutine);
    }

    private IEnumerator ShootCoroutine()
    {
        while (true)
        {
            if (canShoot)
            {
                Shoot();
                canShoot = false;
                Invoke(nameof(ResetCanShoot), shootRate);
            }
            yield return null;
        }
    }

    private void ResetCanShoot()
    {
        canShoot = true;
    }
    
    private void Shoot()
    {
        GameObject bullet = bulletPool.GetObject();
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb)
        {
            bullet.transform.position = gunTip.position;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce((aimTarget.position - gunTip.position).normalized * bulletSpeed, ForceMode.Impulse);
            ShootEvent?.Invoke();
        }
        else
        {
            Debug.LogWarning("The bullet pool is not set up correctly in the Character Shooter component.", this);

            if (bullet.TryGetComponent<IPooledObject>(out var pooledObject)) pooledObject.ReturnToPoll();
            else
            {
                Destroy(bullet);
            }
        }
    }
}
