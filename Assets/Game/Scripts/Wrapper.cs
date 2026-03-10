using NUnit.Framework.Internal.Execution;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


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
    [SerializeField] private List<BoxAnimationProfile> animationProfiles = new List<BoxAnimationProfile>();

    private List<WorldItem> currentItemsInWrapper = new List<WorldItem>();

    private BoxSize AVAILABLE_BOX_SIZE = BoxSize.Medium;

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
            if (currentItemsInWrapper.Count == 0)
            {
                Debug.Log("El Wrapper no tiene items insertados.");
                return;
            }

            SpawnBox(AVAILABLE_BOX_SIZE);
            return;
        }

        StartCoroutine(WrapRoutine(player, itemData.boxSlots));
    }

    private IEnumerator WrapRoutine(Player player, int boxSlots)
    {
        isBusy = true;


        Box box = new Box();


        // Insert item in list
        int currentCapacityFilled = GetItemCapacity();

        int boxCapacity = box.GetCapacityBySize(AVAILABLE_BOX_SIZE);

        if (currentCapacityFilled + boxSlots <= boxCapacity)
        {
            BoxAnimationProfile profile = GetProfile(AVAILABLE_BOX_SIZE);

            // Take item from player if it fits
            player.TryTakeTopHeldItem(out GameObject consumedItem, out WorldItem itemData);

            WorldItem worldItemCopy = new WorldItem();
            worldItemCopy.Setup(itemData);

            if (consumedItem != null)
                Destroy(consumedItem);

            // Animation
            if (animator != null && profile != null && !string.IsNullOrEmpty(profile.introTrigger))
            {
                animator.SetTrigger(profile.introTrigger);
            }

            if (profile != null && profile.introDuration > 0f)
                yield return new WaitForSeconds(profile.introDuration);


            currentItemsInWrapper.Add(worldItemCopy);

            if (currentCapacityFilled + boxSlots == boxCapacity)
            {
                SpawnBox(AVAILABLE_BOX_SIZE);
            }

            Debug.Log($"Estadisticas Wrapper --> ESPACIO RELLENO: {currentCapacityFilled + boxSlots}, TOTAL SLOTS CAJA: {boxCapacity}, SOBRANTE: {boxCapacity - (currentCapacityFilled + boxSlots)}");

        } 
        else
        {
            Debug.Log($"La caja está llena.");
        }

        

        isBusy = false;
    }

    private void SpawnBox(BoxSize boxSize)
    {
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
     
        box.SetActive(true);

        Box boxData = box.GetComponent<Box>();

        // Add items stacked in wrapper to box and clear items in wrapper
        boxData.playerItemPool.AddRange(currentItemsInWrapper);

        currentItemsInWrapper.Clear();

        Debug.Log("Caja spawneada");
    }

    private BoxAnimationProfile GetProfile(BoxSize size)
    {
        for (int i = 0; i < animationProfiles.Count; i++)
        {
            if (animationProfiles[i].boxSize == size)
                return animationProfiles[i];
        }

        return null;
    }


    private int GetItemCapacity()
    {
        int capacity = 0;

        foreach (var item in currentItemsInWrapper)
        {
            capacity += item.boxSlots;
        }

        return capacity;
    }


    private void ModifyItemsInWrapper()
    {
        // Reset items to initial values
        List<WorldItem> itemsCopy = new List<WorldItem>();
        itemsCopy.AddRange(currentItemsInWrapper);

        currentItemsInWrapper.Clear();

        foreach (WorldItem item in itemsCopy)
        {
            Item itemData = Resources.Load<Item>("Items/" + item.itemID);

            WorldItem worldItem = new WorldItem();
            worldItem.Setup(itemData);

            currentItemsInWrapper.Add(worldItem);
        }


        // Modify items with new passives

        ApplyPassives();
    }

    private void ApplyPassives()
    {

        foreach (WorldItem item in currentItemsInWrapper)
        {
            foreach (Passive passive in item.passives) {
                passive.ExecutePassive();
            }
        }

    }


}
