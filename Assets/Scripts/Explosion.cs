using System;
using System.Collections.Generic;
using UnityEngine;

// Výbuch (sud, raketa): zraní všetko v okruhu, čím bližšie, tým viac. Steny výbuch zastavia.
public static class Explosion
{
    public static event Action<Vector3, float> Exploded;   // bod, polomer (efekt, zvuk – Samo)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Exploded = null; }

    public static void Blast(Vector3 center, float radius, float damage, GameObject attacker)
    {
        Exploded?.Invoke(center, radius);
        Enemy.AlertNear(center, radius * 4f);   // výbuch je počuť ďaleko

        HashSet<IDamageable> done = new HashSet<IDamageable>();
        foreach (Collider c in Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Ignore))
        {
            IDamageable target = c.GetComponentInParent<IDamageable>();
            if (target == null || !done.Add(target)) continue;

            Vector3 point = c.ClosestPoint(center);
            if (Blocked(center, point, target)) continue;

            float k = 1f - Vector3.Distance(center, point) / radius;
            if (k <= 0f) continue;
            Vector3 dir = point - center;
            target.TakeDamage(damage * k, point, dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.up, attacker);
        }
    }

    // je medzi stredom výbuchu a cieľom stena?
    static bool Blocked(Vector3 from, Vector3 to, IDamageable target)
    {
        if (!Physics.Linecast(from, to, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore)) return false;
        return hit.collider.GetComponentInParent<IDamageable>() != target && (to - hit.point).sqrMagnitude > 0.01f;
    }
}
