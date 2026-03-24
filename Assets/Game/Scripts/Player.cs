using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    private enum HoldCategory { None, Box, Item }

    private enum AnimState
    {
        Idle,
        RunFront,
        RunBack,
        HoldingIdle,
        HoldingFront,
        HoldingBack
    }

    [Header("Movement")]
    [SerializeField] private float speed = 5f;

    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private Transform holdTarget;

    [Header("Interaction Target Preference")]
    [SerializeField] private float directionalPreferenceWeight = 1.25f;
    [SerializeField] private float lastInputDeadZone = 0.05f;

    [Header("Target Indicator")]
    [SerializeField] private GameObject selectedTargetPrefab;
    [SerializeField] private float selectedTargetYOffset = 0.75f;
    [SerializeField] private float indicatorBobAmplitude = 0.08f;
    [SerializeField] private float indicatorBobSpeed = 3.5f;
    [SerializeField] private Color indicatorNormalColor = Color.white;
    [SerializeField] private Color indicatorBlockedColor = Color.red;
    [SerializeField] private float blockedShakeDuration = 0.16f;
    [SerializeField] private float blockedShakeStrength = 0.14f;

    [Header("Drop")]
    [SerializeField] private Transform dropOrigin;
    [SerializeField] private float dropRadius = 1.25f;
    [SerializeField][Range(0f, 180f)] private float dropPreferredAngle = 60f;
    [SerializeField] private float dropMinDistanceFactor = 0.35f;

    [Header("Drop Juice")]
    [SerializeField] private float dropLandingDelay = 0.08f;
    [SerializeField] private float dropSquashDuration = 0.08f;
    [SerializeField] private float dropStretchDuration = 0.10f;
    [SerializeField] private Vector3 dropSquashScale = new Vector3(1.15f, 0.82f, 1f);
    [SerializeField] private Vector3 dropStretchScale = new Vector3(0.92f, 1.12f, 1f);

    [Header("Holding Capacity")]
    [SerializeField] private int maxHolding = 3;

    [Header("Stack Offsets")]
    [SerializeField] private float boxStackYOffset = 0.35f;
    [SerializeField] private float itemStackYOffset = 0.20f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Animation Trigger Names")]
    [SerializeField] private string idleTrigger = "Idle";
    [SerializeField] private string runFrontTrigger = "RunFront";
    [SerializeField] private string runBackTrigger = "RunBack";
    [SerializeField] private string holdingIdleTrigger = "HoldingIdle";
    [SerializeField] private string holdingFrontTrigger = "HoldingFront";
    [SerializeField] private string holdingBackTrigger = "HoldingBack";

    [Header("Debug")]
    [SerializeField] private Interactable currentTarget;

    private Rigidbody2D rb;
    private Vector2 moveDirection;
    private Vector2 lastInputDirection = Vector2.down;

    private readonly List<Interactable> inRange = new();
    [SerializeField] private List<Interactable> heldStack = new();

    private HoldCategory heldCategory = HoldCategory.None;
    private int currentWeight = 0;

    private AnimState currentAnimState = AnimState.Idle;
    private bool movementLocked = false;

    private GameObject currentTargetIndicator;
    private Interactable lastVisualTarget;
    private bool lastIndicatorBlocked;
    private readonly List<SpriteRenderer> cachedIndicatorRenderers = new();
    private Coroutine indicatorShakeRoutine;
    private Vector3 indicatorBaseLocalPos;

    private readonly Dictionary<Transform, Coroutine> activeDropJuices = new();
    private readonly Dictionary<Transform, Vector3> cachedOriginalScales = new();

    private bool IsHolding => heldStack.Count > 0;
    public bool IsMovementLocked => movementLocked;
    public Transform HoldTarget => holdTarget != null ? holdTarget : transform;
    public Transform DropOrigin => dropOrigin != null ? dropOrigin : transform;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (holdTarget == null)
            Debug.LogWarning($"{name}: HoldTarget no asignado. Se usará el transform del player.");

        if (dropOrigin == null)
            Debug.LogWarning($"{name}: DropOrigin no asignado. Se usará el transform del player.");
    }

    private void Start()
    {
        ForcePlayAnimationState(GetTargetAnimState());
        RefreshTargetVisuals();
    }

    private void Update()
    {
        HandleMovementInput();
        UpdateClosestTarget();
        RefreshTargetVisuals();
        UpdateAnimationState();

        if (Input.GetKeyDown(interactKey))
            HandleInteraction();
    }

    private void LateUpdate()
    {
        UpdateTargetIndicatorMotion();
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = movementLocked ? Vector2.zero : moveDirection * speed;
    }

    private void OnDisable()
    {
        ClearTargetVisuals();
        StopAllDropJuices();
    }

    private void OnDestroy()
    {
        ClearTargetVisuals();
        StopAllDropJuices();
    }

    public void SetMovementLocked(bool locked)
    {
        movementLocked = locked;

        if (locked)
        {
            moveDirection = Vector2.zero;
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
        }
    }

    private void HandleMovementInput()
    {
        if (movementLocked)
        {
            moveDirection = Vector2.zero;
            return;
        }

        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        Vector2 input = new Vector2(x, y);
        moveDirection = input.sqrMagnitude > 0.01f ? input.normalized : Vector2.zero;

        if (input.sqrMagnitude > lastInputDeadZone * lastInputDeadZone)
            lastInputDirection = input.normalized;
    }

    private AnimState GetTargetAnimState()
    {
        bool moving = moveDirection.sqrMagnitude > 0.001f;
        bool goingUp = moveDirection.y > 0.01f;

        if (IsHolding)
        {
            if (!moving)
                return AnimState.HoldingIdle;

            return goingUp ? AnimState.HoldingBack : AnimState.HoldingFront;
        }

        if (!moving)
            return AnimState.Idle;

        return goingUp ? AnimState.RunBack : AnimState.RunFront;
    }

    private void UpdateAnimationState()
    {
        if (animator == null)
            return;

        AnimState targetState = GetTargetAnimState();

        if (targetState == currentAnimState)
            return;

        PlayAnimationState(targetState);
    }

    private void ForcePlayAnimationState(AnimState state)
    {
        currentAnimState = state;
        ResetAllAnimationTriggers();
        SetTriggerForState(state);
    }

    private void PlayAnimationState(AnimState newState)
    {
        currentAnimState = newState;
        ResetAllAnimationTriggers();
        SetTriggerForState(newState);
    }

    private void ResetAllAnimationTriggers()
    {
        if (animator == null) return;

        animator.ResetTrigger(idleTrigger);
        animator.ResetTrigger(runFrontTrigger);
        animator.ResetTrigger(runBackTrigger);
        animator.ResetTrigger(holdingIdleTrigger);
        animator.ResetTrigger(holdingFrontTrigger);
        animator.ResetTrigger(holdingBackTrigger);
    }

    private void SetTriggerForState(AnimState state)
    {
        switch (state)
        {
            case AnimState.Idle:
                animator.SetTrigger(idleTrigger);
                break;
            case AnimState.RunFront:
                animator.SetTrigger(runFrontTrigger);
                break;
            case AnimState.RunBack:
                animator.SetTrigger(runBackTrigger);
                break;
            case AnimState.HoldingIdle:
                animator.SetTrigger(holdingIdleTrigger);
                break;
            case AnimState.HoldingFront:
                animator.SetTrigger(holdingFrontTrigger);
                break;
            case AnimState.HoldingBack:
                animator.SetTrigger(holdingBackTrigger);
                break;
        }
    }

    public void RegisterInRange(Interactable i)
    {
        if (i == null) return;
        if (!inRange.Contains(i))
            inRange.Add(i);
    }

    public void UnregisterInRange(Interactable i)
    {
        if (i == null) return;
        inRange.Remove(i);

        if (currentTarget == i)
            currentTarget = null;

        if (lastVisualTarget == i)
            ClearTargetVisuals();
    }

    private void UpdateClosestTarget()
    {
        for (int i = inRange.Count - 1; i >= 0; i--)
        {
            if (inRange[i] == null)
                inRange.RemoveAt(i);
        }

        if (inRange.Count == 0)
        {
            currentTarget = null;
            return;
        }

        Interactable bestTarget = null;
        float bestScore = float.MinValue;
        Vector2 playerPos = transform.position;
        bool hasDirectionalPreference = lastInputDirection.sqrMagnitude > 0.0001f;

        foreach (var interactable in inRange)
        {
            if (interactable == null)
                continue;

            Vector2 toTarget = (Vector2)interactable.transform.position - playerPos;
            float sqrDist = toTarget.sqrMagnitude;
            float score = -sqrDist;

            if (hasDirectionalPreference && sqrDist > 0.0001f)
            {
                Vector2 dirToTarget = toTarget.normalized;
                float dot = Vector2.Dot(lastInputDirection, dirToTarget);
                score += dot * directionalPreferenceWeight;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = interactable;
            }
        }

        currentTarget = bestTarget;
    }

    private bool IsPickable(Interactable i)
    {
        return i != null && (i.IsBox || i.IsItem);
    }

    private HoldCategory CategoryOf(Interactable i)
    {
        if (i.IsBox) return HoldCategory.Box;
        if (i.IsItem) return HoldCategory.Item;
        return HoldCategory.None;
    }

    private int WeightOf(Interactable i)
    {
        if (i == null) return 999;

        if (i.IsItem)
            return 1;

        if (i.IsBox)
        {
            Box box = i.GetComponent<Box>();
            if (box == null)
            {
                Debug.LogWarning($"{i.name}: Está marcado como Box pero no tiene componente Box.");
                return 999;
            }

            return box.Weight;
        }

        return 999;
    }

    private float OffsetYForCategory(HoldCategory category)
    {
        return category == HoldCategory.Box ? boxStackYOffset : itemStackYOffset;
    }

    private void RefreshStackVisuals()
    {
        float yOffset = OffsetYForCategory(heldCategory);

        for (int i = 0; i < heldStack.Count; i++)
        {
            Interactable interactable = heldStack[i];
            if (interactable == null) continue;

            Vector3 localOffset = new Vector3(0f, yOffset * i, 0f);

            interactable.transform.SetParent(HoldTarget, true);
            interactable.transform.localPosition = localOffset;
            interactable.transform.localRotation = Quaternion.identity;

            if (heldCategory == HoldCategory.Box)
            {
                Box box = interactable.GetComponent<Box>();
                if (box != null)
                    box.ApplyStackSprite(i);
            }
        }
    }

    private void PushToStack(Interactable target)
    {
        if (target == null) return;

        if (heldCategory == HoldCategory.None)
            heldCategory = CategoryOf(target);

        CancelDropJuice(target);

        heldStack.Add(target);
        currentWeight += WeightOf(target);

        float yOffset = OffsetYForCategory(heldCategory);
        int stackIndex = heldStack.Count - 1;
        Vector3 localOffset = new Vector3(0f, yOffset * stackIndex, 0f);

        target.PickUpToStack(HoldTarget, localOffset);
        RefreshStackVisuals();
    }

    private Interactable PopLastFromStack()
    {
        if (heldStack.Count == 0)
            return null;

        int lastIndex = heldStack.Count - 1;
        Interactable last = heldStack[lastIndex];

        heldStack.RemoveAt(lastIndex);

        if (last != null)
            currentWeight -= WeightOf(last);

        if (heldStack.Count == 0)
            heldCategory = HoldCategory.None;

        RefreshStackVisuals();
        return last;
    }

    private bool CanAdd(Interactable target)
    {
        if (!IsPickable(target))
            return false;

        HoldCategory targetCategory = CategoryOf(target);

        if (heldCategory != HoldCategory.None && heldCategory != targetCategory)
            return false;

        int weight = WeightOf(target);
        return (currentWeight + weight) <= maxHolding;
    }

    private Vector3 GetRandomDropPosition()
    {
        Vector3 origin = DropOrigin.position;

        if (lastInputDirection.sqrMagnitude <= lastInputDeadZone * lastInputDeadZone)
        {
            Vector2 randomOffset = Random.insideUnitCircle * Mathf.Max(0f, dropRadius);
            return new Vector3(origin.x + randomOffset.x, origin.y + randomOffset.y, origin.z);
        }

        Vector2 dir = lastInputDirection.normalized;
        float halfAngle = dropPreferredAngle * 0.5f;
        float randomAngle = Random.Range(-halfAngle, halfAngle);
        Vector2 preferredDir = RotateVector(dir, randomAngle);

        float minDistance = Mathf.Clamp01(dropMinDistanceFactor) * dropRadius;
        float distance = Random.Range(minDistance, Mathf.Max(minDistance, dropRadius));
        Vector2 offset = preferredDir * distance;

        return new Vector3(origin.x + offset.x, origin.y + offset.y, origin.z);
    }

    private Vector2 RotateVector(Vector2 vector, float angleDegrees)
    {
        float radians = angleDegrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);

        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos
        ).normalized;
    }

    private void HandleInteraction()
    {
        Interactable target = currentTarget;

        if (target != null && !IsTargetInteractableNow(target))
        {
            PlayBlockedIndicatorFeedback();
            return;
        }

        if (target != null && !IsPickable(target))
        {
            target.Interact(this);
            return;
        }

        if (target == null)
        {
            if (heldStack.Count > 0)
            {
                Interactable last = PopLastFromStack();
                if (last != null)
                {
                    Vector3 randomDropPos = GetRandomDropPosition();
                    last.DropTo(randomDropPos, last.transform.rotation);
                    StartDropJuiceIfObject(last);
                }
            }
            return;
        }

        if (heldStack.Count == 0)
        {
            if (CanAdd(target))
                PushToStack(target);

            return;
        }

        HoldCategory targetCategory = CategoryOf(target);

        if (heldCategory != targetCategory)
            return;

        if (CanAdd(target))
        {
            PushToStack(target);
            return;
        }

        Interactable top = heldStack[heldStack.Count - 1];
        int topWeight = WeightOf(top);
        int targetWeight = WeightOf(target);

        bool swapFits = (currentWeight - topWeight + targetWeight) <= maxHolding;
        if (!swapFits)
            return;

        Vector3 dropPos = target.transform.position;
        Quaternion dropRot = target.transform.rotation;

        PopLastFromStack();

        if (top != null)
        {
            top.DropTo(dropPos, dropRot);
            StartDropJuiceIfObject(top);
        }

        PushToStack(target);
    }

    private void StartDropJuiceIfObject(Interactable interactable)
    {
        if (interactable == null)
            return;

        if (!interactable.IsBox && !interactable.IsItem)
            return;

        Transform t = interactable.transform;

        CancelDropJuice(interactable);

        cachedOriginalScales[t] = t.localScale;

        Coroutine routine = StartCoroutine(DropJuiceRoutine(t));
        activeDropJuices[t] = routine;
    }

    private void CancelDropJuice(Interactable interactable)
    {
        if (interactable == null)
            return;

        Transform t = interactable.transform;
        if (activeDropJuices.TryGetValue(t, out Coroutine routine))
        {
            if (routine != null)
                StopCoroutine(routine);

            activeDropJuices.Remove(t);
        }

        if (t != null && cachedOriginalScales.TryGetValue(t, out Vector3 originalScale))
            t.localScale = originalScale;
    }

    private void StopAllDropJuices()
    {
        List<Transform> keys = new List<Transform>(activeDropJuices.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            Transform t = keys[i];
            if (t == null)
                continue;

            if (activeDropJuices.TryGetValue(t, out Coroutine routine) && routine != null)
                StopCoroutine(routine);

            if (cachedOriginalScales.TryGetValue(t, out Vector3 originalScale))
                t.localScale = originalScale;
        }

        activeDropJuices.Clear();
    }

    private IEnumerator DropJuiceRoutine(Transform droppedTransform)
    {
        if (droppedTransform == null)
            yield break;

        if (!cachedOriginalScales.TryGetValue(droppedTransform, out Vector3 originalScale))
            originalScale = droppedTransform.localScale;

        if (dropLandingDelay > 0f)
            yield return new WaitForSeconds(dropLandingDelay);

        Vector3 squashScale = Vector3.Scale(originalScale, dropSquashScale);
        Vector3 stretchScale = Vector3.Scale(originalScale, dropStretchScale);

        float t = 0f;
        while (t < dropSquashDuration)
        {
            if (droppedTransform == null)
                yield break;

            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / dropSquashDuration);
            droppedTransform.localScale = Vector3.LerpUnclamped(originalScale, squashScale, n);
            yield return null;
        }

        t = 0f;
        while (t < dropStretchDuration)
        {
            if (droppedTransform == null)
                yield break;

            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / dropStretchDuration);

            if (n < 0.5f)
            {
                float phase = n / 0.5f;
                droppedTransform.localScale = Vector3.LerpUnclamped(squashScale, stretchScale, phase);
            }
            else
            {
                float phase = (n - 0.5f) / 0.5f;
                droppedTransform.localScale = Vector3.LerpUnclamped(stretchScale, originalScale, phase);
            }

            yield return null;
        }

        if (droppedTransform != null)
            droppedTransform.localScale = originalScale;

        activeDropJuices.Remove(droppedTransform);
    }

    private void RefreshTargetVisuals()
    {
        Interactable target = currentTarget;

        if (target == null)
        {
            ClearTargetVisuals();
            return;
        }

        bool isBlocked = !IsTargetInteractableNow(target);

        if (lastVisualTarget != target)
        {
            ClearTargetVisuals();
            ApplyTargetVisuals(target, isBlocked);
            return;
        }

        if (lastIndicatorBlocked != isBlocked)
            UpdateIndicatorBlockedColor(isBlocked);
    }

    private void ApplyTargetVisuals(Interactable target, bool blocked)
    {
        if (target == null)
            return;

        lastVisualTarget = target;
        lastIndicatorBlocked = blocked;

        if (selectedTargetPrefab != null)
        {
            currentTargetIndicator = Instantiate(selectedTargetPrefab);
            currentTargetIndicator.transform.SetParent(target.transform, false);
            currentTargetIndicator.transform.localRotation = Quaternion.identity;
            currentTargetIndicator.transform.localScale = Vector3.one;
            indicatorBaseLocalPos = new Vector3(0f, selectedTargetYOffset, 0f);
            currentTargetIndicator.transform.localPosition = indicatorBaseLocalPos;

            CacheIndicatorRenderers();
            ApplyIndicatorColor(blocked ? indicatorBlockedColor : indicatorNormalColor);
        }
    }

    private void UpdateIndicatorBlockedColor(bool blocked)
    {
        lastIndicatorBlocked = blocked;
        ApplyIndicatorColor(blocked ? indicatorBlockedColor : indicatorNormalColor);
    }

    private void CacheIndicatorRenderers()
    {
        cachedIndicatorRenderers.Clear();

        if (currentTargetIndicator == null)
            return;

        SpriteRenderer[] renderers = currentTargetIndicator.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;

            cachedIndicatorRenderers.Add(renderers[i]);
        }
    }

    private void ApplyIndicatorColor(Color color)
    {
        for (int i = 0; i < cachedIndicatorRenderers.Count; i++)
        {
            if (cachedIndicatorRenderers[i] == null)
                continue;

            cachedIndicatorRenderers[i].color = color;
        }
    }

    private void ClearTargetVisuals()
    {
        if (indicatorShakeRoutine != null)
        {
            StopCoroutine(indicatorShakeRoutine);
            indicatorShakeRoutine = null;
        }

        cachedIndicatorRenderers.Clear();

        if (currentTargetIndicator != null)
            Destroy(currentTargetIndicator);

        currentTargetIndicator = null;
        lastVisualTarget = null;
        lastIndicatorBlocked = false;
    }

    private void UpdateTargetIndicatorMotion()
    {
        if (currentTargetIndicator == null || lastVisualTarget == null)
            return;

        if (indicatorShakeRoutine != null)
            return;

        float bob = Mathf.Sin(Time.time * indicatorBobSpeed) * indicatorBobAmplitude;
        currentTargetIndicator.transform.localPosition = indicatorBaseLocalPos + new Vector3(0f, bob, 0f);
    }

    private void PlayBlockedIndicatorFeedback()
    {
        if (currentTargetIndicator == null)
            return;

        UpdateIndicatorBlockedColor(true);

        if (indicatorShakeRoutine != null)
            StopCoroutine(indicatorShakeRoutine);

        indicatorShakeRoutine = StartCoroutine(BlockedIndicatorShakeRoutine());
    }

    private IEnumerator BlockedIndicatorShakeRoutine()
    {
        float elapsed = 0f;

        while (elapsed < blockedShakeDuration)
        {
            elapsed += Time.deltaTime;
            Vector2 offset = Random.insideUnitCircle * blockedShakeStrength;
            currentTargetIndicator.transform.localPosition =
                indicatorBaseLocalPos + new Vector3(offset.x, offset.y, 0f);
            yield return null;
        }

        indicatorShakeRoutine = null;

        if (currentTargetIndicator != null)
        {
            float bob = Mathf.Sin(Time.time * indicatorBobSpeed) * indicatorBobAmplitude;
            currentTargetIndicator.transform.localPosition = indicatorBaseLocalPos + new Vector3(0f, bob, 0f);
        }
    }

    private bool IsTargetInteractableNow(Interactable target)
    {
        if (target == null)
            return false;

        if (IsPickable(target))
            return CanInteractWithPickableTarget(target);

        return IsNonPickableTargetAvailable(target);
    }

    private bool CanInteractWithPickableTarget(Interactable target)
    {
        if (target == null)
            return false;

        if (heldStack.Count == 0)
            return CanAdd(target);

        HoldCategory targetCategory = CategoryOf(target);
        if (heldCategory != targetCategory)
            return false;

        if (CanAdd(target))
            return true;

        Interactable top = heldStack[heldStack.Count - 1];
        if (top == null)
            return false;

        int topWeight = WeightOf(top);
        int targetWeight = WeightOf(target);
        return (currentWeight - topWeight + targetWeight) <= maxHolding;
    }

    private bool IsNonPickableTargetAvailable(Interactable target)
    {
        if (target == null)
            return false;

        MonoBehaviour[] behaviours = target.GetComponents<MonoBehaviour>();

        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null)
                continue;

            System.Type type = behaviour.GetType();

            if (type.Name == "SellMachine")
            {
                if (!ReturnHeldBox(out _, out Box heldBox) || heldBox == null || heldBox.Type != BoxType.Player)
                    return false;
            }

            if (type.Name == "Wrapper")
            {
                bool holdsItem = ReturnHeldItem(out _, out _);
                bool wrapperHasPreparedBox = WrapperHasPreparedBox(behaviour);

                if (!holdsItem && !wrapperHasPreparedBox)
                    return false;
            }

            if (type.Name == "Unwrapper")
            {
                if (!ReturnHeldBox(out _, out _))
                    return false;
            }

            FieldInfo isBusyField = type.GetField("isBusy", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (isBusyField != null && isBusyField.FieldType == typeof(bool))
            {
                bool busy = (bool)isBusyField.GetValue(behaviour);
                if (busy)
                    return false;
            }

            PropertyInfo isBusyProperty = type.GetProperty("IsBusy", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (isBusyProperty != null && isBusyProperty.PropertyType == typeof(bool) && isBusyProperty.CanRead)
            {
                bool busy = (bool)isBusyProperty.GetValue(behaviour);
                if (busy)
                    return false;
            }

            if (type.Name.Contains("PC") && PCShoppingCartManager.IsGloballyLocked)
                return false;
        }

        return true;
    }

    private bool WrapperHasPreparedBox(MonoBehaviour wrapperBehaviour)
    {
        if (wrapperBehaviour == null)
            return false;

        System.Type type = wrapperBehaviour.GetType();
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

    public bool TryTakeTopHeldBox(out GameObject boxGameObject, out Box boxData)
    {
        boxGameObject = null;
        boxData = null;

        if (heldStack.Count == 0)
            return false;

        Interactable top = heldStack[heldStack.Count - 1];

        if (top == null || !top.IsBox)
            return false;

        Box foundBox = top.GetComponent<Box>();
        if (foundBox == null)
            return false;

        CancelDropJuice(top);

        PopLastFromStack();

        top.transform.SetParent(null);
        top.ResetSortingOrder();

        boxGameObject = top.gameObject;
        boxData = foundBox;
        return true;
    }

    public bool ReturnHeldItem(out GameObject itemGameObject, out WorldItem itemData)
    {
        itemGameObject = null;
        itemData = null;

        if (heldStack.Count == 0)
            return false;

        Interactable last = heldStack[heldStack.Count - 1];
        if (last == null || !last.IsItem)
            return false;

        WorldItemComponent foundItem = last.GetComponent<WorldItemComponent>();
        if (foundItem == null)
            return false;

        itemGameObject = last.gameObject;
        itemData = foundItem.Data;

        return true;
    }

    public bool ReturnHeldBox(out GameObject boxGameObject, out Box boxData)
    {
        boxGameObject = null;
        boxData = null;

        if (heldStack.Count == 0)
            return false;

        Interactable last = heldStack[heldStack.Count - 1];
        if (last == null || !last.IsBox)
            return false;

        Box foundBox = last.GetComponent<Box>();
        if (foundBox == null)
            return false;

        boxGameObject = last.gameObject;
        boxData = foundBox;

        return true;
    }

    public bool TryTakeTopHeldItem(out GameObject itemGameObject, out WorldItem itemData)
    {
        itemGameObject = null;
        itemData = null;

        if (heldStack.Count == 0)
            return false;

        Interactable last = heldStack[heldStack.Count - 1];
        if (last == null || !last.IsItem)
            return false;

        WorldItemComponent foundItem = last.GetComponent<WorldItemComponent>();
        if (foundItem == null)
            return false;

        CancelDropJuice(last);

        PopLastFromStack();

        last.transform.SetParent(null);
        last.ResetSortingOrder();

        itemGameObject = last.gameObject;
        itemData = foundItem.Data;

        return true;
    }

    public bool IsHoldingAtLeastOneBox()
    {
        if (heldStack.Count == 0)
            return false;

        Interactable top = heldStack[heldStack.Count - 1];
        return top != null && top.IsBox;
    }

    private void OnDrawGizmosSelected()
    {
        Transform origin = dropOrigin != null ? dropOrigin : transform;
        if (origin == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin.position, dropRadius);
    }
}