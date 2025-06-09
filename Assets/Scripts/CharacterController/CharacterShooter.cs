using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.VFX;

public class CharacterShooter : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float maxDistance = 25f;
    [SerializeField] private float cameraSafeDistance = 2;
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
    [SerializeField] private LayerMask environmentLayer;
    [SerializeField] private ObjectPool bulletPool;
    [SerializeField] private VisualEffect muzzleFlash;

    [SerializeField] private BulletProjectile bulletPrefab;

    private bool canShoot = true;
    public event Action ShootEvent;
    private Coroutine shootCoroutine;
    private CustomCharacterController characterController;

    private void Awake()
    {
        if (cam == null)
            cam = FindAnyObjectByType<CinemachineBrain>().transform;
        if (characterController == null)
            characterController = GetComponent<CustomCharacterController>();
    }

    public void UpdateAim()
    {
        LayerMask layer = enemiesLayer | environmentLayer;
        
        if (Physics.Raycast(cam.position + cam.forward * cameraSafeDistance, cam.forward, out RaycastHit hit, maxDistance, layer))
        {
            if (hit.collider.gameObject.layer == playerLayer || hit.collider.GetComponent<BulletProjectile>() != null) return;

            if (Vector3.Distance(transform.position, hit.point) > 1 ||
                Vector3.Dot(orientation.forward, (transform.position - hit.point).normalized) < -.5f)
                aimTarget.position = hit.point;
            else
                aimTarget.position = cam.forward * maxDistance;
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
        if (characterController?.CharacterState == CharacterStates.Console) return;

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
        // GameObject bullet = Instantiate(bulletPrefab.gameObject, gunTip.position, Quaternion.identity);

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        bullet.transform.position = gunTip.position;
        bullet.transform.parent = null;

        if (rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            Vector3 direction = (aimTarget.position - gunTip.position).normalized;

            rb.linearVelocity = direction * bulletSpeed;

            ShootEvent?.Invoke();
            if (muzzleFlash) muzzleFlash.Play();
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
