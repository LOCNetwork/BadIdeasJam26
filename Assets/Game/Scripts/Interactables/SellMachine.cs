using System.Collections;
using UnityEngine;

public class SellMachine : Interactable
{
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string sellSmallTrigger = "SellS";
    [SerializeField] private string sellMediumTrigger = "SellM";
    [SerializeField] private string sellLargeTrigger = "SellL";

    [Header("Interaction Cooldown")]
    [SerializeField] private float interactCooldown = 1.0f;

    [Header("Delay")]
    [SerializeField] private float delay = 1.0f;

    private bool isBusy = false;

    public override void Interact(Player player)
    {
        if (isBusy)
            return;

        if (player == null)
            return;

        if (!player.ReturnHeldBox(out GameObject boxObject, out Box boxData))
            return;

        if (boxData == null)
            return;

        if (boxData.Type == BoxType.Delivery)
            return;

        TriggerSellAnimation(boxData);

        player.TryTakeTopHeldBox(out GameObject boxObj, out Box boxToSell);

        StartCoroutine(sellEnumerator(boxObj, boxToSell));

        StartCoroutine(BusyCooldownRoutine());
    }

    IEnumerator sellEnumerator(GameObject boxObj, Box boxToSell)
    {
        boxObj.SetActive(false);
        yield return new WaitForSeconds(delay);

        if (boxObj != null && boxToSell != null)
        {
            boxObj.SetActive(true);
            GameManager.instance.sellManager.PutBoxToSell(boxObj, boxToSell);
        }
    }


    private void TriggerSellAnimation(Box boxData)
    {
        if (animator == null || boxData == null)
            return;

        switch (boxData.Size)
        {
            case BoxSize.Small:
                if (!string.IsNullOrEmpty(sellSmallTrigger))
                    animator.SetTrigger(sellSmallTrigger);
                break;

            case BoxSize.Medium:
                if (!string.IsNullOrEmpty(sellMediumTrigger))
                    animator.SetTrigger(sellMediumTrigger);
                break;

            case BoxSize.Large:
                if (!string.IsNullOrEmpty(sellLargeTrigger))
                    animator.SetTrigger(sellLargeTrigger);
                break;
        }
    }

    private System.Collections.IEnumerator BusyCooldownRoutine()
    {
        isBusy = true;
        yield return new WaitForSeconds(interactCooldown);
        isBusy = false;
    }
}