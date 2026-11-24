using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// Lukáš: vytvorí prefaby (nepriatelia, predmety, dvere, východ, steny) a scénu Game.
// Level sa stavia až pri Play (LevelBuilder z LevelMaps). Spustenie znova prepíše assety na tom istom mieste.
public static class LukasSetup
{
    const string Art = "Assets/Art/Lukas";
    const string PrefabDir = "Assets/Prefabs/Lukas";
    const string ScenePath = "Assets/Scenes/Game.unity";

    [MenuItem("Tools/Lukas/1. Vytvor prefaby a scénu Game")]
    public static void Setup()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Directory.CreateDirectory(Art);
        Directory.CreateDirectory(PrefabDir);
        Directory.CreateDirectory("Assets/Scenes");
        AssetDatabase.Refresh();

        // ── materiály (Samo ich neskôr vymení) ──
        Material wallMat = Mat("Wall", new Color(0.36f, 0.30f, 0.27f));
        Material floorMat = Mat("Floor", new Color(0.22f, 0.21f, 0.22f));
        Material doorMat = Mat("Door", new Color(0.55f, 0.55f, 0.6f));
        Material gunMat = Mat("Gun", new Color(0.15f, 0.15f, 0.17f));
        Material zombieMat = Mat("Zombie", new Color(0.35f, 0.5f, 0.3f));
        Material impMat = Mat("Imp", new Color(0.5f, 0.3f, 0.2f));
        Material demonMat = Mat("Demon", new Color(0.85f, 0.45f, 0.55f));
        Material eyeMat = Mat("EnemyEye", new Color(1f, 0.2f, 0.1f), true);
        Material fireMat = Mat("Fireball", new Color(1f, 0.5f, 0.1f), true);
        Material exitMat = Mat("Exit", new Color(0.2f, 1f, 0.3f), true);
        Material healthMat = Mat("Health", new Color(0.9f, 0.9f, 0.9f));
        Material armorMat = Mat("Armor", new Color(0.2f, 0.75f, 0.3f));
        Material bulletsMat = Mat("Bullets", new Color(0.8f, 0.65f, 0.2f));
        Material shellsMat = Mat("Shells", new Color(0.75f, 0.25f, 0.15f));
        Material weaponMat = Mat("WeaponPickup", new Color(0.3f, 0.3f, 0.35f));

        // ── prostredie ──
        GameObject wall = SavePrefab(Prim(PrimitiveType.Cube, "Wall", wallMat, null, Vector3.zero, Vector3.one, true));
        GameObject floor = SavePrefab(Prim(PrimitiveType.Cube, "Floor", floorMat, null, Vector3.zero, Vector3.one, true));

        GameObject doorGo = Prim(PrimitiveType.Cube, "Door", doorMat, null, Vector3.zero, Vector3.one, true);
        doorGo.AddComponent<Door>();
        NavMeshObstacle obstacle = doorGo.AddComponent<NavMeshObstacle>();
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.size = Vector3.one;
        obstacle.carving = true;
        Door door = SavePrefab(doorGo).GetComponent<Door>();

        GameObject exitGo = new GameObject("Exit");
        BoxCollider exitCol = exitGo.AddComponent<BoxCollider>();
        exitCol.isTrigger = true;
        exitCol.center = new Vector3(0f, 1f, 0f);
        exitCol.size = new Vector3(3f, 2f, 3f);
        exitGo.AddComponent<LevelExit>();
        Prim(PrimitiveType.Cube, "Pad", exitMat, exitGo.transform, new Vector3(0f, 0.03f, 0f), new Vector3(3.5f, 0.05f, 3.5f), false);
        AddLight(exitGo.transform, new Vector3(0f, 1.5f, 0f), new Color(0.3f, 1f, 0.4f), 8f, 2f);
        GameObject exit = SavePrefab(exitGo);

        // ── nepriatelia ──
        GameObject fireGo = Prim(PrimitiveType.Sphere, "Fireball", fireMat, null, Vector3.zero, Vector3.one * 0.35f, true);
        fireGo.GetComponent<Collider>().isTrigger = true;
        fireGo.AddComponent<Rigidbody>().isKinematic = true;
        fireGo.AddComponent<Projectile>();
        AddLight(fireGo.transform, Vector3.zero, new Color(1f, 0.6f, 0.2f), 4f, 2f);
        Projectile fireball = SavePrefab(fireGo).GetComponent<Projectile>();

        Enemy zombie = MakeEnemy("Enemy_Zombie", EnemyKind.Zombie, zombieMat, eyeMat, 1f, null);
        Enemy imp = MakeEnemy("Enemy_Imp", EnemyKind.Imp, impMat, eyeMat, 1f, fireball);
        Enemy demon = MakeEnemy("Enemy_Demon", EnemyKind.Demon, demonMat, eyeMat, 1.2f, null);

        // ── predmety ──
        Pickup health = MakePickup("Pickup_Health", PickupType.Health, 25, healthMat, new Vector3(0.6f, 0.4f, 0.6f));
        Pickup armor = MakePickup("Pickup_Armor", PickupType.Armor, 50, armorMat, new Vector3(0.7f, 0.5f, 0.3f));
        Pickup bullets = MakePickup("Pickup_Bullets", PickupType.Bullets, 20, bulletsMat, new Vector3(0.5f, 0.3f, 0.3f));
        Pickup shells = MakePickup("Pickup_Shells", PickupType.Shells, 8, shellsMat, new Vector3(0.5f, 0.3f, 0.3f));
        Pickup keyRed = MakeKey("Pickup_KeyRed", KeyColor.Red);
        Pickup keyBlue = MakeKey("Pickup_KeyBlue", KeyColor.Blue);
        Pickup keyYellow = MakeKey("Pickup_KeyYellow", KeyColor.Yellow);
        Pickup shotgun = MakePickup("Pickup_Shotgun", PickupType.Weapon, 0, weaponMat, new Vector3(0.2f, 0.2f, 1.1f));
        shotgun = SetWeapon(shotgun, 2);
        Pickup chaingun = MakePickup("Pickup_Chaingun", PickupType.Weapon, 0, weaponMat, new Vector3(0.35f, 0.35f, 1f));
        chaingun = SetWeapon(chaingun, 3);

        // ── scéna Game ──
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Light sun = new GameObject("Directional Light").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 0.7f;
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.28f, 0.24f, 0.24f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.02f;
        RenderSettings.fogColor = new Color(0.1f, 0.06f, 0.05f);

        new GameObject("GameManager").AddComponent<GameManager>();

        GameObject player = new GameObject("Player");
        player.tag = "Player";
        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.4f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        GameObject pivot = new GameObject("CameraPivot");
        pivot.tag = "MainCamera";
        pivot.transform.SetParent(player.transform, false);
        pivot.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        Camera cam = pivot.AddComponent<Camera>();
        cam.nearClipPlane = 0.05f;
        cam.fieldOfView = 75f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = RenderSettings.fogColor;
        pivot.AddComponent<AudioListener>();
        player.AddComponent<PlayerController>().cameraPivot = pivot.transform;
        player.AddComponent<PlayerHealth>();
        player.AddComponent<PlayerInventory>();
        WeaponSystem ws = player.AddComponent<WeaponSystem>();
        ws.cam = cam;
        ws.viewModelMaterial = gunMat;

        SurvivalSpawner spawner = new GameObject("SurvivalSpawner").AddComponent<SurvivalSpawner>();
        spawner.enemyPrefabs = new[] { zombie, imp, demon };

        LevelBuilder b = new GameObject("Level").AddComponent<LevelBuilder>();
        NavMeshSurface surface = b.GetComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        b.wallPrefab = wall;
        b.floorPrefab = floor;
        b.doorPrefab = door;
        b.exitPrefab = exit;
        b.zombiePrefab = zombie;
        b.impPrefab = imp;
        b.demonPrefab = demon;
        b.healthPrefab = health;
        b.armorPrefab = armor;
        b.bulletsPrefab = bullets;
        b.shellsPrefab = shells;
        b.keyRedPrefab = keyRed;
        b.keyBluePrefab = keyBlue;
        b.keyYellowPrefab = keyYellow;
        b.shotgunPrefab = shotgun;
        b.chaingunPrefab = chaingun;
        b.player = player.transform;
        b.spawner = spawner;

        new GameObject("DebugHUD").AddComponent<DebugHUD>();

        EditorSceneManager.SaveScene(scene, ScenePath);

        var list = EditorBuildSettings.scenes.ToList();
        if (!list.Any(s => s.path == ScenePath)) list.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = list.ToArray();

        AssetDatabase.SaveAssets();
        Debug.Log("Hotovo: prefaby v " + PrefabDir + ", scéna " + ScenePath + " (pridaná do Build Settings). Stlač Play.");
    }

    // ───────────────────────── pomocné ─────────────────────────

    static Material Mat(string name, Color color, bool emissive = false)
    {
        string path = Art + "/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetColor("_BaseColor", color);
        if (emissive)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", color * 2f);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    static GameObject Prim(PrimitiveType type, string name, Material mat, Transform parent, Vector3 pos, Vector3 scale, bool keepCollider)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = mat;
        if (parent) go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        return go;
    }

    static void AddLight(Transform parent, Vector3 pos, Color color, float range, float intensity)
    {
        Light l = new GameObject("Light").AddComponent<Light>();
        l.transform.SetParent(parent, false);
        l.transform.localPosition = pos;
        l.type = LightType.Point;
        l.color = color;
        l.range = range;
        l.intensity = intensity;
    }

    static GameObject SavePrefab(GameObject go)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "/" + go.name + ".prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    static Enemy MakeEnemy(string name, EnemyKind kind, Material body, Material eyeMat, float s, Projectile projectile)
    {
        GameObject root = new GameObject(name);
        CapsuleCollider col = root.AddComponent<CapsuleCollider>();
        col.height = 2f * s;
        col.radius = 0.4f * s;
        col.center = new Vector3(0f, s, 0f);

        NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
        agent.radius = 0.4f * s;
        agent.height = 2f * s;
        agent.angularSpeed = 360f;
        agent.acceleration = 12f;

        Prim(PrimitiveType.Capsule, "Body", body, root.transform, new Vector3(0f, s, 0f), new Vector3(0.8f, 1f, 0.8f) * s, false);
        GameObject eye = Prim(PrimitiveType.Sphere, "Eye", eyeMat, root.transform, new Vector3(0f, 1.6f * s, 0.3f * s), Vector3.one * 0.25f * s, false);

        Enemy e = root.AddComponent<Enemy>();
        e.kind = kind;
        e.eye = eye.transform;
        switch (kind)
        {
            case EnemyKind.Zombie:
                e.health = 40f; e.speed = 3f; e.attackRange = 20f; e.attackDamage = 6f;
                e.attackCooldown = 1.4f; e.accuracy = 0.55f; e.scoreValue = 100;
                break;
            case EnemyKind.Imp:
                e.health = 70f; e.speed = 3.5f; e.attackRange = 25f; e.attackDamage = 12f;
                e.attackCooldown = 2f; e.projectilePrefab = projectile; e.scoreValue = 200;
                break;
            case EnemyKind.Demon:
                e.health = 150f; e.speed = 6f; e.attackRange = 2f; e.attackDamage = 20f;
                e.attackCooldown = 1f; e.scoreValue = 300;
                break;
        }
        return SavePrefab(root).GetComponent<Enemy>();
    }

    static Pickup MakePickup(string name, PickupType type, int amount, Material mat, Vector3 size)
    {
        GameObject root = new GameObject(name);
        BoxCollider col = root.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = Vector3.one * 1.2f;
        Prim(PrimitiveType.Cube, "Model", mat, root.transform, Vector3.zero, size, false);
        Pickup p = root.AddComponent<Pickup>();
        p.type = type;
        p.amount = amount;
        return SavePrefab(root).GetComponent<Pickup>();
    }

    static Pickup MakeKey(string name, KeyColor key)
    {
        Material m = Mat("Key" + key, LevelBuilder.KeyTint(key), true);
        GameObject root = new GameObject(name);
        BoxCollider col = root.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = Vector3.one * 1.2f;
        Prim(PrimitiveType.Cube, "Model", m, root.transform, Vector3.zero, new Vector3(0.15f, 0.5f, 0.3f), false);
        AddLight(root.transform, Vector3.zero, LevelBuilder.KeyTint(key), 3f, 1.5f);
        Pickup p = root.AddComponent<Pickup>();
        p.type = PickupType.Key;
        p.key = key;
        return SavePrefab(root).GetComponent<Pickup>();
    }

    static Pickup SetWeapon(Pickup prefab, int index)
    {
        string path = AssetDatabase.GetAssetPath(prefab);
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        root.GetComponent<Pickup>().weaponIndex = index;
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        return AssetDatabase.LoadAssetAtPath<Pickup>(path);
    }
}
