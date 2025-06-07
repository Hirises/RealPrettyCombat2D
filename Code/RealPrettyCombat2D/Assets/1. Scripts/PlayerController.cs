using Assets._1._Scripts;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;
using NaughtyAttributes;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private new Rigidbody2D rigidbody;
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private float movespeed;
    [SerializeField]
    private float sprintspeed;
    [SerializeField]
    private float jumpforce;
    [SerializeField]
    private int maxJumpCount;

    private InputAction moveAction;
    private InputAction sprintAction;

    [ReadOnly]
    private bool isGrounded;
    [ReadOnly]
    private int jumpCount;

    private void Awake()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        sprintAction = InputSystem.actions.FindAction("Sprint");
        SetGrounded(true);
    }

    void Update()
    {
        float speed = sprintAction.IsPressed() ? sprintspeed : movespeed;
        float velX = rigidbody.linearVelocityX = moveAction.ReadValue<float>() * speed;
        animator.SetBool("IsMove", velX != 0);
    }

    public void OnGroundedBoxEnter(Collider2D collision)
    {
        SetGrounded(true);
    }

    public void OnGroundedBoxExit(Collider2D collision)
    {
        SetGrounded(false);
    }

    private void SetGrounded(bool value)
    {
        this.isGrounded = value;
        if (this.isGrounded)
        {
            jumpCount = maxJumpCount;
        }
    }

    public void OnAttack(CallbackContext context)
    {
        if(context.phase == InputActionPhase.Performed)
        {
            //Button Down
        }else if(context.phase == InputActionPhase.Canceled)
        {
            //Button Up
        }
    }

    public void OnJump(CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            //Button Down
            if (jumpCount <= 0) return;

            rigidbody.linearVelocityY = jumpforce;

            jumpCount--;
        }
        else if (context.phase == InputActionPhase.Canceled)
        {
            //Button Up
        }
    }
}
