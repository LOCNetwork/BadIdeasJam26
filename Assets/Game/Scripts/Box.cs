using System.Collections.Generic;
using UnityEngine;

public enum BoxType
{
    Player,
    Delivery
}

public enum BoxSize
{
    Small,
    Medium,
    Large
}

[System.Serializable]
public struct BoxSpriteSet
{
    public Sprite front;
    public Sprite side;
}

[DisallowMultipleComponent]
public class Box : MonoBehaviour
{
    [Header("Box Config")]
    [SerializeField] private BoxType type = BoxType.Player;
    [SerializeField] private BoxSize size = BoxSize.Small;

    [Header("Sprites (per size)")]
    [SerializeField] private BoxSpriteSet smallSprites;
    [SerializeField] private BoxSpriteSet mediumSprites;
    [SerializeField] private BoxSpriteSet largeSprites;

    [Header("Item Pool (prepared)")]
    [Tooltip("Pool de items que puede contener esta caja (para futuras implementaciones).")]
    [SerializeField] private List<ItemDefinition> itemPool = new List<ItemDefinition>();

    private SpriteRenderer sr;

    public BoxType Type => type;
    public BoxSize Size => size;

    public int Weight => size switch
    {
        BoxSize.Small => 1,
        BoxSize.Medium => 2,
        BoxSize.Large => 3,
        _ => 1
    };

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            Debug.LogWarning($"{name}: Box necesita SpriteRenderer si quieres usar sprites Front/Side.");
    }

    public void ApplyStackSprite(int stackIndex)
    {
        if (sr == null) return;

        bool useFront = (stackIndex % 2 == 0);

        BoxSpriteSet set = size switch
        {
            BoxSize.Small => smallSprites,
            BoxSize.Medium => mediumSprites,
            BoxSize.Large => largeSprites,
            _ => smallSprites
        };

        Sprite chosen = useFront ? set.front : set.side;

        if (chosen != null)
            sr.sprite = chosen;
    }

    // Preparado para futuras implementaciones:
    // public IReadOnlyList<ItemDefinition> ItemPool => itemPool;
}

/// <summary>
/// PREPARADO para el sistema de items.
/// (En el futuro lo normal sería crear un ScriptableObject real "ItemDefinition").
/// </summary>
public class ItemDefinition : ScriptableObject
{
    // public string displayName;
    // public int value;
    // public Rarity rarity;
    // public List<PassiveDefinition> passives;
    // public Dictionary<string, int> attributes;
}