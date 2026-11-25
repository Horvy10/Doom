using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Samo: hlavné menu (scéna Menu). V pozadí je aréna (LevelBuilder so survival mapou) a kamera okolo nej krúži.
// Kampaň (výber levelu), survival, rebríček z Firebase, nastavenia (hlasitosť, citlivosť, meno), koniec.
[DefaultExecutionOrder(-300)]   // pred LevelBuilderom: pozadie je aréna
public class HlavneMenu : MonoBehaviour
{
    public const string Title = "BUNKER";          // názov hry v menu
    public Camera cam;
    public Enemy[] decor;                           // nepriatelia, ktorí stoja v aréne (bez hráča len stoja)
    public Vector3 center = new Vector3(32f, 0f, -24f);
    public float radius = 11f;

    enum Screen { Main, Levels, Board, Settings }
    static readonly Color Orange = new Color(1f, 0.55f, 0.15f);
    static readonly Color Grey = new Color(0.7f, 0.7f, 0.66f);

    Screen screen = Screen.Main;
    readonly PixelMenu menu = new PixelMenu();
    float angle;
    string boardMode = FirebaseService.Campaign;
    List<ScoreEntry> board;
    bool boardLoading, boardFailed;
    int nameItem = -1;

    void Awake()
    {
        GameSession.Mode = GameMode.Survival;
        Time.timeScale = 1f;
        PlayerController.InputLocked = false;
        PlayerController.LockCursor(false);
    }

    void Start()
    {
        Nastavenia.Apply();
        for (int i = 0; i < decor.Length; i++)
        {
            if (decor[i] == null) continue;
            float a = i * Mathf.PI * 2f / decor.Length + 0.4f;
            Vector3 pos = center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 3.2f;
            Instantiate(decor[i], pos, Quaternion.LookRotation(pos - center));
        }
    }

    void Update()
    {
        angle += Time.deltaTime * 0.12f;
        if (cam)
        {
            cam.transform.position = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius + Vector3.up * 2.3f;
            cam.transform.LookAt(center + Vector3.up * 1.3f);
        }

        menu.HandleKeys();
        if (Input.GetKeyDown(KeyCode.Escape) && screen != Screen.Main) Go(Screen.Main);

        // písanie mena v nastaveniach
        if (screen == Screen.Settings && menu.selected == nameItem)
        {
            string name = FirebaseService.PlayerName;
            foreach (char c in Input.inputString)
            {
                if (c == '\b') { if (name.Length > 0) name = name.Substring(0, name.Length - 1); }
                else if (!char.IsControl(c) && name.Length < 12) name += c;
            }
            if (name != FirebaseService.PlayerName) FirebaseService.PlayerName = name;
        }
    }

    void Go(Screen s)
    {
        screen = s;
        menu.selected = 0;
        if (s == Screen.Board) LoadBoard();
    }

    void LoadBoard()
    {
        board = null;
        boardLoading = true;
        boardFailed = false;
        string mode = boardMode;
        FirebaseService.LoadTop10(mode, list =>
        {
            if (this == null || mode != boardMode) return;
            boardLoading = false;
            board = list;
            boardFailed = list == null;
        });
    }

    static void Play(GameMode mode, int level)
    {
        GameSession.Mode = mode;
        GameSession.CurrentLevel = level;
        SceneManager.LoadScene("Game");
    }

    void OnGUI()
    {
        int u = PixelUi.Unit;
        float W = UnityEngine.Screen.width, H = UnityEngine.Screen.height;

        // stmavenie okrajov, aby text vynikol
        PixelUi.Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 0.25f));
        PixelUi.Fill(new Rect(W * 0.5f - 110 * u, 0, 220 * u, H), new Color(0f, 0f, 0f, 0.3f));

        string sub = "RETRO FPS";
        float y = 96 * u;
        float width = 180 * u;

        menu.Begin();
        nameItem = -1;
        switch (screen)
        {
            case Screen.Main:
                menu.Add("KAMPAŇ", () => Go(Screen.Levels));
                menu.Add("SURVIVAL", () => Play(GameMode.Survival, 1));
                menu.Add("REBRÍČEK", () => Go(Screen.Board));
                menu.Add("NASTAVENIA", () => Go(Screen.Settings));
                menu.Add("ODÍSŤ", Application.Quit);
                break;

            case Screen.Levels:
                sub = "VYBER LEVEL";
                for (int i = 1; i <= GameSession.LevelCount; i++)
                {
                    int level = i;
                    bool locked = i > GameSession.UnlockedLevel;
                    menu.Add(GameSession.Levels[i - 1].name + (locked ? "  (ZAMKNUTÝ)" : ""), () => Play(GameMode.Campaign, level), null, locked);
                }
                menu.Add("SPÄŤ", () => Go(Screen.Main));
                break;

            case Screen.Settings:
                menu.Add(Nastavenia.HlasitostText, null, Nastavenia.ZmenHlasitost);
                menu.Add(Nastavenia.CitlivostText, null, Nastavenia.ZmenCitlivost);
                bool blink = Mathf.Repeat(Time.unscaledTime * 2f, 1f) < 0.5f;
                nameItem = 2;
                menu.Add("MENO: " + FirebaseService.PlayerName + (menu.selected == nameItem && blink ? "_" : " "), null);
                menu.Add("SPÄŤ", () => Go(Screen.Main));
                sub = menu.selected == nameItem ? "PÍŠ MENO, BACKSPACE MAŽE" : "NASTAVENIA";
                break;

            case Screen.Board:
                sub = "REBRÍČEK - TOP 10";
                y = Board(W, y, u);
                menu.Add("REŽIM: " + (boardMode == FirebaseService.Campaign ? "KAMPAŇ" : "SURVIVAL"), () =>
                {
                    boardMode = boardMode == FirebaseService.Campaign ? FirebaseService.Survival : FirebaseService.Campaign;
                    LoadBoard();
                });
                menu.Add("SPÄŤ", () => Go(Screen.Main));
                break;
        }
        menu.Draw(W * 0.5f, y, u, width);
        TitleText(W * 0.5f, 18 * u, u, sub);

        PixelUi.Center("ŠÍPKY + ENTER ALEBO MYŠ", W * 0.5f, H - 14 * u, u, new Color(0.45f, 0.45f, 0.42f));
    }

    static void TitleText(float cx, float y, int u, string sub)
    {
        int px = 6 * u;
        float x = Mathf.Round(cx - PixelUi.Width(Title, px) * 0.5f);
        float flicker = Mathf.PerlinNoise(Time.unscaledTime * 3f, 0f) > 0.2f ? 1f : 0.6f;
        PixelUi.Text(Title, x + u * 2, y + u * 2, px, new Color(0.3f, 0.02f, 0f), false);
        PixelUi.Text(Title, x, y, px, new Color(0.85f, 0.25f, 0.05f) * flicker);
        // horná polovica písmen svetlejšia (dvojfarebný kov)
        GUI.BeginGroup(new Rect(0, y, UnityEngine.Screen.width, 6.5f * px));
        PixelUi.Text(Title, x, 0, px, new Color(1f, 0.6f, 0.15f) * flicker, false);
        GUI.EndGroup();
        PixelUi.Center(sub, cx, y + 10 * px + 2 * u, u, new Color(0.75f, 0.7f, 0.6f));
    }

    float Board(float W, float y, int u)
    {
        Rect p = new Rect(Mathf.Round(W * 0.5f - 150 * u), y, 300 * u, 125 * u);
        PixelUi.Panel(p, u, new Color(0.85f, 0.8f, 0.75f));
        float ty = p.y + 6 * u;
        PixelUi.Text("#  MENO", p.x + 8 * u, ty, u, Orange);
        string head = boardMode == FirebaseService.Campaign ? "LEVEL   SKÓRE" : "VLNA   SKÓRE";
        PixelUi.Text(head, p.xMax - 8 * u - PixelUi.Width(head, u), ty, u, Orange);
        ty += 13 * u;

        if (boardLoading) PixelUi.Center("NAČÍTAVAM...", p.center.x, ty + 30 * u, u, Grey);
        else if (boardFailed) PixelUi.Center("REBRÍČEK NIE JE DOSTUPNÝ", p.center.x, ty + 30 * u, u, new Color(1f, 0.4f, 0.3f));
        else if (board != null && board.Count == 0) PixelUi.Center("ZATIAĽ PRÁZDNY", p.center.x, ty + 30 * u, u, Grey);
        else if (board != null)
            for (int i = 0; i < board.Count; i++)
            {
                ScoreEntry e = board[i];
                Color c = i == 0 ? new Color(1f, 0.85f, 0.3f) : Grey;
                string name = e.name.Length > 12 ? e.name.Substring(0, 12) : e.name;
                PixelUi.Text((i + 1) + ". " + name, p.x + 8 * u, ty, u, c);
                string right = e.level + "   " + e.score;
                PixelUi.Text(right, p.xMax - 8 * u - PixelUi.Width(right, u), ty, u, c);
                ty += 10 * u;
            }
        return p.yMax + 6 * u;
    }
}
