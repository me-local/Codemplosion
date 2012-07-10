using System.Diagnostics;
using Styx.Logic.Inventory.Frames.LootFrame;
using TreeSharp;

namespace HighVoltz.Composites
{
    public class LootAction : Action
    {
        private static readonly Stopwatch _lootSw = new Stopwatch();

        public static Stopwatch WaitingForLootSW
        {
            get { return _lootSw; }
        }

        protected override RunStatus Run(object context)
        {
            if (GetLoot())
                return RunStatus.Success;
            else
                return RunStatus.Failure;
        }

        /// <summary>
        /// returns true if waiting for loot or if successfully looted.
        /// </summary>
        /// <returns></returns>
        public static bool GetLoot()
        {
            if (_lootSw.IsRunning && _lootSw.ElapsedMilliseconds < 5000)
            {
                // loot everything.
                if (LootFrame.Instance != null && LootFrame.Instance.IsVisible)
                {
                    for (int i = 0; i < LootFrame.Instance.LootItems; i++)
                    {
                        LootSlotInfo lootInfo = LootFrame.Instance.LootInfo(i);
                        if (AutoAngler.FishCaught.ContainsKey(lootInfo.LootName))
                            AutoAngler.FishCaught[lootInfo.LootName] += (uint) lootInfo.LootQuantity;
                        else
                            AutoAngler.FishCaught.Add(lootInfo.LootName, (uint) lootInfo.LootQuantity);
                    }
                    LootFrame.Instance.LootAll();
                    _lootSw.Reset();
                }
                return true;
            }
            return false;
        }
    }
}