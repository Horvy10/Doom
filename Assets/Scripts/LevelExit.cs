using UnityEngine;

// Východ z levelu: hráč doň vojde a level je hotový
public class LevelExit : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerHealth>() && GameManager.Instance)
            GameManager.Instance.Win();
    }
}
