using System.Collections.Generic;

public class SellManager
{
    
    public Dictionary<WorldItem, float> itemsOnSale { get; set; }

    public int SECONDS_TO_SELL_SLOW { get; set; }
    public int SECONDS_TO_SELL_FAST  { get; set;  }
    public int SECONDS_TO_SELL_VERY_FAST { get; set;  }
    
    
    public SellManager()
    {
        itemsOnSale = new Dictionary<WorldItem, float>();
        
        SECONDS_TO_SELL_SLOW = 300;
        SECONDS_TO_SELL_FAST = 100;
        SECONDS_TO_SELL_VERY_FAST = 20;
    }
    
    

    public bool SellBox(Box box)
    {
        if (box.Type == BoxType.Delivery) return false;

        foreach (var item in box.playerItemPool)
        {
            itemsOnSale.Add(item, GameManager.instance.timer);
        }

        return true;
    }

    public void SellItem(WorldItem item)
    {
        itemsOnSale.Remove(item);

        GameManager.instance.gameStats.money += item.value;
    }
    
    
    
    
}
