using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerModel model;
    private void Awake()
    {
        model = GetComponent<PlayerModel>();
    }
    // Update is called once per frame
    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 direction = new Vector3(horizontal, 0, vertical);
        model.Walk(direction);
        if (horizontal != 0 || vertical != 0)
        {
            model.Rotate(direction);
        }
    }
}
