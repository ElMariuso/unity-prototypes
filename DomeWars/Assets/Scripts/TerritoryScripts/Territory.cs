using System.Collections.Generic;
using UnityEngine;

public class Territory : MonoBehaviour
{
    public int id;
    public string territoryName;
    public List<int> neighbourgs = new List<int>();
    // buildings
    // private short danger = 0;
    public List<Unit> units;
    public Gang gang = null;
    
    public Vector2Int coords;

    /* Utils */
    private Renderer territoryRenderer;

    private void Awake()
    {
        territoryRenderer = GetComponent<Renderer>();
    }

    public void AddNeighborg(int id) { neighbourgs.Add(id); }

    /* Gang Management */
    public void SetGang(Gang newGang)
    { 
        gang = newGang;

        changeColor(newGang.color);
    }
    private void changeColor(Color newColor) { territoryRenderer.material.color = newColor; }
}
