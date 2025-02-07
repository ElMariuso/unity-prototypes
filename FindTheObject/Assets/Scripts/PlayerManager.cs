using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;

/// <summary>
/// Manages player behavior, movement, and interactions in the multiplayer game.
/// Handles player placement, movement, color synchronization, and collision events.
/// </summary>
public class PlayerManager : NetworkBehaviour
{
    private bool isPlayer1 = true; // Determines if the player is Player 1 or Player 2
    private int gridSize = 6; // Defines the size of the game grid - 6 like asked in the PDF
    private Renderer playerRenderer; // Reference to the player's Renderer component

    // Network-synchronized player color
    private NetworkVariable<Color> playerColor = new NetworkVariable<Color>(Color.white);

    /// <summary>
    /// Called when the player is spawned on the network.
    /// Initializes player color and placement.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        playerColor.OnValueChanged += (oldColor, newColor) => ApplyColor(newColor);
        if (IsOwner)
        {
            isPlayer1 = IsHost;
            RequestColorChangeServerRpc(isPlayer1 ? Color.cyan : Color.magenta);
            PlacePlayer();
        }
        ApplyColor(playerColor.Value);
    }

    /// <summary>
    /// Handles player movement based on input.
    /// </summary>
    private void Update()
    {
        if (!IsOwner || !GameManager.Instance.isGameActive.Value) return ; // Avoid moving if the game is finished, or has not started, or wait for more players

        Vector3 moveDir = Vector3.zero;

        // Controls for Player 1 (WASD) and Player 2 (Arrow Keys)
        if (isPlayer1)
        {
            if (Input.GetKey(KeyCode.W)) moveDir.z = +1f;
            if (Input.GetKey(KeyCode.S)) moveDir.z = -1f;
            if (Input.GetKey(KeyCode.A)) moveDir.x = -1f;
            if (Input.GetKey(KeyCode.D)) moveDir.x = +1f;
        }
        else
        {
            if (Input.GetKey(KeyCode.UpArrow)) moveDir.z = +1f;
            if (Input.GetKey(KeyCode.DownArrow)) moveDir.z = -1f;
            if (Input.GetKey(KeyCode.LeftArrow)) moveDir.x = -1f;
            if (Input.GetKey(KeyCode.RightArrow)) moveDir.x = +1f;
        }

        float moveSpeed = 3f;
        transform.position += moveDir * moveSpeed * Time.deltaTime;
    }

    /// <summary>
    /// Places the player at the initial grid position.
    /// </summary>
    public void PlacePlayer()
    {
        float positionOffset = (gridSize - 1) / 2f;
        Vector3 newPosition = isPlayer1 ? new Vector3(-positionOffset, 4, positionOffset) : new Vector3(positionOffset, 4, -positionOffset);

        transform.position = newPosition;
        transform.rotation = Quaternion.identity;

        // Reset rigidbody physics - So forces are reset when the entire game is reset. This prevents you from retaining the force of movement when you are replaced.
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            bool wasKinematic = rb.isKinematic;
            rb.isKinematic = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = wasKinematic;
        }
    }

    /// <summary>
    /// Applies the given color to the player's material.
    /// </summary>
    private void ApplyColor(Color color)
    {
        if (playerRenderer == null) playerRenderer = GetComponentInChildren<Renderer>();
        if (playerRenderer != null) playerRenderer.material.color = color;
        else Debug.LogError("ERROR::PlayerManager::Renderer not found");
    }

    /// <summary>
    /// Requests the server to change the player's color.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void RequestColorChangeServerRpc(Color newColor)
    {
        if (!IsServer) return;
        playerColor.Value = newColor;
    }

    /// <summary>
    /// Handles collision events, triggering game-ending or push mechanics.
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        if (!IsOwner) return;

        // If the player collides with the floor (its tag is "Finish"), they lose the game
        if (collision.gameObject.CompareTag("Finish"))
        {
            RequestEndGameServerRpc(!isPlayer1);
        }
        else if (collision.gameObject.CompareTag("Player")) // If the player collides with another player, apply push mechanics
        {
            Vector3 pushDirection = (collision.transform.position - transform.position).normalized;
            float pushForceOther = 4f;
            float pushForceSelf = 1f;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null) rb.AddForce(-pushDirection * pushForceSelf, ForceMode.Impulse);  // Small push for yourself

            ulong targetId = collision.gameObject.GetComponent<NetworkObject>().NetworkObjectId;

            RequestPushAllClientsServerRpc(targetId, pushDirection, pushForceOther);
        }
    }

    /// <summary>
    /// Requests the server to end the game, determining the winner.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void RequestEndGameServerRpc(bool player1Won)
    {
        if (!IsServer) return;

        GameManager.Instance.EndGameServerRpc(player1Won);
    }

    /// <summary>
    /// Requests all clients to apply push physics to the targeted player.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void RequestPushAllClientsServerRpc(ulong targetPlayerId, Vector3 direction, float force)
    {
        if (!IsServer) return;

        ApplyPushClientRpc(targetPlayerId, direction, force);
    }

    /// <summary>
    /// Applies a push force to the specified player client-side.
    /// </summary>
    [ClientRpc]
    private void ApplyPushClientRpc(ulong targetPlayerId, Vector3 direction, float force)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetPlayerId, out NetworkObject targetObject))
        {
            Rigidbody targetRb = targetObject.GetComponent<Rigidbody>();
            if (targetRb != null)
            {
                targetRb.AddForce(direction * force, ForceMode.Impulse);
            }
        }
    }
}