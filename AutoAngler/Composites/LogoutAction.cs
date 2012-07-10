using System.Diagnostics;
using System.Threading;
using Styx.Logic;
using Styx.Logic.BehaviorTree;
using Styx.WoWInternals;
using TreeSharp;

namespace HighVoltz.Composites
{
    public class LogoutAction : Action
    {
        protected override RunStatus Run(object context)
        {
            if (ObjectManager.Me.Mounted)
                Mount.Dismount();
            Utils.UseItemByID(6948);
            var hearthSW = new Stopwatch();
            hearthSW.Start();
            // since I'm logging out lets just abuse sleep anyways :D
            while (hearthSW.ElapsedMilliseconds < 20000)
            {
                // damn.. we got something beating on us... 
                if (ObjectManager.Me.Combat)
                    return RunStatus.Success;
                Thread.Sleep(100); // I feel so teribad... not!
            }
            AutoAngler.Instance.Log("Logging out");
            Lua.DoString("Logout()");
            TreeRoot.Stop();
            return RunStatus.Success;
        }
    }
}