# Doom (Unity klon)

Školský tímový projekt: klon hry Doom (1993) v Unity 6 (`6000.6.2f1`, URP), skóre sa ukladá do Firebase Realtime Database.
Štruktúra je rovnaká ako v [car-race-master](https://github.com/Horvy10/car-race-master).

## Režimy
- **Kampaň**: 5 levelov. Prejdi level, nájdi kľúče, otvor dvere a vojdi do zeleného východu. Skóre = zabitia + bonus za čas + bonus za tajné miesta.
- **Survival**: aréna, vlny nepriateľov, ktoré sú čoraz väčšie a silnejšie. Po vlne sa predmety v aréne obnovia. Hra končí smrťou. Skóre = zabitia.

## Ovládanie
WASD pohyb, Shift beh, myš mierenie, ľavé tlačidlo streľba, 1–5 alebo koliesko výber zbrane, E otvorenie dverí (aj tajných), Esc pauza.

## Rozdelenie práce
| Kto | Čo | Kde |
|---|---|---|
| Lukáš | jadro hry: hráč, zbrane, nepriatelia, predmety, dvere, levely, survival | `Assets/Scripts/`, `Editor/LukasSetup.cs` |
| Marco | UI (menu, výber levelu, HUD, pauza, rebríček) + Firebase | `Assets/Scripts/UI/`, `Editor/MarcoSetup.cs` |
| Samo | grafika a prostredie: steny, podlahy, modely, efekty, svetlá, zvuky | `Assets/Scripts/Prostredie/`, `Editor/SamoSetup.cs` |

## Prvé spustenie
1. Otvor projekt v Unity Hub (Add project from disk).
2. Menu **Tools > Lukas > 1. Vytvor prefaby a scénu Game**. Vytvorí prefaby v `Assets/Prefabs/Lukas`, materiály v `Assets/Art/Lukas` a scénu `Assets/Scenes/Game.unity` (pridá ju do Build Settings).
3. Otvor scénu `Game` a stlač Play.

Na test survivalu bez menu: objekt `GameManager`, zaškrtni `Override Session` a nastav `Test Mode = Survival`.

## Ako to funguje
- `GameSession`: tabuľka levelov (obtiažnosť), vybraný level a režim. Menu ich nastaví pred načítaním scény `Game`.
- `LevelMaps`: mapy levelov ako text (legenda je v súbore). Nový level = nová textová mapa. Znaky: `#` stena, `P` štart, `E` východ, `z/i/d/H` nepriatelia, `o` sud, `D` dvere, `r/u/y` dvere na kľúč, `R/U/Y` kľúče, `=` tajné dvere, `!` tajné miesto, `h/a/b/s/k` predmety, `S/C/L` zbrane, `x` spawn survivalu.
- `LevelBuilder`: pri štarte postaví level z mapy a upečie NavMesh. Prefaby stien a podlahy sú kocky 1×1×1, builder ich naškáluje na bunku 4 m.
- `GameManager`: zabitia, skóre, čas, výhra a smrť. Po hre zavolá `FirebaseService.SaveScore`.
- `SurvivalSpawner`: vlny nepriateľov na bodoch `x` v aréne. Každé 2 vlny pribudne silnejší typ, nepriatelia majú v každej vlne o 8 % viac životov.
- Zbrane (`WeaponSystem.weapons`): 0 päsť, 1 pištoľ, 2 brokovnica, 3 guľomet, 4 raketomet (strieľa `Projectile` s výbuchom).
- Nepriatelia (`Enemy`): zombie (strieľa), imp (ohnivé gule), démon (zblízka), ťažký strelec (dávky po 4 výstreloch, veľa životov). Po zásahu sa môžu na chvíľu zastaviť (`painChance`), zombie a ťažký strelec pustia náboje (`dropPrefab`). Keď nepriateľa zraní iný nepriateľ (ohnivá guľa, výbuch), na chvíľu bojuje s ním. Projektily preletia cez nepriateľov rovnakého druhu.
- `Barrel`: výbušný sud, po zásahoch vybuchne (`Explosion.Blast`) a zraní všetko v okruhu, aj hráča a iné sudy.
- `Secret`: tajné miesto za tajnými dverami. Pri vstupe `GameManager.FoundSecret()` (+500 bodov).
- `IDamageable.TakeDamage(amount, point, normal, attacker)`: `attacker` je ten, kto zranil (hráč, nepriateľ alebo null).
- `DebugHUD`: **dočasné** HUD na testovanie. Marco ho nahradí vlastným UI.

### Napojenie pre UI (Marco)
- Spustenie hry: `GameSession.Mode = GameMode.Campaign` (alebo `Survival`), `GameSession.CurrentLevel = n`, potom `SceneManager.LoadScene("Game")`.
- Stav: `GameManager.Instance` (`Kills`, `TotalEnemies`, `Score`, `PlayTime`, `Wave`, `SecretsFound`, `TotalSecrets`, `CurrentState`, eventy `Won` a `Lost`), `Restart()`, `NextLevel()`.
- Hráč: `PlayerHealth.Instance` (`health`, `armor`, event `Damaged`), `PlayerInventory` (munícia `Bullets`/`Shells`/`Rockets`, kľúče, event `Changed`), `WeaponSystem` (`CurrentWeapon`, eventy `Fired` a `Switched`).
- Správy: `Door.NeedKey`, `Pickup.Collected`, `Secret.Found` („Našiel si tajné miesto!“), `SurvivalSpawner.WaveStarted`.
- Pauza: `Time.timeScale = 0`, `PlayerController.LockCursor(false)`.
- Firebase: `FirebaseService.LoadTop10(FirebaseService.Campaign | Survival, callback)`. Dáta sú v `doom/scores/<režim>`.

### Napojenie pre grafiku (Samo)
- Vymeň `wallPrefab`, `floorPrefab` a voliteľný `ceilingPrefab` na objekte `Level`. Tajné dvere (`SecretDoor`) majú vyzerať ako stena.
- Modely zbraní: `WeaponSystem.viewModels` (index = poradie zbrane, 0–4).
- Efekty: `WeaponSystem.Hit` (bod, normála, zasiahnutý nepriateľ), `Projectile.Impact`, `Explosion.Exploded` (bod, polomer), `Barrel.Exploding`, `Enemy.Attacked`, `Enemy.Hurt`, `Enemy.Killed`, `Door.Opened`, `Secret.Found`.
- Nepriatelia: prefaby `Enemy_*`. Model daj ako dieťa a kapsulu `Body` skry alebo vymeň (animácia útoku škáluje objekt `Body`).
- Sud (`Barrel`) a predmety (`Pickup_*`): model je dieťa `Model`, ten môžeš vymeniť. Collidery na koreni nemeň. Raketa (`Rocket`) a ohnivá guľa (`Fireball`) sú guľa so svetlom, model daj ako dieťa a MeshRenderer gule vypni.

## Firebase
`Assets/google-services.json` a `Assets/StreamingAssets/google-services-desktop.json` sú prevzaté z Car Race Master (rovnaký Firebase projekt). Repo musí ostať **súkromné**. Skontrolujte pravidlá databázy (Realtime Database > Rules).

## Poznámka
V projekte je vypnutý reload domény (Enter Play Mode Options). Každé statické pole a event preto treba vynulovať cez `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`.
