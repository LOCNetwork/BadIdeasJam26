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

    // Box prefabs
    public GameObject playerSmallBoxPrefab;
    public GameObject playerMediumBoxPrefab;
    public GameObject playerLargeBoxPrefab;

    // Truck object
    public GameObject truckObject;
    public Sprite truckBodySprite;
    public GameObject truckBack;

  

    void Start()
    {
        instance = this;
        gameStats = new GameStats();
        
        sellManager = new SellManager(container, fontAsset, this);
        
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

        GameObject boxObject = sellManager.boxesQueue.Peek();

        Box box = boxObject.GetComponent<Box>();

        if (!sellManager.boxesOnSale.ContainsKey(box.guid)) // This box is on animation
        {
            return;
        }


        sellManager.boxesOnSale.TryGetValue(box.guid, out KeyValuePair<Box, float> info);

        Debug.Log(info.Value + sellManager.sellSpeedArray[box.sellTimeIndex]);
        Debug.Log(timer);
        Debug.Log(info.Value + sellManager.sellSpeedArray[box.sellTimeIndex] <= timer);
        if (info.Value + sellManager.sellSpeedArray[box.sellTimeIndex] <= timer)
        {
            Debug.Log("SOLD BOX");
            sellManager.CompleteBoxSale(info.Key);
        }

    }

    public void UpdateMoneyUI()
    {
        if (moneyUI != null)
        {
            moneyUI.text = $"{gameStats.money}";
        }

    }

}