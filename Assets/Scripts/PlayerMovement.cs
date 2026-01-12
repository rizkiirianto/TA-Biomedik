using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private float horizontalInput;
    private float verticalInput;
    private bool isSprinting;

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 1.5f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        // Konsep 2.5D 
        rb.gravityScale = 0f; 
        rb.freezeRotation = true;
    }

    // Update is called once per frame
    void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        isSprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (isSprinting) {
            Debug.Log("Shift dipencet");
        }
    }

    void FixedUpdate()
    {
        Vector2 Input = new Vector2(horizontalInput, verticalInput);

        // Normalize to keep consistent speed diagonally
        if (Input.sqrMagnitude > 1f)
            Input = Input.normalized;

        float speed = isSprinting ? moveSpeed * sprintMultiplier : moveSpeed;
        rb.linearVelocity = Input * speed;
    }

    
}
