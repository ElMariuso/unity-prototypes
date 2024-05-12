using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TerritoryManager : MonoBehaviour
{
    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private int initialTerritories;
    [SerializeField] private GameObject territoryPrefab;
    [SerializeField] private GameObject gangPrefab;
    public Dictionary<int, GameObject> territories = new Dictionary<int, GameObject>();
    public Dictionary<int, GameObject> gangs = new Dictionary<int, GameObject>();

    /* Name Generator */
    [SerializeField] private TerritoryTermsList territoryTermsList;

    private Vector2 territorySize;

    public void InitAll()
    {
        CalculateTerritorySize();
        GenerateTerritories();
        AssignNeighborgs();
        GenerateGangs(((width * height) / initialTerritories));   
    }

    /* Generate Territories */
    private void CalculateTerritorySize()
    {
        Renderer renderer = territoryPrefab.GetComponent<Renderer>();

        if (renderer != null)
            territorySize = new Vector2(renderer.bounds.size.x, renderer.bounds.size.y);
        else
            territorySize = new Vector2(1f, 1f);
    }

    private void GenerateTerritories()
    {
        Vector3 gridCenter;
        Vector3 spawnPosition;
        float xOffset;
        int id = 0;
        
        if (width % 2 == 0)
            gridCenter = new Vector3((width - 1) * territorySize.x * 0.75f / 2, (height - 1) * territorySize.y * 0.75f / 2, 0);
        else
            gridCenter = new Vector3(((width - 1) * (territorySize.x) * 0.75f / 2) + (territorySize.x / 2), (height - 1) * territorySize.y * 0.75f / 2, 0);
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                xOffset = (y % 2 == 0) ? 0 : territorySize.x * 0.5f;
                spawnPosition = new Vector3(x * territorySize.x + xOffset, y * territorySize.y * 0.75f, 0) - gridCenter;
                GenerateNewTerritory(spawnPosition, id++, x, y);
            }
        }
    }
    
    private void GenerateNewTerritory(Vector3 spawnPosition, int id, int x, int y)
    {
        /* Parent obj for sorting purposes */
        GameObject parentObj = GameObject.Find("Territories") ?? new GameObject("Territories");

        /* Instantiate new territory */
        GameObject newTerritoryObject = Instantiate(territoryPrefab, spawnPosition, Quaternion.identity);
        newTerritoryObject.name = "Territory" + id;

        /* Set parentObj as parent */
        newTerritoryObject.transform.SetParent(parentObj.transform);

        /* Territory attributes */
        Territory newTerritory = newTerritoryObject.GetComponent<Territory>();
        newTerritory.id = id;
        newTerritory.territoryName = GenerateName();
        newTerritory.coords = new Vector2Int(x, y);

        /* Add to the list */
        territories.Add(id, newTerritoryObject);
    }

    /* Generate neighborgs */
    private void AssignNeighborgs()
    {
        Territory territoryCode;
        Vector2Int coords;

        foreach (var territory in territories)
        {
            territoryCode = territory.Value.GetComponent<Territory>();
            coords = territoryCode.coords;

            CheckLeftAndRight(territoryCode, coords.x, coords.y);
            if (coords.y != height - 1)
                CheckBottomLeftBottomRight(territoryCode, territoryCode.id, coords.x, coords.y);
            if (coords.y != 0)
                CheckTopLeftTopRight(territoryCode, territoryCode.id, coords.x, coords.y);
        }
    }

    private void CheckLeftAndRight(Territory territory, int x, int y)
    {
        /* Left Neighborg */
        if (x > 0)
            territory.AddNeighborg((x - 1) + (y * width));

        /* Right Neighborg */
        if (x < (width - 1))
            territory.AddNeighborg((x + 1) + (y * width));
    }

    private void CheckBottomLeftBottomRight(Territory territory, int id, int x, int y)
    {
        if (y % 2 == 0)
        {
            /* Left */
            if (x != 0)
                territory.AddNeighborg(id + width - 1);

            /* Right */
            territory.AddNeighborg(id + width);
        }
        else
        {
            /* Left */
            territory.AddNeighborg(id + width);

            /* Right */
            if ((x + 1) != width)
                territory.AddNeighborg(id + width + 1);
        }
    }

    private void CheckTopLeftTopRight(Territory territory, int id, int x, int y)
    {
        if (y % 2 == 0)
        {
            /* Left */
            if (x != 0)
                territory.AddNeighborg(id - width - 1);

            /* Right */
            territory.AddNeighborg(id - width);
        }
        else
        {
            /* Left */
            territory.AddNeighborg(id - width);

            /* Right */
            if ((x + 1) != width)
                territory.AddNeighborg(id - width + 1);
        }
    }

    /* Generate Gang Positions */
    private void GenerateGangs(int amount)
    {
        GameObject parentObj = GameObject.Find("Gangs") ?? new GameObject("Gangs");
        GameObject newGang;
        Gang gangScript;

        for (int i = 0; i != amount; i++)
        {
            newGang = Instantiate(gangPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            newGang.name = "Gang" + i;

            newGang.transform.SetParent(parentObj.transform);

            gangScript = newGang.GetComponent<Gang>();
            gangScript.id = i;

            AttributeRandomMainTerritory(gangScript);

            /* Add to the list */
            gangs.Add(i, newGang);
        }
        MoreTerritories();
    }
    
    private void AttributeRandomMainTerritory(Gang gang)
    {
        var availableTerritories = territories.Where(t => t.Value.GetComponent<Territory>().gang == null).ToList();
        if (availableTerritories.Count > 0)
        {
            Territory territory = availableTerritories[Random.Range(0, availableTerritories.Count)].Value.GetComponent<Territory>();
            territory.canBeOccupied = true;
            territory.SetGang(gang);
            gang.AddTerritory(territory.id);
        }
    }

    private void MoreTerritories()
    {
        GameObject gang;
        Gang gangScript;

        for (int i = 0; i != (initialTerritories - 1); i++)
        {
            foreach (KeyValuePair<int, GameObject> pair in gangs)
            {
                gang = pair.Value;
                gangScript = gang.GetComponent<Gang>();
                ExpandTerritory(gangScript);
            }
        }
    }
    
    private void ExpandTerritory(Gang gang)
    {
        HashSet<int> allAvailableNeighbours = new HashSet<int>();

        foreach (int territoryId in gang.territorylist)
        {
            Territory currentTerritory = territories[territoryId].GetComponent<Territory>();

            foreach (int neighbourId in currentTerritory.neighbourgs)
            {
                if (territories[neighbourId].GetComponent<Territory>().gang == null)
                    allAvailableNeighbours.Add(neighbourId);
            }
        }

        if (allAvailableNeighbours.Count > 0)
        {
            int randomIndex = Random.Range(0, allAvailableNeighbours.Count);
            int newTerritoryId = allAvailableNeighbours.ElementAt(randomIndex);
            Territory newTerritory = territories[newTerritoryId].GetComponent<Territory>();
            newTerritory.canBeOccupied = true;
            newTerritory.SetGang(gang);
            gang.AddTerritory(newTerritoryId);
        }
    }


    /* Name Generator */
    private string GenerateName()
    {
        int rd = Random.Range(1, 5);
        string adjectiveName = territoryTermsList.Adjectives[Random.Range(0, territoryTermsList.Adjectives.Count)];
        string placeName = territoryTermsList.Locations[Random.Range(0, territoryTermsList.Locations.Count)];
        string gangName = territoryTermsList.GangNames[Random.Range(0, territoryTermsList.GangNames.Count)];

        switch (rd)
        {
            case 1:
                return (GangPlace(gangName, placeName));
            case 2:
                return (AdjectivePlaceGang(adjectiveName, placeName, gangName));
            case 3:
                return (PlaceOfTheGang(placeName, gangName));
            case 4:
                return (AdjectiveGangOfThePlace(adjectiveName, gangName, placeName));
            case 5:
                return (GangAdjectivePlace(gangName, adjectiveName, placeName));
            default:
                return ("ERROR PLEASE FIX");
        }
    }

    private string GangPlace(string gangName, string placeName) { return ($"{gangName} {placeName}"); }
    private string AdjectivePlaceGang(string adjectiveName, string placeName, string gangName) { return ($"{adjectiveName} {placeName} {gangName}"); }
    private string PlaceOfTheGang(string placeName, string gangName) { return ($"{placeName} of the {gangName}"); }
    private string AdjectiveGangOfThePlace(string adjectiveName, string gangName, string placeName) { return ($"{adjectiveName} {gangName} of the {placeName}"); }
    private string GangAdjectivePlace(string gangName, string adjectiveName, string placeName) { return ($"{gangName} {adjectiveName} {placeName}"); }
}
