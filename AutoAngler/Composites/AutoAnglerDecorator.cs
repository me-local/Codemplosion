using Styx.Logic.Combat;
using Styx.WoWInternals;
using TreeSharp;

namespace HighVoltz.Composites
{
    internal class AutoAnglerDecorator : Decorator
    {
        public AutoAnglerDecorator(Composite child) : base(child)
        {
        }

        protected override bool CanRun(object context)
        {
            return ObjectManager.Me.IsAlive && !ObjectManager.Me.Combat &&
                   !RoutineManager.Current.NeedRest;
        }
    }
}