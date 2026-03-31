using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class DeliverySpeedExtraPassive : Passive
{

    /*
    * <> -> NEEDED
    * [] -> OPTIONAL
    * 
    * DELIVERY SPEED EXTRA PARSER: 
    * 
    * DS_EXTRA_MONEY:<EXTRA_KIT_VALUE_PER_SECOND>
    * 
    * 
    */

    public new int priority = 2;

    public override string Display(WorldItem worldItem, Box box, List<string> info)
    {

        string passiveInfo = null;
        foreach (string passive in worldItem.passivesInfo)
        {
            if (passive.StartsWith("DS_EXTRA_MONEY"))
            {
                passiveInfo = passive;
                break;
            }
        }

        if (passiveInfo == null) return "";

        string[] requiredInfo = passiveInfo.Split(':');

        double extraKitValuePerSecond = double.Parse(requiredInfo[1], CultureInfo.InvariantCulture);


        // Calculate sell time
        int secondsOfDelivery = 0;

        foreach (WorldItem item in box.playerItemPool)
        {
            secondsOfDelivery += (int)GameManager.instance.sellManager.GetSellTimeByIndex(int.Parse(item.GetAttribute(Attributes.SELL_TIME).value));
        }

        double totalValue = extraKitValuePerSecond * secondsOfDelivery;

        string message = "";

        if (extraKitValuePerSecond > 0)
        {
            message = $"The item [10:{worldItem.itemID}:40] has added an extra <color=green>{totalValue * 100}%</color> kit value (delivery speed extra)!";
        } else
        {
            message = $"The item [10:{worldItem.itemID}:40] has removed a total of <color=red>{Mathf.RoundToInt(Mathf.Abs((float) totalValue * 100))}%</color> kit value (delivery speed penalty)!";
        }

        return message;
    }

    public override void ExecutePassive(WorldItem worldItem, Box box, List<string> info)
    {
        string passiveInfo = null;
        foreach (string passive in worldItem.passivesInfo)
        {
            if (passive.StartsWith("DS_EXTRA_MONEY"))
            {
                passiveInfo = passive;
                break;
            }
        }

        if (passiveInfo == null) return;

        string[] requiredInfo = passiveInfo.Split(':');

        double extraKitValuePerSecond = double.Parse(requiredInfo[1], CultureInfo.InvariantCulture);


        // Calculate sell time
        double sellTimeIndex = 0;

        foreach (WorldItem item in box.playerItemPool)
        {
            sellTimeIndex += int.Parse(item.GetAttribute(Attributes.SELL_TIME).value);
        }

        sellTimeIndex /= box.playerItemPool.Count;

        int finalIndex = (int) System.Math.Round(sellTimeIndex);

        
        float secondsOfDelivery = GameManager.instance.sellManager.GetSellTimeByIndex(finalIndex);


        box.extraPercentage += extraKitValuePerSecond * secondsOfDelivery;
    }

    public override bool MeetsCondition(WorldItem worldItem, Box box, List<string> info)
    {
        return true;
    }
}
