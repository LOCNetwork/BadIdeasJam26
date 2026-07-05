using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuotaUpgradeSelectionUI : MonoBehaviour
{
    private enum UpgradeType
    {
        PlayerSpeed,
        WrapperCapacity,
        CarryCapacity,
        Catalogs
    }

    [System.Serializable]
    private class UpgradeVisual
    {
        public TMP_Text levelText;
        public Button button;
        public int maxLevel = 1;
    }

    [Header("Core References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private QuotaDepositMachine quotaMachine;
    [SerializeField] private GameObject upgradeUIRoot;
    [SerializeField] private RectTransform upgradeUIRect;
    [SerializeField] private CanvasGroup upgradeUICanvasGroup;

    [Header("Targets To Modify")]
    [SerializeField] private Player player;
    [SerializeField] private List<Wrapper> wrappers = new List<Wrapper>();
    [SerializeField] private PCMenuController pcMenuController;

    [Header("Upgrade Visuals")]
    [SerializeField] private UpgradeVisual speedUpgrade = new UpgradeVisual();
    [SerializeField] private UpgradeVisual wrapperUpgrade = new UpgradeVisual();
    [SerializeField] private UpgradeVisual carryUpgrade = new UpgradeVisual();
    [SerializeField] private UpgradeVisual catalogUpgrade = new UpgradeVisual();

    [Header("Open Juice")]
    [SerializeField] private float openScaleDuration = 0.14f;
    [SerializeField] private float openSettleDuration = 0.10f;
    [SerializeField] private Vector3 openStartScaleMultiplier = new Vector3(1f, 0f, 1f);
    [SerializeField] private Vector3 openOvershootScaleMultiplier = new Vector3(0.92f, 1.12f, 1f);

    [Header("Close Juice")]
    [SerializeField] private float closeSquashDuration = 0.12f;
    [SerializeField] private float closeCollapseDuration = 0.12f;
    [SerializeField] private Vector3 closeOvershootScaleMultiplier = new Vector3(1.10f, 0.82f, 1f);

    [Header("Blocked Click Feedback")]
    [SerializeField] private Color blockedTextColor = Color.red;
    [SerializeField] private float blockedShakeDuration = 0.15f;
    [SerializeField] private float blockedShakeStrength = 8f;
    [SerializeField] private float blockedRedDuration = 0.18f;

    [Header("Player Speed Upgrade")]
    [SerializeField] private float speedIncreasePerLevel = 1f;

    [Header("Carry Capacity Upgrade")]
    [SerializeField] private int carryIncreasePerLevel = 1;

    [Header("Initial Levels")]
    [SerializeField] private int currentSpeedLevel = 0;
    [SerializeField] private int currentWrapperLevel = 0;
    [SerializeField] private int currentCarryLevel = 0;
    [SerializeField] private int currentCatalogLevel = 0;

    private bool upgradeUIOpen = false;
    private bool upgradeAlreadyChosenThisQuota = false;
    private bool isOpening = false;
    private bool isClosing = false;
    private int lastObservedDay = -1;

    private FieldInfo playerSpeedField;
    private FieldInfo playerMaxHoldingField;
    private FieldInfo pcCurrentUpgradeLevelField;
    private FieldInfo pcDebugModeField;
    private FieldInfo pcDebugUpgradeLevelField;
    private MethodInfo wrapperRefreshSlotsMethod;

    private Coroutine openCloseRoutine;
    private readonly Dictionary<TMP_Text, Coroutine> textFeedbackRoutines = new();
    private readonly Dictionary<TMP_Text, Color> originalTextColors = new();
    private readonly Dictionary<TMP_Text, Vector2> originalTextAnchoredPositions = new();

    private Vector3 originalPanelScale = Vector3.one;
    private float originalPanelAlpha = 1f;

    private void Awake()
    {
        if (gameManager == null)
            gameManager = GameManager.instance;

        CacheReflection();
    }

    private void Start()
    {
        if (gameManager == null)
            gameManager = GameManager.instance;

        if (player == null)
            player = FindFirstObjectByType<Player>();

        if (upgradeUICanvasGroup == null && upgradeUIRoot != null)
            upgradeUICanvasGroup = upgradeUIRoot.GetComponent<CanvasGroup>();

        if (upgradeUIRect == null && upgradeUIRoot != null)
            upgradeUIRect = upgradeUIRoot.GetComponent<RectTransform>();

        if (upgradeUIRect != null)
            originalPanelScale = upgradeUIRect.localScale;

        if (upgradeUICanvasGroup != null)
            originalPanelAlpha = upgradeUICanvasGroup.alpha;

        CacheInitialTextStates();

        if (upgradeUIRoot != null)
            upgradeUIRoot.SetActive(false);

        lastObservedDay = gameManager != null ? gameManager.currentDay : 0;

        ApplyInitialWrapperLevel();
        ApplyInitialCatalogLevel();
        RefreshAllTextsAndButtons();
    }

    private void Update()
    {
        if (gameManager == null || quotaMachine == null)
            return;

        DetectDayChange();
        DetectQuotaPaidAndOpenUI();
    }

    private void OnDisable()
    {
        if (upgradeUIOpen)
            ResumeGame();

        StopAllTextFeedback();
    }

    private void OnDestroy()
    {
        if (upgradeUIOpen)
            ResumeGame();

        StopAllTextFeedback();
    }

    private void CacheReflection()
    {
        playerSpeedField = typeof(Player).GetField("speed", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        playerMaxHoldingField = typeof(Player).GetField("maxHolding", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        pcCurrentUpgradeLevelField = typeof(PCMenuController).GetField("currentUpgradeLevel", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        pcDebugModeField = typeof(PCMenuController).GetField("debugMode", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        pcDebugUpgradeLevelField = typeof(PCMenuController).GetField("debugUpgradeLevel", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        wrapperRefreshSlotsMethod = typeof(Wrapper).GetMethod("RefreshSlotsText", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
    }

    private void CacheInitialTextStates()
    {
        CacheTMPState(speedUpgrade.levelText);
        CacheTMPState(wrapperUpgrade.levelText);
        CacheTMPState(carryUpgrade.levelText);
        CacheTMPState(catalogUpgrade.levelText);
    }

    private void CacheTMPState(TMP_Text text)
    {
        if (text == null)
            return;

        if (!originalTextColors.ContainsKey(text))
            originalTextColors[text] = text.color;

        RectTransform rect = text.GetComponent<RectTransform>();
        if (rect != null && !originalTextAnchoredPositions.ContainsKey(text))
            originalTextAnchoredPositions[text] = rect.anchoredPosition;
    }

    private void DetectDayChange()
    {
        if (gameManager.currentDay != lastObservedDay)
        {
            lastObservedDay = gameManager.currentDay;
            upgradeAlreadyChosenThisQuota = false;
        }
    }

    private void DetectQuotaPaidAndOpenUI()
    {
        if (upgradeUIOpen || isOpening || isClosing)
            return;

        if (upgradeAlreadyChosenThisQuota)
            return;

        int deposited = quotaMachine.GetCurrentDeposited();
        int quota = quotaMachine.GetCurrentQuota();

        if (deposited < quota)
            return;

        OpenUpgradeUI();
    }

    private void OpenUpgradeUI()
    {
        if (upgradeUIRoot == null)
            return;

        if (currentCarryLevel >= carryUpgrade.maxLevel && currentSpeedLevel >= speedUpgrade.maxLevel && currentWrapperLevel >= wrapperUpgrade.maxLevel && currentCatalogLevel >= catalogUpgrade.maxLevel)
        {
            return;
        }

        upgradeUIOpen = true;
        isOpening = true;

        if (openCloseRoutine != null)
            StopCoroutine(openCloseRoutine);

        upgradeUIRoot.SetActive(true);
        RefreshAllTextsAndButtons();
        PauseGame();

        openCloseRoutine = StartCoroutine(OpenRoutine());
    }

    private void CloseUpgradeUI()
    {
        if (upgradeUIRoot == null)
        {
            upgradeUIOpen = false;
            upgradeAlreadyChosenThisQuota = true;
            ResumeGame();
            return;
        }

        if (isClosing)
            return;

        isClosing = true;
        upgradeAlreadyChosenThisQuota = true;

        if (openCloseRoutine != null)
            StopCoroutine(openCloseRoutine);

        openCloseRoutine = StartCoroutine(CloseRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        Vector3 startScale = Vector3.Scale(originalPanelScale, openStartScaleMultiplier);
        Vector3 overshootScale = Vector3.Scale(originalPanelScale, openOvershootScaleMultiplier);

        if (upgradeUIRect != null)
            upgradeUIRect.localScale = startScale;

        if (upgradeUICanvasGroup != null)
            upgradeUICanvasGroup.alpha = originalPanelAlpha;

        float t = 0f;
        while (t < openScaleDuration)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(t / openScaleDuration);

            if (upgradeUIRect != null)
                upgradeUIRect.localScale = Vector3.LerpUnclamped(startScale, overshootScale, n);

            yield return null;
        }

        t = 0f;
        while (t < openSettleDuration)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(t / openSettleDuration);

            if (upgradeUIRect != null)
                upgradeUIRect.localScale = Vector3.LerpUnclamped(overshootScale, originalPanelScale, n);

            yield return null;
        }

        if (upgradeUIRect != null)
            upgradeUIRect.localScale = originalPanelScale;

        if (upgradeUICanvasGroup != null)
            upgradeUICanvasGroup.alpha = originalPanelAlpha;

        isOpening = false;
    }

    private IEnumerator CloseRoutine()
    {
        Vector3 startScale = upgradeUIRect != null ? upgradeUIRect.localScale : originalPanelScale;
        Vector3 overshootScale = Vector3.Scale(originalPanelScale, closeOvershootScaleMultiplier);
        Vector3 collapseTarget = new Vector3(originalPanelScale.x, 0f, originalPanelScale.z);

        float t = 0f;
        while (t < closeSquashDuration)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(t / closeSquashDuration);

            if (upgradeUIRect != null)
                upgradeUIRect.localScale = Vector3.LerpUnclamped(startScale, overshootScale, n);

            yield return null;
        }

        float startAlpha = upgradeUICanvasGroup != null ? upgradeUICanvasGroup.alpha : originalPanelAlpha;

        t = 0f;
        while (t < closeCollapseDuration)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(t / closeCollapseDuration);

            if (upgradeUIRect != null)
                upgradeUIRect.localScale = Vector3.LerpUnclamped(overshootScale, collapseTarget, n);

            if (upgradeUICanvasGroup != null)
                upgradeUICanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, n);

            yield return null;
        }

        if (upgradeUICanvasGroup != null)
            upgradeUICanvasGroup.alpha = originalPanelAlpha;

        if (upgradeUIRect != null)
            upgradeUIRect.localScale = originalPanelScale;

        upgradeUIRoot.SetActive(false);

        upgradeUIOpen = false;
        isClosing = false;

        ResumeGame();
    }

    private void PauseGame()
    {
        Time.timeScale = 0f;
    }

    private void ResumeGame()
    {
        Time.timeScale = 1f;
    }

    private void RefreshAllTextsAndButtons()
    {
        RefreshUpgradeVisual(speedUpgrade, currentSpeedLevel);
        RefreshUpgradeVisual(wrapperUpgrade, currentWrapperLevel);
        RefreshUpgradeVisual(carryUpgrade, currentCarryLevel);
        RefreshUpgradeVisual(catalogUpgrade, currentCatalogLevel);

        RestoreTextState(speedUpgrade.levelText);
        RestoreTextState(wrapperUpgrade.levelText);
        RestoreTextState(carryUpgrade.levelText);
        RestoreTextState(catalogUpgrade.levelText);
    }

    private void RefreshUpgradeVisual(UpgradeVisual visual, int currentLevel)
    {
        if (visual.levelText != null)
            visual.levelText.text = $"{currentLevel}/{visual.maxLevel}";

        if (visual.button != null)
            visual.button.interactable = true;
    }

    private bool CanApplyUpgrade(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.PlayerSpeed:
                return currentSpeedLevel < speedUpgrade.maxLevel;
            case UpgradeType.WrapperCapacity:
                return currentWrapperLevel < wrapperUpgrade.maxLevel;
            case UpgradeType.CarryCapacity:
                return currentCarryLevel < carryUpgrade.maxLevel;
            case UpgradeType.Catalogs:
                return currentCatalogLevel < catalogUpgrade.maxLevel;
        }

        return false;
    }

    private void ApplyUpgrade(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.PlayerSpeed:
                ApplyPlayerSpeedUpgrade();
                break;
            case UpgradeType.WrapperCapacity:
                ApplyWrapperCapacityUpgrade();
                break;
            case UpgradeType.CarryCapacity:
                ApplyCarryCapacityUpgrade();
                break;
            case UpgradeType.Catalogs:
                ApplyCatalogUpgrade();
                break;
        }

        RefreshAllTextsAndButtons();
        CloseUpgradeUI();
    }

    private void ApplyPlayerSpeedUpgrade()
    {
        if (!CanApplyUpgrade(UpgradeType.PlayerSpeed))
            return;

        if (player == null)
            player = FindFirstObjectByType<Player>();

        if (player == null)
        {
            Debug.LogWarning("QuotaUpgradeSelectionUI: No se encontró Player para aplicar mejora de velocidad.");
            return;
        }

        if (playerSpeedField == null)
        {
            CacheReflection();
            if (playerSpeedField == null)
            {
                Debug.LogWarning("QuotaUpgradeSelectionUI: No se encontró el campo speed en Player.");
                return;
            }
        }

        object rawValue = playerSpeedField.GetValue(player);
        float currentSpeed;

        if (rawValue is float floatValue)
            currentSpeed = floatValue;
        else if (rawValue is int intValue)
            currentSpeed = intValue;
        else
        {
            Debug.LogWarning("QuotaUpgradeSelectionUI: El campo speed de Player no es compatible.");
            return;
        }

        currentSpeed += speedIncreasePerLevel;
        playerSpeedField.SetValue(player, currentSpeed);

        currentSpeedLevel++;

        Debug.Log($"Mejora aplicada: Speed -> nivel {currentSpeedLevel}, nuevo speed = {currentSpeed}");
    }

    private void ApplyWrapperCapacityUpgrade()
    {
        if (!CanApplyUpgrade(UpgradeType.WrapperCapacity))
            return;

        currentWrapperLevel++;
        ApplyInitialWrapperLevel();
        RefreshWrapperTextsWithoutModifyingWrapper();

        Debug.Log($"Mejora aplicada: Wrapper Capacity -> nivel {currentWrapperLevel}");
    }

    private void ApplyCarryCapacityUpgrade()
    {
        if (!CanApplyUpgrade(UpgradeType.CarryCapacity))
            return;

        if (player == null)
            player = FindFirstObjectByType<Player>();

        if (player == null || playerMaxHoldingField == null)
        {
            Debug.LogWarning("QuotaUpgradeSelectionUI: No se pudo aplicar mejora de carga.");
            return;
        }

        int currentMaxHolding = (int)playerMaxHoldingField.GetValue(player);
        currentMaxHolding += carryIncreasePerLevel;
        playerMaxHoldingField.SetValue(player, currentMaxHolding);

        currentCarryLevel++;

        Debug.Log($"Mejora aplicada: Carry Capacity -> nivel {currentCarryLevel}, nuevo maxHolding = {currentMaxHolding}");
    }

    private void ApplyCatalogUpgrade()
    {
        if (!CanApplyUpgrade(UpgradeType.Catalogs))
            return;

        currentCatalogLevel++;
        ApplyInitialCatalogLevel();

        Debug.Log($"Mejora aplicada: Catalog Upgrade -> nivel {currentCatalogLevel}");
    }

    private void ApplyInitialWrapperLevel()
    {
        BoxSize sizeToApply = BoxSize.Small;

        if (currentWrapperLevel <= 0)
            sizeToApply = BoxSize.Small;
        else if (currentWrapperLevel == 1)
            sizeToApply = BoxSize.Medium;
        else
            sizeToApply = BoxSize.Large;

        for (int i = 0; i < wrappers.Count; i++)
        {
            if (wrappers[i] != null)
                wrappers[i].AVAILABLE_BOX_SIZE = sizeToApply;
        }
    }

    private void RefreshWrapperTextsWithoutModifyingWrapper()
    {
        if (wrapperRefreshSlotsMethod == null)
            CacheReflection();

        if (wrapperRefreshSlotsMethod == null)
            return;

        for (int i = 0; i < wrappers.Count; i++)
        {
            if (wrappers[i] == null)
                continue;

            wrapperRefreshSlotsMethod.Invoke(wrappers[i], null);
        }
    }

    private void ApplyInitialCatalogLevel()
    {
        if (pcMenuController == null || pcCurrentUpgradeLevelField == null)
            return;

        pcCurrentUpgradeLevelField.SetValue(pcMenuController, currentCatalogLevel);

        if (pcDebugModeField != null && pcDebugUpgradeLevelField != null)
        {
            bool debugMode = (bool)pcDebugModeField.GetValue(pcMenuController);
            if (debugMode)
                pcDebugUpgradeLevelField.SetValue(pcMenuController, currentCatalogLevel);
        }
    }

    private void PlayBlockedFeedback(TMP_Text targetText)
    {
        if (targetText == null)
            return;

        CacheTMPState(targetText);

        if (textFeedbackRoutines.TryGetValue(targetText, out Coroutine existingRoutine) && existingRoutine != null)
            StopCoroutine(existingRoutine);

        Coroutine routine = StartCoroutine(BlockedTextFeedbackRoutine(targetText));
        textFeedbackRoutines[targetText] = routine;
    }

    private IEnumerator BlockedTextFeedbackRoutine(TMP_Text text)
    {
        if (text == null)
            yield break;

        RectTransform rect = text.GetComponent<RectTransform>();
        Vector2 basePos = rect != null && originalTextAnchoredPositions.ContainsKey(text)
            ? originalTextAnchoredPositions[text]
            : (rect != null ? rect.anchoredPosition : Vector2.zero);

        Color baseColor = originalTextColors.ContainsKey(text) ? originalTextColors[text] : text.color;

        text.color = blockedTextColor;

        float elapsed = 0f;
        while (elapsed < blockedShakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            if (rect != null)
            {
                Vector2 randomOffset = Random.insideUnitCircle * blockedShakeStrength;
                rect.anchoredPosition = basePos + randomOffset;
            }

            yield return null;
        }

        if (rect != null)
            rect.anchoredPosition = basePos;

        yield return new WaitForSecondsRealtime(blockedRedDuration);

        text.color = baseColor;
        textFeedbackRoutines.Remove(text);
    }

    private void RestoreTextState(TMP_Text text)
    {
        if (text == null)
            return;

        if (originalTextColors.TryGetValue(text, out Color originalColor))
            text.color = originalColor;

        RectTransform rect = text.GetComponent<RectTransform>();
        if (rect != null && originalTextAnchoredPositions.TryGetValue(text, out Vector2 originalPos))
            rect.anchoredPosition = originalPos;
    }

    private void StopAllTextFeedback()
    {
        foreach (KeyValuePair<TMP_Text, Coroutine> pair in textFeedbackRoutines)
        {
            if (pair.Value != null)
                StopCoroutine(pair.Value);

            RestoreTextState(pair.Key);
        }

        textFeedbackRoutines.Clear();
    }

    public void ChooseSpeedUpgrade()
    {
        if (!upgradeUIOpen || isOpening || isClosing)
            return;

        if (!CanApplyUpgrade(UpgradeType.PlayerSpeed))
        {
            PlayBlockedFeedback(speedUpgrade.levelText);
            return;
        }

        ApplyUpgrade(UpgradeType.PlayerSpeed);
    }

    public void ChooseWrapperCapacityUpgrade()
    {
        if (!upgradeUIOpen || isOpening || isClosing)
            return;

        if (!CanApplyUpgrade(UpgradeType.WrapperCapacity))
        {
            PlayBlockedFeedback(wrapperUpgrade.levelText);
            return;
        }

        ApplyUpgrade(UpgradeType.WrapperCapacity);
    }

    public void ChooseCarryCapacityUpgrade()
    {
        if (!upgradeUIOpen || isOpening || isClosing)
            return;

        if (!CanApplyUpgrade(UpgradeType.CarryCapacity))
        {
            PlayBlockedFeedback(carryUpgrade.levelText);
            return;
        }

        ApplyUpgrade(UpgradeType.CarryCapacity);
    }

    public void ChooseCatalogUpgrade()
    {
        if (!upgradeUIOpen || isOpening || isClosing)
            return;

        if (!CanApplyUpgrade(UpgradeType.Catalogs))
        {
            PlayBlockedFeedback(catalogUpgrade.levelText);
            return;
        }

        ApplyUpgrade(UpgradeType.Catalogs);
    }

    public bool IsUpgradeUIOpen()
    {
        return upgradeUIOpen;
    }

    public int GetSpeedLevel() => currentSpeedLevel;
    public int GetWrapperLevel() => currentWrapperLevel;
    public int GetCarryLevel() => currentCarryLevel;
    public int GetCatalogLevel() => currentCatalogLevel;
}