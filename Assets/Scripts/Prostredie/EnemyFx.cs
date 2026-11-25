using UnityEngine;

// Samo: na prefaboch Enemy_*. Eventy nepriateľa sú inštančné, preto ich posiela do Efekty.
[RequireComponent(typeof(Enemy))]
public class EnemyFx : MonoBehaviour
{
    public Transform muzzle;   // koniec zbrane (strelci), inak oko

    void Awake()
    {
        Enemy e = GetComponent<Enemy>();
        e.Attacked += a => { if (Efekty.Instance) Efekty.Instance.EnemyAttacked(a, muzzle ? muzzle : a.eye); };
        e.Hurt += a => { if (Efekty.Instance) Efekty.Instance.EnemyHurt(a); };
    }
}
