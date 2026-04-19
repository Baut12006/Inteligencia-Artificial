using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "AI/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Enemy Type")]
    public string enemyTypeName = "Normal";

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

    [Header("Vision Light")]
    public Color visionLightColor = Color.yellow;
    public float lightIntensity = 2f;
    public bool showVisionLight = true;

    [Header("Sentry Specific")]
    public bool isSentry = false;
    public float sentryRotationSpeed = 2f;
    public float sentryViewAngle = 360f;
}