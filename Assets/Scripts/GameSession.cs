using UnityEngine;

public enum GameMode { Campaign, Survival }

public struct LevelData
{
    public string name;
    public float enemyHealth;      // násobok zdravia nepriateľov
    public float enemyDamage;      // násobok poškodenia nepriateľov
    public float enemySpeed;       // násobok rýchlosti nepriateľov
    public float ammoFactor;       // násobok munície z pickupov
    public int survivalStart;      // počet nepriateľov v 1. vlne (survival)
    public int survivalGrowth;     // o koľko nepriateľov viac v každej ďalšej vlne

    public LevelData(string name, float enemyHealth, float enemyDamage, float enemySpeed,
                     float ammoFactor, int survivalStart, int survivalGrowth)
    {
        this.name = name;
        this.enemyHealth = enemyHealth;
        this.enemyDamage = enemyDamage;
        this.enemySpeed = enemySpeed;
        this.ammoFactor = ammoFactor;
        this.survivalStart = survivalStart;
        this.survivalGrowth = survivalGrowth;
    }
}

public static class GameSession
{
    // Tabuľka levelov: tu sa ladí obtiažnosť. Mapa levelu je v LevelMaps (cyklicky, ak je máp menej).
    public static readonly LevelData[] Levels =
    {
        new LevelData("Level 1", 1.00f, 1.00f, 1.00f, 1.20f, 4, 2),
        new LevelData("Level 2", 1.10f, 1.10f, 1.05f, 1.10f, 5, 2),
        new LevelData("Level 3", 1.25f, 1.20f, 1.10f, 1.00f, 6, 3),
        new LevelData("Level 4", 1.40f, 1.35f, 1.15f, 0.90f, 7, 3),
        new LevelData("Level 5", 1.60f, 1.50f, 1.20f, 0.80f, 8, 4),
    };

    // Marco nastaví z menu pred načítaním scény Game
    public static int CurrentLevel = 1;
    public static GameMode Mode = GameMode.Campaign;

    public static int LevelCount { get { return Levels.Length; } }

    public static LevelData Current
    {
        get { return Levels[Mathf.Clamp(CurrentLevel, 1, Levels.Length) - 1]; }
    }

    // Odomknuté levely (PlayerPrefs, Marco ich môže synchronizovať s Firebase)
    public static int UnlockedLevel
    {
        get { return PlayerPrefs.GetInt("UnlockedLevel", 1); }
        set
        {
            PlayerPrefs.SetInt("UnlockedLevel", Mathf.Clamp(value, 1, Levels.Length));
            PlayerPrefs.Save();
        }
    }

    // Zavolá sa po dokončení levelu v kampani: odomkne ďalší level
    public static void UnlockNext()
    {
        if (CurrentLevel >= UnlockedLevel)
            UnlockedLevel = CurrentLevel + 1;
    }
}
