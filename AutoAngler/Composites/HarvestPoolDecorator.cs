using System;
using System.Linq;
using Styx;
using Styx.Logic;
using Styx.Logic.POI;
using Styx.Logic.Profiles;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using TreeSharp;
using System.Reflection;

namespace HighVoltz.Composites
{
    internal class HarvestPoolDecorator : Decorator
    {
        private ulong _lastPoolGuid;

        public HarvestPoolDecorator(Composite child) : base(child) { }

        protected override bool CanRun(object context)
        {
            if (!AutoAngler.Instance.MySettings.Poolfishing || (AutoAngler.FishAtHotspot && StyxWoW.Me.Location.Distance(AutoAngler.CurrentPoint) <= 3))
                return true;
            WoWGameObject pool = ObjectManager.GetObjectsOfType<WoWGameObject>()
                .OrderBy(o => o.Distance)
                .FirstOrDefault(o => o.SubType == WoWGameObjectType.FishingHole && !Blacklist.Contains(o.Guid) &&
                    // Check if we're fishing from specific pools
                                     ((AutoAngler.PoolsToFish.Count > 0 && AutoAngler.PoolsToFish.Contains(o.Entry))
                                      || AutoAngler.PoolsToFish.Count == 0) &&
                    // chaeck if pool is in a blackspot
                                     !IsInBlackspot(o) &&
                    // check if player is near pool
                                     NinjaCheck(o));

            WoWGameObject poiObj = BotPoi.Current != null && BotPoi.Current.Type == PoiType.Harvest
                                       ? (WoWGameObject)BotPoi.Current.AsObject
                                       : null;
            if (pool != null)
            {
                if (poiObj == null || poiObj.Entry != pool.Entry)
                {
                    BotPoi.Current = new BotPoi(pool, PoiType.Harvest);
                    AutoAngler.CycleToNextIfBehind(pool);
                }
                return true;
            }
            return false;
        }

        private bool NinjaCheck(WoWGameObject pool)
        {
            if (pool.Guid == _lastPoolGuid || (pool.Distance2D <= 22 && !ObjectManager.Me.Mounted))
                return true;
            _lastPoolGuid = pool.Guid;
            bool nearbyPlayers = ObjectManager.GetObjectsOfType<WoWPlayer>(false, false).
                Any(p => !p.IsFlying && p.Location.Distance2D(pool.Location) < 20);
            bool fishDaPool = !(!AutoAngler.Instance.MySettings.NinjaNodes && nearbyPlayers);
            if (!fishDaPool)
                Utils.BlacklistPool(pool, TimeSpan.FromMinutes(1), "Another player fishing that pool");
            return fishDaPool;
        }


        private bool IsInBlackspot(WoWGameObject pool)
        {
            if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.Blackspots != null)
            {
                if (
                    ProfileManager.CurrentProfile.Blackspots.Any(
                        blackSpot => blackSpot.Location.Distance2D(pool.Location) <= blackSpot.Radius))
                {
                    AutoAngler.Instance.Log("Ignoring {0} at {1} since it's in a BlackSpot", pool.Name, pool.Location);
                    return true;
                }
            }
            return false;
        }
    }
}