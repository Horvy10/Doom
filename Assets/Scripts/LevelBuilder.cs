using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

// Postaví level z textovej mapy (LevelMaps) a upečie NavMesh.
// Kampaň: mapa podľa GameSession.CurrentLevel, survival: LevelMaps.Arena.
// Prefaby stien/podlahy/stropu sú kocky 1×1×1, builder ich naškáluje na bunku. Samo ich môže vymeniť.
[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(NavMeshSurface))]
public class LevelBuilder : MonoBehaviour
{
    public float cell = 4f;
    public float wallHeight = 4f;

    [Header("Prostredie (Samo)")]
    public GameObject wallPrefab;
    public GameObject floorPrefab;
    public GameObject ceilingPrefab;           // voliteľné

    [Header("Objekty")]
    public Door doorPrefab;
    public GameObject exitPrefab;
    public Enemy zombiePrefab, impPrefab, demonPrefab;
    public Pickup healthPrefab, armorPrefab, bulletsPrefab, shellsPrefab;
    public Pickup keyRedPrefab, keyBluePrefab, keyYellowPrefab;
    public Pickup shotgunPrefab, chaingunPrefab;

    [Header("Scéna")]
    public Transform player;
    public SurvivalSpawner spawner;

    public readonly List<Vector3> SpawnPoints = new List<Vector3>();

    Transform geometry;   // steny a podlaha: z nich sa pečie NavMesh
    Transform objects;    // nepriatelia, predmety, dvere: mimo NavMeshu

    void Awake()
    {
        string[] map = GameSession.Mode == GameMode.Survival ? LevelMaps.Arena : LevelMaps.Campaign(GameSession.CurrentLevel);
        Build(map);

        NavMeshSurface surface = GetComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.BuildNavMesh();

        if (spawner) spawner.spawnPoints = SpawnPoints;
    }

    void Build(string[] map)
    {
        geometry = new GameObject("Geometry").transform;
        geometry.SetParent(transform, false);
        objects = new GameObject("Objects").transform;

        for (int r = 0; r < map.Length; r++)
        {
            for (int c = 0; c < map[r].Length; c++)
            {
                char ch = map[r][c];
                if (ch == ' ') continue;
                Vector3 pos = new Vector3(c * cell, 0f, -r * cell);

                if (ch == '#')
                {
                    Place(wallPrefab, pos + Vector3.up * wallHeight * 0.5f, new Vector3(cell, wallHeight, cell), geometry);
                    continue;
                }

                Place(floorPrefab, pos + Vector3.down * 0.1f, new Vector3(cell, 0.2f, cell), geometry);
                if (ceilingPrefab)
                    Place(ceilingPrefab, pos + Vector3.up * (wallHeight + 0.1f), new Vector3(cell, 0.2f, cell), geometry);

                Vector3 item = pos + Vector3.up * 0.5f;
                switch (ch)
                {
                    case 'P': MovePlayer(pos); break;
                    case 'z': Spawn(zombiePrefab, pos); break;
                    case 'i': Spawn(impPrefab, pos); break;
                    case 'd': Spawn(demonPrefab, pos); break;
                    case 'h': Spawn(healthPrefab, item); break;
                    case 'a': Spawn(armorPrefab, item); break;
                    case 'b': Spawn(bulletsPrefab, item); break;
                    case 's': Spawn(shellsPrefab, item); break;
                    case 'S': Spawn(shotgunPrefab, item); break;
                    case 'C': Spawn(chaingunPrefab, item); break;
                    case 'R': Spawn(keyRedPrefab, item); break;
                    case 'U': Spawn(keyBluePrefab, item); break;
                    case 'Y': Spawn(keyYellowPrefab, item); break;
                    case 'D': MakeDoor(map, r, c, pos, KeyColor.None); break;
                    case 'r': MakeDoor(map, r, c, pos, KeyColor.Red); break;
                    case 'u': MakeDoor(map, r, c, pos, KeyColor.Blue); break;
                    case 'y': MakeDoor(map, r, c, pos, KeyColor.Yellow); break;
                    case 'E': Spawn(exitPrefab, pos); break;
                    case 'x': SpawnPoints.Add(pos); break;
                }
            }
        }
    }

    static bool IsWall(string[] map, int r, int c)
    {
        return r >= 0 && r < map.Length && c >= 0 && c < map[r].Length && map[r][c] == '#';
    }

    void Place(GameObject prefab, Vector3 pos, Vector3 size, Transform parent)
    {
        if (prefab == null) return;
        GameObject go = Instantiate(prefab, pos, Quaternion.identity, parent);
        go.transform.localScale = size;
        go.isStatic = true;
    }

    void Spawn(Object prefab, Vector3 pos)
    {
        if (prefab == null) return;
        Object o = Instantiate(prefab, pos, Quaternion.identity, objects);
        // nepriatelia sa otočia náhodne, aby nestáli všetci rovnako
        if (o is Enemy e) e.transform.rotation = Quaternion.Euler(0f, Random.Range(0, 4) * 90f, 0f);
    }

    void MakeDoor(string[] map, int r, int c, Vector3 pos, KeyColor key)
    {
        if (doorPrefab == null) return;
        bool spansX = IsWall(map, r, c - 1) && IsWall(map, r, c + 1);   // steny vľavo a vpravo
        Vector3 size = spansX ? new Vector3(cell, wallHeight, 0.4f) : new Vector3(0.4f, wallHeight, cell);

        Door d = Instantiate(doorPrefab, pos + Vector3.up * wallHeight * 0.5f, Quaternion.identity, objects);
        d.transform.localScale = size;
        d.requiredKey = key;

        if (key != KeyColor.None)
        {
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            mpb.SetColor("_BaseColor", KeyTint(key));
            foreach (Renderer rend in d.GetComponentsInChildren<Renderer>()) rend.SetPropertyBlock(mpb);
        }
    }

    public static Color KeyTint(KeyColor key)
    {
        switch (key)
        {
            case KeyColor.Red: return new Color(0.8f, 0.15f, 0.12f);
            case KeyColor.Blue: return new Color(0.15f, 0.3f, 0.85f);
            case KeyColor.Yellow: return new Color(0.9f, 0.8f, 0.15f);
        }
        return Color.white;
    }

    void MovePlayer(Vector3 pos)
    {
        if (player == null) return;
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc) cc.enabled = false;
        player.position = pos;
        if (cc) cc.enabled = true;
    }
}
