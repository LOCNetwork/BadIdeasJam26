using System.Collections;
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
    private enum DisplayKind
    {
        None,
        Item,
        PlayerBox
    }

    [Header("References")]
    [SerializeField] private Player player;
    [SerializeField] private GameObject uiRoot;

    [Header("Juice Root")]
    [SerializeField] private RectTransform uiAnimatedRoot;

    [Header("Common UI")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text slotsText;

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

    [Header("Player Box Display Sprites")]
    [SerializeField] private Sprite smallBoxDisplaySprite;
    [SerializeField] private Sprite mediumBoxDisplaySprite;
    [SerializeField] private Sprite largeBoxDisplaySprite;

    [Header("Rarity Colors")]
    [SerializeField]
    private List<RarityColorEntry> rarityColors = new List<RarityColorEntry>()
    {
        new RarityColorEntry { rarity = ItemRarity.COMMON, color = Color.white },
        new RarityColorEntry { rarity = ItemRarity.RARE, color = Color.cyan },
        new RarityColorEntry { rarity = ItemRarity.LEGENDARY, color = Color.yellow }
    };

    [Header("Idle Open Juice")]
    [SerializeField] private float swayXAmplitude = 6f;
    [SerializeField] private float swayXFrequency = 1.2f;
    [SerializeField] private float swayYAmplitude = 4f;
    [SerializeField] private float swayYFrequency = 1.8f;

    [Header("Open / Change Juice")]
    [SerializeField] private float squashDuration = 0.12f;
    [SerializeField] private Vector3 squashStartScale = new Vector3(1.14f, 0.86f, 1f);
    [SerializeField] private Vector3 squashOvershootScale = new Vector3(0.96f, 1.06f, 1f);
    [SerializeField] private float settleDuration = 0.10f;
    [SerializeField] private float textFadeDuration = 0.12f;

    private DisplayKind currentKind = DisplayKind.None;
    private string currentSignature = string.Empty;

    private Vector2 baseAnchoredPosition;
    private Coroutine changeRoutine;

    private readonly List<TMP_Text> allTexts = new List<TMP_Text>();

    private void Awake()
    {
        if (player == null)
            player = FindFirstObjectByType<Player>();

        if (uiAnimatedRoot == null && uiRoot != null)
            uiAnimatedRoot = uiRoot.GetComponent<RectTransform>();

        if (uiAnimatedRoot != null)
            baseAnchoredPosition = uiAnimatedRoot.anchoredPosition;

        CacheTexts();
    }

    private void OnEnable()
    {
        if (uiAnimatedRoot != null)
            baseAnchoredPosition = uiAnimatedRoot.anchoredPosition;

        CacheTexts();
    }

    private void Update()
    {
        RefreshUI();
        UpdateIdleJuice();
    }

    private void CacheTexts()
    {
        allTexts.Clear();

        AddTextIfNotNull(nameText);
        AddTextIfNotNull(slotsText);

        AddTextIfNotNull(descriptionText);
        AddTextIfNotNull(attributesText);
        AddTextIfNotNull(passivesNamesText);
        AddTextIfNotNull(passivesDescriptionsText);
        AddTextIfNotNull(rarityText);
        AddTextIfNotNull(valueText);
        AddTextIfNotNull(weightText);

        AddTextIfNotNull(sellTimeText);
        AddTextIfNotNull(playerBoxItemsText);
    }

    private void AddTextIfNotNull(TMP_Text text)
    {
        if (text != null && !allTexts.Contains(text))
            allTexts.Add(text);
    }

    private void RefreshUI()
    {
        if (player == null || uiRoot == null)
        {
            SetUIActive(false);
            SetCurrentDisplay(DisplayKind.None, string.Empty);
            return;
        }

        if (player.ReturnHeldItem(out _, out WorldItem heldItem) && heldItem != null)
        {
            SetUIActive(true);
            ShowItemInfo(heldItem);
            SetCurrentDisplay(DisplayKind.Item, BuildItemSignature(heldItem));
            return;
        }

        if (player.ReturnHeldBox(out _, out Box heldBox) && heldBox != null)
        {
            if (heldBox.Type == BoxType.Player)
            {
                SetUIActive(true);
                ShowPlayerBoxInfo(heldBox);
                SetCurrentDisplay(DisplayKind.PlayerBox, BuildBoxSignature(heldBox));
                return;
            }

            SetUIActive(false);
            SetCurrentDisplay(DisplayKind.None, string.Empty);
            return;
        }

        SetUIActive(false);
        SetCurrentDisplay(DisplayKind.None, string.Empty);
    }

    private void SetCurrentDisplay(DisplayKind newKind, string newSignature)
    {
        bool changed = currentKind != newKind || currentSignature != newSignature;
        currentKind = newKind;
        currentSignature = newSignature;

        if (!changed || newKind == DisplayKind.None)
            return;

        PlayChangeJuice();
    }

    private void SetUIActive(bool active)
    {
        bool wasActive = uiRoot.activeSelf;

        if (wasActive != active)
            uiRoot.SetActive(active);

        if (!active)
        {
            if (changeRoutine != null)
            {
                StopCoroutine(changeRoutine);
                changeRoutine = null;
            }

            if (uiAnimatedRoot != null)
            {
                uiAnimatedRoot.localScale = Vector3.one;
                uiAnimatedRoot.anchoredPosition = baseAnchoredPosition;
            }

            SetAllContentAlpha(1f);
        }
        else if (!wasActive)
        {
            if (uiAnimatedRoot != null)
                baseAnchoredPosition = uiAnimatedRoot.anchoredPosition;

            PlayChangeJuice();
        }
    }

    private void UpdateIdleJuice()
    {
        if (uiRoot == null || !uiRoot.activeSelf || uiAnimatedRoot == null)
            return;

        float x = Mathf.Sin(Time.unscaledTime * swayXFrequency * Mathf.PI * 2f) * swayXAmplitude;
        float y = Mathf.Sin(Time.unscaledTime * swayYFrequency * Mathf.PI * 2f) * swayYAmplitude;

        uiAnimatedRoot.anchoredPosition = baseAnchoredPosition + new Vector2(x, y);
    }

    private void PlayChangeJuice()
    {
        if (uiRoot == null || !uiRoot.activeSelf || uiAnimatedRoot == null)
            return;

        if (changeRoutine != null)
            StopCoroutine(changeRoutine);

        changeRoutine = StartCoroutine(ChangeJuiceRoutine());
    }

    private IEnumerator ChangeJuiceRoutine()
    {
        SetAllContentAlpha(0f);
        uiAnimatedRoot.localScale = squashStartScale;

        float t = 0f;
        while (t < squashDuration)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(t / squashDuration);
            uiAnimatedRoot.localScale = Vector3.LerpUnclamped(squashStartScale, squashOvershootScale, EaseOutBack(n));
            yield return null;
        }

        t = 0f;
        while (t < settleDuration)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(t / settleDuration);
            uiAnimatedRoot.localScale = Vector3.LerpUnclamped(squashOvershootScale, Vector3.one, n);
            yield return null;
        }

        uiAnimatedRoot.localScale = Vector3.one;

        t = 0f;
        while (t < textFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(t / textFadeDuration);
            SetAllContentAlpha(n);
            yield return null;
        }

        SetAllContentAlpha(1f);
        changeRoutine = null;
    }

    private void SetAllContentAlpha(float alpha)
    {
        for (int i = 0; i < allTexts.Count; i++)
        {
            if (allTexts[i] == null)
                continue;

            Color c = allTexts[i].color;
            c.a = alpha;
            allTexts[i].color = c;
        }

        if (iconImage != null)
        {
            Color c = iconImage.color;
            c.a = alpha;
            iconImage.color = c;
        }
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

        if (slotsText != null)
            slotsText.text = $"Slots: {item.boxSlots}";

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
            sellTimeText.text = $"Sell Time: {FormatSellTime(box.sellTimeIndex)}";

        if (playerBoxItemsText != null)
            playerBoxItemsText.text = $"Items in player box: {FormatPlayerBoxItems(box.playerItemPool)}";

        if (slotsText != null)
            slotsText.text = $"Slots: {GetUsedSlots(box.playerItemPool)}/{box.Capacity}";

        if (iconImage != null)
        {
            iconImage.sprite = GetPlayerBoxDisplaySprite(box.Size);
            iconImage.enabled = iconImage.sprite != null;
        }
    }

    private string FormatSellTime(int sellTimeIndex)
    {
        switch (sellTimeIndex)
        {
            case 3: return "Very Fast";
            case 2: return "Fast";
            case 1: return "Slow";
            case 0: return "Very Slow";
            default: return sellTimeIndex.ToString();
        }
    }

    private Sprite GetPlayerBoxDisplaySprite(BoxSize size)
    {
        switch (size)
        {
            case BoxSize.Small:
                return smallBoxDisplaySprite;
            case BoxSize.Medium:
                return mediumBoxDisplaySprite;
            case BoxSize.Large:
                return largeBoxDisplaySprite;
            default:
                return null;
        }
    }

    private int GetUsedSlots(List<WorldItem> items)
    {
        if (items == null || items.Count == 0)
            return 0;

        int total = 0;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null)
                continue;

            total += items[i].boxSlots;
        }

        return total;
    }

    private string BuildItemSignature(WorldItem item)
    {
        if (item == null)
            return string.Empty;

        return $"ITEM|{item.itemID}|{item.displayName}|{item.value}|{item.weight}|{item.rarity}|{item.boxSlots}";
    }

    private string BuildBoxSignature(Box box)
    {
        if (box == null)
            return string.Empty;

        int count = box.playerItemPool != null ? box.playerItemPool.Count : 0;
        int usedSlots = GetUsedSlots(box.playerItemPool);

        return $"BOX|{box.GetInstanceID()}|{box.Size}|{box.sellTimeIndex}|{count}|{usedSlots}";
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

        string keyText = key != null ? key.ToString().Replace("_", " ") : string.Empty;
        string valueTextLocal = value != null ? value.ToString().Replace("_", " ") : string.Empty;
        string amountText = amount != null ? amount.ToString().Replace("_", " ") : string.Empty;

        bool isSellTime =
            key != null &&
            string.Equals(key.ToString(), "SELL_TIME", System.StringComparison.OrdinalIgnoreCase);

        if (isSellTime)
        {
            if (int.TryParse(valueTextLocal, out int sellTimeValueFromValue))
                valueTextLocal = FormatSellTime(sellTimeValueFromValue);

            if (int.TryParse(amountText, out int sellTimeValueFromAmount))
                amountText = FormatSellTime(sellTimeValueFromAmount);
        }

        if (!string.IsNullOrEmpty(keyText) && !string.IsNullOrEmpty(valueTextLocal))
            return $"{keyText}: {valueTextLocal}";

        if (!string.IsNullOrEmpty(keyText) && !string.IsNullOrEmpty(amountText))
            return $"{keyText}: {amountText}";

        if (!string.IsNullOrEmpty(keyText))
            return keyText;

        return attribute.ToString().Replace("_", " ");
    }

    private string FormatPassives(List<Passive> passives)
    {
        if (passives == null || passives.Count == 0)
            return string.Empty;

        List<string> names = new List<string>();

        for (int i = 0; i < passives.Count; i++)
            names.Add(passives[i].ToString().Replace("_", " "));

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

    private float EaseOutBack(float x)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    private string Safe(string value)
    {
        return string.IsNullOrEmpty(value) ? string.Empty : value;
    }
}