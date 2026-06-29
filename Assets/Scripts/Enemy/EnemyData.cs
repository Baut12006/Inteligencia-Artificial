using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "AI/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public enum MovementProfile { Waypoints, Hunter, Scout }

    [Header("Enemy Type")]
    public string enemyTypeName = "Normal";
    public MovementProfile movementProfile = MovementProfile.Waypoints;

    [Header("Detection")]
    public bool canSeeInShadows = false;
    public float closeDetectionRange = 3f;
    public bool requiresLineOfSight = true;
    public bool requiresAngleCheck = true;
    public bool requiresRangeCheck = true;

    [Header("Movement")]
    public float patrolSpeed = 3f;
    public float pursuitSpeed = 5f;
    public float rotationSpeed = 5f;

    [Header("Behavior")]
    public bool canPatrol = true;
    public bool canPursue = true;
    public float searchDuration = 5f;

    [Header("Steering")]
    public float maxForce = 20f;
    public float arriveRadius = 2.5f;
    public float predictionTime = 1f;

    [Header("Wander (Scout)")]
    public float wanderCircleDistance = 2f;
    public float wanderCircleRadius = 1.2f;
    public float wanderJitter = 0.3f;

    [Header("Pathfinding")]
    public bool usePathfinding = true;
    public float pathRecalcTime = 0.5f;
    public float waypointTolerance = 1f;

    [Header("Separation (anti-amontonamiento)")]
    public float separationRadius = 1.5f;
    public float separationWeight = 1.5f;

    [Header("Vision Light")]
    public Color visionLightColor = Color.yellow;
    public float lightIntensity = 2f;
    public bool showVisionLight = true;

    [Header("Sentry Specific")]
    public bool isSentry = false;
    public float sentryRotationSpeed = 2f;
    public float sentryViewAngle = 360f;
    public float sentryNavRadius = 1.2f;
}
