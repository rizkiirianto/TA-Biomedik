using System.Drawing;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private float horizontalInput;
    private bool isSprinting;

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 1.5f;

    [SerializeField] public Animator myAnimator;
    [SerializeField] private float baseScale = 0.1f;

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
        isSprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        // Set isRunning to true if horizontalInput is not 0
        myAnimator.SetBool("isRunning", horizontalInput != 0);
        // Handle Sprite Flipping based on your asset scale (0.1)
        if (horizontalInput > 0)
            transform.localScale = new Vector3(baseScale - 0.01f, baseScale, baseScale);
        else if (horizontalInput < 0)
            transform.localScale = new Vector3(-baseScale-0.01f, baseScale, baseScale);
    }

    void FixedUpdate()
    {
        // Only use horizontalInput, set Y to 0
        Vector2 moveDirection = new Vector2(horizontalInput, 0f);

        // Normalize if you want to keep it consistent (though less critical for 1D)
        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection = moveDirection.normalized;
        }

        float speed = isSprinting ? moveSpeed * sprintMultiplier : moveSpeed;
        rb.linearVelocity = moveDirection * speed;
    }
    
}
