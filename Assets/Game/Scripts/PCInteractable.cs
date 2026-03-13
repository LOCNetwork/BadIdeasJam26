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

        pcMenu.Toggle(player);
    }
}