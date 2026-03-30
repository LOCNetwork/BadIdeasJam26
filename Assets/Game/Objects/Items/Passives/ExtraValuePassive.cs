using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering.LookDev;

public class ExtraValuePassive : Passive
{

    /*
    * <> -> NEEDED
    * [] -> OPTIONAL
    * 
    * EXTRA VALUE PARSER: 
    * 
    * EXTRA_VALUE:<ADDITIVE/MULTIPLIER>:<VALUE>:[ITEM_ID]
    * 
    * <ADDITIVE/MULTIPLIER> extra for each item sold.
    * 
    */

    public new int priority = 0;

    public override string Display(WorldItem worldItem, Box box, List<string> info)
    {
        int amountOfSells = GameManager.instance.gameStats.itemsSold.TryGetValue(worldItem.itemID, out int sells) ? sells : 0;
        string finalItem = worldItem.itemID;

        if (amountOfSells == 0) return "";

        string passiveInfo = null;
        foreach (string passive in worldItem.passivesInfo)
        {
            if (passive.StartsWith("EXTRA_VALUE"))
            {
                passiveInfo = passive;
                break;
            }
        }

        if (passiveInfo == null) return "";

        string[] requiredInfo = passiveInfo.Split(':');

        string option = requiredInfo[1];
        string value = requiredInfo[2];

        if (requiredInfo.Length == 4)
        {
            string itemTarget = requiredInfo[3];

            amountOfSells = GameManager.instance.gameStats.itemsSold.TryGetValue(itemTarget, out int sells2) ? sells2 : 0;
            finalItem = itemTarget;
        }

        Debug.Log("VALUE GAINED 1: " + value);
        Debug.Log("VALUE GAINED: " + float.Parse(value, CultureInfo.InvariantCulture));
        if (option == "ADDITIVE")
        {
            return $"<color=green>+{float.Parse(value, CultureInfo.InvariantCulture) * amountOfSells} item value</color> from selling [10:{finalItem}:40] x{amountOfSells} times";
        }
        else if (option == "MULTIPLIER")
        {
            return $"<color=green>+{Mathf.RoundToInt(worldItem.value * float.Parse(value, CultureInfo.InvariantCulture) * amountOfSells)} item value</color> [10:{finalItem}:40] x{amountOfSells} times.";
        }

        return "";
    }

    public override void ExecutePassive(WorldItem worldItem, Box box, List<string> info)
    {
        int amountOfSells = GameManager.instance.gameStats.itemsSold.TryGetValue(worldItem.itemID, out int sells) ? sells : 0;

        if (amountOfSells == 0) return;

        string passiveInfo = null;
        foreach (string passive in worldItem.passivesInfo)
        {
            if (passive.StartsWith("EXTRA_VALUE"))
            {
                passiveInfo = passive;
                break;
            }
        }

        if (passiveInfo == null) return;

        string[] requiredInfo = passiveInfo.Split(':');


        string option = requiredInfo[1];
        string value = requiredInfo[2];

        if (requiredInfo.Length == 4)
        {
            string itemTarget = requiredInfo[3];

            amountOfSells = GameManager.instance.gameStats.itemsSold.TryGetValue(itemTarget, out int sells2) ? sells2 : 0;
        }

        if (option == "ADDITIVE")
        {
            worldItem.value += Mathf.RoundToInt(float.Parse(value, CultureInfo.InvariantCulture) * amountOfSells);
        }
        else if (option == "MULTIPLIER")
        {
            worldItem.value += Mathf.RoundToInt(worldItem.value * float.Parse(value, CultureInfo.InvariantCulture) * amountOfSells);
        }


    }

    public override bool MeetsCondition(WorldItem worldItem, Box box, List<string> info)
    {
        return true;
    }
}
