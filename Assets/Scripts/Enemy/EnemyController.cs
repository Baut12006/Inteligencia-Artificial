using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private LineOfSight los;
    [SerializeField] private FSM fsm;
    [SerializeField] private Transform[] patrolPoints;

    private PlayerModel playerModel;
    private Vector3 lastKnownPosition;
    private float closeDetectionRange = 3f;
    private Rigidbody rb;

    [Header("Movement")]
    [SerializeField] private float speed = 3f;
    [SerializeField] private float rotationSpeed = 5f;

    private int currentPoint = 0;

    void Awake()
    {
        if (los == null)
            los = GetComponent<LineOfSight>();

        if (fsm == null)
            fsm = GetComponent<FSM>();

        playerModel = player.GetComponent<PlayerModel>();
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        bool normalVision =
            los.isInRange(transform, player)
            && los.isInAngle(transform, player)
            && los.hasLineOfSight(transform, player);

        bool isCloseEnough = distanceToPlayer <= closeDetectionRange;

        bool canSeePlayer = (normalVision && !playerModel.IsInShadow) || isCloseEnough;

        if (canSeePlayer)
        {
            lastKnownPosition = player.position;
        }

        fsm.UpdateState(canSeePlayer);

        ExecuteState();
    }

    void ExecuteState()
    {
        switch (fsm.currentState)
        {
            case FSM.EnemyState.Patrol:
                Patrol();
                break;

            case FSM.EnemyState.Pursuit:
                PursuePlayer();
                break;

            case FSM.EnemyState.Search:
                Search();
                break;
        }
    }

    void Patrol()
    {
        Transform target = patrolPoints[currentPoint];

        Vector3 dir = target.position - transform.position;
        dir.y = 0;

        if (dir.magnitude < 0.5f)
        {
            currentPoint = (currentPoint + 1) % patrolPoints.Length;
            return;
        }

        Move(dir);
    }

    void PursuePlayer()
    {
        Vector3 dir = player.position - transform.position;
        dir.y = 0;

        Move(dir);
    }

    void Search()
    {
        Vector3 dir = lastKnownPosition - transform.position;
        dir.y = 0;

        if (dir.magnitude < 0.5f)
        {
            fsm.currentState = FSM.EnemyState.Patrol;
            return;
        }

        Move(dir);

    }

    void Move(Vector3 dir)
    {
        Vector3 moveDir = dir.normalized;

        Vector3 newPosition = rb.position + moveDir * speed * Time.deltaTime;
        rb.MovePosition(newPosition);

        Vector3 newForward = Vector3.Lerp(
            transform.forward,
            moveDir,
            Time.deltaTime * rotationSpeed
        );

        rb.MoveRotation(Quaternion.LookRotation(newForward));
    }
}