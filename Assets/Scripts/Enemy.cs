using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyKind { Zombie, Imp, Demon, Heavy }

// Nepriateľ: stojí, kým hráča neuvidí, nepočuje streľbu alebo ho niekto nezraní.
// Potom ho naháňa po NavMeshi a útočí: hitscan (zombie, ťažký strelec), projektil (imp) alebo zblízka (démon).
// Keď ho zraní iný nepriateľ (ohnivá guľa, výbuch), na chvíľu sa obráti proti nemu (bitka medzi nepriateľmi).
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
    public int burst = 1;                             // počet výstrelov v jednom útoku (ťažký strelec)
    public Projectile projectilePrefab;               // ak je nastavený, strieľa projektily
    public int scoreValue = 100;
    public Transform eye;

    [Header("Zásah")]
    [Range(0f, 1f)] public float painChance = 0.5f;  // šanca, že ho zásah na chvíľu zastaví
    public float painTime = 0.3f;

    [Header("Korisť")]
    public Pickup dropPrefab;                         // čo po smrti pustí (napr. náboje)
    [Range(0f, 1f)] public float dropChance = 0f;
    public int dropAmount = 0;                        // 0 = množstvo podľa prefabu

    public static readonly List<Enemy> All = new List<Enemy>();
    public static event Action<Enemy> Killed;
    public event Action<Enemy> Attacked;      // animácia, zvuk (Samo); hitscan: pri každom výstrele
    public event Action<Enemy> Hurt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { All.Clear(); Killed = null; }

    enum State { Idle, Chase, Dead }
    State state = State.Idle;
    NavMeshAgent agent;
    float nextAttack;
    float nextRepath;
    float stunnedUntil;
    Enemy rival;              // nepriateľ, ktorý ho zranil
    float rivalUntil;
    Transform body;
    Vector3 bodyScale;

    public bool IsDead { get { return state == State.Dead; } }
    public bool Melee { get { return attackRange <= 3f; } }
    public bool Stunned { get { return Time.time < stunnedUntil; } }
    Vector3 EyePos { get { return eye ? eye.position : transform.position + Vector3.up * 1.6f; } }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        body = transform.Find("Body");
        if (body) bodyScale = body.localScale;
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
        if (!agent.isOnNavMesh) { agent.Warp(transform.position); return; }

        Transform target = CurrentTarget();
        if (target == null) { agent.isStopped = true; return; }

        float dist = Vector3.Distance(transform.position, target.position);
        bool sees = dist <= sightRange && CanSee(target);

        if (state == State.Idle)
        {
            if (!sees) return;
            Alert();
        }

        if (Stunned) { agent.isStopped = true; return; }

        bool inRange = sees && dist <= attackRange;

        // strelci zastanú, keď sú blízko; inak sa stále približujú
        agent.isStopped = !Melee && inRange && dist < attackRange * 0.6f;
        if (!agent.isStopped && Time.time >= nextRepath)
        {
            agent.SetDestination(target.position);
            nextRepath = Time.time + 0.25f;
        }

        if (inRange)
        {
            Face(target.position);
            if (Time.time >= nextAttack) Attack(target, dist);
        }
    }

    // cieľ: nepriateľ, ktorý ho zranil, inak hráč
    Transform CurrentTarget()
    {
        if (rival && (rival.IsDead || Time.time > rivalUntil)) rival = null;
        if (rival) return rival.transform;
        PlayerHealth player = PlayerHealth.Instance;
        return player != null && !player.IsDead ? player.transform : null;
    }

    static Vector3 Chest(Transform t) { return t.position + Vector3.up * 1.2f; }

    bool CanSee(Transform target)
    {
        Vector3 to = Chest(target) - EyePos;
        if (Physics.Raycast(EyePos, to.normalized, out RaycastHit hit, to.magnitude + 0.5f, ~0, QueryTriggerInteraction.Ignore))
            return hit.collider.transform.IsChildOf(target);
        return true;
    }

    void Face(Vector3 target)
    {
        Vector3 d = target - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude < 0.01f) return;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), 360f * Time.deltaTime);
    }

    void Attack(Transform target, float dist)
    {
        nextAttack = Time.time + attackCooldown * UnityEngine.Random.Range(0.8f, 1.2f);
        IDamageable victim = target.GetComponent<IDamageable>();
        Vector3 chest = Chest(target);

        if (Melee)
        {
            Attacked?.Invoke(this);
            StartCoroutine(AttackPose());
            if (victim != null && dist <= attackRange + 0.5f) victim.TakeDamage(attackDamage, chest, transform.forward, gameObject);
        }
        else if (projectilePrefab)
        {
            Attacked?.Invoke(this);
            StartCoroutine(AttackPose());
            Vector3 from = EyePos + transform.forward * 0.6f;
            Projectile p = Instantiate(projectilePrefab, from, Quaternion.LookRotation(chest - from));
            p.Launch(chest - from, attackDamage, gameObject);
        }
        else
        {
            StartCoroutine(Shoot(target, victim));
        }
    }

    // hitscan: jeden alebo viac výstrelov (burst), každý trafí so šancou accuracy
    IEnumerator Shoot(Transform target, IDamageable victim)
    {
        for (int i = 0; i < Mathf.Max(1, burst); i++)
        {
            if (target == null || state == State.Dead) yield break;
            Attacked?.Invoke(this);
            StartCoroutine(AttackPose());
            if (victim != null && UnityEngine.Random.value < accuracy)
                victim.TakeDamage(attackDamage, Chest(target), transform.forward, gameObject);
            if (i < burst - 1) yield return new WaitForSeconds(0.12f);
        }
    }

    // nepriateľ začne hráča naháňať
    public void Alert()
    {
        if (state == State.Idle) state = State.Chase;
    }

    // hluk (výstrel, výbuch) zobudí nepriateľov v okruhu
    public static void AlertNear(Vector3 position, float radius)
    {
        float r2 = radius * radius;
        foreach (Enemy e in All)
            if ((e.transform.position - position).sqrMagnitude <= r2) e.Alert();
    }

    public void TakeDamage(float amount, Vector3 point, Vector3 normal, GameObject attacker)
    {
        if (state == State.Dead) return;
        health -= amount;
        Hurt?.Invoke(this);

        // obráti sa proti tomu, kto ho zranil naposledy
        Enemy other = attacker ? attacker.GetComponent<Enemy>() : null;
        if (other && other != this && !other.IsDead) { rival = other; rivalUntil = Time.time + 10f; }
        else if (attacker && attacker.GetComponent<PlayerHealth>()) rival = null;

        Alert();
        if (health <= 0f) { Die(); return; }
        if (UnityEngine.Random.value < painChance) stunnedUntil = Time.time + painTime;
    }

    void Die()
    {
        state = State.Dead;
        agent.enabled = false;
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;
        StopAllCoroutines();
        DropLoot();
        StartCoroutine(FallOver());
        Killed?.Invoke(this);
    }

    void DropLoot()
    {
        if (dropPrefab == null || UnityEngine.Random.value >= dropChance) return;
        Pickup p = Instantiate(dropPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        if (dropAmount > 0) p.amount = dropAmount;
    }

    IEnumerator AttackPose()
    {
        if (body == null) yield break;
        body.localScale = bodyScale * 1.12f;
        yield return new WaitForSeconds(0.1f);
        body.localScale = bodyScale;
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
