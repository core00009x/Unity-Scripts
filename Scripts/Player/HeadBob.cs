using UnityEngine;

public class HeadBob : MonoBehaviour
{
    [Header("Bob Settings")]
    public float walkBobSpeed = 14f;
    public float walkBobAmount = 0.05f;
    public float runBobSpeed = 18f;
    public float runBobAmount = 0.1f;
    public float idleSwayAmount = 0.01f;
    public float idleSwaySpeed = 1f;

    [Header("Jump/Land Settings")]
    public float jumpBobAmount = 0.1f;
    public float jumpBobSpeed = 6f;

    [Header("Tilt Settings")]
    public float tiltAmount = 2f; // degrees
    public float tiltSpeed = 4f;

    [Header("References")]
    public Transform cameraHolder;
    public CharacterController controller;
    public KeyCode runKey = KeyCode.LeftShift;

    private float bobTimer = 0f;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private bool wasGroundedLastFrame = true;
    private float jumpBobTimer = 0f;
    private bool isJumpBobbing = false;

    void Start()
    {
        if (cameraHolder == null) cameraHolder = transform;
        initialPosition = cameraHolder.localPosition;
        initialRotation = cameraHolder.localRotation;
    }

    void Update()
    {
        HandleHeadBob();
        HandleJumpBob();
        HandleTilt();
        wasGroundedLastFrame = controller.isGrounded;
    }

    void HandleHeadBob()
    {
        Vector3 velocity = controller.velocity;
        bool isMoving = velocity.magnitude > 0.1f && controller.isGrounded;
        bool isRunning = Input.GetKey(runKey);

        if (isMoving)
        {
            float speed = isRunning ? runBobSpeed : walkBobSpeed;
            float amount = isRunning ? runBobAmount : walkBobAmount;

            bobTimer += Time.deltaTime * speed;
            float bobOffsetY = Mathf.Sin(bobTimer) * amount;
            float bobOffsetX = Mathf.Cos(bobTimer / 2f) * amount;

            cameraHolder.localPosition = initialPosition + new Vector3(bobOffsetX, bobOffsetY, 0);
        }
        else
        {
            bobTimer = 0f;

            float swayY = Mathf.Sin(Time.time * idleSwaySpeed) * idleSwayAmount;
            float swayX = Mathf.Cos(Time.time * idleSwaySpeed) * idleSwayAmount;
            cameraHolder.localPosition = Vector3.Lerp(cameraHolder.localPosition, initialPosition + new Vector3(swayX, swayY, 0), Time.deltaTime * 2f);
        }
    }

    void HandleJumpBob()
    {
        if (!wasGroundedLastFrame && controller.isGrounded)
        {
            // Landed
            isJumpBobbing = true;
            jumpBobTimer = 0f;
        }

        if (isJumpBobbing)
        {
            jumpBobTimer += Time.deltaTime * jumpBobSpeed;
            float bobOffsetY = Mathf.Sin(jumpBobTimer * Mathf.PI) * jumpBobAmount;

            cameraHolder.localPosition += new Vector3(0, -Mathf.Abs(bobOffsetY), 0);

            if (jumpBobTimer >= 1f)
            {
                isJumpBobbing = false;
            }
        }
    }

    void HandleTilt()
    {
        Vector3 velocity = controller.velocity;
        bool isMoving = velocity.magnitude > 0.1f && controller.isGrounded;

        float tilt = Mathf.Sin(bobTimer) * tiltAmount;
        Quaternion targetRotation = initialRotation * Quaternion.Euler(0, 0, -tilt);

        cameraHolder.localRotation = Quaternion.Lerp(cameraHolder.localRotation, targetRotation, Time.deltaTime * tiltSpeed);
    }
}
