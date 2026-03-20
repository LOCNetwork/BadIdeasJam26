using System.Collections.Generic;
using UnityEngine;

public abstract class Passive
{

    public abstract void ExecutePassive(Box box, List<string> info);

    public abstract string Display(WorldItem worldItem, Box box, List<string> info);


    public abstract bool MeetsCondition(Box box, List<string> info);

}
