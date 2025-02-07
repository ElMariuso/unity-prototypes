using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Manages the game grid, tile generation, and hidden object placement.
/// Handles interactions with tiles and synchronizes game state over the network.
/// </summary>
public class GridManager : NetworkBehaviour
{
    public static GridManager Instance { get; private set; }

    [SerializeField] private GameObject tilePrefab; // Prefab for individual tiles

    private int gridSize = 6; // Defines the size of the game grid - 6 like asked in the PDF

    // To look like a checkboard
    private Color color1 = Color.white;
    private Color color2 = Color.black;

    // Network-synchronized hidden object position
    private NetworkVariable<Vector2Int> hiddenObjectPosition = new NetworkVariable<Vector2Int>();

    /// <summary>
    /// Called when the GridManager is spawned on the network.
    /// Ensures singleton instance and initializes the grid.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Be sure only a GridManager exist
        if (FindObjectsOfType<GridManager>().Length > 1)
        {
            Destroy(gameObject);
            return ;
        }

        // If the instance don't exist, define it as this
        if (Instance == null) Instance = this;
        if (IsServer)
        {
            GenerateGrid();
            PlaceHiddenObjectServerRpc();
        }
    }

    /// <summary>
    /// Generates a 6x6 grid of tiles and initializes them.
    /// </summary>
    private void GenerateGrid()
    {
        float offset = (gridSize - 1) / 2f;

        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                Vector3 position = new Vector3(x - offset, 2, y - offset);
                GameObject tile = Instantiate(tilePrefab, position, Quaternion.identity, transform);
                NetworkObject tileNetworkObject = tile.GetComponent<NetworkObject>();

                if (tileNetworkObject != null)
                {
                    tileNetworkObject.Spawn();
                }
                // Ensure tiles stay childs of Gridmanager
                if (IsServer)
                {
                    tileNetworkObject.TrySetParent(transform);
                }

                // Set its position in networked variables, for synchronized clicking
                Tile tileScript = tile.GetComponent<Tile>();
                tileScript.SetGridPosition(x, y);

                // Set the color of the tile
                Color tileColor = (x + y) % 2 == 0 ? color1 : color2;
                tileScript.SetBasicColor(tileColor);
            }
        }   
    }

    /// <summary>
    /// Randomly places the hidden object within the grid.
    /// </summary>
    [ServerRpc]
    public void PlaceHiddenObjectServerRpc()
    {
        Vector2Int newPosition = new Vector2Int(Random.Range(0, gridSize), Random.Range(0, gridSize));
        hiddenObjectPosition.Value = newPosition;
        Debug.Log($"[GridManager] Placed at : {newPosition}");
    }

    /// <summary>
    /// Handles player interaction with tiles, ensuring turn-based rules are respected.
    /// </summary>
   public void OnTileClicked(Vector2Int clickedPosition, ulong playerId)
    {
        Debug.Log($"[CLIENT] Player {playerId} clicked on {clickedPosition}");

        bool isPlayer1 = playerId == 0;
        if (isPlayer1 != GameManager.Instance.IsPlayer1Turn) return ;

        if (NetworkManager.Singleton.IsServer)
        {
            HandleTileClick(clickedPosition, playerId);
        }
        else
        {
            ClickTileServerRpc(clickedPosition); // Ask server to handle click
        }
    }

    /// <summary>
    /// Sends a request to the server when a tile is clicked.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void ClickTileServerRpc(Vector2Int clickedPosition, ServerRpcParams rpcParams = default)
    {
        ulong playerId = rpcParams.Receive.SenderClientId;
        HandleTileClick(clickedPosition, playerId);
    }

    /// <summary>
    /// Determines the outcome of a tile click event and updates game state.
    /// </summary>
    private void HandleTileClick(Vector2Int clickedPosition, ulong playerId)
    {
        Vector2Int hiddenPosition = hiddenObjectPosition.Value;
        Color newColor;

        if (clickedPosition == hiddenPosition) // Object found
        {
            newColor = Color.green;
            GameManager.Instance.EndGameServerRpc(playerId == 0);
        }
        else // Object not found, determine Manhatthan distance
        {
            int distance = GetManhattanDistance(clickedPosition);

            if (distance == 1)
                newColor = Color.yellow;
            else if (distance > 3)
                newColor = Color.red;
            else // Between 2 et 3
                newColor = new Color(1.0f, 0.5f, 0.0f);
        }

        
        ResetAllTilesClientRpc(); // Ensuring that every tile has the right color so
        UpdateTileColorClientRpc(clickedPosition, newColor); // Change to the right color
        GameManager.Instance.SwitchTurnServerRpc(); // Your turn is over, now it's up to the other to click
    }

    /// <summary>
    /// Resets all tile colors to their default state
    /// </summary>
    [ClientRpc]
    private void ResetAllTilesClientRpc()
    {
        foreach (Transform child in transform)
        {
            Tile tile = child.GetComponent<Tile>();
            tile.ResetColor();
        }
    }

    /// <summary>
    /// Updates the color of a specific tile based on click results.
    /// </summary>
    [ClientRpc]
    private void UpdateTileColorClientRpc(Vector2Int position, Color color)
    {
        Tile tile = FindClickedTile(position);
        if (tile != null)
        {
            tile.GetComponent<Renderer>().material.color = color;
        }
    }

    /// <summary>
    /// Calculates the Manhattan distance between a clicked position and the hidden object.
    /// </summary>
    private int GetManhattanDistance(Vector2Int clickedPosition)
    {
        Vector2Int hiddenPosition = hiddenObjectPosition.Value;
        return Mathf.Abs(clickedPosition.x - hiddenPosition.x) + Mathf.Abs(clickedPosition.y - hiddenPosition.y);
    }

    /// <summary>
    /// Finds the tile object at a specified grid position.
    /// </summary>
    private Tile FindClickedTile(Vector2Int position)
    {
        foreach (Transform child in transform)
        {
            Tile tile = child.GetComponent<Tile>();
            if (tile != null && tile.GetGridPosition() == position)
            {
                return tile;
            }
        }
        return null;
    }
}