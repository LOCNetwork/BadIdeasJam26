using System;
using UnityEngine;

[Serializable]
public class WorldItem : MonoBehaviour
{

    public Item data;

    public void Setup(Item item)
    {
        data = item;
    }

}
