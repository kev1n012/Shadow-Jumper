using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class characterMovement : MonoBehaviour
{
    [Header("Movement")]
    public float maxSpeed = 8f;
    public float acceleration = 15f;
    public float deceleration = 20f;

    [Header("Jumping")]
    public float jumpForce = 16f;
    public float jumpCutMultiplier = 0.4f;
    public float fallGravityMultiplier = 2.5f;
    public float jumpGravityMultiplier = 1.5f;

    Rigidbody2D rb;
    Vector2 move;
    bool isGrounded;
    bool jumpPressed;
    bool jumpReleased;
    bool isJumping;
    bool isFacingRight = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        move = new Vector2(Input.GetAxisRaw("Horizontal"), 0);

        if (Input.GetButtonDown("Jump")) jumpPressed = true;
        if (Input.GetButtonUp("Jump")) jumpReleased = true;

        isGrounded = false;
        ContactPoint2D[] contacts = new ContactPoint2D[10];
        rb.GetContacts(contacts);
        foreach (ContactPoint2D contact in contacts)
        {
            if (contact.normal.y > 0.7f)
            {
                isGrounded = true;
                break;
            }
        }
    }

    void FixedUpdate()
    {
        HandleMovement();
        HandleJump();
        HandleGravity();
    }

    void HandleMovement()
    {
        float targetSpeed = move.x * maxSpeed;
        float speedDiff = targetSpeed - rb.velocity.x;
        float rate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        float force = speedDiff * rate;
        force = Mathf.Clamp(force, -maxSpeed * rate, maxSpeed * rate);
        rb.AddForce(Vector2.right * force, ForceMode2D.Force);

        if (move.x > 0 && !isFacingRight) Flip();
        else if (move.x < 0 && isFacingRight) Flip();
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    void HandleJump()
    {
        if (jumpPressed && isGrounded && !isJumping)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            isJumping = true;
        }
        jumpPressed = false;

        if (jumpReleased && rb.velocity.y > 0)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * jumpCutMultiplier);
        }
        jumpReleased = false;

        if (isGrounded && rb.velocity.y <= 0)
        {
            isJumping = false;
        }
    }

    void HandleGravity()
    {
        if (rb.velocity.y < 0)
        {
            rb.gravityScale = fallGravityMultiplier;
        }
        else if (rb.velocity.y > 0 && !Input.GetButton("Jump"))
        {
            rb.gravityScale = jumpGravityMultiplier;
        }
        else
        {
            rb.gravityScale = 1f;
        }
    }
}