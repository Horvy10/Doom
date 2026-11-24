using UnityEngine;

// Všetko, čo sa dá zraniť (hráč, nepriatelia, prípadne sudy)
public interface IDamageable
{
    void TakeDamage(float amount, Vector3 point, Vector3 normal);
}
