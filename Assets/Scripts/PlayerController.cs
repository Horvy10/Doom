using UnityEngine;

// FPS pohyb: WASD, Shift = beh, myš = rozhliadanie. Bez skákania (ako pôvodný Doom).
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float walkSpeed = 7f;
    public float runSpeed = 11f;
    public float mouseSensitivity = 2f;
    public float gravity = -25f;
    public Transform cameraPivot;

    // UI (pauza, koniec hry) zamkne ovládanie
    public static bool InputLocked;

    // V projekte je vypnutý reload domény, statické polia treba pri každom Play vynulovať
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { InputLocked = false; }

    CharacterController cc;
    float pitch;
    float verticalSpeed;

    public static void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    void Start()
    {
        cc = GetComponent<CharacterController>();
        if (cameraPivot == null) cameraPivot = GetComponentInChildren<Camera>().transform;
        LockCursor(true);
    }

    void Update()
    {
        if (InputLocked || Time.timeScale == 0f) return;

        // rozhliadanie
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity;
        transform.Rotate(0f, mx, 0f);
        pitch = Mathf.Clamp(pitch - my, -80f, 80f);
        cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);

        // pohyb
        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        input = Vector3.ClampMagnitude(input, 1f);
        float speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        Vector3 move = transform.TransformDirection(input) * speed;

        if (cc.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
        verticalSpeed += gravity * Time.deltaTime;
        move.y = verticalSpeed;

        cc.Move(move * Time.deltaTime);
    }
}
