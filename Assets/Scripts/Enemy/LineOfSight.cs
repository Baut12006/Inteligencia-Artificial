using UnityEngine;

public class LineOfSight : MonoBehaviour
{
    [SerializeField] private int distance;
    [SerializeField] private int angle;
    [SerializeField] private LayerMask layerMask;

    private float distanceSqr;
    private float halfAngle;
    private float angleOverride = -1f;

    void Awake()
    {
        distanceSqr = distance * distance;
        halfAngle = angle * 0.5f;
    }

    public void SetAngleOverride(float newAngle)
    {
        angleOverride = newAngle;
    }

    public bool isInRange(Transform self, Transform target)
    {
        return (self.position - target.position).sqrMagnitude <= distanceSqr;
    }

    public bool isInAngle(Transform self, Transform target)
    {
        float checkAngle = angleOverride >= 0 ? angleOverride * 0.5f : halfAngle;
        Vector3 dir = (target.position - self.position).normalized;
        return Vector3.Angle(self.forward, dir) <= checkAngle;
    }

    public bool hasLineOfSight(Transform self, Transform target)
    {
        Vector3 dir = target.position - self.position;
        return !Physics.Raycast(self.position, dir, dir.magnitude, layerMask);
    }

    public float GetDistance()
    {
        return distance;
    }

    public float GetAngle()
    {
        return angleOverride >= 0 ? angleOverride : angle;
    }

    void OnValidate()
    {
        distanceSqr = distance * distance;
        halfAngle = angle * 0.5f;
    }
}
