using Mirror;
using UnityEngine;

[RequireComponent(typeof(PlayerManager))]
public class Local : NetworkBehaviour
{
    [SerializeField] private FirstPersonLook fps_look;
    [SerializeField] private FirstPersonMovement fps_move;
    public Camera fps_camera;
    public AudioListener listener;
    [SerializeField] private Shooting shooting;
    [SerializeField] private string other_player_layer = "OtherPlayer";
    [SerializeField] private GameObject playerUI;

    private GameObject playerUIInstance;
    private bool nameEntryOpen;

    private void Awake()
    {
        if (fps_look == null)
            fps_look = GetComponentInChildren<FirstPersonLook>(true);
        if (fps_move == null)
            fps_move = GetComponent<FirstPersonMovement>();
        if (shooting == null)
            shooting = GetComponent<Shooting>();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        GetComponent<PlayerManager>().Setup();
        GameManager.RegisterPlayer(netId.ToString(), GetComponent<PlayerManager>());
        ApplyOwnershipRules();
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        ApplyOwnershipRules();

        if (playerUI != null)
        {
            playerUIInstance = Instantiate(playerUI);
            playerUIInstance.name = playerUI.name;
            if (shooting != null)
                shooting.SetLocalUI(playerUIInstance.GetComponentInChildren<UI>(true));

            PlayerNameUI namePrompt = playerUIInstance.GetComponentInChildren<PlayerNameUI>(true);
            if (namePrompt != null)
                namePrompt.Initialize(GetComponent<PlayerManager>(), this);
            else
                Debug.LogWarning("PlayerNameUI is missing from the playerUI prefab.");
        }
    }

    public void SetNameEntryOpen(bool open)
    {
        if (!isLocalPlayer) return;

        nameEntryOpen = open;
        ApplyOwnershipRules();
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;
    }

    public void ApplyOwnershipRules()
    {
        bool owner = isLocalPlayer;
        bool allowInput = owner && !nameEntryOpen;

        if (fps_move != null) fps_move.enabled = allowInput;
        if (fps_look != null) fps_look.enabled = allowInput;
        if (fps_camera != null) fps_camera.enabled = owner;
        if (listener != null) listener.enabled = owner;
        if (shooting != null) shooting.enabled = allowInput;

        if (!owner)
        {
            int layer = LayerMask.NameToLayer(other_player_layer);
            if (layer >= 0)
                gameObject.layer = layer;
        }
    }

    public override void OnStopClient()
    {
        GameManager.UnRegisterPlayer(transform.name);
        if (playerUIInstance != null)
            Destroy(playerUIInstance);
        base.OnStopClient();
    }
}
