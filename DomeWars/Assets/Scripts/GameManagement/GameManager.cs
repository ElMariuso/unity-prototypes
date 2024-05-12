using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public ColorManagement colorManager;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            colorManager.GenerateRandomColors(30);
            GenerateTerritories();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void GenerateTerritories()
    {
        TerritoryManager territoryManager = GetComponent<TerritoryManager>();

        territoryManager.InitAll();
    }
}
