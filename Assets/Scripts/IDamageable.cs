using UnityEngine;

// Všetko, čo sa dá zraniť (hráč, nepriatelia, sudy).
// attacker = kto zranenie spôsobil (hráč, nepriateľ, alebo null). Nepriatelia podľa neho vedia, s kým sa pobiť.
public interface IDamageable
{
    void TakeDamage(float amount, Vector3 point, Vector3 normal, GameObject attacker);
}
