using UnityEngine;

public class PlayerModel : MonoBehaviour
{
    private Rigidbody rb;
    [SerializeField] private int speed = 5;
    [SerializeField] private int rotationSpeed = 5;
    [SerializeField] private LayerMask shadowLayer;
    private bool isInShadow;
    private int shadowCounter = 0;
    private bool isDead = false;

    public bool IsInShadow => shadowCounter > 0;
    public bool IsDead => isDead;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Walk(Vector3 dir)
    {
        if (isDead) return;
        rb.linearVelocity = dir * speed;
    }

    public void Rotate(Vector3 dir) 
    {
        if (isDead) return;
        transform.forward = Vector3.Lerp(transform.forward, dir, rotationSpeed * Time.deltaTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;

        EnemyController enemy = collision.gameObject.GetComponent<EnemyController>();
        if (enemy != null && !enemy.IsDead)
        {
            if (CombatHelper.IsAttackFromBehind(transform, enemy.transform))
            {
                Debug.Log("Player killed enemy from behind!");
                enemy.Die();
            }
            else
            {
                Debug.Log("Enemy killed player!");
                Die();
            }
        }
    }

    public void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log("Player died!");
        
        rb.linearVelocity = Vector3.zero;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDeath();
        }
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((shadowLayer & (1 << other.gameObject.layer)) != 0)
        {
            shadowCounter++;
            Debug.Log("Entered Shadow | Count: " + shadowCounter);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if ((shadowLayer & (1 << other.gameObject.layer)) != 0)
        {
            shadowCounter--;

            if (shadowCounter < 0)
                shadowCounter = 0;

            Debug.Log("Exited Shadow | Count: " + shadowCounter);
        }
    }

    void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = isDead ? Color.red : Color.green;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);
        }
    }
}


