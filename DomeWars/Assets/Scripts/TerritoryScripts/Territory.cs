using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

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
    [SerializeField] private GameObject highlight;
    private Renderer territoryRenderer;
    public bool canBeOccupied = false;

    private void Awake()
    {
        territoryRenderer = GetComponent<Renderer>();
    }

    public void AddNeighborg(int id) { neighbourgs.Add(id); }

    /*** Mouse Management ***/
    /* Highlight */
    public void OnMouseEnter()
    {
        if (!canBeOccupied) return ;
        highlight.SetActive(true);
    }

    public void OnMouseExit()
    {
        if (!canBeOccupied) return ;
        highlight.SetActive(false);
    }

    /* Click */
    private void OnMouseDown()
    {
        Debug.Log("HEY" + id);
    }

    /* Gang Management */
    public void SetGang(Gang newGang)
    { 
        if (!canBeOccupied) return ;
        gang = newGang;

        changeColor(newGang.color);
    }
    private void changeColor(Color newColor) { territoryRenderer.material.color = newColor; }
}
