using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class QuotaHoldStage
{
    [Tooltip("Tiempo mínimo mantenido para entrar en este tramo.")]
    public float holdTimeThreshold = 0f;

    [Tooltip("Cantidad que mete por tick en este tramo.")]
    public int amountPerTick = 1;
}

public class QuotaDepositMachine : Interactable
{
    [Header("Input")]
    [SerializeField] private KeyCode interactHoldKey = KeyCode.E;

    [Header("GameManager")]
    [SerializeField] private GameManager gameManager;

    [Header("Quota By Day")]
    [Tooltip("Cuota del día 0.")]
    [SerializeField] private int startingQuota = 100;
    [Tooltip("Incremento de cuota por cada día.")]
    [SerializeField] private int quotaIncreasePerDay = 50;
    [SerializeField] private bool clampDepositToQuota = true;

    [Header("Deposit Timing")]
    [SerializeField] private float tickInterval = 0.10f;
    [SerializeField]
    private List<QuotaHoldStage> holdStages = new List<QuotaHoldStage>()
    {
        new QuotaHoldStage { holdTimeThreshold = 0f,    amountPerTick = 1   },
        new QuotaHoldStage { holdTimeThreshold = 0.60f, amountPerTick = 2   },
        new QuotaHoldStage { holdTimeThreshold = 1.20f, amountPerTick = 5   },
        new QuotaHoldStage { holdTimeThreshold = 2.00f, amountPerTick = 10  },
        new QuotaHoldStage { holdTimeThreshold = 3.20f, amountPerTick = 100 }
    };

    [Header("Completion / Lock")]
    [SerializeField] private float quotaReachedCooldown = 1.25f;

    [Header("UI Sliders")]
    [SerializeField] private Slider quotaSlider;
    [SerializeField] private Slider currentTimerSlider;

    [Header("UI Texts")]
    [SerializeField] private TMP_Text quotaDebugText;
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private TMP_Text quotaText;

    [Header("Object Animator (briefcase / object)")]
    [SerializeField] private Animator objectAnimator;
    [SerializeField] private string level25Trigger = "briefcaselv1";
    [SerializeField] private string level50Trigger = "briefcaselv2";
    [SerializeField] private string level75Trigger = "briefcaselv3";
    [SerializeField] private string level100Trigger = "briefcaselv4";

    [Header("UI Animator")]
    [SerializeField] private Animator uiAnimator;
    [SerializeField] private string uiTime33Trigger = "enf1";
    [SerializeField] private string uiTime66Trigger = "enf2";
    [SerializeField] private string uiEndTrigger = "enf0";

    private Player activePlayer;
    private bool isHoldingInteraction = false;
    private bool interactionLocked = false;
    private bool dayResolved = false;

    private float heldTime = 0f;
    private float tickTimer = 0f;

    private int currentDeposited = 0;

    private bool triggered25 = false;
    private bool triggered50 = false;
    private bool triggered75 = false;
    private bool triggered100 = false;

    private bool triggeredTime33 = false;
    private bool triggeredTime66 = false;

    private void Awake()
    {
        if (gameManager == null)
            gameManager = GameManager.instance;
    }

    private void Start()
    {
        if (gameManager == null)
            gameManager = GameManager.instance;

        ResetForNewDayVisualAndLogic();
        RefreshAllUI(true);
    }

    private void Update()
    {
        if (gameManager == null)
            gameManager = GameManager.instance;

        if (gameManager == null)
            return;

        UpdateTimerUI();
        UpdateDayAndQuotaTexts();

        CheckTimeThresholdTriggers();
        CheckDayFail();

        if (interactionLocked)
            return;

        if (!isHoldingInteraction)
            return;

        if (activePlayer == null)
        {
            StopHoldingInteraction();
            return;
        }

        if (!Input.GetKey(interactHoldKey))
        {
            StopHoldingInteraction();
            return;
        }

        heldTime += Time.deltaTime;
        tickTimer += Time.deltaTime;

        while (tickTimer >= tickInterval)
        {
            tickTimer -= tickInterval;

            int currentStep = GetCurrentDepositAmount();
            TryDeposit(currentStep);

            if (interactionLocked)
                break;
        }
    }

    public override void Interact(Player player)
    {
        if (interactionLocked)
            return;

        if (dayResolved)
            return;

        if (gameManager == null)
            gameManager = GameManager.instance;

        if (gameManager == null || gameManager.gameStats == null)
        {
            Debug.LogWarning("QuotaDepositMachine: GameManager o GameStats no disponibles.");
            return;
        }

        activePlayer = player;
        isHoldingInteraction = true;
        heldTime = 0f;
        tickTimer = 0f;

        TryDeposit(GetCurrentDepositAmount());
    }

    private void StopHoldingInteraction()
    {
        isHoldingInteraction = false;
        activePlayer = null;
        heldTime = 0f;
        tickTimer = 0f;
    }

    private int GetCurrentDepositAmount()
    {
        if (holdStages == null || holdStages.Count == 0)
            return 1;

        int result = holdStages[0].amountPerTick;

        for (int i = 0; i < holdStages.Count; i++)
        {
            if (heldTime >= holdStages[i].holdTimeThreshold)
                result = Mathf.Max(1, holdStages[i].amountPerTick);
        }

        return result;
    }

    private void TryDeposit(int requestedAmount)
    {
        if (gameManager == null || gameManager.gameStats == null)
            return;

        if (requestedAmount <= 0)
            return;

        int playerMoney = gameManager.gameStats.money;
        if (playerMoney <= 0)
        {
            RefreshAllUI();
            return;
        }

        int currentQuota = GetCurrentQuota();
        int amountToDeposit = requestedAmount;

        if (clampDepositToQuota)
        {
            int remainingQuota = Mathf.Max(0, currentQuota - currentDeposited);
            amountToDeposit = Mathf.Min(amountToDeposit, remainingQuota);
        }

        amountToDeposit = Mathf.Min(amountToDeposit, playerMoney);

        if (amountToDeposit <= 0)
        {
            RefreshAllUI();
            return;
        }

        gameManager.gameStats.money -= amountToDeposit;
        currentDeposited += amountToDeposit;

        Debug.Log($"Dinero insertado / cuota: {currentDeposited}/{currentQuota}");

        RefreshAllUI();
        CheckQuotaThresholds();

        if (currentDeposited >= currentQuota)
            StartCoroutine(QuotaReachedRoutine());
    }

    private void CheckQuotaThresholds()
    {
        float progress = GetQuotaProgress01();

        if (!triggered25 && progress >= 0.25f)
        {
            triggered25 = true;
            TriggerObjectAnimator(level25Trigger);
        }

        if (!triggered50 && progress >= 0.50f)
        {
            triggered50 = true;
            TriggerObjectAnimator(level50Trigger);
        }

        if (!triggered75 && progress >= 0.75f)
        {
            triggered75 = true;
            TriggerObjectAnimator(level75Trigger);
        }

        if (!triggered100 && progress >= 1f)
        {
            triggered100 = true;
            TriggerObjectAnimator(level100Trigger);
        }
    }

    private void CheckTimeThresholdTriggers()
    {
        if (dayResolved)
            return;

        if (gameManager.dayDurationSeconds <= 0f)
            return;

        float normalizedTime = Mathf.Clamp01(gameManager.currentTimer / gameManager.dayDurationSeconds);

        if (!triggeredTime33 && normalizedTime >= 0.33f)
        {
            triggeredTime33 = true;
            TriggerUIAnimator(uiTime33Trigger);
        }

        if (!triggeredTime66 && normalizedTime >= 0.66f)
        {
            triggeredTime66 = true;
            TriggerUIAnimator(uiTime66Trigger);
        }
    }

    private IEnumerator QuotaReachedRoutine()
    {
        if (interactionLocked)
            yield break;

        interactionLocked = true;
        dayResolved = true;
        StopHoldingInteraction();

        TriggerUIAnimator(uiEndTrigger);
        Debug.Log("Elegir mejora");

        yield return new WaitForSeconds(quotaReachedCooldown);

        if (gameManager != null)
            gameManager.AdvanceDay();

        ResetForNewDayVisualAndLogic();
        RefreshAllUI(true);

        interactionLocked = false;
    }

    private void CheckDayFail()
    {
        if (dayResolved)
            return;

        if (interactionLocked)
            return;

        if (gameManager == null)
            return;

        float remaining = gameManager.dayDurationSeconds - gameManager.currentTimer;
        if (remaining > 0f)
            return;

        if (currentDeposited < GetCurrentQuota())
        {
            dayResolved = true;
            interactionLocked = true;
            StopHoldingInteraction();

            TriggerUIAnimator(uiEndTrigger);

            Debug.Log("Game Over");
        }
    }

    private void TriggerObjectAnimator(string triggerName)
    {
        if (objectAnimator == null || string.IsNullOrEmpty(triggerName))
            return;

        objectAnimator.SetTrigger(triggerName);
    }

    private void TriggerUIAnimator(string triggerName)
    {
        if (uiAnimator == null || string.IsNullOrEmpty(triggerName))
            return;

        uiAnimator.SetTrigger(triggerName);
    }

    private float GetQuotaProgress01()
    {
        int quota = GetCurrentQuota();
        if (quota <= 0)
            return 0f;

        return Mathf.Clamp01((float)currentDeposited / quota);
    }

    private void UpdateQuotaSlider(bool forceReset = false)
    {
        if (quotaSlider == null)
            return;

        float progress = forceReset ? 0f : GetQuotaProgress01();

        quotaSlider.minValue = 0f;
        quotaSlider.maxValue = 1f;
        quotaSlider.value = progress;
    }

    private void UpdateTimerUI()
    {
        if (currentTimerSlider == null || gameManager == null)
            return;

        float duration = Mathf.Max(0.0001f, gameManager.dayDurationSeconds);
        float remaining01 = Mathf.Clamp01((duration - gameManager.currentTimer) / duration);

        currentTimerSlider.minValue = 0f;
        currentTimerSlider.maxValue = 1f;
        currentTimerSlider.value = remaining01;
    }

    private void UpdateDayAndQuotaTexts()
    {
        int day = gameManager != null ? gameManager.currentDay : 0;
        int quota = GetCurrentQuota();

        if (dayText != null)
            dayText.text = $"Day: {day}";

        if (quotaText != null)
            quotaText.text = $"Quote: {currentDeposited}/{quota}";

        if (quotaDebugText != null)
            quotaDebugText.text = $"{currentDeposited}/{quota}";
    }

    private void RefreshAllUI(bool forceResetQuotaSlider = false)
    {
        UpdateQuotaSlider(forceResetQuotaSlider);
        UpdateTimerUI();
        UpdateDayAndQuotaTexts();

        int quota = GetCurrentQuota();
        Debug.Log($"Dinero insertado / cuota: {currentDeposited}/{quota}");
    }

    private void ResetThresholdFlags()
    {
        triggered25 = false;
        triggered50 = false;
        triggered75 = false;
        triggered100 = false;

        triggeredTime33 = false;
        triggeredTime66 = false;
    }

    private void ResetForNewDayVisualAndLogic()
    {
        currentDeposited = 0;
        heldTime = 0f;
        tickTimer = 0f;
        dayResolved = false;
        isHoldingInteraction = false;
        activePlayer = null;

        ResetThresholdFlags();
    }

    public int GetCurrentQuota()
    {
        int day = gameManager != null ? gameManager.currentDay : 0;
        return Mathf.Max(1, startingQuota + (day * quotaIncreasePerDay));
    }

    public int GetCurrentDeposited()
    {
        return currentDeposited;
    }

    public float GetRemainingDayTime()
    {
        if (gameManager == null)
            return 0f;

        return Mathf.Max(0f, gameManager.dayDurationSeconds - gameManager.currentTimer);
    }

    public bool IsInteractionLocked()
    {
        return interactionLocked;
    }
}