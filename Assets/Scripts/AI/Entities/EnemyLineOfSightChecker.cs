using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class EnemyLineOfSightChecker : MonoBehaviour
{
    public SphereCollider Collider;
    public float FieldOfView = 90f;
    public LayerMask LineOfSightLayers;

    public delegate void GainSightEvent(Transform Target);
    public GainSightEvent OnGainSight;
    public delegate void LoseSightEvent(Transform Target);
    public LoseSightEvent OnLoseSight;

    private Coroutine CheckForLineOfSightCoroutine;
    private EnemyAI enemyAI;

    private void Start()
    {
        enemyAI = transform.parent.GetComponent<EnemyAI>();
        Collider = GetComponent<SphereCollider>();
        if (enemyAI.Player == null)
        {
            Debug.LogError("Player not found");
            return;
        }
        CheckForLineOfSightCoroutine = StartCoroutine(
            CheckForLineOfSight(enemyAI.Player.transform)
        );
    }

    public bool CheckLineOfSight(Transform Target)
    {
        Vector3 direction = (Target.transform.position - transform.position).normalized;
        //float dotProduct = Vector3.Dot(transform.forward, direction);
        //if (dotProduct >= Mathf.Cos(FieldOfView))
        //{
        if (
            Physics.Raycast(
                transform.position,
                direction,
                out RaycastHit hit,
                1000,
                LineOfSightLayers
            )
        )
        {
            if (hit.collider && hit.collider.gameObject.CompareTag("EnemyAI"))
                return true;
        }
        //}

        return false;
    }

    private IEnumerator CheckForLineOfSight(Transform Target)
    {
        EnemyAI enemyAI = transform.parent.GetComponent<EnemyAI>();
        WaitForSeconds Wait = new WaitForSeconds(enemyAI.UpdateFrequency);

        bool seen = false;

        while (true)
        {
            bool canSee = CheckLineOfSight(Target);
            if (canSee && !seen)
            {
                OnGainSight?.Invoke(Target);
                seen = true;
            }
            else if (!canSee && seen)
            {
                OnLoseSight?.Invoke(Target);
                seen = false;
            }

            yield return Wait;
        }
    }
}
