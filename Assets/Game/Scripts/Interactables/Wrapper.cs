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

    public BoxSize AVAILABLE_BOX_SIZE = BoxSize.Medium;

    private GameObject currentBox;

    private bool isBusy = false;

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

            StartCoroutine(SpawnBoxRoutine(AVAILABLE_BOX_SIZE));
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

            if (currentCapacityFilled + boxSlots == boxCapacity)
            {
                yield return StartCoroutine(SpawnBoxRoutine(AVAILABLE_BOX_SIZE));
            }

            Debug.Log($"Estadisticas Wrapper --> ESPACIO RELLENO: {currentCapacityFilled + boxSlots}, TOTAL SLOTS CAJA: {boxCapacity}, SOBRANTE: {boxCapacity - (currentCapacityFilled + boxSlots)}");
        }
        else
        {
            Debug.Log($"La caja está llena.");
        }

        isBusy = false;
    }

    private IEnumerator SpawnBoxRoutine(BoxSize boxSize)
    {
        if (isBusy && currentBox == null)
            yield break;

        TriggerWrapAnimation(boxSize);

        float waitTime = GetWrapDuration(boxSize);
        if (waitTime > 0f)
            yield return new WaitForSeconds(waitTime);

        SpawnBox(boxSize);
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
        int capacity = 0;

        foreach (var item in currentBox.GetComponent<Box>().playerItemPool)
        {
            capacity += item.boxSlots;
        }

        return capacity;
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