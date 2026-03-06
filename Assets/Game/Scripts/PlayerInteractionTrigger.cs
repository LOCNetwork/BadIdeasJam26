using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerInteractionTrigger : MonoBehaviour
{
    [SerializeField] private Player player;

    private void Reset()
    {
        player = GetComponentInParent<Player>();

        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        if (player == null)
            player = GetComponentInParent<Player>();

        if (player == null)
            Debug.LogError($"{name}: No se encontró Player en el padre.");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var interactable = other.GetComponentInParent<Interactable>();
        if (interactable != null)
            player.RegisterInRange(interactable);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var interactable = other.GetComponentInParent<Interactable>();
        if (interactable != null)
            player.UnregisterInRange(interactable);
    }
}