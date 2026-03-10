using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Objects", menuName = "Item")]
public class Item : ScriptableObject
{
    [Header("Information")]
    public string itemID;
    public string displayName;
    public string description;
    public int value;
    public Weights weight;
    public ItemRarity rarity;

    [Header("Box / Unwrapper")]
    [Min(1)]
    public int boxSlots = 1;

    [Header("Attributes")]
    public List<Attribute> attributes;

    [Header("Passives")]
    public List<Passive> passives;

    [Header("Display")]
    public SpriteRenderer spriteRenderer;
    public Sprite displaySprite;
}