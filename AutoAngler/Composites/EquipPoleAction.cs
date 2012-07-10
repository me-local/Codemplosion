using System.Linq;
using Styx;
using Styx.Logic.BehaviorTree;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using TreeSharp;

namespace HighVoltz.Composites
{
    public class EquipPoleAction : Action
    {
        private readonly LocalPlayer _me = ObjectManager.Me;

        protected override RunStatus Run(object context)
        {
            // equip fishing pole if there's none equipped
            if (_me.Inventory.Equipped.MainHand == null ||
                _me.Inventory.Equipped.MainHand.ItemInfo.WeaponClass != WoWItemWeaponClass.FishingPole)
            {
                if (EquipPole())
                    return RunStatus.Success;
            }
            return RunStatus.Failure;
        }

        private bool EquipPole()
        {
            WoWItem pole = _me.BagItems.FirstOrDefault(i => i.ItemInfo.WeaponClass == WoWItemWeaponClass.FishingPole);
            if (pole != null)
            {
                AutoAngler.Instance.Log("Equipping " + pole.Name);
                // fix for cases where pole is in a fish bag
                //using (new FrameLock())
                //{
                //    if (_me.Inventory.Equipped.MainHand != null)
                //    {
                //        _me.Inventory.Equipped.MainHand.PickUp();
                //        if (_me.Inventory.Backpack.FreeSlots > 0)
                //            Lua.DoString("PutItemInBackpack()");
                //        else
                //            Lua.DoString("for i=1,4 do if GetContainerNumFreeSlots(i) > 0 then PutItemInBag(i) end end");
                //    }
                //    if (_me.Inventory.Equipped.OffHand != null)
                //    {
                //        _me.Inventory.Equipped.OffHand.PickUp();
                //        if (_me.Inventory.Backpack.FreeSlots > 1)
                //            Lua.DoString("PutItemInBackpack()");
                //        else
                //            Lua.DoString("for i=1,4 do if GetContainerNumFreeSlots(i) > 1 then PutItemInBag(i) end end");
                //    }
                Utils.EquipItemByID(pole.Entry);
                //}
                return true;
            }
            AutoAngler.Instance.Err("No fishing pole found");
            TreeRoot.Stop();
            return false;
        }
    }
}