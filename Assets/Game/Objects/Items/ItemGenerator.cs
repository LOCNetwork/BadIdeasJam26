using System;
using UnityEngine;

[Serializable]
public class ItemGenerator : MonoBehaviour
{

    [SerializeField] GameObject itemGameObject;
    
    public void generateItemGameObject(string ID)
    {
        Item itemData = Resources.Load<Item>("Items/" + ID);

        if (itemData == null)
        {
            Debug.LogError("Item not found in Resources folder: " + ID);
            return;
        }


        SpriteRenderer spriteRenderer = itemData.spriteRenderer;

        itemGameObject.GetComponent<SpriteRenderer>().sprite = spriteRenderer.sprite;

        itemGameObject.AddComponent<WorldItem>();
        itemGameObject.GetComponent<WorldItem>().data = itemData;
    }

}
