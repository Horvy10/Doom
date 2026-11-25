using System.Collections;
using UnityEngine;

// Samo: efekty a zvuky. Počúva eventy hry (README: Napojenie pre grafiku) a spúšťa častice, svetlá a zvuky.
// Všetky referencie nastaví Tools > Samo > Grafika, efekty a zvuky.
public class Efekty : MonoBehaviour
{
    public static Efekty Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Instance = null; }

    [Header("Častice")]
    public GameObject sparks;
    public GameObject blood;
    public GameObject bloodBig;
    public GameObject explosion;
    public GameObject fireImpact;
    public GameObject muzzle;

    [Header("Zvuky")]
    public AudioClip[] weaponClips;     // index = zbraň (päsť, pištoľ, brokovnica, guľomet, raketomet)
    public AudioClip explosionClip, impactClip, enemyShot, fireballClip, biteClip;
    public AudioClip enemyHurt, enemyDeath, doorClip, deniedClip, pickupClip, weaponPickupClip, secretClip;
    public AudioClip playerHurt, playerDeath, ambience;

    AudioSource ui;          // 2D zvuky hráča
    WeaponSystem ws;
    PlayerHealth player;
    float nextHurtSound;
    float flashUntil;
    Transform flashShown;
    float spin;              // rýchlosť točenia hlavne guľometu

    void Awake()
    {
        Instance = this;
        ui = gameObject.AddComponent<AudioSource>();
        ui.playOnAwake = false;
        ui.volume = 0.55f;

        if (ambience)
        {
            AudioSource amb = gameObject.AddComponent<AudioSource>();
            amb.clip = ambience;
            amb.loop = true;
            amb.volume = 0.15f;
            amb.Play();
        }
    }

    void OnEnable()
    {
        WeaponSystem.Hit += OnHit;
        Projectile.Impact += OnImpact;
        Explosion.Exploded += OnExploded;
        Enemy.Killed += OnKilled;
        Door.Opened += OnDoor;
        Door.NeedKey += OnNeedKey;
        Secret.Found += OnSecret;
        Pickup.Collected += OnPickup;
    }

    void OnDisable()
    {
        WeaponSystem.Hit -= OnHit;
        Projectile.Impact -= OnImpact;
        Explosion.Exploded -= OnExploded;
        Enemy.Killed -= OnKilled;
        Door.Opened -= OnDoor;
        Door.NeedKey -= OnNeedKey;
        Secret.Found -= OnSecret;
        Pickup.Collected -= OnPickup;
        if (ws) ws.Fired -= OnFired;
        if (player) { player.Damaged -= OnPlayerDamaged; player.Died -= OnPlayerDied; }
    }

    void Start()
    {
        ws = FindAnyObjectByType<WeaponSystem>();
        if (ws) ws.Fired += OnFired;
        player = PlayerHealth.Instance;
        if (player) { player.Damaged += OnPlayerDamaged; player.Died += OnPlayerDied; }
    }

    void Update()
    {
        if (flashShown && Time.time >= flashUntil) { flashShown.gameObject.SetActive(false); flashShown = null; }

        spin = Mathf.MoveTowards(spin, 0f, 2000f * Time.deltaTime);
        Transform barrels = ViewModelChild(3, "Barrels");
        if (barrels) barrels.Rotate(0f, 0f, spin * Time.deltaTime, Space.Self);
    }

    // ───────── hráč ─────────

    void OnFired()
    {
        int i = ws.Current;
        if (weaponClips != null && i < weaponClips.Length && weaponClips[i]) ui.PlayOneShot(weaponClips[i], i == 3 ? 0.6f : 1f);
        if (i == 3) spin = 1500f;

        Transform flash = ViewModelChild(i, "Flash");
        if (flash == null) return;
        if (flashShown && flashShown != flash) flashShown.gameObject.SetActive(false);
        flash.gameObject.SetActive(true);
        flash.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        flashShown = flash;
        flashUntil = Time.time + 0.05f;
    }

    Transform ViewModelChild(int weapon, string child)
    {
        if (ws == null || ws.viewModels == null || weapon >= ws.viewModels.Length || ws.viewModels[weapon] == null) return null;
        return ws.viewModels[weapon].transform.Find(child);
    }

    void OnPlayerDamaged(float amount)
    {
        if (Time.time < nextHurtSound) return;
        nextHurtSound = Time.time + 0.35f;
        ui.PlayOneShot(playerHurt);
    }

    void OnPlayerDied() { ui.PlayOneShot(playerDeath); }

    // ───────── svet ─────────

    void OnHit(Vector3 point, Vector3 normal, bool enemy)
    {
        Spawn(enemy ? blood : sparks, point, normal);
    }

    void OnImpact(Vector3 point)
    {
        Spawn(fireImpact, point, Vector3.up);
        Play(impactClip, point, 0.7f);
    }

    void OnExploded(Vector3 center, float radius)
    {
        GameObject fx = Spawn(explosion, center, Vector3.up);
        if (fx) fx.transform.localScale = Vector3.one * (radius / 4f);
        Play(explosionClip, center, 1f, 60f);
        StartCoroutine(Flash(center, radius));
    }

    IEnumerator Flash(Vector3 at, float radius)
    {
        Light l = new GameObject("ExplosionLight").AddComponent<Light>();
        l.transform.position = at;
        l.type = LightType.Point;
        l.color = new Color(1f, 0.6f, 0.25f);
        l.range = radius * 3f;
        for (float k = 1f; k > 0f; k -= Time.deltaTime * 2.5f)
        {
            l.intensity = 10f * k * k;
            yield return null;
        }
        Destroy(l.gameObject);
    }

    void OnKilled(Enemy e)
    {
        Spawn(bloodBig, e.transform.position + Vector3.up * 1.1f, Vector3.up);
        Play(enemyDeath, e.transform.position, 1f, 40f, Pitch(e));
    }

    void OnDoor(Door d) { Play(doorClip, d.transform.position, 0.9f); }

    void OnNeedKey(KeyColor key) { ui.PlayOneShot(deniedClip); }

    void OnSecret(Secret s) { ui.PlayOneShot(secretClip); }

    void OnPickup(Pickup p)
    {
        bool big = p.type == PickupType.Weapon || p.type == PickupType.Key;
        ui.PlayOneShot(big ? weaponPickupClip : pickupClip, 0.8f);
    }

    // ───────── nepriatelia (cez EnemyFx) ─────────

    public void EnemyAttacked(Enemy e, Transform from)
    {
        Vector3 at = from ? from.position : e.transform.position + Vector3.up * 1.5f;
        switch (e.kind)
        {
            case EnemyKind.Imp: Play(fireballClip, at, 0.9f); break;
            case EnemyKind.Demon: Play(biteClip, at, 1f); break;
            default:
                Play(enemyShot, at, 0.8f, 40f, Pitch(e));
                Spawn(muzzle, at, e.transform.forward);
                break;
        }
    }

    public void EnemyHurt(Enemy e)
    {
        if (e.health > 0f) Play(enemyHurt, e.transform.position + Vector3.up, 0.8f, 30f, Pitch(e));
    }

    // väčší nepriateľ = hlbší hlas
    static float Pitch(Enemy e)
    {
        switch (e.kind)
        {
            case EnemyKind.Imp: return 1.15f;
            case EnemyKind.Demon: return 0.75f;
            case EnemyKind.Heavy: return 0.65f;
        }
        return 1f;
    }

    // ───────── pomocné ─────────

    static GameObject Spawn(GameObject prefab, Vector3 at, Vector3 dir)
    {
        if (prefab == null) return null;
        GameObject go = Instantiate(prefab, at, Quaternion.LookRotation(dir.sqrMagnitude > 0.0001f ? dir : Vector3.up));
        Destroy(go, 3f);
        return go;
    }

    static void Play(AudioClip clip, Vector3 at, float volume, float maxDistance = 35f, float pitch = 1f)
    {
        if (clip == null) return;
        AudioSource s = new GameObject("Sfx").AddComponent<AudioSource>();
        s.transform.position = at;
        s.clip = clip;
        s.volume = volume * 0.6f;   // celková hlasitosť rieši AudioListener (Nastavenia)
        s.pitch = pitch * Random.Range(0.92f, 1.08f);
        s.spatialBlend = 1f;
        s.rolloffMode = AudioRolloffMode.Linear;
        s.minDistance = 3f;
        s.maxDistance = maxDistance;
        s.Play();
        Destroy(s.gameObject, clip.length / s.pitch + 0.1f);
    }
}
