using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private EnemyData config;
    private Vector3 lastStuckCheckPos;
    private float stuckTimer;

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private LineOfSight los;
    [SerializeField] private FSM fsm;
    [SerializeField] private PatrolRoute patrolRoute;
    [SerializeField] private Light visionLight;
    [SerializeField] private float alertRadius = 10f;

    private PlayerModel playerModel;
    private Rigidbody playerRb;
    private Vector3 lastKnownPosition;
    private Rigidbody rb;
    private Camera mainCamera;

    private int currentPoint = 0;
    private float closeDetectionRangeSqr;
    private float searchTimer = 0f;
    private bool isDead = false;

    // --- Estado de steering / pathfinding (Entrega 2) ---
    private Vector3 steeringVelocity;   // velocidad propia acumulada para el steering
    private float wanderAngle;          // ángulo persistente del Wander (Scout)
    private List<Vector3> currentPath;  // ruta A* actual (waypoints en world space)
    private int pathIndex;
    private float pathTimer;
    private FSM.EnemyState lastState = FSM.EnemyState.Patrol;
    private Transform cachedSentry;
    private List<PathNode> sentryBlockedNodes;

    [Header("Light Culling")]
    private Color originalLightColor;
    [SerializeField] private float lightCullingDistance = 30f;

    private static readonly Collider[] separationBuffer = new Collider[8];
    public bool IsDead => isDead;
    private bool IsSentry => config != null && config.isSentry;

    void Awake()
    {
        lastStuckCheckPos = transform.position;
        if (los == null) los = GetComponent<LineOfSight>();
        if (fsm == null) fsm = GetComponent<FSM>();

        if (player != null)
        {
            playerModel = player.GetComponent<PlayerModel>();
            playerRb = player.GetComponent<Rigidbody>();
        }

        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main;

        if (config != null)
            closeDetectionRangeSqr = config.closeDetectionRange * config.closeDetectionRange;

        SetupVisionLight();

        if (config != null && config.isSentry)
            SetupSentryVision();

        FindClosestWaypoint();
    }
    void Start()
    {
        if (config != null && config.isSentry && GridManager.Instance != null)
            sentryBlockedNodes = GridManager.Instance.BlockCircle(transform.position, config.sentryNavRadius);
    }

    void FindClosestWaypoint()
    {
        if (config == null || config.isSentry || patrolRoute == null || patrolRoute.WaypointCount == 0)
            return;

        float closestDistanceSqr = float.MaxValue;
        int closestIndex = 0;

        for (int i = 0; i < patrolRoute.WaypointCount; i++)
        {
            Vector3 waypoint = patrolRoute.GetWaypoint(i);
            float distanceSqr = (transform.position - waypoint).sqrMagnitude;
            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closestIndex = i;
            }
        }
        currentPoint = closestIndex;
    }

    void SetupVisionLight()
    {
        if (config != null) originalLightColor = config.visionLightColor;
        if (config == null || !config.showVisionLight) return;

        if (visionLight == null)
        {
            GameObject lightObj = new GameObject("VisionLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = Vector3.zero;
            lightObj.transform.localRotation = Quaternion.Euler(90, 0, 0);
            visionLight = lightObj.AddComponent<Light>();
        }

        visionLight.type = LightType.Spot;
        visionLight.range = los.GetDistance();
        visionLight.spotAngle = config.isSentry ? config.sentryViewAngle : los.GetAngle();
        visionLight.intensity = config.lightIntensity;
        visionLight.color = config.visionLightColor;
        visionLight.enabled = true;
        visionLight.shadows = LightShadows.None;
        visionLight.renderMode = LightRenderMode.ForcePixel;
        visionLight.cullingMask = LayerMask.GetMask("Default");
        visionLight.innerSpotAngle = visionLight.spotAngle * 0.8f;
    }

    void SetupSentryVision()
    {
        if (los != null) los.SetAngleOverride(config.sentryViewAngle);
    }

    void Update()
    {
        if (isDead || config == null || player == null || playerModel == null) return;
        if (playerModel.IsDead) return;

        UpdateLightVisibility();

        float sqrDistanceToPlayer = (transform.position - player.position).sqrMagnitude;
        bool normalVision = CheckNormalVision();
        bool isCloseEnough = sqrDistanceToPlayer <= closeDetectionRangeSqr;
        bool canSeePlayer = CanDetectPlayer(normalVision, isCloseEnough);

        if (canSeePlayer)
        {
            lastKnownPosition = player.position;
            searchTimer = config.searchDuration;

            // Sentry y Scout dan la alarma a los enemigos cercanos.
            if (config.isSentry || config.movementProfile == EnemyData.MovementProfile.Scout)
                AlertNearbyEnemies();
        }

        fsm.UpdateState(canSeePlayer, config.isSentry);
        ExecuteState();
    }

    void OnTriggerEnter(Collider other)
    {
        if (isDead) return;
        PlayerModel p = other.GetComponent<PlayerModel>();
        if (p != null && !p.IsDead)
        {
            if (CombatHelper.IsAttackFromBehind(transform, p.transform)) p.Die();
            else Die();
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        if (rb != null) rb.linearVelocity = Vector3.zero;
        steeringVelocity = Vector3.zero;
        if (visionLight != null) visionLight.enabled = false;
        if (fsm != null) enabled = false;
        if (GameManager.Instance != null) GameManager.Instance.OnEnemyKilled();

        if (sentryBlockedNodes != null && GridManager.Instance != null)
            GridManager.Instance.UnblockNodes(sentryBlockedNodes);
        Destroy(gameObject);
    }

    bool CheckNormalVision()
    {
        bool inRange = config.requiresRangeCheck ? los.isInRange(transform, player) : true;
        bool inAngle = config.requiresAngleCheck ? los.isInAngle(transform, player) : true;
        bool hasLOS = config.requiresLineOfSight ? los.hasLineOfSight(transform, player) : true;
        return inRange && inAngle && hasLOS;
    }

    void UpdateLightVisibility()
    {
        if (!config.showVisionLight || visionLight == null || mainCamera == null) return;
        float sqrDistanceToCamera = (transform.position - mainCamera.transform.position).sqrMagnitude;
        float cullingDistanceSqr = lightCullingDistance * lightCullingDistance;
        visionLight.enabled = sqrDistanceToCamera <= cullingDistanceSqr;
    }

    bool CanDetectPlayer(bool normalVision, bool isCloseEnough)
    {
        if (config == null) return false;
        bool shadowCheck = config.canSeeInShadows || !playerModel.IsInShadow;
        return (normalVision && shadowCheck) || isCloseEnough;
    }

    void UpdateLightColor()
    {
        if (visionLight == null || config == null) return;
        if (config.isSentry && fsm.currentState == FSM.EnemyState.Alert)
            visionLight.color = Color.red;
        else
            visionLight.color = originalLightColor;
    }

    // ----------------------------------------------------------------------
    //  EJECUCIÓN DE ESTADOS  ->  acá se integra FSM + Pathfinding + Steering
    // ----------------------------------------------------------------------
    void ExecuteState()
    {
        UpdateLightColor();

        // Al cambiar de estado, invalido la ruta para que recalcule contra el nuevo objetivo.
        if (fsm.currentState != lastState)
        {
            currentPath = null;
            pathIndex = 0;
            pathTimer = 0f;
            lastState = fsm.currentState;
        }

        switch (fsm.currentState)
        {
            case FSM.EnemyState.Patrol: Patrol(); break;
            case FSM.EnemyState.Pursuit: PursuePlayer(); break;
            case FSM.EnemyState.Alert: Alert(); break;
            case FSM.EnemyState.Search: Search(); break;
        }
    }

    // ---------------- PATROL ----------------
    void Patrol()
    {
        if (IsSentry) { SentryPatrol(); return; }

        switch (config.movementProfile)
        {
            case EnemyData.MovementProfile.Scout:
                Roam(); // deambula con Wander
                break;
            default: // Waypoints y Hunter patrullan la ruta con A* + Arrive
                PatrolWaypoints();
                break;
        }
    }

    void SentryPatrol()
    {
        transform.Rotate(0, config.sentryRotationSpeed * Time.deltaTime, 0);
    }

    void PatrolWaypoints()
    {
        if (TryUnstick(config.patrolSpeed)) return;
        if (!config.canPatrol || patrolRoute == null || patrolRoute.WaypointCount == 0) return;

        Vector3 target = patrolRoute.GetWaypoint(currentPoint);
        bool arrived = FollowPath(target, config.patrolSpeed, useArrive: true);
        if (arrived)
            currentPoint = (currentPoint + 1) % patrolRoute.WaypointCount;
    }

    // ---------------- PURSUIT ----------------
    void PursuePlayer()
    {
        if (!config.canPursue) return;

        if (config.movementProfile == EnemyData.MovementProfile.Scout)
        {
            ScoutEscape();
            return;
        }

        Vector3 pVel = playerRb != null ? Planar(playerRb.linearVelocity) : Vector3.zero;

        // Hunter -> Pursue con predicción. Resto -> Seek directo.
        Vector3 force = (config.movementProfile == EnemyData.MovementProfile.Hunter)
            ? Steering.Pursue(transform.position, steeringVelocity, player.position, pVel, config.pursuitSpeed, config.predictionTime)
            : Steering.Seek(transform.position, steeringVelocity, player.position, config.pursuitSpeed);

        // El avoidance SOLO actúa de lejos (para navegar). Al entrar en rango de ataque
        // lo soltamos, así puede arrinconar al player aunque esté pegado a una pared.
        float distToPlayer = Vector3.Distance(Planar(transform.position), Planar(player.position));
        float attackRange = config.closeDetectionRange + 1f;
        if (distToPlayer > attackRange)
            force += Steering.AvoidBlocked(transform.position, steeringVelocity, GridManager.Instance, config.arriveRadius, config.pursuitSpeed) * 2f;

        ApplySteering(force, config.pursuitSpeed);
    }

    // El Scout no ataca: huye del player (Evade) y corre hacia el centinela por A* para dar la alarma.
    void ScoutEscape()
    {
        Vector3 pVel = playerRb != null ? Planar(playerRb.linearVelocity) : Vector3.zero;
        GridManager grid = GridManager.Instance;

        // ¿La huida directa está despejada? Miramos unos metros en la dirección de escape.
        Vector3 away = Planar(transform.position - player.position).normalized;
        bool directEscapeClear = grid == null ||
            (grid.IsWalkable(transform.position + away * 2f) && grid.IsWalkable(transform.position + away * 4f));

        if (directEscapeClear)
        {
            // Camino libre detrás: Evade LOCAL, rápido y reactivo (steering puro).
            currentPath = null;
            Vector3 evade = Steering.Evade(transform.position, steeringVelocity, player.position, pVel, config.pursuitSpeed, config.predictionTime);
            evade += Steering.AvoidBlocked(transform.position, steeringVelocity, grid, config.arriveRadius, config.pursuitSpeed) * 2f;
            ApplySteering(evade, config.pursuitSpeed);
        }
        else
        {
            // Acorralado contra una pared: el Evade local no sirve -> huida GLOBAL con A*
            // hacia un punto navegable lejos del player (A* rodea el muro, no se choca).
            Vector3 fleeTarget = GetFleeTarget();
            FollowPath(fleeTarget, config.pursuitSpeed, useArrive: false);
        }
    }

    // Elige un punto NAVEGABLE lejos del player para huir por A* sin chocar paredes.
    // Puntea 12 direcciones alrededor y se queda con la mejor celda caminable
    // (lejos del player, alineada con el escape y, si hay, hacia un centinela).
    Vector3 GetFleeTarget()
    {
        GridManager grid = GridManager.Instance;
        Vector3 awayFromPlayer = Planar(transform.position - player.position);
        if (awayFromPlayer.sqrMagnitude < 0.01f) awayFromPlayer = transform.forward;
        awayFromPlayer.Normalize();

        if (grid == null) return transform.position + awayFromPlayer * 7f;

        Transform sentry = FindNearestSentry();
        float fleeDist = 7f;
        Vector3 best = Vector3.zero;
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < 12; i++)
        {
            float ang = i * 30f * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
            Vector3 candidate = transform.position + dir * fleeDist;
            if (!grid.IsWalkable(candidate)) continue;

            float distFromPlayer = Vector3.Distance(Planar(candidate), Planar(player.position));
            float alignment = Vector3.Dot(dir, awayFromPlayer); // +1 = se aleja del player
            float score = distFromPlayer + alignment * 3f;

            if (sentry != null)
            {
                Vector3 toSentry = Planar(sentry.position - transform.position).normalized;
                score += Vector3.Dot(dir, toSentry) * 2f; // bonus: busca refugio cerca del centinela
            }

            if (score > bestScore) { bestScore = score; best = candidate; }
        }

        if (bestScore == float.NegativeInfinity)
            return transform.position + awayFromPlayer * 3f; // encajonado: empuja igual

        return best;
    }

    // ---------------- ALERT (Sentry) ----------------
    void Alert()
    {
        Vector3 dirToPlayer = player.position - transform.position;
        dirToPlayer.y = 0;
        if (dirToPlayer.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(dirToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * config.rotationSpeed);
        }
    }

    // ---------------- SEARCH ----------------
    void Search()
    {
        searchTimer -= Time.deltaTime;
        if (searchTimer <= 0f)
        {
            fsm.currentState = FSM.EnemyState.Patrol;
            currentPath = null;
            return;
        }

        if (IsSentry) { SentrySearch(); return; }

        if (config.movementProfile == EnemyData.MovementProfile.Scout)
        {
            Roam(); // el Scout vuelve a deambular mientras se "calma"
            return;
        }

        // Va a la última posición conocida por A* + Arrive. Si llega y no ve nada, espera ahí.
        if (TryUnstick(config.patrolSpeed)) return;
        FollowPath(lastKnownPosition, config.patrolSpeed, useArrive: true);
    }

    void SentrySearch()
    {
        Vector3 dirToLastKnown = lastKnownPosition - transform.position;
        dirToLastKnown.y = 0;
        if (dirToLastKnown.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(dirToLastKnown);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * config.rotationSpeed * 0.5f);
        }
    }

    // ---------------- ROAM (Wander) ----------------
    void Roam()
    {
        if (TryUnstick(config.patrolSpeed)) return;
        Vector3 wander = Steering.Wander(steeringVelocity, ref wanderAngle,
            config.wanderCircleDistance, config.wanderCircleRadius, config.wanderJitter, config.patrolSpeed);
        Vector3 avoid = Steering.AvoidBlocked(transform.position, steeringVelocity, GridManager.Instance, config.arriveRadius, config.patrolSpeed) * 2f;
        Vector3 bounds = StayInBounds(config.patrolSpeed) * 2f;
        ApplySteering(wander + avoid + bounds, config.patrolSpeed);
    }

    // ----------------------------------------------------------------------
    //  NÚCLEO DE MOVIMIENTO
    // ----------------------------------------------------------------------

    // Sigue una ruta A* hacia finalTarget. Si no hay grilla/camino, hace steering directo (fallback).
    // Devuelve true cuando llegó al destino final.
    bool FollowPath(Vector3 finalTarget, float speed, bool useArrive)
    {
        pathTimer -= Time.deltaTime;
        if (currentPath == null || pathTimer <= 0f)
        {
            RequestPath(finalTarget);
            pathTimer = config.pathRecalcTime;
        }

        // Fallback: sin grilla o sin ruta -> Seek/Arrive directo al objetivo.
        if (currentPath == null || currentPath.Count == 0)
        {
            Vector3 f = useArrive
                ? Steering.Arrive(transform.position, steeringVelocity, finalTarget, speed, config.arriveRadius)
                : Steering.Seek(transform.position, steeringVelocity, finalTarget, speed);
            ApplySteering(f, speed);
            return ReachedXZ(finalTarget, config.waypointTolerance);
        }

        // Avanza por los waypoints de la ruta.
        Vector3 node = currentPath[pathIndex];
        if (ReachedXZ(node, config.waypointTolerance))
        {
            pathIndex++;
            if (pathIndex >= currentPath.Count)
            {
                currentPath = null;
                pathIndex = 0;
                return true;
            }
            node = currentPath[pathIndex];
        }

        bool lastNode = pathIndex == currentPath.Count - 1;
        Vector3 force = (useArrive && lastNode)
            ? Steering.Arrive(transform.position, steeringVelocity, node, speed, config.arriveRadius)
            : Steering.Seek(transform.position, steeringVelocity, node, speed);

        ApplySteering(force, speed);
        return false;
    }

    void RequestPath(Vector3 target)
    {
        if (!config.usePathfinding || Pathfinder.Instance == null)
        {
            currentPath = null;
            return;
        }
        List<Vector3> p = Pathfinder.Instance.FindPath(transform.position, target);
        if (p != null && p.Count > 0) { currentPath = p; pathIndex = 0; }
        else currentPath = null;
    }

    // Acumula la fuerza de steering sobre la velocidad y la aplica al Rigidbody (modelo Reynolds).
    void ApplySteering(Vector3 steeringForce, float maxSpeed)
    {
        steeringForce.y = 0f;
        steeringForce += Separation(maxSpeed) * config.separationWeight;  
        steeringForce = Vector3.ClampMagnitude(steeringForce, config.maxForce);

        steeringVelocity += steeringForce * Time.deltaTime;
        steeringVelocity.y = 0f;
        steeringVelocity = Vector3.ClampMagnitude(steeringVelocity, maxSpeed);

        rb.linearVelocity = new Vector3(steeringVelocity.x, rb.linearVelocity.y, steeringVelocity.z);

        if (steeringVelocity.sqrMagnitude > 0.01f)
        {
            Vector3 newForward = Vector3.Slerp(transform.forward, steeringVelocity.normalized, Time.deltaTime * config.rotationSpeed);
            newForward.y = 0f;
            if (newForward.sqrMagnitude > 0.001f)
                rb.MoveRotation(Quaternion.LookRotation(newForward));
        }
        // Anti-atasco: SÓLO fuera de Pursuit (en persecución queremos que apriete la pared).
        if (fsm.currentState != FSM.EnemyState.Pursuit)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= 0.3f)
            {
                float moved = (transform.position - lastStuckCheckPos).sqrMagnitude;
                if (maxSpeed > 0.1f && moved < 0.02f) // debería avanzar pero no avanza
                {
                    Vector3 escape = WallRepulsion();
                    if (escape.sqrMagnitude > 0.01f)  // hay pared al lado -> está trabado (no es idle)
                    {
                        steeringVelocity = escape.normalized * maxSpeed; // empujón al espacio libre
                        rb.linearVelocity = new Vector3(steeringVelocity.x, rb.linearVelocity.y, steeringVelocity.z);
                        currentPath = null;
                        pathTimer = 0f;          // recalcula ruta cuando se libere
                        wanderAngle += Mathf.PI;
                    }
                }
                lastStuckCheckPos = transform.position;
                stuckTimer = 0f;
            }
        }
    }
    bool TryUnstick(float speed)
    {
        GridManager grid = GridManager.Instance;
        if (grid == null || grid.IsWalkable(transform.position)) return false;

        currentPath = null;
        Vector3 safe = grid.ClosestWalkablePoint(transform.position);
        Vector3 force = Steering.Seek(transform.position, steeringVelocity, safe, speed);
        ApplySteering(force, speed);
        return true;
    }
    bool ReachedXZ(Vector3 target, float tolerance)
    {
        Vector3 d = target - transform.position; d.y = 0f;
        return d.sqrMagnitude <= tolerance * tolerance;
    }

    Vector3 Planar(Vector3 v) { v.y = 0f; return v; }

    // Mantiene al Scout dentro del rectángulo de la grilla (lo empuja al centro si se sale).
    Vector3 StayInBounds(float speed)
    {
        GridManager grid = GridManager.Instance;
        if (grid == null) return Vector3.zero;

        Bounds b = grid.WorldBounds;
        Vector3 flat = new Vector3(transform.position.x, b.center.y, transform.position.z);
        if (b.Contains(flat)) return Vector3.zero;

        Vector3 toCenter = b.center - transform.position; toCenter.y = 0f;
        Vector3 desired = toCenter.normalized * speed;
        return desired - steeringVelocity;
    }

    Transform FindNearestSentry()
    {
        if (cachedSentry != null)
        {
            EnemyController ec = cachedSentry.GetComponent<EnemyController>();
            if (ec != null && !ec.IsDead) return cachedSentry;
        }

        EnemyController[] all = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        Transform best = null;
        float bestSqr = float.MaxValue;
        foreach (EnemyController e in all)
        {
            if (e == this || e.IsDead || e.config == null || !e.config.isSentry) continue;
            float d = (e.transform.position - transform.position).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = e.transform; }
        }
        cachedSentry = best;
        return best;
    }

    public void ReceiveAlert(Vector3 alertPosition)
    {
        if (isDead) return;
        lastKnownPosition = alertPosition;
        searchTimer = config.searchDuration;
        if (fsm.currentState != FSM.EnemyState.Pursuit)
            fsm.currentState = FSM.EnemyState.Search;
    }

    void AlertNearbyEnemies()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, alertRadius);
        foreach (Collider hit in hits)
        {
            EnemyController enemy = hit.GetComponent<EnemyController>();
            if (enemy != null && enemy != this && !enemy.IsDead)
                enemy.ReceiveAlert(lastKnownPosition);
        }
    }
    // SEPARATION: fuerza que empuja al agente lejos de los enemigos cercanos
    // (incluido el centinela), para que no se amontonen ni lo arrinconen contra una pared.
    Vector3 Separation(float maxSpeed)
    {
        if (config.separationRadius <= 0f) return Vector3.zero;

        int count = Physics.OverlapSphereNonAlloc(transform.position, config.separationRadius, separationBuffer);
        Vector3 push = Vector3.zero;
        int neighbours = 0;

        for (int i = 0; i < count; i++)
        {
            EnemyController other = separationBuffer[i].GetComponent<EnemyController>();
            if (other == null || other == this || other.IsDead) continue;

            Vector3 diff = transform.position - other.transform.position;
            diff.y = 0f;
            float d = diff.magnitude;
            if (d < 0.001f) { diff = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, UnityEngine.Random.Range(-1f, 1f)); d = 0.1f; }

            push += diff.normalized / d; // cuanto más cerca, más fuerte empuja
            neighbours++;
        }

        if (neighbours == 0) return Vector3.zero;
        push /= neighbours; push.y = 0f;
        if (push.sqrMagnitude < 0.0001f) return Vector3.zero;

        Vector3 desired = push.normalized * maxSpeed;
        return desired - steeringVelocity;
    }
    void OnValidate()
    {
        if (Application.isPlaying && visionLight != null && config != null)
            SetupVisionLight();
    }
    // Mira en 8 direcciones alrededor y devuelve hacia dónde está el espacio libre
    // (suma lo contrario a cada celda bloqueada). Sirve para despegarse de una esquina.
    Vector3 WallRepulsion()
    {
        GridManager grid = GridManager.Instance;
        if (grid == null) return Vector3.zero;

        float sample = 0.8f; // a qué distancia "siente" la pared (ajustable)
        Vector3 sum = Vector3.zero;
        for (int i = 0; i < 8; i++)
        {
            float ang = i * 45f * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
            if (!grid.IsWalkable(transform.position + dir * sample))
                sum -= dir;
        }
        sum.y = 0f;
        return sum;
    }
    void OnDrawGizmos()
    {
        if (config == null) return;

        Gizmos.color = isDead ? Color.gray : Color.red;
        Gizmos.DrawWireSphere(transform.position, config.closeDetectionRange);

        if (!isDead && Application.isPlaying)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);
        }

        if (patrolRoute != null && patrolRoute.WaypointCount > 0 && !config.isSentry)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < patrolRoute.WaypointCount; i++)
            {
                Vector3 waypoint = patrolRoute.GetWaypoint(i);
                Gizmos.DrawWireSphere(waypoint, 0.5f);
                Vector3 nextWaypoint = patrolRoute.GetWaypoint(i + 1);
                Gizmos.DrawLine(waypoint, nextWaypoint);
            }
        }

        // Ruta A* actual (en verde) para debug visual.
        if (Application.isPlaying && currentPath != null && currentPath.Count > 0)
        {
            Gizmos.color = Color.green;
            Vector3 prev = transform.position;
            for (int i = 0; i < currentPath.Count; i++)
            {
                Gizmos.DrawLine(prev, currentPath[i]);
                Gizmos.DrawWireSphere(currentPath[i], 0.25f);
                prev = currentPath[i];
            }
        }

        if (config.isSentry)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }
}
