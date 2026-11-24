using System;
using System.Collections;
using UnityEngine;

// Zdravie a brnenie hráča. Brnenie zachytí časť poškodenia (ako v Doome).
public class PlayerHealth : MonoBehaviour, IDamageable
{
    public float maxHealth = 100f;
    public float maxArmor = 200f;
    [Range(0f, 1f)] public float armorAbsorb = 1f / 3f;
    public float health = 100f;
    public float armor = 0f;

    public static PlayerHealth Instance { get; private set; }

    public event Action<float> Damaged;   // HUD: červený záblesk
    public event Action Died;

    public bool IsDead { get; private set; }

    void Awake() { Instance = this; }

    void OnDestroy() { if (Instance == this) Instance = null; }

    public void TakeDamage(float amount, Vector3 point, Vector3 normal, GameObject attacker)
    {
        if (IsDead || amount <= 0f) return;

        float saved = Mathf.Min(armor, amount * armorAbsorb);
        armor -= saved;
        health -= amount - saved;
        Damaged?.Invoke(amount);

        if (health <= 0f)
        {
            health = 0f;
            IsDead = true;
            PlayerController pc = GetComponent<PlayerController>();
            if (pc) pc.enabled = false;
            StartCoroutine(DeathCamera());
            Died?.Invoke();
        }
    }

    // vráti false, ak už je plné (pickup ostane ležať)
    public bool Heal(float amount)
    {
        if (IsDead || health >= maxHealth) return false;
        health = Mathf.Min(maxHealth, health + amount);
        return true;
    }

    public bool AddArmor(float amount)
    {
        if (IsDead || armor >= maxArmor) return false;
        armor = Mathf.Min(maxArmor, armor + amount);
        return true;
    }

    // kamera klesne k zemi
    IEnumerator DeathCamera()
    {
        Camera cam = GetComponentInChildren<Camera>();
        if (cam == null) yield break;
        Transform t = cam.transform;
        Vector3 from = t.localPosition;
        Vector3 to = new Vector3(from.x, from.y - 1.2f, from.z);
        for (float k = 0f; k < 1f; k += Time.deltaTime * 2f)
        {
            t.localPosition = Vector3.Lerp(from, to, k);
            t.localRotation = Quaternion.Slerp(t.localRotation, Quaternion.Euler(0f, 0f, 30f), k);
            yield return null;
        }
    }
}
