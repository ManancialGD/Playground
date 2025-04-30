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
    public EnemyAI Player;

    [SerializeField]
    public LayerMask HidableLayers;

    public EnemyLineOfSightChecker LineOfSightChecker;

    [HideInInspector]
    public NavMeshAgent Agent;
    NavMeshPath Path;
    public Rigidbody RB;

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
    [Range(0f, 10f)]
    public float AI_D_Importance = 1;

    [Range(0f, 10f)]
    public float AI_AD_Importance = 1;

    [Range(0f, 10f)]
    public float AI_OD_Importance = 1;

    [Range(0f, 10f)]
    public float AI_Dot_Importance = 1;

    [Range(0f, 10f)]
    public float AI_MH_Importance = 1;

    [Range(0f, 10f)]
    public float AI_ZS_Importance = 1;

    private Coroutine MovementCoroutine;
    public Collider[] Colliders { get; private set; } = new Collider[40]; // more is less performant, but more options

    private List<HidePoint> DetectedPositions = new List<HidePoint>(); // current detected positions (not all)
    public HidePoint TargetPoint { get; private set; } = null;

    [SerializeField]
    ScoresLearner HidingLearner;

    [SerializeField]
    SimulationControl simulationControl;
    float baseVelocity = 0;

    public ScoresDatabase HidingScores { get; private set; }

    private void Start()
    {
        Agent = GetComponent<NavMeshAgent>();
        RB = GetComponent<Rigidbody>();

        LineOfSightChecker.OnGainSight += HandleGainSight;
        LineOfSightChecker.OnLoseSight += HandleLoseSight;
        TreeRootAI = CreateTreeAI();
        StartCoroutine(UpdatePlaytestData());

        baseUpdateFrequency = UpdateFrequency;
        UpdateFrequency /= simulationControl.SimulationSpeed;

        baseVelocity = Agent.speed;

        HidingScores = simulationControl.ScoresDatabase;
        historyDistanceFromEnemy = Vector3.Distance(transform.position, Player.transform.position);
    }

    private void FixedUpdate()
    {
        // Simulation Control
        Agent.avoidancePriority = (int)
            Mathf.Clamp(50f / simulationControl.SimulationSpeed, 0f, 99f);
        UpdateFrequency = baseUpdateFrequency / simulationControl.SimulationSpeed;
        Agent.speed = baseVelocity * simulationControl.SimulationSpeed;
        Agent.acceleration = baseVelocity * simulationControl.SimulationSpeed * 5f;
        Agent.angularSpeed = baseVelocity * simulationControl.SimulationSpeed * 100f;

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
            if (!Agent.enabled || !Agent.isOnNavMesh)
            {
                Debug.LogError("NavMeshAgent está desativado ou fora do NavMesh!");
                yield break;
            }

            for (int i = 0; i < Colliders.Length; i++)
            {
                Colliders[i] = null;
            }

            int hits = Physics.OverlapSphereNonAlloc(
                Agent.transform.position,
                LineOfSightChecker.Collider.radius,
                Colliders,
                HidableLayers
            );

            int hitReduction = 0;
            for (int i = 0; i < hits; i++)
            {
                if (!Colliders[i] || Colliders[i] == null)
                    continue;
                if (Colliders[i] == null
                //|| Vector3.Distance(Colliders[i].transform.position, Target.position)
                //  < MinPlayerDistance / 2
                //|| Colliders[i].bounds.size.y < MinObstacleHeight
                )
                {
                    Colliders[i] = null;
                    hitReduction++;
                }
            }
            hits -= hitReduction;

            System.Array.Sort(Colliders, ColliderArraySortComparer);
            DetectedPositions = new List<HidePoint>();

            int additionalHits = 0;

            for (int i = 0; i < hits; i++)
            {
                if (Colliders[i] == null)
                    continue;

                if (
                    NavMesh.SamplePosition(
                        Colliders[i].transform.position,
                        out NavMeshHit hit,
                        2f,
                        Agent.areaMask
                    )
                )
                {
                    if (!NavMesh.FindClosestEdge(hit.position, out hit, Agent.areaMask))
                    {
                        Debug.LogError(
                            $"Não foi possível encontrar a borda do NavMesh em {hit.position}"
                        );
                        continue;
                    }

                    Vector3 directionToPlayer = (Target.position - hit.position).normalized;
                    float dot = Vector3.Dot(hit.normal, directionToPlayer);

                    if (dot > HideSensitivity) // Se não estiver bem escondido, tenta outro ponto
                    {
                        continue;
                    }

                    GameObject wallObj = GetGameObjectFromNavMeshHit(hit);
                    float wallSize = GetWallWidth(wallObj.transform, transform.position);
                    if (wallSize >= 30f)
                    {
                        additionalHits += 2;
                        Vector3 wDir = (wallObj.transform.position - transform.position).normalized;
                        HidePoint leftPosition;
                        leftPosition = new HidePoint(
                            Mathf.Abs(Vector3.Dot(wallObj.transform.right, wDir))
                            > Mathf.Abs(Vector3.Dot(wallObj.transform.forward, wDir))
                                ? hit.position - new Vector3(wallSize / 4f, 0f, 0f)
                                : hit.position - new Vector3(0f, 0f, wallSize / 4f),
                            simulationControl.CurrentConfig,
                            this,
                            simulationControl.ScoresDatabase,
                            simulationControl
                        );

                        HidePoint rightPosition;
                        rightPosition = new HidePoint(
                            Mathf.Abs(Vector3.Dot(wallObj.transform.right, wDir))
                            > Mathf.Abs(Vector3.Dot(wallObj.transform.forward, wDir))
                                ? hit.position + new Vector3(wallSize / 4f, 0f, 0f)
                                : hit.position + new Vector3(0f, 0f, wallSize / 4f),
                            simulationControl.CurrentConfig,
                            this,
                            simulationControl.ScoresDatabase,
                            simulationControl
                        );

                        DetectedPositions.Add(leftPosition);
                        DetectedPositions.Add(rightPosition);
                        UpdateDatabases(rightPosition);
                        UpdateDatabases(leftPosition);
                    }
                    else if (wallSize >= 60f)
                    {
                        additionalHits += 4;
                        Vector3 wDir = (wallObj.transform.position - transform.position).normalized;
                        HidePoint leftPosition1;
                        leftPosition1 = new HidePoint(
                            Mathf.Abs(Vector3.Dot(wallObj.transform.right, wDir))
                            > Mathf.Abs(Vector3.Dot(wallObj.transform.forward, wDir))
                                ? hit.position - new Vector3(wallSize / 4f, 0f, 0f)
                                : hit.position - new Vector3(0f, 0f, wallSize / 4f),
                            simulationControl.CurrentConfig,
                            this,
                            simulationControl.ScoresDatabase,
                            simulationControl
                        );

                        HidePoint leftPosition2;
                        leftPosition2 = new HidePoint(
                            Mathf.Abs(Vector3.Dot(wallObj.transform.right, wDir))
                            > Mathf.Abs(Vector3.Dot(wallObj.transform.forward, wDir))
                                ? hit.position - new Vector3(wallSize / 2.2f, 0f, 0f)
                                : hit.position - new Vector3(0f, 0f, wallSize / 2.2f),
                            simulationControl.CurrentConfig,
                            this,
                            simulationControl.ScoresDatabase,
                            simulationControl
                        );

                        HidePoint rightPosition1;
                        rightPosition1 = new HidePoint(
                            Mathf.Abs(Vector3.Dot(wallObj.transform.right, wDir))
                            > Mathf.Abs(Vector3.Dot(wallObj.transform.forward, wDir))
                                ? hit.position + new Vector3(wallSize / 4f, 0f, 0f)
                                : hit.position + new Vector3(0f, 0f, wallSize / 4f),
                            simulationControl.CurrentConfig,
                            this,
                            simulationControl.ScoresDatabase,
                            simulationControl
                        );

                        HidePoint rightPosition2;
                        rightPosition2 = new HidePoint(
                            Mathf.Abs(Vector3.Dot(wallObj.transform.right, wDir))
                            > Mathf.Abs(Vector3.Dot(wallObj.transform.forward, wDir))
                                ? hit.position + new Vector3(wallSize / 2.2f, 0f, 0f)
                                : hit.position + new Vector3(0f, 0f, wallSize / 2.2f),
                            simulationControl.CurrentConfig,
                            this,
                            simulationControl.ScoresDatabase,
                            simulationControl
                        );

                        DetectedPositions.Add(leftPosition1);
                        DetectedPositions.Add(rightPosition1);
                        DetectedPositions.Add(leftPosition2);
                        DetectedPositions.Add(rightPosition2);

                        UpdateDatabases(leftPosition1, 0);
                        UpdateDatabases(rightPosition1, 0);
                        UpdateDatabases(leftPosition2, 0);
                        UpdateDatabases(rightPosition2, 0);
                    }

                    HidePoint centerPoint = new HidePoint(
                        hit.position,
                        simulationControl.CurrentConfig,
                        this,
                        simulationControl.ScoresDatabase,
                        simulationControl
                    );
                    DetectedPositions.Add(centerPoint);
                    UpdateDatabases(centerPoint, 0);
                }
                else
                {
                    Debug.LogError(
                        $"Não foi possível encontrar NavMesh próximo ao objeto {Colliders[i].name}"
                    );
                }
            }

            hits += additionalHits;

            float score = 0;
            HidePoint finalPoint = GetBetterHidePoint(out score);

            if (finalPoint == null || finalPoint.Position == null) // no point found
            {
                yield return Wait;
                continue;
            }

            if (finalPoint.Position != Vector3.zero)
            {
                TargetPoint = finalPoint;
                NavMeshPath path = null;
                path = GetSafeNavMeshPath(transform.position, finalPoint.Position);
                if (path != null)
                {
                    Agent.ResetPath();
                    Agent.SetPath(path);
                }
                else
                {
                    Agent.ResetPath();
                    Agent.SetDestination(finalPoint.Position);
                }
            }

            yield return Wait;
        }
    }

    public int ColliderArraySortComparer(Collider A, Collider B)
    {
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

        ExecutionNode defenseNode = new ExecutionNode(Defense);
        root.AddChild(defenseNode);

        ExecutionNode attackNode = new ExecutionNode(Attack);
        root.AddChild(attackNode);

        int Attack(float value)
        {
            if (MovementCoroutine != null)
                StopCoroutine(MovementCoroutine);
            Agent.ResetPath();
            //Agent.SetDestination(Player.transform.position);
            MovementCoroutine = StartCoroutine(AttackStealthMove(Player.transform));

            return 1;
        }

        int Defense(float value)
        {
            if (MovementCoroutine != null)
                StopCoroutine(MovementCoroutine);
            MovementCoroutine = StartCoroutine(Hide(Player.transform));

            return 1;
        }

        AI_LastUpdate = Time.time;
        return root;
    }

    IEnumerator UpdatePlaytestData()
    {
        WaitForSeconds wait = new WaitForSeconds(0.1f);
        while (true)
        {
            float seen = BeingSeen ? 30 : 0;
            float dist = Vector3.Distance(transform.position, Player.transform.position);
            float dot =
                (
                    1
                    + Vector3.Dot(
                        transform.rotation.eulerAngles,
                        Player.transform.rotation.eulerAngles
                    )
                ) * 15f;
            float currentPointScore = TargetPoint != null ? TargetPoint.ScoreNormalized() * 30f : 0;

            distanceReport.Feed((Time.time, (dist, 0)));
            visionReport.Feed((Time.time, (seen, 0)));
            directionReport.Feed((Time.time, (dot, 0)));
            scoresReport.Feed((Time.time, (currentPointScore, 0)));
            yield return wait;
        }
    }

    void UpdateAI()
    {
        float distance = Vector3.Distance(transform.position, Player.transform.position);
        historyDistanceFromEnemy = distance;
        /*
        if (Time.time - AI_LastUpdate >= AI_UpdateInterval)
        {
            AI_LastUpdate = Time.time;
            TreeRootAI.Execute(distance);
        }
        */
        if (TreeRootAI.Action(distance) != TreeRootAI.history)
        {
            TreeRootAI.Execute(distance);
            // Debug.LogWarning("Tree root AI updated");
        }
    }

    void ForceUpdateAI()
    {
        float distance = Vector3.Distance(transform.position, Player.transform.position);
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
        hidePointScore = 0;
        ScoresDatabase hidePoints = simulationControl.ScoresDatabase;
        if (hidePoints.Scores.Count <= 0)
        {
            Debug.LogError("No HidePoints in the database");
            return null;
        }

        List<HidePoint> points = new List<HidePoint>(hidePoints.Scores.Keys);

        foreach (HidePoint point in points)
        {
            float score = point.Score; // update database
        }

        HidePoint chosenPoint = hidePoints.Scores.OrderByDescending(h => h.Value).First().Key;
        hidePointScore = chosenPoint.Score;
        return chosenPoint;
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

    GameObject GetGameObjectFromNavMeshHit(NavMeshHit hit, float radius = 1f)
    {
        Collider[] colliders = Physics.OverlapSphere(hit.position, radius);

        foreach (Collider col in colliders)
        {
            if (col.gameObject.CompareTag("World")) // Filtra se necessário
            {
                return col.gameObject;
            }
        }

        return null; // Retorna null se nada for encontrado
    }

    public NavMeshPath GetSafeNavMeshPath(Vector3 startPosition, Vector3 TargetPoint)
    {
        NavMeshPath tempPath = new NavMeshPath();

        if (!NavMesh.CalculatePath(startPosition, TargetPoint, Agent.areaMask, tempPath))
        {
            Debug.LogWarning("Não foi possível calcular um caminho válido.");
            return null;
        }

        List<Vector3> filteredPoints = new List<Vector3>();
        Vector3 previousPoint = startPosition;

        foreach (Vector3 point in tempPath.corners)
        {
            if (CanSee(point, Player.transform.position))
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
        Collider[] obstacles = Physics.OverlapSphere(unsafePoint, 10f, HidableLayers);

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

    private IEnumerator AttackStealthMove(Transform player)
    {
        WaitForSeconds wait = new WaitForSeconds(UpdateFrequency);
        while (true)
        {
            NavMeshPath safePath = GetSafeNavMeshPath(
                transform.position,
                player.transform.position
            );
            if (safePath == null)
            {
                yield return wait;
                continue;
            }

            if (safePath.corners.Length > 0)
            {
                Agent.ResetPath();
                Agent.SetPath(safePath);
            }
            else
                Debug.LogError("Stealth path not found");

            yield return wait;
        }
    }
}
