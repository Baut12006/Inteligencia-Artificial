using UnityEngine;

public static class CombatHelper
{
    // Threshold: -1.0 = completamente detrás, 0.0 = perpendicular, 1.0 = completamente de frente
    public static bool IsAttackFromBehind(Transform attacker, Transform victim, float threshold = -0.2f)
    {
        Vector3 dirToAttacker = (attacker.position - victim.position).normalized;
        float dotProduct = Vector3.Dot(victim.forward, dirToAttacker);

        return dotProduct < threshold;
    }

    public static void DrawAttackDebug(Transform entity, Color color)
    {
        Debug.DrawRay(entity.position, entity.forward * 2f, color, 0.5f);
    }
}