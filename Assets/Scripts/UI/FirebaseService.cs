using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;

public class ScoreEntry
{
    public string name;
    public int score;      // body (viac = lepšie)
    public int kills;
    public float time;
    public int level;      // kampaň: číslo levelu, survival: dosiahnutá vlna
    public string date;
}

// Ukladanie skóre do Firebase Realtime Database: doom/scores/campaign/... a doom/scores/survival/...
// Prevzaté z Car Race Master; Firebase projekt môže byť ten istý, dáta sú pod vlastným uzlom "doom".
public static class FirebaseService
{
    public const string Campaign = "campaign";
    public const string Survival = "survival";

    static Task<DependencyStatus> init;

    // V projekte je vypnutý reload domény, statické polia treba pri každom Play vynulovať
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { init = null; }

    public static string PlayerName
    {
        get { return PlayerPrefs.GetString("PlayerName", "Hráč"); }
        set { PlayerPrefs.SetString("PlayerName", value); PlayerPrefs.Save(); }
    }

    static void WhenReady(Action action, Action fail)
    {
        if (init == null) init = FirebaseApp.CheckAndFixDependenciesAsync();

        init.ContinueWithOnMainThread(t =>
        {
            if (!t.IsFaulted && t.Result == DependencyStatus.Available)
                action();
            else
            {
                Debug.LogError("Firebase nie je dostupné: " + (t.IsFaulted ? t.Exception.GetBaseException().Message : t.Result.ToString()));
                fail();
            }
        });
    }

    static DatabaseReference Scores(string mode)
    {
        return FirebaseDatabase.DefaultInstance.GetReference("doom").Child("scores").Child(mode);
    }

    public static void SaveScore(string mode, int score, int kills, float time, int level)
    {
        Dictionary<string, object> data = new Dictionary<string, object>
        {
            { "name", PlayerName },
            { "score", score },
            { "kills", kills },
            { "time", Math.Round(time, 2) },
            { "level", level },
            { "date", DateTime.Now.ToString("yyyy-MM-dd HH:mm") },
        };

        WhenReady(() =>
        {
            Scores(mode).Push().SetValueAsync(data).ContinueWithOnMainThread(t =>
            {
                if (t.IsFaulted) Debug.LogError("Uloženie skóre zlyhalo: " + t.Exception.GetBaseException().Message);
                else Debug.Log("Skóre uložené do Firebase: " + mode + " " + score);
            });
        }, () => { });
    }

    // done dostane null, ak sa načítanie nepodarí
    public static void LoadTop10(string mode, Action<List<ScoreEntry>> done)
    {
        WhenReady(() =>
        {
            Scores(mode).OrderByChild("score").LimitToLast(10).GetValueAsync().ContinueWithOnMainThread(t =>
            {
                if (t.IsFaulted)
                {
                    Debug.LogError("Načítanie rebríčka zlyhalo: " + t.Exception.GetBaseException().Message);
                    done(null);
                    return;
                }
                done(t.Result.Children.Select(Parse).OrderByDescending(e => e.score).ToList());
            });
        }, () => done(null));
    }

    static ScoreEntry Parse(DataSnapshot s)
    {
        return new ScoreEntry
        {
            name = s.Child("name").Value as string ?? "?",
            score = Convert.ToInt32(s.Child("score").Value),
            kills = Convert.ToInt32(s.Child("kills").Value),
            time = Convert.ToSingle(s.Child("time").Value),
            level = Convert.ToInt32(s.Child("level").Value),
            date = s.Child("date").Value as string ?? "",
        };
    }
}
