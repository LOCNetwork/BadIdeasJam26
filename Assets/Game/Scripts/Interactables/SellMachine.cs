using Unity.VisualScripting;
using UnityEngine;

public class SellMachine : Interactable
{

    bool isBusy = false;

    public override void Interact(Player player)
    {
       
        if (isBusy) return;

        if (player.ReturnHeldBox(out GameObject boxObject, out Box boxData))
        {

            if (boxData.Type == BoxType.Delivery) return;

            player.TryTakeTopHeldBox(out GameObject boxObj, out Box boxToSell);

            GameManager.instance.sellManager.PutBoxToSell(boxObj, boxToSell);
        }


    }


}
