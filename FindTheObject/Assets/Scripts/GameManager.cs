using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Net;
using System.Net.Sockets;

/// <summary>
/// Manages the overall game state, player turns, scores, and UI elements.
/// Handles multiplayer synchronization using Unity Netcode.
/// </summary>
public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    // Network-synchronized game state variables
    public NetworkVariable<bool> isGameActive = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> isPlayer1Turn = new NetworkVariable<bool>(true);
    public bool IsPlayer1Turn => isPlayer1Turn.Value;
    private NetworkVariable<int> connectedPlayers = new NetworkVariable<int>(0);
    private NetworkVariable<int> player1Score = new NetworkVariable<int>(0);
    private NetworkVariable<int> player2Score = new NetworkVariable<int>(0);

    // UI Elements
    [SerializeField] private GameObject WonPanel;
    [SerializeField] private Button restartButton;
    [SerializeField] private TextMeshProUGUI ipText;
    [SerializeField] private TextMeshProUGUI turnText;
    [SerializeField] private TextMeshProUGUI scoreText;

    // Ip address name
    private string hostIPAddress = "Unknown";

    /// <summary>
    /// Ensures a single instance of GameManager is active.
    /// </summary>
    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    /// <summary>
    /// Called when the object is spawned in the network.
    /// Initializes UI elements and sets up event listeners.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        isPlayer1Turn.OnValueChanged += (previous, current) => UpdateTurnTextClientRpc(current);

        // Locate and assign UI elements dynamically
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            WonPanel = canvas.transform.Find("WonPanel")?.gameObject;
            restartButton = canvas.transform.Find("RestartButton")?.GetComponent<Button>();
            ipText = canvas.transform.Find("IPDisplay")?.GetComponent<TextMeshProUGUI>();
            turnText = canvas.transform.Find("TurnText")?.GetComponent<TextMeshProUGUI>();
            scoreText = canvas.transform.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();

            if (ipText != null)
            {
                ipText.gameObject.SetActive(true);
                UpdateIPText(); // When the UI about the IP address is found, assign it
            }
            if (turnText != null) turnText.gameObject.SetActive(true);
            if (scoreText != null) scoreText.gameObject.SetActive(true);
        }

        // Only the server should handle network-related setup
        if (IsServer)
        {
            hostIPAddress = GetLocalIPAddress(); // Retrieve the server's local IP
            SendHostIPToClientRpc(hostIPAddress); // Send the host's IP to clients

            restartButton.gameObject.SetActive(true);
            restartButton.onClick.AddListener(() => RestartGameServerRpc());
            restartButton.gameObject.SetActive(false);

            connectedPlayers.Value = 1;
            CheckGameStatus(); // To pause the game if there is not much players

            // Monitor player connections and disconnections
            connectedPlayers.OnValueChanged += (oldValue, newValue) => { CheckGameStatus(); };
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    /// <summary>
    /// Increments the player count when a client connects and sends the host's IP to clients when they connect
    /// </summary>
    private void OnClientConnected(ulong clientId)
    {
        if (IsServer)
        {
            connectedPlayers.Value++;
            SendHostIPToClientRpc(hostIPAddress);
        }
    }

    /// <summary>
    /// Decrements the player count when a client disconnects and checks game status.
    /// </summary>
    private void OnClientDisconnected(ulong clientId)
    {
        if (IsServer)
        {
            if (NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
            {
                connectedPlayers.Value--;
            }
            StopAllCoroutines();
            CheckGameStatus();
        }
    }

    /// <summary>
    /// Checks the game state based on the number of connected players.
    /// If fewer than 2 players are connected, resets the game state and wait for more.
    /// Otherwise, starts a countdown for the match to begin.
    /// </summary>
    private void CheckGameStatus()
    {
        if (connectedPlayers.Value < 2)
        {
            if (GridManager.Instance != null) RestartGameServerRpc();
            isGameActive.Value = false;
            player1Score.Value = 0;
            player2Score.Value = 0;
            UpdateScoreClientRpc(player1Score.Value, player2Score.Value);
            if (turnText != null) turnText.text = "Waiting for players...";
        }
        else
        {
            if (!isGameActive.Value)
            {
                StopAllCoroutines();
                StartCoroutine(StartCountdown());
            }
        }
    }

    /// <summary>
    /// Starts a countdown before the game begins.
    /// Updates all clients with the remaining time.
    /// </summary>
    private IEnumerator StartCountdown()
    {
        int countdown = 3;
        while (countdown > 0)
        {
            UpdateCountdownClientRpc(countdown);

            yield return new WaitForSecondsRealtime(1f);
            countdown--;

            if (connectedPlayers.Value < 2)  // Stop the count if players has disconnected
            {
                UpdateCountdownClientRpc(-1);
                yield break;
            }
        }

        // Launch the game
        isGameActive.Value = true;
        UpdateTurnTextClientRpc(isPlayer1Turn.Value);
    }

    /// <summary>
    /// Updates the countdown timer on all clients, including UI.
    /// </summary>
    [ClientRpc]
    private void UpdateCountdownClientRpc(int countdown)
    {
        if (turnText != null)
        {
            if (countdown > 0) turnText.text = $"Game starts in {countdown}...";
            else turnText.text = "Waiting for players...";
        }
    }

    /// <summary>
    /// Switches the player's turn on the server.
    /// </summary>
    [ServerRpc]
    public void SwitchTurnServerRpc()
    {
        isPlayer1Turn.Value = !isPlayer1Turn.Value;
        UpdateTurnTextClientRpc(isPlayer1Turn.Value);
    }

    /// <summary>
    /// Updates the turn text in the UI to reflect the current player's turn.
    /// This method is called on all clients via ClientRpc.
    /// </summary>
    /// <param name="isPlayer1TurnNow">Indicates whether it is Player 1's turn (true) or Player 2's turn (false).</param>
    [ClientRpc]
    private void UpdateTurnTextClientRpc(bool isPlayer1TurnNow)
    {
        if (turnText != null) turnText.text = isPlayer1TurnNow ? "PLAYER 1 TURN" : "PLAYER 2 TURN";
    }

    /// <summary>
    /// Updates the IP display text in the UI to show the player's role (Host or Client) and the host's IP address.
    /// </summary>
    private void UpdateIPText()
    {
        if (ipText == null) 
        {
            return;
        }

        string role = IsHost ? "HOST" : "CLIENT";
        ipText.text = $"{role} - {hostIPAddress}";
    }

    /// <summary>
    /// Retrieves the local IP address of the host machine.
    /// Uses a UDP socket connection to an external address to determine the local network IP.
    /// </summary>
    /// <returns>The local IP address as a string, or "127.0.0.1" if an error occurs.</returns>
    private string GetLocalIPAddress()
    {
        string localIP = "127.0.0.1"; // Default fallback IP (localhost)

        try
        {
            // Create a UDP socket to determine the local network IP
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530); // Connect to an external IP to determine local network IP
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                localIP = endPoint?.Address.ToString(); // Retrieve the assigned local IP
            }
        }
        catch
        {
            Debug.LogError("ERROR::GameManager::Can't find IP");
        }

        return localIP;
    }

    /// <summary>
    /// Sends the host's IP address to all clients.
    /// This method is called via ClientRpc to ensure all connected clients receive the correct IP.
    /// </summary>
    /// <param name="ip">The host's IP address to be sent to all clients.</param>
    [ClientRpc]
    private void SendHostIPToClientRpc(string ip)
    {
        hostIPAddress = ip; // Update the local variable on the client
        UpdateIPText(); // Refresh the UI to display the correct IP information
    }

    /// <summary>
    /// Ends the game and updates the winner's score.
    /// This method is executed on the server and updates all clients accordingly.
    /// </summary>
    /// <param name="player1Won">Indicates whether Player 1 won (true) or Player 2 won (false).</param>
    [ServerRpc(RequireOwnership = false)]
    public void EndGameServerRpc(bool player1Won)
    {
        if (!IsServer) return ; // Ensure this method is only executed by the server

        isGameActive.Value = false; // Mark the game as inactive

        // Update the score for the winning player
        if (player1Won) player1Score.Value++;
        else player2Score.Value++;

        // Synchronize the updated scores with all clients
        // Then notify all clients that the game has ended and who won
        UpdateScoreClientRpc(player1Score.Value, player2Score.Value);
        EndGameClientRpc(player1Won);
    }

    /// <summary>
    /// Updates the UI to display the current scores for both players.
    /// This method is called on all clients via ClientRpc.
    /// </summary>
    /// <param name="p1Score">Player 1's current score.</param>
    /// <param name="p2Score">Player 2's current score.</param>
    [ClientRpc]
    private void UpdateScoreClientRpc(int p1Score, int p2Score)
    {
        if (scoreText != null)
        {
            // Display "+99" if the score exceeds 99 to prevent UI overflow
            string displayP1 = p1Score > 99 ? "+99" : p1Score.ToString();
            string displayP2 = p2Score > 99 ? "+99" : p2Score.ToString();
            
            // Update the score text in the UI
            scoreText.text = $"PLAYER 1 - {displayP1}\nPLAYER 2 - {displayP2}";
        }
    }

    /// <summary>
    /// Displays the game over panel and announces the winner on all clients.
    /// If the server is hosting, it also enables the restart button.
    /// </summary>
    /// <param name="player1Won">Indicates whether Player 1 won (true) or Player 2 won (false).</param>
    [ClientRpc]
    private void EndGameClientRpc(bool player1Won)
    {
        if (WonPanel != null) WonPanel.SetActive(true); // Show the game over panel

        // Find the text component inside the WonPanel and update it with the winner's name
        TMP_Text wonText = WonPanel.GetComponentInChildren<TMP_Text>();
        if (wonText != null) wonText.text = player1Won ? "PLAYER 1 WON" : "PLAYER 2 WON";
        else Debug.LogError("ERROR:GameManager::WonText not found!");

        // If this is the server, make the restart button visible
        if (IsServer && restartButton != null) restartButton.gameObject.SetActive(true);
    }

    /// <summary>
    /// Resets the game state and hides the game over panel.
    /// Also reinitializes the game grid and resets player positions.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RestartGameServerRpc()
    {
        if (!IsServer) return; // Ensure this method runs only on the server
        if (WonPanel != null) WonPanel.SetActive(false); // Hide the game over panel
        if (IsServer && restartButton != null) restartButton.gameObject.SetActive(false); // Hide the restart button

        isGameActive.Value = true; // Set the game state to active

        // Reset the game grid if GridManager is available
        if (GridManager.Instance != null) GridManager.Instance.PlaceHiddenObjectServerRpc();
        else Debug.LogError("ERROR:GameManager::gridManager not available");

        // Reset visuals and player positions on all clients
        ResetColorClientRpc();
        ResetGameClientRpc();
    }

    /// <summary>
    /// Resets the colors of all tiles on all clients.
    /// This ensures the board returns to its default state visually.
    /// </summary>
    [ClientRpc]
    private void ResetColorClientRpc()
    {
        foreach (var tile in FindObjectsOfType<Tile>())
        {
            tile.ResetColor();
        }
    }

    /// <summary>
    /// Resets the game state on all clients by hiding the game over panel
    /// and repositioning the players at their starting locations.
    /// </summary>
    [ClientRpc]
    private void ResetGameClientRpc()
    {
        WonPanel.SetActive(false); // Hide the game over panel
        // Move each player back to their initial position
        foreach (var player in FindObjectsOfType<PlayerManager>())
        {
            player.PlacePlayer();
        }
    }
}
