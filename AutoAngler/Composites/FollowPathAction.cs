using System;
using Styx;
using Styx.Combat.CombatRoutine;
using Styx.Helpers;
using Styx.Logic;
using Styx.Logic.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using TreeSharp;
using Action = TreeSharp.Action;

namespace HighVoltz.Composites
{
    public class FollowPathAction : Action
    {

        private readonly LocalPlayer _me = ObjectManager.Me;
        private readonly AutoAnglerSettings _settings = AutoAngler.Instance.MySettings;

        protected override RunStatus Run(object context)
        {
            if (LootAction.GetLoot())
                return RunStatus.Success;
            //  dks can refresh water walking while flying around.
            if (AutoAngler.Instance.MySettings.UseWaterWalking &&
                ObjectManager.Me.Class == WoWClass.DeathKnight && !WaterWalking.IsActive)
            {
                WaterWalking.Cast();
            }
            if (AutoAngler.CurrentPoint == WoWPoint.Zero )
                return RunStatus.Failure;
            if (AutoAngler.FishAtHotspot && StyxWoW.Me.Location.Distance(AutoAngler.CurrentPoint) <= 3)
            {
                return RunStatus.Failure;
            }
            float speed = ObjectManager.Me.MovementInfo.CurrentSpeed;
            float modifier = _settings.Fly ? 4f : 2f;
            float precision = speed > 7 ? (modifier*speed)/7f : modifier;
            if (ObjectManager.Me.Location.Distance(AutoAngler.CurrentPoint) <= precision)
                AutoAngler.CycleToNextPoint();
            if (_settings.Fly)
            {
                if (_me.IsSwimming)
                {
                    if (_me.GetMirrorTimerInfo(MirrorTimerType.Breath).CurrentTime > 0)
                        WoWMovement.Move(WoWMovement.MovementDirection.JumpAscend);
                    else if (_me.MovementInfo.IsAscending || _me.MovementInfo.JumpingOrShortFalling)
                        WoWMovement.MoveStop(WoWMovement.MovementDirection.JumpAscend);
                }
                if (!ObjectManager.Me.Mounted)
                    Flightor.MountHelper.MountUp();
                Flightor.MoveTo(AutoAngler.CurrentPoint);
            }
            else
            {
                if (!ObjectManager.Me.Mounted && Mount.ShouldMount(AutoAngler.CurrentPoint) && Mount.CanMount())
                    Mount.MountUp(() => AutoAngler.CurrentPoint);
                Navigator.MoveTo(AutoAngler.CurrentPoint);
            }
            return RunStatus.Success;
        }

 
    }
}