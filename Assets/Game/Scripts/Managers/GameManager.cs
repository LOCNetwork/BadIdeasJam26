using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private Dictionary<string, Item> loadedItems;

    public static GameManager instance;
    
    // Managers
    private SellManager sellManager { get; set; }
    
    // Game stats
    public GameStats gameStats { get; set;  }

    public float currentTimer; // Timer that starts in the beginning of each day (Resets each day)
    public float timer; // Timer that never resets (Game timer)
    
    

    
    void Start()
    {
        instance = this;
        gameStats = new GameStats();
        
        sellManager = new SellManager();
        
        LoadItems();
        
    }


    void Update()
    {
        currentTimer += Time.deltaTime;
        timer += Time.deltaTime;


        HandleSells();
        UpdateMoneyUI();
    }



    public string GetTimeFormat()
    {
        int minutes = Mathf.FloorToInt(timer / 60F);
        int seconds = Mathf.FloorToInt(timer - minutes * 60);

        string timeFormatted = string.Format("{0:0}:{1:00}", minutes, seconds);

        return timeFormatted;
    }


    public Item GetItemByID(string itemID)
    {
        return loadedItems.GetValueOrDefault(itemID, null);
    }
    
    
    
    

    // Load all scriptable objects from Resources/Items folder so it can be accessed from the list
    private void LoadItems()
    {
        loadedItems = new Dictionary<string, Item>();

        Item[] itemsLoaded = Resources.LoadAll<Item>("Items");

        foreach (Item item in itemsLoaded)
        {
            loadedItems.Add(item.itemID, item);
        }
    }

    private void HandleSells()
    {


        foreach (KeyValuePair<WorldItem, float> item in sellManager.itemsOnSale)
        {
            Attribute sellSpeed = item.Key.GetAttribute(Attributes.SELL_TIME);

            int sellMillis = 0;
            switch (sellSpeed.value)
            {
                case "SLOW":
                    sellMillis = sellManager.SECONDS_TO_SELL_SLOW * 1_000;
                    break;
                case "FAST":
                    sellMillis = sellManager.SECONDS_TO_SELL_FAST * 1_000;
                    break;
                case "VERY_FAST":
                    sellMillis = sellManager.SECONDS_TO_SELL_VERY_FAST * 1_000;
                    break;
            }


            if (item.Value + sellMillis <= currentTimer)
            {
                sellManager.SellItem(item.Key);
            }
        }
        
        
        
    }

    private void UpdateMoneyUI()
    {
        
    }
    
}