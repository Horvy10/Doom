using System;
using UnityEngine;

// Projektil: ohnivá guľa impa alebo raketa hráča. Letí rovno a zanikne pri prvom náraze.
// splashRadius > 0 = pri náraze vybuchne (raketa), inak zraní len to, do čoho narazil.
// Projektil nepriateľa preletí cez nepriateľov rovnakého druhu, ostatných zraní (a tí sa potom pobijú).
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    public float speed = 14f;
    public float lifetime = 6f;
    public float splashRadius = 0f;

    public static event Action<Vector3> Impact;   // efekt nárazu (Samo)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Impact = null; }

    Vector3 dir;
    float damage;
    GameObject owner;
    Enemy ownerEnemy;
    bool done;

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
        ownerEnemy = from ? from.GetComponent<Enemy>() : null;
        transform.rotation = Quaternion.LookRotation(dir);
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.position += dir * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (done || other.isTrigger) return;
        if (owner && other.transform.IsChildOf(owner.transform)) return;

        Enemy hitEnemy = other.GetComponentInParent<Enemy>();
        if (ownerEnemy && hitEnemy && hitEnemy.kind == ownerEnemy.kind) return;   // rovnaký druh preletí

        done = true;
        Vector3 point = transform.position - dir * 0.3f;   // kúsok pred stenou, nie v nej
        if (splashRadius > 0f)
        {
            Explosion.Blast(point, splashRadius, damage, owner);
        }
        else
        {
            IDamageable target = other.GetComponentInParent<IDamageable>();
            if (target != null) target.TakeDamage(damage, transform.position, dir, owner);
        }

        Impact?.Invoke(point);
        Destroy(gameObject);
    }
}
