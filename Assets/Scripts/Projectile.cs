using System;
using UnityEngine;

// Ohnivá guľa impa. Letí rovno, zraní hráča, zanikne pri náraze do steny.
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    public float speed = 14f;
    public float lifetime = 6f;

    public static event Action<Vector3> Impact;   // efekt výbuchu (Samo)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Impact = null; }

    Vector3 dir;
    float damage;
    GameObject owner;

    void Awake()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    public void Launch(Vector3 direction, float dmg, GameObject from)
    {
        dir = direction.normalized;
        damage = dmg;
        owner = from;
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.position += dir * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger) return;
        if (owner && other.transform.IsChildOf(owner.transform)) return;
        if (other.GetComponentInParent<Enemy>()) return;   // cez iných nepriateľov preletí

        PlayerHealth p = other.GetComponentInParent<PlayerHealth>();
        if (p) p.TakeDamage(damage, transform.position, dir);

        Impact?.Invoke(transform.position);
        Destroy(gameObject);
    }
}
