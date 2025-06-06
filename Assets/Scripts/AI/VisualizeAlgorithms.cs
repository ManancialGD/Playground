using System.Collections;
using System.Linq;
using UnityEngine;

public class VisualizeAlgorithms : MonoBehaviour
{
    [SerializeField]
    private EnemyAI EntityAI;

    [SerializeField]
    SimulationControl simulationControl;

    bool started = false;

    public void Start()
    {
        StartVisualization();
    }

    void StartVisualization() => StartCoroutine(WaitForDatabase());

    IEnumerator WaitForDatabase()
    {
        yield return new WaitForSeconds(0.2f); // Wait for the game to start
        started = true;
    }

#if UNITY_EDITOR
    public void OnDrawGizmos()
    {
        if (!Application.isPlaying || !started || EntityAI == null || EntityAI.HidingScores == null)
            return;

        var scores = EntityAI.HidingScores.Scores;
        if (scores == null || scores.Count == 0)
            return;

        float min = scores.Values.Min();
        float max = scores.Values.Max();

        for (int i = 0; i < scores.Count; i++)
        {
            var spot = scores.Keys.ElementAt(i);
            float value = scores.Values.ElementAt(i);
            Gizmos.color = new Color(1f - (value - min) / (max - min), value / max, 0f);
            Gizmos.DrawLine(EntityAI.transform.position, spot.Position);
        }
    }

# endif
}
