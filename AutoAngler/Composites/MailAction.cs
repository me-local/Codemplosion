using System.Linq;
using Styx;
using Styx.Helpers;
using Styx.Logic;
using Styx.Logic.Inventory.Frames.MailBox;
using Styx.Logic.POI;
using Styx.Logic.Pathing;
using Styx.Logic.Profiles;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using TreeSharp;

namespace HighVoltz.Composites
{
    public class MailAction : Action
    {
        private readonly LocalPlayer _me = ObjectManager.Me;

        protected override RunStatus Run(object context)
        {
            WoWPoint mboxLoc = BotPoi.Current.Location;
            WoWGameObject mailbox = ObjectManager.GetObjectsOfType<WoWGameObject>().
                FirstOrDefault(m => m.SubType == WoWGameObjectType.Mailbox &&
                                    m.Location.Distance(mboxLoc) < 10);
            WoWPoint loc = mailbox != null ? mailbox.Location : mboxLoc;

            if (_me.Location.Distance(loc) > 4)
            {
                if (AutoAngler.Instance.MySettings.Fly)
                    Flightor.MoveTo(WoWMathHelper.CalculatePointFrom(_me.Location, loc, 3));
                else
                {
                    if (!ObjectManager.Me.Mounted && Mount.ShouldMount(loc) && Mount.CanMount())
                        Mount.MountUp(() => loc);
                    Navigator.MoveTo(WoWMathHelper.CalculatePointFrom(_me.Location, loc, 4));
                }
            }
            else
            {
                if (MailFrame.Instance == null || !MailFrame.Instance.IsVisible)
                {
                    if (mailbox == null)
                    {
                        AutoAngler.Instance.Log("No Mailbox found at location {0}. Vendoring instead", loc);
                        Vendor ven = ProfileManager.CurrentOuterProfile.VendorManager.GetClosestVendor();
                        if (ven != null)
                            BotPoi.Current = new BotPoi(ven, PoiType.Repair);
                        else
                            BotPoi.Current = new BotPoi(PoiType.InnKeeper);
                        return RunStatus.Failure;
                    }
                    mailbox.Interact();
                }
                else
                {
                    Vendor ven = ProfileManager.CurrentOuterProfile.VendorManager.GetClosestVendor();
                    if (ven != null)
                    {
// mail all except grey items which we will vendor.
                        MailFrame.Instance.SendMailWithManyAttachments(CharacterSettings.Instance.MailRecipient, 0,
                                                                       _me.BagItems.Where(
                                                                           i => !i.IsSoulbound && !i.IsConjured &&
                                                                                i.Quality != WoWItemQuality.Poor &&
                                                                                !ProtectedItemsManager.Contains(i.Entry))
                                                                           .
                                                                           ToArray());
                        BotPoi.Current = new BotPoi(ven, PoiType.Repair);
                    }
                    else
                    {
                        // mail all since no vender is in profile
                        MailFrame.Instance.SendMailWithManyAttachments(CharacterSettings.Instance.MailRecipient, 0,
                                                                       _me.BagItems.Where(
                                                                           i => !i.IsSoulbound && !i.IsConjured &&
                                                                                !ProtectedItemsManager.Contains(i.Entry))
                                                                           .
                                                                           ToArray());
                        BotPoi.Current = new BotPoi(PoiType.None);
                    }
                }
            }
            return RunStatus.Success;
        }
    }
}