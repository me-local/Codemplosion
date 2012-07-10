//!CompilerOption:Optimize:On
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using CommonBehaviors.Actions;
using HighVoltz.Composites;
using Levelbot.Actions.Death;
using Levelbot.Decorators.Death;
using Styx;
using Styx.Helpers;
using Styx.Logic;
using Styx.Logic.BehaviorTree;
using Styx.Logic.Combat;
using Styx.Logic.POI;
using Styx.Logic.Pathing;
using Styx.Logic.Profiles;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using TreeSharp;
using Action = TreeSharp.Action;

namespace HighVoltz
{
    public enum PathingType
    {
        Circle,
        Bounce
    }

    public class AutoAngler : BotBase
    {
        public static readonly List<uint> PoolsToFish = new List<uint>();
        private static DateTime _botStartTime;
        public static List<WoWPoint> WayPoints = new List<WoWPoint>();
        private static int _lastUkTagCallTime;
        private static int _currentIndex;
        public readonly Version Version = new Version(2, new Svn().Revision);
        private readonly LocalPlayer _me = ObjectManager.Me;
        public static readonly string BotPath = Logging.ApplicationPath + @"\Bots\" + "AutoAngler2";
        private AutoAnglerSettings _settings;

        public AutoAngler()
        {
            Instance = this;
            BotEvents.Profile.OnNewOuterProfileLoaded += Profile_OnNewOuterProfileLoaded;
            Profile.OnUnknownProfileElement += Profile_OnUnknownProfileElement;
        }

        public static Dictionary<string, uint> FishCaught { get; private set; }
        public static AutoAngler Instance { get; private set; }

        public AutoAnglerSettings MySettings
        {
            get
            {
                return _settings ?? (_settings = new AutoAnglerSettings(
                                                     String.Format("{0}\\Settings\\AutoAngler\\AutoAngler-{1}",
                                                                   Logging.ApplicationPath, _me.Name)));
            }
        }

        public static WoWPoint CurrentPoint
        {
            get
            {
                return WayPoints != null && WayPoints.Count > 0
                           ? WayPoints[_currentIndex]
                           : WoWPoint.Zero;
            }
        }

        public static bool FishAtHotspot { get; private set; }
        #region overrides

        private PrioritySelector _root;

        public override string Name
        {
            get { return "AutoAngler"; }
        }

        public override PulseFlags PulseFlags
        {
            get { return PulseFlags.All; }
        }

        private DateTime _pulseTimestamp;
        public override Composite Root
        {
            get
            {
                return _root ?? (_root = new PrioritySelector(
                    new Action(ctx =>
                                   {
                                       var pulseTime = DateTime.Now - _pulseTimestamp;
                                       if (pulseTime >= TimeSpan.FromSeconds(3))
                                       {
                                           Err("Warning: It took {0} seconds to pulse.\nThis can cause missed bites. To fix try disabling all plugins", pulseTime.TotalSeconds);
                                       }
                                       _pulseTimestamp = DateTime.Now;
                                       return RunStatus.Failure;
                                   }),
                    // Is bot dead? if so, release and run back to corpse
                                             new Decorator(c => !_me.IsAlive,
                                                           new PrioritySelector(
                                                               new DecoratorNeedToRelease(new ActionReleaseFromCorpse()),
                                                               new DecoratorNeedToMoveToCorpse(new ActionMoveToCorpse()),
                                                               new DecoratorNeedToTakeCorpse(new ActionRetrieveCorpse()),
                                                               new ActionSuceedIfDeadOrGhost()
                                                               )),
                    // If bot is in combat call the CC routine
                                             new Decorator(c => _me.Combat && !_me.IsFlying,
                                                 new PrioritySelector(  // equip weapons since we're in combat.
                                                           new Decorator(ret => StyxWoW.Me.Inventory.Equipped.MainHand == null
                                                                               || StyxWoW.Me.Inventory.Equipped.MainHand.ItemInfo.WeaponClass == WoWItemWeaponClass.FishingPole,
                                                                               new Action(ret => Utils.EquipWeapon())),
                                                           // reset the 'MoveToPool' timer when in combat. 
                                                           new Decorator(ret => BotPoi.Current != null && BotPoi.Current.Type == PoiType.Harvest, 
                                                               new Action(ret => {
                                                                   MoveToPoolAction.MoveToPoolSW.Reset();
                                                                   MoveToPoolAction.MoveToPoolSW.Start();
                                                                   return RunStatus.Failure; // move on down to the next behavior.
                                                               })),
                                                           Bots.Grind.LevelBot.CreateCombatBehavior()

                                            )),
                    // If bot needs to rest then call the CC rest behavior
                                             new Decorator(
                                                 c =>
                                                 RoutineManager.Current.NeedRest && !ObjectManager.Me.IsCasting &&
                                                 !_me.IsFlying,
                                                 new PrioritySelector(
                    // if Rest Behavior exists use it..
                                                     new Decorator(c => RoutineManager.Current.RestBehavior != null,
                                                                   new PrioritySelector(
                                                                       new Action(c =>
                                                                                      {
                                                                                          // reset the autoBlacklist timer since we're stoping to rest.
                                                                                          if (BotPoi.Current != null &&
                                                                                              BotPoi.Current.Type ==
                                                                                              PoiType.Harvest)
                                                                                          {
                                                                                              MoveToPoolAction.MoveToPoolSW.Reset();
                                                                                              MoveToPoolAction.MoveToPoolSW.Start();
                                                                                          }
                                                                                          return RunStatus.Failure;
                                                                                      }),
                                                                       RoutineManager.Current.RestBehavior
                                                                       )),
                    // else call legacy Rest() method
                                                     new Action(c =>
                                                                    {
                                                                        // reset the autoBlacklist timer since we're stoping to rest.
                                                                        if (BotPoi.Current != null &&
                                                                            BotPoi.Current.Type == PoiType.Harvest)
                                                                        {
                                                                            MoveToPoolAction.MoveToPoolSW.Reset();
                                                                            MoveToPoolAction.MoveToPoolSW.Start();
                                                                        }
                                                                        RoutineManager.Current.Rest();
                                                                    })
                                                     )),
                    // mail and repair if bags are full or items have low durability. logout if profile doesn't have mailbox and vendor.
                                             new Decorator(c => _me.BagsFull || _me.DurabilityPercent <= 0.2 &&
                                                                (BotPoi.Current == null ||
                                                                 BotPoi.Current.Type != PoiType.Mail ||
                                                                 BotPoi.Current.Type != PoiType.Repair ||
                                                                 BotPoi.Current.Type != PoiType.InnKeeper),
                                                           new Action(c =>
                                                                          {
                                                                              if (ProfileManager.CurrentOuterProfile !=
                                                                                  null &&
                                                                                  ProfileManager.CurrentOuterProfile.
                                                                                      MailboxManager != null)
                                                                              {
                                                                                  Mailbox mbox =
                                                                                      ProfileManager.CurrentOuterProfile
                                                                                          .MailboxManager.
                                                                                          GetClosestMailbox();
                                                                                  if (mbox != null &&
                                                                                      !String.IsNullOrEmpty(
                                                                                          CharacterSettings.Instance.
                                                                                              MailRecipient))
                                                                                      BotPoi.Current = new BotPoi(mbox);
                                                                                  else
                                                                                  {
                                                                                      Vendor ven =
                                                                                          ProfileManager.CurrentOuterProfile.
                                                                                              VendorManager.
                                                                                              GetClosestVendor();
                                                                                      if (ven != null)
                                                                                          BotPoi.Current =
                                                                                              new BotPoi(ven,
                                                                                                         PoiType.Repair);
                                                                                      else
                                                                                          // we'll use this POI to hearth+Logout...
                                                                                          BotPoi.Current =
                                                                                              new BotPoi(
                                                                                                  PoiType.InnKeeper);
                                                                                  }
                                                                              }
                                                                              return RunStatus.Failure;
                                                                          })),
                                             new Decorator(
                                                 c => BotPoi.Current != null && BotPoi.Current.Type == PoiType.Mail,
                                                 new MailAction()),
                                             new Decorator(
                                                 c => BotPoi.Current != null && BotPoi.Current.Type == PoiType.Repair,
                                                 new VendorAction()),
                                             new Decorator(
                                                 c => BotPoi.Current != null && BotPoi.Current.Type == PoiType.InnKeeper,
                                                 new LogoutAction()),
                    // loot Any dead lootable NPCs if setting is enabled.
                                             new Decorator(c => AutoAnglerSettings.Instance.LootNPCs,
                                                           new LootNPCsAction()),
                                             new AutoAnglerDecorator(
                                                 new PrioritySelector(
                                                     new HarvestPoolDecorator(
                                                         new PrioritySelector(
                                                             new LootAction(),
                                                             new WaterWalkingAction(),
                                                             new MoveToPoolAction(),
                                                             new EquipPoleAction(),
                                                             new ApplyLureAction(),
                                                             new Decorator(c => FishAtHotspot,
                                                                 new FaceWaterAction()),
                                                             new FishAction()
                                                             ))
                                                     )),
                    // follow the path...
                                             new FollowPathAction()
                                             ));
            }
        }

        public override bool IsPrimaryType
        {
            get { return true; }
        }

        public override Form ConfigurationForm
        {
            get { return new MainForm(); }
        }

        public override void Initialize()
        {
            try
            {
                WoWItem mainhand = (MySettings.MainHand != 0 ? Utils.GetIteminBag(MySettings.MainHand) : null) ??
                                   FindMainHand();
                WoWItem offhand = MySettings.OffHand != 0 ? Utils.GetIteminBag(MySettings.OffHand) : null;
                if (((mainhand != null && mainhand.ItemInfo.InventoryType != InventoryType.TwoHandWeapon) ||
                     mainhand == null) && offhand == null)
                {
                    offhand = FindOffhand();
                }
                if (mainhand != null)
                    Log("Using {0} for mainhand weapon", mainhand.Name);
                if (offhand != null)
                    Log("Using {0} for offhand weapon", offhand.Name);
                if (!MySettings.Poolfishing)
                {
                    if (!String.IsNullOrEmpty(ProfileManager.XmlLocation))
                    {
                        MySettings.LastLoadedProfile = ProfileManager.XmlLocation;
                        MySettings.Save();
                    }
                    ProfileManager.LoadEmpty();
                }
                else if (ProfileManager.CurrentProfile == null && !String.IsNullOrEmpty(MySettings.LastLoadedProfile) &&
                         File.Exists(MySettings.LastLoadedProfile))
                {
                    ProfileManager.LoadNew(MySettings.LastLoadedProfile);
                }
                // check for Autoangler updates
                new Thread(Updater.CheckForUpdate) { IsBackground = true }.Start();
            }
            catch (Exception ex)
            {
                Logging.WriteException(ex);
            }
        }

        #endregion

        public override void Start()
        {
            _botStartTime = DateTime.Now;
            _pulseTimestamp = DateTime.Now;
            FishCaught = new Dictionary<string, uint>();
        }

        private DateTime _weaponCheckTimeStamp = DateTime.Now;

        public override void Stop()
        {
            Log("Equipping weapons");
            Utils.EquipWeapon();
            Log("In {0} days, {1} hours and {2} minutes we have caught",
                (DateTime.Now - _botStartTime).Days,
                (DateTime.Now - _botStartTime).Hours,
                (DateTime.Now - _botStartTime).Minutes);
            foreach (var kv in FishCaught)
            {
                Log("{0} x{1}", kv.Key, kv.Value);
            }
        }

        #region Profile

        private void Profile_OnNewOuterProfileLoaded(BotEvents.Profile.NewProfileLoadedEventArgs args)
        {
            try
            {
                LoadWayPoints(ProfileManager.CurrentProfile);
            }
            catch (Exception ex)
            {
                Logging.WriteException(ex);
            }
        }

        public static void Profile_OnUnknownProfileElement(object sender, UnknownProfileElementEventArgs e)
        {
            if (e.Element.Name == "FishingSchool")
            {
                // hackish way to clear my list of pool before loading new profile... wtb OnNewOuterProfileLoading event
                if (Environment.TickCount - _lastUkTagCallTime > 4000)
                    PoolsToFish.Clear();
                _lastUkTagCallTime = Environment.TickCount;
                XAttribute entryAttrib = e.Element.Attribute("Entry");
                if (entryAttrib != null)
                {
                    uint entry;
                    UInt32.TryParse(entryAttrib.Value, out entry);
                    if (!PoolsToFish.Contains(entry))
                    {
                        PoolsToFish.Add(entry);
                        XAttribute nameAttrib = e.Element.Attribute("Name");
                        if (nameAttrib != null)
                            Instance.Log("Adding Pool Entry: {0} to the list of pools to fish from",
                                         nameAttrib.Value);
                        else
                            Instance.Log("Adding Pool Entry: {0} to the list of pools to fish from", entry);
                    }
                }
                else
                {
                    Instance.Err(
                        "<FishingSchool> tag must have the 'Entry' Attribute, e.g <FishingSchool Entry=\"202780\"/>\nAlso supports 'Name' attribute but only used for display purposes");
                }
                e.Handled = true;
            }
            else if (e.Element.Name == "Pathing")
            {
                XAttribute typeAttrib = e.Element.Attribute("Type");
                if (typeAttrib != null)
                {
                    Instance.MySettings.PathingType = (PathingType)
                                                      Enum.Parse(typeof(PathingType), typeAttrib.Value, true);
                    Instance.Log("Setting Pathing Type to {0} Mode",
                                 Instance.MySettings.PathingType);
                }
                else
                {
                    Instance.Err(
                        "<Pathing> tag must have the 'Type' Attribute, e.g <Pathing Type=\"Circle\"/>");
                }
                e.Handled = true;
            }
        }

        private void LoadWayPoints(Profile profile)
        {
            WayPoints.Clear();
            if (profile != null && profile.GrindArea != null)
            {
                if (profile.GrindArea.Hotspots != null)
                {
                    WayPoints = profile.GrindArea.Hotspots.ConvertAll(hs => hs.Position);
                    WoWPoint closestPoint =
                        WayPoints.OrderBy(u => u.Distance(ObjectManager.Me.Location)).FirstOrDefault();
                    _currentIndex = WayPoints.FindIndex(w => w == closestPoint);
                }
                else
                    WayPoints = new List<WoWPoint>();
                FishAtHotspot = WayPoints.Count == 1;
            }
        }

        public static void CycleToNextPoint()
        {
            if (WayPoints != null)
            {
                if (_currentIndex >= WayPoints.Count - 1)
                {
                    if (Instance.MySettings.PathingType == PathingType.Bounce)
                    {
                        WayPoints.Reverse();
                        _currentIndex = 1;
                    }
                    else
                        _currentIndex = 0;
                }
                else
                    _currentIndex++;
            }
        }

        private static WoWPoint GetNextWayPoint()
        {
            int i = _currentIndex + 1;
            if (i >= WayPoints.Count)
            {
                if (Instance.MySettings.PathingType == PathingType.Bounce)
                    i = WayPoints.Count - 2;
                else
                    i = 0;
            }
            if (WayPoints != null && i < WayPoints.Count)
                return WayPoints[i];
            return WoWPoint.Zero;
        }

        //if pool is between CurrentPoint and NextPoint then cycle to nextPoint
        public static void CycleToNextIfBehind(WoWGameObject pool)
        {
            WoWPoint cp = CurrentPoint;
            WoWPoint point = GetNextWayPoint();
            point = new WoWPoint(point.X - cp.X, point.Y - cp.Y, 0);
            point.Normalize();
            float angle = WoWMathHelper.NormalizeRadian((float)Math.Atan2(point.Y, point.X - 1));
            if (WoWMathHelper.IsFacing(CurrentPoint, angle, pool.Location, 3.141593f) &&
                CurrentPoint != WayPoints[WayPoints.Count - 1])
            {
                CycleToNextPoint();
            }
        }
        #endregion

        private WoWItem FindMainHand()
        {
            WoWItem mainHand = _me.Inventory.Equipped.MainHand;
            if (mainHand == null || mainHand.ItemInfo.WeaponClass == WoWItemWeaponClass.FishingPole)
            {
                mainHand = _me.CarriedItems.OrderByDescending(u => u.ItemInfo.Level).
                    FirstOrDefault(i => i.IsSoulbound && (i.ItemInfo.InventoryType == InventoryType.WeaponMainHand ||
                                                          i.ItemInfo.InventoryType == InventoryType.TwoHandWeapon) &&
                                        _me.CanEquipItem(i));
                if (mainHand != null)
                {
                    MySettings.MainHand = mainHand.Entry;
                }
                else
                    Err("Unable to find a mainhand weapon to swap to when in combat");
            }
            else
                MySettings.MainHand = mainHand.Entry;
            MySettings.Save();
            return mainHand;
        }

        // scans bags for offhand weapon if mainhand isn't 2h and none are equipped and uses the highest ilvl one
        private WoWItem FindOffhand()
        {
            WoWItem offHand = _me.Inventory.Equipped.OffHand;
            if (offHand == null)
            {
                offHand = _me.CarriedItems.OrderByDescending(u => u.ItemInfo.Level).
                    FirstOrDefault(i => i.IsSoulbound && (i.ItemInfo.InventoryType == InventoryType.WeaponOffHand ||
                                                          i.ItemInfo.InventoryType == InventoryType.Weapon ||
                                                          i.ItemInfo.InventoryType == InventoryType.Shield) &&
                                        MySettings.MainHand != i.Entry &&
                                        _me.CanEquipItem(i));
                if (offHand != null)
                {
                    MySettings.OffHand = offHand.Entry;
                }
                else
                    Err("Unable to find an offhand weapon to swap to when in combat");
            }
            else
                MySettings.OffHand = offHand.Entry;
            MySettings.Save();
            return offHand;
        }

        public void Log(string format, params object[] args)
        {
            Logging.Write(Color.DodgerBlue, String.Format("AutoAngler[{0}]: {1}", Version, format), args);
        }

        public void Err(string format, params object[] args)
        {
            Logging.Write(Color.Red, String.Format("AutoAngler[{0}]: {1}", Version, format), args);
        }

        public void Debug(string format, params object[] args)
        {
            Logging.WriteDebug(Color.DodgerBlue, String.Format("AutoAngler[{0}]: {1}", Version, format), args);
        }

        public static string R = @"";
    }
}