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
    private ParticleSystem doubleJumpParticle;
    [SerializeField]
    private FSMRunner FSM;

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
    }

    private void Start()
    {
        SetGrounded(true);
        FSM.AddTriggerEvent("RunJump", OnJumpTrigged);
    }

    void Update()
    {
        float speed = sprintAction.IsPressed() ? sprintspeed : movespeed;
        float velX = moveAction.ReadValue<float>() * speed;
        if(FSM.GetBool("ApplyMovement")) rigidbody.linearVelocityX = velX;
        else rigidbody.linearVelocityX = 0;
        //animator.SetBool("IsMove", velX != 0);
        FSM.SetBool("IsMove", velX != 0);
        FSM.SetBool("IsSprint", sprintAction.IsPressed());
        //animator.SetBool("IsSprint", sprintAction.IsPressed());
        if (velX < 0) transform.localScale = new Vector3(-1 , 1, 1);
        if (velX > 0) transform.localScale = new Vector3(1, 1, 1);
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
        //animator.SetBool("IsGround", isGrounded);
        FSM.SetBool("IsGround", isGrounded);
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
            //animator.SetTrigger("NormalAttack");
            FSM.SetTrigger("NormalAttack");
        }
        else if(context.phase == InputActionPhase.Canceled)
        {
            //Button Up
        }
    }

    public void OnJumpTrigged()
    {
        Debug.Log("Jump Triggered");
        SetGrounded(false);
        rigidbody.linearVelocityY = jumpforce;

        if (jumpCount < maxJumpCount)  //더블점프 파티클
        {
            var particle = Instantiate(doubleJumpParticle);
            particle.transform.position = transform.position;
            particle.Play();
        }

        jumpCount--;
    }

    public void OnJump(CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            //Button Down
            if (jumpCount <= 0) return;

            FSM.SetTrigger("Jump");
            //animator.SetTrigger("Jump");
        }
        else if (context.phase == InputActionPhase.Canceled)
        {
            //Button Up
        }
    }
}
