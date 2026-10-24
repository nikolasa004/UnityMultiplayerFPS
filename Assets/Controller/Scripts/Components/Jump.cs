using UnityEngine;

public class Jump : MonoBehaviour
{
    Rigidbody rigidbody;
    public float jumpStrength = 2;
    public event System.Action Jumped;
    private Animator animator;
    [SerializeField, Tooltip("Prevents jumping when the transform is in mid-air.")]
    GroundCheck groundCheck;

    private bool can_double_jump = false;

    void Reset()
    {
        // Try to get groundCheck.
        groundCheck = GetComponentInChildren<GroundCheck>();
    }

    void Awake()
    {
        // Get rigidbody.
        rigidbody = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
    }

    void LateUpdate()
    {
        if (!groundCheck || groundCheck.isGrounded)
        {
            can_double_jump = true;

            // Jump when the Jump button is pressed and we are on the ground.
            if (Input.GetButtonDown("Jump"))
            {
                rigidbody.AddForce(Vector3.up * 100 * jumpStrength);
                Jumped?.Invoke();
                animator.SetBool("Jump", true);
            }
            else
                animator.SetBool("Jump", false);
            
        }
        else
        {
            if (Input.GetButtonDown("Jump") && can_double_jump)
            {
                rigidbody.AddForce(Vector3.up * 100 * jumpStrength);
                can_double_jump = false;
                Jumped?.Invoke();
                animator.SetBool("Jump", true);
            }
            else
                animator.SetBool("Jump", false);
        }
    }
}
