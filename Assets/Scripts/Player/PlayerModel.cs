using UnityEngine;

public class PlayerModel : MonoBehaviour
{
    private Rigidbody rb;
    [SerializeField] private int speed = 5;
    [SerializeField] private int rotationSpeed = 5;
    [SerializeField] private LayerMask shadowLayer;
    private bool isInShadow;
    private int shadowCounter = 0;
    public bool IsInShadow => shadowCounter > 0;


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Walk(Vector3 dir)
    {
        rb.linearVelocity = dir * speed;
    }
    public void Rotate(Vector3 dir) 
    { 
        transform.forward = Vector3.Lerp(transform.forward, dir, rotationSpeed * Time.deltaTime);
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
}


