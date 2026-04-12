using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private LineOfSight los;
    [SerializeField] private FSM fsm;
    [SerializeField] private Transform[] patrolPoints;

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
    }

    void Update()
    {
        bool canSeePlayer =
            los.isInRange(transform, player)
            && los.isInAngle(transform, player)
            && los.hasLineOfSight(transform, player)
            && !player.GetComponent<PlayerModel>().IsInShadow;

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
        }
    }

    void Patrol()
    {
        Transform target = patrolPoints[currentPoint];

        Vector3 dir = target.position - transform.position;
        dir.y = 0;

        if(dir.magnitude < 0.5f)
        {
            currentPoint = (currentPoint + 1) % patrolPoints.Length;
            return;
        }

        Vector3 moveDir = dir.normalized;

        transform.position += moveDir * speed * Time.deltaTime;
        transform.forward = Vector3.Lerp(transform.forward, moveDir, Time.deltaTime * rotationSpeed);

    }

    void PursuePlayer()
    {
        Vector3 dir = player.position - transform.position;
        dir.y = 0;

        Vector3 moveDir = dir.normalized;

        transform.position += moveDir * speed * Time.deltaTime;

        transform.forward = Vector3.Lerp(
            transform.forward,
            moveDir,
            Time.deltaTime * rotationSpeed
        );
    }
}