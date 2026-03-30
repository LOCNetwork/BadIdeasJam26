using System.Collections.Generic;
using UnityEngine;

public class ItemReplacerPassive : Passive
{

    /*
    * <> -> NEEDED
    * [] -> OPTIONAL
    * 
    * ITEM REPLACER PARSER: 
    * 
    * ITEM_REPLACER
    * 
    */

    public new int priority = 1;

    public override string Display(WorldItem worldItem, Box box, List<string> info)
    {
        return "";
    }

    public override void ExecutePassive(WorldItem worldItem, Box box, List<string> info)
    {

        int index = 0;
        foreach (WorldItem item in box.playerItemPool)
        {
            if (item.itemID.Equals(item.itemID))
            {
                break;
            }
            index++;
        }

        if (index + 1 == box.playerItemPool.Count)
        {
            return;
        }

        WorldItem itemCopy = box.playerItemPool[index + 1].Clone();

        box.playerItemPool.RemoveAt(index);
        box.playerItemPool.Add(itemCopy);
    }

    public override bool MeetsCondition(WorldItem worldItem, Box box, List<string> info)
    {
        int index = 0;
        foreach (WorldItem item in box.playerItemPool)
        {
            if (item.itemID.Equals(item.itemID))
            {
                break;
            }
            index++;
        }

        if (index + 1 == box.playerItemPool.Count)
        {
            return false;
        }

        return true;
    }
}
