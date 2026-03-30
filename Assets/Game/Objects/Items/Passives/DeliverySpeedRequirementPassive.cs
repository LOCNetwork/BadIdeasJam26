using NUnit.Framework.Internal.Execution;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

public class DeliverySpeedRequirementPassive : Passive
{

    /*
    * <> -> NEEDED
    * [] -> OPTIONAL
    * 
    * DELIVERY SPEED REQUIREMENT PARSER: 
    * 
    * DS_REQUIREMENT:<MAXIMUM_SECONDS_WITHOUT_PENALTY>:<PENALTY_PERCENTAGE>:<ITEM/BOX>
    * 
    */

    public new int priority = 3;

    public override string Display(WorldItem worldItem, Box box, List<string> info)
    {
        string passiveInfo = null;
        foreach (string passive in worldItem.passivesInfo)
        {
            if (passive.StartsWith("DS_REQUIREMENT"))
            {
                passiveInfo = passive;
                break;
            }
        }

        if (passiveInfo == null) return "";

        string[] requiredInfo = passiveInfo.Split(':');

        int maxSeconds = int.Parse(requiredInfo[1]);
        float penaltyPercentage = float.Parse(requiredInfo[2], CultureInfo.InvariantCulture);
        string type = requiredInfo[3];



        // Calculate sell time
        int secondsOfDelivery = 0;

        foreach (WorldItem item in box.playerItemPool)
        {
            secondsOfDelivery += (int)GameManager.instance.sellManager.GetSellTimeByIndex(int.Parse(item.GetAttribute(Attributes.SELL_TIME).value));
        }

        if (secondsOfDelivery > maxSeconds)
        {
            string message = $"The delivery will take more than {maxSeconds}! The ";

            if (type.Equals("BOX"))
            {
                message += $"kit ";
            } else
            {
                message += $"item [10:{worldItem.itemID}:40] ";
            }

            message += $"lost a {penaltyPercentage}% of its value";

            return message;
        }

        return "";
    }

    public override void ExecutePassive(WorldItem worldItem, Box box, List<string> info)
    {
        string passiveInfo = null;
        foreach (string passive in worldItem.passivesInfo)
        {
            if (passive.StartsWith("DS_REQUIREMENT"))
            {
                passiveInfo = passive;
                break;
            }
        }

        if (passiveInfo == null) return;

        string[] requiredInfo = passiveInfo.Split(':');

        int maxSeconds = int.Parse(requiredInfo[1]);
        float penaltyPercentage = float.Parse(requiredInfo[2], CultureInfo.InvariantCulture);
        string type = requiredInfo[3];



        // Calculate sell time
        double sellTimeIndex = 0;

        foreach (WorldItem item in box.playerItemPool)
        {
            sellTimeIndex += int.Parse(item.GetAttribute(Attributes.SELL_TIME).value);
        }

        sellTimeIndex /= box.playerItemPool.Count;

        int finalIndex = (int)System.Math.Round(sellTimeIndex);
        float secondsOfDelivery = GameManager.instance.sellManager.GetSellTimeByIndex(finalIndex);

        if (secondsOfDelivery > maxSeconds)
        {
            if (type.Equals("BOX"))
            {
                box.extraPercentage -= penaltyPercentage;
            } else
            {
                worldItem.value -= Mathf.RoundToInt((float)box.value * penaltyPercentage);
            }
        }
    }

    public override bool MeetsCondition(WorldItem worldItem, Box box, List<string> info)
    {
        string passiveInfo = null;
        foreach (string passive in worldItem.passivesInfo)
        {
            if (passive.StartsWith("DS_REQUIREMENT"))
            {
                passiveInfo = passive;
                break;
            }
        }

        if (passiveInfo == null) return false;

        string[] requiredInfo = passiveInfo.Split(':');

        int maxSeconds = int.Parse(requiredInfo[1]);
        float penaltyPercentage = float.Parse(requiredInfo[2], CultureInfo.InvariantCulture);
        string type = requiredInfo[3];



        // Calculate sell time
        double sellTimeIndex = 0;

        foreach (WorldItem item in box.playerItemPool)
        {
            sellTimeIndex += int.Parse(item.GetAttribute(Attributes.SELL_TIME).value);
        }

        sellTimeIndex /= box.playerItemPool.Count;

        int finalIndex = (int)System.Math.Round(sellTimeIndex);
        float secondsOfDelivery = GameManager.instance.sellManager.GetSellTimeByIndex(finalIndex);

        if (secondsOfDelivery > maxSeconds)
        {
            return true;
        }

        return false;
    }



}
