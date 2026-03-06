using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    private enum HoldCategory { None, Box, Item }

    [Header("Movement")]
    [SerializeField] private float speed = 5f;

    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private Transform holdTarget;

    [Header("Holding Capacity (by weight)")]
    [SerializeField] private int maxHolding = 3;

    [Header("Stack Offsets")]
    [SerializeField] private float boxStackYOffset = 0.35f;
    [SerializeField] private float itemStackYOffset = 0.20f;

    [Header("Debug")]
    [SerializeField] private Interactable currentTarget;

    private Rigidbody2D rb;
    private Vector2 moveDirection;

    private readonly List<Interactable> inRange = new();

    // Stack (último = top)
    [SerializeField] private List<Interactable> heldStack = new();
    private HoldCategory heldCategory = HoldCategory.None;
    private int currentWeight = 0;

    public Transform HoldTarget => holdTarget != null ? holdTarget : transform;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (holdTarget == null)
            Debug.LogWarning($"{name}: HoldTarget no asignado. Usaré el Transform del Player como fallback.");
    }

    private void Update()
    {
        HandleMovementInput();
        UpdateClosestTarget();

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

        moveDirection = input.sqrMagnitude > 0.001f ? input.normalized : Vector2.zero;
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
        if (currentTarget == i) currentTarget = null;
    }

    private void UpdateClosestTarget()
    {
        for (int idx = inRange.Count - 1; idx >= 0; idx--)
            if (inRange[idx] == null) inRange.RemoveAt(idx);

        if (inRange.Count == 0)
        {
            currentTarget = null;
            return;
        }

        float best = float.MaxValue;
        Interactable bestI = null;
        Vector2 p = transform.position;

        foreach (var i in inRange)
        {
            if (i == null) continue;
            float d = ((Vector2)i.transform.position - p).sqrMagnitude;
            if (d < best)
            {
                best = d;
                bestI = i;
            }
        }

        currentTarget = bestI;
    }

    private bool IsPickable(Interactable i) => i != null && (i.IsBox || i.IsItem);

    private HoldCategory CategoryOf(Interactable i)
    {
        if (i.IsBox) return HoldCategory.Box;
        if (i.IsItem) return HoldCategory.Item;
        return HoldCategory.None;
    }

    private int WeightOf(Interactable i)
    {
        if (i == null) return 999;

        if (i.IsItem) return 1;

        if (i.IsBox)
        {
            Box box = i.GetComponent<Box>();
            if (box == null)
            {
                Debug.LogWarning($"{i.name}: Tiene IsBox marcado pero NO tiene componente Box.cs.");
                return 999;
            }
            return box.Weight;
        }

        return 999;
    }

    private float OffsetYForCategory(HoldCategory cat)
    {
        return cat switch
        {
            HoldCategory.Box => boxStackYOffset,
            HoldCategory.Item => itemStackYOffset,
            _ => 0f
        };
    }

    private void RefreshStackVisuals()
    {
        float yOff = OffsetYForCategory(heldCategory);

        for (int i = 0; i < heldStack.Count; i++)
        {
            Interactable it = heldStack[i];
            if (it == null) continue;

            Vector3 localOffset = new Vector3(0f, yOff * i, 0f);

            it.transform.SetParent(HoldTarget, worldPositionStays: true);
            it.transform.localPosition = localOffset;
            it.transform.localRotation = Quaternion.identity;

            // Sorting: +1 para que en suelo sea 0 y en pila 1..N
            it.SetSortingOrder(i + 1);

            // Alternar sprites si es caja
            if (heldCategory == HoldCategory.Box)
            {
                Box box = it.GetComponent<Box>();
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

        float yOff = OffsetYForCategory(heldCategory);
        int index = heldStack.Count - 1;
        Vector3 localOffset = new Vector3(0f, yOff * index, 0f);

        target.PickUpToStack(HoldTarget, localOffset);

        RefreshStackVisuals();
    }

    private Interactable PopLastFromStack()
    {
        if (heldStack.Count == 0) return null;

        int lastIdx = heldStack.Count - 1;
        Interactable last = heldStack[lastIdx];
        heldStack.RemoveAt(lastIdx);

        if (last != null)
            currentWeight -= WeightOf(last);

        if (heldStack.Count == 0)
            heldCategory = HoldCategory.None;

        RefreshStackVisuals();
        return last;
    }

    private bool CanAdd(Interactable target)
    {
        if (!IsPickable(target)) return false;

        HoldCategory cat = CategoryOf(target);
        if (cat == HoldCategory.None) return false;

        // No mezclar
        if (heldCategory != HoldCategory.None && heldCategory != cat)
            return false;

        int w = WeightOf(target);
        if (w >= 999) return false;

        return (currentWeight + w) <= maxHolding;
    }

    private void HandleInteraction()
    {
        Interactable target = currentTarget;

        // target NO pickable => interacción normal
        if (target != null && !IsPickable(target))
        {
            target.Interact(this);
            return;
        }

        // NO target => si hay stack, suelta el último
        if (target == null)
        {
            if (heldStack.Count > 0)
            {
                Interactable last = PopLastFromStack();
                if (last != null) last.DropInPlace();
            }
            return;
        }

        // target es pickable (box o item)

        // Pila vacía
        if (heldStack.Count == 0)
        {
            if (CanAdd(target))
                PushToStack(target);

            return;
        }

        // Categoría compatible
        HoldCategory targetCat = CategoryOf(target);
        if (heldCategory != targetCat)
            return;

        // Si cabe: push
        if (CanAdd(target))
        {
            PushToStack(target);
            return;
        }

        // Si NO cabe: swap SOLO del último si al reemplazar cabe
        if (heldStack.Count > 0)
        {
            Interactable last = heldStack[heldStack.Count - 1];
            int lastW = WeightOf(last);
            int targetW = WeightOf(target);

            bool swapFits = (currentWeight - lastW + targetW) <= maxHolding;
            if (!swapFits) return;

            Vector3 dropPos = target.transform.position;
            Quaternion dropRot = target.transform.rotation;

            PopLastFromStack();
            if (last != null)
                last.DropTo(dropPos, dropRot);

            PushToStack(target);
        }
    }
}