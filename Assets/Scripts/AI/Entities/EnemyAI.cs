using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DataSpace;
using DecisionTreeAI;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [SerializeField]
    float Speed;

    [SerializeField]
    string ReportName;

    [SerializeField]
    LayerMask WorldLayer;

    [SerializeField]
    EnemyState ForceState = EnemyState.None;
    public Transform Player;

    public EnemyLineOfSightChecker LineOfSightChecker;

    [HideInInspector]
    public NavMeshAgent Agent;
    NavMeshPath Path;

    public bool BeingSeen { get; private set; } = false;

    Data distanceReport;
    Data visionReport;
    Data directionReport;
    Data scoresReport;

    private float historyDistanceFromEnemy = 0f;

    public float GetLastUpdateAIDistanceFromTarget() => historyDistanceFromEnemy;

    enum EnemyState
    {
        None,
        Attack,
        Defense,
    }

    ControlNode TreeRootAI;
    private float AI_LastUpdate;

    public void SetPath(NavMeshPath path) => Path = path;

    [Range(-1, 1)]
    [Tooltip("Lower is a better hiding spot")]
    public float HideSensitivity = 0;

    [Range(1, 100)]
    public float MinPlayerDistance = 10f;

    [Range(0, 50f)]
    public float MinObstacleHeight = 1.25f;

    [Range(0f, 1f)]
    public float UpdateFrequency = 0.25f;
    private float baseUpdateFrequency;

    [Header("AI Parameters")]
    [Range(0f, 1f)]
    public float AI_D_Importance = 0;

    [Range(0f, 1f)]
    public float AI_AD_Importance = 0;

    [Range(0f, 1f)]
    public float AI_OD_Importance = 0;

    [Range(0f, 1f)]
    public float AI_Dot_Importance = 0;

    [Range(0f, 1f)]
    public float AI_MH_Importance = 0;

    [Range(0f, 1f)]
    public float AI_ZS_Importance = 0;

    [Range(0f, 1f)]
    public float AI_EX_Importance = 0;

    private Coroutine MovementCoroutine;
    public Collider[] Colliders { get; private set; } = new Collider[100]; // more is less performant, but more options

    private List<HidePoint> DetectedPositions = new List<HidePoint>(); // current detected positions (not all)
    public HidePoint TargetPoint { get; private set; } = null;

    [SerializeField]
    ScoresLearner HidingLearner;

    [SerializeField]
    SimulationControl simulationControl;

    public ScoresDatabase HidingScores { get; private set; }

    private void Start()
    {
        Agent = GetComponent<NavMeshAgent>();
        
        if (Player == null)
        {
            Debug.LogError("EnemyAI.Player is not assigned in the inspector!");
            enabled = false;
            return;
        }
        if (LineOfSightChecker == null)
        {
            Debug.LogError("EnemyAI.LineOfSightChecker is not assigned in the inspector!");
            enabled = false;
            return;
        }
        if (simulationControl == null)
        {
            Debug.LogError("EnemyAI.simulationControl is not assigned in the inspector!");
            enabled = false;
            return;
        }

        LineOfSightChecker.OnGainSight += HandleGainSight;
        LineOfSightChecker.OnLoseSight += HandleLoseSight;
        TreeRootAI = CreateTreeAI();
        StartCoroutine(UpdatePlaytestData());

        baseUpdateFrequency = UpdateFrequency;

        HidingScores = simulationControl.ScoresDatabase;
        historyDistanceFromEnemy = Vector3.Distance(transform.position, Player.position);
    }

    private void FixedUpdate()
    {
        if (Time.timeScale != simulationControl.SimulationSpeed)
            Time.timeScale = simulationControl.SimulationSpeed;
        UpdateAI();

        //Vector3 direction = (Player.transform.position - transform.position).normalized;
        /*
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.Euler(direction),
            0.5f
        );
        */
    }

    private void HandleGainSight(Transform Target)
    {
        BeingSeen = true;
        return;
    }

    private void HandleLoseSight(Transform Target)
    {
        BeingSeen = false;
        return;
    }

    private IEnumerator Hide(Transform Target)
    {
        WaitForSeconds Wait = new WaitForSeconds(UpdateFrequency);

        while (true)
        {
            if (simulationControl.ScoresDatabase.Scores.Count <= 0)
            {
                yield return Wait;
                continue;
            }
            TargetPoint = simulationControl.CurrentBestPoint;
            NavMeshPath path = null;
            path = FindNormalPath(transform.position, TargetPoint.Position);
            if (path != null)
            {
                Agent.ResetPath();
                Agent.SetPath(path);
            }
            else
            {
                Agent.ResetPath();
                Agent.SetDestination(TargetPoint.Position);
            }

            yield return Wait;
        }
    }

    ControlNode CreateTreeAI()
    {
        distanceReport = new Data(ReportName);
        visionReport = new Data(ReportName + "_Vision");
        directionReport = new Data(ReportName + "_Direction");
        scoresReport = new Data(ReportName + "_Scores");
        ControlNode root = new ControlNode(AttackOrDefense);

        int AttackOrDefense(float distance)
        {
            if (ForceState != EnemyState.None)
            {
                switch (ForceState)
                {
                    case EnemyState.Attack:
                        return 1;
                    case EnemyState.Defense:
                        return 0;

                    default:
                        Debug.LogError("Invalid EnemyState");
                        break;
                }
            }

            if (distance <= MinPlayerDistance)
                return 0; // defense
            else
                return 1; // attack
        }

        ExecutionNode defenseNode = new ExecutionNode(input =>
        {
            Defense(input);
            AI_LastUpdate = Time.time;
            return NodeStatus.Success;
        });

        ControlNode chooseAttack = new ControlNode(beingSeen =>
        {
            // input should be ( float 0 or 1 where 0 is stealth and 1 is brute )
            // enemy always attack stealth until it is caught

            if (beingSeen < 1)
                return 0; // attack stealth
            else
                return 1; // attack brute
        });

        ExecutionNode stealthAttackNode = new ExecutionNode(input =>
        {
            return StealthAttack(Player);
        });

        ExecutionNode bruteAttackNode = new ExecutionNode(input =>
        {
            return BruteAttack(Player);
        });

        root.AddChild(defenseNode).AddChild(chooseAttack);
        chooseAttack.AddChild(stealthAttackNode).AddChild(bruteAttackNode);

        NodeStatus Defense(float value)
        {
            if (MovementCoroutine != null)
                StopCoroutine(MovementCoroutine);
            MovementCoroutine = StartCoroutine(Hide(Player));

            return NodeStatus.Success;
        }

        return root;
    }

    IEnumerator UpdatePlaytestData()
    {
        WaitForSeconds wait = new WaitForSeconds(0.1f);
        while (true)
        {
            float seen = BeingSeen ? 30 : 0;
            float dist = Vector3.Distance(transform.position, Player.position);
            float dot =
                (
                    1
                    + Vector3.Dot(
                        transform.rotation.eulerAngles,
                        Player.transform.rotation.eulerAngles
                    )
                ) * 15f;
            float currentPointScore = TargetPoint != null ? TargetPoint.NormalizedScore * 30f : 0;

            distanceReport.Feed((Time.time, (dist, 0)));
            visionReport.Feed((Time.time, (seen, 0)));
            directionReport.Feed((Time.time, (dot, 0)));
            scoresReport.Feed((Time.time, (currentPointScore, 0)));
            yield return wait;
        }
    }

    void UpdateAI()
    {
        float distance = Vector3.Distance(transform.position, Player.position);
        historyDistanceFromEnemy = distance;
        /*
        if (Time.time - AI_LastUpdate >= AI_UpdateInterval)
        {
            AI_LastUpdate = Time.time;
            TreeRootAI.Execute(distance);
        }
        */
        if (TreeRootAI.DecisionFunction(distance) != TreeRootAI.LastChildIndex)
        {
            TreeRootAI.Execute(distance);
            Debug.LogWarning("Tree root AI changed state");
        }
    }

    void ForceUpdateAI()
    {
        float distance = Vector3.Distance(transform.position, Player.position);
        TreeRootAI.Execute(distance);
    }

    bool CanSee(Vector3 pos1, Vector3 pos2)
    {
        float distance = Vector3.Distance(pos1, pos2);
        Vector3 dir = (pos2 - pos1).normalized; // Agora aponta na direção correta

        if (Physics.Raycast(pos1, dir, out RaycastHit hit, distance, WorldLayer))
        {
            return hit.collider.CompareTag("EnemyAI") && hit.collider.gameObject != gameObject;
        }

        return false;
    }

    private void UpdateDatabases(HidePoint point, float score = 0)
    {
        HidingScores.SetScore(point, score);
        simulationControl.HeuristicDatabase.SetScore(point, 0);
    }

    public static Vector3 RoundVector3(Vector3 v, int decimalPlaces = 4)
    {
        float multiplier = Mathf.Pow(10, decimalPlaces);
        return new Vector3(
            Mathf.Round(v.x * multiplier) / multiplier,
            Mathf.Round(v.y * multiplier) / multiplier,
            Mathf.Round(v.z * multiplier) / multiplier
        );
    }

    HidePoint GetBetterHidePoint(out float hidePointScore)
    {
        HidePoint bestPoint = simulationControl.CurrentBestPoint;
        hidePointScore = bestPoint.Score; // ( updating the point is not idial )
        return simulationControl.CurrentBestPoint;
    }

    public NavMeshPath FindNormalPath(Vector3 startPosition, Vector3 TargetPoint)
    {
        NavMeshPath tempPath = new NavMeshPath();

        if (!NavMesh.CalculatePath(startPosition, TargetPoint, Agent.areaMask, tempPath))
        {
            Debug.LogWarning("Não foi possível calcular um caminho válido.");
            Debug.LogWarning("startPosition: " + startPosition);
            Debug.LogWarning("TargetPoint: " + TargetPoint);
            return null;
        }
        else
            return tempPath;
    }

    public NavMeshPath GetSafeNavMeshPath(Vector3 startPosition, Vector3 TargetPoint)
    {
        NavMeshPath tempPath = new NavMeshPath();

        if (!NavMesh.CalculatePath(startPosition, TargetPoint, Agent.areaMask, tempPath))
        {
            Debug.LogWarning("Não foi possível calcular um caminho válido.");
            Debug.LogWarning("startPosition: " + startPosition);
            Debug.LogWarning("TargetPoint: " + TargetPoint);
            return null;
        }

        List<Vector3> filteredPoints = new List<Vector3>();
        Vector3 previousPoint = startPosition;

        foreach (Vector3 point in tempPath.corners)
        {
            if (CanSee(point, Player.position))
            {
                Vector3 alternativePoint = FindAlternativePoint(previousPoint, point, TargetPoint);
                if (alternativePoint != Vector3.zero)
                {
                    filteredPoints.Add(alternativePoint);
                    previousPoint = alternativePoint;
                    continue;
                }
                else
                {
                    Debug.LogWarning("Não há caminho seguro possível.");
                    return null; // Retorna o caminho original se não houver alternativa
                }
            }

            filteredPoints.Add(point);
            previousPoint = point;
        }

        // Criar um novo caminho baseado nos pontos filtrados
        NavMeshPath safePath = new NavMeshPath();
        if (filteredPoints.Count > 1)
        {
            NavMesh.CalculatePath(startPosition, filteredPoints.Last(), Agent.areaMask, safePath);
            return safePath;
        }
        else
            return null;
    }

    private Vector3 FindAlternativePoint(Vector3 from, Vector3 unsafePoint, Vector3 enemyPosition)
    {
        Collider[] obstacles = Physics.OverlapSphere(unsafePoint, 10f, simulationControl.mapLayer);

        foreach (Collider obstacle in obstacles)
        {
            Vector3 candidatePoint = obstacle.ClosestPoint(from);

            if (NavMesh.SamplePosition(candidatePoint, out NavMeshHit hit, 2f, Agent.areaMask))
            {
                if (!CanSee(hit.position, enemyPosition))
                {
                    return hit.position;
                }
            }
        }

        return Vector3.zero; // Nenhuma alternativa segura encontrada
    }

    private NodeStatus StealthAttack(Transform player)
    {
        Coroutine stealthCoroutine = StartCoroutine(StealthAttackBehaviour(player));
        if (stealthCoroutine == null)
            return NodeStatus.Failure;

        return NodeStatus.Success;
    }

    private NodeStatus BruteAttack(Transform player)
    {
        Coroutine stealthCoroutine = StartCoroutine(StealthAttackBehaviour(player));
        if (stealthCoroutine == null)
            return NodeStatus.Failure;

        return NodeStatus.Success;
    }

    private IEnumerator StealthAttackBehaviour(Transform player)
    {
        WaitForSeconds wait = new WaitForSeconds(UpdateFrequency);
        while (true)
        {
            NavMeshPath safePath = FindNormalPath(transform.position, player.position);

            if (safePath == null)
            {
                Debug.LogError("Stealth path not found");
                yield return wait;
                continue;
            }

            if (safePath.corners.Length > 0)
            {
                Agent.SetPath(safePath);
            }
            else
            {
                Debug.LogError("Stealth path not found");
                yield return wait;
            }
        }
    }

    private IEnumerator BruteAttackBehaviour(Transform player)
    {
        WaitForSeconds wait = new WaitForSeconds(UpdateFrequency);
        while (true)
        {
            NavMeshPath safePath = FindNormalPath(transform.position, player.position);

            if (safePath == null)
            {
                Debug.LogError("Stealth path not found");
                yield return wait;
                continue;
            }

            if (safePath.corners.Length > 0)
            {
                Agent.SetPath(safePath);
            }
            else
            {
                Debug.LogError("Stealth path not found");
                yield return wait;
            }
        }
    }
}
