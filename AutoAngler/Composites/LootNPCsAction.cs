using System.Linq;
using Styx.Logic.Inventory.Frames.LootFrame;
using Styx.Logic.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using TreeSharp;

namespace HighVoltz.Composites
{
    public class LootNPCsAction : Action
    {
        protected override RunStatus Run(object context)
        {
            WoWUnit lootableNpc = ObjectManager.GetObjectsOfType<WoWUnit>(true)
                .OrderBy(unit => unit.Distance)
                .FirstOrDefault(unit => unit.Lootable);
            if (lootableNpc != null)
            {
                LootNPC(lootableNpc);
                return RunStatus.Success;
            }
            return RunStatus.Failure;
        }

        private void LootNPC(WoWUnit lootableUnit)
        {
            if (lootableUnit.WithinInteractRange)
            {
                if (LootFrame.Instance != null && LootFrame.Instance.IsVisible)
                {
                    // record all loot info..
                    for (int i = 0; i < LootFrame.Instance.LootItems; i++)
                    {
                        LootSlotInfo lootInfo = LootFrame.Instance.LootInfo(i);
                        if (AutoAngler.FishCaught.ContainsKey(lootInfo.LootName))
                            AutoAngler.FishCaught[lootInfo.LootName] += (uint) lootInfo.LootQuantity;
                        else
                            AutoAngler.FishCaught.Add(lootInfo.LootName, (uint) lootInfo.LootQuantity);
                    }
                    LootFrame.Instance.LootAll();
                }
                else
                    lootableUnit.Interact();
            }
            else
                Navigator.MoveTo(lootableUnit.Location);
        }
    }
}