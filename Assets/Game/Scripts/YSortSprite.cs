using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class YSortSprite : MonoBehaviour
{
    [Header("Ground Y Sort")]
    [SerializeField] private int sortingOffset = 0;
    [SerializeField] private float multiplier = 100f;

    [Header("Held Sort")]
    [SerializeField] private int heldBaseOrder = 700;

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        // Si está siendo holdeado / parentado en una pila
        if (transform.parent != null)
        {
            int siblingIndex = transform.GetSiblingIndex();
            sr.sortingOrder = heldBaseOrder + siblingIndex;
            return;
        }

        // En el suelo: sorting normal por Y
        sr.sortingOrder = sortingOffset + Mathf.RoundToInt(-transform.position.y * multiplier);
    }
}