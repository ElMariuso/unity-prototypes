using System.Collections.Generic;
using UnityEngine;

public class Territory : MonoBehaviour
{
    public int id;
    public string territoryName;
    public List<int> neighborgs;
    // buildings
    // private short danger = 0;
    // units
    // public Gang gang;

    /* Utils */
    private Renderer territoryRenderer;

    private void Awake()
    {
        territoryRenderer = GetComponent<Renderer>();
    }

    /* Neighborgs */
    public void AddNeighborg(int id) { neighborgs.Add(id); }
    private void changeColor(Color newColor) { territoryRenderer.material.color = newColor; }
}
