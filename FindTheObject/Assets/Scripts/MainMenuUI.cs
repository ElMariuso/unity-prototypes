using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;
using System.Text.RegularExpressions;

/// <summary>
/// Handles the main menu UI, allowing players to start as a host or client.
/// Manages network connections and validates IP input.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private TMP_InputField joinIPField;
    [SerializeField] private GameObject gameManagerPrefab;
    [SerializeField] private GameObject gridManagerPrefab;

    /// <summary>
    /// Sets up button listeners and registers disconnect callback on awake.
    /// </summary>
    private void Awake()
    {
        hostButton.onClick.AddListener(() => StartNetworkMode(true));
        clientButton.onClick.AddListener(() => StartNetworkMode(false));
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    /// <summary>
    /// Handles client disconnection and displays appropriate error messages.
    /// </summary>
    /// <param name="clientId">ID of the disconnected client.</param>
    private void OnClientDisconnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            string disconnectReason = NetworkManager.Singleton.DisconnectReason;
            
            if (string.IsNullOrEmpty(disconnectReason))
            {
                disconnectReason = "Disconnected from server";
            }

            mainMenuPanel.SetActive(true);
            ShowErrorMessage(disconnectReason);
            Debug.Log($"Client disconnected: {disconnectReason}");
        }
    }

    /// <summary>
    /// Starts the network session as a host or client.
    /// </summary>
    /// <param name="isHost">True if starting as a host, false if as a client.</param>
    private void StartNetworkMode(bool isHost)
    {
        if (isHost)
        {
            Debug.Log("Starting Host...");
            if (NetworkManager.Singleton.StartHost())
            {
                Debug.Log("Host started successfully!");
                SpawnGameManagerGridManager();
                HideMenu();
            }
            else
            {
                Debug.LogError("Failed to start host!");
            }
        }
        else
        {
            if (joinIPField == null) return;

            string ipToJoin = joinIPField.text.Trim();
            if (!IsValidIPv4(ipToJoin))
            {
                ShowErrorMessage("INVALID IP");
                return;
            }

            Debug.Log($"Attempting to connect to {ipToJoin}:7777...");

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                transport.ConnectionData.Address = ipToJoin;
                transport.ConnectionData.Port = 7777;
            }

            if (NetworkManager.Singleton.StartClient())
            {
                Debug.Log("Client started successfully!");
                HideMenu();
            }
            else
            {
                ShowErrorMessage("CONNECTION FAILED");
            }
        }
    }

    /// <summary>
    /// Spawns the GameManager and GridManager on the server if they don't already exist.
    /// </summary>
    private void SpawnGameManagerGridManager()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            if (FindObjectOfType<GameManager>() == null)
            {
                GameObject gameManager = Instantiate(gameManagerPrefab);
                gameManager.GetComponent<NetworkObject>().Spawn();
                Debug.Log("GameManager spawned successfully.");
            }
            else
            {
                Debug.LogWarning("GameManager already exists, skipping spawn.");
            }

            if (FindObjectOfType<GridManager>() == null)
            {
                GameObject gridManager = Instantiate(gridManagerPrefab);
                gridManager.GetComponent<NetworkObject>().Spawn();
                Debug.Log("GridManager spawned successfully.");
            }
            else
            {
                Debug.LogWarning("GridManager already exists, skipping spawn.");
            }
        }
    }

    /// <summary>
    /// Hides the main menu UI after a successful connection.
    /// </summary>
    private void HideMenu()
    {
        mainMenuPanel.SetActive(false);
    }

    /// <summary>
    /// Resets the IP input field to its default state.
    /// </summary>
    private void ResetJoinIPField()
    {
        joinIPField.text = "";
        joinIPField.interactable = true;
    }

    /// <summary>
    /// Validates an IPv4 address using regex.
    /// </summary>
    /// <param name="ipString">The IP address string to validate.</param>
    /// <returns>True if valid, otherwise false.</returns>
    private bool IsValidIPv4(string ipString)
    {
        if (string.IsNullOrWhiteSpace(ipString)) return false;

        string pattern = @"^(25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9]?[0-9])\."
                       + @"(25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9]?[0-9])\."
                       + @"(25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9]?[0-9])\."
                       + @"(25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9]?[0-9])$";

        return Regex.IsMatch(ipString, pattern);
    }

    /// <summary>
    /// Displays an error message in the IP input field and resets it after a delay.
    /// </summary>
    /// <param name="message">Error message to display.</param>
    private void ShowErrorMessage(string message)
    {
        joinIPField.text = message;
        joinIPField.interactable = false;
        Invoke(nameof(ResetJoinIPField), 2f);
    }
}