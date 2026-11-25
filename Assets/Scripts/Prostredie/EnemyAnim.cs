using UnityEngine;
using UnityEngine.AI;

// Samo: procedurálna animácia nepriateľa – chôdza podľa rýchlosti NavMeshAgenta, dýchanie, obzeranie,
// útok, trhnutie a záblesk po zásahu, póza po smrti (pád robí Enemy.FallOver).
// Kosti sú prázdne objekty v Body/Model/Rig, vytvára ich SamoSetup:
// LegL, LegR, ArmL, ArmR, Head, Jaw (démon má LegFL, LegFR, LegBL, LegBR).
[RequireComponent(typeof(Enemy))]
public class EnemyAnim : MonoBehaviour
{
    public float stride = 30f;        // max. uhol nôh pri chôdzi (stupne)
    public float stepRate = 3.5f;     // radiány kroku na meter
    public float bob = 0.05f;         // poskakovanie pri chôdzi

    Enemy enemy;
    NavMeshAgent agent;
    Transform rig, shadow, legL, legR, armL, armR, head, jaw, legFL, legFR, legBL, legBR;
    Vector3 rigPos, headPos;
    Renderer[] rends;
    MaterialPropertyBlock mpb;
    bool flashing;
    float phase, attack, hurt, dead, seed;

    void Awake()
    {
        enemy = GetComponent<Enemy>();
        agent = GetComponent<NavMeshAgent>();
        rig = transform.Find("Body/Model/Rig");
        if (rig == null) { enabled = false; return; }
        shadow = transform.Find("Body/Model/Shadow");
        rigPos = rig.localPosition;
        legL = Bone("LegL"); legR = Bone("LegR"); armL = Bone("ArmL"); armR = Bone("ArmR");
        head = Bone("Head"); jaw = Bone("Jaw");
        legFL = Bone("LegFL"); legFR = Bone("LegFR"); legBL = Bone("LegBL"); legBR = Bone("LegBR");
        if (head) headPos = head.localPosition;
        rends = rig.GetComponentsInChildren<Renderer>();
        mpb = new MaterialPropertyBlock();
        seed = Random.value * 10f;
        enemy.Attacked += e => attack = 1f;
        enemy.Hurt += e => hurt = 1f;
    }

    Transform Bone(string name) { return Find(rig, name); }

    static Transform Find(Transform t, string name)
    {
        foreach (Transform c in t)
        {
            if (c.name == name) return c;
            Transform r = Find(c, name);
            if (r) return r;
        }
        return null;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        attack = Mathf.MoveTowards(attack, 0f, dt * 3f);
        hurt = Mathf.MoveTowards(hurt, 0f, dt * 5f);
        Flash(hurt > 0.65f);

        if (enemy.IsDead) { Dead(dt); return; }

        float speed = agent.enabled ? new Vector3(agent.velocity.x, 0f, agent.velocity.z).magnitude : 0f;
        float move = Mathf.Clamp01(speed / Mathf.Max(0.5f, agent.speed));
        phase += speed * dt * stepRate;
        float s = Mathf.Sin(phase) * stride * move;
        float t = Time.time + seed;
        float breathe = Mathf.Sin(t * 2.2f);

        Rot(legL, s); Rot(legR, -s);
        Rot(legFL, s); Rot(legBR, s); Rot(legFR, -s); Rot(legBL, -s);

        float strike = Mathf.Sin(attack * Mathf.PI);   // 0 → 1 → 0
        switch (enemy.kind)
        {
            case EnemyKind.Imp:      // hod ohnivej gule oboma rukami
                Rot(armL, -s * 0.8f - strike * 110f + breathe * 4f);
                Rot(armR, s * 0.8f - strike * 110f - breathe * 4f);
                break;
            case EnemyKind.Demon:    // otvorí papuľu a výpad hlavou
                Rot(jaw, attack * 45f + (breathe + 1f) * 3f);
                if (head) head.localPosition = headPos + Vector3.forward * strike * 0.2f;
                break;
            default:                 // strelci: ľavá ruka sa kýve, pravá drží zbraň a pri výstrele kopne
                Rot(armL, -s * 0.7f + breathe * 3f);
                Rot(armR, -attack * 18f + breathe);
                break;
        }

        if (head && enemy.kind != EnemyKind.Demon)
            head.localRotation = Quaternion.Euler(breathe * 2f, move < 0.1f ? Mathf.Sin(t * 0.6f) * 25f : 0f, 0f);

        rig.localPosition = rigPos + Vector3.up * (Mathf.Abs(Mathf.Cos(phase)) * bob * move + breathe * 0.01f);
        rig.localRotation = Quaternion.Euler(move * 6f - hurt * 14f, 0f, Mathf.Sin(phase) * 3f * move);
    }

    static void Rot(Transform t, float x) { if (t) t.localRotation = Quaternion.Euler(x, 0f, 0f); }

    // po smrti: ruky nad hlavu, nohy od seba (telo padá dozadu cez Enemy.FallOver)
    void Dead(float dt)
    {
        if (shadow) shadow.gameObject.SetActive(false);
        dead = Mathf.MoveTowards(dead, 1f, dt * 3f);
        float k = 1f - (1f - dead) * (1f - dead);
        Rot(armL, -150f * k); Rot(armR, -140f * k);
        Rot(legL, 15f * k); Rot(legR, -20f * k);
        Rot(legFL, -60f * k); Rot(legFR, -60f * k); Rot(legBL, 60f * k); Rot(legBR, 60f * k);
        Rot(jaw, 40f * k);
        if (head) head.localRotation = Quaternion.Euler(-25f * k, 0f, 0f);
        rig.localRotation = Quaternion.identity;
        if (dead >= 1f && hurt <= 0f) enabled = false;
    }

    void Flash(bool on)
    {
        if (on == flashing) return;
        flashing = on;
        if (on) mpb.SetColor("_BaseColor", new Color(2.6f, 1.1f, 1f));
        foreach (Renderer r in rends) r.SetPropertyBlock(on ? mpb : null);
    }
}
