using System;
using System.Diagnostics;
using System.Linq;
using Styx;
using Styx.Combat.CombatRoutine;
using Styx.Helpers;
using Styx.Logic.Combat;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace HighVoltz
{
    public class WaterWalking
    {
        private static readonly Stopwatch _recastSW = new Stopwatch();

        public static bool CanCast
        {
            get
            {
                return AutoAngler.Instance.MySettings.UseWaterWalking &&
                       (SpellManager.HasSpell(1706) || // priest levitate
                        SpellManager.HasSpell(546) || // shaman water walking
                        SpellManager.HasSpell(3714) || // Dk Path of frost
                        Utils.IsItemInBag(8827)); //isItemInBag(8827);
            }
        }

        public static bool IsActive
        {
            get
            {
                // DKs have 2 Path of Frost auras. only one can be stored in WoWAuras at any time. 
                return ObjectManager.Me.Auras.Values.
                           Count(a => (a.SpellId == 11319 || a.SpellId == 1706 || a.SpellId == 546) &&
                                      a.TimeLeft >= new TimeSpan(0, 0, 20)) > 0 ||
                       ObjectManager.Me.HasAura("Path of Frost");
            }
        }

        public static bool Cast()
        {
            bool casted = false;
            if (!IsActive)
            {
                if (_recastSW.IsRunning && _recastSW.ElapsedMilliseconds < 5000)
                    return false;
                _recastSW.Reset();
                _recastSW.Start();
                int waterwalkingSpellID = 0;
                switch (ObjectManager.Me.Class)
                {
                    case WoWClass.Priest:
                        waterwalkingSpellID = 1706;
                        break;
                    case WoWClass.Shaman:
                        waterwalkingSpellID = 546;
                        break;
                    case WoWClass.DeathKnight:
                        waterwalkingSpellID = 3714;
                        break;
                }
                if (SpellManager.CanCast(waterwalkingSpellID))
                {
                    SpellManager.Cast(waterwalkingSpellID);
                    casted = true;
                }
                WoWItem waterPot = Utils.GetIteminBag(8827);
                if (waterPot != null && waterPot.Use())
                {
                    casted = true;
                }
            }
            if (ObjectManager.Me.IsSwimming)
            {
                using (new FrameLock())
                {
                    KeyboardManager.AntiAfk();
                    WoWMovement.Move(WoWMovement.MovementDirection.JumpAscend);
                }
            }
            return casted;
        }
    }
}