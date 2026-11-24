using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Weapon
{
    public string name;
    public float damage;        // poškodenie jedného zásahu (broky: jedného broku)
    public int pellets = 1;     // počet striel naraz (brokovnica)
    public float spread;        // rozptyl v stupňoch
    public float fireDelay;     // sekundy medzi výstrelmi
    public float range = 100f;
    public AmmoType ammo;
    public int ammoPerShot = 1;
    public bool owned;
    public Projectile projectile;   // ak je nastavený, zbraň strieľa projektil (raketomet), damage = sila výbuchu

    public bool Melee { get { return range <= 3f; } }

    public Weapon() { }

    public Weapon(string name, float damage, int pellets, float spread, float fireDelay,
                  float range, AmmoType ammo, bool owned)
    {
        this.name = name;
        this.damage = damage;
        this.pellets = pellets;
        this.spread = spread;
        this.fireDelay = fireDelay;
        this.range = range;
        this.ammo = ammo;
        this.owned = owned;
    }
}

// Zbrane hráča: výber 1-5 alebo kolieskom, streľba ľavým tlačidlom (podržať = automaticky).
// Streľba je raycast z kamery. Model zbrane je zatiaľ kocka; Samo môže dať vlastné do viewModels.
public class WeaponSystem : MonoBehaviour
{
    public Camera cam;
    public List<Weapon> weapons = new List<Weapon>
    {
        new Weapon("Päsť",       20f, 1, 0f,   0.50f, 2.2f, AmmoType.None,    true),
        new Weapon("Pištoľ",     15f, 1, 1f,   0.40f, 100f, AmmoType.Bullets, true),
        new Weapon("Brokovnica", 10f, 7, 5f,   0.90f, 60f,  AmmoType.Shells,  false),
        new Weapon("Guľomet",    12f, 1, 2.5f, 0.11f, 100f, AmmoType.Bullets, false),
        new Weapon("Raketomet", 100f, 1, 0f,   0.80f, 100f, AmmoType.Rockets, false),
    };
    public float noiseRadius = 25f;          // streľba zobudí nepriateľov v tomto okruhu

    [Header("Grafika (Samo)")]
    public Material viewModelMaterial;
    public GameObject[] viewModels;           // voliteľné: model pre každú zbraň (index = poradie zbrane)

    public int Current { get; private set; } = 1;
    public Weapon CurrentWeapon { get { return weapons[Current]; } }

    public event Action Fired;                // zvuk, animácia
    public event Action Switched;
    public static event Action<Vector3, Vector3, bool> Hit;   // bod, normála, zasiahnutý nepriateľ (efekty)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Hit = null; }

    static readonly Vector3[] PlaceholderScales =
    {
        new Vector3(0.12f, 0.12f, 0.20f),
        new Vector3(0.07f, 0.12f, 0.28f),
        new Vector3(0.09f, 0.09f, 0.60f),
        new Vector3(0.16f, 0.16f, 0.50f),
        new Vector3(0.18f, 0.18f, 0.70f),
    };

    PlayerInventory inv;
    PlayerHealth hp;
    CharacterController cc;
    float nextFire;
    Transform view;
    Transform placeholder;
    Vector3 viewRest = new Vector3(0.28f, -0.28f, 0.45f);
    float kick;
    Light flash;
    float flashUntil;

    void Start()
    {
        inv = GetComponent<PlayerInventory>();
        hp = GetComponent<PlayerHealth>();
        cc = GetComponent<CharacterController>();
        if (cam == null) cam = GetComponentInChildren<Camera>();
        BuildViewModel();
    }

    void Update()
    {
        if (PlayerController.InputLocked || Time.timeScale == 0f || (hp && hp.IsDead)) return;

        for (int i = 0; i < weapons.Count && i < 9; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i)) Select(i);

        float wheel = Input.mouseScrollDelta.y;
        if (wheel > 0f) Cycle(-1);
        else if (wheel < 0f) Cycle(1);

        if (Input.GetMouseButton(0) && Time.time >= nextFire) Fire();

        AnimateView();
    }

    public bool HasAmmo(Weapon w) { return w.ammo == AmmoType.None || inv.Get(w.ammo) >= w.ammoPerShot; }

    public void Select(int i)
    {
        if (i < 0 || i >= weapons.Count || !weapons[i].owned || i == Current) return;
        Current = i;
        nextFire = Time.time + 0.25f;
        RefreshViewModel();
        Switched?.Invoke();
    }

    void Cycle(int dir)
    {
        int n = weapons.Count;
        for (int k = 1; k < n; k++)
        {
            int i = ((Current + dir * k) % n + n) % n;
            if (weapons[i].owned) { Select(i); return; }
        }
    }

    // pickup zbrane: pridá ju a trochu munície; false = nič nové
    public bool Give(int i)
    {
        if (i < 0 || i >= weapons.Count) return false;
        Weapon w = weapons[i];
        bool gotAmmo = inv.AddAmmo(w.ammo, StartAmmo(w.ammo));
        if (w.owned) return gotAmmo;
        w.owned = true;
        Select(i);
        return true;
    }

    // koľko munície dá zobratie zbrane
    static int StartAmmo(AmmoType type)
    {
        switch (type)
        {
            case AmmoType.Shells: return 8;
            case AmmoType.Rockets: return 2;
            case AmmoType.None: return 0;
        }
        return 20;
    }

    // keď dôjde munícia, prepne na najlepšiu zbraň s muníciou (na raketomet sám neprepne, mohol by si sa zraniť)
    void SwitchToBest()
    {
        for (int i = weapons.Count - 1; i >= 0; i--)
            if (i != Current && weapons[i].owned && weapons[i].projectile == null && HasAmmo(weapons[i])) { Select(i); return; }
    }

    void Fire()
    {
        Weapon w = CurrentWeapon;
        if (!inv.Use(w.ammo, w.ammoPerShot)) { SwitchToBest(); return; }
        nextFire = Time.time + w.fireDelay;

        Transform c = cam.transform;
        if (w.projectile)
        {
            Vector3 from = c.position + c.forward * 0.8f;
            Projectile proj = Instantiate(w.projectile, from, c.rotation);
            proj.Launch(c.forward, w.damage, gameObject);
        }
        for (int p = 0; !w.projectile && p < w.pellets; p++)
        {
            Quaternion offset = Quaternion.Euler(UnityEngine.Random.Range(-w.spread, w.spread),
                                                 UnityEngine.Random.Range(-w.spread, w.spread), 0f);
            Vector3 dir = c.rotation * offset * Vector3.forward;

            if (Physics.Raycast(c.position, dir, out RaycastHit hit, w.range, ~0, QueryTriggerInteraction.Ignore))
            {
                IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
                if (target != null && !(target is PlayerHealth))
                    target.TakeDamage(w.damage * UnityEngine.Random.Range(0.8f, 1.2f), hit.point, hit.normal, gameObject);
                Hit?.Invoke(hit.point, hit.normal, target is Enemy);
            }
        }

        if (!w.Melee)
        {
            Enemy.AlertNear(transform.position, noiseRadius);
            flashUntil = Time.time + 0.05f;
        }
        kick = 1f;
        Fired?.Invoke();
    }

    // ───────── model zbrane v ruke ─────────

    void BuildViewModel()
    {
        view = new GameObject("ViewModel").transform;
        view.SetParent(cam.transform, false);
        view.localPosition = viewRest;

        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        DestroyImmediate(cube.GetComponent<Collider>());   // nesmie zachytávať vlastné výstrely
        cube.name = "Placeholder";
        if (viewModelMaterial) cube.GetComponent<Renderer>().sharedMaterial = viewModelMaterial;
        placeholder = cube.transform;
        placeholder.SetParent(view, false);

        flash = new GameObject("MuzzleFlash").AddComponent<Light>();
        flash.transform.SetParent(view, false);
        flash.transform.localPosition = new Vector3(0f, 0f, 0.5f);
        flash.type = LightType.Point;
        flash.range = 8f;
        flash.intensity = 3f;
        flash.color = new Color(1f, 0.8f, 0.4f);
        flash.enabled = false;

        if (viewModels != null)
            foreach (GameObject m in viewModels)
                if (m) m.transform.SetParent(view, false);

        RefreshViewModel();
    }

    void RefreshViewModel()
    {
        if (view == null) return;
        bool custom = viewModels != null && Current < viewModels.Length && viewModels[Current] != null;
        if (viewModels != null)
            for (int i = 0; i < viewModels.Length; i++)
                if (viewModels[i]) viewModels[i].SetActive(i == Current);

        placeholder.gameObject.SetActive(!custom);
        placeholder.localScale = PlaceholderScales[Mathf.Min(Current, PlaceholderScales.Length - 1)];
        placeholder.localPosition = new Vector3(0f, 0f, placeholder.localScale.z * 0.5f);
    }

    void AnimateView()
    {
        if (view == null) return;
        kick = Mathf.MoveTowards(kick, 0f, Time.deltaTime * 6f);
        float speed = cc ? new Vector3(cc.velocity.x, 0f, cc.velocity.z).magnitude : 0f;
        float bob = Mathf.Sin(Time.time * 10f) * 0.015f * Mathf.Clamp01(speed / 7f);
        float push = CurrentWeapon.Melee ? 0.25f * kick : -0.12f * kick;   // päsť ide dopredu, zbraň dozadu
        view.localPosition = viewRest + new Vector3(bob, Mathf.Abs(bob), push);
        flash.enabled = Time.time < flashUntil;
    }
}
