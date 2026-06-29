using UnityEngine;

public static class Steering
{
    public static Vector3 Seek(Vector3 position, Vector3 velocity, Vector3 target, float maxSpeed)
    {
        Vector3 desired = target - position;
        desired.y = 0f;
        if (desired.sqrMagnitude < 0.0001f) return -velocity;
        desired = desired.normalized * maxSpeed;
        return desired - velocity;
    }

    public static Vector3 Flee(Vector3 position, Vector3 velocity, Vector3 target, float maxSpeed)
    {
        Vector3 desired = position - target;
        desired.y = 0f;
        if (desired.sqrMagnitude < 0.0001f) return Vector3.zero;
        desired = desired.normalized * maxSpeed;
        return desired - velocity;
    }

    public static Vector3 Arrive(Vector3 position, Vector3 velocity, Vector3 target, float maxSpeed, float slowingRadius)
    {
        Vector3 toTarget = target - position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;
        if (distance < 0.001f) return -velocity;

        float ramped = maxSpeed * (distance / Mathf.Max(slowingRadius, 0.001f));
        float clipped = Mathf.Min(ramped, maxSpeed);
        Vector3 desired = toTarget.normalized * clipped;
        return desired - velocity;
    }

    public static Vector3 Pursue(Vector3 position, Vector3 velocity, Vector3 targetPos, Vector3 targetVel, float maxSpeed, float maxPrediction)
    {
        Vector3 toTarget = targetPos - position; toTarget.y = 0f;
        float distance = toTarget.magnitude;
        float speed = velocity.magnitude;

        float prediction = (speed <= distance / maxPrediction)
            ? maxPrediction
            : distance / Mathf.Max(speed, 0.001f);

        Vector3 futurePos = targetPos + targetVel * prediction;
        return Seek(position, velocity, futurePos, maxSpeed);
    }

    public static Vector3 Evade(Vector3 position, Vector3 velocity, Vector3 targetPos, Vector3 targetVel, float maxSpeed, float maxPrediction)
    {
        Vector3 toTarget = targetPos - position; toTarget.y = 0f;
        float distance = toTarget.magnitude;
        float speed = velocity.magnitude;

        float prediction = (speed <= distance / maxPrediction)
            ? maxPrediction
            : distance / Mathf.Max(speed, 0.001f);

        Vector3 futurePos = targetPos + targetVel * prediction;
        return Flee(position, velocity, futurePos, maxSpeed);
    }

    public static Vector3 Wander(Vector3 velocity, ref float wanderAngle, float circleDistance, float circleRadius, float jitter, float maxSpeed)
    {
        wanderAngle += Random.Range(-1f, 1f) * jitter;

        Vector3 forward = velocity.sqrMagnitude > 0.001f ? velocity.normalized : Vector3.forward;
        Vector3 circleCenter = forward * circleDistance;
        Vector3 displacement = new Vector3(Mathf.Cos(wanderAngle), 0f, Mathf.Sin(wanderAngle)) * circleRadius;

        Vector3 desiredDir = circleCenter + displacement; desiredDir.y = 0f;
        if (desiredDir.sqrMagnitude < 0.0001f) return Vector3.zero;
        Vector3 desired = desiredDir.normalized * maxSpeed;
        return desired - velocity;
    }

    public static Vector3 AvoidBlocked(Vector3 position, Vector3 velocity, GridManager grid, float lookAhead, float maxSpeed)
    {
        if (grid == null || velocity.sqrMagnitude < 0.001f) return Vector3.zero;

        Vector3 fwd = velocity.normalized;
        Vector3 ahead = position + fwd * lookAhead;
        if (grid.IsWalkable(ahead)) return Vector3.zero;

        Vector3 left = Vector3.Cross(Vector3.up, fwd); 
        Vector3 leftPoint = position + fwd * (lookAhead * 0.5f) + left * lookAhead;
        Vector3 rightPoint = position + fwd * (lookAhead * 0.5f) - left * lookAhead;

        bool leftOpen = grid.IsWalkable(leftPoint);
        bool rightOpen = grid.IsWalkable(rightPoint);

        Vector3 chosen;
        if (leftOpen && !rightOpen) chosen = left;
        else if (rightOpen && !leftOpen) chosen = -left;
        else chosen = left; 

        Vector3 desired = chosen.normalized * maxSpeed;
        return desired - velocity;
    }
}
