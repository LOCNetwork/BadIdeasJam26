using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public Dictionary<string, Item> loadedItems;

    public static GameManager instance;

    // Player object
    public GameObject player;

    // Managers
    public SellManager sellManager;
    public ItemManager itemManager;

    // Game stats
    public GameStats gameStats;

    [Header("Day System")]
    public int currentDay = 0;
    public float dayDurationSeconds = 120f;


    public float currentTimer; // Timer that starts in the beginning of each day (Resets each day)
    public float timer; // Timer that never resets (Game timer)

    // UI Font
    public TMP_FontAsset fontAsset;

    // Interfaces
    public TextMeshProUGUI moneyUI;


    // Box sell animation
    public RectTransform container; // Parent


    // Box prefabs
    public GameObject playerSmallBoxPrefab;
    public GameObject playerMediumBoxPrefab;
    public GameObject playerLargeBoxPrefab;

    // Truck object
    public GameObject truckObject;
    public Sprite truckBodySprite;
    public GameObject truckBack;

    // Game over UI
    [SerializeField] private GameObject gameOverScreen;

    // Fade
    [SerializeField] private Image fadeImage;




    void Start()
    {
        StartCoroutine(UIManager.FadeInCoroutine(fadeImage, 250f));

        instance = this;
        gameStats = new GameStats();
        
        sellManager = new SellManager(container, fontAsset, this);
        
        LoadItems();
        
        itemManager = new ItemManager();

        player.GetComponent<Player>().SetMovementLocked(false);
    }


    void Update()
    {
        currentTimer += Time.deltaTime;
        timer += Time.deltaTime;


        HandleSells();
        UpdateMoneyUI();
    }


    public void GameOver()
    {
        // Stop player movement
        player.GetComponent<Player>().SetMovementLocked(true);

        // Play Game Over animation
        GameOverVisuals();
    }


    private IEnumerator GameOverVisuals()
    {
        // Pre-animation start time?
        yield return new WaitForSeconds(2f);


        // Play animation
        gameOverScreen.SetActive(true);
        // REPLACE WITH ANIMATION CODE



        yield return new WaitForSeconds(2f);

        BackToMainMenu();


    }


    public void BackToMainMenu()
    {
        
    }

    public void Restart()
    {

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


    public void ResetCurrentDayTimer()
    {
        currentTimer = 0f;
    }

    public void AdvanceDay()
    {
        currentDay++;
        currentTimer = 0f;
    }



    // Load all scriptable objects from Resources/Items folder so it can be accessed from the list
    private void LoadItems()
    {
        loadedItems = new Dictionary<string, Item>();

        Item[] itemsLoaded = Resources.LoadAll<Item>("Items");

        foreach (Item item in itemsLoaded)
        {
            item.passives = new List<Passive>();

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

        if (info.Value + box.sellTime <= timer)
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