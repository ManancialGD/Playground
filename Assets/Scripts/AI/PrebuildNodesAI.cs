using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class PrebuildNodesAI : MonoBehaviour
{
    [SerializeField]
    public NavMeshSurface navMeshSurface;

    public DecisionTreeAI.NodeStatus IsTargetInRange(EnemyAI enemy, EnemyAI target)
    {
        float distance = Vector3.Distance(enemy.transform.position, target.transform.position);
        return (distance <= 6f)
            ? DecisionTreeAI.NodeStatus.Success
            : DecisionTreeAI.NodeStatus.Failure;
    }

    public DecisionTreeAI.NodeStatus CanSeeTarget(EnemyAI enemy, EnemyAI target)
    {
        RaycastHit hit;
        Vector3 direction = (target.transform.position - enemy.transform.position).normalized;

        if (Physics.Raycast(enemy.transform.position, direction, out hit, 100))
        {
            return (hit.collider != null && hit.collider.gameObject == target.gameObject)
                ? DecisionTreeAI.NodeStatus.Success
                : DecisionTreeAI.NodeStatus.Failure;
        }
        return DecisionTreeAI.NodeStatus.Failure;
    }

    public NavMeshPath GoToPosition(EnemyAI enemy, Vector3 position)
    {
        NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogError("NavMeshAgent não encontrado no inimigo!");
            return null;
        }

        agent.ResetPath();
        NavMeshPath path = new NavMeshPath();

        agent.SetDestination(position);
        if (agent.CalculatePath(position, path))
        {
            enemy.SetPath(path);
            return path; // Retorna o caminho calculado
        }
        else
        {
            Debug.LogWarning("Não foi possível calcular um caminho para o destino.");
            enemy.SetPath(path);
            return path; // Retorna um caminho vazio se falhar
        }
    }

    public void StopMoving(EnemyAI enemy)
    {
        NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent não encontrado no inimigo!");
            return;
        }
        agent.ResetPath();
        agent.isStopped = true;
    }

    public DecisionTreeAI.NodeStatus FollowTarget(EnemyAI enemy, EnemyAI target)
    {
        GoToPosition(enemy, target.transform.position);
        return DecisionTreeAI.NodeStatus.Success;
    }
}
