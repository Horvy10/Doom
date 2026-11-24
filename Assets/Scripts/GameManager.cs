using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// Stav hry: zabitia, skóre, čas, výhra/smrť, uloženie skóre do Firebase.
// Kampaň: vyhráš vstupom do východu. Survival: vlny (SurvivalSpawner), hra končí smrťou.
[DefaultExecutionOrder(-200)]
public class GameManager : MonoBehaviour
{
    public enum State { Playing, Won, Dead }

    [Header("Test bez menu")]
    public bool overrideSession;
    public GameMode testMode = GameMode.Campaign;
    public int testLevel = 1;

    public static GameManager Instance { get; private set; }

    public State CurrentState { get; private set; }
    public GameMode Mode { get { return GameSession.Mode; } }
    public int Kills { get; private set; }
    public int TotalEnemies { get; private set; }
    public int Score { get; private set; }
    public float PlayTime { get; private set; }
    public int Wave { get; set; }              // nastavuje SurvivalSpawner

    public event Action Won;
    public event Action Lost;

    // bonus za čas v kampani: 5 bodov za každú sekundu pod 5 minút
    int TimeBonus { get { return Mathf.Max(0, Mathf.RoundToInt((300f - PlayTime) * 5f)); } }

    void Awake()
    {
        Instance = this;
        if (overrideSession)
        {
            GameSession.Mode = testMode;
            GameSession.CurrentLevel = testLevel;
        }
        Time.timeScale = 1f;
        PlayerController.InputLocked = false;
    }

    void Start()
    {
        Enemy.Killed += OnKilled;
        if (PlayerHealth.Instance) PlayerHealth.Instance.Died += OnDied;
        TotalEnemies = Mode == GameMode.Campaign ? Enemy.All.Count : 0;
        PlayerController.LockCursor(true);
    }

    void OnDestroy()
    {
        Enemy.Killed -= OnKilled;
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (CurrentState == State.Playing) PlayTime += Time.deltaTime;
    }

    void OnKilled(Enemy e)
    {
        if (CurrentState != State.Playing) return;
        Kills++;
        Score += e.scoreValue;
    }

    public void Win()
    {
        if (CurrentState != State.Playing || Mode != GameMode.Campaign) return;
        CurrentState = State.Won;
        Score += TimeBonus;
        GameSession.UnlockNext();
        Save();
        EndGame();
        Won?.Invoke();
    }

    void OnDied()
    {
        if (CurrentState != State.Playing) return;
        CurrentState = State.Dead;
        if (Mode == GameMode.Survival) Save();
        EndGame();
        Lost?.Invoke();
    }

    void EndGame()
    {
        PlayerController.InputLocked = true;
        PlayerController.LockCursor(false);
    }

    // kampaň: level = číslo levelu; survival: level = dosiahnutá vlna
    void Save()
    {
        string mode = Mode == GameMode.Campaign ? FirebaseService.Campaign : FirebaseService.Survival;
        int level = Mode == GameMode.Campaign ? GameSession.CurrentLevel : Wave;
        FirebaseService.SaveScore(mode, Score, Kills, PlayTime, level);
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void NextLevel()
    {
        if (GameSession.CurrentLevel < GameSession.LevelCount) GameSession.CurrentLevel++;
        Restart();
    }
}
