using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct RarityColorEntry
{
    public ItemRarity rarity;
    public Color color;
}

public class HeldInfoUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Player player;
    [SerializeField] private GameObject uiRoot;

    [Header("Common UI")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image iconImage;

    [Header("Item UI")]
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text attributesText;
    [SerializeField] private TMP_Text passivesNamesText;
    [SerializeField] private TMP_Text passivesDescriptionsText;
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private TMP_Text weightText;

    [Header("Player Box UI")]
    [SerializeField] private TMP_Text sellTimeText;
    [SerializeField] private TMP_Text playerBoxItemsText;

    [Header("Rarity Colors")]
    [SerializeField]
    private List<RarityColorEntry> rarityColors = new List<RarityColorEntry>()
    {
        new RarityColorEntry { rarity = ItemRarity.COMMON, color = Color.white },
        new RarityColorEntry { rarity = ItemRarity.RARE, color = Color.cyan },
        new RarityColorEntry { rarity = ItemRarity.LEGENDARY, color = Color.yellow }
    };

    private void Awake()
    {
        if (player == null)
            player = FindFirstObjectByType<Player>();
    }

    private void Update()
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (player == null || uiRoot == null)
        {
            SetUIActive(false);
            return;
        }

        if (player.ReturnHeldItem(out _, out WorldItem heldItem) && heldItem != null)
        {
            SetUIActive(true);
            ShowItemInfo(heldItem);
            return;
        }

        if (player.ReturnHeldBox(out _, out Box heldBox) && heldBox != null)
        {
            if (heldBox.Type == BoxType.Player)
            {
                SetUIActive(true);
                ShowPlayerBoxInfo(heldBox);
                return;
            }

            SetUIActive(false);
            return;
        }

        SetUIActive(false);
    }

    private void SetUIActive(bool active)
    {
        if (uiRoot.activeSelf != active)
            uiRoot.SetActive(active);
    }

    private void ShowItemInfo(WorldItem item)
    {
        if (nameText != null)
            nameText.text = Safe(item.displayName);

        if (descriptionText != null)
            descriptionText.text = Safe(item.description);

        if (attributesText != null)
            attributesText.text = FormatAttributes(item.attributes);

        if (passivesNamesText != null)
            passivesNamesText.text = FormatPassives(item.passives);

        if (passivesDescriptionsText != null)
            passivesDescriptionsText.text = FormatPassiveDescriptions(item.passivesDescriptions);

        if (rarityText != null)
        {
            rarityText.text = item.rarity.ToString();
            rarityText.color = GetRarityColor(item.rarity);
        }

        if (valueText != null)
            valueText.text = $"Value: {item.value}$";

        if (weightText != null)
            weightText.text = $"Weight: {item.weight}";

        if (sellTimeText != null)
            sellTimeText.text = string.Empty;

        if (playerBoxItemsText != null)
            playerBoxItemsText.text = string.Empty;

        if (iconImage != null)
        {
            Sprite sprite = GetWorldItemSprite(item);
            iconImage.sprite = sprite;
            iconImage.enabled = sprite != null;
        }
    }

    private void ShowPlayerBoxInfo(Box box)
    {
        if (nameText != null)
            nameText.text = $"{box.Size} Box Player";

        if (descriptionText != null)
            descriptionText.text = string.Empty;

        if (attributesText != null)
            attributesText.text = string.Empty;

        if (passivesNamesText != null)
            passivesNamesText.text = string.Empty;

        if (passivesDescriptionsText != null)
            passivesDescriptionsText.text = string.Empty;

        if (rarityText != null)
            rarityText.text = string.Empty;

        if (valueText != null)
            valueText.text = string.Empty;

        if (weightText != null)
            weightText.text = string.Empty;

        if (sellTimeText != null)
            sellTimeText.text = $"Sell Time: {box.sellTimeIndex}";

        if (playerBoxItemsText != null)
            playerBoxItemsText.text = $"Items in player box: {FormatPlayerBoxItems(box.playerItemPool)}";

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }
    }

    private string FormatAttributes(List<Attribute> attributes)
    {
        if (attributes == null || attributes.Count == 0)
            return string.Empty;

        List<string> parts = new List<string>();

        for (int i = 0; i < attributes.Count; i++)
        {
            if (attributes[i] == null)
                continue;

            parts.Add(FormatAttributeEntry(attributes[i]));
        }

        return string.Join(", ", parts);
    }

    private string FormatAttributeEntry(Attribute attribute)
    {
        if (attribute == null)
            return string.Empty;

        System.Type t = attribute.GetType();

        FieldInfo keyField = t.GetField("key", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo valueField = t.GetField("value", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo amountField = t.GetField("amount", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        object key = keyField != null ? keyField.GetValue(attribute) : null;
        object value = valueField != null ? valueField.GetValue(attribute) : null;
        object amount = amountField != null ? amountField.GetValue(attribute) : null;

        if (key != null && value != null)
            return $"{key}: {value}";

        if (key != null && amount != null)
            return $"{key}: {amount}";

        if (key != null)
            return key.ToString();

        return attribute.ToString();
    }

    private string FormatPassives(List<Passive> passives)
    {
        if (passives == null || passives.Count == 0)
            return string.Empty;

        List<string> names = new List<string>();

        for (int i = 0; i < passives.Count; i++)
        {
            names.Add(passives[i].ToString());
        }

        return string.Join(", ", names);
    }

    private string FormatPassiveDescriptions(List<string> descriptions)
    {
        if (descriptions == null || descriptions.Count == 0)
            return string.Empty;

        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < descriptions.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(descriptions[i]))
                continue;

            if (sb.Length > 0)
                sb.Append('\n');

            sb.Append(descriptions[i]);
        }

        return sb.ToString();
    }

    private string FormatPlayerBoxItems(List<WorldItem> items)
    {
        if (items == null || items.Count == 0)
            return string.Empty;

        List<string> names = new List<string>();

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null)
                continue;

            names.Add(Safe(items[i].displayName));
        }

        return string.Join(", ", names);
    }

    private Sprite GetWorldItemSprite(WorldItem item)
    {
        if (item == null)
            return null;

        if (item.displaySprite != null)
            return item.displaySprite;

        if (item.spriteRenderer != null)
            return item.spriteRenderer.sprite;

        return null;
    }

    private Color GetRarityColor(ItemRarity rarity)
    {
        for (int i = 0; i < rarityColors.Count; i++)
        {
            if (rarityColors[i].rarity == rarity)
                return rarityColors[i].color;
        }

        return Color.white;
    }

    private string Safe(string value)
    {
        return string.IsNullOrEmpty(value) ? string.Empty : value;
    }
}