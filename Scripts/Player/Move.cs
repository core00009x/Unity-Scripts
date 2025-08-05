using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Move : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 6f;
    public float sprintSpeed = 10f;
    public float acceleration = 20f;
    public float deceleration = 25f;
    public AnimationCurve speedCurve;

    [Header("Jump Settings")]
    public float jumpHeight = 2.5f;
    public float gravity = -20f;
    public float coyoteTime = 0.2f;
    public float jumpBufferTime = 0.2f;

    [Header("Dash Settings")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    [Header("Wall Jump Settings")]
    public float wallJumpForce = 8f;
    public LayerMask wallLayer;
    public float wallCheckDistance = 0.6f;

    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 moveInput;
    private float currentSpeed;
    private float verticalVelocity;
    private float lastGroundedTime;
    private float lastJumpPressedTime;
    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        HandleInput();
        HandleMovement();
        HandleJump();
        HandleDash();
        ApplyGravity();
        CheckLedgeGrab();
        CheckVault();
        HandleLedgeClimb();
        HandleVault();
        HandleCrouch();

        controller.Move((velocity + Vector3.up * verticalVelocity) * Time.deltaTime);      
    }

    void HandleInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        moveInput = new Vector3(h, 0, v).normalized;

        if (Input.GetButtonDown("Jump"))
            lastJumpPressedTime = Time.time;

        if (Input.GetKeyDown(KeyCode.LeftControl) && dashCooldownTimer <= 0)
        {
            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
        }
    }

    void HandleMovement()
    {
        Vector3 targetDirection = transform.TransformDirection(moveInput);
        float targetSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;

        float speedFactor = speedCurve.Evaluate(moveInput.magnitude);
        targetSpeed *= speedFactor;

        if (moveInput.magnitude > 0)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0, deceleration * Time.deltaTime);
        }

        velocity = targetDirection * currentSpeed;
    }

    void HandleDash()
    {
        if (isDashing)
        {
            velocity = transform.forward * dashSpeed;
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0)
                isDashing = false;
        }

        if (dashCooldownTimer > 0)
            dashCooldownTimer -= Time.deltaTime;
    }

    void ApplyGravity()
    {
        if (controller.isGrounded && verticalVelocity < 0)
            verticalVelocity = -2f;
        else
            verticalVelocity += gravity * Time.deltaTime;
    }

    bool IsTouchingWall()
    {
        return Physics.Raycast(transform.position, transform.right, wallCheckDistance, wallLayer) ||
               Physics.Raycast(transform.position, -transform.right, wallCheckDistance, wallLayer);
    }

    Vector3 GetWallNormal()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.right, out hit, wallCheckDistance, wallLayer))
            return hit.normal;
        if (Physics.Raycast(transform.position, -transform.right, out hit, wallCheckDistance, wallLayer))
            return hit.normal;
        return Vector3.zero;
    }
    
    [Header("Ledge Grab Settings")]
    public float ledgeCheckDistance = 1f;
    public float ledgeHeightOffset = 1.5f;
    public float climbDuration = 0.5f;
    public LayerMask ledgeLayer;

    private bool isGrabbingLedge;
    private Vector3 ledgePosition;
    private float climbTimer;

    void CheckLedgeGrab()
    {
        if (isGrabbingLedge || controller.isGrounded || verticalVelocity > 0) return;

        Vector3 origin = transform.position + Vector3.up * ledgeHeightOffset;
        if (Physics.Raycast(transform.position, transform.forward, ledgeCheckDistance, ledgeLayer) &&
            !Physics.Raycast(origin, transform.forward, ledgeCheckDistance, ledgeLayer))
        {
            isGrabbingLedge = true;
            ledgePosition = transform.position + transform.forward * ledgeCheckDistance;
            climbTimer = climbDuration;
            velocity = Vector3.zero;
            verticalVelocity = 0;
        }
    }

    void HandleLedgeClimb()
    {
        if (!isGrabbingLedge) return;

        climbTimer -= Time.deltaTime;
        if (climbTimer <= 0)
        {
            transform.position = ledgePosition + Vector3.up * 1.5f;
            isGrabbingLedge = false;
        }
    }

    [Header("Double Jump Settings")]
    public int maxJumps = 2;
    private int jumpCount;

    void HandleJump()
    {
        bool isGrounded = controller.isGrounded;

        if (isGrounded)
        {
            lastGroundedTime = Time.time;
            jumpCount = 0;
        }

        if ((Time.time - lastGroundedTime <= coyoteTime || jumpCount < maxJumps) &&
            Time.time - lastJumpPressedTime <= jumpBufferTime)
        {
            verticalVelocity = Mathf.Sqrt(-2f * gravity * jumpHeight);
            lastJumpPressedTime = -1f;
            jumpCount++;
        }

        if (IsTouchingWall() && Input.GetButtonDown("Jump"))
        {
            Vector3 wallNormal = GetWallNormal();
            velocity = wallNormal * wallJumpForce;
            verticalVelocity = Mathf.Sqrt(-2f * gravity * jumpHeight);
            jumpCount = 1; // Reset after wall jump
        }
    }

    [Header("Vault Settings")]
    public float vaultCheckDistance = 1f;
    public float vaultHeight = 1f;
    public float vaultSpeed = 5f;
    public LayerMask vaultLayer;

    private bool isVaulting;
    private Vector3 vaultTarget;

    void CheckVault()
    {
        if (isVaulting || moveInput.z <= 0) return;

        Vector3 origin = transform.position + Vector3.up * 0.5f;
        if (Physics.Raycast(origin, transform.forward, out RaycastHit hit, vaultCheckDistance, vaultLayer))
        {
            if (!Physics.Raycast(origin + Vector3.up * vaultHeight, transform.forward, vaultCheckDistance, vaultLayer))
            {
                isVaulting = true;
                vaultTarget = transform.position + transform.forward * (hit.distance + 0.5f);
            }
        }
    }

    void HandleVault()
    {
        if (!isVaulting) return;

        transform.position = Vector3.MoveTowards(transform.position, vaultTarget, vaultSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, vaultTarget) < 0.1f)
            isVaulting = false;
    }
    
    [Header("Crouch Settings")]
    public KeyCode crouchKey = KeyCode.LeftControl;
    public float crouchHeight = 1f;
    public float standingHeight = 2f;
    public float crouchSpeed = 3f;
    public float crouchTransitionSpeed = 8f;
    public LayerMask ceilingLayer;

    private bool isCrouching;
    private float targetHeight;

    void HandleCrouch()
    {
        if (Input.GetKeyDown(crouchKey))
        {
            isCrouching = !isCrouching;
        }

        // Prevent standing if there's a ceiling
        if (isCrouching == false && Physics.Raycast(transform.position, Vector3.up, standingHeight - crouchHeight + 0.1f, ceilingLayer))
        {
            isCrouching = true;
        }

        targetHeight = isCrouching ? crouchHeight : standingHeight;
        controller.height = Mathf.Lerp(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);

        // Adjust center to keep feet grounded
        controller.center = new Vector3(0, controller.height / 2f, 0);

        // Adjust movement speed
        if (isCrouching)
            currentSpeed = Mathf.Min(currentSpeed, crouchSpeed);
    }
}
