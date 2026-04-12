using UnityEngine;

public class PlayerView : MonoBehaviour
{
    [SerializeField] private Animator anim;
    [SerializeField] private Rigidbody rb;
    private void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        anim.SetFloat("Speed", rb.linearVelocity.magnitude);
    }
}
