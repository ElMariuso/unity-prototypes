using System.Collections.Generic;
using UnityEngine;

public class TerritoryManager : MonoBehaviour
{
    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private GameObject territoryPrefab;
    public Dictionary<int, GameObject> territories = new Dictionary<int, GameObject>();

    /* Name Generator */
    [SerializeField] private TerritoryTermsList territoryTermsList;

    private Vector2 territorySize;

    private void Awake()
    {
        CalculateTerritorySize();
        GenerateTerritories();
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
        Vector3 gridCenter = new Vector3((width - 1) * territorySize.x * 0.75f / 2, (height - 1) * territorySize.y * 0.75f / 2, 0);
        Vector3 spawnPosition;
        float xOffset;
        int id = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                xOffset = (y % 2 == 0) ? 0 : territorySize.x * 0.5f;
                spawnPosition = new Vector3(x * territorySize.x * 0.75f + xOffset, y * territorySize.y * 0.75f, 0) - gridCenter;
                GenerateNewTerritory(spawnPosition, id++);
            }
        }
    }

    private void GenerateNewTerritory(Vector3 spawnPosition, int id)
    {
        GameObject newTerritoryObject = Instantiate(territoryPrefab, spawnPosition, Quaternion.identity);

        Territory newTerritory = newTerritoryObject.GetComponent<Territory>();
        newTerritory.id = id;
        newTerritory.territoryName = GenerateName();
        territories.Add(id, newTerritoryObject);
    }

    /* Generate neighborgs */

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
