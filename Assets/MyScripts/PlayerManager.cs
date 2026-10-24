using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    [SerializeField] private GroundCheck ground;
    [SerializeField] private int max_health = 100;
    [SerializeField] private Behaviour[] disable_on_death;
    public GameObject spine;

    [Header("Hitboxes (drag existing colliders on animated bones here)")]
    [SerializeField] private Collider[] bodyHitboxes;
    [SerializeField] private Collider headHitbox;

    private Renderer[] characterRenderers;
    private bool[] originalRendererStates;

    public bool IsHeadHit(Collider hit)
    {
        return headHitbox != null && hit == headHitbox;
    }

    public bool IsBodyHit(Collider hit)
    {
        if (hit == null || bodyHitboxes == null)
            return false;

        for (int i = 0; i < bodyHitboxes.Length; i++)
        {
            if (bodyHitboxes[i] == hit)
                return true;
        }
        return false;
    }

    [SyncVar(hook = nameof(OnDeadStateChanged))]
    private bool is_dead;
    public bool Is_dead { get { return is_dead; } }

    [SyncVar]
    public int current_health;

    [SyncVar] public string playerName = "Player";
    [SyncVar] public int kills = 0;

    [Command]
    public void CmdSetPlayerName(string requestedName)
    {
        if (string.IsNullOrWhiteSpace(requestedName))
            return;

        var cleaned = new System.Text.StringBuilder(20);
        foreach (char c in requestedName.Trim())
        {
            if (cleaned.Length >= 20)
                break;
            if (!char.IsControl(c) && c != '<' && c != '>')
                cleaned.Append(c);
        }

        string result = cleaned.ToString().Trim();
        if (result.Length > 0)
            playerName = result;
    }

    [Server]
    public void ServerAddKill()
    {
        kills++;
    }

    private bool[] was_enabled;
    private Animator animator;
    private Rigidbody rootRigidbody;
    private bool rootWasKinematic;
    private CapsuleCollider rootCapsule;
    private SphereCollider oldRootHeadSphere;

    private readonly List<Collider> ragdollColliders = new List<Collider>();
    private readonly List<bool> originalRagdollTriggers = new List<bool>();
    private readonly List<Rigidbody> ragdollRigidbodies = new List<Rigidbody>();

    private readonly List<Transform> savedTransforms = new List<Transform>();
    private readonly List<Vector3> savedLocalPositions = new List<Vector3>();
    private readonly List<Quaternion> savedLocalRotations = new List<Quaternion>();
    private float previousSpineY;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rootRigidbody = GetComponent<Rigidbody>();
        rootWasKinematic = rootRigidbody != null && rootRigidbody.isKinematic;
        rootCapsule = GetComponent<CapsuleCollider>();
        oldRootHeadSphere = GetComponent<SphereCollider>();

        was_enabled = new bool[disable_on_death.Length];
        for (int i = 0; i < disable_on_death.Length; i++)
            was_enabled[i] = disable_on_death[i] != null && disable_on_death[i].enabled;

        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child == transform) continue;
            savedTransforms.Add(child);
            savedLocalPositions.Add(child.localPosition);
            savedLocalRotations.Add(child.localRotation);
        }

        foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>(true))
        {
            if (rb != rootRigidbody)
                ragdollRigidbodies.Add(rb);
        }

        foreach (Collider col in GetComponentsInChildren<Collider>(true))
        {
            if (col.gameObject == gameObject)
                continue;

            bool assignedHitbox = IsHeadHit(col) || IsBodyHit(col);
            bool partOfRagdoll = col.attachedRigidbody != null &&
                                 col.attachedRigidbody != rootRigidbody;
            if (!assignedHitbox && !partOfRagdoll)
                continue;

            ragdollColliders.Add(col);
            originalRagdollTriggers.Add(col.isTrigger);
        }

        SetRagdollEnabled(false);
        if (oldRootHeadSphere != null)
            oldRootHeadSphere.enabled = false;

        if (spine != null)
            previousSpineY = spine.transform.eulerAngles.y;

        characterRenderers = GetComponentsInChildren<Renderer>(true);
        originalRendererStates = new bool[characterRenderers.Length];

        for (int i = 0; i < characterRenderers.Length; i++)
        {
            originalRendererStates[i] = characterRenderers[i].enabled;
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        current_health = Mathf.Max(1, max_health);
        is_dead = false;
        playerName = "Player " + netId;
        kills = 0;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        ApplyLifeState(is_dead);
    }

    public void Setup() { }

    [Server]
    public bool ServerTakeDamage(int damage)
    {
        if (is_dead || damage <= 0)
            return false;

        current_health = Mathf.Max(0, current_health - damage);
        if (current_health > 0)
            return false;

        is_dead = true;
        ApplyLifeState(true);
        StartCoroutine(ServerRespawn());
        return true;
    }

    private void OnDeadStateChanged(bool oldValue, bool newValue)
    {
        ApplyLifeState(newValue);
    }

    private void ApplyLifeState(bool dead)
    {
        for (int i = 0; i < disable_on_death.Length; i++)
        {
            if (disable_on_death[i] != null)
                disable_on_death[i].enabled = !dead && was_enabled[i];
        }

        if (rootCapsule != null)
            rootCapsule.enabled = !dead;
        if (oldRootHeadSphere != null)
            oldRootHeadSphere.enabled = false;

        if (rootRigidbody != null)
        {
            if (dead)
            {
                rootRigidbody.linearVelocity = Vector3.zero;
                rootRigidbody.angularVelocity = Vector3.zero;
                rootRigidbody.isKinematic = true;
            }
            else
            {
                rootRigidbody.isKinematic = rootWasKinematic;
                if (!rootRigidbody.isKinematic)
                {
                    rootRigidbody.linearVelocity = Vector3.zero;
                    rootRigidbody.angularVelocity = Vector3.zero;
                }
            }
        }

        SetRagdollEnabled(dead);

        if (animator != null)
            animator.enabled = !dead;

        if (!dead)
        {
            Local local = GetComponent<Local>();
            if (local != null && isClient)
                local.ApplyOwnershipRules();
        }
    }

    private void SetRagdollEnabled(bool dead)
    {

        for (int i = 0; i < ragdollRigidbodies.Count; i++)
        {
            if (ragdollRigidbodies[i] != null)
                ragdollRigidbodies[i].isKinematic = !dead;
        }

        for (int i = 0; i < ragdollColliders.Count; i++)
        {
            Collider col = ragdollColliders[i];
            if (col == null) continue;

            bool isHitbox = IsHeadHit(col) || IsBodyHit(col);
            if (dead)
            {
                col.isTrigger = isHitbox ? false : originalRagdollTriggers[i];
                col.enabled = true;
            }
            else
            {
                col.isTrigger = isHitbox ? true : originalRagdollTriggers[i];
                col.enabled = isHitbox;
            }
        }
    }


    [Server]
    private IEnumerator ServerRespawn()
    {
        float wait = 3f;

        if (GameManager.instance != null &&
            GameManager.instance.matchSettings != null)
        {
            wait = Mathf.Max(
                0.1f,
                GameManager.instance.matchSettings.respawn_time
            );
        }

        // Sakri telo pre respawna
        float hideTime = Mathf.Min(0.15f, wait);

        yield return new WaitForSeconds(wait - hideTime);

        RpcSetPlayerVisible(false);

        yield return new WaitForSeconds(hideTime);

        Transform start = NetworkManager.singleton != null
            ? NetworkManager.singleton.GetStartPosition()
            : null;

        Vector3 newPosition = start != null
            ? start.position
            : transform.position;

        Quaternion newRotation = start != null
            ? start.rotation
            : transform.rotation;

        SetRagdollEnabled(false);
        RestoreChildPose();

        transform.SetPositionAndRotation(newPosition, newRotation);

        current_health = Mathf.Max(1, max_health);
        is_dead = false;

        ApplyLifeState(false);

        RpcRespawnAt(newPosition, newRotation);

        yield return new WaitForSeconds(0.35f);

        RpcSetPlayerVisible(true);
    }


    [ClientRpc]
    private void RpcRespawnAt(Vector3 position, Quaternion rotation)
    {
        if (isServer) return;

        SetRagdollEnabled(false);
        RestoreChildPose();
        transform.SetPositionAndRotation(position, rotation);
        ApplyLifeState(false);
    }

    private void RestoreChildPose()
    {
        for (int i = 0; i < savedTransforms.Count; i++)
        {
            if (savedTransforms[i] == null) continue;
            savedTransforms[i].localPosition = savedLocalPositions[i];
            savedTransforms[i].localRotation = savedLocalRotations[i];
        }
    }

    private void Update()
    {
        if (!isLocalPlayer || is_dead || animator == null || ground == null)
            return;

        Animacije();
    }

    public void Animacije()
    {
        bool onGround = ground.isGrounded;
        bool running = Input.GetKey(KeyCode.LeftShift);
        bool forward = Input.GetKey(KeyCode.W);
        bool backward = Input.GetKey(KeyCode.S);
        bool left = Input.GetKey(KeyCode.A);
        bool right = Input.GetKey(KeyCode.D);

        animator.SetBool("IsMovingForward", (forward || backward) && onGround && !running);
        animator.SetBool("IsRunning", (forward || backward) && onGround && running);
        animator.SetBool("Left", left && onGround);
        animator.SetBool("Right", right && onGround);
        animator.SetBool("Emote1", Input.GetKey(KeyCode.Alpha1));

        float speed = backward ? -1f : (running && (left || right) ? 1.1f : 1f);
        animator.SetFloat("Speed", speed);

        if (spine != null)
        {
            float yaw = spine.transform.eulerAngles.y;
            float difference = Mathf.Abs(Mathf.DeltaAngle(previousSpineY, yaw));
            animator.SetBool("IsTurning", difference > 3.5f && onGround);
            previousSpineY = yaw;
        }
    }

    private void SetPlayerVisible(bool visible)
    {
        for (int i = 0; i < characterRenderers.Length; i++)
        {
            if (characterRenderers[i] != null)
            {
                characterRenderers[i].enabled =
                    visible && originalRendererStates[i];
            }
        }
    }

    [ClientRpc]
    private void RpcSetPlayerVisible(bool visible)
    {
        SetPlayerVisible(visible);
    }

}
