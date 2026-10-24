using System.Collections.Generic;
using UnityEngine;
using Mirror;
public class FirstPersonMovement : NetworkBehaviour
{
    public float speed = 5;

    [Header("Running")]
    public bool canRun = true;
    public bool IsRunning { get; private set; }
    public float runSpeed = 9;
    public KeyCode runningKey = KeyCode.LeftShift;
    public bool local=false; 
    Rigidbody rigidbody;
    public Transform body;
    /// <summary> Functions to override movement speed. Will use the last added override. </summary>
    public List<System.Func<float>> speedOverrides = new List<System.Func<float>>();
    public override void OnStartLocalPlayer()
    {
        local = true;
    }

    private GroundCheck groundCheck;
    private Jump jump;
    private float lastJumpTime = -100f;

    [SerializeField] private LayerMask groundLayers = ~0;

    void Awake()
    {
       
        // Get the rigidbody on this.
        rigidbody = GetComponent<Rigidbody>();
        if (isLocalPlayer)
            local = true;

        groundCheck = GetComponentInChildren<GroundCheck>();
        jump = GetComponent<Jump>();

        if (jump != null)
            jump.Jumped += OnPlayerJumped;
    }
    
    void FixedUpdate()
    {
        //Debug.Log(local);
        // Update IsRunning from input.
        IsRunning = canRun && Input.GetKey(runningKey);
       
        // Get targetMovingSpeed.
        float targetMovingSpeed = IsRunning ? runSpeed : speed;
        if (speedOverrides.Count > 0)
        {
            targetMovingSpeed = speedOverrides[speedOverrides.Count - 1]();
        }

        // Get targetVelocity from input.
        Vector2 targetVelocity =new Vector2( Input.GetAxis("Horizontal") * targetMovingSpeed, Input.GetAxis("Vertical") * targetMovingSpeed);

        // Apply movement.


        Vector3 horizontalVelocity = body.rotation * new Vector3(
            targetVelocity.x,
            0f,
            targetVelocity.y
        );

        float verticalVelocity = rigidbody.linearVelocity.y;

        bool recentlyJumped = Time.time - lastJumpTime < 0.7f;

        if (groundCheck != null &&
            !groundCheck.isGrounded &&
            !recentlyJumped &&
            verticalVelocity > 0f)
        {
            verticalVelocity = 0f;
        }

        rigidbody.linearVelocity = new Vector3(
            horizontalVelocity.x,
            verticalVelocity,
            horizontalVelocity.z
        );
    }

    private void OnPlayerJumped()
    {
        lastJumpTime = Time.time;
    }

    private void OnDestroy()
    {
        if (jump != null)
            jump.Jumped -= OnPlayerJumped;
    }
}