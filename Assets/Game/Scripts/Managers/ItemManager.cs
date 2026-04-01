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


        loadedItems["ALIEN_HEART"].passives.Add(new DeliverySpeedExtraPassive());
        loadedItems["ARTIST_BRUSH"].passives.Add(new ItemReplacerPassive());
        loadedItems["BLEEDING_MEAT_CUBE"].passives.Add(new DeliverySpeedExtraPassive());
        loadedItems["BLUE_OVERALLS"].passives.Add(new SynergyPassive());
        loadedItems["BOOTLEG_GAME"].passives.Add(new SynergyPassive());
        loadedItems["BRANDNEW_PRINTER"].passives.Add(new SynergyPassive());
        loadedItems["BULKY_LETTER"].passives.Add(new DeliverySpeedRequirementPassive());
        loadedItems["CHEAP_CHIP"].passives.Add(new SynergyPassive());
        loadedItems["CLOWN_COSTUME"].passives.Add(new SynergyPassive());
        loadedItems["COMFORTABLE_JACKET"].passives.Add(new SynergyPassive());
        loadedItems["COMPRESSOR_TOOL"].passives.Add(new SlotReducerPassive());
        loadedItems["COMPRESSOR_TOOL"].passives.Add(new SynergyPassive());
        loadedItems["CORRECTION_TAPE"].passives.Add(new SynergyPassive());
        loadedItems["DECK_OF_CARDS"].passives.Add(new RandomValuePassive());
        loadedItems["DANGEROUS_VENOM"].passives.Add(new SynergyPassive());
        loadedItems["ELEGANT_MOLE_TOP_HAT"].passives.Add(new DeliverySpeedExtraPassive());
        loadedItems["EMPTY_STAPLER"].passives.Add(new SynergyPassive());
        loadedItems["EMPTY_STAPLER"].passives.Add(new ItemRequirementPassive());
        loadedItems["ENCYCLOPEDIA"].passives.Add(new SynergyPassive());
        loadedItems["ERASABLE_INK_PEN"].passives.Add(new SynergyPassive());
        loadedItems["FASHION_MAGAZINE"].passives.Add(new SynergyPassive());
        loadedItems["FLASHY_COMIC"].passives.Add(new SynergyPassive());
        loadedItems["HOME_CONSOLE"].passives.Add(new ExtraValuePassive());
        loadedItems["HOME_CONSOLE"].passives.Add(new SynergyPassive());
        loadedItems["JONNYS_SUNGLASSES"].passives.Add(new SynergyPassive());
        loadedItems["JUMPING_SHOES"].passives.Add(new DeliverySpeedRequirementPassive());
        loadedItems["NOTEBOOK"].passives.Add(new SynergyPassive());
        loadedItems["OLD_CALENDAR"].passives.Add(new SynergyPassive());
        loadedItems["OLD_CALENDAR"].passives.Add(new ExtraValuePassive());
        loadedItems["OLD_TOASTER"].passives.Add(new ChangeDeliverySpeedPassive());
        loadedItems["OVERCHARGED_BATTERY"].passives.Add(new SynergyPassive());
        loadedItems["OVERSIZED_JEANS"].passives.Add(new ChangeDeliverySpeedPassive());
        loadedItems["PAID_SUBSCRIPTION"].passives.Add(new SynergyPassive());
        loadedItems["PAPER_STACK"].passives.Add(new SynergyPassive());
        loadedItems["PEAR_PHONE"].passives.Add(new SynergyPassive());
        loadedItems["PEN"].passives.Add(new SynergyPassive());
        loadedItems["PENCIL"].passives.Add(new SynergyPassive());
        loadedItems["PENCIL_SHARPENER"].passives.Add(new ExtraValuePassive());
        loadedItems["PHONE_CHARGER"].passives.Add(new SynergyPassive());
        loadedItems["PHYSICS_GUN"].passives.Add(new SynergyPassive());
        loadedItems["POPULAR_GAME"].passives.Add(new ChangeDeliverySpeedPassive());
        loadedItems["PRINTER_INK"].passives.Add(new SynergyPassive());
        loadedItems["QUIRKY_HERBS"].passives.Add(new SynergyPassive());
        loadedItems["REPURPOSED_SNEAKERS"].passives.Add(new DeliverySpeedExtraPassive());
        loadedItems["ROUTER"].passives.Add(new SynergyPassive());
        loadedItems["SCISSORS"].passives.Add(new SynergyPassive());
        loadedItems["SCREWDRIVER"].passives.Add(new ExtraValuePassive());
        loadedItems["SHORT_MAGAZINE"].passives.Add(new SynergyPassive());
        loadedItems["SIM_CARD"].passives.Add(new ItemReplacerPassive());
        loadedItems["TIGHT_SHORTS"].passives.Add(new ChangeDeliverySpeedPassive());
        loadedItems["USED_PENCILCASE"].passives.Add(new SynergyPassive());
        loadedItems["WORLD_MAP"].passives.Add(new DeliverySpeedExtraPassive());
        loadedItems["XXL_TSHIRT"].passives.Add(new SynergyPassive());
        loadedItems["YESTERDAY_NEWSPAPER"].passives.Add(new SynergyPassive());
    }


}
