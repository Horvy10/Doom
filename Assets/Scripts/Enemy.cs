using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyKind { Zombie, Imp, Demon }

// Nepriateľ: stojí, kým hráča neuvidí, nepočuje streľbu alebo ho niekto nezraní.
// Potom ho naháňa po NavMeshi a útočí: hitscan (zombie), projektil (imp) alebo zblízka (démon).
[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : MonoBehaviour, IDamageable
{
    public EnemyKind kind = EnemyKind.Zombie;
    public float health = 40f;
    public float speed = 3f;
    public float sightRange = 35f;
    public float attackRange = 20f;           // <= 3 = útok zblízka
    public float attackDamage = 6f;
    public float attackCooldown = 1.4f;
    [Range(0f, 1f)] public float accuracy = 0.55f;   // šanca zásahu pri hitscan útoku
    public Projectile projectilePrefab;               // ak je nastavený, strieľa projektily
    public int scoreValue = 100;
    public Transform eye;

    public static readonly List<Enemy> All = new List<Enemy>();
    public static event Action<Enemy> Killed;
    public event Action<Enemy> Attacked;      // animácia, zvuk (Samo)
    public event Action<Enemy> Hurt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { All.Clear(); Killed = null; }

    enum State { Idle, Chase, Dead }
    State state = State.Idle;
    NavMeshAgent agent;
    float nextAttack;
    float nextRepath;

    public bool IsDead { get { return state == State.Dead; } }
    public bool Melee { get { return attackRange <= 3f; } }
    Vector3 EyePos { get { return eye ? eye.position : transform.position + Vector3.up * 1.6f; } }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        All.Add(this);
    }

    void OnDestroy() { All.Remove(this); }

    void Start()
    {
        LevelData lv = GameSession.Current;
        health *= lv.enemyHealth;
        attackDamage *= lv.enemyDamage;
        speed *= lv.enemySpeed;
        agent.speed = speed;
        agent.stoppingDistance = Melee ? attackRange * 0.7f : 0f;
        nextAttack = Time.time + UnityEngine.Random.Range(0.5f, attackCooldown);
    }

    void Update()
    {
        if (state == State.Dead) return;

        PlayerHealth player = PlayerHealth.Instance;
        if (!agent.isOnNavMesh) { agent.Warp(transform.position); return; }
        if (player == null || player.IsDead) { agent.isStopped = true; return; }

        Vector3 target = player.transform.position;
        float dist = Vector3.Distance(transform.position, target);
        bool sees = dist <= sightRange && CanSee(player);

        if (state == State.Idle)
        {
            if (!sees) return;
            Alert();
        }

        bool inRange = sees && dist <= attackRange;

        // strelci zastanú, keď sú blízko; inak sa stále približujú
        agent.isStopped = !Melee && inRange && dist < attackRange * 0.6f;
        if (!agent.isStopped && Time.time >= nextRepath)
        {
            agent.SetDestination(target);
            nextRepath = Time.time + 0.25f;
        }

        if (inRange)
        {
            Face(target);
            if (Time.time >= nextAttack) Attack(player, dist);
        }
    }

    bool CanSee(PlayerHealth player)
    {
        Vector3 to = player.transform.position + Vector3.up * 1.4f - EyePos;
        if (Physics.Raycast(EyePos, to.normalized, out RaycastHit hit, to.magnitude + 0.5f, ~0, QueryTriggerInteraction.Ignore))
            return hit.collider.GetComponentInParent<PlayerHealth>() == player;
        return true;
    }

    void Face(Vector3 target)
    {
        Vector3 d = target - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude < 0.01f) return;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), 360f * Time.deltaTime);
    }

    void Attack(PlayerHealth player, float dist)
    {
        nextAttack = Time.time + attackCooldown * UnityEngine.Random.Range(0.8f, 1.2f);
        Attacked?.Invoke(this);
        StartCoroutine(AttackPose());

        Vector3 chest = player.transform.position + Vector3.up * 1.2f;
        if (Melee)
        {
            if (dist <= attackRange + 0.5f) player.TakeDamage(attackDamage, chest, transform.forward);
        }
        else if (projectilePrefab)
        {
            Vector3 from = EyePos + transform.forward * 0.6f;
            Projectile p = Instantiate(projectilePrefab, from, Quaternion.LookRotation(chest - from));
            p.Launch(chest - from, attackDamage, gameObject);
        }
        else if (UnityEngine.Random.value < accuracy)
        {
            player.TakeDamage(attackDamage, chest, transform.forward);
        }
    }

    // nepriateľ začne hráča naháňať
    public void Alert()
    {
        if (state == State.Idle) state = State.Chase;
    }

    // hluk (výstrel) zobudí nepriateľov v okruhu
    public static void AlertNear(Vector3 position, float radius)
    {
        float r2 = radius * radius;
        foreach (Enemy e in All)
            if ((e.transform.position - position).sqrMagnitude <= r2) e.Alert();
    }

    public void TakeDamage(float amount, Vector3 point, Vector3 normal)
    {
        if (state == State.Dead) return;
        health -= amount;
        Hurt?.Invoke(this);
        Alert();
        if (health <= 0f) Die();
    }

    void Die()
    {
        state = State.Dead;
        agent.enabled = false;
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;
        StopAllCoroutines();
        StartCoroutine(FallOver());
        Killed?.Invoke(this);
    }

    IEnumerator AttackPose()
    {
        Transform body = transform.Find("Body");
        if (body == null) yield break;
        Vector3 s = body.localScale;
        body.localScale = s * 1.12f;
        yield return new WaitForSeconds(0.15f);
        body.localScale = s;
    }

    // telo ostane ležať (ako v Doome)
    IEnumerator FallOver()
    {
        Quaternion from = transform.rotation;
        Quaternion to = from * Quaternion.Euler(-90f, 0f, 0f);
        for (float k = 0f; k < 1f; k += Time.deltaTime * 3f)
        {
            transform.rotation = Quaternion.Slerp(from, to, k);
            yield return null;
        }
        transform.rotation = to;
        enabled = false;
    }
}
