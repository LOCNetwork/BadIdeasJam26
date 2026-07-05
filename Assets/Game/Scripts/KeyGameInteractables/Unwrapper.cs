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

public enum UnwrapperSpawnDirectionAxis
{
    Up,
    Right
}

public class Unwrapper : Interactable
{
    [Header("Validation")]
    [SerializeField] private bool requireUnwrapperTag = true;

    [Header("Drops")]
    [SerializeField] private RarityDropRates rarityRates;
    [SerializeField] private GameObject itemPrefab;

    [Header("Spawn Points")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    [SerializeField] private UnwrapperSpawnDirectionAxis spawnDirectionAxis = UnwrapperSpawnDirectionAxis.Up;
    [SerializeField] private float minSpawnForce = 2f;
    [SerializeField] private float maxSpawnForce = 5f;

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

        if (consumedBoxGO != null)
            Destroy(consumedBoxGO);

        // INTRO
        if (animator != null && profile != null && !string.IsNullOrEmpty(profile.introTrigger))
        {
            animator.SetTrigger(profile.introTrigger);
        }

        if (profile != null && profile.introDuration > 0f)
            yield return new WaitForSeconds(profile.introDuration);

        // Preparar puntos de spawn aleatorios sin repetición
        List<Transform> shuffledPoints = new List<Transform>(spawnPoints);
        ShuffleTransformList(shuffledPoints);

        if (shuffledPoints.Count == 0)
        {
            Debug.LogError($"{name}: No hay spawnPoints asignados en Unwrapper.");
            isBusy = false;
            yield break;
        }

        if (rolledItems.Count > shuffledPoints.Count)
        {
            Debug.LogWarning($"{name}: Hay más items que spawnPoints. Se reutilizarán puntos.");
        }

        // LOOP + spawn
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

            Transform spawnPoint = shuffledPoints[i % shuffledPoints.Count];
            SpawnRolledItem(item, spawnPoint);
        }

        isBusy = false;
    }

    private void SpawnRolledItem(Item item, Transform spawnPoint)
    {
        if (itemPrefab == null)
        {
            Debug.LogError($"{name}: No hay itemPrefab asignado en Unwrapper.");
            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogError($"{name}: SpawnPoint nulo en Unwrapper.");
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

        Rigidbody2D rb = instance.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 dir = GetSpawnDownDirection(spawnPoint);
            float force = Random.Range(minSpawnForce, maxSpawnForce);

            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.AddForce(dir * force, ForceMode2D.Impulse);
        }
    }

    private Vector2 GetSpawnDownDirection(Transform spawnPoint)
    {
        if (spawnPoint == null)
            return Vector2.down;

        Vector2 baseDir = spawnDirectionAxis == UnwrapperSpawnDirectionAxis.Up
            ? (Vector2)spawnPoint.up
            : (Vector2)spawnPoint.right;

        if (baseDir.sqrMagnitude < 0.0001f)
            baseDir = Vector2.up;

        // Hacia abajo = invertir la dirección del eje elegido
        return (-baseDir).normalized;
    }

    private void ShuffleTransformList(List<Transform> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            Transform temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
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