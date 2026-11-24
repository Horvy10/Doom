using System;
using System.Collections.Generic;
using UnityEngine;

public enum AmmoType { None, Bullets, Shells }
public enum KeyColor { None, Red, Blue, Yellow }

// Munícia a kľúče hráča
public class PlayerInventory : MonoBehaviour
{
    public int bullets = 50;
    public int shells = 0;
    public int maxBullets = 200;
    public int maxShells = 50;

    readonly HashSet<KeyColor> keys = new HashSet<KeyColor>();

    public event Action Changed;

    public IEnumerable<KeyColor> Keys { get { return keys; } }

    public int Get(AmmoType type)
    {
        if (type == AmmoType.Bullets) return bullets;
        if (type == AmmoType.Shells) return shells;
        return 0;
    }

    public int Max(AmmoType type)
    {
        if (type == AmmoType.Bullets) return maxBullets;
        if (type == AmmoType.Shells) return maxShells;
        return 0;
    }

    void Set(AmmoType type, int value)
    {
        if (type == AmmoType.Bullets) bullets = value;
        else if (type == AmmoType.Shells) shells = value;
    }

    // vráti false, ak už je plné
    public bool AddAmmo(AmmoType type, int amount)
    {
        if (type == AmmoType.None || amount <= 0) return false;
        int before = Get(type);
        int after = Mathf.Min(Max(type), before + amount);
        if (after == before) return false;
        Set(type, after);
        Changed?.Invoke();
        return true;
    }

    public bool Use(AmmoType type, int amount)
    {
        if (type == AmmoType.None) return true;
        if (Get(type) < amount) return false;
        Set(type, Get(type) - amount);
        Changed?.Invoke();
        return true;
    }

    public bool HasKey(KeyColor key) { return key == KeyColor.None || keys.Contains(key); }

    public bool AddKey(KeyColor key)
    {
        if (key == KeyColor.None || !keys.Add(key)) return false;
        Changed?.Invoke();
        return true;
    }
}
