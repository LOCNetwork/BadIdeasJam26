using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BoxAnimationProfile
{
    public BoxSize boxSize;

    [Header("Animator Triggers")]
    public string introTrigger;
    public string loopTrigger;

    [Header("Timings")]
    [Min(0f)] public float introDuration = 0.5f;
    [Min(0f)] public float loopDuration = 0.3f;
}

public class Unwrapper : Interactable
{
    [Header("Validation")]
    [SerializeField] private bool requireUnwrapperTag = true;

    [Header("Drops")]
    [SerializeField] private RarityDropRates rarityRates;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private List<BoxAnimationProfile> animationProfiles = new List<BoxAnimationProfile>();

    private bool isBusy = false;

    public override void Interact(Player player)
    {
        if (isBusy)
            return;

        if (player == null)
            return;

        if (requireUnwrapperTag && !CompareTag("Unwrapper"))
        {
            Debug.LogWarning($"{name}: Este objeto no tiene tag 'Unwrapper'.");
            return;
        }

        if (!player.TryTakeTopHeldBox(out GameObject boxGO, out Box boxData))
        {
            Debug.Log("No tienes una caja en la última posición de la pila para abrir.");
            return;
        }

        StartCoroutine(UnwrapRoutine(boxGO, boxData));
    }

    private IEnumerator UnwrapRoutine(GameObject consumedBoxGO, Box boxData)
    {
        isBusy = true;

        
        List<Item> playerItems =  new List<Item>();
        if (boxData.Type == BoxType.Player)
        {
            foreach (WorldItem item in boxData.playerItemPool)
            {
                playerItems.Add(GameManager.instance.GetItemByID(item.itemID));
            }
        }


        List<Item> rolledItems = boxData.Type == BoxType.Delivery ? boxData.RollContents(rarityRates) : playerItems;
        BoxAnimationProfile profile = GetProfile(boxData.Size);

        // Ya hemos leído todo lo necesario de la caja
        if (consumedBoxGO != null)
            Destroy(consumedBoxGO);

        // 1) INTRO
        if (animator != null && profile != null && !string.IsNullOrEmpty(profile.introTrigger))
        {
            animator.SetTrigger(profile.introTrigger);
        }

        if (profile != null && profile.introDuration > 0f)
            yield return new WaitForSeconds(profile.introDuration);

        // 2) CADA ITEM: trigger loop -> esperar loopDuration -> spawn item
        for (int i = 0; i < rolledItems.Count; i++)
        {
            Item item = rolledItems[i];
            if (item == null)
                continue;

            if (animator != null && profile != null && !string.IsNullOrEmpty(profile.loopTrigger))
            {
                animator.SetTrigger(profile.loopTrigger);
            }

            if (profile != null && profile.loopDuration > 0f)
                yield return new WaitForSeconds(profile.loopDuration);

            SpawnRolledItem(item);
        }

        isBusy = false;
    }

    private void SpawnRolledItem(Item item)
    {
        if (itemPrefab == null)
        {
            Debug.LogError($"{name}: No hay itemPrefab asignado en Unwrapper.");
            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogError($"{name}: No hay spawnPoint asignado en Unwrapper.");
            return;
        }

        GameObject instance = Instantiate(itemPrefab, spawnPoint.position, Quaternion.identity);
        instance.SetActive(false);

        ItemGenerator generator = instance.GetComponent<ItemGenerator>();
        if (generator == null)
            generator = instance.AddComponent<ItemGenerator>();

        generator.SetTarget(instance);
        generator.generateItemGameObject(item.itemID);

        instance.SetActive(true);
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
}