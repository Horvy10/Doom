# Doom (Unity klon)

Školský tímový projekt: klon hry Doom (1993) v Unity 6 (`6000.6.2f1`, URP), skóre sa ukladá do Firebase Realtime Database.
Štruktúra je rovnaká ako v [car-race-master](https://github.com/Horvy10/car-race-master).

## Režimy
- **Kampaň**: prejdi level, nájdi kľúče, otvor dvere a vojdi do zeleného východu. Skóre = zabitia + bonus za čas.
- **Survival**: aréna, vlny nepriateľov, ktoré sú čoraz väčšie. Hra končí smrťou. Skóre = zabitia.

## Ovládanie
WASD pohyb, Shift beh, myš mierenie, ľavé tlačidlo streľba, 1–4 alebo koliesko výber zbrane, E otvorenie dverí, Esc pauza.

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
- `LevelMaps`: mapy levelov ako text (legenda je v súbore). Nový level = nová textová mapa.
- `LevelBuilder`: pri štarte postaví level z mapy a upečie NavMesh. Prefaby stien a podlahy sú kocky 1×1×1, builder ich naškáluje na bunku 4 m.
- `GameManager`: zabitia, skóre, čas, výhra a smrť. Po hre zavolá `FirebaseService.SaveScore`.
- `SurvivalSpawner`: vlny nepriateľov na bodoch `x` v aréne.
- `DebugHUD`: **dočasné** HUD na testovanie. Marco ho nahradí vlastným UI.

### Napojenie pre UI (Marco)
- Spustenie hry: `GameSession.Mode = GameMode.Campaign` (alebo `Survival`), `GameSession.CurrentLevel = n`, potom `SceneManager.LoadScene("Game")`.
- Stav: `GameManager.Instance` (`Kills`, `TotalEnemies`, `Score`, `PlayTime`, `Wave`, `CurrentState`, eventy `Won` a `Lost`), `Restart()`, `NextLevel()`.
- Hráč: `PlayerHealth.Instance` (`health`, `armor`, event `Damaged`), `PlayerInventory` (munícia, kľúče, event `Changed`), `WeaponSystem` (`CurrentWeapon`, eventy `Fired` a `Switched`).
- Správy: `Door.NeedKey`, `Pickup.Collected`.
- Pauza: `Time.timeScale = 0`, `PlayerController.LockCursor(false)`.
- Firebase: `FirebaseService.LoadTop10(FirebaseService.Campaign | Survival, callback)`. Dáta sú v `doom/scores/<režim>`.

### Napojenie pre grafiku (Samo)
- Vymeň `wallPrefab`, `floorPrefab` a voliteľný `ceilingPrefab` na objekte `Level`.
- Modely zbraní: `WeaponSystem.viewModels` (index = poradie zbrane).
- Efekty: `WeaponSystem.Hit` (bod, normála, zasiahnutý nepriateľ), `Projectile.Impact`, `Enemy.Attacked`, `Enemy.Hurt`, `Enemy.Killed`.
- Nepriatelia: prefaby `Enemy_*`. Model daj ako dieťa a kapsulu `Body` skry alebo vymeň (animácia útoku škáluje objekt `Body`).

## Firebase
`Assets/google-services.json` a `Assets/StreamingAssets/google-services-desktop.json` sú prevzaté z Car Race Master (rovnaký Firebase projekt). Repo musí ostať **súkromné**. Skontrolujte pravidlá databázy (Realtime Database > Rules).

## Poznámka
V projekte je vypnutý reload domény (Enter Play Mode Options). Každé statické pole a event preto treba vynulovať cez `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`.
