using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Follow Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 10, -5);
    [SerializeField] private bool followX = true;
    [SerializeField] private bool followZ = true;

    void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 newPosition = transform.position;

        if (followX)
            newPosition.x = target.position.x + offset.x;

        if (followZ)
            newPosition.z = target.position.z + offset.z;

        newPosition.y = target.position.y + offset.y;

        transform.position = newPosition;
    }
}