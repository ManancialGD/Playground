using System.Collections;
using UnityEngine;

public class ManualHidePoint : MonoBehaviour
{
    [SerializeField]
    private SimulationControl simulationControl;

    private void OnDrawGizmos()
    {
        if (simulationControl == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position, 1f);
    }

    void Start()
    {
        StartCoroutine(UpdateDatabase());
    }

    IEnumerator UpdateDatabase()
    {
        yield return new WaitUntil(() => simulationControl.DatabaseLoaded);
        bool pointExists = simulationControl.HeuristicDatabase.HasPosition(
            transform.position,
            out HidePoint hidePointFound
        );

        if (!pointExists)
        { // If the point does not exist, create it with a score of 0
            HidePoint newPoint = new HidePoint(
                transform.position,
                simulationControl.CurrentConfig,
                simulationControl.TrainingEntity,
                simulationControl
            );
            simulationControl.HeuristicDatabase.SetScore(newPoint, 0f);
        }
    }
}
