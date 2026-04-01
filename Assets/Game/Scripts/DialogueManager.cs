using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using TMPro;
using UnityEngine;

public enum DialogueTriggerType
{
    Tutorial,

    PC_FirstCorrect,
    PC_FirstWrong,
    PC_RepeatWrong,

    Wrapper_FirstCorrect,
    Wrapper_FirstWrong,
    Wrapper_RepeatWrong,

    Unwrapper_FirstCorrect,
    Unwrapper_FirstWrong,
    Unwrapper_RepeatWrong,

    SellMachine_FirstCorrect,
    SellMachine_FirstWrong,
    SellMachine_RepeatWrong,

    Quota_FirstCorrect,
    Quota_FirstWrong,
    Quota_RepeatWrong,

    BuyBox,

    Time_Remaining_66,
    Time_Remaining_33,

    Quota_25,
    Quota_50,
    Quota_75
}

[Serializable]
public class DialogueEntry
{
    public string id;

    [Header("Trigger")]
    public DialogueTriggerType triggerType = DialogueTriggerType.Tutorial;

    [TextArea(3, 8)]
    public string phrase;

    [Header("Playback")]
    [Min(0.001f)] public float secondsPerCharacter = 0.03f;
    [Min(0f)] public float holdCompletedTextTime = 1.2f;
}

[Serializable]
public class DialogueSequenceState
{
    public DialogueTriggerType triggerType;
    public int nextIndex;
    public bool restartFromBeginning;

    public DialogueSequenceState(DialogueTriggerType type, int index, bool restart)
    {
        triggerType = type;
        nextIndex = index;
        restartFromBeginning = restart;
    }
}

public class DialogueManager : MonoBehaviour
{
    [Header("UI Root")]
    [SerializeField] private GameObject dialogueRoot;
    [SerializeField] private CanvasGroup dialogueCanvasGroup;

    [Header("Bubble")]
    [SerializeField] private RectTransform bubbleRect;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Bubble Open/Close Juice")]
    [SerializeField] private float openFadeDuration = 0.16f;
    [SerializeField] private float closeFadeDuration = 0.18f;
    [SerializeField] private Vector3 openStartScale = new Vector3(0.94f, 0.82f, 1f);
    [SerializeField] private Vector3 openOvershootScale = new Vector3(1.03f, 1.07f, 1f);
    [SerializeField] private float openSettleDuration = 0.10f;

    [Header("Talking Bubble Juice")]
    [SerializeField] private bool useTalkingJuice = true;
    [SerializeField] private float talkPosAmplitude = 4f;
    [SerializeField] private float talkPosSpeed = 2.25f;
    [SerializeField] private float talkScaleXMultiplier = 0.985f;
    [SerializeField] private float talkScaleYMultiplier = 1.02f;
    [SerializeField] private float talkScaleSpeed = 2.1f;
    [SerializeField] private float talkRotationMin = -15f;
    [SerializeField] private float talkRotationMax = 15f;
    [SerializeField] private float talkRotationSpeed = 1.8f;

    [Header("Visible Text Paging")]
    [SerializeField][Min(5)] private int maxVisibleCharactersPerChunk = 50;

    [Header("Dialogue Database")]
    [SerializeField] private List<DialogueEntry> dialogueEntries = new List<DialogueEntry>();

    [Header("References")]
    [SerializeField] private Player player;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private MonoBehaviour quotaManager;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private readonly Dictionary<DialogueTriggerType, List<DialogueEntry>> groupedDialogues = new();
    private readonly Dictionary<DialogueTriggerType, bool> firstCorrectPlayed = new();
    private readonly Dictionary<DialogueTriggerType, bool> firstWrongPlayed = new();

    private readonly Queue<DialogueSequenceState> pendingQueue = new();

    private DialogueSequenceState currentSequence;
    private Coroutine dialogueRoutine;

    private bool isOpen = false;
    private bool isPlaying = false;

    private Vector3 bubbleBaseScale = Vector3.one;
    private Vector2 bubbleBaseAnchoredPos = Vector2.zero;
    private Quaternion bubbleBaseRotation = Quaternion.identity;

    private bool triggeredTime66 = false;
    private bool triggeredTime33 = false;
    private bool triggeredQuota25 = false;
    private bool triggeredQuota50 = false;
    private bool triggeredQuota75 = false;

    private int lastObservedDay = -1;
    private bool lastObservedPurchaseRunning = false;
    private bool firstBuyBoxPlayed = false;

    private FieldInfo playerCurrentTargetField;

    private void Awake()
    {
        if (dialogueRoot != null && dialogueCanvasGroup == null)
            dialogueCanvasGroup = dialogueRoot.GetComponent<CanvasGroup>();

        if (dialogueCanvasGroup == null && dialogueRoot != null)
            dialogueCanvasGroup = dialogueRoot.AddComponent<CanvasGroup>();

        if (bubbleRect != null)
        {
            bubbleBaseScale = bubbleRect.localScale;
            bubbleBaseAnchoredPos = bubbleRect.anchoredPosition;
            bubbleBaseRotation = bubbleRect.localRotation;
        }

        if (gameManager == null)
            gameManager = GameManager.instance;

        if (player == null)
            player = FindFirstObjectByType<Player>();

        playerCurrentTargetField = typeof(Player).GetField("currentTarget", BindingFlags.Instance | BindingFlags.NonPublic);

        BuildDatabase();
        ResetDayThresholds();

        if (dialogueRoot != null)
            dialogueRoot.SetActive(false);
    }

    private void Start()
    {
        if (gameManager == null)
            gameManager = GameManager.instance;

        lastObservedDay = gameManager != null ? gameManager.currentDay : 0;
        lastObservedPurchaseRunning = GetPurchaseRunningState();

        TriggerDialogue(DialogueTriggerType.Tutorial, true);
    }

    private void Update()
    {
        UpdateTalkingJuice();
        AutoWatchQuotaAndTime();
        DetectDayChangeForThresholdReset();
        DetectInteractionTriggers();
        DetectBuyBoxTrigger();
    }

    // =========================================================
    // PUBLIC TRIGGERS
    // =========================================================

    public void TriggerDialogue(DialogueTriggerType triggerType, bool interruptIfPlaying = true)
    {
        if (!groupedDialogues.TryGetValue(triggerType, out List<DialogueEntry> list) || list.Count == 0)
            return;

        DialogueSequenceState newState = new DialogueSequenceState(triggerType, 0, true);

        if (!isPlaying)
        {
            StartSequence(newState);
            return;
        }

        if (!interruptIfPlaying)
        {
            pendingQueue.Enqueue(newState);
            return;
        }

        if (currentSequence != null && currentSequence.nextIndex < GetSequenceCount(currentSequence.triggerType))
            pendingQueue.Enqueue(new DialogueSequenceState(currentSequence.triggerType, currentSequence.nextIndex, false));

        Queue<DialogueSequenceState> rebuilt = new Queue<DialogueSequenceState>();
        rebuilt.Enqueue(newState);

        while (pendingQueue.Count > 0)
            rebuilt.Enqueue(pendingQueue.Dequeue());

        while (rebuilt.Count > 0)
            pendingQueue.Enqueue(rebuilt.Dequeue());

        StopCurrentAndContinueWithQueue();
    }

    public void TriggerPCFirstCorrect() => TriggerFirstCorrect(DialogueTriggerType.PC_FirstCorrect);
    public void TriggerPCFirstWrong() => TriggerFirstWrong(DialogueTriggerType.PC_FirstWrong, DialogueTriggerType.PC_RepeatWrong);
    public void TriggerPCRepeatWrong() => TriggerDialogue(DialogueTriggerType.PC_RepeatWrong, true);

    public void TriggerWrapperFirstCorrect() => TriggerFirstCorrect(DialogueTriggerType.Wrapper_FirstCorrect);
    public void TriggerWrapperFirstWrong() => TriggerFirstWrong(DialogueTriggerType.Wrapper_FirstWrong, DialogueTriggerType.Wrapper_RepeatWrong);
    public void TriggerWrapperRepeatWrong() => TriggerDialogue(DialogueTriggerType.Wrapper_RepeatWrong, true);

    public void TriggerUnwrapperFirstCorrect() => TriggerFirstCorrect(DialogueTriggerType.Unwrapper_FirstCorrect);
    public void TriggerUnwrapperFirstWrong() => TriggerFirstWrong(DialogueTriggerType.Unwrapper_FirstWrong, DialogueTriggerType.Unwrapper_RepeatWrong);
    public void TriggerUnwrapperRepeatWrong() => TriggerDialogue(DialogueTriggerType.Unwrapper_RepeatWrong, true);

    public void TriggerSellMachineFirstCorrect() => TriggerFirstCorrect(DialogueTriggerType.SellMachine_FirstCorrect);
    public void TriggerSellMachineFirstWrong() => TriggerFirstWrong(DialogueTriggerType.SellMachine_FirstWrong, DialogueTriggerType.SellMachine_RepeatWrong);
    public void TriggerSellMachineRepeatWrong() => TriggerDialogue(DialogueTriggerType.SellMachine_RepeatWrong, true);

    public void TriggerQuotaFirstCorrect() => TriggerFirstCorrect(DialogueTriggerType.Quota_FirstCorrect);
    public void TriggerQuotaFirstWrong() => TriggerFirstWrong(DialogueTriggerType.Quota_FirstWrong, DialogueTriggerType.Quota_RepeatWrong);
    public void TriggerQuotaRepeatWrong() => TriggerDialogue(DialogueTriggerType.Quota_RepeatWrong, true);

    public void TriggerBuyBox() => TriggerDialogue(DialogueTriggerType.BuyBox, true);

    // =========================================================
    // CORE
    // =========================================================

    private void BuildDatabase()
    {
        groupedDialogues.Clear();

        for (int i = 0; i < dialogueEntries.Count; i++)
        {
            DialogueEntry entry = dialogueEntries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.phrase))
                continue;

            if (!groupedDialogues.ContainsKey(entry.triggerType))
                groupedDialogues.Add(entry.triggerType, new List<DialogueEntry>());

            groupedDialogues[entry.triggerType].Add(entry);
        }
    }

    private void TriggerFirstCorrect(DialogueTriggerType firstCorrectType)
    {
        if (!firstCorrectPlayed.ContainsKey(firstCorrectType))
            firstCorrectPlayed[firstCorrectType] = false;

        if (!firstCorrectPlayed[firstCorrectType])
        {
            firstCorrectPlayed[firstCorrectType] = true;
            TriggerDialogue(firstCorrectType, true);
        }
    }

    private void TriggerFirstWrong(DialogueTriggerType firstWrongType, DialogueTriggerType repeatWrongType)
    {
        if (!firstWrongPlayed.ContainsKey(firstWrongType))
            firstWrongPlayed[firstWrongType] = false;

        if (!firstWrongPlayed[firstWrongType])
        {
            firstWrongPlayed[firstWrongType] = true;
            TriggerDialogue(firstWrongType, true);
        }
        else
        {
            TriggerDialogue(repeatWrongType, true);
        }
    }

    private void StartSequence(DialogueSequenceState sequence)
    {
        currentSequence = sequence;

        if (dialogueRoutine != null)
            StopCoroutine(dialogueRoutine);

        dialogueRoutine = StartCoroutine(PlaySequenceRoutine(sequence));
    }

    private IEnumerator PlaySequenceRoutine(DialogueSequenceState sequence)
    {
        isPlaying = true;

        if (!isOpen)
            yield return StartCoroutine(OpenDialogueUIRoutine());

        if (!groupedDialogues.TryGetValue(sequence.triggerType, out List<DialogueEntry> list) || list.Count == 0)
        {
            isPlaying = false;
            yield return StartCoroutine(HandleNextOrCloseRoutine());
            yield break;
        }

        int startIndex = sequence.restartFromBeginning ? 0 : Mathf.Clamp(sequence.nextIndex, 0, list.Count - 1);

        for (int i = startIndex; i < list.Count; i++)
        {
            currentSequence.nextIndex = i + 1;
            yield return StartCoroutine(PlayEntryRoutine(list[i]));
        }

        isPlaying = false;
        currentSequence = null;

        yield return StartCoroutine(HandleNextOrCloseRoutine());
    }

    private IEnumerator PlayEntryRoutine(DialogueEntry entry)
    {
        if (entry == null || dialogueText == null)
            yield break;

        List<string> chunks = SplitPhraseIntoChunks(entry.phrase, maxVisibleCharactersPerChunk);

        for (int c = 0; c < chunks.Count; c++)
        {
            string chunk = chunks[c];
            dialogueText.text = string.Empty;

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < chunk.Length; i++)
            {
                sb.Append(chunk[i]);
                dialogueText.text = sb.ToString();

                yield return new WaitForSecondsRealtime(Mathf.Max(0.001f, entry.secondsPerCharacter));
            }

            if (entry.holdCompletedTextTime > 0f)
                yield return new WaitForSecondsRealtime(entry.holdCompletedTextTime);
        }
    }

    private IEnumerator HandleNextOrCloseRoutine()
    {
        if (pendingQueue.Count > 0)
        {
            DialogueSequenceState next = pendingQueue.Dequeue();
            StartSequence(next);
            yield break;
        }

        yield return StartCoroutine(CloseDialogueUIRoutine());
    }

    private void StopCurrentAndContinueWithQueue()
    {
        if (dialogueRoutine != null)
            StopCoroutine(dialogueRoutine);

        dialogueRoutine = null;
        isPlaying = false;
        currentSequence = null;

        if (pendingQueue.Count > 0)
        {
            DialogueSequenceState next = pendingQueue.Dequeue();
            StartSequence(next);
        }
        else
        {
            StartCoroutine(CloseDialogueUIRoutine());
        }
    }

    private int GetSequenceCount(DialogueTriggerType type)
    {
        if (!groupedDialogues.TryGetValue(type, out List<DialogueEntry> list))
            return 0;

        return list.Count;
    }

    // =========================================================
    // UI OPEN / CLOSE
    // =========================================================

    private IEnumerator OpenDialogueUIRoutine()
    {
        if (dialogueRoot == null || bubbleRect == null)
            yield break;

        isOpen = true;
        dialogueRoot.SetActive(true);

        if (dialogueCanvasGroup != null)
            dialogueCanvasGroup.alpha = 0f;

        bubbleRect.localScale = openStartScale;
        bubbleRect.anchoredPosition = bubbleBaseAnchoredPos;
        bubbleRect.localRotation = bubbleBaseRotation;

        float t = 0f;
        while (t < openFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(t / openFadeDuration);

            if (dialogueCanvasGroup != null)
                dialogueCanvasGroup.alpha = Mathf.Lerp(0f, 1f, n);

            bubbleRect.localScale = Vector3.LerpUnclamped(openStartScale, openOvershootScale, EaseOutBack(n));
            yield return null;
        }

        t = 0f;
        while (t < openSettleDuration)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(t / openSettleDuration);
            bubbleRect.localScale = Vector3.LerpUnclamped(openOvershootScale, bubbleBaseScale, n);
            yield return null;
        }

        bubbleRect.localScale = bubbleBaseScale;

        if (dialogueCanvasGroup != null)
            dialogueCanvasGroup.alpha = 1f;
    }

    private IEnumerator CloseDialogueUIRoutine()
    {
        if (!isOpen || dialogueRoot == null || bubbleRect == null)
            yield break;

        isOpen = false;

        float t = 0f;
        Vector3 startScale = bubbleRect.localScale;
        Vector3 endScale = new Vector3(startScale.x * 1.04f, startScale.y * 0.94f, startScale.z);

        while (t < closeFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(t / closeFadeDuration);

            if (dialogueCanvasGroup != null)
                dialogueCanvasGroup.alpha = Mathf.Lerp(1f, 0f, n);

            bubbleRect.localScale = Vector3.Lerp(startScale, endScale, n);
            yield return null;
        }

        if (dialogueText != null)
            dialogueText.text = string.Empty;

        bubbleRect.localScale = bubbleBaseScale;
        bubbleRect.anchoredPosition = bubbleBaseAnchoredPos;
        bubbleRect.localRotation = bubbleBaseRotation;

        if (dialogueCanvasGroup != null)
            dialogueCanvasGroup.alpha = 1f;

        dialogueRoot.SetActive(false);
    }

    // =========================================================
    // TALKING JUICE
    // =========================================================

    private void UpdateTalkingJuice()
    {
        if (!useTalkingJuice || !isOpen || !isPlaying || bubbleRect == null)
            return;

        float posWave = Mathf.Sin(Time.unscaledTime * talkPosSpeed) * talkPosAmplitude;
        float scaleWave = Mathf.Sin(Time.unscaledTime * talkScaleSpeed);
        float rotWave = Mathf.Sin(Time.unscaledTime * talkRotationSpeed);

        float rotZ = Mathf.Lerp(talkRotationMin, talkRotationMax, (rotWave + 1f) * 0.5f);

        bubbleRect.anchoredPosition = bubbleBaseAnchoredPos + new Vector2(0f, posWave);

        Vector3 targetScale = new Vector3(
            bubbleBaseScale.x * Mathf.Lerp(1f, talkScaleXMultiplier, (scaleWave + 1f) * 0.5f),
            bubbleBaseScale.y * Mathf.Lerp(1f, talkScaleYMultiplier, (scaleWave + 1f) * 0.5f),
            bubbleBaseScale.z
        );

        bubbleRect.localScale = Vector3.Lerp(bubbleRect.localScale, targetScale, Time.unscaledDeltaTime * 6f);
        bubbleRect.localRotation = Quaternion.Lerp(
            bubbleRect.localRotation,
            Quaternion.Euler(0f, 0f, rotZ),
            Time.unscaledDeltaTime * 4f
        );
    }

    // =========================================================
    // AUTO WATCHERS
    // =========================================================

    private void AutoWatchQuotaAndTime()
    {
        if (gameManager == null || quotaManager == null)
            return;

        float dayDuration = gameManager.dayDurationSeconds;
        if (dayDuration > 0.001f)
        {
            float remaining01 = Mathf.Clamp01((dayDuration - gameManager.currentTimer) / dayDuration);

            if (!triggeredTime66 && remaining01 <= 0.66f)
            {
                triggeredTime66 = true;
                TriggerDialogue(DialogueTriggerType.Time_Remaining_66, true);
            }

            if (!triggeredTime33 && remaining01 <= 0.33f)
            {
                triggeredTime33 = true;
                TriggerDialogue(DialogueTriggerType.Time_Remaining_33, true);
            }
        }

        MethodInfo depositedMethod = quotaManager.GetType().GetMethod("GetCurrentDeposited");
        MethodInfo quotaMethod = quotaManager.GetType().GetMethod("GetCurrentQuota");

        if (depositedMethod != null && quotaMethod != null)
        {
            int deposited = (int)depositedMethod.Invoke(quotaManager, null);
            int quota = (int)quotaMethod.Invoke(quotaManager, null);

            if (quota > 0)
            {
                float progress = Mathf.Clamp01((float)deposited / quota);

                if (!triggeredQuota25 && progress >= 0.25f)
                {
                    triggeredQuota25 = true;
                    TriggerDialogue(DialogueTriggerType.Quota_25, true);
                }

                if (!triggeredQuota50 && progress >= 0.50f)
                {
                    triggeredQuota50 = true;
                    TriggerDialogue(DialogueTriggerType.Quota_50, true);
                }

                if (!triggeredQuota75 && progress >= 0.75f)
                {
                    triggeredQuota75 = true;
                    TriggerDialogue(DialogueTriggerType.Quota_75, true);
                }
            }
        }
    }

    private void DetectDayChangeForThresholdReset()
    {
        if (gameManager == null)
            return;

        if (gameManager.currentDay != lastObservedDay)
        {
            lastObservedDay = gameManager.currentDay;
            ResetDayThresholds();
        }
    }

    private void ResetDayThresholds()
    {
        triggeredTime66 = false;
        triggeredTime33 = false;
        triggeredQuota25 = false;
        triggeredQuota50 = false;
        triggeredQuota75 = false;
    }

    // =========================================================
    // AUTO INTERACTION DETECTION
    // =========================================================

    private void DetectInteractionTriggers()
    {
        if (player == null || playerCurrentTargetField == null)
            return;

        if (!Input.GetKeyDown(interactKey))
            return;

        object targetObj = playerCurrentTargetField.GetValue(player);
        if (targetObj == null)
            return;

        MonoBehaviour targetBehaviour = targetObj as MonoBehaviour;
        if (targetBehaviour == null)
            return;

        string typeName = targetBehaviour.GetType().Name;
        bool isInteractableNow = EvaluateTargetInteractableNow(targetBehaviour);

        switch (typeName)
        {
            case "Wrapper":
                if (isInteractableNow)
                    TriggerWrapperFirstCorrect();
                else
                    TriggerWrapperFirstWrong();
                break;

            case "Unwrapper":
                if (isInteractableNow)
                    TriggerUnwrapperFirstCorrect();
                else
                    TriggerUnwrapperFirstWrong();
                break;

            case "SellMachine":
                if (isInteractableNow)
                    TriggerSellMachineFirstCorrect();
                else
                    TriggerSellMachineFirstWrong();
                break;

            case "QuotaDepositMachine":
                if (isInteractableNow)
                    TriggerQuotaFirstCorrect();
                else
                    TriggerQuotaFirstWrong();
                break;

            default:
                if (typeName.Contains("PC"))
                {
                    if (isInteractableNow)
                        TriggerPCFirstCorrect();
                    else
                        TriggerPCFirstWrong();
                }
                break;
        }
    }

    private bool EvaluateTargetInteractableNow(MonoBehaviour targetBehaviour)
    {
        if (targetBehaviour == null || player == null)
            return false;

        string typeName = targetBehaviour.GetType().Name;

        if (IsBusyOrLocked(targetBehaviour))
            return false;

        if (typeName == "Wrapper")
        {
            bool holdsItem = player.ReturnHeldItem(out _, out _);
            bool wrapperHasPreparedBox = WrapperHasPreparedBox(targetBehaviour);
            return holdsItem || wrapperHasPreparedBox;
        }

        if (typeName == "Unwrapper")
        {
            return player.ReturnHeldBox(out _, out _);
        }

        if (typeName == "SellMachine")
        {
            if (!player.ReturnHeldBox(out _, out Box heldBox) || heldBox == null)
                return false;

            return heldBox.Type == BoxType.Player;
        }

        if (typeName == "QuotaDepositMachine")
        {
            if (GameManager.instance == null || GameManager.instance.gameStats == null)
                return false;

            if (GameManager.instance.gameStats.money <= 0)
                return false;

            return !IsBusyOrLocked(targetBehaviour);
        }

        if (typeName.Contains("PC"))
        {
            if (PCShoppingCartManager.IsGloballyLocked)
                return false;

            return !IsBusyOrLocked(targetBehaviour);
        }

        return true;
    }

    private bool IsBusyOrLocked(MonoBehaviour behaviour)
    {
        if (behaviour == null)
            return false;

        Type type = behaviour.GetType();

        FieldInfo isBusyField = type.GetField("isBusy", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (isBusyField != null && isBusyField.FieldType == typeof(bool))
        {
            bool busy = (bool)isBusyField.GetValue(behaviour);
            if (busy)
                return true;
        }

        PropertyInfo isBusyProperty = type.GetProperty("IsBusy", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (isBusyProperty != null && isBusyProperty.PropertyType == typeof(bool) && isBusyProperty.CanRead)
        {
            bool busy = (bool)isBusyProperty.GetValue(behaviour);
            if (busy)
                return true;
        }

        MethodInfo interactionLockedMethod = type.GetMethod("IsInteractionLocked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (interactionLockedMethod != null && interactionLockedMethod.ReturnType == typeof(bool))
        {
            bool locked = (bool)interactionLockedMethod.Invoke(behaviour, null);
            if (locked)
                return true;
        }

        return false;
    }

    private bool WrapperHasPreparedBox(MonoBehaviour wrapperBehaviour)
    {
        if (wrapperBehaviour == null)
            return false;

        Type type = wrapperBehaviour.GetType();
        FieldInfo currentBoxField = type.GetField("currentBox", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (currentBoxField == null)
            return false;

        GameObject currentBoxObject = currentBoxField.GetValue(wrapperBehaviour) as GameObject;
        if (currentBoxObject == null)
            return false;

        Box box = currentBoxObject.GetComponent<Box>();
        if (box == null || box.playerItemPool == null)
            return false;

        return box.playerItemPool.Count > 0;
    }

    private void DetectBuyBoxTrigger()
    {
        bool currentPurchaseRunning = GetPurchaseRunningState();

        if (!firstBuyBoxPlayed && currentPurchaseRunning && !lastObservedPurchaseRunning)
        {
            firstBuyBoxPlayed = true;
            TriggerBuyBox();
        }

        lastObservedPurchaseRunning = currentPurchaseRunning;
    }

    private bool GetPurchaseRunningState()
    {
        if (PCShoppingCartManager.Instance == null)
            return false;

        FieldInfo purchaseRunningField = typeof(PCShoppingCartManager).GetField(
            "purchaseRunning",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        if (purchaseRunningField == null)
            return false;

        object value = purchaseRunningField.GetValue(PCShoppingCartManager.Instance);
        if (value is bool running)
            return running;

        return false;
    }

    // =========================================================
    // TEXT CHUNKING
    // =========================================================

    private List<string> SplitPhraseIntoChunks(string fullText, int maxVisibleChars)
    {
        List<string> result = new List<string>();

        if (string.IsNullOrWhiteSpace(fullText))
        {
            result.Add(string.Empty);
            return result;
        }

        if (maxVisibleChars <= 0)
        {
            result.Add(fullText);
            return result;
        }

        string[] words = fullText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            result.Add(fullText);
            return result;
        }

        StringBuilder current = new StringBuilder();

        for (int i = 0; i < words.Length; i++)
        {
            string word = words[i];

            if (current.Length == 0)
            {
                if (word.Length <= maxVisibleChars)
                {
                    current.Append(word);
                }
                else
                {
                    AddLongWordChunks(word, maxVisibleChars, result);
                    current.Clear();
                }

                continue;
            }

            string candidate = current + " " + word;

            if (candidate.Length <= maxVisibleChars)
            {
                current.Clear();
                current.Append(candidate);
            }
            else
            {
                result.Add(current.ToString());
                current.Clear();

                if (word.Length <= maxVisibleChars)
                {
                    current.Append(word);
                }
                else
                {
                    AddLongWordChunks(word, maxVisibleChars, result);
                }
            }
        }

        if (current.Length > 0)
            result.Add(current.ToString());

        return result;
    }

    private void AddLongWordChunks(string word, int maxVisibleChars, List<string> output)
    {
        if (string.IsNullOrEmpty(word))
            return;

        int index = 0;
        while (index < word.Length)
        {
            int len = Mathf.Min(maxVisibleChars, word.Length - index);
            output.Add(word.Substring(index, len));
            index += len;
        }
    }

    // =========================================================
    // EASING
    // =========================================================

    private float EaseOutBack(float x)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}