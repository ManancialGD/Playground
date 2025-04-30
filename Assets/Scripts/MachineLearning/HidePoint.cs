using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class HidePoint
{
    private EnemyAI player;
    public Vector3 Position { get; private set; }
    public float Score => GetHidePointScore(database.Scores.Keys.ToList(), player, true); // auto updates shared database
    public float Heuristic => CalculateSingleHeuristic();
    public float ZoneScore => CalculateZoneScore();
    public HidePointInteractionReport Report { get; private set; }
    public EntityAIConfiguration Config { get; private set; }
    private ScoresLearner learnerAI;
    private ScoresDatabase database;
    private SimulationControl simulationControl;

    public float ScoreNormalized()
    {
        float score = Score; // update database;
        return simulationControl.ScoresDatabase.normalized.Scores[this];
    }

    public HidePoint(
        Vector3 position,
        EntityAIConfiguration config,
        EnemyAI player,
        ScoresDatabase database,
        SimulationControl simulationControl
    )
    {
        this.simulationControl = simulationControl;
        learnerAI = simulationControl.LearnerAI;
        Position = PublicMethods.RoundVector3(position);
        Report = new HidePointInteractionReport(this, learnerAI);
        Config = config;
        this.database = database;
        this.player = player;
    }

    public float CalculateZoneScore(int pointsAsked = 5)
    {
        if (database.Scores.Count <= 0)
            return 0;

        if (pointsAsked > database.Scores.Count)
            pointsAsked = database.Scores.Count;

        Dictionary<HidePoint, float> distances = new Dictionary<HidePoint, float>();

        foreach (HidePoint point in database.Scores.Keys)
        {
            float dist = Vector3.Distance(player.transform.position, point.Position);
            distances.Add(point, dist);
        }

        distances.OrderBy(x => x.Value);
        float sum = 0;

        int index = 0;
        foreach (HidePoint point in distances.Keys)
        {
            if (index >= pointsAsked)
                break;

            sum += distances[point];
            index++;
        }

        float zoneScore = sum / pointsAsked;
        return zoneScore;
    }

    public void ChangeEntityConfig(EntityAIConfiguration config)
    {
        Config = config;
    }

    public float CalculateSingleHeuristic()
    {
        float heuristic = Report.DistanceFromEnemy * learnerAI.DistanceImportance;
        heuristic += Report.ExpositionTime * learnerAI.ExpositionImportance;
        heuristic += Report.ReactionTime * learnerAI.ReactionImportance;
        return heuristic;
    }

    public void OnInteract()
    {
        float dist = Vector3.Distance(player.Player.transform.position, this.Position);
        float expTime = player.BeingSeen ? player.UpdateFrequency : -player.UpdateFrequency;
        float reactionTime =
            Vector3.Distance(player.transform.position, player.Player.transform.position)
            - player.GetLastUpdateAIDistanceFromTarget();

        Report.FeedReport(dist, expTime, reactionTime);
        // Debug.LogWarning("Hide point interacted");
    }

    float GetHidePointScore(
        List<HidePoint> hidePoints,
        EnemyAI player,
        bool updateCollection = false
    )
    {
        if (this.Position == Vector3.zero)
            return 0;

        if (hidePoints.Count == 0)
            return 0;

        Dictionary<HidePoint, float> distances = new Dictionary<HidePoint, float>();

        foreach (HidePoint point in hidePoints)
        {
            if (this.Position == point.Position || distances.ContainsKey(point))
                continue;

            float distance = Vector3.Distance(this.Position, point.Position);
            distances.Add(point, distance);
        }

        float avgDistance =
            distances.Count > 0
                ? distances.Sum(d => d.Value)
                    / distances.Count
                    / (simulationControl.MapMaxDistance / 2f) // because value will always be distant from MapMaxDistance
                : 0;
        float ownDistance =
            Vector3.Distance(this.Position, player.transform.position)
            / simulationControl.MapMaxDistance;
        float enemyDistance =
            Vector3.Distance(this.Position, player.Player.transform.position)
            / simulationControl.MapMaxDistance;

        Vector3 playerDir = (this.Position - player.transform.position).normalized;
        Vector3 enemyDir = (player.transform.position - player.transform.position).normalized;
        float dot = Vector3.Dot(playerDir, enemyDir);

        float score = enemyDistance * Config.EnemyDistance_Importance;

        score -= avgDistance * Config.AverageSpotDistance_Importance;
        score -= ownDistance * Config.OwnDistance_Importance;
        score += dot / 2 * Config.DotDirection_Importance;
        score += learnerAI.Scores.normalized.Scores[this] * Config.MapHeuristic_Importance;

        if (simulationControl.ScoresDatabase.normalized.Scores.ContainsKey(this))
            score +=
                simulationControl.ScoresDatabase.normalized.Scores[this]
                * Config.MapHeuristic_Importance;

        score += ZoneScore / simulationControl.MapMaxDistance * Config.ZoneScore_Importance; // zone score (average of the x(default=5) nearest points)

        if (updateCollection)
            database.SetScore(this, score);

        return score;
    }

    public override bool Equals(object obj)
    {
        if (obj is HidePoint other)
        {
            return Position == other.Position;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Position.GetHashCode();
    }
}
