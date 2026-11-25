using UnityEngine;

// Samo: stena/podlaha si podľa svojej polohy vyberie jeden z materiálov, aby level nebol jednotvárny.
// Ten istý materiál môže byť v poli viackrát (= častejší).
public class Varianta : MonoBehaviour
{
    public Material[] materials;

    void Awake()
    {
        if (materials == null || materials.Length == 0) return;
        Vector3 p = transform.position;
        int h = Hash(Mathf.RoundToInt(p.x / 4f), Mathf.RoundToInt(p.z / 4f));
        GetComponent<Renderer>().sharedMaterial = materials[h % materials.Length];
    }

    // nezáporný hash bunky mriežky
    public static int Hash(int x, int z)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + z * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            return (int)((h ^ (h >> 16)) & 0x7fffffff);
        }
    }
}
