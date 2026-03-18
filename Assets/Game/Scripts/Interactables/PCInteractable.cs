using UnityEngine;

public class PCInteractable : Interactable
{
    [SerializeField] private PCMenuController pcMenu;

    public override void Interact(Player player)
    {
        if (pcMenu == null)
        {
            Debug.LogWarning($"{name}: No hay PCMenuController asignado.");
            return;
        }

        if (PCShoppingCartManager.IsGloballyLocked)
        {
            Debug.Log("El PC está bloqueado hasta que termine la reconstrucción.");
            return;
        }

        pcMenu.Toggle(player);
    }
}
