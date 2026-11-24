using System;
using UnityEngine;

// Výbušný sud ('o' v mape). Po pár zásahoch vybuchne a zraní všetko okolo, aj iné sudy (reťazová reakcia).
public class Barrel : MonoBehaviour, IDamageable
{
    public float health = 20f;
    public float radius = 4f;
    public float damage = 80f;
    public float delay = 0.15f;    // oneskorenie výbuchu, aby reťaz sudov nevybuchla naraz

    public static event Action<Barrel> Exploding;   // tesne pred výbuchom (Samo: zvuk, efekt)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Exploding = null; }

    bool exploded;
    GameObject lastAttacker;

    public void TakeDamage(float amount, Vector3 point, Vector3 normal, GameObject attacker)
    {
        if (exploded) return;
        health -= amount;
        if (attacker) lastAttacker = attacker;
        if (health > 0f) return;
        exploded = true;
        Invoke(nameof(Explode), delay);
    }

    void Explode()
    {
        Exploding?.Invoke(this);
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;
        // útočník = ten, kto sud rozstrelil (nepriatelia sa potom obrátia proti nemu)
        Explosion.Blast(transform.position + Vector3.up * 0.6f, radius, damage, lastAttacker);
        Destroy(gameObject);
    }
}
