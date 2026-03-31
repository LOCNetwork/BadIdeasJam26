using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class ItemRequirementPassive : Passive
{

    /*
    * <> -> NEEDED
    * [] -> OPTIONAL
    * 
    * ITEM REQUIREMENT PARSER: 
    * 
    * ITEM_REQUIREMENT:<LIST_OF_ITEMS>:<PENALTY_PERCENTAGE>
    * 
    * <LIST_OF_ITEMS>, all items needed to be sold with, separated with '-'. To specify a type, put TYPE/<TYPE_NAME> instead of ITEM_ID
    * 
    */

    public new int priority = 1;

    public override string Display(WorldItem worldItem, Box box, List<string> info)
    {
        string passiveInfo = null;
        foreach (string passive in worldItem.passivesInfo)
        {
            if (passive.StartsWith("ITEM_REQUIREMENT"))
            {
                passiveInfo = passive;
                break;
            }
        }

        if (passiveInfo == null) return "";

        string[] requiredInfo = passiveInfo.Split(':');

        string listItems = requiredInfo[1];
        float penaltyPercentage = float.Parse(requiredInfo[2], CultureInfo.InvariantCulture);

        string[] items = listItems.Split("-");

        bool meetsCondition = true;

        int itemIndex = 0;
        while (itemIndex < items.Length && meetsCondition)
        {
            string item = items[itemIndex++];

            if (item.Contains("TYPE"))
            {
                string type = item.Split("/")[1];

                bool found = false;
                foreach (WorldItem itemBox in box.playerItemPool)
                {
                    Attribute att = itemBox.GetAttribute(Attributes.ITEM_TYPE);
                    if (att != null && att.value.Contains(type))
                    {
                        found = true;
                        break;
                    }
                }

                meetsCondition = found;
            } else
            {
                bool found = false;
                foreach (WorldItem itemBox in box.playerItemPool)
                {
                    if (itemBox.itemID.Equals(item))
                    {
                        found = true;
                        break;
                    }
                }

                meetsCondition = found;
            }
        }


        if (!meetsCondition)
        {
            return $"The item [10:{worldItem.itemID}:40] didn't meet the requirements losing <color=red>{penaltyPercentage}%</color> of its value!";
        }

        return "";
    }

    public override void ExecutePassive(WorldItem worldItem, Box box, List<string> info)
    {
        string passiveInfo = null;
        foreach (string passive in worldItem.passivesInfo)
        {
            if (passive.StartsWith("ITEM_REQUIREMENT"))
            {
                passiveInfo = passive;
                break;
            }
        }

        if (passiveInfo == null) return;

        string[] requiredInfo = passiveInfo.Split(':');

        string listItems = requiredInfo[1];
        float penaltyPercentage = float.Parse(requiredInfo[2], CultureInfo.InvariantCulture);

        string[] items = listItems.Split("-");

        bool meetsCondition = true;

        int itemIndex = 0;
        while (itemIndex < items.Length && meetsCondition)
        {
            string item = items[itemIndex++];

            if (item.Contains("TYPE"))
            {
                string type = item.Split("/")[1];

                bool found = false;
                foreach (WorldItem itemBox in box.playerItemPool)
                {
                    Attribute att = itemBox.GetAttribute(Attributes.ITEM_TYPE);
                    if (att != null && att.value.Equals(type))
                    {
                        found = true;
                        break;
                    }
                }

                meetsCondition = found;
            } else
            {
                bool found = false;
                foreach (WorldItem itemBox in box.playerItemPool)
                {
                    if (itemBox.itemID.Equals(item))
                    {
                        found = true;
                        break;
                    }
                }

                meetsCondition = found;
            }
        }


        if (!meetsCondition)
        {
            // Apply penalty
            worldItem.value -= Mathf.RoundToInt((float) worldItem.value * penaltyPercentage);
        }


    }

    public override bool MeetsCondition(WorldItem worldItem, Box box, List<string> info)
    {
        string passiveInfo = null;
        foreach (string passive in worldItem.passivesInfo)
        {
            if (passive.StartsWith("ITEM_REQUIREMENT"))
            {
                passiveInfo = passive;
                break;
            }
        }

        if (passiveInfo == null) return false;

        string[] requiredInfo = passiveInfo.Split(':');

        string listItems = requiredInfo[1];
        float penaltyPercentage = float.Parse(requiredInfo[2], CultureInfo.InvariantCulture);

        string[] items = listItems.Split("-");

        bool meetsCondition = true;

        int itemIndex = 0;
        while (itemIndex < items.Length && meetsCondition)
        {
            string item = items[itemIndex++];

            if (item.Contains("TYPE"))
            {
                string type = item.Split("/")[1];

                bool found = false;
                foreach (WorldItem itemBox in box.playerItemPool)
                {
                    Attribute att = itemBox.GetAttribute(Attributes.ITEM_TYPE);
                    if (att != null && att.value.Equals(type))
                    {
                        found = true;
                        break;
                    }
                }

                meetsCondition = found;
            } else
            {
                bool found = false;
                foreach (WorldItem itemBox in box.playerItemPool)
                {
                    if (itemBox.itemID.Equals(item))
                    {
                        found = true;
                        break;
                    }
                }

                meetsCondition = found;
            }
        }


        return !meetsCondition;
    }
}
