using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[Serializable]
public class GameStats
{


    public int money;
    public Dictionary<string, int> itemsSold;
    public Dictionary<string, int> itemsObtained;
    public int policeRaids;
    public int day;
    public int totalMoneySpent; 
    public int totalBoxesUnwrapped;
    public int totalBoxesWrapped;
    public List<string> unlockedCatalogues;



    public GameStats()
    {
        money = 25;
        itemsSold = new Dictionary<string, int>();
        itemsObtained = new Dictionary<string, int>();
        policeRaids = 0;
        day = 1;
        totalMoneySpent = 0;
        totalBoxesUnwrapped = 0;
        totalBoxesWrapped = 0;
        
        unlockedCatalogues = new List<string>();
        unlockedCatalogues.Add("STATIONERY");
    }
    
    



}
