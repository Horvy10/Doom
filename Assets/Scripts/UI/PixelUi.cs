using System;
using System.Collections.Generic;
using UnityEngine;

// Samo: pixelové UI kreslené cez OnGUI. Vlastný 5×7 font (veľké písmená aj so slovenskou diakritikou),
// kovové panely a menu ovládateľné myšou aj šípkami + Enter. Používa ho Hud aj HlavneMenu.
public static class PixelUi
{
    const int CellW = 6, CellH = 10;   // 5 px znak + 1 px medzera; 2 riadky diakritika, 1 medzera, 7 znak
    static Texture2D atlas, metal;
    static readonly Dictionary<char, int> index = new Dictionary<char, int>();
    static readonly Dictionary<char, string[]> glyphs = new Dictionary<char, string[]>();

    // veľkosť jedného "pixelu" UI na obrazovke (celé číslo, aby font ostal ostrý)
    public static int Unit { get { return Mathf.Max(1, Mathf.Min(Screen.height / 270, Screen.width / 490)); } }

    public static float Width(string s, float px) { return s.Length == 0 ? 0f : (s.Length * CellW - 1) * px; }
    public static float Height(float px) { return CellH * px; }

    public static void Text(string s, float x, float y, float px, Color c, bool shadow = true)
    {
        Init();
        s = s.ToUpperInvariant();
        if (shadow) Raw(s, x + px, y + px, px, new Color(0f, 0f, 0f, c.a * 0.85f));
        Raw(s, x, y, px, c);
    }

    public static void Center(string s, float cx, float y, float px, Color c, bool shadow = true)
    {
        Text(s, Mathf.Round(cx - Width(s, px) * 0.5f), y, px, c, shadow);
    }

    static void Raw(string s, float x, float y, float px, Color c)
    {
        if (Event.current.type != EventType.Repaint) return;
        Color old = GUI.color;
        GUI.color = c;
        float u = 5f / atlas.width;
        foreach (char ch in s)
        {
            if (index.TryGetValue(ch, out int i))
                GUI.DrawTextureWithTexCoords(new Rect(x, y, 5 * px, CellH * px), atlas, new Rect(i * CellW / (float)atlas.width, 0f, u, 1f));
            x += CellW * px;
        }
        GUI.color = old;
    }

    public static void Fill(Rect r, Color c)
    {
        if (Event.current.type != EventType.Repaint) return;
        Color old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
    }

    // kovový panel s vypuklým (raised) alebo vtlačeným okrajom a nitmi v rohoch
    public static void Panel(Rect r, float px, Color tint, bool raised = true)
    {
        if (Event.current.type != EventType.Repaint) return;
        Init();
        Color old = GUI.color;
        GUI.color = tint;
        GUI.DrawTextureWithTexCoords(r, metal, new Rect(0f, 0f, r.width / (32f * px), r.height / (32f * px)));
        GUI.color = old;

        Color light = new Color(1f, 1f, 1f, 0.22f), dark = new Color(0f, 0f, 0f, 0.55f);
        Color top = raised ? light : dark, bottom = raised ? dark : light;
        Fill(new Rect(r.x, r.y, r.width, px), top);
        Fill(new Rect(r.x, r.y, px, r.height), top);
        Fill(new Rect(r.x, r.yMax - px, r.width, px), bottom);
        Fill(new Rect(r.xMax - px, r.y, px, r.height), bottom);
        if (raised && r.width > 12 * px && r.height > 12 * px)
            foreach (Vector2 p in new[] { new Vector2(r.x + 3 * px, r.y + 3 * px), new Vector2(r.xMax - 5 * px, r.y + 3 * px),
                                          new Vector2(r.x + 3 * px, r.yMax - 5 * px), new Vector2(r.xMax - 5 * px, r.yMax - 5 * px) })
            {
                Fill(new Rect(p.x, p.y, 2 * px, 2 * px), new Color(0f, 0f, 0f, 0.5f));
                Fill(new Rect(p.x, p.y, px, px), new Color(1f, 1f, 1f, 0.35f));
            }
    }

    // ───────── font ─────────

    static void Init()
    {
        if (atlas != null) return;
        Define();

        string marks = "ÁÉÍÓÚÝŔĹČĎĚŇŘŠŤŽÄÔÖÜ";
        string bases = "AEIOUYRLCDENRSTZAOOU";
        string kinds = "aaaaaaaacccccccc" + "u^uu";   // a = dĺžeň, c = mäkčeň, u = prehláska, ^ = vokáň
        List<char> all = new List<char>(glyphs.Keys);
        all.AddRange(marks);

        atlas = new Texture2D(all.Count * CellW, CellH, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        Color32[] px = new Color32[atlas.width * atlas.height];
        for (int i = 0; i < all.Count; i++)
        {
            char ch = all[i];
            index[ch] = i;
            int m = marks.IndexOf(ch);
            string[] g = glyphs[m >= 0 ? bases[m] : ch];
            for (int r = 0; r < 7; r++)
                for (int c = 0; c < 5; c++)
                    if (g[r][c] == '#') px[(CellH - 4 - r) * atlas.width + i * CellW + c] = new Color32(255, 255, 255, 255);
            if (m < 0) continue;
            string[] mark = Mark(kinds[m]);
            for (int r = 0; r < 2; r++)
                for (int c = 0; c < 5; c++)
                    if (mark[r][c] == '#') px[(CellH - 1 - r) * atlas.width + i * CellW + c] = new Color32(255, 255, 255, 255);
        }
        atlas.SetPixels32(px);
        atlas.Apply();

        metal = new Texture2D(32, 32, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
        Color[] mp = new Color[32 * 32];
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.21f, y * 0.21f) * 0.6f + Mathf.PerlinNoise(x * 0.7f + 9f, y * 0.7f) * 0.4f;
                float v = 0.2f + n * 0.12f + ((x * 7 + y * 13) % 29 == 0 ? 0.08f : 0f);
                mp[y * 32 + x] = new Color(v, v, v * 1.06f, 1f);
            }
        metal.SetPixels(mp);
        metal.Apply();
    }

    static string[] Mark(char kind)
    {
        switch (kind)
        {
            case 'a': return new[] { "...#.", "..#.." };
            case 'c': return new[] { ".#.#.", "..#.." };
            case '^': return new[] { "..#..", ".#.#." };
            default: return new[] { ".....", ".#.#." };
        }
    }

    static void G(char c, params string[] rows) { glyphs[c] = rows; }

    static void Define()
    {
        G('A', ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#");
        G('B', "####.", "#...#", "#...#", "####.", "#...#", "#...#", "####.");
        G('C', ".###.", "#...#", "#....", "#....", "#....", "#...#", ".###.");
        G('D', "####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####.");
        G('E', "#####", "#....", "#....", "####.", "#....", "#....", "#####");
        G('F', "#####", "#....", "#....", "####.", "#....", "#....", "#....");
        G('G', ".###.", "#...#", "#....", "#.###", "#...#", "#...#", ".####");
        G('H', "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#");
        G('I', ".###.", "..#..", "..#..", "..#..", "..#..", "..#..", ".###.");
        G('J', "..###", "...#.", "...#.", "...#.", "...#.", "#..#.", ".##..");
        G('K', "#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#");
        G('L', "#....", "#....", "#....", "#....", "#....", "#....", "#####");
        G('Ľ', "#..#.", "#..#.", "#....", "#....", "#....", "#....", "#####");
        G('M', "#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#");
        G('N', "#...#", "#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#");
        G('O', ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###.");
        G('P', "####.", "#...#", "#...#", "####.", "#....", "#....", "#....");
        G('Q', ".###.", "#...#", "#...#", "#...#", "#.#.#", "#..#.", ".##.#");
        G('R', "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#");
        G('S', ".####", "#....", "#....", ".###.", "....#", "....#", "####.");
        G('T', "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#..");
        G('U', "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###.");
        G('V', "#...#", "#...#", "#...#", "#...#", "#...#", ".#.#.", "..#..");
        G('W', "#...#", "#...#", "#...#", "#.#.#", "#.#.#", "#.#.#", ".#.#.");
        G('X', "#...#", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "#...#");
        G('Y', "#...#", "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#..");
        G('Z', "#####", "....#", "...#.", "..#..", ".#...", "#....", "#####");
        G('0', ".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###.");
        G('1', "..#..", ".##..", "..#..", "..#..", "..#..", "..#..", ".###.");
        G('2', ".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####");
        G('3', "####.", "....#", "....#", ".###.", "....#", "....#", "####.");
        G('4', "...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#.");
        G('5', "#####", "#....", "####.", "....#", "....#", "#...#", ".###.");
        G('6', ".###.", "#....", "#....", "####.", "#...#", "#...#", ".###.");
        G('7', "#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#...");
        G('8', ".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###.");
        G('9', ".###.", "#...#", "#...#", ".####", "....#", "....#", ".###.");
        G('.', ".....", ".....", ".....", ".....", ".....", ".##..", ".##..");
        G(',', ".....", ".....", ".....", ".....", ".##..", "..#..", ".#...");
        G(':', ".....", ".##..", ".##..", ".....", ".##..", ".##..", ".....");
        G('!', "..#..", "..#..", "..#..", "..#..", "..#..", ".....", "..#..");
        G('?', ".###.", "#...#", "....#", "...#.", "..#..", ".....", "..#..");
        G('-', ".....", ".....", ".....", "#####", ".....", ".....", ".....");
        G('+', ".....", "..#..", "..#..", "#####", "..#..", "..#..", ".....");
        G('/', "....#", "....#", "...#.", "..#..", ".#...", "#....", "#....");
        G('%', "##..#", "##..#", "...#.", "..#..", ".#...", "#..##", "#..##");
        G('(', "...#.", "..#..", ".#...", ".#...", ".#...", "..#..", "...#.");
        G(')', ".#...", "..#..", "...#.", "...#.", "...#.", "..#..", ".#...");
        G('<', "...#.", "..#..", ".#...", "#....", ".#...", "..#..", "...#.");
        G('>', ".#...", "..#..", "...#.", "....#", "...#.", "..#..", ".#...");
        G('\'', "..#..", "..#..", ".#...", ".....", ".....", ".....", ".....");
        G('=', ".....", ".....", "#####", ".....", "#####", ".....", ".....");
        G('_', ".....", ".....", ".....", ".....", ".....", ".....", "#####");
        G('#', ".#.#.", "#####", ".#.#.", ".#.#.", ".#.#.", "#####", ".#.#.");
    }
}

// Zoznam položiek menu: myš (hover + klik) aj klávesnica (šípky, Enter, vľavo/vpravo pre nastavenia).
public class PixelMenu
{
    struct Item { public string label; public Action click; public Action<int> adjust; public bool disabled; }

    public int selected;
    readonly List<Item> items = new List<Item>();
    Vector3 lastMouse;

    public void Begin() { items.Clear(); }

    public void Add(string label, Action click, Action<int> adjust = null, bool disabled = false)
    {
        items.Add(new Item { label = label, click = click, adjust = adjust, disabled = disabled });
    }

    // volaj z Update (len keď je menu viditeľné); položky sú z posledného OnGUI
    public void HandleKeys()
    {
        if (items.Count == 0) return;
        if (Input.GetKeyDown(KeyCode.DownArrow)) Move(1);
        if (Input.GetKeyDown(KeyCode.UpArrow)) Move(-1);
        selected = Mathf.Clamp(selected, 0, items.Count - 1);
        Item it = items[selected];
        if (it.disabled) return;
        if (it.adjust != null && Input.GetKeyDown(KeyCode.LeftArrow)) it.adjust(-1);
        if (it.adjust != null && Input.GetKeyDown(KeyCode.RightArrow)) it.adjust(1);
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
        {
            if (it.click != null) it.click();
            else if (it.adjust != null) it.adjust(1);
        }
    }

    void Move(int dir)
    {
        for (int k = 0; k < items.Count; k++)
        {
            selected = (selected + dir + items.Count) % items.Count;
            if (!items[selected].disabled) return;
        }
    }

    // nakreslí položky pod seba so stredom v cx, začína na y; vráti spodný okraj
    public float Draw(float cx, float y, float px, float width)
    {
        Event e = Event.current;
        bool mouseMoved = (Input.mousePosition - lastMouse).sqrMagnitude > 0.5f;
        if (e.type == EventType.Repaint) lastMouse = Input.mousePosition;
        float h = 14 * px;

        for (int i = 0; i < items.Count; i++)
        {
            Item it = items[i];
            Rect r = new Rect(Mathf.Round(cx - width * 0.5f), y + i * (h + 2 * px), width, h);
            bool hover = r.Contains(e.mousePosition);
            if (hover && mouseMoved && !it.disabled) selected = i;
            bool sel = i == selected && !it.disabled;

            if (sel)
            {
                PixelUi.Panel(r, px, new Color(1.25f, 0.85f, 0.6f));
                PixelUi.Fill(new Rect(r.x, r.y, 2 * px, r.height), new Color(1f, 0.55f, 0.1f));
                bool blink = Mathf.Repeat(Time.unscaledTime * 2f, 1f) < 0.6f;
                if (blink) PixelUi.Text(">", r.x + 5 * px, r.y + 3 * px, px, new Color(1f, 0.8f, 0.3f));
            }
            else PixelUi.Fill(r, new Color(0f, 0f, 0f, 0.35f));

            Color c = it.disabled ? new Color(0.4f, 0.4f, 0.4f) : sel ? new Color(1f, 0.85f, 0.35f) : new Color(0.85f, 0.85f, 0.8f);
            PixelUi.Center(it.label, cx, r.y + 3 * px, px, c);

            if (hover && !it.disabled && e.type == EventType.MouseDown && e.button == 0)
            {
                selected = i;
                if (it.click != null) it.click();
                else if (it.adjust != null) it.adjust(e.mousePosition.x < r.center.x ? -1 : 1);
                e.Use();
            }
        }
        return y + items.Count * (h + 2 * px);
    }
}

// Nastavenia hráča (PlayerPrefs): hlasitosť a citlivosť myši
public static class Nastavenia
{
    public static float Hlasitost
    {
        get { return PlayerPrefs.GetFloat("Volume", 0.6f); }
        set { PlayerPrefs.SetFloat("Volume", Mathf.Clamp01(value)); PlayerPrefs.Save(); Apply(); }
    }

    public static float Citlivost
    {
        get { return PlayerPrefs.GetFloat("Sensitivity", 2f); }
        set { PlayerPrefs.SetFloat("Sensitivity", Mathf.Clamp(value, 0.25f, 6f)); PlayerPrefs.Save(); Apply(); }
    }

    public static void Apply()
    {
        AudioListener.volume = Hlasitost;
        PlayerController pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        if (pc) pc.mouseSensitivity = Citlivost;
    }

    public static string HlasitostText { get { return "HLASITOSŤ  < " + Mathf.RoundToInt(Hlasitost * 100f) + "% >"; } }
    public static string CitlivostText { get { return "CITLIVOSŤ MYŠI  < " + Citlivost.ToString("0.00") + " >"; } }
    public static void ZmenHlasitost(int d) { Hlasitost = Mathf.Round(Hlasitost * 10f + d) / 10f; }
    public static void ZmenCitlivost(int d) { Citlivost += d * 0.25f; }
}
