using UnityEngine;

public class FirstPersonController : MonoBehaviour
{
    public GameObject AntiPlayer;

    public float MouseSensitivity = 100.0f;

    public Transform PlayerTransform;

    public bool LockCamera = true;
    private float rotationX = 0.0f;

    // Mouse deltas are already per-frame; the old code multiplied by Time.deltaTime,
    // which made the look speed frame-rate dependent. This factor keeps the same
    // feel the game had at ~60 fps with the serialized sensitivity values.
    private const float SensitivityScale = 1f / 60f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        HideOwnHead();
    }

    void Update()
    {
        if (!LockCamera)
        {
            float mouseX = Input.GetAxis("Mouse X") * MouseSensitivity * SensitivityScale;
            float mouseY = Input.GetAxis("Mouse Y") * MouseSensitivity * SensitivityScale;

            rotationX -= mouseY;
            rotationX = Mathf.Clamp(rotationX, -90.0f, 90.0f);

            transform.localRotation = Quaternion.Euler(rotationX, 0.0f, 0.0f);
            PlayerTransform.Rotate(Vector3.up * mouseX);
        }
    }

    public void LookAtAntiPlayer()
    {
        Vector3 position = transform.position;
        Vector3 antiPlayerPosition = AntiPlayer.transform.position;
        Quaternion rotation = Quaternion.LookRotation(antiPlayerPosition - position);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, 0.25f * Time.deltaTime);
        PlayerTransform.rotation = Quaternion.Slerp(PlayerTransform.rotation, rotation, 1f * Time.deltaTime);
    }

    public void ResetRotation()
    {
        PlayerTransform.rotation = Quaternion.identity;
    }

    // Shrink the player's own head bone so the face never clips into the
    // first-person camera during walk animations. Only affects this instance;
    // the AntiPlayer keeps its head.
    private void HideOwnHead()
    {
        foreach (Transform t in PlayerTransform.GetComponentsInChildren<Transform>())
        {
            if (t.name.EndsWith("_head"))
            {
                t.localScale = Vector3.one * 0.0001f;
                break;
            }
        }
    }
}
