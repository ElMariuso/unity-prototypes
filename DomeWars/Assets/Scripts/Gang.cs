using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gang : MonoBehaviour
{
    public int id;
    private string name;
    private string color;
    private int mainTerritoryId;
    private List<int> territorylist = new List<int> ();
    private Dictionary<Gang,int> relation = new Dictionary<Gang, int> ();
    private int money;
    private int publicOpinion;
    private List<Unit> units = new List<Unit> ();
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
