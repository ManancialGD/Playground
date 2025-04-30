using UnityEngine;

public class HidePointInteractionReport
{
    private ScoresLearner learner;
    private HidePoint Point;
    public float DistanceFromEnemy { get; private set; } // maintained distance from enemy
    public float ReactionTime { get; private set; } // speed/ease of moving away from the enemy
    public float ExpositionTime { get; private set; } // average time beeing exposed

    public HidePointInteractionReport(
        HidePoint point,
        float distanceFromEnemy,
        float expositionTime,
        float reactionTime,
        ScoresLearner learner
    )
    {
        this.Point = point;
        this.ExpositionTime = Mathf.Max(expositionTime, 0f);
        this.DistanceFromEnemy = Mathf.Max(distanceFromEnemy, 0f);
        this.ReactionTime = Mathf.Max(reactionTime, 0f);
        this.learner = learner;
    }

    public HidePointInteractionReport(HidePoint point, ScoresLearner learner)
    {
        this.Point = point;
        this.ExpositionTime = 0f;
        this.DistanceFromEnemy = 0f;
        this.ReactionTime = 0f;
        this.learner = learner;
    }

    public void FeedReport(float distance, float expositionTime, float reactionTime)
    {
        this.DistanceFromEnemy = Mathf.Max(
            Mathf.Lerp(
                this.DistanceFromEnemy,
                distance,
                learner.simulationControl.TrainingEntity.UpdateFrequency * learner.LearningRate
            ),
            0
        );
        this.ExpositionTime = Mathf.Max(
            this.ExpositionTime + (expositionTime * learner.LearningRate),
            0
        );
        this.ReactionTime = Mathf.Max(
            Mathf.Lerp(
                this.ReactionTime,
                reactionTime,
                learner.simulationControl.TrainingEntity.UpdateFrequency * learner.LearningRate
            ),
            0
        );

        float heuristic = Point.CalculateSingleHeuristic();
        learner.Scores.SetScore(Point, heuristic);
    }
}
