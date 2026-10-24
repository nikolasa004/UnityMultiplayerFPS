
using UnityEngine;
using Mirror;

public class LadderTeleport : MonoBehaviour
{
    [SerializeField] private Transform roofPoint;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private NetworkIdentity localPlayer;
    private Rigidbody playerRigidbody;

    private void OnTriggerEnter(Collider other)
    {
        NetworkIdentity player = other.GetComponentInParent<NetworkIdentity>();

        if (player == null || !player.isLocalPlayer)
            return;

        // Only use the player's main physical collider.
        if (other.gameObject != player.gameObject)
            return;

        PlayerManager manager = player.GetComponent<PlayerManager>();

        if (manager != null && manager.Is_dead)
            return;

        localPlayer = player;
        playerRigidbody = player.GetComponent<Rigidbody>();
    }

    private void OnTriggerExit(Collider other)
    {
        if (localPlayer != null &&
            other.gameObject == localPlayer.gameObject)
        {
            localPlayer = null;
            playerRigidbody = null;
        }
    }

    private void Update()
    {
        if (localPlayer == null || roofPoint == null)
            return;

        if (!Input.GetKeyDown(interactKey))
            return;


        PlayerManager manager = localPlayer.GetComponent<PlayerManager>();

        if (manager == null || manager.spine == null)
            return;

        Vector3 offset = localPlayer.transform.position - manager.spine.transform.position;

        Vector3 destination = roofPoint.position + new Vector3(offset.x, 0f, offset.z);

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            playerRigidbody.position = destination;
        }
        else
        {
            localPlayer.transform.position = destination;
        }


        localPlayer = null;
        playerRigidbody = null;
    }
}
