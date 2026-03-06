using System.Collections;
using System.Collections.Generic;
using System.Transactions;
using UnityEngine;


[CreateAssetMenu(fileName = "Objects", menuName = "Item")]
public class Item : ScriptableObject
{

    [Header("Information")]
    public string itemID;
    public string displayName;
    public int value;
    public Weights weight;
    public ItemRarity rarity;

    [Header("Attributes")]
    public List<Attribute> attributes;

    [Header("Passives")]
    public List<Passive> passives;

    [Header("Display")]
    public SpriteRenderer spriteRenderer;







}
