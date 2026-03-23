using NUnit.Framework.Internal.Execution;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using static Unity.Burst.Intrinsics.X86.Avx;

[Serializable]
public class Wrapper : Interactable
{
    [Header("Validation")]
    [SerializeField] private bool requireWrapperTag = true;

    [Header("Drops")]
    [SerializeField] private RarityDropRates rarityRates;
    [SerializeField] private GameObject playerSmallBoxPrefab;
    [SerializeField] private GameObject playerMediumBoxPrefab;
    [SerializeField] private GameObject playerLargeBoxPrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string wrapSmallTrigger = "WrapS";
    [SerializeField] private string wrapMediumTrigger = "WrapM";
    [SerializeField] private string wrapLargeTrigger = "WrapL";
    [SerializeField] private float wrapSmallDuration = 0.5f;
    [SerializeField] private float wrapMediumDuration = 0.5f;
    [SerializeField] private float wrapLargeDuration = 0.5f;

    [Header("Extraction Cooldown")]
    [SerializeField] private float extractCooldown = 0.4f;

    [Header("Slots UI")]
    [SerializeField] private TMP_Text slotsText;
    [SerializeField] private Color normalSlotsColor = Color.white;
    [SerializeField] private Color errorSlotsColor = Color.red;

    [Header("Slots UI Juice - Success")]
    [SerializeField] private float successSquashDuration = 0.08f;
    [SerializeField] private float successSettleDuration = 0.10f;
    [SerializeField] private Vector3 successStartScale = new Vector3(1.15f, 0.85f, 1f);
    [SerializeField] private Vector3 successOvershootScale = new Vector3(0.95f, 1.08f, 1f);
    [SerializeField] private float successShakeDuration = 0.10f;
    [SerializeField] private float successShakeStrength = 4f;

    [Header("Slots UI Juice - Fail")]
    [SerializeField] private float failShakeDuration = 0.18f;
    [SerializeField] private float failShakeStrength = 12f;
    [SerializeField] private float failColorDuration = 0.20f;

    public BoxSize AVAILABLE_BOX_SIZE = BoxSize.Medium;

    private GameObject currentBox;
    private bool isBusy = false;

    private RectTransform slotsRect;
    private Vector3 slotsBaseScale = Vector3.one;
    private Vector2 slotsBaseAnchoredPos = Vector2.zero;
    private Coroutine slotsJuiceRoutine;
    private Coroutine slotsErrorRoutine;

    private void Awake()
    {
        if (slotsText != null)
        {
            slotsRect = slotsText.GetComponent<RectTransform>();
            if (slotsRect != null)
                slotsBaseAnchoredPos = slotsRect.anchoredPosition;

            slotsBaseScale = slotsText.transform.localScale;
        }
    }

    private void Start()
    {
        RefreshSlotsText();
    }

    private void OnEnable()
    {
        RefreshSlotsText();
    }

    public override void Interact(Player player)
    {
        if (isBusy)
            return;

        if (player == null)
            return;

        if (requireWrapperTag && !CompareTag("Wrapper"))
        {
            Debug.LogWarning($"{name}: Este objeto no tiene tag 'Wrapper'.");
            return;
        }

        if (!player.ReturnHeldItem(out GameObject item, out WorldItem itemData))
        {
            if (currentBox == null || currentBox.GetComponent<Box>().playerItemPool.Count == 0)
            {
                Debug.Log("El Wrapper no tiene items insertados.");
                return;
            }

            StartCoroutine(SpawnBoxRoutine(AVAILABLE_BOX_SIZE, true));
            return;
        }

        StartCoroutine(WrapRoutine(player, itemData.boxSlots));
    }

    private IEnumerator WrapRoutine(Player player, int boxSlots)
    {
        isBusy = true;

        if (currentBox == null)
        {
            SpawnBox(AVAILABLE_BOX_SIZE);
        }

        Box box = currentBox.GetComponent<Box>();

        int currentCapacityFilled = GetItemCapacity();
        int boxCapacity = box.GetCapacityBySize(AVAILABLE_BOX_SIZE);

        if (currentCapacityFilled + boxSlots <= boxCapacity)
        {
            player.TryTakeTopHeldItem(out GameObject consumedItem, out WorldItem itemData);

            WorldItem worldItemCopy = new WorldItem();
            worldItemCopy.Setup(itemData);

            if (consumedItem != null)
                Destroy(consumedItem);

            currentBox.GetComponent<Box>().playerItemPool.Add(worldItemCopy);

            ModifyItemsInWrapper();

            RefreshSlotsText();
            PlaySuccessSlotsJuice();

            if (currentCapacityFilled + boxSlots == boxCapacity)
            {
                yield return StartCoroutine(SpawnBoxRoutine(AVAILABLE_BOX_SIZE, true));
                yield break;
            }

            Debug.Log($"Estadisticas Wrapper --> ESPACIO RELLENO: {currentCapacityFilled + boxSlots}, TOTAL SLOTS CAJA: {boxCapacity}, SOBRANTE: {boxCapacity - (currentCapacityFilled + boxSlots)}");
        }
        else
        {
            Debug.Log($"La caja está llena.");
            PlayFailSlotsJuice();
        }

        isBusy = false;
    }

    private IEnumerator SpawnBoxRoutine(BoxSize boxSize, bool applyCooldownAfterSpawn)
    {
        if (currentBox == null)
            yield break;

        isBusy = true;

        TriggerWrapAnimation(boxSize);

        float waitTime = GetWrapDuration(boxSize);
        if (waitTime > 0f)
            yield return new WaitForSeconds(waitTime);

        SpawnBox(boxSize);
        RefreshSlotsText();

        if (applyCooldownAfterSpawn && extractCooldown > 0f)
            yield return new WaitForSeconds(extractCooldown);

        isBusy = false;
    }

    private void TriggerWrapAnimation(BoxSize boxSize)
    {
        if (animator == null)
            return;

        switch (boxSize)
        {
            case BoxSize.Small:
                if (!string.IsNullOrEmpty(wrapSmallTrigger))
                    animator.SetTrigger(wrapSmallTrigger);
                break;

            case BoxSize.Medium:
                if (!string.IsNullOrEmpty(wrapMediumTrigger))
                    animator.SetTrigger(wrapMediumTrigger);
                break;

            case BoxSize.Large:
                if (!string.IsNullOrEmpty(wrapLargeTrigger))
                    animator.SetTrigger(wrapLargeTrigger);
                break;
        }
    }

    private float GetWrapDuration(BoxSize boxSize)
    {
        switch (boxSize)
        {
            case BoxSize.Small:
                return wrapSmallDuration;
            case BoxSize.Medium:
                return wrapMediumDuration;
            case BoxSize.Large:
                return wrapLargeDuration;
        }

        return 0f;
    }

    // This method spawns a box of the unlocked size. As a box is needed in order to apply passives, the box is spawned as inactive the first time, and then set active in the next call. This way we can apply passives and calculate price before the box appears in the world.
    private void SpawnBox(BoxSize boxSize)
    {
        if (currentBox != null)
        {
            Box boxData = currentBox.GetComponent<Box>();

            CalculatePrice();
            CalculateSellTime();

            Debug.Log("Caja spawneada");

            ChooseBoxTag();

            boxData.guid = Guid.NewGuid(); // Unique identifier for the box, used to track it in the sell system

            currentBox.SetActive(true);

            currentBox = null;
            return;
        }

        GameObject box = null;

        switch (boxSize)
        {
            case BoxSize.Small:
                box = Instantiate(playerSmallBoxPrefab, spawnPoint.position, Quaternion.identity);
                break;
            case BoxSize.Medium:
                box = Instantiate(playerMediumBoxPrefab, spawnPoint.position, Quaternion.identity);
                break;
            case BoxSize.Large:
                box = Instantiate(playerLargeBoxPrefab, spawnPoint.position, Quaternion.identity);
                break;
        }

        box.SetActive(false);

        currentBox = box;
    }

    private int GetItemCapacity()
    {
        if (currentBox == null)
            return 0;

        int capacity = 0;

        foreach (var item in currentBox.GetComponent<Box>().playerItemPool)
        {
            capacity += item.boxSlots;
        }

        return capacity;
    }

    private int GetMaxCapacity()
    {
        switch (AVAILABLE_BOX_SIZE)
        {
            case BoxSize.Small:
                return GetPrefabBoxCapacity(playerSmallBoxPrefab, BoxSize.Small);
            case BoxSize.Medium:
                return GetPrefabBoxCapacity(playerMediumBoxPrefab, BoxSize.Medium);
            case BoxSize.Large:
                return GetPrefabBoxCapacity(playerLargeBoxPrefab, BoxSize.Large);
        }

        return 0;
    }

    private int GetPrefabBoxCapacity(GameObject prefab, BoxSize size)
    {
        if (prefab == null)
            return 0;

        Box box = prefab.GetComponent<Box>();
        if (box == null)
            return 0;

        return box.GetCapacityBySize(size);
    }

    private void RefreshSlotsText()
    {
        if (slotsText == null)
            return;

        int used = GetItemCapacity();
        int max = GetMaxCapacity();

        slotsText.text = $"{used}/{max}";
        slotsText.color = normalSlotsColor;

        if (slotsRect != null)
            slotsRect.anchoredPosition = slotsBaseAnchoredPos;

        slotsText.transform.localScale = slotsBaseScale;
    }

    private void PlaySuccessSlotsJuice()
    {
        if (slotsText == null)
            return;

        if (slotsJuiceRoutine != null)
            StopCoroutine(slotsJuiceRoutine);

        if (slotsErrorRoutine != null)
            StopCoroutine(slotsErrorRoutine);

        slotsText.color = normalSlotsColor;
        slotsJuiceRoutine = StartCoroutine(SuccessSlotsJuiceRoutine());
    }

    private void PlayFailSlotsJuice()
    {
        if (slotsText == null)
            return;

        if (slotsJuiceRoutine != null)
            StopCoroutine(slotsJuiceRoutine);

        if (slotsErrorRoutine != null)
            StopCoroutine(slotsErrorRoutine);

        slotsErrorRoutine = StartCoroutine(FailSlotsJuiceRoutine());
    }

    private IEnumerator SuccessSlotsJuiceRoutine()
    {
        float t = 0f;

        while (t < successSquashDuration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / successSquashDuration);
            slotsText.transform.localScale = Vector3.LerpUnclamped(slotsBaseScale, Vector3.Scale(slotsBaseScale, successStartScale), n);
            yield return null;
        }

        t = 0f;
        while (t < successSettleDuration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / successSettleDuration);
            slotsText.transform.localScale = Vector3.LerpUnclamped(
                Vector3.Scale(slotsBaseScale, successStartScale),
                Vector3.Scale(slotsBaseScale, successOvershootScale),
                n
            );
            yield return null;
        }

        float shakeTimer = 0f;
        while (shakeTimer < successShakeDuration)
        {
            shakeTimer += Time.deltaTime;

            if (slotsRect != null)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle * successShakeStrength;
                slotsRect.anchoredPosition = slotsBaseAnchoredPos + offset;
            }

            yield return null;
        }

        if (slotsRect != null)
            slotsRect.anchoredPosition = slotsBaseAnchoredPos;

        slotsText.transform.localScale = slotsBaseScale;
        slotsJuiceRoutine = null;
    }

    private IEnumerator FailSlotsJuiceRoutine()
    {
        slotsText.color = errorSlotsColor;

        float shakeTimer = 0f;
        while (shakeTimer < failShakeDuration)
        {
            shakeTimer += Time.deltaTime;

            if (slotsRect != null)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle * failShakeStrength;
                slotsRect.anchoredPosition = slotsBaseAnchoredPos + offset;
            }

            yield return null;
        }

        if (slotsRect != null)
            slotsRect.anchoredPosition = slotsBaseAnchoredPos;

        yield return new WaitForSeconds(failColorDuration);

        slotsText.color = normalSlotsColor;
        slotsErrorRoutine = null;
    }

    private void ModifyItemsInWrapper()
    {
        // Reset items to initial values
        List<WorldItem> itemsCopy = new List<WorldItem>();
        itemsCopy.AddRange(currentBox.GetComponent<Box>().playerItemPool);

        currentBox.GetComponent<Box>().playerItemPool.Clear();

        foreach (WorldItem item in itemsCopy)
        {
            Item itemData = Resources.Load<Item>("Items/" + item.itemID);

            WorldItem worldItem = new WorldItem();
            worldItem.Setup(itemData);

            currentBox.GetComponent<Box>().playerItemPool.Add(worldItem);
        }

        // Modify items with new passives
        ApplyPassives();
    }

    private void ApplyPassives()
    {
        Box box = currentBox.GetComponent<Box>();

        foreach (WorldItem item in currentBox.GetComponent<Box>().playerItemPool)
        {
            foreach (Passive passive in item.passives)
            {
                Debug.Log($"Aplicando pasiva {passive.GetType().Name} al item {item.displayName}");
                passive.ExecutePassive(box, item.passivesInfo);
            }
        }
    }

    private void CalculatePrice()
    {
        Box box = currentBox.GetComponent<Box>();
        int totalPrice = 0;

        foreach (WorldItem item in currentBox.GetComponent<Box>().playerItemPool)
        {
            totalPrice += item.value;
        }

        box.value = (int)Math.Round(totalPrice * (1 + box.extraPercentage) + box.extraValue);
    }

    private void CalculateSellTime()
    {
        Box box = currentBox.GetComponent<Box>();
        double sellTimeIndex = 0;

        foreach (WorldItem item in currentBox.GetComponent<Box>().playerItemPool)
        {
            sellTimeIndex += int.Parse(item.GetAttribute(Attributes.SELL_TIME).value);
        }

        sellTimeIndex /= currentBox.GetComponent<Box>().playerItemPool.Count;

        int finalIndex = (int)Math.Round(sellTimeIndex);

        box.sellTimeIndex = finalIndex;
    }

    private void ChooseBoxTag()
    {
        Box box = currentBox.GetComponent<Box>();
        String tag = "";

        switch (box.sellTimeIndex)
        {
            case 0:
                tag = "Very Slow";
                break;
            case 1:
                tag = "Slow";
                break;
            case 2:
                tag = "Fast";
                break;
            case 3:
                tag = "Very Fast";
                break;
        }

        GameObject go = new GameObject("BoxTag");
    }
}