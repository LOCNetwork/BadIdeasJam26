using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum CatalogType
{
    Paper,
    Clothing,
    Tech,
    Dark
}

public enum BoxSizes
{
    Small = 1,
    Medium = 2,
    Large = 3
}

[System.Serializable]
public class CatalogItem
{
    public CatalogType catalog;
    public BoxSizes size;

    [Header("Price")]
    public int price = 1;

    [Header("Prefab spawned after delivery")]
    public GameObject deliveryPrefab;

    [Header("Visual UI prefab")]
    public GameObject visualPrefab;
}

public class PCShoppingCartManager : MonoBehaviour
{
    public static PCShoppingCartManager Instance { get; private set; }

    [Header("DEBUG MONEY")]
    [SerializeField] private int startingMoney = 100;

    [Header("Money UI")]
    [SerializeField] private TMP_Text moneyText;

    [Header("PC Manager reference")]
    [SerializeField] private PCMenuController pcManager;

    [Header("UI VISUAL SPAWN")]
    [SerializeField] private RectTransform visualSpawnPoint;
    [SerializeField] private Transform visualParent;

    [Header("UI Visual Animator")]
    [SerializeField] private Animator uiAnimator;
    [SerializeField] private string dropTrigger = "Drop";

    [Header("Delivery spawn")]
    [SerializeField] private Transform deliverySpawnPoint;
    [SerializeField] private Animator deliveryAnimator;
    [SerializeField] private float deliveryForce = 6f;

    [Header("Animation names")]
    [SerializeField] private string purchaseTrigger = "Purchase";
    [SerializeField] private float purchaseDuration = 2f;

    [SerializeField] private string deliveryTrigger = "Delivery";
    [SerializeField] private float deliveryDuration = 2f;

    [SerializeField] private string repairTrigger = "Repair";
    [SerializeField] private float repairDuration = 2f;

    [Header("Catalog items")]
    [SerializeField] private List<CatalogItem> catalogItems = new();

    private int currentMoney;
    private int reservedMoney;

    private bool purchaseRunning;
    private bool pcLocked;
    private bool wasPcOpenLastFrame;

    private readonly List<CatalogItem> cart = new();
    private readonly List<GameObject> visualObjects = new();

    public static bool IsGloballyLocked =>
        Instance != null && Instance.IsPCLocked();

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        currentMoney = startingMoney;
        UpdateMoneyUI();

        if (pcManager != null)
            wasPcOpenLastFrame = pcManager.IsOpen;
    }

    private void Update()
    {
        if (pcManager == null)
            return;

        bool isCurrentlyOpen = pcManager.IsOpen;

        if (wasPcOpenLastFrame && !isCurrentlyOpen)
        {
            if (!purchaseRunning)
                ClearCart();
        }

        wasPcOpenLastFrame = isCurrentlyOpen;
    }

    private void UpdateMoneyUI()
    {
        int remaining = currentMoney - reservedMoney;

        if (moneyText != null)
        {
            moneyText.text =
                $"Money: {currentMoney}\n" +
                $"Cart Cost: {reservedMoney}\n" +
                $"Remaining: {remaining}";
        }
    }

    private int RemainingMoney()
    {
        return currentMoney - reservedMoney;
    }

    private CatalogItem GetItem(CatalogType catalog, BoxSizes size)
    {
        foreach (var item in catalogItems)
        {
            if (item.catalog == catalog && item.size == size)
                return item;
        }

        return null;
    }

    private void AddToCart(CatalogType catalog, BoxSizes size)
    {
        if (purchaseRunning)
            return;

        CatalogItem item = GetItem(catalog, size);

        if (item == null)
        {
            Debug.Log("Item not configured in catalog");
            return;
        }

        if (RemainingMoney() < item.price)
        {
            Debug.Log("Not enough money");
            return;
        }

        cart.Add(item);
        reservedMoney += item.price;

        Debug.Log(
            $"Money Max: {currentMoney}\n" +
            $"Money Spending (cart): {reservedMoney}\n" +
            $"Money Remaining After Purchase: {currentMoney - reservedMoney}"
        );

        UpdateMoneyUI();
        SpawnVisual(item);
    }

    private void SpawnVisual(CatalogItem item)
    {
        if (item.visualPrefab == null || visualSpawnPoint == null || visualParent == null)
            return;

        if (uiAnimator != null && !string.IsNullOrEmpty(dropTrigger))
            uiAnimator.SetTrigger(dropTrigger);

        GameObject obj = Instantiate(item.visualPrefab, visualParent);

        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect != null)
            rect.anchoredPosition = visualSpawnPoint.anchoredPosition;

        visualObjects.Add(obj);
    }

    public void AddPaperSmall() => AddToCart(CatalogType.Paper, BoxSizes.Small);
    public void AddPaperMedium() => AddToCart(CatalogType.Paper, BoxSizes.Medium);
    public void AddPaperLarge() => AddToCart(CatalogType.Paper, BoxSizes.Large);

    public void AddClothingSmall() => AddToCart(CatalogType.Clothing, BoxSizes.Small);
    public void AddClothingMedium() => AddToCart(CatalogType.Clothing, BoxSizes.Medium);
    public void AddClothingLarge() => AddToCart(CatalogType.Clothing, BoxSizes.Large);

    public void AddTechSmall() => AddToCart(CatalogType.Tech, BoxSizes.Small);
    public void AddTechMedium() => AddToCart(CatalogType.Tech, BoxSizes.Medium);
    public void AddTechLarge() => AddToCart(CatalogType.Tech, BoxSizes.Large);

    public void AddDarkSmall() => AddToCart(CatalogType.Dark, BoxSizes.Small);
    public void AddDarkMedium() => AddToCart(CatalogType.Dark, BoxSizes.Medium);
    public void AddDarkLarge() => AddToCart(CatalogType.Dark, BoxSizes.Large);

    public void Purchase()
    {
        if (purchaseRunning)
            return;

        if (cart.Count == 0)
        {
            Debug.Log("No items in cart");
            return;
        }

        StartCoroutine(PurchaseRoutine());
    }

    private IEnumerator PurchaseRoutine()
    {
        purchaseRunning = true;
        pcLocked = true;

        int spentThisPurchase = reservedMoney;
        currentMoney -= spentThisPurchase;

        Debug.Log(
            $"PURCHASE CONFIRMED\n" +
            $"Money Max: {currentMoney + spentThisPurchase}\n" +
            $"Money Spent: {spentThisPurchase}\n" +
            $"Money Remaining: {currentMoney}"
        );

        UpdateMoneyUI();

        if (deliveryAnimator != null && !string.IsNullOrEmpty(purchaseTrigger))
            deliveryAnimator.SetTrigger(purchaseTrigger);

        yield return new WaitForSeconds(purchaseDuration);

        if (pcManager != null)
            pcManager.Close();

        yield return new WaitForSeconds(0.3f);

        if (deliveryAnimator != null && !string.IsNullOrEmpty(deliveryTrigger))
            deliveryAnimator.SetTrigger(deliveryTrigger);

        yield return new WaitForSeconds(deliveryDuration);

        SpawnDelivery();

        if (deliveryAnimator != null && !string.IsNullOrEmpty(repairTrigger))
            deliveryAnimator.SetTrigger(repairTrigger);

        // Sigue bloqueado durante la reconstrucción/reparación
        yield return new WaitForSeconds(repairDuration);

        pcLocked = false;
        ClearCart();
        purchaseRunning = false;
    }

    private void SpawnDelivery()
    {
        if (deliverySpawnPoint == null)
        {
            Debug.LogWarning("DeliverySpawnPoint is not assigned.");
            return;
        }

        foreach (var item in cart)
        {
            if (item == null || item.deliveryPrefab == null)
                continue;

            Vector3 spawnPos = deliverySpawnPoint.position;
            Quaternion spawnRot = deliverySpawnPoint.rotation;

            GameObject obj = Instantiate(item.deliveryPrefab, spawnPos, spawnRot);

            Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 random = Random.insideUnitCircle.normalized;
                rb.AddForce(random * deliveryForce, ForceMode2D.Impulse);
            }
        }
    }

    public void ClearCart()
    {
        reservedMoney = 0;
        cart.Clear();

        foreach (var obj in visualObjects)
        {
            if (obj != null)
                Destroy(obj);
        }

        visualObjects.Clear();
        UpdateMoneyUI();
    }

    public bool IsPCLocked()
    {
        return pcLocked || purchaseRunning;
    }
}
