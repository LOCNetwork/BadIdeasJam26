using System.Collections.Generic;
using System.Data;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering.LookDev;

public class SynergyPassive : Passive
{

    /*
     * <> -> NEEDED
     * [] -> OPTIONAL
     * 
     * SYNERGY PARSER: 
     * 
     * SYNERGY:<ITEM_TO_SELL_WITH>-<MIN_AMOUNT>:<ADDITIVE/MULTIPLIER>:<VALUE>
     * 
     * <ITEM_TO_SELL_WITH> can be either an item ID or an item type. If it's an item type, specify TYPE-<ITEM_TYPE>, e.g. TYPE-School
     * 
     */




    public override string Display(WorldItem worldItem, Box box, List<string> info)
    {
        
        Dictionary<string, int> itemCounts = GetItemCounts(box, info);

        foreach (KeyValuePair<string, int> pair in itemCounts)
        {
            string itemID = pair.Key;
            int count = pair.Value;
        
            return $"[60:{worldItem.itemID}:40] synergy with [50:{itemID}:50]";
        }

        return "";
    }

    public override void ExecutePassive(Box box, List<string> info)
    {
        List<string> synergyPassives = new List<string>();

        foreach (string passive in info)
        {
            if (passive.StartsWith("SYNERGY"))
            {
                synergyPassives.Add(passive);
            }
        }

        Debug.Log($"Executing synergy passive with {synergyPassives.Count} synergies");

        foreach (string synergy in synergyPassives)
        {
            Debug.Log($"Executing synergy: {synergy}");
            string[] parts = synergy.Split(':');

            string itemToSellWith = parts[1];

            string item = itemToSellWith.Split('-')[0];

            if (item.StartsWith("TYPE"))
            {
                Debug.Log($"Synergy is based on item type");
                string itemType = itemToSellWith.Split('-')[1];
                int count = 0;


                foreach (WorldItem i in box.playerItemPool)
                {
                    Attribute att = i.GetAttribute(Attributes.ITEM_TYPE);
                    if (att != null && att.value.Equals(itemType))
                    {
                        count++;
                    }
                }

                Debug.Log($"Count for item type {itemType}: {count}, needed: {itemToSellWith.Split('-')[2]}");

                if (count >= int.Parse(itemToSellWith.Split('-')[2]))
                {
                    string rewardType = parts[2];

                    Debug.Log($"Reward type: {rewardType}, reward value: {parts[3]}");
                    if (rewardType.Equals("ADDITIVE"))
                    {
                        int rewardValue = int.Parse(parts[3], CultureInfo.InvariantCulture);
                        box.extraValue += rewardValue;
                    } else if (rewardType.Equals("MULTIPLIER"))
                    {
                        double rewardValue = double.Parse(parts[3], CultureInfo.InvariantCulture);
                        box.extraPercentage += rewardValue;
                    }
                }
            } else
            {
                Debug.Log($"Synergy is based on specific item");
                string itemID = itemToSellWith.Split('-')[0];
                string itemAmount = itemToSellWith.Split('-')[1];


                int count = 0;
                foreach (WorldItem i in box.playerItemPool)
                {
                    if (i.itemID.Equals(itemID))
                    {
                        count++;
                    }
                }

                Debug.Log($"Count for item {itemID}: {count}, needed: {itemAmount}");   

                if (count >= int.Parse(itemAmount))
                {
                    string rewardType = parts[2];

                    Debug.Log($"Reward type: {rewardType}, reward value: {parts[3]}");  
                    if (rewardType.Equals("ADDITIVE"))
                    {
                        int rewardValue = int.Parse(parts[3], CultureInfo.InvariantCulture);
                        box.extraValue += rewardValue;
                    } else if (rewardType.Equals("MULTIPLIER"))
                    {
                        double rewardValue = double.Parse(parts[3], CultureInfo.InvariantCulture);
                        box.extraPercentage += rewardValue;
                    }
                }
            }

        }



    }

    public override bool MeetsCondition(Box box, List<string> info)
    {
        if (GetItemCounts(box, info).Count > 0) return true;

        return false;
    }




    private Dictionary<string, int> GetItemCounts(Box box, List<string> info)
    {
        Dictionary<string, int> itemCounts = new Dictionary<string, int>();


        List<string> synergyPassives = new List<string>();

        foreach (string passive in info)
        {
            if (passive.StartsWith("SYNERGY"))
            {
                synergyPassives.Add(passive);
            }
        }


        foreach (string synergy in synergyPassives)
        {
            string[] parts = synergy.Split(':');

            string itemToSellWith = parts[1];

            string item = itemToSellWith.Split('-')[0];

            if (item.StartsWith("TYPE"))
            {
                string itemType = itemToSellWith.Split('-')[1];
                int count = 0;

                List<string> midItems = new List<string>();

                foreach (WorldItem i in box.playerItemPool)
                {
                    Attribute att = i.GetAttribute(Attributes.ITEM_TYPE);
                    if (att != null && att.value.Equals(itemType))
                    {
                        midItems.Add(i.itemID);
                        count++;
                    }
                }

                if (count >= int.Parse(itemToSellWith.Split('-')[2]))
                {
                    foreach (string midItem in midItems)
                    {
                        if (!itemCounts.ContainsKey(midItem)) itemCounts.Add(item, 1);
                    }
                    
                }
            } else
            {
                string itemID = itemToSellWith.Split('-')[0];
                string itemAmount = itemToSellWith.Split('-')[1];


                int count = 0;
                foreach (WorldItem i in box.playerItemPool)
                {
                    if (i.itemID.Equals(itemID))
                    {
                        count++;
                    }
                }

                if (count >= int.Parse(itemAmount))
                {
                    itemCounts.Add(itemID, int.Parse(itemAmount));
                }
            }

        }

        return itemCounts;
    }
}
