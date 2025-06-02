public class EntityAIConfiguration
{
    public float EnemyDistance_Importance { get; private set; }
    public float OwnDistance_Importance { get; private set; }
    public float AverageSpotDistance_Importance { get; private set; }
    public float DotDirection_Importance { get; private set; }
    public float MapHeuristic_Importance { get; private set; }
    public float ZoneScore_Importance { get; private set; }
    public float Exploration_Importance { get; private set; }

    public EntityAIConfiguration(
        float enemyDistance,
        float ownDistance,
        float averageSpotDistance,
        float dotDirection,
        float mapHeuristic,
        float zoneScore,
        float explorationImportance
    )
    {
        EnemyDistance_Importance = enemyDistance;
        OwnDistance_Importance = ownDistance;
        AverageSpotDistance_Importance = averageSpotDistance;
        DotDirection_Importance = dotDirection;
        MapHeuristic_Importance = mapHeuristic;
        ZoneScore_Importance = zoneScore;
        Exploration_Importance = explorationImportance;
    }

    public EntityAIConfiguration()
    {
        EnemyDistance_Importance = 0;
        OwnDistance_Importance = 0;
        AverageSpotDistance_Importance = 0;
        DotDirection_Importance = 0;
        MapHeuristic_Importance = 0;
        ZoneScore_Importance = 0;
        Exploration_Importance = 0;
    }

    public bool Compare(float D, float AD, float OD, float Dot, float MH, float ZS, float EX)
    {
        if (
            this.EnemyDistance_Importance != D
            || this.AverageSpotDistance_Importance != AD
            || this.OwnDistance_Importance != OD
            || this.DotDirection_Importance != Dot
            || this.MapHeuristic_Importance != MH
            || this.ZoneScore_Importance != ZS
            || this.Exploration_Importance != EX
        )
            return false;
        else
            return true;
    }

    public override bool Equals(object obj)
    {
        if (obj is EntityAIConfiguration other)
        {
            if (
                this.EnemyDistance_Importance != other.EnemyDistance_Importance
                || this.AverageSpotDistance_Importance != other.AverageSpotDistance_Importance
                || this.OwnDistance_Importance != other.OwnDistance_Importance
                || this.DotDirection_Importance != other.DotDirection_Importance
                || this.MapHeuristic_Importance != other.MapHeuristic_Importance
                || this.ZoneScore_Importance != other.ZoneScore_Importance
                || this.Exploration_Importance != other.Exploration_Importance
            )
                return false;
            else
                return true;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return this.GetHashCode();
    }

    public void ChangeConfiguration(
        float enemyDistance,
        float ownDistance,
        float averageSpotDistance,
        float dotDirection,
        float mapHeuristic,
        float zoneScore,
        float explorationImportance
    )
    {
        EnemyDistance_Importance = enemyDistance;
        OwnDistance_Importance = ownDistance;
        AverageSpotDistance_Importance = averageSpotDistance;
        DotDirection_Importance = dotDirection;
        MapHeuristic_Importance = mapHeuristic;
        ZoneScore_Importance = zoneScore;
        Exploration_Importance = explorationImportance;
    }
}
