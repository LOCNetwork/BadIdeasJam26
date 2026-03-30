using System.Collections.Generic;
using UnityEngine;

public class ItemManager
{
    


    public ItemManager()
    {
        Init();
    }


    private void Init()
    {
        Dictionary<string, Item> loadedItems = GameManager.instance.loadedItems;


        SynergyPassive sp = new SynergyPassive();
        ChangeDeliverySpeedPassive cds = new ChangeDeliverySpeedPassive();
        DeliverySpeedExtraPassive dse = new DeliverySpeedExtraPassive();
        DeliverySpeedRequirementPassive dsr = new DeliverySpeedRequirementPassive();
        ExtraValuePassive ev = new ExtraValuePassive();
        ItemReplacerPassive ir = new ItemReplacerPassive();
        ItemRequirementPassive irq = new ItemRequirementPassive();
        RandomValuePassive rv = new RandomValuePassive();
        SlotReducerPassive sr = new SlotReducerPassive();

        loadedItems["ALIEN_HEART"].passives.Add(dse);
        loadedItems["ARTIST_BRUSH"].passives.Add(ir);
        loadedItems["BLEEDING_MEAT_CUBE"].passives.Add(dse);
        loadedItems["BLUE_OVERALLS"].passives.Add(sp);
        loadedItems["BOOTLEG_GAME"].passives.Add(sp);
        loadedItems["BRANDNEW_PRINTER"].passives.Add(sp);
        loadedItems["BULKY_LETTER"].passives.Add(dsr);
        loadedItems["CHEAP_CHIP"].passives.Add(sp);
        loadedItems["CLOWN_COSTUME"].passives.Add(sp);
        loadedItems["COMFORTABLE_JACKET"].passives.Add(sp);
        loadedItems["COMPRESSOR_TOOL"].passives.Add(sr);
        loadedItems["COMPRESSOR_TOOL"].passives.Add(sp);
        loadedItems["CORRECTION_TAPE"].passives.Add(sp);
        loadedItems["DECK_OF_CARDS"].passives.Add(rv);
        loadedItems["ELEGANT_MOLE_TOP_HAT"].passives.Add(dse);
        loadedItems["EMPTY_STAPLER"].passives.Add(sp);
        loadedItems["EMPTY_STAPLER"].passives.Add(irq);
        loadedItems["ENCYCLOPEDIA"].passives.Add(sp);
        loadedItems["ERASABLE_INK_PEN"].passives.Add(sp);
        loadedItems["FASHION_MAGAZINE"].passives.Add(sp);
        loadedItems["FLASHY_COMIC"].passives.Add(sp);
        loadedItems["HOME_CONSOLE"].passives.Add(ev);
        loadedItems["HOME_CONSOLE"].passives.Add(sp);
        loadedItems["JONNYS_SUNGLASSES"].passives.Add(sp);
        loadedItems["JUMPING_SHOES"].passives.Add(dsr);
        loadedItems["NOTEBOOK"].passives.Add(sp);
        loadedItems["OLD_CALENDAR"].passives.Add(sp);
        loadedItems["OLD_CALENDAR"].passives.Add(ev);
        loadedItems["OLD_TOASTER"].passives.Add(cds);
        loadedItems["OVERCHARGED_BATTERY"].passives.Add(sp);
        loadedItems["OVERSIZED_JEANS"].passives.Add(cds);
        loadedItems["PAID_SUBSCRIPTION"].passives.Add(sp);
        loadedItems["PAPER_STACK"].passives.Add(sp);
        loadedItems["PEAR_PHONE"].passives.Add(sp);
        loadedItems["PEN"].passives.Add(sp);
        loadedItems["PENCIL"].passives.Add(sp);
        loadedItems["PENCIL_SHARPENER"].passives.Add(ev);
        loadedItems["PHONE_CHARGER"].passives.Add(sp);
        loadedItems["PHYSICS_GUN"].passives.Add(sp);
        loadedItems["POPULAR_GAME"].passives.Add(cds);
        loadedItems["PRINTER_INK"].passives.Add(sp);
        loadedItems["QUIRKY_HERBS"].passives.Add(sp);
        loadedItems["REPURPOSED_SNEAKERS"].passives.Add(dse);
        loadedItems["ROUTER"].passives.Add(sp);
        loadedItems["SCISSORS"].passives.Add(sp);
        loadedItems["SCREWDRIVER"].passives.Add(ev);
        loadedItems["SHORT_MAGAZINE"].passives.Add(sp);
        loadedItems["SIM_CARD"].passives.Add(ir);
        loadedItems["TIGHT_SHORTS"].passives.Add(cds);
        loadedItems["USED_PENCILCASE"].passives.Add(sp);
        loadedItems["WORLD_MAP"].passives.Add(dse);
        loadedItems["XXL_TSHIRT"].passives.Add(sp);
        loadedItems["YESTERDAY_NEWSPAPER"].passives.Add(sp);
    }


}
