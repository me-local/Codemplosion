using System;
using System.Diagnostics;
using System.Linq;
using Styx;
using Styx.Logic;
using Styx.Logic.POI;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace HighVoltz
{
    class Utils
    {
        private static readonly LocalPlayer Me = ObjectManager.Me;
        private static uint _ping = Lua.GetReturnVal<uint>("return GetNetStats()", 3);
        private static readonly Stopwatch PingSW = new Stopwatch();

        public static bool IsLureOnPole
        {
            get
            {
                //if poolfishing, dont need lure say we have one
                if (AutoAngler.Instance.MySettings.Poolfishing && !AutoAngler.FishAtHotspot)
                    return true;
                return Lua.GetReturnValues("return GetWeaponEnchantInfo()")[0] == "1";
            }
        }

        /// <summary>
        /// Returns WoW's ping, refreshed every 30 seconds.
        /// </summary>
        public static uint WoWPing
        {
            get
            {
                if (!PingSW.IsRunning)
                    PingSW.Start();
                if (PingSW.ElapsedMilliseconds > 30000)
                {
                    _ping = Lua.GetReturnVal<uint>("return GetNetStats()", 3);
                    PingSW.Reset();
                    PingSW.Start();
                }
                return _ping;
            }
        }

        public static bool IsItemInBag(uint entry)
        {
            return Me.BagItems.Any(i => i.Entry == entry);
        }

        public static WoWItem GetIteminBag(uint entry)
        {
            return ObjectManager.Me.BagItems.FirstOrDefault(i => i.Entry == entry);
        }

        public static void EquipWeapon()
        {
            bool is2Hand = false;
            // equip right hand weapon
            uint mainHandID = AutoAngler.Instance.MySettings.MainHand;
            WoWItem mainHand = ObjectManager.Me.Inventory.Equipped.MainHand;
            if (mainHand == null || (mainHand.Entry != mainHandID && Utils.IsItemInBag(mainHandID)))
            {
                is2Hand = Utils.GetIteminBag(AutoAngler.Instance.MySettings.MainHand).ItemInfo.InventoryType ==
                          InventoryType.TwoHandWeapon;
                Utils.EquipItemByID(AutoAngler.Instance.MySettings.MainHand);
            }

            // equip left hand weapon
            uint offhandID = AutoAngler.Instance.MySettings.OffHand;
            WoWItem offhand = ObjectManager.Me.Inventory.Equipped.OffHand;

            if ((!is2Hand && offhandID > 0 &&
                 (offhand == null || (offhand.Entry != offhandID && Utils.IsItemInBag(offhandID)))))
            {
                Utils.EquipItemByID(AutoAngler.Instance.MySettings.OffHand);
            }
        }

        public static void UseItemByID(int id)
        {
            Lua.DoString("UseItemByName(\"" + id + "\")");
        }

        public static void EquipItemByName(String name)
        {
            Lua.DoString("EquipItemByName (\"" + name + "\")");
        }

        public static void EquipItemByID(uint id)
        {
            Lua.DoString("EquipItemByName ({0})", id);
        }

        public static void BlacklistPool(WoWGameObject pool, TimeSpan time, string reason)
        {
            Blacklist.Add(pool.Guid, time);
            AutoAngler.Instance.Log("Blacklisting {0} for {1} Reason: {2}", pool.Name, time, reason);
            BotPoi.Current = new BotPoi(PoiType.None);
        }
    }
}