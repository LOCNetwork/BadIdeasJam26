using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private Dictionary<string, Item> loadedItems;

    public static GameManager instance;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        instance = this;
        
        loadItems();
        
    }

    // Update is called once per frame
    void Update()
    {
    }





    public Item GetItemByID(string itemID)
    {
        return loadedItems.GetValueOrDefault(itemID, null);
    }
    
    
    
    

    // Load all scriptable objects from Item folder so it can be accessed from the list
    private void loadItems()
    {
        loadedItems = new Dictionary<string, Item>();

        Item[] itemsLoaded = Resources.LoadAll<Item>("Items");

        foreach (Item item in itemsLoaded)
        {
            loadedItems.Add(item.itemID, item);
        }
    }
    
}