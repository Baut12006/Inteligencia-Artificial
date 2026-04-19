using UnityEngine;

[CreateAssetMenu(fileName = "NewPatrolRoute", menuName = "AI/Patrol Route")]
public class PatrolRoute : ScriptableObject
{
    [SerializeField] private Vector3[] waypoints;

    public int WaypointCount => waypoints != null ? waypoints.Length : 0;

    public Vector3 GetWaypoint(int index)
    {
        if (waypoints == null || waypoints.Length == 0)
            return Vector3.zero;

        return waypoints[index % waypoints.Length];
    }
}
