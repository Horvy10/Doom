using System;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

// Samo: grafika, prostredie, efekty a zvuky. Všetko sa generuje kódom (pixelové textúry, modely z primitív,
// častice, syntetizované zvuky), takže v repe nie sú cudzie assety.
// Spúšťaj PO Tools > Lukas > 1. (Lukáš prefaby a scénu pri svojom spustení prepíše, potom treba spustiť znova).
// Opakované spustenie prepíše vlastné assety na tom istom mieste.
public static class SamoSetup
{
    const string Art = "Assets/Art/Samo";
    const string TexDir = "Assets/Art/Samo/Textures";
    const string AudioDir = "Assets/Audio/Samo";
    const string PrefabDir = "Assets/Prefabs/Samo";
    const string LukasPrefabs = "Assets/Prefabs/Lukas";
    const string ScenePath = "Assets/Scenes/Game.unity";
    const string MenuPath = "Assets/Scenes/Menu.unity";

    // materiály pre modely (nastaví Setup)
    static Material gunMat, woodMat, armorMat, clothMat, gloveMat, fleshMat, impMat, demonMat, boneMat,
                    eyeYellow, eyeRed, fireCore, rocketPaint, ammoMat, brassMat, flashMat, blobAdd, blobAlpha, shadowMat;

    [MenuItem("Tools/Samo/Grafika, efekty a zvuky")]
    public static void Setup()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!File.Exists(ScenePath)) { Debug.LogError("Najprv spusti Tools > Lukas > 1. (chýba " + ScenePath + ")"); return; }

        foreach (string d in new[] { TexDir, AudioDir, PrefabDir }) Directory.CreateDirectory(d);
        AssetDatabase.Refresh();

        // ── textúry ──
        Texture2D concrete = Tex("Wall_Concrete", 128, (x, y) => Concrete(x, y, 1));
        Texture2D panel = Tex("Wall_Panel", 128, (x, y) => Panel(x, y, false));
        Texture2D panelGlow = Tex("Wall_Panel_Emission", 128, (x, y) => Panel(x, y, true));
        Texture2D pipes = Tex("Wall_Pipes", 128, Pipes);
        Texture2D plate = Tex("Floor_Plate", 128, Plate);
        Texture2D grate = Tex("Floor_Grate", 128, Grate);
        Texture2D ceiling = Tex("Ceiling", 128, Ceiling);
        Texture2D lamp = Tex("Lamp", 32, Lamp);
        Texture2D door = Tex("Door", 128, Door);
        Texture2D barrel = Tex("Barrel", 64, Barrel);
        Texture2D flesh = Tex("Flesh", 64, Flesh);
        Texture2D cloth = Tex("Cloth", 64, Cloth);
        Texture2D imp = Tex("ImpHide", 64, (x, y) => ImpHide(x, y, false));
        Texture2D impGlow = Tex("ImpHide_Emission", 64, (x, y) => ImpHide(x, y, true));
        Texture2D demon = Tex("DemonHide", 64, DemonHide);
        Texture2D armor = Tex("Armor", 64, Armor);
        Texture2D gun = Tex("GunMetal", 64, GunMetal);
        Texture2D wood = Tex("Wood", 64, Wood);
        Texture2D medkit = Tex("Medkit", 64, Medkit);
        Texture2D ammo = Tex("AmmoBox", 64, AmmoBox);
        Texture2D shells = Tex("ShellBox", 64, ShellBox);
        Texture2D exit = Tex("Exit", 64, Exit);
        Texture2D blob = Tex("Fx_Blob", 16, Blob, true);
        Texture2D star = Tex("Fx_Flash", 32, Star, true);

        // ── materiály ──
        Material concreteM = Lit("Wall_Concrete", concrete, Color.white);
        Material panelM = Lit("Wall_Panel", panel, Color.white, 0.3f, panelGlow, new Color(0.3f, 0.9f, 0.85f) * 2.5f);
        Material pipesM = Lit("Wall_Pipes", pipes, Color.white);
        Material plateM = Lit("Floor_Plate", plate, Color.white, 0.25f);
        Material grateM = Lit("Floor_Grate", grate, Color.white, 0.2f);
        Material ceilingM = Lit("Ceiling", ceiling, Color.white);
        Material lampWarm = Lit("Lamp_Warm", lamp, Color.white, 0.1f, lamp, new Color(1f, 0.8f, 0.55f) * 3f);
        Material lampCold = Lit("Lamp_Cold", lamp, Color.white, 0.1f, lamp, new Color(0.35f, 0.85f, 1f) * 3f);
        Material doorM = Lit("Door", door, Color.white, 0.3f);
        Material barrelM = Lit("Barrel", barrel, Color.white, 0.2f);
        Material exitM = Lit("Exit", exit, Color.white, 0.1f, exit, new Color(0.4f, 1f, 0.6f) * 2.5f);
        Material medkitM = Lit("Medkit", medkit, Color.white);
        Material shellsM = Lit("ShellBox", shells, Color.white);
        gunMat = Lit("GunMetal", gun, Color.white, 0.4f);
        woodMat = Lit("Wood", wood, Color.white);
        armorMat = Lit("Armor", armor, Color.white, 0.3f);
        clothMat = Lit("Cloth", cloth, Color.white);
        gloveMat = Lit("Glove", cloth, new Color(0.55f, 0.4f, 0.35f));
        fleshMat = Lit("Flesh", flesh, Color.white);
        impMat = Lit("ImpHide", imp, Color.white, 0.15f, impGlow, new Color(1f, 0.45f, 0.1f) * 1.6f);
        demonMat = Lit("DemonHide", demon, Color.white, 0.25f);
        boneMat = Lit("Bone", flesh, new Color(1.6f, 1.55f, 1.4f));
        eyeYellow = Lit("Eye_Yellow", null, new Color(1f, 0.9f, 0.3f), 0.5f, null, new Color(1f, 0.85f, 0.2f) * 4f);
        eyeRed = Lit("Eye_Red", null, new Color(1f, 0.3f, 0.1f), 0.5f, null, new Color(1f, 0.25f, 0.05f) * 4f);
        fireCore = Lit("FireCore", null, new Color(1f, 0.6f, 0.2f), 0f, null, new Color(1f, 0.55f, 0.15f) * 5f);
        rocketPaint = Lit("RocketPaint", armor, new Color(0.75f, 0.85f, 0.6f), 0.3f);
        ammoMat = Lit("AmmoBox", ammo, Color.white);
        brassMat = Lit("Brass", gun, new Color(3.5f, 2.8f, 1.3f), 0.6f);
        Material armorVest = Lit("ArmorVest", armor, new Color(0.6f, 1.2f, 1.1f), 0.4f);
        Material keyCard = Lit("KeyCard", gun, new Color(2.5f, 2.5f, 2.6f), 0.4f);

        blobAdd = Particle("Fx_Additive", blob, true);
        blobAlpha = Particle("Fx_Alpha", blob, false);
        flashMat = Particle("Fx_Flash", star, true);
        shadowMat = Particle("Fx_Shadow", blob, false);
        shadowMat.SetColor("_BaseColor", new Color(0f, 0f, 0f, 0.6f));

        Material[] wallVariants = { concreteM, concreteM, concreteM, panelM, pipesM };
        Material[] floorVariants = { plateM, plateM, plateM, grateM };

        // ── prostredie: vlastné prefaby stien, podlahy a stropu ──
        GameObject wallPf = SaveCube("Samo_Wall", concreteM, wallVariants);
        GameObject floorPf = SaveCube("Samo_Floor", plateM, floorVariants);
        GameObject ceilingPf = SaveCeiling(ceilingM, lampWarm, lampCold);

        // ── častice ──
        GameObject sparks = FxSparks();
        GameObject blood = FxBlood("FX_Blood", 12, 0.3f, 0.6f, 1.5f, 4f, 0.06f, 0.14f);
        GameObject bloodBig = FxBlood("FX_BloodBig", 35, 0.5f, 0.9f, 2f, 6f, 0.1f, 0.25f);
        GameObject explosion = FxExplosion();
        GameObject fireImpact = FxFireImpact();
        GameObject muzzle = FxMuzzle();

        // ── Lukášove prefaby: nové modely ako deti (collidery a skripty ostávajú) ──
        Edit("Enemy_Zombie", r => DressEnemy(r, BuildZombie, 32f, 3.6f, 0.05f, 1.0f));
        Edit("Enemy_Imp", r => DressEnemy(r, BuildImp, 38f, 3.4f, 0.06f, 0.9f));
        Edit("Enemy_Demon", r => DressEnemy(r, BuildDemon, 28f, 2.4f, 0.04f, 1.8f));
        Edit("Enemy_Heavy", r => DressEnemy(r, BuildHeavy, 20f, 3.0f, 0.07f, 1.3f));

        DressPickup("Pickup_Health", m =>
        {
            P(m, PrimitiveType.Cube, medkitM, 0, 0, 0, 0.55f, 0.38f, 0.38f);
            P(m, PrimitiveType.Cube, gunMat, 0, 0.21f, 0, 0.22f, 0.05f, 0.06f);
        });
        DressPickup("Pickup_Armor", m =>
        {
            P(m, PrimitiveType.Cube, armorVest, 0, 0, 0, 0.6f, 0.62f, 0.2f);
            P(m, PrimitiveType.Cube, armorVest, -0.2f, 0.36f, 0, 0.18f, 0.12f, 0.2f);
            P(m, PrimitiveType.Cube, armorVest, 0.2f, 0.36f, 0, 0.18f, 0.12f, 0.2f);
            P(m, PrimitiveType.Cube, eyeYellow, 0, 0.05f, 0.1f, 0.12f, 0.12f, 0.03f);
        });
        DressPickup("Pickup_Bullets", m =>
        {
            P(m, PrimitiveType.Cube, ammoMat, 0, 0, 0, 0.45f, 0.28f, 0.28f);
            for (int i = -1; i <= 1; i++) P(m, PrimitiveType.Cylinder, brassMat, i * 0.1f, 0.18f, 0, 0.05f, 0.05f, 0.05f);
        });
        DressPickup("Pickup_Shells", m => P(m, PrimitiveType.Cube, shellsM, 0, 0, 0, 0.42f, 0.24f, 0.3f));
        DressPickup("Pickup_Rockets", m =>
        {
            for (int i = -1; i <= 1; i += 2)
            {
                P(m, PrimitiveType.Cylinder, rocketPaint, i * 0.08f, 0, 0, 0.12f, 0.25f, 0.12f, 90, 0, 0);
                P(m, PrimitiveType.Sphere, eyeRed, i * 0.08f, 0, 0.25f, 0.12f, 0.12f, 0.14f);
            }
        });
        foreach (string key in new[] { "Red", "Blue", "Yellow" })
        {
            Material stripe = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Lukas/Key" + key + ".mat");
            DressPickup("Pickup_Key" + key, m =>
            {
                P(m, PrimitiveType.Cube, keyCard, 0, 0, 0, 0.32f, 0.45f, 0.04f);
                P(m, PrimitiveType.Cube, stripe ? stripe : eyeRed, 0, 0.1f, 0, 0.33f, 0.1f, 0.05f);
                P(m, PrimitiveType.Cube, gunMat, 0, -0.12f, 0, 0.2f, 0.08f, 0.05f);
            });
        }
        DressPickup("Pickup_Shotgun", m => WeaponPickup(m, 2));
        DressPickup("Pickup_Chaingun", m => WeaponPickup(m, 3));
        DressPickup("Pickup_RocketLauncher", m => WeaponPickup(m, 4));

        DressPickup("Barrel", m =>
        {
            P(m, PrimitiveType.Cylinder, barrelM, 0, 0.6f, 0, 0.8f, 0.6f, 0.8f);
            P(m, PrimitiveType.Cylinder, gunMat, 0, 1.2f, 0, 0.72f, 0.02f, 0.72f);
            P(m, PrimitiveType.Cylinder, fireCore, 0, 1.22f, 0, 0.45f, 0.01f, 0.45f);
        });

        Edit("Door", r => r.GetComponent<Renderer>().sharedMaterial = doorM);
        Edit("SecretDoor", r =>
        {
            r.GetComponent<Renderer>().sharedMaterial = concreteM;
            Get<Varianta>(r).materials = wallVariants;   // vyzerá ako susedné steny
        });
        Edit("Exit", r => r.transform.Find("Pad").GetComponent<Renderer>().sharedMaterial = exitM);

        Edit("Rocket", r =>
        {
            r.GetComponent<Renderer>().enabled = false;
            Kill(r.transform.Find("Model"));
            Transform m = Group(r.transform, "Model", Vector3.zero);
            m.localScale = Vector3.one * 4f;   // koreň má mierku 0.25
            P(m, PrimitiveType.Cylinder, rocketPaint, 0, 0, 0, 0.1f, 0.22f, 0.1f, 90, 0, 0);
            P(m, PrimitiveType.Sphere, eyeRed, 0, 0, 0.22f, 0.1f, 0.1f, 0.14f);
            P(m, PrimitiveType.Sphere, fireCore, 0, 0, -0.24f, 0.09f, 0.09f, 0.09f);
            Trail(m, new Vector3(0, 0, -0.26f));
        });
        Edit("Fireball", r =>
        {
            r.GetComponent<Renderer>().sharedMaterial = fireCore;
            Kill(r.transform.Find("Flames"));
            Flames(r.transform);
        });

        // ── efekty a zvuky: prefab, ktorý majú obe scény ──
        GameObject fxGo = new GameObject("Samo_Efekty");
        Efekty fx = fxGo.AddComponent<Efekty>();
        fx.sparks = sparks;
        fx.blood = blood;
        fx.bloodBig = bloodBig;
        fx.explosion = explosion;
        fx.fireImpact = fireImpact;
        fx.muzzle = muzzle;
        fx.weaponClips = new[]
        {
            Sfx("Fist", 0.3f, Fist), Sfx("Pistol", 0.35f, Pistol), Sfx("Shotgun", 0.75f, Shotgun),
            Sfx("Chaingun", 0.18f, Chaingun), Sfx("RocketLaunch", 0.9f, RocketLaunch),
        };
        fx.explosionClip = Sfx("Explosion", 1.8f, Boom);
        fx.impactClip = Sfx("Impact", 0.35f, Impact);
        fx.enemyShot = Sfx("EnemyShot", 0.3f, EnemyShot);
        fx.fireballClip = Sfx("Fireball", 0.6f, Fireball);
        fx.biteClip = Sfx("Bite", 0.5f, Bite);
        fx.enemyHurt = Sfx("EnemyHurt", 0.3f, EnemyHurt);
        fx.enemyDeath = Sfx("EnemyDeath", 1f, EnemyDeath);
        fx.doorClip = Sfx("Door", 1.1f, DoorSound);
        fx.deniedClip = Sfx("Denied", 0.35f, Denied);
        fx.pickupClip = Sfx("Pickup", 0.16f, PickupSound);
        fx.weaponPickupClip = Sfx("WeaponPickup", 0.5f, WeaponPickupSound);
        fx.secretClip = Sfx("Secret", 1f, SecretSound);
        fx.playerHurt = Sfx("PlayerHurt", 0.3f, PlayerHurt);
        fx.playerDeath = Sfx("PlayerDeath", 1.3f, PlayerDeath);
        fx.ambience = Sfx("Ambience", 8f, Ambience, true);
        GameObject fxPf = SavePrefab(fxGo);

        GameObject volGo = new GameObject("Samo_PostFX");
        Volume vol = volGo.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.sharedProfile = PostFx();
        GameObject postPf = SavePrefab(volGo);

        // ── scéna Game ──
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        LevelBuilder b = Object.FindAnyObjectByType<LevelBuilder>();
        b.wallPrefab = wallPf;
        b.floorPrefab = floorPf;
        b.ceilingPrefab = ceilingPf;
        EditorUtility.SetDirty(b);

        WeaponSystem ws = Object.FindAnyObjectByType<WeaponSystem>();
        ws.viewModelMaterial = gunMat;
        Transform pivot = ws.cam.transform;
        Kill(pivot.Find("Samo_ViewModels"));
        Transform holder = Group(pivot, "Samo_ViewModels", Vector3.zero);
        ws.viewModels = new GameObject[5];
        for (int i = 0; i < 5; i++)
        {
            ws.viewModels[i] = BuildWeapon(i, holder);
            ws.viewModels[i].SetActive(false);
        }
        EditorUtility.SetDirty(ws);

        foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l.type == LightType.Directional))
        {
            l.intensity = 0.25f;
            l.color = new Color(0.6f, 0.7f, 0.9f);
            l.shadows = LightShadows.None;
        }
        Atmosphere(ws.cam);

        foreach (string old in new[] { "Efekty", "Samo_Efekty", "Samo_PostFX", "Hud", "DebugHUD" })
            Kill(GameObject.Find(old)?.transform);
        PrefabUtility.InstantiatePrefab(fxPf);
        PrefabUtility.InstantiatePrefab(postPf);
        new GameObject("Hud").AddComponent<Hud>();   // nahrádza DebugHUD

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        // ── scéna Menu: aréna v pozadí, kamera krúži ──
        var menuScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Camera mcam = new GameObject("Main Camera").AddComponent<Camera>();
        mcam.tag = "MainCamera";
        mcam.gameObject.AddComponent<AudioListener>();
        mcam.nearClipPlane = 0.05f;
        mcam.fieldOfView = 70f;
        mcam.clearFlags = CameraClearFlags.SolidColor;
        Atmosphere(mcam);

        LevelBuilder mb = new GameObject("Level").AddComponent<LevelBuilder>();
        NavMeshSurface surface = mb.GetComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
        mb.wallPrefab = wallPf;
        mb.floorPrefab = floorPf;
        mb.ceilingPrefab = ceilingPf;
        mb.barrelPrefab = Lukas<Barrel>("Barrel");
        mb.healthPrefab = Lukas<Pickup>("Pickup_Health");
        mb.armorPrefab = Lukas<Pickup>("Pickup_Armor");
        mb.bulletsPrefab = Lukas<Pickup>("Pickup_Bullets");
        mb.shellsPrefab = Lukas<Pickup>("Pickup_Shells");
        mb.rocketsPrefab = Lukas<Pickup>("Pickup_Rockets");
        mb.shotgunPrefab = Lukas<Pickup>("Pickup_Shotgun");
        mb.chaingunPrefab = Lukas<Pickup>("Pickup_Chaingun");
        mb.launcherPrefab = Lukas<Pickup>("Pickup_RocketLauncher");

        PrefabUtility.InstantiatePrefab(fxPf);
        PrefabUtility.InstantiatePrefab(postPf);
        HlavneMenu hm = new GameObject("HlavneMenu").AddComponent<HlavneMenu>();
        hm.cam = mcam;
        hm.decor = new[] { Lukas<Enemy>("Enemy_Zombie"), Lukas<Enemy>("Enemy_Imp"), Lukas<Enemy>("Enemy_Demon"), Lukas<Enemy>("Enemy_Heavy") };
        EditorSceneManager.SaveScene(menuScene, MenuPath);

        // Menu musí byť v builde prvé
        var list = EditorBuildSettings.scenes.Where(s => s.path != MenuPath).ToList();
        list.Insert(0, new EditorBuildSettingsScene(MenuPath, true));
        if (!list.Any(s => s.path == ScenePath)) list.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        RetroResolution();
        AssetDatabase.SaveAssets();
        Debug.Log("Samo: grafika, efekty a zvuky hotové. Stlač Play.");
    }

    // tma, hmla, lampy na strope robia väčšinu svetla
    static void Atmosphere(Camera cam)
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.17f, 0.16f, 0.18f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.035f;
        RenderSettings.fogColor = new Color(0.04f, 0.035f, 0.04f);
        cam.backgroundColor = RenderSettings.fogColor;
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
    }

    static T Lukas<T>(string name) where T : Object
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(LukasPrefabs + "/" + name + ".prefab").GetComponent<T>();
    }

    // nižšie rozlíšenie s ostrými pixelmi (retro vzhľad), týka sa len 3D obrazu, nie UI
    static void RetroResolution()
    {
        var assets = Enumerable.Range(0, QualitySettings.count).Select(QualitySettings.GetRenderPipelineAssetAt)
                               .Append(GraphicsSettings.defaultRenderPipeline);
        foreach (UniversalRenderPipelineAsset u in assets.OfType<UniversalRenderPipelineAsset>().Distinct())
        {
            u.renderScale = 0.5f;
            u.upscalingFilter = UpscalingFilterSelection.Point;
            EditorUtility.SetDirty(u);
        }
    }

    static VolumeProfile PostFx()
    {
        string path = Art + "/PostFX.asset";
        AssetDatabase.DeleteAsset(path);
        VolumeProfile p = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(p, path);

        Bloom bloom = p.Add<Bloom>(true);
        bloom.threshold.Override(0.9f);
        bloom.intensity.Override(0.7f);
        bloom.scatter.Override(0.6f);
        Vignette v = p.Add<Vignette>(true);
        v.intensity.Override(0.38f);
        v.smoothness.Override(0.45f);
        ColorAdjustments ca = p.Add<ColorAdjustments>(true);
        ca.contrast.Override(18f);
        ca.saturation.Override(-10f);
        ca.postExposure.Override(0.15f);
        foreach (VolumeComponent c in p.components) AssetDatabase.AddObjectToAsset(c, p);
        EditorUtility.SetDirty(p);
        return p;
    }

    // ───────────────────────── prefaby ─────────────────────────

    static GameObject SaveCube(string name, Material mat, Material[] variants)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        go.AddComponent<Varianta>().materials = variants;
        return SavePrefab(go);
    }

    static GameObject SaveCeiling(Material mat, Material warm, Material cold)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Samo_Ceiling";
        go.GetComponent<Renderer>().sharedMaterial = mat;
        go.AddComponent<NavMeshModifier>().ignoreFromBuild = true;   // strop nesmie dostať NavMesh na vrchu

        // kocka 1×1×1 sa naškáluje na 4 × 0.2 × 4, preto sú miery lampy v tomto priestore
        Transform lampT = P(go.transform, PrimitiveType.Cube, warm, 0, -0.55f, 0, 0.3f, 0.25f, 0.15f);
        lampT.name = "Lamp";
        Light l = new GameObject("Light").AddComponent<Light>();
        l.transform.SetParent(lampT, false);
        l.transform.localPosition = new Vector3(0f, -6f, 0f);   // ~0.3 m pod stropom
        l.type = LightType.Point;
        l.color = new Color(1f, 0.75f, 0.5f);
        l.range = 12f;
        l.intensity = 14f;
        l.shadows = LightShadows.None;

        Lampa lampa = go.AddComponent<Lampa>();
        lampa.coldMaterial = cold;
        return SavePrefab(go);
    }

    static GameObject SavePrefab(GameObject go)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "/" + go.name + ".prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    static void Edit(string name, Action<GameObject> edit)
    {
        string path = LukasPrefabs + "/" + name + ".prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        edit(root);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    // model predmetu/sudu je dieťa "Model" (README); starý sa zmaže
    static void DressPickup(string name, Action<Transform> build)
    {
        Edit(name, r =>
        {
            Kill(r.transform.Find("Model"));
            build(Group(r.transform, "Model", Vector3.zero));
        });
    }

    // model nepriateľa ide pod "Body" (animácia útoku ho škáluje), kapsula a guľa oka sa skryjú.
    // Body/Model/Rig = kosti pre EnemyAnim, Body/Model/Shadow = tmavý fľak pod nohami
    static void DressEnemy(GameObject r, Action<Transform> build, float stride, float stepRate, float bob, float shadowSize)
    {
        Transform body = r.transform.Find("Body");
        body.GetComponent<Renderer>().enabled = false;
        Transform eye = r.transform.Find("Eye");
        if (eye) eye.GetComponent<Renderer>().enabled = false;

        Kill(body.Find("Model"));
        // Body má mierku (0.8s, s, 0.8s) a stred vo výške s → model má rovnomernú mierku s a päty na zemi
        Transform m = Group(body, "Model", new Vector3(0f, -1f, 0f));
        m.localScale = new Vector3(1.25f, 1f, 1.25f);
        P(m, PrimitiveType.Quad, shadowMat, 0, 0.03f, 0, shadowSize, shadowSize, 1f, 90, 0, 0).name = "Shadow";
        Transform rig = Group(m, "Rig", Vector3.zero);
        build(rig);

        Get<EnemyFx>(r).muzzle = FindDeep(rig, "Muzzle");
        EnemyAnim anim = Get<EnemyAnim>(r);
        anim.stride = stride;
        anim.stepRate = stepRate;
        anim.bob = bob;
    }

    static Transform FindDeep(Transform t, string name)
    {
        foreach (Transform c in t)
        {
            if (c.name == name) return c;
            Transform found = FindDeep(c, name);
            if (found) return found;
        }
        return null;
    }

    static void WeaponPickup(Transform m, int weapon)
    {
        GameObject w = BuildWeapon(weapon, m);
        w.SetActive(true);
        w.transform.localScale = Vector3.one * 1.8f;
        w.transform.localPosition = new Vector3(0f, 0f, -0.3f);
    }

    // ───────────────────────── modely ─────────────────────────

    // nepriatelia sú navrhnutí vo výške ~2 m, otočení dopredu (+Z). Kĺby (J) sú prázdne objekty, ktoré otáča EnemyAnim.
    static Transform J(Transform parent, string name, float x, float y, float z) { return Group(parent, name, new Vector3(x, y, z)); }
    static string Side(int i) { return i < 0 ? "L" : "R"; }
    const PrimitiveType Cube = PrimitiveType.Cube, Cyl = PrimitiveType.Cylinder, Sph = PrimitiveType.Sphere;

    static void BuildZombie(Transform m)
    {
        for (int i = -1; i <= 1; i += 2)
        {
            Transform leg = J(m, "Leg" + Side(i), i * 0.13f, 0.82f, 0);
            P(leg, Cube, clothMat, 0, -0.38f, 0, 0.2f, 0.78f, 0.22f);
            P(leg, Cube, armorMat, 0, -0.32f, 0.1f, 0.16f, 0.14f, 0.06f);
            P(leg, Cube, gunMat, 0, -0.75f, 0.04f, 0.22f, 0.14f, 0.3f);
            P(m, Cube, ammoMat, i * 0.16f, 0.92f, 0.17f, 0.1f, 0.1f, 0.06f);
        }
        P(m, Cube, clothMat, 0, 1.12f, 0, 0.52f, 0.62f, 0.3f);
        P(m, Cube, armorMat, 0, 1.2f, 0, 0.55f, 0.38f, 0.33f);
        P(m, Cube, gunMat, 0, 0.86f, 0, 0.54f, 0.08f, 0.32f);
        P(m, Cube, gunMat, 0, 1.22f, -0.22f, 0.3f, 0.35f, 0.12f);

        Transform armL = J(m, "ArmL", -0.34f, 1.38f, 0);
        P(armL, Cube, clothMat, 0, -0.28f, 0, 0.14f, 0.58f, 0.15f);
        P(armL, Cube, fleshMat, 0, -0.62f, 0, 0.12f, 0.1f, 0.12f);

        Transform armR = J(m, "ArmR", 0.3f, 1.3f, 0);   // drží pušku dopredu
        P(armR, Cube, clothMat, 0, -0.05f, 0.05f, 0.14f, 0.16f, 0.25f);
        P(armR, Cube, clothMat, 0, -0.1f, 0.2f, 0.14f, 0.15f, 0.45f);
        P(armR, Cube, gunMat, -0.08f, -0.05f, 0.45f, 0.07f, 0.09f, 0.7f);
        P(armR, Cube, gunMat, -0.08f, -0.15f, 0.4f, 0.05f, 0.12f, 0.06f);
        P(armR, Cube, woodMat, -0.08f, -0.13f, 0.25f, 0.06f, 0.12f, 0.08f);
        Group(armR, "Muzzle", new Vector3(-0.08f, -0.05f, 0.82f));

        Transform head = J(m, "Head", 0, 1.44f, 0);
        P(head, Cube, fleshMat, 0, 0.16f, 0, 0.3f, 0.32f, 0.3f);
        P(head, Cube, armorMat, 0, 0.33f, 0, 0.34f, 0.1f, 0.34f);
        P(head, Cyl, gunMat, 0, 0.09f, 0.17f, 0.15f, 0.07f, 0.15f, 90, 0, 0);
        P(head, Cyl, gunMat, 0, 0.03f, 0.25f, 0.08f, 0.05f, 0.08f, 90, 0, 0);
        for (int i = -1; i <= 1; i += 2) P(head, Cube, eyeYellow, i * 0.07f, 0.22f, 0.155f, 0.07f, 0.05f, 0.02f);
    }

    static void BuildImp(Transform m)
    {
        for (int i = -1; i <= 1; i += 2)
        {
            Transform leg = J(m, "Leg" + Side(i), i * 0.12f, 0.86f, 0);
            P(leg, Cube, impMat, 0, -0.4f, 0, 0.14f, 0.82f, 0.16f);
            P(leg, Cube, boneMat, 0, -0.83f, 0.06f, 0.15f, 0.06f, 0.22f);
            Transform arm = J(m, "Arm" + Side(i), i * 0.29f, 1.38f, 0.02f);
            P(arm, Cube, impMat, i * 0.02f, -0.4f, 0, 0.11f, 0.82f, 0.12f, 0, 0, i * 5);
            P(arm, Cube, boneMat, i * 0.04f, -0.83f, 0.06f, 0.13f, 0.07f, 0.2f);
        }
        P(m, Cube, impMat, 0, 1.15f, 0, 0.42f, 0.55f, 0.26f);
        P(m, Sph, fireCore, 0, 1.2f, 0.12f, 0.16f, 0.16f, 0.06f);
        for (int k = 0; k < 3; k++) P(m, Cube, boneMat, 0, 1.0f + k * 0.15f, -0.15f, 0.05f, 0.06f, 0.08f);

        Transform head = J(m, "Head", 0, 1.45f, 0);
        P(head, Cube, impMat, 0, 0.15f, 0.02f, 0.3f, 0.28f, 0.3f);
        P(head, Cube, boneMat, 0, 0.04f, 0.14f, 0.22f, 0.05f, 0.08f);
        for (int i = -1; i <= 1; i += 2)
        {
            P(head, Cyl, boneMat, i * 0.13f, 0.41f, 0, 0.06f, 0.13f, 0.06f, 0, 0, -i * 25);
            P(head, Cube, eyeYellow, i * 0.07f, 0.18f, 0.17f, 0.07f, 0.04f, 0.02f);
        }
    }

    static void BuildDemon(Transform m)
    {
        P(m, Cube, demonMat, 0, 0.95f, -0.05f, 0.9f, 0.75f, 1.1f);
        P(m, Cube, demonMat, 0, 1.4f, -0.15f, 0.7f, 0.35f, 0.7f);
        for (int k = 0; k < 3; k++) P(m, Cyl, boneMat, 0, 1.62f, -0.35f + k * 0.2f, 0.07f, 0.1f, 0.07f, -20, 0, 0);
        P(m, Cyl, demonMat, 0, 1.0f, -0.72f, 0.12f, 0.2f, 0.12f, -60, 0, 0);
        for (int i = -1; i <= 1; i += 2)
            for (int j = -1; j <= 1; j += 2)
            {
                Transform leg = J(m, (j > 0 ? "LegF" : "LegB") + Side(i), i * 0.3f, 0.62f, j * 0.35f);
                P(leg, Cube, demonMat, 0, -0.3f, 0, 0.22f, 0.6f, 0.22f);
                P(leg, Cube, boneMat, 0, -0.6f, 0.04f, 0.24f, 0.08f, 0.28f);
            }

        Transform head = J(m, "Head", 0, 1.0f, 0.45f);
        P(head, Cube, demonMat, 0, 0.05f, 0.2f, 0.6f, 0.45f, 0.45f);
        for (int k = 0; k < 4; k++) P(head, Cube, boneMat, -0.18f + k * 0.12f, -0.15f, 0.42f, 0.06f, 0.08f, 0.05f);
        for (int i = -1; i <= 1; i += 2)
        {
            P(head, Cube, eyeRed, i * 0.16f, 0.18f, 0.43f, 0.08f, 0.05f, 0.02f);
            P(head, Cyl, boneMat, i * 0.25f, 0.35f, 0.17f, 0.08f, 0.15f, 0.08f, 40, 0, -i * 15);
        }
        Transform jaw = J(head, "Jaw", 0, -0.15f, 0.05f);
        P(jaw, Cube, demonMat, 0, -0.07f, 0.18f, 0.55f, 0.15f, 0.4f);
        for (int k = 0; k < 3; k++) P(jaw, Cube, boneMat, -0.12f + k * 0.12f, 0.03f, 0.34f, 0.05f, 0.07f, 0.04f);
    }

    static void BuildHeavy(Transform m)
    {
        for (int i = -1; i <= 1; i += 2)
        {
            Transform leg = J(m, "Leg" + Side(i), i * 0.16f, 0.72f, 0);
            P(leg, Cube, armorMat, 0, -0.34f, 0, 0.24f, 0.66f, 0.26f);
            P(leg, Cube, gunMat, 0, -0.65f, 0.04f, 0.28f, 0.14f, 0.34f);
            P(m, Cyl, gunMat, i * 0.15f, 1.5f, -0.32f, 0.08f, 0.12f, 0.08f);
            P(m, Sph, fireCore, i * 0.15f, 1.63f, -0.32f, 0.06f, 0.03f, 0.06f);
        }
        P(m, Cube, clothMat, 0, 0.78f, 0, 0.5f, 0.2f, 0.3f);
        P(m, Cube, armorMat, 0, 1.15f, 0, 0.7f, 0.6f, 0.42f);
        P(m, Cube, eyeRed, 0, 1.3f, 0.215f, 0.3f, 0.04f, 0.02f);
        P(m, Cube, gunMat, 0, 1.2f, -0.3f, 0.5f, 0.45f, 0.18f);

        Transform armL = J(m, "ArmL", -0.46f, 1.38f, 0);
        P(armL, Cube, armorMat, 0, 0.04f, 0, 0.26f, 0.18f, 0.34f);
        P(armL, Cube, armorMat, 0, -0.33f, 0, 0.18f, 0.55f, 0.2f);
        P(armL, Cube, gunMat, 0, -0.66f, 0, 0.2f, 0.16f, 0.2f);

        Transform armR = J(m, "ArmR", 0.46f, 1.38f, 0);   // ruka je dvojhlavňový kanón
        P(armR, Cube, armorMat, 0, 0.04f, 0, 0.26f, 0.18f, 0.34f);
        P(armR, Cube, gunMat, 0, -0.23f, 0.15f, 0.2f, 0.2f, 0.4f);
        for (int i = -1; i <= 1; i += 2) P(armR, Cyl, gunMat, i * 0.045f, -0.23f, 0.5f, 0.07f, 0.22f, 0.07f, 90, 0, 0);
        Group(armR, "Muzzle", new Vector3(0f, -0.23f, 0.74f));

        Transform head = J(m, "Head", 0, 1.46f, 0.02f);
        P(head, Cube, armorMat, 0, 0.14f, 0, 0.26f, 0.24f, 0.26f);
        P(head, Cube, eyeRed, 0, 0.16f, 0.13f, 0.2f, 0.05f, 0.02f);
    }

    // model zbrane v ruke: hlaveň smeruje dopredu (+Z), "Flash" = záblesk pri výstrele, "Barrels" sa točia (guľomet)
    static GameObject BuildWeapon(int i, Transform parent)
    {
        string[] names = { "Fist", "Pistol", "Shotgun", "Chaingun", "RocketLauncher" };
        Transform m = Group(parent, "VM_" + names[i], Vector3.zero);
        float flashZ = 0f, flashSize = 0f, flashY = 0f;
        switch (i)
        {
            case 0:
                P(m, PrimitiveType.Cylinder, gloveMat, 0, -0.02f, -0.05f, 0.09f, 0.15f, 0.09f, 90, 0, 0);
                P(m, PrimitiveType.Cube, gloveMat, 0, 0, 0.15f, 0.12f, 0.11f, 0.13f);
                for (int k = 0; k < 4; k++) P(m, PrimitiveType.Cube, brassMat, -0.045f + k * 0.03f, 0.04f, 0.22f, 0.025f, 0.03f, 0.03f);
                break;
            case 1:
                P(m, PrimitiveType.Cube, gunMat, 0, 0.02f, 0.13f, 0.05f, 0.06f, 0.26f);
                P(m, PrimitiveType.Cube, gunMat, 0, -0.02f, 0.1f, 0.045f, 0.03f, 0.2f);
                P(m, PrimitiveType.Cube, woodMat, 0, -0.08f, 0.0f, 0.045f, 0.12f, 0.065f, 15, 0, 0);
                P(m, PrimitiveType.Cube, gunMat, 0, 0.058f, 0.24f, 0.012f, 0.015f, 0.02f);
                flashZ = 0.3f; flashY = 0.02f; flashSize = 0.25f;
                break;
            case 2:
                P(m, PrimitiveType.Cylinder, gunMat, 0, 0.02f, 0.28f, 0.045f, 0.275f, 0.045f, 90, 0, 0);
                P(m, PrimitiveType.Cylinder, gunMat, 0, -0.025f, 0.24f, 0.035f, 0.225f, 0.035f, 90, 0, 0);
                P(m, PrimitiveType.Cube, woodMat, 0, -0.025f, 0.3f, 0.065f, 0.055f, 0.16f);
                P(m, PrimitiveType.Cube, gunMat, 0, 0, -0.02f, 0.07f, 0.085f, 0.2f);
                P(m, PrimitiveType.Cube, woodMat, 0, -0.04f, -0.16f, 0.06f, 0.1f, 0.12f);
                flashZ = 0.58f; flashY = 0.02f; flashSize = 0.4f;
                break;
            case 3:
                P(m, PrimitiveType.Cylinder, gunMat, 0, 0, 0, 0.13f, 0.1f, 0.13f, 90, 0, 0);
                Transform barrels = Group(m, "Barrels", new Vector3(0f, 0f, 0.1f));
                for (int k = 0; k < 6; k++)
                {
                    float a = k * Mathf.PI / 3f;
                    P(barrels, PrimitiveType.Cylinder, gunMat, Mathf.Cos(a) * 0.04f, Mathf.Sin(a) * 0.04f, 0.21f, 0.03f, 0.21f, 0.03f, 90, 0, 0);
                }
                P(barrels, PrimitiveType.Cylinder, gunMat, 0, 0, 0.36f, 0.12f, 0.015f, 0.12f, 90, 0, 0);
                P(m, PrimitiveType.Cube, gunMat, 0, 0.08f, -0.02f, 0.03f, 0.04f, 0.14f);
                P(m, PrimitiveType.Cube, ammoMat, -0.06f, -0.07f, -0.04f, 0.08f, 0.08f, 0.1f);
                flashZ = 0.55f; flashSize = 0.4f;
                break;
            case 4:
                P(m, PrimitiveType.Cylinder, rocketPaint, 0, 0.02f, 0.18f, 0.14f, 0.325f, 0.14f, 90, 0, 0);
                P(m, PrimitiveType.Cylinder, gunMat, 0, 0.02f, 0.5f, 0.17f, 0.025f, 0.17f, 90, 0, 0);
                P(m, PrimitiveType.Cylinder, gunMat, 0, 0.02f, -0.12f, 0.17f, 0.025f, 0.17f, 90, 0, 0);
                P(m, PrimitiveType.Cube, woodMat, 0, -0.09f, 0.05f, 0.04f, 0.11f, 0.05f);
                P(m, PrimitiveType.Cube, gunMat, -0.07f, 0.09f, 0.12f, 0.03f, 0.05f, 0.08f);
                P(m, PrimitiveType.Sphere, eyeRed, 0, 0.02f, 0.49f, 0.1f, 0.1f, 0.06f);
                flashZ = 0.56f; flashY = 0.02f; flashSize = 0.5f;
                break;
        }
        if (flashSize > 0f)
        {
            Transform f = P(m, PrimitiveType.Quad, flashMat, 0, flashY, flashZ, flashSize, flashSize, 1f);
            f.name = "Flash";
            f.gameObject.SetActive(false);
        }
        return m.gameObject;
    }

    // ───────────────────────── častice ─────────────────────────

    static ParticleSystem Ps(Transform parent, string name, Material mat, bool loop = false)
    {
        GameObject go = new GameObject(name);
        if (parent) go.transform.SetParent(parent, false);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = loop;
        main.duration = loop ? 1f : 0.2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.playOnAwake = true;
        var em = ps.emission;
        em.rateOverTime = 0f;
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Sphere;
        sh.radius = 0.05f;
        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        return ps;
    }

    static void Look(ParticleSystem ps, float life0, float life1, float speed0, float speed1, float size0, float size1,
                     Color from, Color to, float gravity = 0f, bool shrink = true)
    {
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life0, life1);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed0, speed1);
        main.startSize = new ParticleSystem.MinMaxCurve(size0, size1);
        main.gravityModifier = gravity;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(from, 0f), new GradientColorKey(to, 1f) },
                  new[] { new GradientAlphaKey(from.a, 0f), new GradientAlphaKey(from.a, 0.5f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, shrink ? AnimationCurve.Linear(0f, 1f, 1f, 0.2f) : AnimationCurve.Linear(0f, 0.6f, 1f, 1.4f));
    }

    static void Burst(ParticleSystem ps, int count)
    {
        var em = ps.emission;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
    }

    static void Cone(ParticleSystem ps, float angle)
    {
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Cone;
        sh.angle = angle;
        sh.radius = 0.02f;
    }

    static GameObject FxSparks()
    {
        ParticleSystem s = Ps(null, "FX_Sparks", blobAdd);
        Look(s, 0.15f, 0.35f, 3f, 8f, 0.04f, 0.08f, new Color(1f, 0.85f, 0.4f), new Color(1f, 0.35f, 0.1f), 1.5f);
        Burst(s, 10);
        Cone(s, 35f);
        ParticleSystem dust = Ps(s.transform, "Dust", blobAlpha);
        Look(dust, 0.4f, 0.7f, 0.3f, 1f, 0.2f, 0.4f, new Color(0.5f, 0.48f, 0.45f, 0.6f), new Color(0.3f, 0.3f, 0.3f, 0f), 0f, false);
        Burst(dust, 4);
        Cone(dust, 25f);
        return SavePrefab(s.gameObject);
    }

    static GameObject FxBlood(string name, int count, float life0, float life1, float speed0, float speed1, float size0, float size1)
    {
        ParticleSystem s = Ps(null, name, blobAlpha);
        Look(s, life0, life1, speed0, speed1, size0, size1, new Color(0.55f, 0.02f, 0.02f), new Color(0.25f, 0f, 0f), 2f);
        Burst(s, count);
        Cone(s, 50f);
        return SavePrefab(s.gameObject);
    }

    static GameObject FxExplosion()
    {
        ParticleSystem fire = Ps(null, "FX_Explosion", blobAdd);
        Look(fire, 0.35f, 0.7f, 2f, 7f, 0.8f, 1.8f, new Color(1f, 0.9f, 0.5f), new Color(0.8f, 0.2f, 0.05f), 0f, false);
        Burst(fire, 30);
        var sh = fire.shape;
        sh.radius = 0.5f;
        ParticleSystem smoke = Ps(fire.transform, "Smoke", blobAlpha);
        Look(smoke, 0.8f, 1.4f, 0.5f, 2f, 1f, 2.2f, new Color(0.15f, 0.13f, 0.12f, 0.8f), new Color(0.1f, 0.1f, 0.1f, 0f), -0.2f, false);
        Burst(smoke, 15);
        ParticleSystem debris = Ps(fire.transform, "Debris", blobAdd);
        Look(debris, 0.4f, 0.8f, 6f, 12f, 0.05f, 0.1f, new Color(1f, 0.8f, 0.3f), new Color(1f, 0.3f, 0.05f), 2f);
        Burst(debris, 20);
        return SavePrefab(fire.gameObject);
    }

    static GameObject FxFireImpact()
    {
        ParticleSystem s = Ps(null, "FX_FireImpact", blobAdd);
        Look(s, 0.2f, 0.4f, 1f, 3f, 0.2f, 0.45f, new Color(1f, 0.7f, 0.25f), new Color(0.8f, 0.15f, 0.05f));
        Burst(s, 14);
        return SavePrefab(s.gameObject);
    }

    static GameObject FxMuzzle()
    {
        ParticleSystem s = Ps(null, "FX_Muzzle", flashMat);
        Look(s, 0.05f, 0.07f, 0f, 0f, 0.5f, 0.7f, new Color(1f, 0.85f, 0.4f), new Color(1f, 0.5f, 0.1f));
        Burst(s, 1);
        return SavePrefab(s.gameObject);
    }

    static void Trail(Transform parent, Vector3 pos)
    {
        ParticleSystem s = Ps(parent, "Trail", blobAlpha, true);
        s.transform.localPosition = pos;
        var main = s.main;
        main.scalingMode = ParticleSystemScalingMode.Shape;   // veľkosť dymu nezávisí od mierky rakety
        Look(s, 0.4f, 0.8f, 0f, 0.2f, 0.15f, 0.35f, new Color(0.6f, 0.58f, 0.55f, 0.7f), new Color(0.3f, 0.3f, 0.3f, 0f), -0.1f, false);
        var em = s.emission;
        em.rateOverDistance = 25f;
    }

    static void Flames(Transform parent)
    {
        ParticleSystem s = Ps(parent, "Flames", blobAdd, true);
        var main = s.main;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        Look(s, 0.15f, 0.3f, 0f, 0.5f, 0.25f, 0.45f, new Color(1f, 0.7f, 0.25f), new Color(0.8f, 0.15f, 0.05f));
        var em = s.emission;
        em.rateOverTime = 50f;
    }

    // ───────────────────────── materiály ─────────────────────────

    static Material Lit(string name, Texture2D tex, Color tint, float smooth = 0.12f, Texture2D emisTex = null, Color? emission = null)
    {
        Material m = LoadOrCreate(name, "Universal Render Pipeline/Lit");
        m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", tint);
        m.SetFloat("_Smoothness", smooth);
        m.SetFloat("_Metallic", 0f);
        if (emission.HasValue)
        {
            m.EnableKeyword("_EMISSION");
            m.SetTexture("_EmissionMap", emisTex);
            m.SetColor("_EmissionColor", emission.Value);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        else
        {
            m.DisableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", Color.black);
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material Particle(string name, Texture2D tex, bool additive)
    {
        Material m = LoadOrCreate(name, "Universal Render Pipeline/Particles/Unlit");
        m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", additive ? 2f : 0f);
        m.SetFloat("_Cull", 0f);
        m.SetFloat("_ZWrite", 0f);
        float dst = (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", dst);
        m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        m.SetFloat("_DstBlendAlpha", dst);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.SetOverrideTag("RenderType", "Transparent");
        m.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material LoadOrCreate(string name, string shader)
    {
        string path = Art + "/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find(shader));
            AssetDatabase.CreateAsset(m, path);
        }
        return m;
    }

    // ───────────────────────── pomocné ─────────────────────────

    static Transform P(Transform parent, PrimitiveType type, Material mat, float x, float y, float z,
                       float sx, float sy, float sz, float ex = 0f, float ey = 0f, float ez = 0f)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        Object.DestroyImmediate(go.GetComponent<Collider>());   // modely nesmú chytať výstrely ani meniť fyziku
        go.name = type.ToString();
        go.GetComponent<Renderer>().sharedMaterial = mat;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(x, y, z);
        go.transform.localRotation = Quaternion.Euler(ex, ey, ez);
        go.transform.localScale = new Vector3(sx, sy, sz);
        return go.transform;
    }

    static Transform Group(Transform parent, string name, Vector3 pos)
    {
        Transform t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = pos;
        return t;
    }

    static void Kill(Transform t) { if (t) Object.DestroyImmediate(t.gameObject); }

    static T Get<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        return c ? c : go.AddComponent<T>();
    }

    // ───────────────────────── textúry ─────────────────────────

    static Texture2D Tex(string name, int size, Func<int, int, Color> f, bool alpha = false)
    {
        Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] px = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Color c = f(x, y);
                // posterizácia: menej odtieňov = pixelový vzhľad
                c.r = Mathf.Round(Mathf.Clamp01(c.r) * 15f) / 15f;
                c.g = Mathf.Round(Mathf.Clamp01(c.g) * 15f) / 15f;
                c.b = Mathf.Round(Mathf.Clamp01(c.b) * 15f) / 15f;
                if (!alpha) c.a = 1f;
                px[y * size + x] = c;
            }
        t.SetPixels(px);
        string path = TexDir + "/" + name + ".png";
        File.WriteAllBytes(path, t.EncodeToPNG());
        Object.DestroyImmediate(t);

        AssetDatabase.ImportAsset(path);
        TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = !alpha;
        ti.alphaIsTransparency = alpha;
        ti.wrapMode = alpha ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
        ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static float H(int x, int y, int s)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + s * 1442695041);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xffffff) / 16777216f;
        }
    }

    static int Wrap(int v, int p) { return (v % p + p) % p; }

    // dlaždicová value noise (opakuje sa na okraji textúry)
    static float VN(float x, float y, int period, int s)
    {
        int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
        float fx = x - x0, fy = y - y0;
        fx = fx * fx * (3f - 2f * fx);
        fy = fy * fy * (3f - 2f * fy);
        int a = Wrap(x0, period), b = Wrap(x0 + 1, period), c = Wrap(y0, period), d = Wrap(y0 + 1, period);
        return Mathf.Lerp(Mathf.Lerp(H(a, c, s), H(b, c, s), fx), Mathf.Lerp(H(a, d, s), H(b, d, s), fx), fy);
    }

    static float N(int x, int y, int size, int cells, int s)
    {
        float v = 0f, amp = 1f, total = 0f;
        for (int o = 0; o < 3; o++)
        {
            int c = cells << o;
            v += amp * VN(x * (float)c / size, y * (float)c / size, c, s + o * 17);
            total += amp;
            amp *= 0.5f;
        }
        return v / total;
    }

    static Color RGB(float r, float g, float b) { return new Color(r, g, b, 1f); }
    static Color Sc(Color c, float k) { return new Color(c.r * k, c.g * k, c.b * k, c.a); }

    // spodný sokel a horná lišta, rovnaké na všetkých stenách (y = 0 je dole)
    static bool Trim(int x, int y, float n, out Color c)
    {
        if (y < 10)
        {
            c = Sc(RGB(0.2f, 0.19f, 0.18f), 0.8f + 0.4f * n);
            if (y == 9) c = Sc(c, 1.4f);
            if (y == 5 && x % 16 == 8) c = RGB(0.55f, 0.52f, 0.48f);
            return true;
        }
        if (y >= 122)
        {
            c = Sc(RGB(0.14f, 0.14f, 0.15f), 0.8f + 0.4f * n);
            if (y == 122) c = Sc(c, 0.6f);
            return true;
        }
        c = default;
        return false;
    }

    static Color Concrete(int x, int y, int s)
    {
        float n = N(x, y, 128, 8, s);
        if (Trim(x, y, n, out Color t)) return t;
        int row = (y - 10) / 28, ly = (y - 10) % 28;
        int off = (row % 2) * 32;
        int lx = (x + off) % 64, bx = ((x + off) / 64) % 2;
        if (lx < 2 || ly < 2) return Sc(RGB(0.15f, 0.15f, 0.17f), 0.8f + 0.4f * n);

        Color c = Sc(RGB(0.40f, 0.40f, 0.43f), (0.8f + 0.35f * H(bx, row, s + 5)) * (0.65f + 0.7f * n));
        if (ly == 27) c = Sc(c, 1.2f);
        else if (ly == 2) c = Sc(c, 0.8f);
        if (Mathf.Abs(N(x, y, 128, 6, s + 13) - 0.5f) < 0.007f) c = Sc(c, 0.55f);   // praskliny

        // hrdzavé šmuhy stekajú od horného okraja bloku
        int gx = x / 3;
        if (H(gx, row, s + 9) < 0.15f)
        {
            float len = 8f + H(gx, row, s + 11) * 18f;
            float d = 27 - ly;
            if (d < len) c = Color.Lerp(c, Sc(RGB(0.45f, 0.22f, 0.1f), 0.8f + 0.4f * n), (1f - d / len) * 0.75f);
        }
        return c;
    }

    static Color Pipes(int x, int y)
    {
        Color c = Concrete(x, y, 2);
        if (y < 10 || y >= 122) return c;
        foreach (var (x0, w) in new[] { (16, 16), (40, 8) })
        {
            if (x < x0 || x >= x0 + w) continue;
            float sh = 0.35f + 0.8f * Mathf.Sin(Mathf.PI * (x - x0 + 0.5f) / w);
            float n = N(x, y, 128, 8, 4);
            c = (y - 10) % 40 < 4 ? Sc(RGB(0.18f, 0.18f, 0.2f), sh * 1.2f) : Sc(RGB(0.36f, 0.25f, 0.18f), sh * (0.8f + 0.4f * n));
        }
        return c;
    }

    static Color Panel(int x, int y, bool emission)
    {
        bool glow = y >= 64 && y < 68 && x % 32 >= 4 && x % 32 < 28;
        if (emission) return glow ? RGB(1f, 1f, 1f) : Color.black;

        float n = N(x, y, 128, 16, 3);
        if (Trim(x, y, n, out Color t)) return t;
        if (y >= 62 && y < 70) return glow ? RGB(0.3f, 0.9f, 0.85f) : RGB(0.08f, 0.08f, 0.09f);

        int row = (y - 10) / 56, px = x % 64, py = (y - 10) % 56;
        Color c = Sc(RGB(0.30f, 0.32f, 0.35f), (0.75f + 0.5f * n) * (0.9f + 0.2f * H(x / 64, row, 7)));
        if (px < 3 || py > 52) c = Sc(c, 1.3f);
        else if (px > 60 || py < 3) c = Sc(c, 0.6f);
        if ((px == 6 || px == 57) && (py == 6 || py == 49)) c = RGB(0.6f, 0.6f, 0.62f);
        return c;
    }

    static Color Plate(int x, int y)
    {
        int tx = x % 64, ty = y % 64;
        float n = N(x, y, 128, 8, 5);
        if (tx < 2 || ty < 2) return RGB(0.1f, 0.1f, 0.1f);
        Color c = Sc(RGB(0.30f, 0.30f, 0.31f), 0.75f + 0.5f * n);
        int cx = tx / 8, cy = ty / 8, lx = tx % 8, ly = ty % 8;
        bool diag = (cx + cy) % 2 == 0 ? lx == ly : lx == 7 - ly;
        if (diag && lx >= 2 && lx <= 5) c = Sc(c, 1.35f);
        if (tx == 2 || ty == 63) c = Sc(c, 1.2f);
        if (N(x, y, 128, 4, 9) > 0.62f) c = Sc(c, 0.6f);   // mastné škvrny
        return c;
    }

    static Color Grate(int x, int y)
    {
        float n = N(x, y, 128, 8, 6);
        if (x < 3 || y < 3 || x > 124 || y > 124) return Sc(RGB(0.25f, 0.25f, 0.26f), 0.8f + 0.4f * n);
        int tx = x % 16, ty = y % 16;
        if (tx >= 4 && ty >= 4) return Sc(RGB(0.04f, 0.04f, 0.04f), 1f + n);
        Color c = Sc(RGB(0.33f, 0.31f, 0.28f), 0.8f + 0.4f * n);
        return tx == 0 || ty == 3 ? Sc(c, 1.3f) : c;
    }

    static Color Ceiling(int x, int y)
    {
        int tx = x % 32, ty = y % 32;
        float n = N(x, y, 128, 8, 8);
        if (tx < 1 || ty < 1) return RGB(0.08f, 0.08f, 0.09f);
        if (H(x / 32, y / 32, 3) < 0.2f && ty % 4 == 0 && tx >= 6 && tx < 26) return RGB(0.05f, 0.05f, 0.05f);   // vetracia mriežka
        return Sc(RGB(0.2f, 0.2f, 0.22f), (0.8f + 0.4f * n) * (0.85f + 0.3f * H(x / 32, y / 32, 2)));
    }

    static Color Lamp(int x, int y)
    {
        if (x < 3 || y < 3 || x > 28 || y > 28) return RGB(0.25f, 0.25f, 0.26f);
        return x % 8 == 0 ? RGB(0.85f, 0.82f, 0.75f) : RGB(1f, 0.97f, 0.9f);
    }

    // sivé, aby farba kľúča (MaterialPropertyBlock) bola dobre vidieť
    static Color Door(int x, int y)
    {
        float n = N(x, y, 128, 8, 10);
        if (y >= 120) return RGB(0.12f, 0.12f, 0.12f);
        if (y < 18) return ((x + y) / 8) % 2 == 0 ? RGB(0.75f, 0.75f, 0.75f) : RGB(0.15f, 0.15f, 0.15f);
        if (y == 18 || y == 56 || y == 72) return RGB(0.1f, 0.1f, 0.1f);
        if (y > 56 && y < 72)
            return x % 16 == 8 && (y == 60 || y == 68) ? RGB(0.85f, 0.85f, 0.85f) : Sc(RGB(0.62f, 0.62f, 0.62f), 0.85f + 0.3f * n);
        Color c = Sc(RGB(0.42f, 0.42f, 0.44f), 0.8f + 0.4f * n);
        int rib = x % 16;
        return rib == 0 ? Sc(c, 1.3f) : rib >= 14 ? Sc(c, 0.55f) : c;
    }

    static Color Barrel(int x, int y)
    {
        float n = N(x, y, 64, 8, 11);
        if (y < 2 || y > 61) return RGB(0.12f, 0.1f, 0.1f);
        if (y == 10 || y == 52) return Sc(RGB(0.6f, 0.2f, 0.12f), 1.4f);
        if (y == 11 || y == 53) return RGB(0.2f, 0.06f, 0.04f);
        if (y >= 24 && y < 40) return ((x + y) / 4) % 3 == 0 ? RGB(0.08f, 0.08f, 0.08f) : Sc(RGB(0.85f, 0.65f, 0.1f), 0.8f + 0.3f * n);
        Color c = Sc(RGB(0.55f, 0.14f, 0.08f), 0.75f + 0.5f * n);
        return N(x, y, 64, 4, 12) > 0.6f ? Color.Lerp(c, RGB(0.35f, 0.2f, 0.1f), 0.7f) : c;
    }

    static Color Flesh(int x, int y)
    {
        float n = N(x, y, 64, 8, 13);
        if (Mathf.Abs(N(x, y, 64, 6, 14) - 0.5f) < 0.025f) return RGB(0.32f, 0.2f, 0.25f);
        return Sc(RGB(0.5f, 0.52f, 0.42f), 0.7f + 0.6f * n);
    }

    static Color Cloth(int x, int y)
    {
        float n = N(x, y, 64, 8, 15);
        if (x % 32 == 0) return RGB(0.12f, 0.11f, 0.08f);
        Color c = Sc(RGB(0.28f, 0.27f, 0.2f), (0.7f + 0.5f * n) * ((x + y) % 2 == 0 ? 1.05f : 0.95f));
        return N(x, y, 64, 4, 16) > 0.65f ? Sc(c, 0.6f) : c;
    }

    static Color ImpHide(int x, int y, bool emission)
    {
        bool crack = Mathf.Abs(N(x, y, 64, 5, 17) - 0.5f) < 0.018f;
        if (emission) return crack ? Color.white : Color.black;
        return crack ? RGB(1f, 0.45f, 0.1f) : Sc(RGB(0.12f, 0.1f, 0.1f), 0.6f + 0.8f * N(x, y, 64, 8, 18));
    }

    static Color DemonHide(int x, int y)
    {
        float n = N(x, y, 64, 8, 19);
        if (N(x, y, 64, 8, 20) > 0.68f) return RGB(0.6f, 0.35f, 0.3f);
        Color c = Sc(RGB(0.42f, 0.12f, 0.12f), 0.6f + 0.7f * n);
        return Mathf.Sin(y * 0.8f + n * 6f) > 0.85f ? Sc(c, 0.6f) : c;
    }

    static Color Armor(int x, int y)
    {
        int tx = x % 32, ty = y % 32;
        float n = N(x, y, 64, 8, 21);
        Color c = Sc(RGB(0.33f, 0.35f, 0.28f), (0.8f + 0.4f * n) * (0.9f + 0.2f * H(x / 32, y / 32, 22)));
        if (tx < 2 || ty > 29) c = Sc(c, 1.35f);
        else if (tx > 29 || ty < 2) c = Sc(c, 0.55f);
        return H(x, y / 2, 23) > 0.985f ? Sc(c, 1.6f) : c;
    }

    static Color GunMetal(int x, int y)
    {
        if (H(x / 2, y, 24) > 0.97f) return RGB(0.45f, 0.45f, 0.48f);
        return Sc(RGB(0.17f, 0.18f, 0.21f), 0.8f + 0.4f * N(x, y, 64, 8, 25));
    }

    static Color Wood(int x, int y)
    {
        float g = Mathf.Sin(x * 0.9f + N(x, y, 64, 4, 26) * 8f);
        return Sc(RGB(0.38f, 0.22f, 0.11f), 0.8f + 0.25f * g);
    }

    static Color Medkit(int x, int y)
    {
        if (x < 3 || y < 3 || x > 60 || y > 60) return RGB(0.6f, 0.6f, 0.58f);
        int dx = Mathf.Abs(x - 32), dy = Mathf.Abs(y - 32);
        if ((dx < 6 && dy < 18) || (dy < 6 && dx < 18)) return RGB(0.8f, 0.08f, 0.08f);
        return Sc(RGB(0.88f, 0.88f, 0.85f), 0.92f + 0.1f * N(x, y, 64, 8, 27));
    }

    static Color AmmoBox(int x, int y)
    {
        if (x < 2 || y < 2 || x > 61 || y > 61) return RGB(0.12f, 0.13f, 0.08f);
        if (y >= 40 && y < 46) return RGB(0.85f, 0.7f, 0.15f);
        return Sc(RGB(0.3f, 0.33f, 0.18f), 0.8f + 0.3f * N(x, y, 64, 8, 28));
    }

    static Color ShellBox(int x, int y)
    {
        if (x < 2 || y < 2 || x > 61 || y > 61) return RGB(0.15f, 0.03f, 0.02f);
        if (y >= 20 && y < 30) return RGB(0.75f, 0.6f, 0.25f);
        return Sc(RGB(0.45f, 0.08f, 0.06f), 0.8f + 0.3f * N(x, y, 64, 8, 29));
    }

    static Color Exit(int x, int y)
    {
        return x % 16 < 2 || y % 16 < 2 ? RGB(0.2f, 1f, 0.55f) : RGB(0.03f, 0.06f, 0.05f);
    }

    static Color Blob(int x, int y)
    {
        float d = new Vector2(x - 7.5f, y - 7.5f).magnitude / 8f;
        float a = d < 1f ? Mathf.Ceil(Mathf.Pow(1f - d, 1.5f) * 4f) / 4f : 0f;
        return new Color(1f, 1f, 1f, a);
    }

    static Color Star(int x, int y)
    {
        float dx = Mathf.Abs(x - 15.5f) / 16f, dy = Mathf.Abs(y - 15.5f) / 16f;
        float r = Mathf.Sqrt(dx * dx + dy * dy);
        float a = Mathf.Max(1f - r * 1.6f, (1f - r) * (1f - Mathf.Min(dx, dy) * 8f));
        return new Color(1f, 1f, 1f, Mathf.Ceil(Mathf.Clamp01(a) * 4f) / 4f);
    }

    // ───────────────────────── zvuky (syntéza, 22 kHz mono) ─────────────────────────

    const int Rate = 22050;
    const float Tau = Mathf.PI * 2f;
    static System.Random rng;

    static AudioClip Sfx(string name, float dur, Func<float, float> f, bool loop = false)
    {
        rng = new System.Random(7);
        lp1 = lp2 = 0f;
        int n = (int)(dur * Rate);
        float[] d = new float[n];
        float peak = 0.0001f;
        for (int i = 0; i < n; i++)
        {
            d[i] = f(i / (float)Rate);
            peak = Mathf.Max(peak, Mathf.Abs(d[i]));
        }
        int fade = loop ? 0 : Rate / 200;   // 5 ms na konci proti cvaknutiu
        string path = AudioDir + "/" + name + ".wav";
        using (BinaryWriter w = new BinaryWriter(File.Create(path)))
        {
            w.Write("RIFF".ToCharArray()); w.Write(36 + n * 2); w.Write("WAVE".ToCharArray());
            w.Write("fmt ".ToCharArray()); w.Write(16); w.Write((short)1); w.Write((short)1);
            w.Write(Rate); w.Write(Rate * 2); w.Write((short)2); w.Write((short)16);
            w.Write("data".ToCharArray()); w.Write(n * 2);
            for (int i = 0; i < n; i++)
            {
                float k = fade > 0 && i > n - fade ? (n - i) / (float)fade : 1f;
                w.Write((short)(Mathf.Clamp(d[i] / peak * 0.9f * k, -1f, 1f) * 32767f));
            }
        }
        AssetDatabase.ImportAsset(path);
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }

    static float Nz() { return (float)(rng.NextDouble() * 2.0 - 1.0); }

    // dva jednoduché dolnopriepustné filtre (a = 0..1, menšie = hlbší zvuk)
    static float lp1, lp2;
    static float Lp1(float x, float a) { return lp1 += a * (x - lp1); }
    static float Lp2(float x, float a) { return lp2 += a * (x - lp2); }

    static float E(float t, float k) { return Mathf.Exp(-t * k); }
    static float Sin(float cycles) { return Mathf.Sin(Tau * cycles); }
    static float Sq(float cycles) { return Mathf.Repeat(cycles, 1f) < 0.5f ? 1f : -1f; }
    static float Saw(float cycles) { return 2f * Mathf.Repeat(cycles, 1f) - 1f; }
    // počet cyklov pri lineárnom posune frekvencie f0 → f1 za čas d
    static float Sweep(float t, float f0, float f1, float d) { return f0 * t + (f1 - f0) * t * t / (2f * d); }
    static float Crackle(float t, float k) { return Nz() > 0.94f ? Nz() * E(t, k) : 0f; }

    static float Fist(float t) { return Lp1(Nz(), 0.12f) * Mathf.Sin(Mathf.PI * t / 0.3f); }
    static float Pistol(float t) { return Lp1(Nz(), 0.35f) * E(t, 16) + Sin(Sweep(t, 160, 60, 0.35f)) * E(t, 22) * 0.9f; }
    static float Shotgun(float t) { return Lp1(Nz(), 0.22f) * E(t, 6) * 1.2f + Sin(Sweep(t, 110, 40, 0.75f)) * E(t, 10) + Crackle(t, 8); }
    static float Chaingun(float t) { return Lp1(Nz(), 0.5f) * E(t, 28) + Sin(Sweep(t, 200, 80, 0.18f)) * E(t, 35) * 0.7f; }
    static float RocketLaunch(float t)
    {
        return Lp1(Nz(), 0.05f + 0.3f * t) * Mathf.Min(t * 20f, 1f) * E(t, 3) + Sin(Sweep(t, 90, 220, 0.9f)) * 0.3f * E(t, 2);
    }
    static float Boom(float t)
    {
        return Lp1(Nz(), 0.06f) * 1.8f * E(t, 2.2f) + Lp2(Nz(), 0.25f) * E(t, 6) * 0.6f
             + Sin(Sweep(t, 60, 25, 1.8f)) * E(t, 3) * 0.8f + Crackle(t, 3);
    }
    static float Impact(float t) { return Lp1(Nz(), 0.2f) * E(t, 12) + Crackle(t, 10); }
    static float EnemyShot(float t) { return Lp1(Nz(), 0.55f) * E(t, 18) + Sin(Sweep(t, 220, 90, 0.3f)) * E(t, 25) * 0.6f; }
    static float Fireball(float t)
    {
        return Lp1(Nz(), 0.3f) * (0.6f + 0.4f * Sin(30f * t)) * Mathf.Sin(Mathf.PI * t / 0.6f)
             + Sin(Sweep(t, 500, 150, 0.6f)) * 0.3f * E(t, 4);
    }
    static float Bite(float t)
    {
        float env = Mathf.Min(t * 30f, 1f) * E(t, 4);
        return Saw(75f * t) * (0.6f + 0.4f * Sin(23f * t)) * env + Lp1(Nz(), 0.3f) * 0.4f * env;
    }
    static float EnemyHurt(float t) { return Sq(Sweep(t, 320, 140, 0.3f)) * 0.5f * E(t, 8) + Lp1(Nz(), 0.4f) * 0.3f * E(t, 12); }
    static float EnemyDeath(float t)
    {
        return Saw(Sweep(t, 240, 45, 1f)) * (0.7f + 0.3f * Sin(12f * t)) * E(t, 2.5f) * 0.7f + Lp1(Nz(), 0.3f) * E(t, 5) * 0.5f;
    }
    static float DoorSound(float t)
    {
        float body = (Saw(48f * t) * 0.4f + Lp1(Nz(), 0.1f) * 0.8f * (0.7f + 0.3f * Sin(7f * t))) * Mathf.Min(t * 10f, 1f) * (t < 0.95f ? 1f : 0.3f);
        float thunk = t > 0.95f ? Sin(60f * (t - 0.95f)) * E(t - 0.95f, 20) * 1.2f : 0f;
        return body + thunk;
    }
    static float Denied(float t) { return Sq(110f * t) * 0.6f * (t % 0.17f < 0.12f ? 1f : 0f); }
    static float PickupSound(float t) { return Sq((t < 0.07f ? 880f : 1320f) * t) * 0.5f * E(t, 6); }
    static float WeaponPickupSound(float t)
    {
        float[] notes = { 440f, 554f, 659f, 880f };
        int k = Mathf.Min((int)(t / 0.1f), 3);
        return Sq(notes[k] * t) * 0.45f * E(t - k * 0.1f, k == 3 ? 5f : 8f);
    }
    static float SecretSound(float t)
    {
        float[] notes = { 523f, 659f, 784f, 1047f };
        float v = 0f;
        for (int k = 0; k < notes.Length; k++)
        {
            float tk = t - k * 0.12f;
            if (tk > 0f) v += Sin(notes[k] * tk) * E(tk, 3) * 0.35f;
        }
        return v;
    }
    static float PlayerHurt(float t) { return Sq(Sweep(t, 420, 180, 0.3f)) * 0.4f * E(t, 9) + Lp1(Nz(), 0.4f) * 0.3f * E(t, 15); }
    static float PlayerDeath(float t) { return Saw(Sweep(t, 320, 40, 1.3f)) * E(t, 1.8f) * 0.6f + Lp1(Nz(), 0.2f) * 0.3f * E(t, 3); }
    // slučka 8 s: všetky frekvencie majú celý počet cyklov za 8 s, takže spoj nie je počuť
    static float Ambience(float t)
    {
        return 0.5f * Sin(40f * t) + 0.3f * Sin(60.5f * t) * (0.6f + 0.4f * Sin(0.25f * t))
             + 0.15f * Sin(81f * t) * (0.5f + 0.5f * Sin(0.125f * t));
    }
}
