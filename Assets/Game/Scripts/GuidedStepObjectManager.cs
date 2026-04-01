using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class GuidedStepObjectManager : MonoBehaviour
{
    [Header("Step Objects")]
    [Tooltip("0=antes de purchase, 1=antes de unwrapper, 2=antes de wrapper, 3=antes de sell machine, 4=antes de quota")]
    [SerializeField] private List<GameObject> stepObjects = new List<GameObject>();

    [Header("References")]
    [SerializeField] private MonoBehaviour unwrapper;
    [SerializeField] private MonoBehaviour wrapper;
    [SerializeField] private MonoBehaviour sellMachine;
    [SerializeField] private MonoBehaviour quotaManager;

    [Header("Polling")]
    [SerializeField] private float checkInterval = 0.05f;

    [Header("Debug")]
    [SerializeField] private bool logTransitions = false;

    private int currentStep = 0;
    private bool finished = false;

    private bool lastPurchaseRunning = false;
    private bool lastUnwrapperBusy = false;
    private bool lastWrapperBusy = false;
    private bool lastSellMachineBusy = false;
    private int lastQuotaDeposited = 0;

    private FieldInfo purchaseRunningField;

    private FieldInfo unwrapperBusyField;
    private PropertyInfo unwrapperBusyProperty;

    private FieldInfo wrapperBusyField;
    private PropertyInfo wrapperBusyProperty;

    private FieldInfo sellMachineBusyField;
    private PropertyInfo sellMachineBusyProperty;

    private MethodInfo quotaDepositedMethod;
    private FieldInfo quotaDepositedField;

    private void Awake()
    {
        CacheReflection();
        ForceInitialState();
    }

    private void Start()
    {
        lastPurchaseRunning = GetPurchaseRunning();
        lastUnwrapperBusy = GetBusy(unwrapper, unwrapperBusyField, unwrapperBusyProperty);
        lastWrapperBusy = GetBusy(wrapper, wrapperBusyField, wrapperBusyProperty);
        lastSellMachineBusy = GetBusy(sellMachine, sellMachineBusyField, sellMachineBusyProperty);
        lastQuotaDeposited = GetQuotaDeposited();

        StartCoroutine(CheckRoutine());
    }

    private void CacheReflection()
    {
        System.Type cartType = typeof(PCShoppingCartManager);
        purchaseRunningField = cartType.GetField("purchaseRunning", BindingFlags.Instance | BindingFlags.NonPublic);

        CacheBusyMembers(unwrapper, out unwrapperBusyField, out unwrapperBusyProperty);
        CacheBusyMembers(wrapper, out wrapperBusyField, out wrapperBusyProperty);
        CacheBusyMembers(sellMachine, out sellMachineBusyField, out sellMachineBusyProperty);

        if (quotaManager != null)
        {
            System.Type quotaType = quotaManager.GetType();
            quotaDepositedMethod = quotaType.GetMethod("GetCurrentDeposited", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            quotaDepositedField =
                quotaType.GetField("currentDeposited", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                quotaType.GetField("currentQuotaPaid", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                quotaType.GetField("depositedMoney", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }
    }

    private void CacheBusyMembers(MonoBehaviour target, out FieldInfo busyField, out PropertyInfo busyProperty)
    {
        busyField = null;
        busyProperty = null;

        if (target == null)
            return;

        System.Type type = target.GetType();
        busyField = type.GetField("isBusy", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        busyProperty = type.GetProperty("IsBusy", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    }

    private void ForceInitialState()
    {
        for (int i = 0; i < stepObjects.Count; i++)
        {
            if (stepObjects[i] != null)
                stepObjects[i].SetActive(i == 0);
        }

        currentStep = 0;
        finished = false;
    }

    private IEnumerator CheckRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(checkInterval);

        while (!finished)
        {
            CheckCurrentStep();
            yield return wait;
        }
    }

    private void CheckCurrentStep()
    {
        switch (currentStep)
        {
            case 0:
                CheckPurchaseStep();
                break;

            case 1:
                CheckUnwrapperStep();
                break;

            case 2:
                CheckWrapperStep();
                break;

            case 3:
                CheckSellMachineStep();
                break;

            case 4:
                CheckQuotaStep();
                break;
        }
    }

    private void CheckPurchaseStep()
    {
        bool currentPurchaseRunning = GetPurchaseRunning();

        if (currentPurchaseRunning && !lastPurchaseRunning)
        {
            AdvanceToStep(1, "Purchase first success");
        }

        lastPurchaseRunning = currentPurchaseRunning;
    }

    private void CheckUnwrapperStep()
    {
        bool currentBusy = GetBusy(unwrapper, unwrapperBusyField, unwrapperBusyProperty);

        if (currentBusy && !lastUnwrapperBusy)
        {
            AdvanceToStep(2, "Unwrapper first effective interaction");
        }

        lastUnwrapperBusy = currentBusy;
    }

    private void CheckWrapperStep()
    {
        bool currentBusy = GetBusy(wrapper, wrapperBusyField, wrapperBusyProperty);

        if (currentBusy && !lastWrapperBusy)
        {
            AdvanceToStep(3, "Wrapper first effective interaction");
        }

        lastWrapperBusy = currentBusy;
    }

    private void CheckSellMachineStep()
    {
        bool currentBusy = GetBusy(sellMachine, sellMachineBusyField, sellMachineBusyProperty);

        if (currentBusy && !lastSellMachineBusy)
        {
            AdvanceToStep(4, "SellMachine first effective interaction");
        }

        lastSellMachineBusy = currentBusy;
    }

    private void CheckQuotaStep()
    {
        int currentDeposited = GetQuotaDeposited();

        if (currentDeposited > lastQuotaDeposited)
        {
            CompleteSequence("Quota first effective interaction");
        }

        lastQuotaDeposited = currentDeposited;
    }

    private bool GetPurchaseRunning()
    {
        if (PCShoppingCartManager.Instance == null || purchaseRunningField == null)
            return false;

        object value = purchaseRunningField.GetValue(PCShoppingCartManager.Instance);
        return value is bool b && b;
    }

    private bool GetBusy(MonoBehaviour target, FieldInfo busyField, PropertyInfo busyProperty)
    {
        if (target == null)
            return false;

        if (busyField != null && busyField.FieldType == typeof(bool))
        {
            object value = busyField.GetValue(target);
            if (value is bool b)
                return b;
        }

        if (busyProperty != null && busyProperty.PropertyType == typeof(bool) && busyProperty.CanRead)
        {
            object value = busyProperty.GetValue(target);
            if (value is bool b)
                return b;
        }

        return false;
    }

    private int GetQuotaDeposited()
    {
        if (quotaManager == null)
            return 0;

        if (quotaDepositedMethod != null && quotaDepositedMethod.ReturnType == typeof(int))
        {
            object value = quotaDepositedMethod.Invoke(quotaManager, null);
            if (value is int intValue)
                return intValue;
        }

        if (quotaDepositedField != null && quotaDepositedField.FieldType == typeof(int))
        {
            object value = quotaDepositedField.GetValue(quotaManager);
            if (value is int intValue)
                return intValue;
        }

        return 0;
    }

    private void AdvanceToStep(int nextStep, string reason)
    {
        if (logTransitions)
            Debug.Log($"GuidedStepObjectManager -> {reason}");

        int previousIndex = currentStep;
        int nextIndex = nextStep;

        if (previousIndex >= 0 && previousIndex < stepObjects.Count && stepObjects[previousIndex] != null)
            stepObjects[previousIndex].SetActive(false);

        if (nextIndex >= 0 && nextIndex < stepObjects.Count && stepObjects[nextIndex] != null)
            stepObjects[nextIndex].SetActive(true);

        currentStep = nextStep;
    }

    private void CompleteSequence(string reason)
    {
        if (logTransitions)
            Debug.Log($"GuidedStepObjectManager -> {reason} -> sequence complete");

        if (currentStep >= 0 && currentStep < stepObjects.Count && stepObjects[currentStep] != null)
            stepObjects[currentStep].SetActive(false);

        finished = true;
    }

    [ContextMenu("Reset Sequence")]
    public void ResetSequence()
    {
        StopAllCoroutines();
        ForceInitialState();

        lastPurchaseRunning = GetPurchaseRunning();
        lastUnwrapperBusy = GetBusy(unwrapper, unwrapperBusyField, unwrapperBusyProperty);
        lastWrapperBusy = GetBusy(wrapper, wrapperBusyField, wrapperBusyProperty);
        lastSellMachineBusy = GetBusy(sellMachine, sellMachineBusyField, sellMachineBusyProperty);
        lastQuotaDeposited = GetQuotaDeposited();

        StartCoroutine(CheckRoutine());
    }
}