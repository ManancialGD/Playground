using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoresVisualizer : MonoBehaviour
{
    [SerializeField]
    SimulationControl simulationControl;
    private ScoresDatabase Scores => simulationControl.HeuristicDatabase;
    private HidePoint lastSelectedPoint => simulationControl.LearnerAI.LastSelectedPoint;

    private bool waitingForDatabase = true;

    void Start() => StartCoroutine(WaitForDatabase());

    private IEnumerator WaitForDatabase()
    {
        yield return new WaitForSeconds(0.2f);
        waitingForDatabase = false;
    }

    void OnDrawGizmos()
    {
        if (Scores == null || Scores.Scores.Count <= 0 || waitingForDatabase)
            return;

        foreach (KeyValuePair<HidePoint, float> spot in Scores.normalized.ToDictionary)
        {
            float value = spot.Value;
            Gizmos.color = Color.Lerp(Color.red, Color.green, value);
            if (lastSelectedPoint != spot.Key)
                Gizmos.DrawLine(spot.Key.Position, spot.Key.Position + Vector3.up * 50f);
            else
                Gizmos.DrawLine(spot.Key.Position, spot.Key.Position + Vector3.up * 80f);
        }
    }
}
