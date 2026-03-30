using System.Collections;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuotaExtraMenuManager : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private MonoBehaviour quotaManager;

    [Header("Open Button")]
    [SerializeField] private Button openMenuButton;
    [SerializeField] private RectTransform openMenuButtonRect;

    [Header("Menu UI")]
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private RectTransform menuRect;
    [SerializeField] private CanvasGroup menuCanvasGroup;

    [Header("Inner UI Elements")]
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button closeButton;

    [Header("Messages")]
    [TextArea][SerializeField] private string normalOpenMessage = "¿Quieres activar esta opción?";
    [TextArea][SerializeField] private string lowMoneyOpenMessage = "Tu dinero es bajo. Quizá te convenga activar esta opción.";

    [Header("Economy Condition")]
    [SerializeField] private int lowMoneyThreshold = 25;

    [Header("Confirm Action Values")]
    [SerializeField] private int quotaIncreaseAmount = 10;
    [SerializeField] private int moneyIncreaseAmount = 10;

    [Header("Open Juice")]
    [SerializeField] private float openDuration = 0.18f;
    [SerializeField] private float openBounceDuration = 0.10f;
    [SerializeField] private Vector3 openStartScale = new Vector3(1.20f, 0.75f, 1f);
    [SerializeField] private Vector3 openOvershootScale = new Vector3(0.92f, 1.10f, 1f);

    [Header("Close Juice")]
    [SerializeField] private float closeDuration = 0.16f;
    [SerializeField] private Vector3 closeOvershootScale = new Vector3(1.12f, 0.86f, 1f);
    [SerializeField] private Vector3 closeEndScale = new Vector3(0.94f, 0.0f, 1f);

    [Header("Low Money Button Notification Juice")]
    [SerializeField] private float notifyShakeStrength = 5f;
    [SerializeField] private float notifyShakeSpeed = 18f;
    [SerializeField] private float notifyPulseSpeed = 7f;
    [SerializeField] private Vector3 notifyPulseScale = new Vector3(1.08f, 0.94f, 1f);

    private bool menuOpen = false;
    private bool isAnimating = false;

    private Vector3 openButtonBaseScale = Vector3.one;
    private Vector2 openButtonBaseAnchoredPos = Vector2.zero;
    private Vector3 menuBaseScale = Vector3.one;

    private void Awake()
    {
        if (gameManager == null)
            gameManager = GameManager.instance;

        if (openMenuButton != null && openMenuButtonRect == null)
            openMenuButtonRect = openMenuButton.GetComponent<RectTransform>();

        if (menuRoot != null)
        {
            if (menuRect == null)
                menuRect = menuRoot.GetComponent<RectTransform>();

            if (menuCanvasGroup == null)
                menuCanvasGroup = menuRoot.GetComponent<CanvasGroup>();

            if (menuCanvasGroup == null)
                menuCanvasGroup = menuRoot.AddComponent<CanvasGroup>();
        }

        if (openMenuButtonRect != null)
        {
            openButtonBaseScale = openMenuButtonRect.localScale;
            openButtonBaseAnchoredPos = openMenuButtonRect.anchoredPosition;
        }

        if (menuRect != null)
            menuBaseScale = menuRect.localScale;

        if (menuRoot != null)
            menuRoot.SetActive(false);
    }

    private void Update()
    {
        UpdateOpenButtonNotificationJuice();
    }

    // --------------------------------------------------
    // ON CLICK METHODS
    // --------------------------------------------------

    public void OnClickOpenMenu()
    {
        if (isAnimating || menuOpen)
            return;

        if (menuRoot == null)
            return;

        if (messageText != null)
            messageText.text = IsLowMoney() ? lowMoneyOpenMessage : normalOpenMessage;

        StartCoroutine(OpenMenuRoutine());
    }

    public void OnClickConfirmMenu()
    {
        if (isAnimating || !menuOpen)
            return;

        ApplyConfirmEffects();
        StartCoroutine(CloseMenuRoutine());
    }

    public void OnClickCloseMenu()
    {
        if (isAnimating || !menuOpen)
            return;

        StartCoroutine(CloseMenuRoutine());
    }

    // --------------------------------------------------
    // OPEN / CLOSE
    // --------------------------------------------------

    private IEnumerator OpenMenuRoutine()
    {
        isAnimating = true;
        menuOpen = true;

        if (openMenuButton != null)
            openMenuButton.gameObject.SetActive(false);

        menuRoot.SetActive(true);

        SetInnerUIVisible(false);

        if (menuRect != null)
            menuRect.localScale = openStartScale;

        if (menuCanvasGroup != null)
            menuCanvasGroup.alpha = 0f;

        float t = 0f;
        while (t < openDuration)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(t / openDuration);

            if (menuRect != null)
                menuRect.localScale = Vector3.LerpUnclamped(openStartScale, openOvershootScale, EaseOutBack(n));

            if (menuCanvasGroup != null)
                menuCanvasGroup.alpha = Mathf.Lerp(0f, 1f, n);

            yield return null;
        }

        t = 0f;
        while (t < openBounceDuration)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(t / openBounceDuration);

            if (menuRect != null)
                menuRect.localScale = Vector3.LerpUnclamped(openOvershootScale, menuBaseScale, n);

            yield return null;
        }

        if (menuRect != null)
            menuRect.localScale = menuBaseScale;

        if (menuCanvasGroup != null)
            menuCanvasGroup.alpha = 1f;

        // El texto y botones aparecen al final del juice de entrada
        SetInnerUIVisible(true);

        isAnimating = false;
    }

    private IEnumerator CloseMenuRoutine()
    {
        isAnimating = true;

        // El texto y botones desaparecen primero
        SetInnerUIVisible(false);

        Vector3 startScale = menuRect != null ? menuRect.localScale : menuBaseScale;

        float t = 0f;
        while (t < closeDuration * 0.45f)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(t / (closeDuration * 0.45f));

            if (menuRect != null)
                menuRect.localScale = Vector3.LerpUnclamped(startScale, closeOvershootScale, n);

            yield return null;
        }

        t = 0f;
        while (t < closeDuration * 0.55f)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(t / (closeDuration * 0.55f));

            if (menuRect != null)
                menuRect.localScale = Vector3.LerpUnclamped(closeOvershootScale, closeEndScale, n);

            if (menuCanvasGroup != null)
                menuCanvasGroup.alpha = Mathf.Lerp(1f, 0f, n);

            yield return null;
        }

        if (menuCanvasGroup != null)
            menuCanvasGroup.alpha = 1f;

        if (menuRect != null)
            menuRect.localScale = menuBaseScale;

        menuRoot.SetActive(false);
        menuOpen = false;
        isAnimating = false;

        if (openMenuButton != null)
            openMenuButton.gameObject.SetActive(true);
    }

    private void SetInnerUIVisible(bool visible)
    {
        if (messageText != null)
            messageText.gameObject.SetActive(visible);

        if (confirmButton != null)
            confirmButton.gameObject.SetActive(visible);

        if (closeButton != null)
            closeButton.gameObject.SetActive(visible);
    }

    // --------------------------------------------------
    // CONFIRM EFFECTS
    // --------------------------------------------------

    private void ApplyConfirmEffects()
    {
        AddMoneyToManager(moneyIncreaseAmount);
        AddQuotaToQuotaManager(quotaIncreaseAmount);
        RefreshQuotaManagerUI();
    }

    private void AddMoneyToManager(int amount)
    {
        if (gameManager == null || gameManager.gameStats == null)
            return;

        gameManager.gameStats.money += amount;
        gameManager.UpdateMoneyUI();
    }

    private void AddQuotaToQuotaManager(int amount)
    {
        if (quotaManager == null || amount == 0)
            return;

        System.Type type = quotaManager.GetType();

        MethodInfo addMethod =
            type.GetMethod("AddQuotaBonusForCurrentDay", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
            type.GetMethod("AddQuotaBonus", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
            type.GetMethod("IncreaseQuota", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (addMethod != null)
        {
            ParameterInfo[] parameters = addMethod.GetParameters();
            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(int))
            {
                addMethod.Invoke(quotaManager, new object[] { amount });
                return;
            }
        }

        FieldInfo bonusField =
            type.GetField("externalQuotaBonus", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
            type.GetField("currentQuotaBonus", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
            type.GetField("manualQuotaBonus", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (bonusField != null && bonusField.FieldType == typeof(int))
        {
            int current = (int)bonusField.GetValue(quotaManager);
            bonusField.SetValue(quotaManager, current + amount);
            return;
        }

        FieldInfo startingQuotaField =
            type.GetField("startingQuota", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (startingQuotaField != null && startingQuotaField.FieldType == typeof(int))
        {
            int current = (int)startingQuotaField.GetValue(quotaManager);
            startingQuotaField.SetValue(quotaManager, current + amount);
        }
    }

    private void RefreshQuotaManagerUI()
    {
        if (quotaManager == null)
            return;

        System.Type type = quotaManager.GetType();

        MethodInfo refreshAllUIBool =
            type.GetMethod("RefreshAllUI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(bool) }, null);

        if (refreshAllUIBool != null)
        {
            refreshAllUIBool.Invoke(quotaManager, new object[] { false });
            return;
        }

        MethodInfo refreshAllUI =
            type.GetMethod("RefreshAllUI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, System.Type.EmptyTypes, null);

        if (refreshAllUI != null)
        {
            refreshAllUI.Invoke(quotaManager, null);
            return;
        }

        MethodInfo updateQuotaUIBool =
            type.GetMethod("UpdateQuotaUI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(bool) }, null);

        if (updateQuotaUIBool != null)
        {
            updateQuotaUIBool.Invoke(quotaManager, new object[] { false });
            return;
        }

        MethodInfo updateQuotaUI =
            type.GetMethod("UpdateQuotaUI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, System.Type.EmptyTypes, null);

        if (updateQuotaUI != null)
        {
            updateQuotaUI.Invoke(quotaManager, null);
        }
    }

    // --------------------------------------------------
    // LOW MONEY NOTIFICATION
    // --------------------------------------------------

    private void UpdateOpenButtonNotificationJuice()
    {
        if (openMenuButtonRect == null || openMenuButton == null)
            return;

        if (!openMenuButton.gameObject.activeInHierarchy || menuOpen || isAnimating)
        {
            openMenuButtonRect.localScale = openButtonBaseScale;
            openMenuButtonRect.anchoredPosition = openButtonBaseAnchoredPos;
            return;
        }

        if (!IsLowMoney())
        {
            openMenuButtonRect.localScale = Vector3.Lerp(
                openMenuButtonRect.localScale,
                openButtonBaseScale,
                Time.unscaledDeltaTime * 10f
            );

            openMenuButtonRect.anchoredPosition = Vector2.Lerp(
                openMenuButtonRect.anchoredPosition,
                openButtonBaseAnchoredPos,
                Time.unscaledDeltaTime * 10f
            );
            return;
        }

        float shakeX = Mathf.Sin(Time.unscaledTime * notifyShakeSpeed) * notifyShakeStrength;
        float shakeY = Mathf.Cos(Time.unscaledTime * (notifyShakeSpeed * 0.85f)) * (notifyShakeStrength * 0.35f);
        Vector2 shakeOffset = new Vector2(shakeX, shakeY);

        float pulseT = (Mathf.Sin(Time.unscaledTime * notifyPulseSpeed) + 1f) * 0.5f;
        Vector3 pulseScale = Vector3.Lerp(openButtonBaseScale, Vector3.Scale(openButtonBaseScale, notifyPulseScale), pulseT);

        openMenuButtonRect.anchoredPosition = Vector2.Lerp(
            openMenuButtonRect.anchoredPosition,
            openButtonBaseAnchoredPos + shakeOffset,
            Time.unscaledDeltaTime * 14f
        );

        openMenuButtonRect.localScale = Vector3.Lerp(
            openMenuButtonRect.localScale,
            pulseScale,
            Time.unscaledDeltaTime * 12f
        );
    }

    private bool IsLowMoney()
    {
        if (gameManager == null || gameManager.gameStats == null)
            return false;

        return gameManager.gameStats.money < lowMoneyThreshold;
    }

    // --------------------------------------------------
    // EASING
    // --------------------------------------------------

    private float EaseOutBack(float x)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}