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
    string ReportName;

    [SerializeField]
    LayerMask WorldLayer;

    [SerializeField]
    EnemyState ForceState = EnemyState.None;
    public Transform Player;

    [SerializeField]
    Transform defendPoint;

    [SerializeField]
    float defendRadius = 15f;

    [SerializeField]
    float stealthDistance = 4f;

    [SerializeField]
    float stealthRecalculateInterval = 3f;

    [Header("Shooting System")]
    [SerializeField]
    private Transform gunTip;

    [SerializeField]
    private ObjectPool bulletPool;

    [SerializeField]
    private float shootRate = 0.5f;

    [SerializeField]
    private float bulletSpeed = 50;

    [SerializeField]
    private float shootingRange = 15f;

    [SerializeField]
    private LayerMask shootingLayers;

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

    private float originalAgentSpeed;
    private Coroutine currentAttackCoroutine;
    private Vector3 currentStealthTarget;
    private float lastStealthCalculation;

    [SerializeField]
    private Transform aimTarget;

    // Shooting variables
    private bool canShoot = true;
    private float lastShootTime;

    [SerializeField]
    ScoresLearner HidingLearner;

    [SerializeField]
    SimulationControl simulationControl;

    public ScoresDatabase HidingScores { get; private set; }

    private void Start()
    {
        Agent = GetComponent<NavMeshAgent>();
        originalAgentSpeed = Agent.speed;

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

        // (Deep Copy)
        var newScores = new Dictionary<HidePoint, float>();
        foreach (var pair in simulationControl.HeuristicDatabase.ToDictionary)
        {
            HidePoint originalPoint = pair.Key;
            float originalScore = pair.Value;

            HidePoint clonedPoint = originalPoint.Clone();

            clonedPoint.SetAgentReference(this);

            newScores.Add(clonedPoint, originalScore);
        }

        HidingScores = new ScoresDatabase(newScores);

        historyDistanceFromEnemy = Vector3.Distance(transform.position, Player.position);
    }

    private void FixedUpdate()
    {
        if (Time.timeScale != simulationControl.SimulationSpeed)
            Time.timeScale = simulationControl.SimulationSpeed;
        UpdateAI();
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
            if (HidingScores.Scores.Count <= 0)
            {
                yield return Wait;
                continue;
            }
            TargetPoint = HidingScores.CurrentBestPoint;
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

            if (defendPoint != null)
            {
                float distanceToDefendPoint = Vector3.Distance(
                    Player.position,
                    defendPoint.position
                );
                if (distanceToDefendPoint <= defendRadius)
                {
                    return 1; // attack - player is too close to defend point
                }
                else
                {
                    return 0; // defense - player is far from defend point, hide
                }
            }

            // Se não há ponto para defender, usa distância do inimigo como fallback
            if (distance <= MinPlayerDistance)
            {
                return 1; // attack - player is too close
            }
            else
            {
                return 0; // defense - player is far, hide
            }
        }

        ExecutionNode defenseNode = new ExecutionNode(input =>
        {
            Defense(input);
            AI_LastUpdate = Time.time;
            return NodeStatus.Success;
        });

        ControlNode chooseAttack = new ControlNode(input =>
        {
            if (BeingSeen)
                return 1; // brute attack - player spotted us

            Vector3 testStealthPos = CalculateStealthPosition(Player);
            if (testStealthPos == Vector3.zero)
                return 1; // brute attack - no valid stealth position
            else
                return 0; // stealth attack - try to stay hidden
        });

        ExecutionNode stealthAttackNode = new ExecutionNode(input =>
        {
            return StealthAttack(Player);
        });

        ExecutionNode bruteAttackNode = new ExecutionNode(input =>
        {
            return BruteAttack(Player);
        });

        // CORREÇÃO: A árvore estava mal estruturada!
        // Quando AttackOrDefense retorna 0 → vai para defenseNode (índice 0)
        // Quando AttackOrDefense retorna 1 → vai para chooseAttack (índice 1)
        root.AddChild(defenseNode); // índice 0 - DEFENSE
        root.AddChild(chooseAttack); // índice 1 - ATTACK
        chooseAttack.AddChild(stealthAttackNode).AddChild(bruteAttackNode);

        NodeStatus Defense(float value)
        {
            if (MovementCoroutine != null)
                StopCoroutine(MovementCoroutine);
            if (currentAttackCoroutine != null)
                StopCoroutine(currentAttackCoroutine);

            Agent.speed = originalAgentSpeed;
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
            if (!simulationControl.IsLearningEnabled)
            {
                yield return wait;
                continue;
            }

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

        TreeRootAI.Execute(distance);
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
        HidePoint bestPoint = HidingScores.CurrentBestPoint;
        hidePointScore = bestPoint.Score; // ( updating the point is not idial )
        return HidingScores.CurrentBestPoint;
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
                    return null;
                }
            }

            filteredPoints.Add(point);
            previousPoint = point;
        }

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

        return Vector3.zero;
    }

    private Vector3 CalculateStealthPosition(Transform player)
    {
        Vector3 playerPosition = player.position;
        Vector3 playerForward = player.forward;

        Vector3 behindPlayer = playerPosition - (playerForward * stealthDistance);

        if (NavMesh.SamplePosition(behindPlayer, out NavMeshHit hit, 5f, Agent.areaMask))
        {
            return hit.position;
        }

        for (int angle = 45; angle <= 180; angle += 45)
        {
            Vector3 leftFlank = CalculateFlankPosition(
                playerPosition,
                playerForward,
                angle,
                stealthDistance
            );
            Vector3 rightFlank = CalculateFlankPosition(
                playerPosition,
                playerForward,
                -angle,
                stealthDistance
            );

            if (NavMesh.SamplePosition(leftFlank, out NavMeshHit leftHit, 3f, Agent.areaMask))
            {
                return leftHit.position;
            }

            if (NavMesh.SamplePosition(rightFlank, out NavMeshHit rightHit, 3f, Agent.areaMask))
            {
                return rightHit.position;
            }
        }

        return Vector3.zero;
    }

    private bool IsValidStealthPosition(Vector3 position, Vector3 playerPosition)
    {
        if (!NavMesh.SamplePosition(position, out NavMeshHit hit, 5f, Agent.areaMask))
        {
            return false;
        }

        Vector3 validPosition = hit.position;

        if (IsPositionBehindWall(validPosition, playerPosition))
        {
            return false;
        }

        return true;
    }

    private bool IsPositionBehindWall(Vector3 position, Vector3 playerPosition)
    {
        Vector3 directionToPlayer = (playerPosition - position).normalized;
        float distanceToPlayer = Vector3.Distance(position, playerPosition);

        if (
            Physics.Raycast(
                position,
                directionToPlayer,
                out RaycastHit hit,
                distanceToPlayer,
                WorldLayer
            )
        )
        {
            if (!hit.collider.CompareTag("Player"))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasClearPathToPlayer(Vector3 position, Vector3 playerPosition)
    {
        NavMeshPath testPath = new NavMeshPath();
        if (NavMesh.CalculatePath(position, playerPosition, Agent.areaMask, testPath))
        {
            return testPath.status == NavMeshPathStatus.PathComplete;
        }
        return false;
    }

    private Vector3 FindFallbackStealthPosition(Vector3 playerPosition)
    {
        float[] distances =
        {
            stealthDistance * 0.5f,
            stealthDistance * 1.5f,
            stealthDistance * 2f,
        };

        foreach (float distance in distances)
        {
            for (int angle = 0; angle < 360; angle += 45)
            {
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward;
                Vector3 testPosition = playerPosition + (direction * distance);

                if (IsValidStealthPosition(testPosition, playerPosition))
                {
                    return testPosition;
                }
            }
        }

        return Vector3.zero;
    }

    private Vector3 CalculateFlankPosition(
        Vector3 playerPos,
        Vector3 playerForward,
        float angle,
        float distance
    )
    {
        Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
        Vector3 direction = rotation * (-playerForward);
        return playerPos + (direction * distance);
    }

    private bool CanShootAtPlayer(Transform player)
    {
        if (gunTip == null || bulletPool == null)
        {
            Debug.Log($"[{gameObject.name}] CanShoot: FALSE - gunTip or bulletPool is NULL");
            return false;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > shootingRange)
        {
            Debug.Log(
                $"[{gameObject.name}] CanShoot: FALSE - Too far ({distanceToPlayer:F1} > {shootingRange})"
            );
            return false;
        }

        Vector3 directionToPlayer = (player.position - gunTip.position).normalized;

        if (
            Physics.Raycast(
                gunTip.position,
                directionToPlayer,
                out RaycastHit hit,
                distanceToPlayer,
                shootingLayers
            )
        )
        {
            bool canSee = hit.collider.transform == player;
            Debug.Log($"[{gameObject.name}] CanShoot: {canSee} - Raycast hit: {hit.collider.name}");
            return canSee;
        }

        Debug.Log($"[{gameObject.name}] CanShoot: TRUE - Clear line of sight");
        return true;
    }

    private void ShootAtPlayer(Transform player)
    {
        Debug.Log(
            $"[{gameObject.name}] ShootAtPlayer called - canShoot: {canShoot}, timeSinceLastShot: {Time.time - lastShootTime:F2}"
        );

        if (!canShoot || Time.time - lastShootTime < shootRate)
        {
            Debug.Log($"[{gameObject.name}] Cannot shoot - rate limit");
            return;
        }

        if (gunTip == null)
        {
            Debug.LogError($"[{gameObject.name}] gunTip is NULL!");
            return;
        }

        if (bulletPool == null)
        {
            Debug.LogError($"[{gameObject.name}] bulletPool is NULL!");
            return;
        }

        GameObject bullet = bulletPool.GetObject();

        if (bullet == null)
        {
            Debug.LogWarning($"[{gameObject.name}] No bullet available from pool");
            return;
        }

        if (!bullet.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            Debug.LogError($"[{gameObject.name}] Bullet has no Rigidbody!");
            if (bullet.TryGetComponent<IPooledObject>(out var pooledObject))
                pooledObject.ReturnToPoll();
            return;
        }

        bullet.transform.position = gunTip.position;
        bullet.transform.parent = null;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Vector3 targetPosition = player.position + Vector3.up * 1.5f;
        Vector3 targetPosition = aimTarget.position;
        Vector3 direction = (targetPosition - gunTip.position).normalized;

        rb.linearVelocity = direction * bulletSpeed;

        lastShootTime = Time.time;
        canShoot = false;
        Invoke(nameof(ResetCanShoot), shootRate);

        Debug.Log(
            $"[{gameObject.name}] BULLET FIRED! Speed: {bulletSpeed}, Direction: {direction}"
        );
    }

    private void ResetCanShoot()
    {
        canShoot = true;
    }

    private Vector3 FindShootingPosition(Vector3 playerPosition)
    {
        float searchRadius = 8f;
        int attempts = 8;

        for (int i = 0; i < attempts; i++)
        {
            float angle = (360f / attempts) * i;
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            Vector3 testPosition = transform.position + direction * searchRadius;

            if (NavMesh.SamplePosition(testPosition, out NavMeshHit hit, 3f, Agent.areaMask))
            {
                Vector3 directionToPlayer = (playerPosition - hit.position).normalized;
                float distanceToPlayer = Vector3.Distance(hit.position, playerPosition);

                if (
                    distanceToPlayer <= shootingRange
                    && !Physics.Raycast(
                        hit.position + Vector3.up * 1.5f,
                        directionToPlayer,
                        distanceToPlayer,
                        shootingLayers
                    )
                )
                {
                    return hit.position;
                }
            }
        }

        return Vector3.zero;
    }

    private NodeStatus StealthAttack(Transform player)
    {
        if (currentAttackCoroutine != null)
            StopCoroutine(currentAttackCoroutine);

        currentStealthTarget = Vector3.zero;

        currentAttackCoroutine = StartCoroutine(StealthAttackBehaviour(player));
        if (currentAttackCoroutine == null)
            return NodeStatus.Failure;

        return NodeStatus.Success;
    }

    private NodeStatus BruteAttack(Transform player)
    {
        if (currentAttackCoroutine != null)
            StopCoroutine(currentAttackCoroutine);

        currentAttackCoroutine = StartCoroutine(BruteAttackBehaviour(player));
        if (currentAttackCoroutine == null)
            return NodeStatus.Failure;

        return NodeStatus.Success;
    }

    private IEnumerator StealthAttackBehaviour(Transform player)
    {
        WaitForSeconds wait = new WaitForSeconds(UpdateFrequency);
        Agent.speed = originalAgentSpeed * 0.8f;

        currentStealthTarget = CalculateStealthPosition(player);
        lastStealthCalculation = Time.time;

        if (currentStealthTarget == Vector3.zero)
        {
            yield break;
        }

        Agent.SetDestination(currentStealthTarget);

        bool hasReachedStealthPosition = false;

        while (true)
        {
            float distanceToTarget = Vector3.Distance(transform.position, currentStealthTarget);
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (!hasReachedStealthPosition && distanceToTarget <= 3f)
            {
                hasReachedStealthPosition = true;
                Agent.SetDestination(player.position);
                Agent.speed = originalAgentSpeed * 1.2f;
            }
            else if (!hasReachedStealthPosition)
            {
                if (!Agent.hasPath || Agent.remainingDistance < 0.5f)
                {
                    Agent.SetDestination(currentStealthTarget);
                }
            }
            else
            {
                // Fase final: atacar o jogador
                if (CanShootAtPlayer(player))
                {
                    // CONSEGUE VER O JOGADOR - PARA E ATIRA
                    Agent.isStopped = true;

                    Vector3 directionToPlayer = (player.position - transform.position).normalized;
                    Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer);
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        lookRotation,
                        Time.deltaTime * 8f
                    );

                    aimTarget.position = player.position + Vector3.up * 1.5f;

                    ShootAtPlayer(player);
                    Debug.Log($"[{gameObject.name}] STEALTH: Can see player - SHOOTING!");
                }
                else if (distanceToPlayer <= 2f)
                {
                    // Muito perto, termina stealth attack
                    Debug.Log($"[{gameObject.name}] STEALTH: Too close, ending stealth attack");
                    yield break;
                }
                else
                {
                    // NÃO CONSEGUE VER - CONTINUA A APROXIMAR-SE
                    Agent.isStopped = false;
                    if (!Agent.hasPath || Agent.remainingDistance < 0.5f)
                    {
                        Agent.SetDestination(player.position);
                    }
                    Debug.Log($"[{gameObject.name}] STEALTH: Cannot see player - APPROACHING");
                }
            }

            yield return wait;
        }
    }

    private IEnumerator BruteAttackBehaviour(Transform player)
    {
        WaitForSeconds wait = new WaitForSeconds(UpdateFrequency);
        Agent.speed = originalAgentSpeed * 1.2f;

        while (true)
        {
            if (MovementCoroutine != null)
                StopCoroutine(MovementCoroutine);

            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            bool canSeePlayer = CanShootAtPlayer(player);

            Debug.Log(
                $"[{gameObject.name}] BruteAttack - Distance: {distanceToPlayer:F1}, CanSee: {canSeePlayer}, Range: {shootingRange}"
            );

            if (canSeePlayer)
            {
                // CONSEGUE VER O JOGADOR - PARA E ATIRA
                Agent.isStopped = true;

                // Roda para o jogador
                Vector3 directionToPlayer = (player.position - transform.position).normalized;
                Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    lookRotation,
                    Time.deltaTime * 8f
                );

                aimTarget.position = player.position + Vector3.up * 1.5f;

                ShootAtPlayer(player);
                Debug.Log($"[{gameObject.name}] Can see player - SHOOTING!");
            }
            else
            {
                // NÃO CONSEGUE VER O JOGADOR - APROXIMA-SE
                Agent.isStopped = false;
                Agent.SetDestination(player.position);
                Debug.Log($"[{gameObject.name}] Cannot see player - APPROACHING");
            }

            yield return wait;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (defendPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(defendPoint.position, defendRadius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, defendPoint.position);
        }

        if (Player != null && Application.isPlaying)
        {
            Vector3 stealthPos = CalculateStealthPosition(Player);

            if (stealthPos != Vector3.zero)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(stealthPos, 1f);
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, stealthPos);
            }
            else
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(Player.position + Vector3.up * 3f, Vector3.one);
            }

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(Player.position - (Player.forward * stealthDistance), 0.5f);
        }
    }
}
