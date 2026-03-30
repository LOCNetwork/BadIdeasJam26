using System.Collections.Generic;
using UnityEngine;

public class RandomValuePassive : Passive
{

    /*
    * <> -> NEEDED
    * [] -> OPTIONAL
    * 
    * RANDOM VALUE PARSER: 
    * 
    * RANDOM_VALUE:<MIN_VALUE>-<MAX_VALUE>
    * 
    */

    public new int priority = 0;

    public override string Display(WorldItem worldItem, Box box, List<string> info)
    {

       return $"The value of [10:{worldItem.itemID}:40] was set to <color=green>{worldItem.value}</color>!";
    }

    public override void ExecutePassive(WorldItem worldItem, Box box, List<string> info)
    {
        string passiveInfo = null;
        foreach (string passive in worldItem.passivesInfo)
        {
            if (passive.StartsWith("RANDOM_VALUE"))
            {
                passiveInfo = passive;
                break;
            }
        }

        if (passiveInfo == null) return;

        string[] requiredInfo = passiveInfo.Split(':');

        string range = requiredInfo[1];
        
        int min = int.Parse(range.Split('-')[0]);
        int max = int.Parse(range.Split('-')[1]);

        int selection = Random.Range(min, max);
        int selection2 = Random.Range(min, max);

        Debug.Log(selection * selection2);

        worldItem.value = selection * selection2;
    }

    public override bool MeetsCondition(WorldItem worldItem, Box box, List<string> info)
    {
        return true;
    }
}
