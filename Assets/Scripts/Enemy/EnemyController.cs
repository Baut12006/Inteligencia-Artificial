using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private EnemyData config;

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private LineOfSight los;
    [SerializeField] private FSM fsm;
    [SerializeField] private PatrolRoute patrolRoute;
    [SerializeField] private Light visionLight;
    [SerializeField] private float alertRadius = 10f;

    private PlayerModel playerModel;
    private Vector3 lastKnownPosition;
    private Rigidbody rb;
    private Camera mainCamera;

    private int currentPoint = 0;
    private float waypointReachedDistanceSqr = 0.25f;
    private float closeDetectionRangeSqr;
    private float searchTimer = 0f;
    private bool isDead = false;

    [Header("Light Culling")]
    private Color originalLightColor;
    [SerializeField] private float lightCullingDistance = 30f;

    public bool IsDead => isDead;

    void Awake()
    {
        if (los == null)
            los = GetComponent<LineOfSight>();

        if (fsm == null)
            fsm = GetComponent<FSM>();

        if (player != null)
            playerModel = player.GetComponent<PlayerModel>();

        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main;

        if (config != null)
        {
            closeDetectionRangeSqr = config.closeDetectionRange * config.closeDetectionRange;
        }

        SetupVisionLight();
        
        if (config != null && config.isSentry)
        {
            SetupSentryVision();
        }
    }

    void SetupVisionLight()
    {
        if (config != null)
            originalLightColor = config.visionLightColor;

        if (config == null || !config.showVisionLight)
            return;

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
        if (los != null)
        {
            los.SetAngleOverride(config.sentryViewAngle);
        }
    }

    void Update()
    {
        if (isDead || config == null || player == null || playerModel == null)
            return;

        if (playerModel.IsDead)
            return;

        UpdateLightVisibility();

        float sqrDistanceToPlayer = (transform.position - player.position).sqrMagnitude;
        
        bool normalVision = CheckNormalVision();
        bool isCloseEnough = sqrDistanceToPlayer <= closeDetectionRangeSqr;

        bool canSeePlayer = CanDetectPlayer(normalVision, isCloseEnough);

        if (canSeePlayer)
        {
            lastKnownPosition = player.position;
            searchTimer = config.searchDuration;

            if (config.isSentry)
            {
                AlertNearbyEnemies();
            }
        }

        fsm.UpdateState(canSeePlayer, config.isSentry);

        ExecuteState();
    }                       

    void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;

        PlayerModel player = collision.gameObject.GetComponent<PlayerModel>();
        if (player != null && !player.IsDead)
        {
            if (CombatHelper.IsAttackFromBehind(transform, player.transform))
            {
                Debug.Log($"{config.enemyTypeName} killed player from behind!");
                player.Die();
            }
            else
            {
                Debug.Log($"Player killed {config.enemyTypeName}!");
                Die();
            }
        }
    }

    public void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log($"{config.enemyTypeName} died!");

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }
        if (visionLight != null)
        {
            visionLight.enabled = false;
        }
        if (fsm != null)
        {
            enabled = false;
        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnEnemyKilled();
        }
    
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
        if (!config.showVisionLight || visionLight == null || mainCamera == null)
            return;

        float sqrDistanceToCamera = (transform.position - mainCamera.transform.position).sqrMagnitude;
        float cullingDistanceSqr = lightCullingDistance * lightCullingDistance;

        visionLight.enabled = sqrDistanceToCamera <= cullingDistanceSqr;
    }

    bool CanDetectPlayer(bool normalVision, bool isCloseEnough)
    {
        if (config == null)
            return false;

        bool shadowCheck = config.canSeeInShadows || !playerModel.IsInShadow;
        return (normalVision && shadowCheck) || isCloseEnough;
    }

    void UpdateLightColor()
    {
        if (visionLight == null || config == null) return;

        if (config.isSentry && fsm.currentState == FSM.EnemyState.Alert)
        {
            visionLight.color = Color.red;
        }
        else
        {
            visionLight.color = originalLightColor;
        }
    }

    void ExecuteState()
    {
        UpdateLightColor();
        switch (fsm.currentState)
        {
            case FSM.EnemyState.Patrol:
                Patrol();
                break;

            case FSM.EnemyState.Pursuit:
                PursuePlayer();
                break;

            case FSM.EnemyState.Alert:
                Alert();
                break;

            case FSM.EnemyState.Search:
                Search();
                break;
        }
    }

    void Patrol()
    {
        if (config.isSentry)
        {
            SentryPatrol();
            return;
        }

        if (!config.canPatrol || patrolRoute == null || patrolRoute.WaypointCount == 0)
            return;

        Vector3 targetPosition = patrolRoute.GetWaypoint(currentPoint);

        Vector3 dir = targetPosition - transform.position;
        dir.y = 0;

        if (dir.sqrMagnitude < waypointReachedDistanceSqr)
        {
            currentPoint = (currentPoint + 1) % patrolRoute.WaypointCount;
            return;
        }

        Move(dir, config.patrolSpeed);
    }

    void SentryPatrol()
    {
        float rotationAmount = config.sentryRotationSpeed * Time.deltaTime;
        transform.Rotate(0, rotationAmount, 0);
    }

    void PursuePlayer()
    {
        if (!config.canPursue)
            return;

        Vector3 dir = player.position - transform.position;
        dir.y = 0;

        Move(dir, config.pursuitSpeed);
    }

    void Alert()
    {
        Vector3 dirToPlayer = player.position - transform.position;
        dirToPlayer.y = 0;

        if (dirToPlayer.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(dirToPlayer);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * config.rotationSpeed
            );
        }
    }

    void Search()
    {
        searchTimer -= Time.deltaTime;

        if (searchTimer <= 0f)
        {
            fsm.currentState = FSM.EnemyState.Patrol;
            return;
        }

        if (config.isSentry)
        {
            SentrySearch();
            return;
        }

        Vector3 dir = lastKnownPosition - transform.position;
        dir.y = 0;

        if (dir.sqrMagnitude < waypointReachedDistanceSqr)
        {
            return;
        }

        Move(dir, config.patrolSpeed);
    }

    void SentrySearch()
    {
        Vector3 dirToLastKnown = lastKnownPosition - transform.position;
        dirToLastKnown.y = 0;

        if (dirToLastKnown.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(dirToLastKnown);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * config.rotationSpeed * 0.5f
            );
        }
    }

    public void ReceiveAlert(Vector3 alertPosition)
    {
        if (isDead) return;

        lastKnownPosition = alertPosition;
        searchTimer = config.searchDuration;

        if (fsm.currentState != FSM.EnemyState.Pursuit)
        {
            fsm.currentState = FSM.EnemyState.Search;
        }
    }

    void AlertNearbyEnemies()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, alertRadius);

        foreach (var hit in hits)
        {
            EnemyController enemy = hit.GetComponent<EnemyController>();

            if (enemy != null && enemy != this && !enemy.IsDead)
            {
                enemy.ReceiveAlert(lastKnownPosition);
            }
        }
    }

    void Move(Vector3 dir, float moveSpeed)
    {
        Vector3 moveDir = dir.normalized;

        rb.linearVelocity = new Vector3(
            moveDir.x * moveSpeed,
            rb.linearVelocity.y, 
            moveDir.z * moveSpeed
        );

        Vector3 newForward = Vector3.Lerp(
            transform.forward,
            moveDir,
            Time.deltaTime * config.rotationSpeed
        );

        rb.MoveRotation(Quaternion.LookRotation(newForward));
    }

    void OnValidate()
    {
        if (Application.isPlaying && visionLight != null && config != null)
        {
            SetupVisionLight();
        }
    }

    void OnDrawGizmos()
    {
        if (config == null)
            return;

        Color gizmoColor = isDead ? Color.gray : Color.red;
        Gizmos.color = gizmoColor;
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

        if (config.isSentry)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }
}