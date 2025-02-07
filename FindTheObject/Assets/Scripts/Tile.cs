using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Represents a tile in the grid, synchronized over the network.
/// Handles position, color changes, and player interactions.
/// </summary>
public class Tile : NetworkBehaviour
{
    // Network-synchronized grid position of the tile (Only writable by the server)
    private NetworkVariable<Vector2Int> gridPosition = new NetworkVariable<Vector2Int>( new Vector2Int(0, 0), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    // Network-synchronized base color of the tile (Only writable by the server)
    private NetworkVariable<Color> basicColor = new NetworkVariable<Color>(Color.white, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private Renderer tileRenderer; // Renderer component reference

    /// <summary>
    /// Initializes the tile's renderer.
    /// </summary>
    private void Awake()
    {
        tileRenderer = GetComponent<Renderer>();
    }

    /// <summary>
    /// Called when the tile is spawned on the network.
    /// Synchronizes color updates and ensures correct color rendering for clients.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Register an event listener to update tile color when it changes on the network
        basicColor.OnValueChanged += (oldColor, newColor) =>
        {
            tileRenderer.material.color = newColor;
        };

        // Ensure clients apply the correct color upon spawning
        if (!IsServer)
        {
            tileRenderer.material.color = basicColor.Value;
        }
    }

    /// <summary>
    /// Sets the grid position of the tile (From the grid generation).
    /// </summary>
    /// <param name="x">X coordinate on the grid</param>
    /// <param name="y">Y coordinate on the grid</param>
    public void SetGridPosition(int x, int y)
    {
        if (IsServer)
        {
            gridPosition.Value = new Vector2Int(x, y);
        }
    }

    /// <summary>
    /// Gets the current grid position of the tile.
    /// </summary>
    /// <returns>Grid position as a Vector2Int</returns>
    public Vector2Int GetGridPosition()
    {
        return gridPosition.Value;
    }

    /// <summary>
    /// Updates the base color of the tile and applies it (From the grid generation).
    /// </summary>
    /// <param name="newColor">New color to set</param>
    public void SetBasicColor(Color newColor)
    {
        if (IsServer)
        {
            basicColor.Value = newColor;
            tileRenderer.material.color = newColor;
        }
    }

    /// <summary>
    /// Resets the tile's color to its base network-synchronized color.
    /// </summary>
    public void ResetColor()
    {
        tileRenderer.material.color = basicColor.Value;
    }

    /// <summary>
    /// Handles tile click events. Notifies the GridManager if the game is active.
    /// </summary>
    private void OnMouseDown()
    {
        // Ensure the game is active and network components are available
        // Avoid moving if the game is finished, or has not started, or wait for more players
        if (GridManager.Instance != null && NetworkManager.Singleton != null && GameManager.Instance.isGameActive.Value)
        {
            ulong playerId = NetworkManager.Singleton.LocalClientId; // Get local player ID
            GridManager.Instance.OnTileClicked(gridPosition.Value, playerId); // Notify GridManager
        }
    }
}
