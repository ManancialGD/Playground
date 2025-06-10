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
            // Check if we hit the player directly or a component of the player
            if (hit.collider && (hit.collider.gameObject.CompareTag("Player") ||
                hit.collider.GetComponentInParent<CustomCharacterController>() != null))
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerator CheckForLineOfSight(Transform Target)
    {
        EnemyAI enemyAI = transform.parent.GetComponent<EnemyAI>();
        WaitForSeconds Wait = new WaitForSeconds(enemyAI.UpdateFrequency);

        bool seen = false;

        while (true)
        {
            // Check if enemy is dead - stop checking line of sight
            if (enemyAI == null || !enemyAI.enabled)
            {
                Debug.Log($"[{gameObject.name}] LineOfSightChecker: Enemy is dead, stopping line of sight checks");
                yield break;
            }

            // Check if this component is still enabled
            if (!enabled)
            {
                Debug.Log($"[{gameObject.name}] LineOfSightChecker: Component disabled, stopping line of sight checks");
                yield break;
            }

            bool canSee = CheckLineOfSight(Target);
            if (canSee && !seen)
            {
                Debug.Log($"[{enemyAI.gameObject.name}] GAINED SIGHT of player - switching to attack mode!");
                OnGainSight?.Invoke(Target);
                seen = true;
            }
            else if (!canSee && seen)
            {
                Debug.Log($"[{enemyAI.gameObject.name}] LOST SIGHT of player");
                OnLoseSight?.Invoke(Target);
                seen = false;
            }

            yield return Wait;
        }
    }

    /// <summary>
    /// Stop the line of sight checking coroutine
    /// </summary>
    public void StopChecking()
    {
        if (CheckForLineOfSightCoroutine != null)
        {
            StopCoroutine(CheckForLineOfSightCoroutine);
            CheckForLineOfSightCoroutine = null;
            Debug.Log($"[{gameObject.name}] LineOfSightChecker: Stopped checking line of sight");
        }
    }

    private void OnDisable()
    {
        StopChecking();
    }
}
