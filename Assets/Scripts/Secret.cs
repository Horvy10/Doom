using System;
using System.Collections.Generic;
using UnityEngine;

// Tajné miesto ('!' v mape): neviditeľný trigger. Keď doň hráč vojde, počíta sa ako nájdené tajomstvo (bonus k skóre).
// Zvyčajne je za tajnými dverami ('=' v mape), ktoré vyzerajú ako stena a otvárajú sa klávesom E.
public class Secret : MonoBehaviour
{
    public static readonly List<Secret> All = new List<Secret>();
    public static event Action<Secret> Found;   // HUD: "Našiel si tajné miesto!", zvuk

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { All.Clear(); Found = null; }

    void Awake() { All.Add(this); }

    void OnDestroy() { All.Remove(this); }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerHealth>() == null) return;
        if (GameManager.Instance) GameManager.Instance.FoundSecret();
        Found?.Invoke(this);
        Destroy(gameObject);
    }
}
