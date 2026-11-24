using System;
using UnityEngine;
using UnityEngine.AI;

// Dvere: hráč ich otvorí klávesom E zblízka. Dvere s kľúčom potrebujú kľúč danej farby.
// Zatvorené dvere vyrežú NavMesh (NavMeshObstacle), takže nepriatelia nimi neprejdú.
public class Door : MonoBehaviour
{
    public KeyColor requiredKey = KeyColor.None;
    public float openSpeed = 4f;
    public float useDistance = 2.5f;

    public static event Action<KeyColor> NeedKey;   // HUD: "Potrebuješ červený kľúč"
    public static event Action<Door> Opened;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { NeedKey = null; Opened = null; }

    Vector3 closedPos, openPos;
    bool open;
    Collider col;
    NavMeshObstacle obstacle;

    public bool IsOpen { get { return open; } }

    void Start()
    {
        closedPos = transform.position;
        openPos = closedPos + Vector3.up * (transform.lossyScale.y - 0.1f);
        col = GetComponent<Collider>();
        obstacle = GetComponent<NavMeshObstacle>();
    }

    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, open ? openPos : closedPos, openSpeed * Time.deltaTime);
        if (open || PlayerController.InputLocked || !Input.GetKeyDown(KeyCode.E)) return;

        PlayerHealth p = PlayerHealth.Instance;
        if (p == null || p.IsDead) return;
        Vector3 chest = p.transform.position + Vector3.up;
        if (Vector3.Distance(col.ClosestPoint(chest), chest) <= useDistance)
            TryOpen(p.GetComponent<PlayerInventory>());
    }

    public void TryOpen(PlayerInventory inv)
    {
        if (open) return;
        if (inv == null || !inv.HasKey(requiredKey))
        {
            NeedKey?.Invoke(requiredKey);
            return;
        }
        open = true;
        if (obstacle) obstacle.enabled = false;
        Opened?.Invoke(this);
    }
}
