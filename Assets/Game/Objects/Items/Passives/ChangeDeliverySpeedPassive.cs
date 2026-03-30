using System.Collections.Generic;

public class ChangeDeliverySpeedPassive : Passive
{

    /*
    * <> -> NEEDED
    * [] -> OPTIONAL
    * 
    * CHANGE DELIVERY SPEED PARSER: 
    * 
    * CHANGE_DS:<ITEM_ID>:<CHANGE_AMOUNT>
    * 
    * 
    */

    public new int priority = 0;

    public override string Display(WorldItem worldItem, Box box, List<string> info)
    {
        string passiveInfo = null;
        foreach (string passive in worldItem.passivesInfo)
        {
            if (passive.StartsWith("CHANGE_DS"))
            {
                passiveInfo = passive;
                break;
            }
        }

        if (passiveInfo == null) return "";

        string[] requiredInfo = passiveInfo.Split(':');

        string item = requiredInfo[1];
        int changeAmount = int.Parse(requiredInfo[2]);


        int count = 0;

        foreach (WorldItem it in box.playerItemPool)
        {
            if (it.itemID.Equals(item))
            {
                count++;
            }
        }

        int change = count * changeAmount;

        int current = int.Parse(worldItem.GetAttribute(Attributes.SELL_TIME).value);

        string speed = "";

        switch (UnityEngine.Mathf.Min(3, UnityEngine.Mathf.Max(0, current + change)))
        {
            case 0:
                speed = "Very slow";
                break;
            case 1:
                speed = "Slow";
                break;
            case 2:
                speed = "Fast";
                break;
            case 3:
                speed = "Very Fast";
                break;
        }


        return $"The item [10:{worldItem.itemID}:40] changed its delivery speed to <color=green>{speed}</color>!";
    }

    public override void ExecutePassive(WorldItem worldItem, Box box, List<string> info)
    {
        string passiveInfo = null;
        foreach (string passive in worldItem.passivesInfo)
        {
            if (passive.StartsWith("CHANGE_DS"))
            {
                passiveInfo = passive;
                break;
            }
        }

        if (passiveInfo == null) return;

        string[] requiredInfo = passiveInfo.Split(':');

        string item = requiredInfo[1];
        int changeAmount = int.Parse(requiredInfo[2]);


        int count = 0;

        foreach (WorldItem it in box.playerItemPool)
        {
            if (it.itemID.Equals(item))
            {
                count++;
            }
        }

        int change = count * changeAmount;

        int current = int.Parse(worldItem.GetAttribute(Attributes.SELL_TIME).value);

        if (current + change > 3)
        {
            worldItem.GetAttribute(Attributes.SELL_TIME).value = "3";
        } else if (current + change < 0)
        {
            worldItem.GetAttribute(Attributes.SELL_TIME).value = "0";
        } else
        {
            worldItem.GetAttribute(Attributes.SELL_TIME).value = "" + (current + change);
        }


    }

    public override bool MeetsCondition(WorldItem worldItem, Box box, List<string> info)
    {
        string passiveInfo = null;
        foreach (string passive in worldItem.passivesInfo)
        {
            if (passive.StartsWith("CHANGE_DS"))
            {
                passiveInfo = passive;
                break;
            }
        }

        if (passiveInfo == null) return false;

        string[] requiredInfo = passiveInfo.Split(':');

        string item = requiredInfo[1];
        int changeAmount = int.Parse(requiredInfo[2]);


        int count = -1;

        foreach (WorldItem it in box.playerItemPool)
        {
            if (it.itemID.Equals(item))
            {
                count++;
            }
        }

        int change = count * changeAmount;

        return change > 0;
    }
}
