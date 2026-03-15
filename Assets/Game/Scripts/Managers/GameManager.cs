using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private Dictionary<string, Item> loadedItems;

    public static GameManager instance;

    // Managers
    public SellManager sellManager;

    // Game stats
    public GameStats gameStats;

    public float currentTimer; // Timer that starts in the beginning of each day (Resets each day)
    public float timer; // Timer that never resets (Game timer)

    // UI Font
    public TMP_FontAsset fontAsset;

    // Interfaces
    public TextMeshProUGUI moneyUI;


    // Box sell animation
    public RectTransform container; // Parent

    private bool test = false;


    void Start()
    {
        instance = this;
        gameStats = new GameStats();
        
        sellManager = new SellManager(container, fontAsset);
        
        LoadItems();
        
    }


    void Update()
    {
        currentTimer += Time.deltaTime;
        timer += Time.deltaTime;

        if (!test)
        {
            test = true;
            sellManager.DisplayItemPassive("TEST 1 [50:CALCULATOR:50] [0:PENCIL:50] This is a test to render a pencil object [50:PENCIL:0]");
        }


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

        if (sellManager.boxesQueue.Count == 0) return;

        Box box = sellManager.boxesQueue.Peek();


        if (!sellManager.boxesOnSale.ContainsKey(box)) // This box is on animation
        {
            return;
        }


        sellManager.boxesOnSale.TryGetValue(box, out float insertTime);

        if (insertTime + sellManager.sellSpeedArray[box.sellTimeIndex] <= timer)
        {
            sellManager.CompleteBoxSale(box);
        }





    }

    public void UpdateMoneyUI()
    {
        if (moneyUI != null)
        {
            moneyUI.text = $"{gameStats.money} $";
        }

    }

}