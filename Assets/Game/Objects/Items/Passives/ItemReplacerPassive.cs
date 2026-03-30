using System.Collections.Generic;

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
        for (index = 0; index < box.playerItemPool.Count; index++)
        {
            if (box.playerItemPool[index].itemID.Equals(worldItem.itemID))
            {
                break;
            }
        }

        if (index + 1 >= box.playerItemPool.Count)
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
        for (index = 0; index < box.playerItemPool.Count; index++)
        {
            if (box.playerItemPool[index].itemID.Equals(worldItem.itemID))
            {
                break;
            }
        }

        if (index + 1 == box.playerItemPool.Count)
        {
            return false;
        }

        return true;
    }
}
