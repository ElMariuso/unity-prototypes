using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TerritoryTermsList", menuName = "GameData/TerritoryTermsList", order = 1)]
public class TerritoryTermsList : ScriptableObject
{
    public List<string> Adjectives = new List<string>();
    public List<string> GangNames = new List<string>();
    public List<string> Locations = new List<string>();
}
