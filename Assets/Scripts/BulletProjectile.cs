using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider), typeof(Rigidbody))]
public class BulletProjectile : MonoBehaviour, IPooledObject
{
    [SerializeField] private TrailRenderer trail;
    [SerializeField] private float destroyTime = 2;

    private Rigidbody rb;
    private ObjectPool thisObjectPool;
    [HideInInspector] public bool isPlayers = false;
    private float elapsedTime = 0;

    private bool damaged = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;

        if (elapsedTime > destroyTime)
        {
            ReturnToPoll();
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if (damaged)
            return;
        
        if (isPlayers && other.collider.GetComponentInParent<CustomCharacterController>())
            return;

        if (!isPlayers && other.collider.GetComponentInParent<EnemyAI>())
            return;

        if (other.collider.TryGetComponent(out RagDollLimb ragDollLimb))
        {
            ragDollLimb.Damage(transform.position, rb.linearVelocity.normalized);
            damaged = true;
        }
        HandleDespawn();
    }

    public void SetPool(ObjectPool pool)
    {
        thisObjectPool = pool;
    }

    public void ReturnToPoll()
    {
        if (thisObjectPool == null)
        {
            Destroy(gameObject);
            return;
        }

        StopObject();
        gameObject.SetActive(false);
        thisObjectPool.ReturnObject(gameObject);
    }

    public void StopObject()
    {
        rb.Sleep();
        elapsedTime = 0;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        trail.emitting = false;
    }
    private void HandleDespawn()
    {
        rb.linearVelocity = Vector3.zero;
        rb.Sleep();
        Invoke(nameof(ReturnToPoll), trail.time);
    }

    public void StartObject()
    {
        rb.WakeUp();
        rb.linearVelocity = Vector3.zero;
        damaged = false;

        StartCoroutine(EnableTrail());
    }

    private IEnumerator EnableTrail()
    {
        while (rb.linearVelocity.magnitude < 1e-4)
            yield return null;

        trail.emitting = true;
    }
}