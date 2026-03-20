using System.Collections.Generic;
using UnityEngine;

public class ItemManager
{
    


    public ItemManager()
    {
        Init();
    }


    private void Init()
    {
        Dictionary<string, Item> loadedItems = GameManager.instance.loadedItems;


        SynergyPassive sp = new SynergyPassive();

        loadedItems["NOTEBOOK"].passives.Add(sp);
    }


}
