using System.Collections;
using System.Linq;
using UnityEngine;

// O Learner propriamente dito
public class ScoresLearner : MonoBehaviour
{
    [SerializeField]
    private TrainingAIConfiguration trainingConfig;
    public TrainingAIConfiguration TrainingConfig => trainingConfig;

    private float learningRate;
    private HidePoint lastSelectedPoint;

    private ScoresDatabase heuristicDatabase;
    public ScoresDatabase HeuristicDatabase => heuristicDatabase;
    public float LearningRate => learningRate;
    public HidePoint LastSelectedPoint => lastSelectedPoint;

    [SerializeField]
    SimulationControl simulationControl;

    private void Awake()
    {
        learningRate = trainingConfig.BaseLearningRate;
        RequestDatabase();
    }

    void RequestDatabase() => StartCoroutine(WaitForDatabase());

    IEnumerator WaitForDatabase()
    {
        yield return new WaitForSeconds(0.2f);
        heuristicDatabase = simulationControl.HeuristicDatabase;
    }

    public void LearnFromReport(HidePointInstantReport report)
    {
        if (!simulationControl.IsLearningEnabled)
        {
            return;
        }

        lastSelectedPoint = report.Point;

        // 1) Atualizar learning rate com decaimento
        UpdateLearningRate(report.InteractionsNumber);

        // 2) Calcular fiabilidade (reliability) de cada feature
        float relDist = ComputeReliability(report.DistanceFromEnemy);
        float relExpo = ComputeReliability(report.ExpositionTime);
        float relReact = ComputeReliability(report.ReactionTime);

        float noise = CalculateNoise(report);

        float reliability = 1f - noise; // quanto menor o ruído, maior a fiabilidade

        // 3) Ajustar pesos originais pela fiabilidade
        float wDistRaw = trainingConfig.DistanceImportance * relDist;
        float wExpoRaw = trainingConfig.ExpositionImportance * relExpo;
        float wReactRaw = trainingConfig.ReactionImportance * relReact;
        float sumRaw = wDistRaw + wExpoRaw + wReactRaw;
        if (sumRaw <= 0f)
            sumRaw = 1f; // evitar divisão por zero

        float wDist = wDistRaw / sumRaw;
        float wExpo = wExpoRaw / sumRaw;
        float wReact = wReactRaw / sumRaw;

        // 4) Computar reward já normalizado [0,1]
        float rd = report.DistanceFromEnemy.current; // quanto mais longe, melhor
        float re = 1f - report.ExpositionTime.current; // quanto menos exposto, melhor
        float rt = 1f - report.ReactionTime.current; // quanto mais rápido, melhor

        float reward = wDist * rd + wExpo * re + wReact * rt;

        // 5) Q-learning update
        float oldQ = heuristicDatabase.Scores[report.Point];
        float newQ = oldQ + learningRate * reliability * (reward - oldQ);

        report.Point.ReportData.Feed((Time.time, (newQ, 0f)));

        heuristicDatabase.SetScore(report.Point, newQ);
    }

    private void UpdateLearningRate(int interactions)
    {
        float baseLearningRate = trainingConfig.BaseLearningRate;
        float learningRateDecay = trainingConfig.LearningRateDecay;
        learningRate = baseLearningRate / (1f + learningRateDecay * interactions);
    }

    private float ComputeReliability((float previous, float current) pair)
    {
        float delta = Mathf.Abs(pair.current - pair.previous);
        float reliability = 1f - Mathf.Clamp01(delta);

        return reliability;
    }

    private float CalculateNoise(HidePointInstantReport report)
    {
        float[] vals =
        {
            report.DistanceFromEnemy.current,
            1f - report.ExpositionTime.current, // we need lower exposition time
            report.ReactionTime.current, // we need higher delta distance from enemy
        };

        float[] h_vals =
        {
            report.DistanceFromEnemy.previous,
            1f - report.ExpositionTime.previous, // we need lower exposition time
            report.ReactionTime.previous, // we need higher delta distance from enemy
        };

        float oldMean = (h_vals[0] + h_vals[1] + h_vals[2]) / 3f;
        float mean = (vals[0] + vals[1] + vals[2]) / 3f;

        float oldVariance = h_vals.Select(v => (v - oldMean) * (v - oldMean)).Sum() / 3f;
        float variance = vals.Select(v => (v - mean) * (v - mean)).Sum() / 3f;

        float historicalVariance = Mathf.Abs(variance - oldVariance);
        float noise = Mathf.Sqrt(historicalVariance);

        return noise;
    }
}
