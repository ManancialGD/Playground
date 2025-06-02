using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider), typeof(Rigidbody))]
public class BulletProjectile : MonoBehaviour, IPooledObject
{
    [SerializeField] private TrailRenderer trail;
    [SerializeField] private float destroyTime = 2;

    private Rigidbody rb;
    private ObjectPool thisObjectPool;
    private float elapsedTime = 0;


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

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out RagDollLimb ragDollLimb))
        {
            ragDollLimb.Damage(transform.position, rb.linearVelocity.normalized);
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

        StartCoroutine(EnableTrail());
    }

    private IEnumerator EnableTrail()
    {
        while (rb.linearVelocity.magnitude < 1e-4)
            yield return null;

        trail.emitting = true;
    }
}