using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Survival: vlny nepriateľov na spawn bodoch ('x' v mape). Každá vlna je väčšia,
// silnejšie typy prichádzajú neskôr. Po vlne hráč dostane muníciu a trochu zdravia.
public class SurvivalSpawner : MonoBehaviour
{
    public Enemy[] enemyPrefabs;               // poradie: od najslabšieho
    public List<Vector3> spawnPoints = new List<Vector3>();   // vyplní LevelBuilder
    public float firstWaveDelay = 2f;
    public float timeBetweenWaves = 4f;
    public float spawnDelay = 0.5f;

    public event Action<int> WaveStarted;

    void Start()
    {
        if (GameSession.Mode != GameMode.Survival || enemyPrefabs == null || enemyPrefabs.Length == 0 || spawnPoints.Count == 0)
        {
            enabled = false;
            return;
        }
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        GameManager gm = GameManager.Instance;
        PlayerHealth hp = PlayerHealth.Instance;
        PlayerInventory inv = hp.GetComponent<PlayerInventory>();

        yield return new WaitForSeconds(firstWaveDelay);

        while (gm.CurrentState == GameManager.State.Playing)
        {
            gm.Wave++;
            WaveStarted?.Invoke(gm.Wave);

            LevelData lv = GameSession.Current;
            int count = lv.survivalStart + lv.survivalGrowth * (gm.Wave - 1);
            for (int i = 0; i < count; i++)
            {
                if (gm.CurrentState != GameManager.State.Playing) yield break;
                Spawn(gm.Wave);
                yield return new WaitForSeconds(spawnDelay);
            }

            while (AliveCount() > 0) yield return null;
            if (gm.CurrentState != GameManager.State.Playing) yield break;

            inv.AddAmmo(AmmoType.Bullets, 40);
            inv.AddAmmo(AmmoType.Shells, 10);
            hp.Heal(20f);

            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    static int AliveCount()
    {
        int n = 0;
        foreach (Enemy e in Enemy.All) if (!e.IsDead) n++;
        return n;
    }

    void Spawn(int wave)
    {
        // vlna 1: len prvý typ, vlna 2-3: dva typy, od vlny 4 všetky
        int kinds = Mathf.Clamp(1 + wave / 2, 1, enemyPrefabs.Length);
        Enemy prefab = enemyPrefabs[UnityEngine.Random.Range(0, kinds)];
        Vector3 p = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)]
                    + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, UnityEngine.Random.Range(-1f, 1f));
        Enemy e = Instantiate(prefab, p, Quaternion.identity);
        e.Alert();
    }
}
