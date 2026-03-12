using System.Collections.Generic;

public class GameStats
{
    
    public int money { get; set; }
    public Dictionary<string, int> itemsSold { get; }
    public Dictionary<string, int> itemsObtained { get; }
    public int policeRaids { get; set; }
    public int day { get; set; }
    public int totalMoneySpent { get; set; }
    public int totalBoxesUnwrapped  { get; set; }
    public int totalBoxesWrapped { get; set; }
    public List<string> unlockedCatalogues { get; }



    public GameStats()
    {
        money = 0;
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
