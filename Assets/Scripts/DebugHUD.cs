using UnityEngine;

// DOČASNÉ HUD na testovanie (OnGUI). Marco ho nahradí poriadnym UI v Scripts/UI a tento objekt zo scény zmaže.
// Esc = pauza, R = znova (po konci), N = ďalší level (po výhre).
public class DebugHUD : MonoBehaviour
{
    PlayerHealth hp;
    PlayerInventory inv;
    WeaponSystem ws;
    GameManager gm;
    SurvivalSpawner spawner;
    bool paused;
    string message;
    float messageUntil;
    float damageFlash;

    void Start()
    {
        hp = PlayerHealth.Instance;
        inv = hp.GetComponent<PlayerInventory>();
        ws = hp.GetComponent<WeaponSystem>();
        gm = GameManager.Instance;
        hp.Damaged += OnDamaged;
        Door.NeedKey += OnNeedKey;
        Pickup.Collected += OnCollected;
        Secret.Found += OnSecret;
        spawner = FindAnyObjectByType<SurvivalSpawner>();
        if (spawner) spawner.WaveStarted += OnWave;
    }

    void OnDestroy()
    {
        if (hp) hp.Damaged -= OnDamaged;
        Door.NeedKey -= OnNeedKey;
        Pickup.Collected -= OnCollected;
        Secret.Found -= OnSecret;
        if (spawner) spawner.WaveStarted -= OnWave;
    }

    void OnDamaged(float amount) { damageFlash = 0.5f; }
    void OnNeedKey(KeyColor k) { Show("Potrebuješ " + Pickup.KeyName(k) + " kľúč"); }
    void OnCollected(Pickup p) { Show("Zobral si: " + p.DisplayName); }
    void OnSecret(Secret s) { Show("Našiel si tajné miesto!"); }
    void OnWave(int wave) { Show("Vlna " + wave); }

    void Show(string text)
    {
        message = text;
        messageUntil = Time.unscaledTime + 2f;
    }

    void Update()
    {
        damageFlash = Mathf.MoveTowards(damageFlash, 0f, Time.unscaledDeltaTime);

        if (gm.CurrentState == GameManager.State.Playing)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                paused = !paused;
                Time.timeScale = paused ? 0f : 1f;
                PlayerController.LockCursor(!paused);
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.R)) gm.Restart();
        if (gm.CurrentState == GameManager.State.Won && Input.GetKeyDown(KeyCode.N)) gm.NextLevel();
    }

    void OnGUI()
    {
        GUIStyle big = new GUIStyle(GUI.skin.label) { fontSize = 28, alignment = TextAnchor.MiddleCenter };
        GUIStyle normal = new GUIStyle(GUI.skin.label) { fontSize = 18 };

        if (damageFlash > 0f)
        {
            GUI.color = new Color(1f, 0f, 0f, damageFlash * 0.6f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        GUI.Label(new Rect(Screen.width / 2 - 10, Screen.height / 2 - 15, 20, 30), "+", big);

        Weapon w = ws.CurrentWeapon;
        string ammo = w.ammo == AmmoType.None ? "-" : inv.Get(w.ammo) + "/" + inv.Max(w.ammo);
        string keys = "";
        foreach (KeyColor k in inv.Keys) keys += Pickup.KeyName(k) + " ";

        GUI.Label(new Rect(20, Screen.height - 90, 600, 30), "Zdravie: " + Mathf.CeilToInt(hp.health) + "   Brnenie: " + Mathf.CeilToInt(hp.armor), normal);
        GUI.Label(new Rect(20, Screen.height - 60, 600, 30), w.name + "   Munícia: " + ammo + (keys.Length > 0 ? "   Kľúče: " + keys : ""), normal);

        string info = gm.Mode == GameMode.Campaign
            ? GameSession.Current.name + "   Zabití: " + gm.Kills + "/" + gm.TotalEnemies + "   Tajomstvá: " + gm.SecretsFound + "/" + gm.TotalSecrets
            : "Survival   Vlna: " + gm.Wave + "   Zabití: " + gm.Kills;
        GUI.Label(new Rect(20, 20, 800, 30), info + "   Skóre: " + gm.Score + "   Čas: " + gm.PlayTime.ToString("0"), normal);

        if (Time.unscaledTime < messageUntil)
            GUI.Label(new Rect(0, 80, Screen.width, 40), message, big);

        string center = null;
        if (paused) center = "PAUZA (Esc)";
        else if (gm.CurrentState == GameManager.State.Dead) center = "ZOMREL SI   Skóre: " + gm.Score + "\nR = znova";
        else if (gm.CurrentState == GameManager.State.Won) center = "LEVEL HOTOVÝ   Skóre: " + gm.Score + "\nN = ďalší level, R = znova";
        if (center != null)
            GUI.Label(new Rect(0, Screen.height / 2 - 80, Screen.width, 100), center, big);
    }
}
