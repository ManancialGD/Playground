public class HidePointInstantReport
{
    public HidePoint Point { get; private set; }
    public (float previous, float current) DistanceFromEnemy { get; private set; } // maintained distance from enemy
    public (float previous, float current) ExpositionTime { get; private set; } // average time being exposed
    public (float previous, float current) ReactionTime { get; private set; } // speed/ease of moving away from the enemy
    public int InteractionsNumber { get; private set; } // number of interactions with this point

    public HidePointInstantReport(
        HidePoint hidePoint,
        (float previous, float current) distanceFromEnemy,
        (float previous, float current) expositionTime,
        (float previous, float current) reactionTime,
        int interactionsNumber
    )
    {
        Point = hidePoint;
        DistanceFromEnemy = distanceFromEnemy;
        ExpositionTime = expositionTime;
        ReactionTime = reactionTime;
        InteractionsNumber = interactionsNumber;
    }
}
