using System;
using UnityEngine;

public enum PickupType { Health, Armor, Bullets, Shells, Key, Weapon }

// Predmet na zemi. Zoberie sa prejdením; ak hráč už má plno, ostane ležať.
public class Pickup : MonoBehaviour
{
    public PickupType type;
    public int amount = 25;
    public KeyColor key = KeyColor.None;   // pre type = Key
    public int weaponIndex = 2;            // pre type = Weapon (index vo WeaponSystem.weapons)

    public static event Action<Pickup> Collected;   // HUD správa, zvuk

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Collected = null; }

    Vector3 start;

    void Start() { start = transform.position; }

    void Update()
    {
        transform.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
        transform.position = start + Vector3.up * (Mathf.Sin(Time.time * 2f + start.x) * 0.15f);
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerInventory inv = other.GetComponentInParent<PlayerInventory>();
        if (inv == null) return;
        if (Apply(inv))
        {
            Collected?.Invoke(this);
            Destroy(gameObject);
        }
    }

    bool Apply(PlayerInventory inv)
    {
        int ammo = Mathf.Max(1, Mathf.RoundToInt(amount * GameSession.Current.ammoFactor));
        switch (type)
        {
            case PickupType.Health:
                PlayerHealth h = inv.GetComponent<PlayerHealth>();
                return h && h.Heal(amount);
            case PickupType.Armor:
                PlayerHealth a = inv.GetComponent<PlayerHealth>();
                return a && a.AddArmor(amount);
            case PickupType.Bullets: return inv.AddAmmo(AmmoType.Bullets, ammo);
            case PickupType.Shells: return inv.AddAmmo(AmmoType.Shells, ammo);
            case PickupType.Key: return inv.AddKey(key);
            case PickupType.Weapon:
                WeaponSystem ws = inv.GetComponent<WeaponSystem>();
                return ws && ws.Give(weaponIndex);
        }
        return false;
    }

    public string DisplayName
    {
        get
        {
            switch (type)
            {
                case PickupType.Health: return "lekárnička";
                case PickupType.Armor: return "brnenie";
                case PickupType.Bullets: return "náboje";
                case PickupType.Shells: return "náboje do brokovnice";
                case PickupType.Key: return KeyName(key) + " kľúč";
                case PickupType.Weapon: return "zbraň";
            }
            return type.ToString();
        }
    }

    public static string KeyName(KeyColor k)
    {
        switch (k)
        {
            case KeyColor.Red: return "červený";
            case KeyColor.Blue: return "modrý";
            case KeyColor.Yellow: return "žltý";
        }
        return "";
    }
}
