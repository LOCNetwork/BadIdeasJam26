using System.Collections.Generic;
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

    private readonly List<Interactable> inRange = new();
    [SerializeField] private List<Interactable> heldStack = new();

    private HoldCategory heldCategory = HoldCategory.None;
    private int currentWeight = 0;

    private AnimState currentAnimState = AnimState.Idle;

    private bool IsHolding => heldStack.Count > 0;
    public Transform HoldTarget => holdTarget != null ? holdTarget : transform;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (holdTarget == null)
            Debug.LogWarning($"{name}: HoldTarget no asignado. Se usar� el transform del player.");
    }

    private void Start()
    {
        ForcePlayAnimationState(GetTargetAnimState());
    }

    private void Update()
    {
        HandleMovementInput();
        UpdateClosestTarget();
        UpdateAnimationState();

        if (Input.GetKeyDown(interactKey))
            HandleInteraction();
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = moveDirection * speed;
    }

    private void HandleMovementInput()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        Vector2 input = new Vector2(x, y);
        moveDirection = input.sqrMagnitude > 0.01f ? input.normalized : Vector2.zero;
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

        float bestDistance = float.MaxValue;
        Interactable bestTarget = null;
        Vector2 playerPos = transform.position;

        foreach (var interactable in inRange)
        {
            if (interactable == null) continue;

            float dist = ((Vector2)interactable.transform.position - playerPos).sqrMagnitude;
            if (dist < bestDistance)
            {
                bestDistance = dist;
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
                Debug.LogWarning($"{i.name}: Est� marcado como Box pero no tiene componente Box.");
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

    public bool TryTakeTopHeldBox(out GameObject boxGameObject, out Box boxData)
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

        PopLastFromStack();

        last.transform.SetParent(null);
        last.ResetSortingOrder();

        boxGameObject = last.gameObject;
        boxData = foundBox;
        return true;
    }

    public bool IsHoldingAtLeastOneBox()
    {
        if (heldStack.Count == 0)
            return false;

        Interactable last = heldStack[heldStack.Count - 1];
        return last != null && last.IsBox;
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

    private void HandleInteraction()
    {
        Interactable target = currentTarget;

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
                    last.DropInPlace();
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
            top.DropTo(dropPos, dropRot);

        PushToStack(target);
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

        PopLastFromStack();

        top.transform.SetParent(null);
        top.ResetSortingOrder();

        boxGameObject = top.gameObject;
        boxData = foundBox;
        return true;
    }

    public bool IsHoldingAtLeastOneBox()
    {
        if (heldStack.Count == 0)
            return false;

        Interactable top = heldStack[heldStack.Count - 1];
        return top != null && top.IsBox;
    }
}