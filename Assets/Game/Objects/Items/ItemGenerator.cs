using System;
using UnityEngine;

[Serializable]
public class ItemGenerator : MonoBehaviour
{
    [SerializeField] private GameObject itemGameObject;

    private void Awake()
    {
        if (itemGameObject == null)
            itemGameObject = gameObject;
    }

    public void SetTarget(GameObject target)
    {
        itemGameObject = target;
    }

    public void generateItemGameObject(string ID)
    {
        if (itemGameObject == null)
            itemGameObject = gameObject;

        Item itemData = GameManager.instance.GetItemByID(ID);

        if (itemData == null)
        {
            Debug.LogError("Item not found in Resources folder: " + ID);
            return;
        }

        SpriteRenderer targetSR = itemGameObject.GetComponent<SpriteRenderer>();
        if (targetSR == null)
            targetSR = itemGameObject.AddComponent<SpriteRenderer>();

        Sprite finalSprite = null;

        if (itemData.displaySprite != null)
        {
            finalSprite = itemData.displaySprite;
        }
        else if (itemData.spriteRenderer != null)
        {
            finalSprite = itemData.spriteRenderer.sprite;
        }

        if (finalSprite != null)
        {
            targetSR.sprite = finalSprite;
        }
        else
        {
            Debug.LogWarning($"El item {ID} no tiene ni displaySprite ni spriteRenderer con sprite asignado.");
        }

        WorldItemComponent worldItem = itemGameObject.GetComponent<WorldItemComponent>();
        if (worldItem == null)
            worldItem = itemGameObject.AddComponent<WorldItemComponent>();

        worldItem.Data.Setup(itemData);
    }
}