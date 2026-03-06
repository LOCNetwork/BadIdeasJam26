using UnityEngine;

public class Interactable : MonoBehaviour
{
    [Header("Type")]
    [SerializeField] private bool isBox = false;
    [SerializeField] private bool isItem = false;

    public bool IsBox => isBox;
    public bool IsItem => isItem;

    [Header("Tween")]
    [SerializeField] private float tweenTime = 0.15f;

    private Rigidbody2D rb2d;
    private Collider2D[] colliders2d;
    private SpriteRenderer sr;
    private Coroutine tweenRoutine;

    private void Awake()
    {
        rb2d = GetComponent<Rigidbody2D>();
        colliders2d = GetComponents<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
    }
    public virtual void Interact(Player player)
    {
        Debug.Log("He interactuado con una no caja");
    }

    public void SetSortingOrder(int order)
    {
        if (sr != null)
            sr.sortingOrder = order;
    }

    public void ResetSortingOrder()
    {
        SetSortingOrder(0);
    }

    private void SetPhysicsEnabled(bool enabled)
    {
        if (rb2d != null)
        {
            rb2d.linearVelocity = Vector2.zero;
            rb2d.angularVelocity = 0f;
            rb2d.simulated = enabled;
        }

        if (colliders2d != null)
        {
            for (int i = 0; i < colliders2d.Length; i++)
                if (colliders2d[i] != null)
                    colliders2d[i].enabled = enabled;
        }
    }

    public void PickUpToStack(Transform parent, Vector3 localOffset)
    {
        if (parent == null)
        {
            Debug.LogWarning($"{name}: PickUpToStack sin parent.");
            return;
        }

        transform.SetParent(null);
        SetPhysicsEnabled(false);

        Vector3 worldTarget = parent.TransformPoint(localOffset);
        Quaternion worldRot = parent.rotation;

        TweenTo(worldTarget, worldRot, () =>
        {
            transform.SetParent(parent, worldPositionStays: true);
            transform.localPosition = localOffset;
            transform.localRotation = Quaternion.identity;
        });
    }

    public void DropTo(Vector3 worldPos, Quaternion worldRot)
    {
        transform.SetParent(null);
        SetPhysicsEnabled(false);

        TweenTo(worldPos, worldRot, () =>
        {
            SetPhysicsEnabled(true);
            ResetSortingOrder();
        });
    }

    public void DropInPlace()
    {
        transform.SetParent(null);
        SetPhysicsEnabled(true);
        ResetSortingOrder();
    }

    private void TweenTo(Vector3 targetPos, Quaternion targetRot, System.Action onArrive)
    {
        if (tweenRoutine != null)
            StopCoroutine(tweenRoutine);

        tweenRoutine = StartCoroutine(TweenRoutine(targetPos, targetRot, onArrive));
    }

    private System.Collections.IEnumerator TweenRoutine(Vector3 targetPos, Quaternion targetRot, System.Action onArrive)
    {
        float duration = Mathf.Max(0.01f, tweenTime);
        float t = 0f;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float s = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(startPos, targetPos, s);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, s);

            yield return null;
        }

        transform.position = targetPos;
        transform.rotation = targetRot;

        tweenRoutine = null;
        onArrive?.Invoke();
    }
}