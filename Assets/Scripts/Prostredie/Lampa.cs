using UnityEngine;

// Samo: dieťa "Lamp" na prefabe stropu. Svieti len každá 4. bunka (vzor platí aj pre úzke chodby
// v oboch smeroch), niektoré lampy sú studené a niektoré blikajú. Ostatné bunky lampu zmažú.
public class Lampa : MonoBehaviour
{
    public Material coldMaterial;
    public Color coldColor = new Color(0.35f, 0.85f, 1f);

    Light lamp;
    float baseIntensity;
    float seed;

    void Awake()
    {
        Transform child = transform.Find("Lamp");
        if (child == null) { enabled = false; return; }

        Vector3 p = transform.position;
        int cx = Mathf.RoundToInt(p.x / 4f), cz = Mathf.RoundToInt(p.z / 4f);
        if (((cx + 3 * cz) % 4 + 4) % 4 != 0)
        {
            Destroy(child.gameObject);
            enabled = false;
            return;
        }

        int h = Varianta.Hash(cx, cz);
        lamp = child.GetComponentInChildren<Light>();
        if (h % 5 == 0)
        {
            lamp.color = coldColor;
            if (coldMaterial) child.GetComponent<Renderer>().sharedMaterial = coldMaterial;
        }
        baseIntensity = lamp.intensity;
        seed = h % 100;
        if (h % 9 != 4) enabled = false;   // bliká len asi každá deviata
    }

    void Update()
    {
        lamp.intensity = Mathf.PerlinNoise(Time.time * 6f, seed) > 0.35f ? baseIntensity : baseIntensity * 0.15f;
    }
}
