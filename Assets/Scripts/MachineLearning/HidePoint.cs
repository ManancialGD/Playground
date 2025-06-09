using System;
using System.Collections;
using System.Linq;
using DataSpace;
using Unity.Collections;
using UnityEngine;

public class HidePoint
{
    // Dependências
    private EnemyAI enemyAI;
    private readonly SimulationControl simulationControl;
    private EntityAIConfiguration config;
    public float lastInteractionTime;

    // Estado
    private float lastZoneScore;
    private float lastHeuristic;
    private float currentScore;

    // Propriedades públicas
    public Vector3 Position { get; }
    public HidePointInteractionReport Report { get; }
    public float Score => UpdateCache();
    public float NormalizedScore => GetNormalizedScore();
    public float Heuristic => lastHeuristic;
    public float ZoneScore => lastZoneScore;
    private Data reportData;
    public Data ReportData => reportData;

    public HidePoint(
        Vector3 position,
        EntityAIConfiguration config,
        EnemyAI enemyAI,
        SimulationControl simulation
    )
    {
        Position = ScoresDatabase.RoundVector3(position);
        this.config = config;
        this.enemyAI = enemyAI;
        simulationControl = simulation;
        Report = new HidePointInteractionReport(this, simulation.LearnerAI);

        reportData = new Data(
            "HidePointReport_" + PublicMethods.Vector3ToString(Position),
            null,
            Data.Type.SIMPLE,
            true
        );

        UpdateCache();
    }

    public HidePoint Clone()
    {
        var clonedPoint = new HidePoint(Position, config, enemyAI, simulationControl);
        clonedPoint.Report.SetInteractionsNumber(Report.InteractionsNumber);
        return clonedPoint;
    }

    public void SetAgentReference(EnemyAI agent) => enemyAI = agent;

    public void ChangeEntityConfig(EntityAIConfiguration config) => this.config = config;

    private float UpdateCache()
    {
        if (!simulationControl.DatabaseLoaded)
            return 0f;

        if (!simulationControl.IsLearningEnabled)
        {
            return currentScore;
        }

        lastZoneScore = CalculateZoneScore();
        lastHeuristic = CalculateHeuristic();
        currentScore = CalculateCompositeScore();

        return currentScore;
    }

    private float CalculateCompositeScore()
    {
        if (Position == Vector3.zero || !simulationControl.HeuristicDatabase.HasPoint(this))
            return 0f;

        if (!simulationControl.IsLearningEnabled)
        {
            float simpleScore = CalculateEnemyDistanceScore() * config.EnemyDistance_Importance;
            return Mathf.Clamp01(simpleScore);
        }

        float enemyDistanceScore = CalculateEnemyDistanceScore() * config.EnemyDistance_Importance;
        float directionScore =
            CalculateDirectionScore()
            * (1 - Mathf.Abs(enemyDistanceScore))
            * config.DotDirection_Importance;

        float zoneScore = lastZoneScore * config.ZoneScore_Importance;
        float heuristicScore = lastHeuristic * config.MapHeuristic_Importance;
        float averageDistanceScore =
            CalculateAverageDistanceScore() * config.AverageSpotDistance_Importance;
        float ownDistanceScore = CalculateOwnDistanceScore() * config.OwnDistance_Importance;

        float maxInteractions = simulationControl
            .HeuristicDatabase.Scores.Keys.OrderByDescending(x => x.Report.InteractionsNumber)
            .First()
            .Report.InteractionsNumber;
        float explorationScore =
            (1f - (Report.InteractionsNumber / maxInteractions)) * config.Exploration_Importance;

        // Combinação dos componentes
        float finalScore =
            enemyDistanceScore
            - averageDistanceScore
            - ownDistanceScore
            + directionScore
            + heuristicScore
            + zoneScore
            + explorationScore;

        // Debug.Log("---------- Point Score Calculation ----------");
        // Debug.Log("Point Score: " + finalScore);
        // Debug.Log("-");
        // Debug.Log("Enemy Distance Score: " + enemyDistanceScore);
        // Debug.Log("Average Distance Score: " + averageDistanceScore);
        // Debug.Log("Own Distance Score: " + ownDistanceScore);
        // Debug.Log("Direction Score: " + directionScore);
        // Debug.Log("Heuristic Score: " + heuristicScore);
        // Debug.Log("Exploration Score: " + explorationScore);
        // Debug.Log("Zone Score: " + zoneScore);

        return finalScore;
    }

    private float CalculateEnemyDistanceScore()
    {
        float enemyDistanceFromPoint = Vector3.Distance(
            Position,
            enemyAI.Player.transform.position
        );

        float score = enemyDistanceFromPoint / simulationControl.MapMaxDistance;

        return score;
        // Retorna um valor entre -1 e 1, onde valores negativos indicam que o ponto está mais perto do inimigo
    }

    private float CalculateDirectionScore()
    {
        Vector3 toSpot = Position - enemyAI.transform.position;
        Vector3 fromEnemy = enemyAI.transform.position - enemyAI.Player.transform.position;

        if (toSpot == Vector3.zero || fromEnemy == Vector3.zero)
            return 0f;

        toSpot.Normalize();
        fromEnemy.Normalize();

        return Vector3.Dot(toSpot, fromEnemy);
    }

    private float CalculateAverageDistanceScore()
    {
        var validPoints = simulationControl
            .HeuristicDatabase.Scores.Keys.Where(p => p != this)
            .Select(p => Vector3.Distance(Position, p.Position))
            .DefaultIfEmpty();

        return validPoints.Average() / simulationControl.MapMaxDistance;
    }

    private float CalculateOwnDistanceScore()
    {
        return Vector3.Distance(Position, enemyAI.transform.position)
            / simulationControl.MapMaxDistance;
    }

    private float CalculateHeuristic()
    {
        if (!simulationControl.HeuristicDatabase.HasPoint(this))
        {
            Debug.LogWarning("Heuristic not found for HidePoint at " + Position);
            return 0f;
        }

        float heuristic = simulationControl.HeuristicDatabase.Scores[this];
        return heuristic;
    }

    private float CalculateZoneScore(int neighbors = 5)
    {
        var nearest = simulationControl
            .HeuristicDatabase.Scores.Keys.Where(p => p != this)
            .OrderBy(p => Vector3.Distance(p.Position, Position))
            .Take(neighbors)
            .Select(p => Vector3.Distance(enemyAI.transform.position, p.Position));

        return nearest.Any() ? nearest.Average() / simulationControl.MapMaxDistance : 0f;
    }

    private float GetNormalizedScore()
    {
        return simulationControl.HeuristicDatabase.normalized.Scores.TryGetValue(
            this,
            out float normalized
        )
            ? normalized
            : 0f;
    }

    public void OnInteract()
    {
        if (!simulationControl.IsLearningEnabled)
        {
            return;
        }

        float distance =
            Vector3.Distance(enemyAI.transform.position, enemyAI.Player.transform.position)
            / simulationControl.MapMaxDistance;
        float expTime = enemyAI.BeingSeen ? enemyAI.UpdateFrequency : -enemyAI.UpdateFrequency;
        float reactionTime =
            distance
            - (enemyAI.GetLastUpdateAIDistanceFromTarget() / simulationControl.MapMaxDistance);

        Report.FeedReport(distance, expTime, reactionTime);
        UpdateCache();
    }

    public override bool Equals(object obj) =>
        obj is HidePoint other
        && PublicMethods.RoundVector3(Position) == PublicMethods.RoundVector3(other.Position);

    public override int GetHashCode() => Position.GetHashCode();
}
