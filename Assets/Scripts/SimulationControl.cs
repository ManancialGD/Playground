using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulationControl : MonoBehaviour
{
    [SerializeField]
    public float SimulationSpeed = 1.0f;

    [SerializeField]
    public EnemyAI TrainingEntity;

    [SerializeField]
    public ScoresLearner LearnerAI;

    [SerializeField]
    public float MapMaxDistance = 0;
    public ScoresDatabase ScoresDatabase { get; set; } = new ScoresDatabase();
    public ScoresDatabase HeuristicDatabase { get; set; } = new ScoresDatabase();
    public EntityAIConfiguration CurrentConfig { get; private set; } = null;
    float[] configHistory = new float[6];

    [SerializeField]
    private VisualizeAlgorithms visualizeAlgorithms;

    private void Awake()
    {
        if (TrainingEntity == null)
            return;

        if (MapMaxDistance == 0)
        {
            Debug.LogError(
                "Map max distance not defined. This value is required for normalization of HidePoints scores"
            );
            return;
        }

        CurrentConfig = GenerateConfig(TrainingEntity);
        HeuristicDatabase = LearnerAI.LoadData();
        ScoresDatabase = new ScoresDatabase();
    }

    void Update()
    {
        if (TrainingEntity == null)
            return;

        bool same = CurrentConfig.Compare(
            TrainingEntity.AI_D_Importance,
            TrainingEntity.AI_OD_Importance,
            TrainingEntity.AI_AD_Importance,
            TrainingEntity.AI_Dot_Importance,
            TrainingEntity.AI_MH_Importance,
            TrainingEntity.AI_ZS_Importance
        ); // more economic

        if (!same)
        {
            CurrentConfig = GenerateConfig(TrainingEntity);

            foreach (HidePoint point in ScoresDatabase.Scores.Keys)
                point.ChangeEntityConfig(CurrentConfig);

            foreach (HidePoint point in HeuristicDatabase.Scores.Keys)
                point.ChangeEntityConfig(CurrentConfig);

            // Debug.LogWarning("Configuration changed");
        }
    }

    private EntityAIConfiguration GenerateConfig(EnemyAI entity)
    {
        if (entity == null)
            return null;

        EntityAIConfiguration config = new EntityAIConfiguration(
            TrainingEntity.AI_D_Importance,
            TrainingEntity.AI_OD_Importance,
            TrainingEntity.AI_AD_Importance,
            TrainingEntity.AI_Dot_Importance,
            TrainingEntity.AI_MH_Importance,
            TrainingEntity.AI_ZS_Importance
        );

        configHistory[0] = TrainingEntity.AI_D_Importance;
        configHistory[1] = TrainingEntity.AI_OD_Importance;
        configHistory[2] = TrainingEntity.AI_AD_Importance;
        configHistory[3] = TrainingEntity.AI_Dot_Importance;
        configHistory[4] = TrainingEntity.AI_MH_Importance;
        configHistory[5] = TrainingEntity.AI_ZS_Importance;

        return config;
    }

    private bool CompareConfigs(EntityAIConfiguration config1, EntityAIConfiguration config2)
    {
        if (
            config1.EnemyDistance_Importance != config2.EnemyDistance_Importance
            || config1.OwnDistance_Importance != config2.OwnDistance_Importance
            || config1.AverageSpotDistance_Importance != config2.AverageSpotDistance_Importance
            || config1.DotDirection_Importance != config2.DotDirection_Importance
            || config1.MapHeuristic_Importance != config2.MapHeuristic_Importance
            || config1.ZoneScore_Importance != config2.ZoneScore_Importance
        )
            return false;

        return true;
    }
}
