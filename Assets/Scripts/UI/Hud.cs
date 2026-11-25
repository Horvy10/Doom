using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Samo: HUD v hre (nahrádza DebugHUD). Spodný stavový panel (munícia, zdravie, EKG monitor, brnenie, zbrane,
// kľúče, zásoby), správy pri zbieraní, inventár (Tab), pauza (Esc), obrazovka smrti a výhry.
public class Hud : MonoBehaviour
{
    static readonly Color Orange = new Color(1f, 0.55f, 0.15f);
    static readonly Color Yellow = new Color(1f, 0.85f, 0.3f);
    static readonly Color Grey = new Color(0.55f, 0.55f, 0.52f);
    static readonly Color Dim = new Color(0.28f, 0.28f, 0.27f);

    class Message { public string text; public Color color; public float until; }

    PlayerHealth hp;
    PlayerInventory inv;
    WeaponSystem ws;
    GameManager gm;
    SurvivalSpawner spawner;
    readonly PixelMenu menu = new PixelMenu();
    readonly List<Message> messages = new List<Message>();
    bool paused, inventory;
    float damageFlash, pickupFlash, unlockAt = -1f, ecgPhase;
    string bigText;
    float bigUntil;
    Color bigColor;

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
        Nastavenia.Apply();
        Big(gm.Mode == GameMode.Campaign ? GameSession.Current.name : "SURVIVAL", Orange, 2.5f);
    }

    void OnDestroy()
    {
        if (hp) hp.Damaged -= OnDamaged;
        Door.NeedKey -= OnNeedKey;
        Pickup.Collected -= OnCollected;
        Secret.Found -= OnSecret;
        if (spawner) spawner.WaveStarted -= OnWave;
    }

    // ───────── udalosti ─────────

    void OnDamaged(float amount) { damageFlash = Mathf.Min(0.7f, damageFlash + 0.25f + amount * 0.01f); }
    void OnNeedKey(KeyColor k) { Say("POTREBUJEŠ " + Pickup.KeyName(k) + " KĽÚČ", new Color(1f, 0.35f, 0.25f)); }
    void OnSecret(Secret s) { Big("TAJNÉ MIESTO!", Yellow, 2f); }
    void OnWave(int wave) { Big("VLNA " + wave, Orange, 2f); }

    void OnCollected(Pickup p)
    {
        pickupFlash = p.type == PickupType.Weapon ? 0.35f : 0.18f;
        switch (p.type)
        {
            case PickupType.Weapon:
                string[] names = { "PÄSŤ", "PIŠTOĽ", "BROKOVNICU", "GUĽOMET", "RAKETOMET" };
                Say("ZÍSKAL SI " + (p.weaponIndex < names.Length ? names[p.weaponIndex] : "ZBRAŇ") + "!", Yellow);
                break;
            case PickupType.Key: Say("ZÍSKAL SI " + Pickup.KeyName(p.key) + " KĽÚČ", LevelBuilder.KeyTint(p.key) * 1.4f); break;
            case PickupType.Health: Say("ZOBRAL SI LEKÁRNIČKU", new Color(0.6f, 1f, 0.6f)); break;
            case PickupType.Armor: Say("ZOBRAL SI BRNENIE", new Color(0.5f, 0.95f, 1f)); break;
            case PickupType.Bullets: Say("ZOBRAL SI NÁBOJE", Grey * 1.5f); break;
            case PickupType.Shells: Say("ZOBRAL SI NÁBOJE DO BROKOVNICE", Grey * 1.5f); break;
            case PickupType.Rockets: Say("ZOBRAL SI RAKETY", Grey * 1.5f); break;
        }
    }

    void Say(string text, Color c)
    {
        messages.Add(new Message { text = text, color = c, until = Time.unscaledTime + 2.5f });
        if (messages.Count > 3) messages.RemoveAt(0);
    }

    void Big(string text, Color c, float seconds)
    {
        bigText = text;
        bigColor = c;
        bigUntil = Time.unscaledTime + seconds;
    }

    // ───────── ovládanie ─────────

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        damageFlash = Mathf.MoveTowards(damageFlash, 0f, dt);
        pickupFlash = Mathf.MoveTowards(pickupFlash, 0f, dt);
        ecgPhase += dt * (1f + (1f - Mathf.Clamp01(hp.health / 100f)) * 1.6f) * (hp.IsDead ? 0f : 1f);

        if (unlockAt > 0f && Time.unscaledTime >= unlockAt) { PlayerController.InputLocked = false; unlockAt = -1f; }

        bool playing = gm.CurrentState == GameManager.State.Playing;
        if (playing && Input.GetKeyDown(KeyCode.Escape)) { if (paused) Resume(); else Pause(); }
        inventory = playing && !paused && Input.GetKey(KeyCode.Tab);
        if (paused || !playing) menu.HandleKeys();
    }

    void Pause()
    {
        paused = true;
        menu.selected = 0;
        Time.timeScale = 0f;
        PlayerController.InputLocked = true;
        PlayerController.LockCursor(false);
    }

    void Resume()
    {
        paused = false;
        Time.timeScale = 1f;
        PlayerController.LockCursor(true);
        unlockAt = Time.unscaledTime + 0.2f;   // klik na "Pokračovať" nesmie vystreliť
    }

    static void ToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu");
    }

    // ───────── kreslenie ─────────

    void OnGUI()
    {
        int u = PixelUi.Unit;
        float W = Screen.width, H = Screen.height;

        if (damageFlash > 0f) PixelUi.Fill(new Rect(0, 0, W, H), new Color(0.8f, 0f, 0f, damageFlash * 0.5f));
        if (pickupFlash > 0f) PixelUi.Fill(new Rect(0, 0, W, H), new Color(1f, 0.85f, 0.3f, pickupFlash * 0.5f));

        bool playing = gm.CurrentState == GameManager.State.Playing;
        if (playing && !paused) Crosshair(W * 0.5f, (H - 36 * u) * 0.5f + 18 * u, u);

        TopInfo(u, W);
        for (int i = 0; i < messages.Count; i++)
        {
            Message m = messages[i];
            float left = m.until - Time.unscaledTime;
            if (left <= 0f) { messages.RemoveAt(i--); continue; }
            Color c = m.color;
            c.a = Mathf.Clamp01(left / 0.5f);
            PixelUi.Center(m.text, W * 0.5f, 16 * u + i * 11 * u, u, c);
        }
        if (bigText != null && Time.unscaledTime < bigUntil)
            PixelUi.Center(bigText, W * 0.5f, H * 0.28f, 3 * u, bigColor);

        StatusBar(u, W, H);

        if (inventory) Inventory(u, W, H);
        if (paused) PauseMenu(u, W, H);
        else if (gm.CurrentState == GameManager.State.Dead) DeathScreen(u, W, H);
        else if (gm.CurrentState == GameManager.State.Won) WinScreen(u, W, H);
    }

    static void Crosshair(float cx, float cy, int u)
    {
        Color c = new Color(1f, 0.9f, 0.6f, 0.85f);
        PixelUi.Fill(new Rect(cx - 4 * u, cy - u * 0.5f, 2 * u, u), c);
        PixelUi.Fill(new Rect(cx + 2 * u, cy - u * 0.5f, 2 * u, u), c);
        PixelUi.Fill(new Rect(cx - u * 0.5f, cy - 4 * u, u, 2 * u), c);
        PixelUi.Fill(new Rect(cx - u * 0.5f, cy + 2 * u, u, 2 * u), c);
    }

    void TopInfo(int u, float W)
    {
        string left = gm.Mode == GameMode.Campaign
            ? GameSession.Current.name + "   ZABITÍ " + gm.Kills + "/" + gm.TotalEnemies + "   TAJNÉ " + gm.SecretsFound + "/" + gm.TotalSecrets
            : "SURVIVAL   VLNA " + gm.Wave + "   ZABITÍ " + gm.Kills;
        PixelUi.Text(left, 4 * u, 3 * u, u, Grey * 1.4f);
        string right = "SKÓRE " + gm.Score + "   ČAS " + Cas(gm.PlayTime);
        PixelUi.Text(right, W - PixelUi.Width(right, u) - 4 * u, 3 * u, u, Grey * 1.4f);
    }

    static string Cas(float s) { return Mathf.FloorToInt(s / 60f) + ":" + Mathf.FloorToInt(s % 60f).ToString("00"); }

    // ───────── stavový panel ─────────

    void StatusBar(int u, float W, float H)
    {
        float barH = 36 * u, y0 = H - barH;
        PixelUi.Panel(new Rect(0, y0, W, barH), u, new Color(0.95f, 0.85f, 0.75f));
        PixelUi.Fill(new Rect(0, y0 - u, W, u), new Color(0f, 0f, 0f, 0.6f));

        int[] widths = { 58, 70, 64, 70, 50, 16, 96 };
        float total = 0f;
        foreach (int w in widths) total += w * u;
        total += (widths.Length - 1) * 4 * u;
        float x = Mathf.Round((W - total) * 0.5f), y = y0 + 3 * u;

        for (int i = 0; i < widths.Length; i++)
        {
            Rect r = new Rect(x, y, widths[i] * u, 30 * u);
            PixelUi.Panel(r, u, new Color(0.45f, 0.42f, 0.4f), false);
            switch (i)
            {
                case 0: Ammo(r, u); break;
                case 1: BigValue(r, u, Mathf.CeilToInt(hp.health) + "%", HealthColor(), "ZDRAVIE"); break;
                case 2: Ecg(r, u); break;
                case 3: BigValue(r, u, Mathf.CeilToInt(hp.armor) + "%", new Color(0.4f, 0.9f, 1f), "BRNENIE"); break;
                case 4: Arms(r, u); break;
                case 5: Keys(r, u); break;
                case 6: Stock(r, u); break;
            }
            x += (widths[i] + 4) * u;
        }
    }

    Color HealthColor()
    {
        if (hp.health > 50f) return Orange;
        if (hp.health > 25f) return Yellow;
        return Mathf.Repeat(Time.unscaledTime * 3f, 1f) < 0.5f ? new Color(1f, 0.15f, 0.1f) : new Color(0.6f, 0.05f, 0.05f);
    }

    static void BigValue(Rect r, int u, string value, Color c, string label)
    {
        PixelUi.Center(value, r.center.x, r.y - 3 * u, 2 * u, c);
        PixelUi.Center(label, r.center.x, r.y + 19 * u, u, Grey);
    }

    void Ammo(Rect r, int u)
    {
        Weapon w = ws.CurrentWeapon;
        string v = w.ammo == AmmoType.None ? "--" : inv.Get(w.ammo).ToString();
        Color c = w.ammo != AmmoType.None && inv.Get(w.ammo) == 0 ? new Color(1f, 0.15f, 0.1f) : Orange;
        BigValue(r, u, v, c, "MUNÍCIA");
    }

    // EKG monitor: rýchlosť a farba podľa zdravia
    void Ecg(Rect r, int u)
    {
        Rect screen = new Rect(r.x + 3 * u, r.y + 3 * u, r.width - 6 * u, 16 * u);
        PixelUi.Fill(screen, new Color(0.02f, 0.07f, 0.04f));
        for (float gx = screen.x + 4 * u; gx < screen.xMax; gx += 6 * u) PixelUi.Fill(new Rect(gx, screen.y, u, screen.height), new Color(0.1f, 0.3f, 0.15f, 0.3f));

        float h01 = Mathf.Clamp01(hp.health / 100f);
        Color c = hp.IsDead ? new Color(1f, 0.15f, 0.1f) : h01 > 0.5f ? new Color(0.3f, 1f, 0.4f) : h01 > 0.25f ? Yellow : new Color(1f, 0.25f, 0.15f);
        int cols = Mathf.FloorToInt(screen.width / u);
        float mid = screen.y + screen.height * 0.5f;
        for (int i = 0; i < cols; i++)
        {
            float p = Mathf.Repeat(ecgPhase - i / (float)cols * 2f, 1f);
            float v = hp.IsDead ? 0f : Beat(p);
            Color cc = c;
            cc.a = 1f - i / (float)cols * 0.7f;
            PixelUi.Fill(new Rect(screen.xMax - (i + 1) * u, Mathf.Round(mid - v * 6f * u), u, u), cc);
        }
        string state = hp.IsDead ? "MŔTVY" : h01 > 0.5f ? "OK" : h01 > 0.25f ? "ZRANENÝ" : "KRITICKÝ";
        PixelUi.Center(state, r.center.x, r.y + 19 * u, u, c);
    }

    static float Beat(float p)
    {
        if (p < 0.1f) return 0f;
        if (p < 0.18f) return Mathf.Sin((p - 0.1f) / 0.08f * Mathf.PI) * 0.15f;
        if (p < 0.26f) return 0f;
        if (p < 0.29f) return -0.25f;
        if (p < 0.33f) return 1f;
        if (p < 0.36f) return -0.45f;
        if (p < 0.5f) return 0f;
        if (p < 0.64f) return Mathf.Sin((p - 0.5f) / 0.14f * Mathf.PI) * 0.25f;
        return 0f;
    }

    void Arms(Rect r, int u)
    {
        for (int i = 0; i < ws.weapons.Count && i < 6; i++)
        {
            float x = r.x + 6 * u + (i % 3) * 14 * u, y = r.y + 1 * u + (i / 3) * 9 * u;
            bool owned = ws.weapons[i].owned, cur = i == ws.Current;
            if (cur) PixelUi.Fill(new Rect(x - 2 * u, y + 2 * u, 9 * u, 9 * u), new Color(1f, 0.55f, 0.1f, 0.35f));
            PixelUi.Text((i + 1).ToString(), x, y, u, cur ? Color.white : owned ? Yellow : Dim, owned);
        }
        PixelUi.Center("ZBRANE", r.center.x, r.y + 19 * u, u, Grey);
    }

    void Keys(Rect r, int u)
    {
        KeyColor[] keys = { KeyColor.Blue, KeyColor.Yellow, KeyColor.Red };
        for (int i = 0; i < 3; i++)
        {
            Rect k = new Rect(r.x + 3 * u, r.y + 3 * u + i * 9 * u, r.width - 6 * u, 6 * u);
            if (inv.HasKey(keys[i]))
            {
                PixelUi.Fill(k, LevelBuilder.KeyTint(keys[i]) * 1.3f);
                PixelUi.Fill(new Rect(k.x, k.y, k.width, u), new Color(1f, 1f, 1f, 0.5f));
            }
            else PixelUi.Fill(k, new Color(0f, 0f, 0f, 0.4f));
        }
    }

    void Stock(Rect r, int u)
    {
        (AmmoType type, string name)[] rows = { (AmmoType.Bullets, "NÁBOJE"), (AmmoType.Shells, "BROKY"), (AmmoType.Rockets, "RAKETY") };
        for (int i = 0; i < rows.Length; i++)
        {
            bool cur = ws.CurrentWeapon.ammo == rows[i].type;
            Color c = cur ? Yellow : Grey;
            float y = r.y + 1 * u + i * 9.5f * u;
            PixelUi.Text(rows[i].name, r.x + 4 * u, y, u, c);
            string v = inv.Get(rows[i].type) + "/" + inv.Max(rows[i].type);
            PixelUi.Text(v, r.xMax - 4 * u - PixelUi.Width(v, u), y, u, c);
        }
    }

    // ───────── inventár a menu ─────────

    void Inventory(int u, float W, float H)
    {
        Rect p = new Rect(Mathf.Round(W * 0.5f - 140 * u), Mathf.Round((H - 36 * u) * 0.5f - 72 * u), 280 * u, 144 * u);
        PixelUi.Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 0.35f));
        PixelUi.Panel(p, u, new Color(0.9f, 0.85f, 0.8f));
        PixelUi.Center("INVENTÁR", p.center.x, p.y + 6 * u, 2 * u, Orange);

        string[] ammoNames = { "", "NÁBOJE", "BROKY", "RAKETY" };
        float y = p.y + 30 * u;
        for (int i = 0; i < ws.weapons.Count; i++)
        {
            Weapon w = ws.weapons[i];
            Color c = !w.owned ? Dim : i == ws.Current ? Color.white : Yellow;
            PixelUi.Text((i + 1) + "  " + w.name, p.x + 12 * u, y, u, c, w.owned);
            string info = !w.owned ? "NEMÁŠ" : w.ammo == AmmoType.None ? "BEZ MUNÍCIE" : inv.Get(w.ammo) + " " + ammoNames[(int)w.ammo];
            PixelUi.Text(info, p.xMax - 12 * u - PixelUi.Width(info, u), y, u, c, w.owned);
            y += 12 * u;
        }

        y += 4 * u;
        PixelUi.Text("KĽÚČE", p.x + 12 * u, y, u, Grey);
        float kx = p.x + 50 * u;
        foreach (KeyColor k in new[] { KeyColor.Red, KeyColor.Blue, KeyColor.Yellow })
        {
            PixelUi.Fill(new Rect(kx, y + 2 * u, 14 * u, 7 * u), inv.HasKey(k) ? LevelBuilder.KeyTint(k) * 1.3f : new Color(0f, 0f, 0f, 0.4f));
            kx += 18 * u;
        }
        string stats = "ZDRAVIE " + Mathf.CeilToInt(hp.health) + "   BRNENIE " + Mathf.CeilToInt(hp.armor);
        PixelUi.Text(stats, p.xMax - 12 * u - PixelUi.Width(stats, u), y, u, Grey * 1.3f);
        PixelUi.Center("PUSTI TAB", p.center.x, p.yMax - 13 * u, u, Dim * 1.6f);
    }

    void Overlay(int u, float W, float H, string title, Color titleColor, Color tint, string[] lines)
    {
        PixelUi.Fill(new Rect(0, 0, W, H), tint);
        float y = H * 0.18f;
        PixelUi.Center(title, W * 0.5f, y, 4 * u, titleColor);
        y += 46 * u;
        foreach (string l in lines) { PixelUi.Center(l, W * 0.5f, y, u, Grey * 1.5f); y += 11 * u; }
        menu.Draw(W * 0.5f, y + 8 * u, u, 170 * u);
    }

    void PauseMenu(int u, float W, float H)
    {
        menu.Begin();
        menu.Add("POKRAČOVAŤ", Resume);
        menu.Add(Nastavenia.HlasitostText, null, Nastavenia.ZmenHlasitost);
        menu.Add(Nastavenia.CitlivostText, null, Nastavenia.ZmenCitlivost);
        menu.Add("ZAČAŤ ZNOVA", gm.Restart);
        menu.Add("HLAVNÉ MENU", ToMenu);
        Overlay(u, W, H, "PAUZA", Orange, new Color(0f, 0f, 0f, 0.6f), new string[0]);
    }

    void DeathScreen(int u, float W, float H)
    {
        menu.Begin();
        menu.Add("ZNOVA", gm.Restart);
        menu.Add("HLAVNÉ MENU", ToMenu);
        string[] lines = gm.Mode == GameMode.Survival
            ? new[] { "VLNA " + gm.Wave + "   ZABITÍ " + gm.Kills, "SKÓRE " + gm.Score }
            : new[] { "ZABITÍ " + gm.Kills + "/" + gm.TotalEnemies, "SKÓRE " + gm.Score };
        Overlay(u, W, H, "ZOMREL SI", new Color(1f, 0.15f, 0.1f), new Color(0.25f, 0f, 0f, 0.55f), lines);
    }

    void WinScreen(int u, float W, float H)
    {
        menu.Begin();
        if (GameSession.CurrentLevel < GameSession.LevelCount) menu.Add("ĎALŠÍ LEVEL", gm.NextLevel);
        menu.Add("ZNOVA", gm.Restart);
        menu.Add("HLAVNÉ MENU", ToMenu);
        string[] lines =
        {
            "ZABITÍ " + gm.Kills + "/" + gm.TotalEnemies + "   TAJNÉ " + gm.SecretsFound + "/" + gm.TotalSecrets,
            "ČAS " + Cas(gm.PlayTime) + "   SKÓRE " + gm.Score,
        };
        Overlay(u, W, H, "LEVEL HOTOVÝ", new Color(0.4f, 1f, 0.5f), new Color(0f, 0.08f, 0.03f, 0.6f), lines);
    }
}
