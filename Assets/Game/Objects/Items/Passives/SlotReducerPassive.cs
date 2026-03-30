using System.Collections.Generic;
using UnityEngine;

public class SlotReducerPassive : Passive
{

    /*
    * <> -> NEEDED
    * [] -> OPTIONAL
    * 
    * SLOT REDUCER PARSER: 
    * 
    * SLOT_REDUCER:<ACCOUNTS_SAME_ITEM>:<MAX_AMOUNT>:<VALUE>
    * 
    * <ACCOUNTS_SAME_ITEM>, boolean. TRUE OR FALSE. If TRUE, the passive affects items of the same type, if FALSE, it doesn't.
    * 
    */

    public new int priority = 0;

    public override string Display(WorldItem worldItem, Box box, List<string> info)
    {
        string passiveInfo = null;
        foreach (string passive in worldItem.passivesInfo)
        {
            if (passive.StartsWith("SLOT_REDUCER"))
            {
                passiveInfo = passive;
                break;
            }
        }


        if (passiveInfo == null) return "";


        string[] separatedInfo = passiveInfo.Split(':');

        string boolString = separatedInfo[1];
        string maxAmountString = separatedInfo[2];
        string valueString = separatedInfo[3];

        bool accountsSameItem = bool.Parse(boolString);
        int maxAmount = int.Parse(maxAmountString);

        int amount = 0;
        List<string> affectedItems = new List<string>();
        foreach (WorldItem item in box.playerItemPool)
        {
            if (amount >= maxAmount) break;

            if (!accountsSameItem && !item.itemID.Equals(worldItem.itemID))
            {
                item.boxSlots = Mathf.Max(0, item.boxSlots - int.Parse((valueString)));
                affectedItems.Add(item.itemID);
                amount++;
            } else if (accountsSameItem && item != worldItem)
            {
                item.boxSlots = Mathf.Max(0, item.boxSlots - int.Parse((valueString)));
                affectedItems.Add(item.itemID);
                amount++;
            }
        }

        string s = $"Reduced {int.Parse(valueString)} box slots of";

        foreach (string itemID in affectedItems)
        {
            s += $" [10:{itemID}:40]";
        }

        return s;
    }

    public override void ExecutePassive(WorldItem worldItem, Box box, List<string> info)
    {
        string passiveInfo = null;
        foreach (string passive in worldItem.passivesInfo)
        {
            if (passive.StartsWith("SLOT_REDUCER"))
            {
                passiveInfo = passive;
                break;
            }
        }

        string[] separatedInfo = passiveInfo.Split(':');

        string boolString = separatedInfo[1];
        string maxAmountString = separatedInfo[2];
        string valueString = separatedInfo[3];

        bool accountsSameItem = bool.Parse(boolString);
        int maxAmount = int.Parse(maxAmountString);

        int amount = 0;
        foreach (WorldItem item in box.playerItemPool)
        {
            if (amount >= maxAmount) break;

            if (!accountsSameItem && !item.itemID.Equals(worldItem.itemID))
            {
                item.boxSlots = Mathf.Max(0, item.boxSlots - int.Parse((valueString)));
                amount++;
            } else if (accountsSameItem && item != worldItem)
            {
                item.boxSlots = Mathf.Max(0, item.boxSlots - int.Parse((valueString)));
                amount++;
            }
        }


    }

    public override bool MeetsCondition(WorldItem worldItem, Box box, List<string> info)
    {
        string passiveInfo = null;
        foreach (string passive in worldItem.passivesInfo)
        {
            if (passive.StartsWith("SLOT_REDUCER"))
            {
                passiveInfo = passive;
                break;
            }
        }

        string[] separatedInfo = passiveInfo.Split(':');

        string boolString = separatedInfo[1];
        string maxAmountString = separatedInfo[2];
        string valueString = separatedInfo[3];

        bool accountsSameItem = bool.Parse(boolString);

        foreach (WorldItem item in box.playerItemPool)
        {

            if (!accountsSameItem && !item.itemID.Equals(worldItem.itemID))
            {
                return true;
            } else if (accountsSameItem && item != worldItem) return true;
        }

        return false;
    }
}
