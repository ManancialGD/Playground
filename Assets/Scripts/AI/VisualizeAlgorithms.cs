using System.Linq;
using UnityEngine;

public class VisualizeAlgorithms : MonoBehaviour
{
    [SerializeField]
    private SimulationControl SimulationControl;

    [SerializeField]
    private EnemyAI EntityAI;
    ScoresDatabase ScoresDatabase;
    ScoresDatabase HeuristicDatabase;

    public void Start()
    {
        ScoresDatabase = SimulationControl.ScoresDatabase;
        HeuristicDatabase = SimulationControl.HeuristicDatabase;
    }

# if UNITY_EDITOR
    public void OnDrawGizmos()
    {
        HidePoint targetPoint = EntityAI.TargetPoint;
        if (targetPoint == null)
            return;
        Gizmos.color = Color.green;
        if (!EntityAI.BeingSeen)
            Gizmos.color = Color.red;

        Gizmos.DrawLine(EntityAI.transform.position, EntityAI.Player.transform.position);

        if (
            EntityAI.Colliders.Length <= 0
            || EntityAI.Colliders[0] == null
            || EntityAI.HidingScores.Scores.Count <= 0
            || !EntityAI.HidingScores.HasPoint(targetPoint)
        )
            return;

        EntityAI.HidingScores.Scores.Values.ToList().OrderBy(x => x);
        foreach (HidePoint hidePoint in EntityAI.HidingScores.Scores.Keys)
        {
            if (hidePoint.Position == Vector3.zero || !EntityAI.HidingScores.HasPoint(hidePoint))
                continue;

            float scoreColor = Mathf.Clamp01(
                EntityAI.HidingScores.Scores[hidePoint] / EntityAI.HidingScores.Scores[targetPoint]
            );
            Gizmos.color = new Color(1 - scoreColor, scoreColor, 0);

            Gizmos.DrawLine(EntityAI.transform.position, hidePoint.Position);
        }
    }
# endif
}
