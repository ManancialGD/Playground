using UnityEngine;

public class HidePointInteractionReport
{
    private ScoresLearner learner;
    private HidePoint Point;
    public float DistanceFromEnemy { get; private set; } // maintained distance from enemy
    public float ReactionTime { get; private set; } // speed/ease of moving away from the enemy
    public float ExpositionTime { get; private set; } // average time beeing exposed
    public int InteractionsNumber { get; private set; } // number of interactions with this point

    public void SetInteractionsNumber(int interactions) => InteractionsNumber = interactions;

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
        this.InteractionsNumber = 1;
    }

    public HidePointInteractionReport(HidePoint point, ScoresLearner learner)
    {
        this.Point = point;
        this.ExpositionTime = 0f;
        this.DistanceFromEnemy = 0f;
        this.ReactionTime = 0f;
        this.learner = learner;
        this.InteractionsNumber = 1;
    }

    public void FeedReport(float distance, float expositionTime, float reactionTime)
    {
        InteractionsNumber++;
        HidePointInstantReport report = new HidePointInstantReport(
            Point,
            (DistanceFromEnemy, distance),
            (ExpositionTime, expositionTime),
            (ReactionTime, reactionTime),
            InteractionsNumber
        );

        if (learner != null)
        {
            learner.LearnFromReport(report);
        }

        UpdateReport(distance, expositionTime, reactionTime);
    }

    void UpdateReport(float distance, float expositionTime, float reactionTime)
    {
        float learningRate = learner.LearningRate;
        DistanceFromEnemy = Mathf.Lerp(DistanceFromEnemy, distance, learningRate);
        ExpositionTime = Mathf.Lerp(ExpositionTime, expositionTime, learningRate);
        ReactionTime = Mathf.Lerp(ReactionTime, reactionTime, learningRate);

        // atualiza o Report com o mesmo peso que atualiza a heuristica (ignorando noise)
    }
}
