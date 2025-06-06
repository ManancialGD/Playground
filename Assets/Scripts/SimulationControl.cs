using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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
    public ScoresDatabase HeuristicDatabase { get; set; } = new ScoresDatabase();
    public EntityAIConfiguration CurrentConfig { get; private set; } = null;
    EntityAIConfiguration configHistory = new EntityAIConfiguration();

    [SerializeField]
    private ScoresFileSystem scoresFileSystem;

    [SerializeField]
    private VisualizeAlgorithms visualizeAlgorithms;

    [SerializeField]
    public LayerMask mapLayer;

    bool databaseLoaded = false;
    public bool DatabaseLoaded => databaseLoaded;

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

        StartDatabase();
    }

    void StartDatabase()
    {
        CurrentConfig = GetConfig();
        HeuristicDatabase = scoresFileSystem.LoadData();
        databaseLoaded = true;

        Debug.Log("Database loaded successfully");
    }

    void Update()
    {
        if (TrainingEntity == null)
            return;

        bool same = CompareConfigs(configHistory, GetConfig());
        if (!same)
        {
            CurrentConfig = GetConfig();

            foreach (HidePoint point in HeuristicDatabase.Scores.Keys)
                point.ChangeEntityConfig(CurrentConfig);

            Debug.LogWarning("Configuration changed");
        }
    }

    private EntityAIConfiguration GetConfig()
    {
        EntityAIConfiguration config = new EntityAIConfiguration(
            TrainingEntity.AI_D_Importance,
            TrainingEntity.AI_OD_Importance,
            TrainingEntity.AI_AD_Importance,
            TrainingEntity.AI_Dot_Importance,
            TrainingEntity.AI_MH_Importance,
            TrainingEntity.AI_ZS_Importance,
            TrainingEntity.AI_EX_Importance
        );

        configHistory = config;

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

    private int ColliderArraySortComparer(Collider A, Collider B)
    {
        NavMeshAgent Agent = TrainingEntity.Agent;
        if (A == null && B != null)
        {
            return 1;
        }
        else if (A != null && B == null)
        {
            return -1;
        }
        else if (A == null && B == null)
        {
            return 0;
        }
        else
        {
            return Vector3
                .Distance(Agent.transform.position, A.transform.position)
                .CompareTo(Vector3.Distance(Agent.transform.position, B.transform.position));
        }
    }

    GameObject GetGameObjectFromNavMeshHit(NavMeshHit hit, float radius = 1f)
    {
        Collider[] colliders = Physics.OverlapSphere(hit.position, radius);

        foreach (Collider col in colliders)
        {
            if (col.gameObject.CompareTag("World"))
            {
                return col.gameObject;
            }
        }

        return null;
    }

    float GetWallWidth(Transform wallTransform, Vector3 playerPosition)
    {
        Collider collider = wallTransform.GetComponent<Collider>();
        if (collider == null)
        {
            Debug.LogError("Nenhum Collider encontrado na parede!");
            return 0f;
        }

        Vector3 directionToWall = (wallTransform.position - playerPosition).normalized;

        Vector3 wallSize = collider.bounds.size;

        float width =
            Mathf.Abs(Vector3.Dot(wallTransform.right, directionToWall))
            > Mathf.Abs(Vector3.Dot(wallTransform.forward, directionToWall))
                ? wallSize.x
                : wallSize.z;

        return width;
    }
}
