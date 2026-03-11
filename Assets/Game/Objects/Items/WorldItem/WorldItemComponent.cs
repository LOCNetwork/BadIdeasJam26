using Unity.VisualScripting;
using UnityEngine;

public class WorldItemComponent : MonoBehaviour
{

    [SerializeField] private WorldItem data;

    public WorldItem Data => data;


    public void Init(WorldItem itemData)
    {
        data = itemData != null ? itemData.Clone() : null;
    }


}
