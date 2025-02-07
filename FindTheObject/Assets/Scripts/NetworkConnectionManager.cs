using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Manages network connections and enforces player limits.
/// Ensures only a set number of players can join the game.
/// </summary>
public class NetworkConnectionManager : MonoBehaviour
{
    /// <summary>
    /// Registers the connection approval callback when the object starts.
    /// </summary>
    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            // Assigns the connection approval callback to validate incoming players
            NetworkManager.Singleton.ConnectionApprovalCallback = ApproveConnection;
        }
    }

    /// <summary>
    /// Validates connection requests and approves or denies them based on player count.
    /// </summary>
    /// <param name="request">The connection request from a client.</param>
    /// <param name="response">The response that will be sent back to the client.</param>
    private void ApproveConnection(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        // Gets the current number of connected players
        int connectedPlayers = NetworkManager.Singleton.ConnectedClients.Count;

        // Allows a maximum of 2 players to connect
        if (connectedPlayers < 2)
        {
            response.Approved = true; // Connection is approved
            response.CreatePlayerObject = true; // A player object will be created
        }
        else
        {
            response.Approved = false; // Connection is denied
            response.Reason = "Server is full"; // Informs the client why the connection was rejected
        }

        response.Pending = false; // Marks the approval process as completed
    }
}
