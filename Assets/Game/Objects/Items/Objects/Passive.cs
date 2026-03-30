using System.Collections.Generic;
using UnityEngine;

public abstract class Passive
{

    public int priority = 0; // For ordering passives, higher priority executes first

    public abstract void ExecutePassive(WorldItem worldItem,Box box, List<string> info);

    public abstract string Display(WorldItem worldItem, Box box, List<string> info);


    public abstract bool MeetsCondition(WorldItem worldItem, Box box, List<string> info);

}
