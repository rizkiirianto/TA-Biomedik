using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // Movement constants
    private const float RUN_SCALE_REDUCTION = 0.01f;

    // Cached components
    private Rigidbody2D rb;
    
    // Input state
    private float horizontalInput;
    private bool isSprinting;
    private bool isMoving;
    private bool isWalking;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 1.5f;

    [Header("Visual Settings")]
    [SerializeField] private Animator myAnimator;
    [SerializeField] private float baseScale = 0.1f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f; 
        rb.freezeRotation = true;
    }

    void Update()
    {
        HandleInput();
        UpdateAnimations();
        UpdateScale();
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        isSprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        isMoving = Mathf.Abs(horizontalInput) > 0f;
        isWalking = isMoving && !isSprinting;
    }

    private void UpdateAnimations()
    {
        myAnimator.SetBool("isWalking", isWalking);
        myAnimator.SetBool("isRunning", isMoving && isSprinting);
    }

    private void UpdateScale()
    {
        Vector3 finalScale = CalculateScale();
        ApplySpriteDirection(finalScale);
    }

    private Vector3 CalculateScale()
    {
        if (isWalking)
        {
            return new Vector3(baseScale, baseScale, baseScale);
        }
        else
        {
            // Running/Idle: only reduce X axis
            return new Vector3(baseScale - RUN_SCALE_REDUCTION, baseScale, baseScale);
        }
    }

    private void ApplySpriteDirection(Vector3 scale)
    {
        if (horizontalInput > 0)
        {
            scale.x = Mathf.Abs(scale.x); // Face right
        }
        else if (horizontalInput < 0)
        {
            scale.x = -Mathf.Abs(scale.x); // Face left
        }
        else
        {
            // Maintain current direction when idle
            bool facingRight = transform.localScale.x > 0;
            scale.x = facingRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        }

        transform.localScale = scale;
    }

    private void HandleMovement()
    {
        Vector2 moveDirection = new Vector2(horizontalInput, 0f);

        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection = moveDirection.normalized;
        }

        float currentSpeed = isSprinting ? moveSpeed * sprintMultiplier : moveSpeed;
        rb.linearVelocity = moveDirection * currentSpeed;
    }
}