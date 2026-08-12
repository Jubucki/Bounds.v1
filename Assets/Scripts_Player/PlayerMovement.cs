using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerMovement2D : MonoBehaviour
{
    [Header("Ground / Wall Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private Transform wallCheckRight;
    [SerializeField] private Transform wallCheckLeft;
    [SerializeField] private float wallCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;

    [Header("Run")]
    [SerializeField] private float walkSpeed = 8f;
    [SerializeField] private float sprintSpeed = 12f;
    [SerializeField] private float groundAcceleration = 60f;
    [SerializeField] private float groundDeceleration = 70f;
    [SerializeField] private float airAcceleration = 45f;
    [SerializeField] private float airDeceleration = 35f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 15f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.12f;
    [SerializeField] private float fallGravityMult = 2.5f;
    [SerializeField] private float lowJumpGravityMult = 3.5f;
    [SerializeField] private float normalGravityScale = 1f;

    [Header("Wall")]
    [SerializeField] private float wallStickTime = 0.5f;      // how long you hang before slipping
    [SerializeField] private float wallSlideStartSpeed = -1f; // near-frozen fall speed while stuck
    [SerializeField] private float wallSlideMaxSpeed = -7f;   // fastest you'll slide once slipping
    [SerializeField] private float wallSlideAcceleration = 10f;
    [SerializeField] private Vector2 wallJumpForce = new Vector2(13f, 16f);
    [SerializeField] private float wallJumpInputLockTime = 0.15f;

    [Header("Slide (momentum)")]
    [SerializeField] private float slideMinEntrySpeed = 9f;
    [SerializeField] private float slideFriction = 22f;
    [SerializeField] private float slideEndSpeed = 3f;
    [SerializeField] private float slideColliderHeight = 0.5f;

    private Rigidbody2D rb;
    private BoxCollider2D col;
    private float originalColliderHeight;
    private Vector2 originalColliderOffset;

    // input
    private float moveInput;
    private bool sprintHeld;
    private bool jumpHeld;
    private bool slideHeld;
    private float jumpBufferCounter;

    // detection state
    private bool isGrounded;
    private bool isTouchingWall;
    private int wallSide; // 1 = wall on right, -1 = wall on left, 0 = none
    private int facing = 1;

    // timers
    private float coyoteCounter;
    private float wallStickCounter;
    private float wallSlideSpeed;
    private float wallJumpLockCounter;

    // slide state
    private bool isSliding;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        rb.freezeRotation = true;
        originalColliderHeight = col.size.y;
        originalColliderOffset = col.offset;
        wallSlideSpeed = wallSlideStartSpeed;
    }

    void Update()
    {
        ReadInput();
        CheckSurroundings();
        UpdateTimers();
        DetermineSlideState();
    }

    void FixedUpdate()
    {
        if (wallJumpLockCounter > 0f)
        {
            wallJumpLockCounter -= Time.fixedDeltaTime;
        }
        else if (isSliding)
        {
            ApplySlideFriction();
        }
        else
        {
            HandleHorizontalMovement();
        }

        HandleWallSlide();
        HandleJump();
        HandleGravity();
    }

    // ---------------- INPUT ----------------

    void ReadInput()
    {
        moveInput = Input.GetAxisRaw("Horizontal");
        sprintHeld = Input.GetKey(KeyCode.LeftShift);
        slideHeld = Input.GetKey(KeyCode.LeftControl);
        jumpHeld = Input.GetButton("Jump");

        if (Input.GetButtonDown("Jump"))
            jumpBufferCounter = jumpBufferTime;

        if (moveInput > 0f) facing = 1;
        else if (moveInput < 0f) facing = -1;
    }

    void UpdateTimers()
    {
        jumpBufferCounter -= Time.deltaTime;

        if (isGrounded) coyoteCounter = coyoteTime;
        else coyoteCounter -= Time.deltaTime;
    }

    // ---------------- DETECTION ----------------

    void CheckSurroundings()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        bool wallRight = Physics2D.OverlapCircle(wallCheckRight.position, wallCheckRadius, wallLayer);
        bool wallLeft = Physics2D.OverlapCircle(wallCheckLeft.position, wallCheckRadius, wallLayer);

        if (wallRight && !isGrounded)
        {
            isTouchingWall = true;
            wallSide = 1;
        }
        else if (wallLeft && !isGrounded)
        {
            isTouchingWall = true;
            wallSide = -1;
        }
        else
        {
            isTouchingWall = false;
            wallSide = 0;
        }

        // reset the stick timer whenever we're not actively on a wall
        if (!isTouchingWall)
        {
            wallStickCounter = wallStickTime;
            wallSlideSpeed = wallSlideStartSpeed;
        }
    }

    // ---------------- GROUND / AIR MOVEMENT ----------------

    void HandleHorizontalMovement()
    {
        float targetSpeed = moveInput * (sprintHeld ? sprintSpeed : walkSpeed);
        float accelRate;

        if (isGrounded)
            accelRate = Mathf.Abs(targetSpeed) > 0.01f ? groundAcceleration : groundDeceleration;
        else
            accelRate = Mathf.Abs(targetSpeed) > 0.01f ? airAcceleration : airDeceleration;

        float speedDiff = targetSpeed - rb.linearVelocity.x;
        float movement = speedDiff * accelRate * Time.fixedDeltaTime;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x + movement, rb.linearVelocity.y);
    }

    // ---------------- WALL SLIDE (stick, then slip) ----------------

    void HandleWallSlide()
    {
        if (wallJumpLockCounter > 0f) return;
        if (!isTouchingWall || isGrounded) return;

        bool holdingIntoWall = (wallSide == 1 && moveInput > 0f) || (wallSide == -1 && moveInput < 0f);
        if (!holdingIntoWall)
        {
            wallStickCounter = 0f; // let go = stop grabbing, fall normally
            return;
        }

        if (wallStickCounter > 0f)
        {
            wallStickCounter -= Time.fixedDeltaTime;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, wallSlideStartSpeed));
        }
        else
        {
            wallSlideSpeed = Mathf.MoveTowards(wallSlideSpeed, wallSlideMaxSpeed, wallSlideAcceleration * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, wallSlideSpeed));
        }
    }

    // ---------------- JUMP (ground + wall) ----------------

    void HandleJump()
    {
        bool canWallJump = jumpBufferCounter > 0f && isTouchingWall && !isGrounded;
        bool canGroundJump = jumpBufferCounter > 0f && coyoteCounter > 0f;

        if (canWallJump)
        {
            rb.linearVelocity = new Vector2(wallJumpForce.x * -wallSide, wallJumpForce.y);
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
            wallJumpLockCounter = wallJumpInputLockTime;
            wallStickCounter = 0f;
            isTouchingWall = false; // avoid instant re-clamp this physics step
        }
        else if (canGroundJump)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }

        // variable jump height: cut the rise short if the button is released early
        if (!jumpHeld && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpGravityMult - 1f) * Time.fixedDeltaTime;
        }
    }

    // ---------------- GRAVITY FEEL ----------------

    void HandleGravity()
    {
        if (isTouchingWall && !isGrounded && wallJumpLockCounter <= 0f)
        {
            rb.gravityScale = normalGravityScale; // wall slide handles fall speed via clamping instead
            return;
        }

        rb.gravityScale = rb.linearVelocity.y < 0f
        ? normalGravityScale * fallGravityMult
        : normalGravityScale;
    }

    // ---------------- MOMENTUM SLIDE ----------------

    void DetermineSlideState()
    {
        if (!isSliding)
        {
            if (isGrounded && slideHeld && Mathf.Abs(rb.linearVelocity.x) >= slideMinEntrySpeed)
                StartSlide();
        }
        else
        {
            if (!slideHeld || !isGrounded || Mathf.Abs(rb.linearVelocity.x) <= slideEndSpeed)
                EndSlide();
        }
    }

    void StartSlide()
    {
        isSliding = true;
        col.size = new Vector2(col.size.x, slideColliderHeight);
        col.offset = new Vector2(
            originalColliderOffset.x,
            originalColliderOffset.y - (originalColliderHeight - slideColliderHeight) / 2f
        );
    }

    void EndSlide()
    {
        isSliding = false;
        col.size = new Vector2(col.size.x, originalColliderHeight);
        col.offset = originalColliderOffset;
    }

    void ApplySlideFriction()
    {
        float decel = slideFriction * Time.fixedDeltaTime;
        float newX = Mathf.MoveTowards(rb.linearVelocity.x, 0f, decel);
        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
    }

    // ---------------- DEBUG ----------------

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        if (groundCheck) Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        Gizmos.color = Color.cyan;
        if (wallCheckRight) Gizmos.DrawWireSphere(wallCheckRight.position, wallCheckRadius);
        if (wallCheckLeft) Gizmos.DrawWireSphere(wallCheckLeft.position, wallCheckRadius);
    }
}
