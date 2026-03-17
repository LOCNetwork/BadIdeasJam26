using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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

[System.Serializable]
public class CatalogUIBinding
{
    public CatalogType catalog;

    [Header("UI Drop")]
    public Animator uiDropAnimator;
    public RectTransform visualSpawnPoint;

    [Header("Shared Parent Candidate For This Unlock Level")]
    public Transform visualParent;

    [Header("UI Purchase")]
    public Animator uiPurchaseAnimator;

    [Header("Unlock Level For Shared Parent Priority")]
    [Min(0)] public int unlockLevel = 0;
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

    [Header("UI Trigger Names")]
    [SerializeField] private string dropTrigger = "Drop";
    [SerializeField] private string purchaseTrigger = "Purchase";

    [Header("Catalog UI Bindings")]
    [SerializeField] private List<CatalogUIBinding> catalogUIBindings = new();

    [Header("Delivery / Repair (world)")]
    [SerializeField] private Transform deliverySpawnPoint;
    [SerializeField] private Animator deliveryAnimator;
    [SerializeField] private float deliveryForce = 6f;

    [Header("Animation timings")]
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

    private CatalogType activeUICatalog = CatalogType.Paper;

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

    private int GetActivePCUpgradeLevel()
    {
        if (pcManager == null)
        {
            Debug.LogWarning("PCShoppingCartManager: pcManager no asignado, usando nivel 0.");
            return 0;
        }

        System.Type pcType = pcManager.GetType();

        FieldInfo debugModeField = pcType.GetField("debugMode", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo debugUpgradeField = pcType.GetField("debugUpgradeLevel", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo currentUpgradeField = pcType.GetField("currentUpgradeLevel", BindingFlags.NonPublic | BindingFlags.Instance);

        if (debugModeField == null || debugUpgradeField == null || currentUpgradeField == null)
        {
            Debug.LogWarning("PCShoppingCartManager: no se pudieron encontrar los campos de nivel en PCMenuController, usando nivel 0.");
            return 0;
        }

        bool debugMode = (bool)debugModeField.GetValue(pcManager);

        if (debugMode)
            return (int)debugUpgradeField.GetValue(pcManager);

        return (int)currentUpgradeField.GetValue(pcManager);
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

    private CatalogUIBinding GetUIBinding(CatalogType catalog)
    {
        foreach (var binding in catalogUIBindings)
        {
            if (binding.catalog == catalog)
                return binding;
        }

        return null;
    }

    private Transform GetHighestUnlockedVisualParent()
    {
        int level = GetActivePCUpgradeLevel();
        CatalogUIBinding bestBinding = null;

        for (int i = 0; i < catalogUIBindings.Count; i++)
        {
            CatalogUIBinding binding = catalogUIBindings[i];
            if (binding == null || binding.visualParent == null)
                continue;

            if (binding.unlockLevel > level)
                continue;

            if (bestBinding == null || binding.unlockLevel > bestBinding.unlockLevel)
                bestBinding = binding;
        }

        return bestBinding != null ? bestBinding.visualParent : null;
    }

    // ============================
    // ACTIVE PAGE FOR UI
    // ============================

    public void SetActivePagePaper() => activeUICatalog = CatalogType.Paper;
    public void SetActivePageClothing() => activeUICatalog = CatalogType.Clothing;
    public void SetActivePageTech() => activeUICatalog = CatalogType.Tech;
    public void SetActivePageDark() => activeUICatalog = CatalogType.Dark;

    // ============================
    // ADD TO CART
    // ============================

    private void AddToCart(CatalogType catalog, BoxSizes size)
    {
        if (purchaseRunning)
            return;

        CatalogItem item = GetItem(catalog, size);

        if (item == null)
        {
            Debug.Log("Item not configured in catalog.");
            return;
        }

        if (RemainingMoney() < item.price)
        {
            Debug.Log("Not enough money.");
            return;
        }

        cart.Add(item);
        reservedMoney += item.price;

        activeUICatalog = catalog;

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
        CatalogUIBinding binding = GetUIBinding(item.catalog);
        if (binding == null)
        {
            Debug.LogWarning($"No UI binding configured for catalog {item.catalog}");
            return;
        }

        if (item.visualPrefab == null)
            return;

        Transform sharedParent = GetHighestUnlockedVisualParent();

        if (binding.visualSpawnPoint == null || sharedParent == null)
        {
            Debug.LogWarning($"Missing VisualSpawnPoint or shared VisualParent for catalog {item.catalog}");
            return;
        }

        if (binding.uiDropAnimator != null && !string.IsNullOrEmpty(dropTrigger))
            binding.uiDropAnimator.SetTrigger(dropTrigger);

        GameObject obj = Instantiate(item.visualPrefab, sharedParent);

        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect != null)
            rect.anchoredPosition = binding.visualSpawnPoint.anchoredPosition;

        visualObjects.Add(obj);
    }

    // ============================
    // BUTTON METHODS - ADD
    // ============================

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

    // ============================
    // PURCHASE BUTTONS
    // ============================

    public void Purchase()
    {
        PurchaseForCatalog(activeUICatalog);
    }

    public void PurchasePaper() => PurchaseForCatalog(CatalogType.Paper);
    public void PurchaseClothing() => PurchaseForCatalog(CatalogType.Clothing);
    public void PurchaseTech() => PurchaseForCatalog(CatalogType.Tech);
    public void PurchaseDark() => PurchaseForCatalog(CatalogType.Dark);

    private void PurchaseForCatalog(CatalogType catalog)
    {
        if (purchaseRunning)
            return;

        if (cart.Count == 0)
        {
            Debug.Log("No items in cart.");
            return;
        }

        activeUICatalog = catalog;
        StartCoroutine(PurchaseRoutine(activeUICatalog));
    }

    private IEnumerator PurchaseRoutine(CatalogType purchaseUICatalog)
    {
        purchaseRunning = true;
        pcLocked = true;

        int spentThisPurchase = reservedMoney;
        currentMoney -= spentThisPurchase;

        Debug.Log(
            $"PURCHASE CONFIRMED\n" +
            $"Money Max: {currentMoney + spentThisPurchase}\n" +
            $"Money Spent This Purchase: {spentThisPurchase}\n" +
            $"Money Remaining: {currentMoney}"
        );

        UpdateMoneyUI();

        CatalogUIBinding binding = GetUIBinding(purchaseUICatalog);
        if (binding != null && binding.uiPurchaseAnimator != null && !string.IsNullOrEmpty(purchaseTrigger))
            binding.uiPurchaseAnimator.SetTrigger(purchaseTrigger);

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

        yield return new WaitForSeconds(repairDuration);

        pcLocked = false;
        ClearCart();
        purchaseRunning = false;
    }

    // ============================
    // DELIVERY
    // ============================

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

    // ============================
    // CLEAR CART
    // ============================

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

    // ============================
    // LOCK PC INTERACTION
    // ============================

    public bool IsPCLocked()
    {
        return pcLocked || purchaseRunning;
    }
}