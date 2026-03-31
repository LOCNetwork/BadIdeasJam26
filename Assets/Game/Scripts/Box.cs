using System;
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

[System.Serializable]
public struct RarityDropRates
{
    [Range(0f, 100f)] public float common;
    [Range(0f, 100f)] public float rare;
    [Range(0f, 100f)] public float legendary;

    public float GetWeight(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.COMMON => common,
            ItemRarity.RARE => rare,
            ItemRarity.LEGENDARY => legendary,
            _ => 0f
        };
    }
}


[DisallowMultipleComponent]
public class Box : MonoBehaviour
{
    [Header("Box Config")]
    [SerializeField] private BoxType type = BoxType.Player;
    [SerializeField] private BoxSize size = BoxSize.Small;

    [Header("Capacity By Size")]
    [SerializeField, Min(1)] private int smallCapacity = 3;
    [SerializeField, Min(1)] private int mediumCapacity = 5;
    [SerializeField, Min(1)] private int largeCapacity = 8;

    [Header("Item Weight")]
    public Weights itemsWeight;

    [Header("Item Roll Settings")]
    [SerializeField] private bool allowRepeatedItems = true;

    [Header("Sprites (per size)")]
    [SerializeField] private BoxSpriteSet smallSprites;
    [SerializeField] private BoxSpriteSet mediumSprites;
    [SerializeField] private BoxSpriteSet largeSprites;

    [Header("Item Pool For This Box")]
    [Tooltip("Pool de ScriptableObjects Item que puede soltar ESTA caja.")]
    [SerializeField] private List<Item> itemPool = new List<Item>();
    [Tooltip("Pool de WorldItems que vende ESTA caja.")]
    public List<WorldItem> playerItemPool = new List<WorldItem>();

    [Tooltip("Extra box value")]
    public double extraPercentage = 0.0;
    public int extraValue = 0;

    [Tooltip("Total box value")]
    public int value;

    [Tooltip("Box sell time")]
    public int sellTime;

    private SpriteRenderer sr;

    public BoxType Type => type;
    public BoxSize Size => size;
    public IReadOnlyList<Item> ItemPool => itemPool;
    public bool AllowRepeatedItems => allowRepeatedItems;

    public Guid guid;

    public int Weight => size switch
    {
        BoxSize.Small => 1,
        BoxSize.Medium => 2,
        BoxSize.Large => 3,
        _ => 1
    };

    public int Capacity => size switch
    {
        BoxSize.Small => smallCapacity,
        BoxSize.Medium => mediumCapacity,
        BoxSize.Large => largeCapacity,
        _ => smallCapacity
    };

    public int GetCapacityBySize(BoxSize size)
    {
        switch (size)
        {
            case BoxSize.Small:
                return smallCapacity;
            case BoxSize.Medium:
                return mediumCapacity;
            case BoxSize.Large:
                return largeCapacity;
        }

        return -1;
    }

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

    public List<Item> RollContents(RarityDropRates rarityRates)
    {
        List<Item> result = new List<Item>();
        double remainingSlots = Capacity;
        int safety = 100;

        List<Item> workingPool = new List<Item>(itemPool);

        while (remainingSlots > 0 && safety-- > 0)
        {
            List<Item> validCandidates = new List<Item>();

            for (int i = 0; i < workingPool.Count; i++)
            {
                Item item = workingPool[i];
                if (item == null) continue;
                if (item.boxSlots <= remainingSlots && rarityRates.GetWeight(item.rarity) > 0f)
                    validCandidates.Add(item);
            }

            if (validCandidates.Count == 0)
                break;

            Item selected = GetWeightedRandomItem(validCandidates, rarityRates);
            if (selected == null)
                break;

            result.Add(selected);
            remainingSlots -= Mathf.Max(0.5f, selected.boxSlots);

            if (!allowRepeatedItems)
            {
                workingPool.Remove(selected);
            }
        }

        return result;
    }

    private Item GetWeightedRandomItem(List<Item> candidates, RarityDropRates rarityRates)
    {
        float totalWeight = 0f;

        for (int i = 0; i < candidates.Count; i++)
            totalWeight += rarityRates.GetWeight(candidates[i].rarity);

        if (totalWeight <= 0f)
            return null;

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float accumulated = 0f;

        for (int i = 0; i < candidates.Count; i++)
        {
            accumulated += rarityRates.GetWeight(candidates[i].rarity);
            if (roll <= accumulated)
                return candidates[i];
        }

        return candidates[candidates.Count - 1];
    }



    public Box Clone(GameObject host, bool copyGuid = true)
    {
        Box clone;

        if (host == null)
        {
            GameObject go = new GameObject($"{gameObject.name}_clone");
            go.transform.SetParent(transform.parent);
            go.transform.position = transform.position;
            go.transform.rotation = transform.rotation;
            go.transform.localScale = transform.localScale;

            clone = go.AddComponent<Box>();
        } else
        {
            clone = host.AddComponent<Box>();
        }

        // copy value/primitive fields and references (we are inside the class so private fields accessible)
        clone.type = this.type;
        clone.size = this.size;

        clone.smallCapacity = this.smallCapacity;
        clone.mediumCapacity = this.mediumCapacity;
        clone.largeCapacity = this.largeCapacity;

        clone.allowRepeatedItems = this.allowRepeatedItems;

        // shallow-copy references to sprite sets (do not create or modify SpriteRenderer)
        clone.smallSprites = this.smallSprites;
        clone.mediumSprites = this.mediumSprites;
        clone.largeSprites = this.largeSprites;

        // shallow-copy lists (new lists but same element references)
        clone.itemPool = new List<Item>(this.itemPool);
        clone.playerItemPool = new List<WorldItem>(this.playerItemPool);

        // copy value fields
        clone.extraPercentage = this.extraPercentage;
        clone.extraValue = this.extraValue;
        clone.value = this.value;
        clone.sellTime = this.sellTime;

        // guid handling
        clone.guid = copyGuid ? this.guid : Guid.NewGuid();

        return clone;
    }
}