using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WorldItem
{
    public string itemID;
    public string displayName;
    public string description;
    public int value;
    public Weights weight;
    public ItemRarity rarity;

    public int boxSlots;

    public List<Attribute> attributes;

    public List<Passive> passives;

    public SpriteRenderer spriteRenderer;
    public Sprite displaySprite;

    public void Setup(Item item)
    {
        itemID = item.itemID;
        displayName = item.displayName;
        value = item.value;
        weight = item.weight;
        rarity = item.rarity;

        boxSlots = item.boxSlots;

        attributes = item.attributes;

        passives = item.passives;

        spriteRenderer = item.spriteRenderer;
        displaySprite = item.displaySprite;
    }


    public void Setup(WorldItem item)
    {
        itemID = item.itemID;
        displayName = item.displayName;
        value = item.value;
        weight = item.weight;
        rarity = item.rarity;

        boxSlots = item.boxSlots;

        attributes = item.attributes;

        passives = item.passives;

        spriteRenderer = item.spriteRenderer;
        displaySprite = item.displaySprite;
    }


    public Attribute GetAttribute(Attributes attributeType)
    {
        Attribute attribute = null;

        foreach (Attribute a in attributes)
        {
            if (a.key == attributeType)
            {
                attribute = a;
                break;
            }
        }
        
        return attribute;
    }

    public WorldItem Clone()
    {
        WorldItem clone = new WorldItem();
        clone.Setup(this);

        return clone;
    }

}