using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public List<Color> gangColors { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            GenerateRandomColors(30);
            GenerateTerritories();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /* Color Management */
    private void GenerateRandomColors(int numberOfColors)
    {
        float r, g, b;
        gangColors = new List<Color>();

        for (int i = 0; i < numberOfColors; i++)
        {
            r = Random.Range(0f, 1f);
            g = Random.Range(0f, 1f);
            b = Random.Range(0f, 1f);
            gangColors.Add(new Color(r, g, b));
        }
    }

    public Color GetAColor()
    {
        if (gangColors.Count > 0)
        {
            int index = Random.Range(0, gangColors.Count);
            Color selectedColor = gangColors[index];
            gangColors.RemoveAt(index);
            return selectedColor;
        }
        else
            return Color.black;
    }

    private void GenerateTerritories()
    {
        TerritoryManager territoryManager = GetComponent<TerritoryManager>();

        territoryManager.InitAll();
    }
}
