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
        this.itemID = item.itemID;
        this.displayName = item.displayName;
        this.value = item.value;
        this.weight = item.weight;
        this.rarity = item.rarity;

        this.boxSlots = item.boxSlots;

        this.attributes = item.attributes;

        this.passives = item.passives;

        this.spriteRenderer = item.spriteRenderer;
        this.displaySprite = item.displaySprite;
    }


    public void Setup(WorldItem item)
    {
        this.itemID = item.itemID;
        this.displayName = item.displayName;
        this.value = item.value;
        this.weight = item.weight;
        this.rarity = item.rarity;

        this.boxSlots = item.boxSlots;

        this.attributes = item.attributes;

        this.passives = item.passives;

        this.spriteRenderer = item.spriteRenderer;
        this.displaySprite = item.displaySprite;
    }

    public WorldItem Clone()
    {
        WorldItem clone = new WorldItem();
        clone.Setup(this);

        return clone;
    }

}