using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class GameAudioEventManager : MonoBehaviour
{
    [Header("Shared Audio Source")]
    [SerializeField] private AudioSource sharedAudioSource;

    [Header("References")]
    [SerializeField] private Player player;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private MonoBehaviour unwrapper;
    [SerializeField] private MonoBehaviour wrapper;
    [SerializeField] private MonoBehaviour sellMachine;
    [SerializeField] private MonoBehaviour quotaManager;
    [SerializeField] private MonoBehaviour pcMenuController;
    [SerializeField] private MonoBehaviour shoppingCartManager;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("PC")]
    [SerializeField] private AudioClip pcOpenClip;
    [SerializeField] private AudioClip pcCloseClip;

    [Header("Unwrapper")]
    [SerializeField] private AudioClip unwrapperIntroClip;
    [SerializeField] private AudioClip unwrapperLoopClip;

    [Header("Wrapper")]
    [SerializeField] private AudioClip wrapperInsertItemClip;
    [SerializeField] private AudioClip wrapperSpawnBoxClip;

    [Header("Sell Machine")]
    [SerializeField] private AudioClip sellMachineSuccessClip;

    [Header("Quota")]
    [SerializeField] private AudioClip quotaInteractClip;
    [SerializeField] private AudioClip quotaHoldLoopClip;
    [SerializeField] private AudioClip quotaReachedClip;

    [Header("Shopping Cart")]
    [SerializeField] private AudioClip addToCartSuccessClip;
    [SerializeField] private AudioClip addToCartErrorClip;
    [SerializeField] private AudioClip purchaseSuccessClip;
    [SerializeField] private AudioClip purchaseFollowupClip;

    [Header("Purchase Success Timing")]
    [SerializeField] private float purchaseToShakeDelay = 0.20f;
    [SerializeField] private float shakeToFollowupDelay = 0.35f;

    [Header("Camera Shake")]
    [SerializeField] private float purchaseShakeDuration = 0.22f;
    [SerializeField] private float purchaseShakeStrength = 0.15f;

    [Header("Volumes")]
    [SerializeField][Range(0f, 1f)] private float oneShotVolume = 1f;
    [SerializeField][Range(0f, 1f)] private float loopVolume = 1f;

    private FieldInfo playerCurrentTargetField;
    private FieldInfo purchaseRunningField;
    private FieldInfo cartField;
    private FieldInfo reservedMoneyField;
    private FieldInfo currentBoxField;
    private FieldInfo rarityRatesField;
    private FieldInfo animationProfilesField;

    private bool lastPCOpenState;
    private bool lastPurchaseRunning;
    private int lastReservedMoney;
    private int lastCartCount;
    private int lastQuotaDeposited;
    private int lastQuotaTarget;
    private int lastObservedDay = -1;
    private bool quotaReachedPlayedThisDay;

    private bool lastUnwrapperBusy;
    private bool lastWrapperBusy;
    private bool lastSellMachineBusy;

    private PendingUnwrapperInfo pendingUnwrapperInfo;
    private PendingWrapperMode pendingWrapperMode = PendingWrapperMode.None;

    private Coroutine unwrapperSequenceRoutine;
    private Coroutine purchaseSequenceRoutine;
    private Coroutine cameraShakeRoutine;

    private bool quotaLoopPlaying = false;

    private bool waitingAddToCartResult = false;
    private int addToCartReservedBefore;
    private int addToCartCartCountBefore;

    private bool waitingPurchaseResult = false;
    private bool purchaseRunningBeforeAttempt;
    private int purchaseCartCountBeforeAttempt;

    private enum PendingWrapperMode
    {
        None,
        InsertItem,
        SpawnBox
    }

    private class PendingUnwrapperInfo
    {
        public Box cachedBox;
        public BoxSize cachedBoxSize;
        public int rolledItemCount;
        public float introDuration;
        public float loopDuration;
    }

    private void Awake()
    {
        if (sharedAudioSource == null)
            sharedAudioSource = GetComponent<AudioSource>();

        if (player == null)
            player = FindFirstObjectByType<Player>();

        if (gameManager == null)
            gameManager = GameManager.instance;

        if (mainCamera == null)
            mainCamera = Camera.main;

        CacheReflection();
    }

    private void Start()
    {
        lastPCOpenState = GetPCIsOpen();
        lastPurchaseRunning = GetPurchaseRunning();
        lastReservedMoney = GetReservedMoney();
        lastCartCount = GetCartCount();
        lastQuotaDeposited = GetQuotaDeposited();
        lastQuotaTarget = GetQuotaTarget();
        lastObservedDay = GetCurrentDay();
        quotaReachedPlayedThisDay = lastQuotaTarget > 0 && lastQuotaDeposited >= lastQuotaTarget;

        lastUnwrapperBusy = GetBusy(unwrapper);
        lastWrapperBusy = GetBusy(wrapper);
        lastSellMachineBusy = GetBusy(sellMachine);
    }

    private void Update()
    {
        DetectInteractionIntents();
        DetectPCOpenClose();
        DetectUnwrapperSuccess();
        DetectWrapperSuccess();
        DetectSellMachineSuccess();
        DetectQuotaHoldLoop();
        DetectQuotaReached();
        DetectDayChange();
        DetectPurchaseRunningStateChange();
        ResolvePendingUIButtonChecks();
    }

    // =========================================================
    // PUBLIC UI BUTTON NOTIFIERS
    // =========================================================

    // Llama este método también en el botón de Add To Cart
    public void NotifyAddToCartAttempt()
    {
        waitingAddToCartResult = true;
        addToCartReservedBefore = GetReservedMoney();
        addToCartCartCountBefore = GetCartCount();
    }

    // Llama este método también en el botón de Purchase
    public void NotifyPurchaseAttempt()
    {
        waitingPurchaseResult = true;
        purchaseRunningBeforeAttempt = GetPurchaseRunning();
        purchaseCartCountBeforeAttempt = GetCartCount();
    }

    // =========================================================
    // DETECTION
    // =========================================================

    private void DetectInteractionIntents()
    {
        if (player == null || !Input.GetKeyDown(interactKey))
            return;

        MonoBehaviour currentTarget = GetPlayerCurrentTarget();
        if (currentTarget == null)
            return;

        if (unwrapper != null && currentTarget == unwrapper)
            CachePendingUnwrapperInfo();

        if (wrapper != null && currentTarget == wrapper)
            CachePendingWrapperMode();

        if (quotaManager != null && currentTarget == quotaManager && CanInteractWithQuota())
            PlayOneShot(quotaInteractClip);
    }

    private void DetectPCOpenClose()
    {
        bool currentOpen = GetPCIsOpen();

        if (currentOpen != lastPCOpenState)
        {
            if (currentOpen)
                PlayOneShot(pcOpenClip);
            else
                PlayOneShot(pcCloseClip);

            lastPCOpenState = currentOpen;
        }
    }

    private void DetectUnwrapperSuccess()
    {
        bool currentBusy = GetBusy(unwrapper);

        if (currentBusy && !lastUnwrapperBusy)
        {
            if (unwrapperSequenceRoutine != null)
                StopCoroutine(unwrapperSequenceRoutine);

            unwrapperSequenceRoutine = StartCoroutine(PlayUnwrapperSequenceRoutine());
        }

        lastUnwrapperBusy = currentBusy;
    }

    private void DetectWrapperSuccess()
    {
        bool currentBusy = GetBusy(wrapper);

        if (currentBusy && !lastWrapperBusy)
        {
            if (pendingWrapperMode == PendingWrapperMode.InsertItem)
                PlayOneShot(wrapperInsertItemClip);
            else if (pendingWrapperMode == PendingWrapperMode.SpawnBox)
                PlayOneShot(wrapperSpawnBoxClip);

            pendingWrapperMode = PendingWrapperMode.None;
        }

        lastWrapperBusy = currentBusy;
    }

    private void DetectSellMachineSuccess()
    {
        bool currentBusy = GetBusy(sellMachine);

        if (currentBusy && !lastSellMachineBusy)
            PlayOneShot(sellMachineSuccessClip);

        lastSellMachineBusy = currentBusy;
    }

    private void DetectQuotaHoldLoop()
    {
        bool shouldLoop = false;

        if (player != null && quotaManager != null)
        {
            MonoBehaviour currentTarget = GetPlayerCurrentTarget();
            shouldLoop =
                currentTarget == quotaManager &&
                Input.GetKey(interactKey) &&
                CanInteractWithQuota();
        }

        if (shouldLoop && !quotaLoopPlaying)
            StartQuotaLoop();

        if (!shouldLoop && quotaLoopPlaying)
            StopQuotaLoop();
    }

    private void DetectQuotaReached()
    {
        int deposited = GetQuotaDeposited();
        int target = GetQuotaTarget();

        if (!quotaReachedPlayedThisDay && target > 0 && deposited >= target)
        {
            quotaReachedPlayedThisDay = true;
            PlayOneShot(quotaReachedClip);
        }

        lastQuotaDeposited = deposited;
        lastQuotaTarget = target;
    }

    private void DetectDayChange()
    {
        int day = GetCurrentDay();
        if (day != lastObservedDay)
        {
            lastObservedDay = day;
            quotaReachedPlayedThisDay = false;
        }
    }

    private void DetectPurchaseRunningStateChange()
    {
        bool currentPurchaseRunning = GetPurchaseRunning();

        if (currentPurchaseRunning && !lastPurchaseRunning)
        {
            if (purchaseSequenceRoutine != null)
                StopCoroutine(purchaseSequenceRoutine);

            purchaseSequenceRoutine = StartCoroutine(PlayPurchaseSuccessSequenceRoutine());
        }

        lastPurchaseRunning = currentPurchaseRunning;
    }

    private void ResolvePendingUIButtonChecks()
    {
        if (waitingAddToCartResult)
        {
            int currentReserved = GetReservedMoney();
            int currentCartCount = GetCartCount();

            if (currentReserved > addToCartReservedBefore || currentCartCount > addToCartCartCountBefore)
            {
                PlayOneShot(addToCartSuccessClip);
                waitingAddToCartResult = false;
            }
            else if (Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.Return) || Input.GetKeyUp(KeyCode.Space))
            {
                PlayOneShot(addToCartErrorClip);
                waitingAddToCartResult = false;
            }
        }

        if (waitingPurchaseResult)
        {
            bool currentRunning = GetPurchaseRunning();
            int currentCartCount = GetCartCount();

            if (currentRunning && !purchaseRunningBeforeAttempt)
            {
                waitingPurchaseResult = false;
            }
            else if ((Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.Return) || Input.GetKeyUp(KeyCode.Space)) && currentCartCount <= 0 && purchaseCartCountBeforeAttempt <= 0)
            {
                PlayOneShot(addToCartErrorClip);
                waitingPurchaseResult = false;
            }
            else if ((Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.Return) || Input.GetKeyUp(KeyCode.Space)) && !currentRunning && purchaseCartCountBeforeAttempt > 0)
            {
                // Si no ha arrancado purchase pese a haberlo intentado, lo tratamos como error
                PlayOneShot(addToCartErrorClip);
                waitingPurchaseResult = false;
            }
        }
    }

    // =========================================================
    // CACHE INTERACTION CONTEXT
    // =========================================================

    private void CachePendingUnwrapperInfo()
    {
        pendingUnwrapperInfo = null;

        if (!player.ReturnHeldBox(out _, out Box heldBox) || heldBox == null)
            return;

        if (heldBox.Type != BoxType.Player && heldBox.Type != BoxType.Delivery)
        {
            // igual dejamos continuar, por si tus cajas son de ambos tipos
        }

        PendingUnwrapperInfo info = new PendingUnwrapperInfo();
        info.cachedBox = heldBox;
        info.cachedBoxSize = heldBox.Size;
        info.rolledItemCount = PredictUnwrapperRolledItemCount(heldBox);
        GetUnwrapperProfileDurations(heldBox.Size, out info.introDuration, out info.loopDuration);

        pendingUnwrapperInfo = info;
    }

    private void CachePendingWrapperMode()
    {
        pendingWrapperMode = PendingWrapperMode.None;

        if (wrapper == null || player == null)
            return;

        if (player.ReturnHeldItem(out _, out _))
        {
            pendingWrapperMode = PendingWrapperMode.InsertItem;
            return;
        }

        GameObject currentBox = GetWrapperCurrentBox();
        if (currentBox != null)
        {
            Box box = currentBox.GetComponent<Box>();
            if (box != null && box.playerItemPool != null && box.playerItemPool.Count > 0)
                pendingWrapperMode = PendingWrapperMode.SpawnBox;
        }
    }

    // =========================================================
    // SEQUENCES
    // =========================================================

    private IEnumerator PlayUnwrapperSequenceRoutine()
    {
        if (pendingUnwrapperInfo == null)
        {
            PlayOneShot(unwrapperIntroClip);
            yield break;
        }

        if (unwrapperIntroClip != null)
            PlayOneShot(unwrapperIntroClip);

        if (pendingUnwrapperInfo.introDuration > 0f)
            yield return new WaitForSeconds(pendingUnwrapperInfo.introDuration);

        int loopCount = Mathf.Max(0, pendingUnwrapperInfo.rolledItemCount);
        for (int i = 0; i < loopCount; i++)
        {
            if (unwrapperLoopClip != null)
                PlayOneShot(unwrapperLoopClip);

            if (pendingUnwrapperInfo.loopDuration > 0f)
                yield return new WaitForSeconds(pendingUnwrapperInfo.loopDuration);
        }

        pendingUnwrapperInfo = null;
    }

    private IEnumerator PlayPurchaseSuccessSequenceRoutine()
    {
        if (purchaseSuccessClip != null)
            PlayOneShot(purchaseSuccessClip);

        if (purchaseToShakeDelay > 0f)
            yield return new WaitForSeconds(purchaseToShakeDelay);

        if (cameraShakeRoutine != null)
            StopCoroutine(cameraShakeRoutine);

        cameraShakeRoutine = StartCoroutine(ShakeMainCameraRoutine());

        if (shakeToFollowupDelay > 0f)
            yield return new WaitForSeconds(shakeToFollowupDelay);

        if (purchaseFollowupClip != null)
            PlayOneShot(purchaseFollowupClip);
    }

    private IEnumerator ShakeMainCameraRoutine()
    {
        if (mainCamera == null)
            yield break;

        Transform cam = mainCamera.transform;
        Vector3 basePos = cam.localPosition;
        float elapsed = 0f;

        while (elapsed < purchaseShakeDuration)
        {
            elapsed += Time.deltaTime;
            Vector2 random = UnityEngine.Random.insideUnitCircle * purchaseShakeStrength;
            cam.localPosition = basePos + new Vector3(random.x, random.y, 0f);
            yield return null;
        }

        cam.localPosition = basePos;
        cameraShakeRoutine = null;
    }

    // =========================================================
    // AUDIO HELPERS
    // =========================================================

    private void PlayOneShot(AudioClip clip)
    {
        if (sharedAudioSource == null || clip == null)
            return;

        sharedAudioSource.PlayOneShot(clip, oneShotVolume);
    }

    private void StartQuotaLoop()
    {
        if (sharedAudioSource == null || quotaHoldLoopClip == null)
            return;

        quotaLoopPlaying = true;
        sharedAudioSource.clip = quotaHoldLoopClip;
        sharedAudioSource.loop = true;
        sharedAudioSource.volume = loopVolume;
        sharedAudioSource.Play();
    }

    private void StopQuotaLoop()
    {
        if (sharedAudioSource == null)
            return;

        if (quotaLoopPlaying && sharedAudioSource.clip == quotaHoldLoopClip)
            sharedAudioSource.Stop();

        sharedAudioSource.loop = false;
        sharedAudioSource.clip = null;
        sharedAudioSource.volume = 1f;
        quotaLoopPlaying = false;
    }

    // =========================================================
    // REFLECTION / DATA ACCESS
    // =========================================================

    private void CacheReflection()
    {
        if (player != null)
            playerCurrentTargetField = typeof(Player).GetField("currentTarget", BindingFlags.Instance | BindingFlags.NonPublic);

        if (typeof(PCShoppingCartManager) != null)
        {
            Type cartManagerType = typeof(PCShoppingCartManager);
            purchaseRunningField = cartManagerType.GetField("purchaseRunning", BindingFlags.Instance | BindingFlags.NonPublic);
            cartField = cartManagerType.GetField("cart", BindingFlags.Instance | BindingFlags.NonPublic);
            reservedMoneyField = cartManagerType.GetField("reservedMoney", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        if (wrapper != null)
            currentBoxField = wrapper.GetType().GetField("currentBox", BindingFlags.Instance | BindingFlags.NonPublic);

        if (unwrapper != null)
        {
            Type t = unwrapper.GetType();
            rarityRatesField = t.GetField("rarityRates", BindingFlags.Instance | BindingFlags.NonPublic);
            animationProfilesField = t.GetField("animationProfiles", BindingFlags.Instance | BindingFlags.NonPublic);
        }
    }

    private MonoBehaviour GetPlayerCurrentTarget()
    {
        if (player == null || playerCurrentTargetField == null)
            return null;

        return playerCurrentTargetField.GetValue(player) as MonoBehaviour;
    }

    private bool GetPCIsOpen()
    {
        if (pcMenuController == null)
            return false;

        PropertyInfo prop = pcMenuController.GetType().GetProperty("IsOpen", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop != null && prop.PropertyType == typeof(bool))
        {
            object value = prop.GetValue(pcMenuController);
            if (value is bool b)
                return b;
        }

        FieldInfo field = pcMenuController.GetType().GetField("isOpen", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null && field.FieldType == typeof(bool))
        {
            object value = field.GetValue(pcMenuController);
            if (value is bool b)
                return b;
        }

        return false;
    }

    private bool GetPurchaseRunning()
    {
        if (shoppingCartManager == null && PCShoppingCartManager.Instance == null)
            return false;

        object target = shoppingCartManager != null ? shoppingCartManager : PCShoppingCartManager.Instance;
        if (target == null || purchaseRunningField == null)
            return false;

        object value = purchaseRunningField.GetValue(target);
        return value is bool b && b;
    }

    private int GetCartCount()
    {
        object target = shoppingCartManager != null ? shoppingCartManager : PCShoppingCartManager.Instance;
        if (target == null || cartField == null)
            return 0;

        object value = cartField.GetValue(target);
        if (value is System.Collections.ICollection collection)
            return collection.Count;

        return 0;
    }

    private int GetReservedMoney()
    {
        object target = shoppingCartManager != null ? shoppingCartManager : PCShoppingCartManager.Instance;
        if (target == null || reservedMoneyField == null)
            return 0;

        object value = reservedMoneyField.GetValue(target);
        return value is int i ? i : 0;
    }

    private bool GetBusy(MonoBehaviour target)
    {
        if (target == null)
            return false;

        Type type = target.GetType();

        FieldInfo busyField = type.GetField("isBusy", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (busyField != null && busyField.FieldType == typeof(bool))
        {
            object value = busyField.GetValue(target);
            if (value is bool b)
                return b;
        }

        PropertyInfo busyProperty = type.GetProperty("IsBusy", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (busyProperty != null && busyProperty.PropertyType == typeof(bool))
        {
            object value = busyProperty.GetValue(target);
            if (value is bool b)
                return b;
        }

        return false;
    }

    private GameObject GetWrapperCurrentBox()
    {
        if (wrapper == null || currentBoxField == null)
            return null;

        return currentBoxField.GetValue(wrapper) as GameObject;
    }

    private bool CanInteractWithQuota()
    {
        if (quotaManager == null || player == null)
            return false;

        if (gameManager == null)
            gameManager = GameManager.instance;

        if (gameManager == null || gameManager.gameStats == null || gameManager.gameStats.money <= 0)
            return false;

        Type type = quotaManager.GetType();

        FieldInfo isBusyField = type.GetField("isBusy", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (isBusyField != null && isBusyField.FieldType == typeof(bool))
        {
            object busyValue = isBusyField.GetValue(quotaManager);
            if (busyValue is bool busy && busy)
                return false;
        }

        MethodInfo lockMethod = type.GetMethod("IsInteractionLocked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (lockMethod != null && lockMethod.ReturnType == typeof(bool))
        {
            object lockedValue = lockMethod.Invoke(quotaManager, null);
            if (lockedValue is bool locked && locked)
                return false;
        }

        return true;
    }

    private int GetQuotaDeposited()
    {
        if (quotaManager == null)
            return 0;

        MethodInfo depositedMethod = quotaManager.GetType().GetMethod("GetCurrentDeposited", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (depositedMethod != null && depositedMethod.ReturnType == typeof(int))
        {
            object value = depositedMethod.Invoke(quotaManager, null);
            if (value is int i)
                return i;
        }

        FieldInfo depositedField =
            quotaManager.GetType().GetField("currentDeposited", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
            quotaManager.GetType().GetField("currentQuotaPaid", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
            quotaManager.GetType().GetField("depositedMoney", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (depositedField != null && depositedField.FieldType == typeof(int))
        {
            object value = depositedField.GetValue(quotaManager);
            if (value is int i)
                return i;
        }

        return 0;
    }

    private int GetQuotaTarget()
    {
        if (quotaManager == null)
            return 0;

        MethodInfo quotaMethod = quotaManager.GetType().GetMethod("GetCurrentQuota", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (quotaMethod != null && quotaMethod.ReturnType == typeof(int))
        {
            object value = quotaMethod.Invoke(quotaManager, null);
            if (value is int i)
                return i;
        }

        FieldInfo quotaField =
            quotaManager.GetType().GetField("currentQuota", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
            quotaManager.GetType().GetField("quotaToPay", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
            quotaManager.GetType().GetField("dailyQuota", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (quotaField != null && quotaField.FieldType == typeof(int))
        {
            object value = quotaField.GetValue(quotaManager);
            if (value is int i)
                return i;
        }

        return 0;
    }

    private int GetCurrentDay()
    {
        if (gameManager == null)
            gameManager = GameManager.instance;

        if (gameManager == null)
            return 0;

        FieldInfo dayField = gameManager.GetType().GetField("currentDay", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (dayField != null && dayField.FieldType == typeof(int))
        {
            object value = dayField.GetValue(gameManager);
            if (value is int i)
                return i;
        }

        return 0;
    }

    private int PredictUnwrapperRolledItemCount(Box box)
    {
        if (box == null || rarityRatesField == null || unwrapper == null)
            return 0;

        object rarityRatesObj = rarityRatesField.GetValue(unwrapper);
        if (rarityRatesObj == null)
            return 0;

        MethodInfo rollMethod = typeof(Box).GetMethod("RollContents", BindingFlags.Instance | BindingFlags.Public);
        if (rollMethod == null)
            return 0;

        try
        {
            object result = rollMethod.Invoke(box, new object[] { rarityRatesObj });
            if (result is System.Collections.ICollection collection)
                return collection.Count;
        }
        catch
        {
            // Ignorado, si falla simplemente no habrá loops predichos
        }

        return 0;
    }

    private void GetUnwrapperProfileDurations(BoxSize targetSize, out float introDuration, out float loopDuration)
    {
        introDuration = 0f;
        loopDuration = 0f;

        if (unwrapper == null || animationProfilesField == null)
            return;

        object profilesObj = animationProfilesField.GetValue(unwrapper);
        if (profilesObj is not System.Collections.IEnumerable enumerable)
            return;

        foreach (object profile in enumerable)
        {
            if (profile == null)
                continue;

            Type profileType = profile.GetType();

            FieldInfo sizeField = profileType.GetField("boxSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            FieldInfo introField = profileType.GetField("introDuration", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            FieldInfo loopField = profileType.GetField("loopDuration", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (sizeField == null)
                continue;

            object sizeValue = sizeField.GetValue(profile);
            if (sizeValue is BoxSize size && size == targetSize)
            {
                if (introField != null)
                {
                    object introValue = introField.GetValue(profile);
                    if (introValue is float fIntro)
                        introDuration = fIntro;
                }

                if (loopField != null)
                {
                    object loopValue = loopField.GetValue(profile);
                    if (loopValue is float fLoop)
                        loopDuration = fLoop;
                }

                return;
            }
        }
    }
}