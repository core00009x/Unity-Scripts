using UnityEngine;
using UnityEngine.UI;

public class MouseSystem : MonoBehaviour
{
    [Header("Sensitivity Settings")]
    public float mouseSensitivityX = 10f;
    public float mouseSensitivityY = 10f;
    public float gamepadSensitivityX = 150f;
    public float gamepadSensitivityY = 150f;
    public float smoothing = 2f;
    public float acceleration = 0.05f;

    [Header("Clamp & Invert")]
    public float minY = -80f;
    public float maxY = 80f;
    public bool invertY = false;

    [Header("References")]
    public Transform playerBody;
    public bool lockCursor = true;

    [Header("UI Controls (Optional)")]
    public Slider sensitivitySliderX;
    public Slider sensitivitySliderY;
    public Toggle invertYToggle;

    [Header("Camera Shake")]
    public float recoilAmount = 5f;
    public float recoilRecoverySpeed = 10f;

    private Vector2 mouseDelta;
    private Vector2 smoothMouse;
    private float xRotation = 0f;
    private float recoilOffset = 0f;

    void Start()
    {
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Optional UI bindings
        if (sensitivitySliderX) sensitivitySliderX.onValueChanged.AddListener(val => mouseSensitivityX = val);
        if (sensitivitySliderY) sensitivitySliderY.onValueChanged.AddListener(val => mouseSensitivityY = val);
        if (invertYToggle) invertYToggle.onValueChanged.AddListener(val => invertY = val);
    }

    void Update()
    {
        HandleLook();
        RecoverRecoil();
    }

    void HandleLook()
    {
        Vector2 rawMouse = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        Vector2 gamepadInput = new Vector2(
            Input.GetAxis("RightStickHorizontal"),
            Input.GetAxis("RightStickVertical")
        );

        Vector2 input = rawMouse * new Vector2(mouseSensitivityX, mouseSensitivityY) +
                        gamepadInput * new Vector2(gamepadSensitivityX, gamepadSensitivityY) * Time.deltaTime;

        mouseDelta = Vector2.Lerp(mouseDelta, input, acceleration);

        smoothMouse.x = Mathf.Lerp(smoothMouse.x, mouseDelta.x, 1f / smoothing);
        smoothMouse.y = Mathf.Lerp(smoothMouse.y, mouseDelta.y, 1f / smoothing);

        float verticalInput = invertY ? smoothMouse.y : -smoothMouse.y;

        xRotation += verticalInput + recoilOffset;
        xRotation = Mathf.Clamp(xRotation, minY, maxY);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        if (playerBody) playerBody.Rotate(Vector3.up * smoothMouse.x);
    }

    void RecoverRecoil()
    {
        recoilOffset = Mathf.Lerp(recoilOffset, 0f, recoilRecoverySpeed * Time.deltaTime);
    }

    public void ApplyRecoil(float amount)
    {
        recoilOffset -= amount;
    }
}
