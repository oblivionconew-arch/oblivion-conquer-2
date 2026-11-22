using Conquer_Online_Server.Game;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Conquer_Online_Server.Network.GamePackets;
using System.Threading;
using System.Threading.Generic;
using Conquer_Online_Server.Network.Sockets;
using Conquer_Online_Server.Game.ConquerStructures;
using Conquer_Online_Server.Game.ConquerStructures.Society;
using Conquer_Online_Server.Client;
using System.Drawing;

namespace Conquer_Online_Server
{
    public class World
    {
        public static StaticPool GenericThreadPool;
        public static StaticPool ReceivePool, SendPool;
        public TimerRule<GameClient> Buffers, Characters, AutoAttack, Companions, Prayer;
        public TimerRule<ClientWrapper> ConnectionReceive, ConnectionReview, ConnectionSend;

        public const uint
            NobilityMapBase = 700,
            ClassPKMapBase = 1730;

        public List<KillTournament> Tournaments;
        public PoleDomination PoleDomination;
        public SteedRace SteedRace;
        public CaptureTheFlag CTF;
        public bool PureLand, DemonBox, MonthlyPKWar;
        private bool ClanWarAI;

        public World()
        {
            GenericThreadPool = new StaticPool(32).Run();
            ReceivePool = new StaticPool(128).Run();
            SendPool = new StaticPool(32).Run();
        }

        public void Init()
        {
            Buffers = new TimerRule<GameClient>(BuffersCallback, 1000, ThreadPriority.BelowNormal);
            Characters = new TimerRule<GameClient>(CharactersCallback, 1000, ThreadPriority.BelowNormal);
            AutoAttack = new TimerRule<GameClient>(AutoAttackCallback, 1000, ThreadPriority.BelowNormal);
            Companions = new TimerRule<GameClient>(CompanionsCallback, 1000, ThreadPriority.BelowNormal);
            Prayer = new TimerRule<GameClient>(PrayerCallback, 1000, ThreadPriority.BelowNormal);
            ConnectionReview = new TimerRule<ClientWrapper>(connectionReview, 60000, ThreadPriority.Lowest);
            ConnectionReceive = new TimerRule<ClientWrapper>(connectionReceive, 1);
            ConnectionSend = new TimerRule<ClientWrapper>(connectionSend, 1);

            Subscribe(WorldTournaments, 1000);
            Subscribe(ServerFunctions, 5000);
            Subscribe(ArenaFunctions, 1000, ThreadPriority.AboveNormal);
            Subscribe(TeamArenaFunctions, 1000, ThreadPriority.AboveNormal);
            Subscribe(ChampionFunctions, 1000, ThreadPriority.AboveNormal);
        }

        public void CreateTournaments()
        {
            var map = Kernel.Maps[700];
            Tournaments = new List<KillTournament>();

            //Tournaments.Add(new KillTournament(map.MakeDynamicMap().ID, WeekDay.Tuesday, 1, 05,
            //    (client) => { client.Entity.ConquerPoints += 5000; }, "Nobility Tournament (Kings)", (p) => { return p.Entity.NobilityRank == NobilityRank.King; }));
           // Tournaments.Add(new KillTournament(map.MakeDynamicMap().ID, WeekDay.Tuesday, 1, 05,
          //      (client) => { client.Entity.ConquerPoints += 5000; }, "Nobility Tournament (Princes)", (p) => { return p.Entity.NobilityRank == NobilityRank.Prince; }));
           // Tournaments.Add(new KillTournament(map.MakeDynamicMap().ID, WeekDay.Tuesday, 1, 05,
           //     (client) => { client.Entity.ConquerPoints += 5000; }, "Nobility Tournament (Dukes)", (p) => { return p.Entity.NobilityRank == NobilityRank.Duke; }));
            //Tournaments.Add(new KillTournament(map.MakeDynamicMap().ID, WeekDay.Tuesday, 1, 05,
             //   (client) => { client.Entity.ConquerPoints += 5000; }, "Nobility Tournament (Earl)", (p) => { return p.Entity.NobilityRank == NobilityRank.Earl; }));
           // Tournaments.Add(new KillTournament(map.MakeDynamicMap().ID, WeekDay.Tuesday, 14, 0,
            //    (client) => { client.Entity.ConquerPoints += 5000; }, "Nobility Tournament (Kings)", (p) => { return p.Entity.NobilityRank == NobilityRank.King; }));
            //Tournaments.Add(new KillTournament(map.MakeDynamicMap().ID, WeekDay.Tuesday, 14, 0,
           //     (client) => { client.Entity.ConquerPoints += 5000; }, "Nobility Tournament (Princes)", (p) => { return p.Entity.NobilityRank == NobilityRank.Prince; }));
           // Tournaments.Add(new KillTournament(map.MakeDynamicMap().ID, WeekDay.Tuesday, 14, 0,
           //     (client) => { client.Entity.ConquerPoints += 5000; }, "Nobility Tournament (Dukes)", (p) => { return p.Entity.NobilityRank == NobilityRank.Duke; }));
           // Tournaments.Add(new KillTournament(map.MakeDynamicMap().ID, WeekDay.Tuesday, 14, 0,
            //    (client) => { client.Entity.ConquerPoints += 5000; }, "Nobility Tournament (Earl)", (p) => { return p.Entity.NobilityRank == NobilityRank.Earl; }));

            #region Class PK Tournament
            map = Kernel.Maps[1730];
            Tournaments.Add(new KillTournament(map.MakeDynamicMap().ID, WeekDay.Monday, 22, 0,
                (client) =>
                {
                    client.Entity.ConquerPoints += 50000;
                    client.Entity.AddTopStatus(Network.GamePackets.Update.Flags.TopTrojan, DateTime.Now.AddDays(7).AddHours(-1));
                }, "Class PK War (Trojan)", (p) => { return p.Entity.Class >= 10 && p.Entity.Class <= 15; },
                "você pode ir até o classe pk enviador e receber 50000 cps e top."));
            Tournaments.Add(new KillTournament(map.MakeDynamicMap().ID, WeekDay.Monday, 22, 0,
                (client) =>
                {
                    client.Entity.ConquerPoints += 50000;
                    client.Entity.AddTopStatus(Network.GamePackets.Update.Flags.TopWarrior, DateTime.Now.AddDays(7).AddHours(-1));
                }, "Class PK War (Warrior)", (p) => { return p.Entity.Class >= 20 && p.Entity.Class <= 25; },
                "você pode ir até o classe pk enviador e receber 50000 cps e top."));
            Tournaments.Add(new KillTournament(map.MakeDynamicMap().ID, WeekDay.Monday, 22, 0,
                (client) =>
                {
                    client.Entity.ConquerPoints += 50000;
                    client.Entity.AddTopStatus(Network.GamePackets.Update.Flags.TopArcher, DateTime.Now.AddDays(7).AddHours(-1));
                }, "Class PK War (Archer)", (p) => { return p.Entity.Class >= 40 && p.Entity.Class <= 45; },
                "você pode ir até o classe pk enviador e receber 50000 cps e top."));
            Tournaments.Add(new KillTournament(map.MakeDynamicMap().ID, WeekDay.Monday, 22, 0,
                (client) =>
                {
                    client.Entity.ConquerPoints += 50000;
                    client.Entity.AddTopStatus(Network.GamePackets.Update.Flags.TopNinja, DateTime.Now.AddDays(7).AddHours(-1));
                }, "Class PK War (Ninja)", (p) => { return p.Entity.Class >= 50 && p.Entity.Class <= 55; },
                "você pode ir até o classe pk enviador e receber 50000 cps e top."));
            Tournaments.Add(new KillTournament(map.MakeDynamicMap().ID, WeekDay.Monday, 22, 0,
                (client) =>
                {
                    client.Entity.ConquerPoints += 50000;
                    client.Entity.AddTopStatus(Network.GamePackets.Update.Flags2.TopMonk, DateTime.Now.AddDays(7).AddHours(-1));
                }, "Class PK War (Monk)", (p) => { return p.Entity.Class >= 60 && p.Entity.Class <= 65; },
                "você pode ir até o classe pk enviador e receber 50000 cps e top."));
            Tournaments.Add(new KillTournament(map.MakeDynamicMap().ID, WeekDay.Monday, 22, 0,
                (client) =>
                {
                    client.Entity.ConquerPoints += 50000;
                    client.Entity.AddTopStatus(Network.GamePackets.Update.Flags2.TopPirate, DateTime.Now.AddDays(7).AddHours(-1));
                }, "Class PK War (Pirate)", (p) => { return p.Entity.Class >= 70 && p.Entity.Class <= 75; },
                "você pode ir até o classe pk enviador e receber 50000 cps e top."));
            Tournaments.Add(new KillTournament(map.MakeDynamicMap().ID, WeekDay.Monday, 22, 0,
                (client) =>
                {
                    client.Entity.ConquerPoints += 50000;
                    client.Entity.AddTopStatus(Network.GamePackets.Update.Flags.TopWaterTaoist, DateTime.Now.AddDays(7).AddHours(-1));
                }, "Class PK War (Water Taoist)", (p) => { return p.Entity.Class >= 130 && p.Entity.Class <= 135; },
                "você pode ir até o classe pk enviador e receber 50000 cps e top"));
            Tournaments.Add(new KillTournament(map.MakeDynamicMap().ID, WeekDay.Monday, 22, 0,
                (client) =>
                {
                    client.Entity.ConquerPoints += 50000;
                    client.Entity.AddTopStatus(Network.GamePackets.Update.Flags.TopFireTaoist, DateTime.Now.AddDays(7).AddHours(-1));
                }, "Class PK War (Fire Taoist)", (p) => { return p.Entity.Class >= 140 && p.Entity.Class <= 145; },
                "você pode ir até o classe pk enviador e receber 50000 cps e top."));
            #endregion

            PoleDomination = new PoleDomination(25000);
            SteedRace = new SteedRace();

            ElitePKTournament.Create();

            CTF = new CaptureTheFlag();
        }
        public DateTime MonthlyPKDate
        {
            get
            {
                DateTime now = DateTime.Now;
                DateTime month = new DateTime(now.Year, now.Month, 1);
                while (month.DayOfWeek != DayOfWeek.Sunday)
                    month = month.AddDays(1);
                return month;
            }
        }
        public DateTime NextMonthlyPKDate
        {
            get
            {
                DateTime now = DateTime.Now;
                DateTime month = new DateTime(now.Year, now.Month, 1).AddMonths(1);
                while (month.DayOfWeek != DayOfWeek.Sunday)
                    month = month.AddDays(1);
                return month;
            }
        }

        private void connectionReview(ClientWrapper wrapper, int time)
        {
            ClientWrapper.TryReview(wrapper);
        }
        private void connectionReceive(ClientWrapper wrapper, int time)
        {
            ClientWrapper.TryReceive(wrapper);
        }
        private void connectionSend(ClientWrapper wrapper, int time)
        {
            ClientWrapper.TrySend(wrapper);
        }

        public bool Register(GameClient client)
        {
            if (client.TimerSubscriptions == null)
            {
                client.TimerSyncRoot = new object();
                client.TimerSubscriptions = new IDisposable[]
                {
                    Buffers.Add(client),
                    Characters.Add(client),
                    AutoAttack.Add(client),
                    Companions.Add(client),
                    Prayer.Add(client),
                };
                return true;
            }
            return false;
        }
        public void Unregister(GameClient client)
        {
            if (client.TimerSubscriptions == null) return;
            lock (client.TimerSyncRoot)
            {
                if (client.TimerSubscriptions != null)
                {
                    foreach (var timer in client.TimerSubscriptions)
                        timer.Dispose();
                    client.TimerSubscriptions = null;
                }
            }
        }
        private bool Valid(GameClient client)
        {
            if (!client.Socket.Alive || client.Entity == null)
            {
                client.Disconnect();
                return false;
            }
            return true;
        }

        private void BuffersCallback(GameClient client, int time)
        {
            if (!Valid(client)) return;
            Time32 Now = new Time32(time);

            #region Bless
            if (client.Entity.ContainsFlag(Update.Flags.CastPray))
            {
                if (client.BlessTime <= 7198500)
                    client.BlessTime += 1000;
                else
                    client.BlessTime = 7200000;
                client.Entity.Update(Update.LuckyTimeTimer, client.BlessTime, false);
            }
            else if (client.Entity.ContainsFlag(Update.Flags.Praying))
            {
                if (client.PrayLead != null)
                {
                    if (client.PrayLead.Socket.Alive)
                    {
                        if (client.BlessTime <= 7199000)
                            client.BlessTime += 500;
                        else
                            client.BlessTime = 7200000;
                        client.Entity.Update(Update.LuckyTimeTimer, client.BlessTime, false);
                    }
                    else
                        client.Entity.RemoveFlag(Update.Flags.Praying);
                }
                else
                    client.Entity.RemoveFlag(Update.Flags.Praying);
            }
            else
            {
                if (client.BlessTime > 0)
                {
                    if (client.BlessTime >= 500)
                        client.BlessTime -= 500;
                    else
                        client.BlessTime = 0;
                    client.Entity.Update(Update.LuckyTimeTimer, client.BlessTime, false);
                }
            }
            #endregion
            #region Flashing name
            if (client.Entity.ContainsFlag(Network.GamePackets.Update.Flags.FlashingName))
            {
                if (Now > client.Entity.FlashingNameStamp.AddSeconds(client.Entity.FlashingNameTime))
                {
                    client.Entity.RemoveFlag(Network.GamePackets.Update.Flags.FlashingName);
                }
            }
            #endregion
            #region XPList
            if (!client.Entity.ContainsFlag(Network.GamePackets.Update.Flags.XPList))
            {
                if (Now > client.XPCountStamp.AddSeconds(3))
                {
                    #region Arrows
                    if (client.Equipment != null)
                    {
                        if (!client.Equipment.Free(5))
                        {
                            if (Network.PacketHandler.IsArrow(client.Equipment.TryGetItem(5).ID))
                            {
                                Database.ConquerItemTable.UpdateDurabilityItem(client.Equipment.TryGetItem(5));
                            }
                        }
                    }
                    #endregion
                    client.XPCountStamp = Now;
                    client.XPCount++;
                    if (client.XPCount >= 100)
                    {
                        client.Entity.AddFlag(Network.GamePackets.Update.Flags.XPList);
                        client.XPCount = 0;
                        client.XPListStamp = Now;
                    }
                }
            }
            else
            {
                if (Now > client.XPListStamp.AddSeconds(20))
                {
                    client.Entity.RemoveFlag(Network.GamePackets.Update.Flags.XPList);
                }
            }
            #endregion
            #region KOSpell
            if (client.Entity.OnKOSpell())
            {
                if (client.Entity.OnCyclone())
                {
                    int Seconds = Now.AllSeconds() - client.Entity.CycloneStamp.AddSeconds(client.Entity.CycloneTime).AllSeconds();
                    if (Seconds >= 1)
                    {
                        client.Entity.RemoveFlag(Network.GamePackets.Update.Flags.Cyclone);
                    }
                }
                if (client.Entity.OnSuperman())
                {
                    int Seconds = Now.AllSeconds() - client.Entity.SupermanStamp.AddSeconds(client.Entity.SupermanTime).AllSeconds();
                    if (Seconds >= 1)
                    {
                        client.Entity.RemoveFlag(Network.GamePackets.Update.Flags.Superman);
                    }
                }
                if (!client.Entity.OnKOSpell())
                {
                    //Record KO
                    client.Entity.KOCount = 0;
                }
            }
            #endregion
            #region Buffers
            if (client.Entity.ContainsFlag2(Network.GamePackets.Update.Flags2.TyrantAura) && !client.TeamAura)
            {
                if (Now >= client.Entity.AuraStamp.AddSeconds(client.Entity.AuraTime))
                {
                    client.Entity.AuraTime = 0;
                    client.Entity.Aura_isActive = false;
                    //client.Entity.StigmaIncrease = 0;
                    client.Entity.RemoveFlag2(Network.GamePackets.Update.Flags2.TyrantAura);
                }
            }
            if (client.Entity.ContainsFlag2(Network.GamePackets.Update.Flags2.FendAura) && !client.TeamAura)
            {
                if (Now >= client.Entity.AuraStamp.AddSeconds(client.Entity.AuraTime))
                {
                    client.Entity.AuraTime = 0;
                    client.Entity.Aura_isActive = false;
                    //client.Entity.StigmaIncrease = 0;
                    client.Entity.RemoveFlag2(Network.GamePackets.Update.Flags2.FendAura);
                }
            }
            if (client.Entity.ContainsFlag2(Network.GamePackets.Update.Flags2.MetalAura))
            {
                if (Now >= client.Entity.AuraStamp.AddSeconds(client.Entity.AuraTime))
                {
                    client.Entity.AuraTime = 0;
                    client.Entity.Aura_isActive = false;
                    //client.Entity.StigmaIncrease = 0;
                    client.Entity.RemoveFlag2(Network.GamePackets.Update.Flags2.MetalAura);
                }
            }
            if (client.Entity.ContainsFlag2(Network.GamePackets.Update.Flags2.WoodAura))
            {
                if (Now >= client.Entity.AuraStamp.AddSeconds(client.Entity.AuraTime))
                {
                    client.Entity.AuraTime = 0;
                    client.Entity.Aura_isActive = false;
                    //client.Entity.StigmaIncrease = 0;
                    client.Entity.RemoveFlag2(Network.GamePackets.Update.Flags2.WoodAura);
                }
            }
            if (client.Entity.ContainsFlag2(Network.GamePackets.Update.Flags2.WaterAura))
            {
                if (Now >= client.Entity.AuraStamp.AddSeconds(client.Entity.AuraTime))
                {
                    client.Entity.AuraTime = 0;
                    client.Entity.Aura_isActive = false;
                    //client.Entity.StigmaIncrease = 0;
                    client.Entity.RemoveFlag2(Network.GamePackets.Update.Flags2.WaterAura);
                }
            }
            if (client.Entity.ContainsFlag2(Network.GamePackets.Update.Flags2.EarthAura))
            {
                if (Now >= client.Entity.AuraStamp.AddSeconds(client.Entity.AuraTime))
                {
                    client.Entity.AuraTime = 0;
                    client.Entity.Aura_isActive = false;
                    //client.Entity.StigmaIncrease = 0;
                    client.Entity.RemoveFlag2(Network.GamePackets.Update.Flags2.EarthAura);
                }
            }
            if (client.Entity.ContainsFlag2(Network.GamePackets.Update.Flags2.FireAura))
            {
                if (Now >= client.Entity.AuraStamp.AddSeconds(client.Entity.AuraTime))
                {
                    client.Entity.AuraTime = 0;
                    client.Entity.Aura_isActive = false;
                    //client.Entity.StigmaIncrease = 0;
                    client.Entity.RemoveFlag2(Network.GamePackets.Update.Flags2.FireAura);
                }
            }

            if (client.Entity.ContainsFlag(Network.GamePackets.Update.Flags.Stigma))
            {
                if (Now >= client.Entity.StigmaStamp.AddSeconds(client.Entity.StigmaTime))
                {
                    client.Entity.StigmaTime = 0;
                    client.Entity.StigmaIncrease = 0;
                    client.Entity.RemoveFlag(Network.GamePackets.Update.Flags.Stigma);
                }
            }
            if (client.Entity.ContainsFlag(Network.GamePackets.Update.Flags.Dodge))
            {
                if (Now >= client.Entity.DodgeStamp.AddSeconds(client.Entity.DodgeTime))
                {
                    client.Entity.DodgeTime = 0;
                    client.Entity.DodgeIncrease = 0;
                    //Console.WriteLine("dodge removed !");
                    client.Entity.RemoveFlag(Network.GamePackets.Update.Flags.Dodge);
                }
            }
            if (client.Entity.ContainsFlag(Network.GamePackets.Update.Flags.Invisibility))
            {
                if (Now >= client.Entity.InvisibilityStamp.AddSeconds(client.Entity.InvisibilityTime))
                {
                    client.Entity.RemoveFlag(Network.GamePackets.Update.Flags.Invisibility);
                }
            }
            if (client.Entity.ContainsFlag(Network.GamePackets.Update.Flags.StarOfAccuracy))
            {
                if (client.Entity.StarOfAccuracyTime != 0)
                {
                    if (Now >= client.Entity.StarOfAccuracyStamp.AddSeconds(client.Entity.StarOfAccuracyTime))
                    {
                        client.Entity.RemoveFlag(Network.GamePackets.Update.Flags.StarOfAccuracy);
                    }
                }
                else
                {
                    if (Now >= client.Entity.AccuracyStamp.AddSeconds(client.Entity.AccuracyTime))
                    {
                        client.Entity.RemoveFlag(Network.GamePackets.Update.Flags.StarOfAccuracy);
                    }
                }
            }
            if (client.Entity.ContainsFlag(Network.GamePackets.Update.Flags.MagicShield))
            {
                if (client.Entity.MagicShieldTime != 0)
                {
                    if (Now >= client.Entity.MagicShieldStamp.AddSeconds(client.Entity.MagicShieldTime))
                    {
                        client.Entity.MagicShieldIncrease = 0;
                        client.Entity.MagicShieldTime = 0;
                        client.Entity.RemoveFlag(Network.GamePackets.Update.Flags.MagicShield);
                    }
                }
                else
                {
                    if (Now >= client.Entity.ShieldStamp.AddSeconds(client.Entity.ShieldTime))
                    {
                        client.Entity.ShieldIncrease = 0;
                        client.Entity.ShieldTime = 0;
                        client.Entity.RemoveFlag(Network.GamePackets.Update.Flags.MagicShield);
                    }
                }
            }
            #endregion
            #region Fly
            if (client.Entity.ContainsFlag(Network.GamePackets.Update.Flags.Fly))
            {
                if (Now >= client.Entity.FlyStamp.AddSeconds(client.Entity.FlyTime))
                {
                    client.Entity.RemoveFlag(Network.GamePackets.Update.Flags.Fly);
                    client.Entity.FlyTime = 0;
                }
            }
            #endregion
            #region PoisonStar
            if (client.Entity.NoDrugsTime > 0)
            {
                if (Now > client.Entity.NoDrugsStamp.AddSeconds(client.Entity.NoDrugsTime))
                {
                    client.Entity.NoDrugsTime = 0;
                }
            }
            #endregion
            #region ToxicFog
            if (client.Entity.ToxicFogLeft > 0)
            {
                if (Now >= client.Entity.ToxicFogStamp.AddSeconds(2))
                {
                    float Percent = client.Entity.ToxicFogPercent;
                    Percent = Percent / 220 * (client.Entity.Immunity / 100F);//região modificada poison
                    //Remove this line if you want it normal 
                    Percent = Math.Min(0.1F, client.Entity.ToxicFogPercent);
                    client.Entity.ToxicFogLeft--;
                    if (client.Entity.ToxicFogLeft == 0)
                    {
                        client.Entity.RemoveFlag(Update.Flags.Poisoned);
                        return;
                    }
                    client.Entity.ToxicFogStamp = Now;
                    if (client.Entity.Hitpoints > 1)
                    {
                        uint damage = Game.Attacking.Calculate.Percent(client.Entity, Percent);
                        client.Entity.Hitpoints -= damage;
                        Network.GamePackets.SpellUse suse = new Network.GamePackets.SpellUse(true);
                        suse.Attacker = client.Entity.UID;
                        suse.SpellID = 10010;
                        suse.AddTarget(client.Entity.UID, damage, null);
                        client.SendScreen(suse, true);
                        if (client != null)
                            client.UpdateQualifier(client.ArenaStatistic.PlayWith, client, damage);
                    }
                }
            }
            else
            {
                if (client.Entity.ContainsFlag(Update.Flags.Poisoned))
                    client.Entity.RemoveFlag(Update.Flags.Poisoned);
            }
            #endregion
            #region FatalStrike
            if (client.Entity.OnFatalStrike())
            {
                if (Now > client.Entity.FatalStrikeStamp.AddSeconds(client.Entity.FatalStrikeTime))
                {
                    client.Entity.RemoveFlag(Network.GamePackets.Update.Flags.FatalStrike);
                }
            }
            #endregion
            #region Oblivion
            if (client.Entity.OnOblivion())
            {
                if (Now > client.Entity.OblivionStamp.AddSeconds(client.Entity.OblivionTime))
                {
                    client.Entity.RemoveFlag2(Network.GamePackets.Update.Flags2.Oblivion);
                }
            }
            #endregion
            #region ShurikenVortex
            if (client.Entity.ContainsFlag(Network.GamePackets.Update.Flags.ShurikenVortex))
            {
                if (Now > client.Entity.ShurikenVortexStamp.AddSeconds(client.Entity.ShurikenVortexTime))
                {
                    client.Entity.RemoveFlag(Network.GamePackets.Update.Flags.ShurikenVortex);
                }
            }
            #endregion
            #region Transformations
            if (client.Entity.Transformed)
            {
                if (Now > client.Entity.TransformationStamp.AddSeconds(client.Entity.TransformationTime))
                {
                    client.Entity.Untransform();
                }
            }
            #endregion



            #region soulshackle
            if (client.Entity.ContainsFlag2(Network.GamePackets.Update.Flags2.SoulShackle))
            {
                if (Now > client.Entity.ShackleStamp.AddSeconds(client.Entity.ShackleTime))
                {
                    client.Entity.RemoveFlag2(Network.GamePackets.Update.Flags2.SoulShackle);
                }
            }
            #endregion
            #region AzureShield
            if (client.Entity.ContainsFlag2(Network.GamePackets.Update.Flags2.AzureShield))
            {
                if (Now > client.Entity.MagicShieldStamp.AddSeconds(client.Entity.MagicShieldTime))
                {
                    client.Entity.RemoveFlag2(Network.GamePackets.Update.Flags2.AzureShield);
                }
            }
            #endregion
            #region Blade Flurry
            if (client.Entity.ContainsFlag3(Update.Flags3.BladeFlurry))
            {
                if (Time32.Now > client.Entity.BladeFlurryStamp.AddSeconds(45))
                {
                    client.Entity.RemoveFlag3(Update.Flags3.BladeFlurry);
                }
            }
            #endregion
            #region Flustered
            if (client.Entity.ContainsFlag(Update.Flags.Frightened) && client.Entity.MapID == 1950)
            {
                if (client.RaceFrightened)
                {
                    if (Now > client.FrightenStamp.AddSeconds(20))
                    {
                        client.RaceFrightened = false;
                        {
                            GameCharacterUpdates update = new GameCharacterUpdates(true);
                            update.UID = client.Entity.UID;
                            update.Remove(GameCharacterUpdates.Flustered);
                            client.SendScreen(update, true);
                        }
                        client.Entity.RemoveFlag(Update.Flags.Frightened);
                    }
                    else
                    {
                        int rand;
                        ushort x, y;
                        do
                        {
                            rand = Kernel.Random.Next(Game.Map.XDir.Length);
                            x = (ushort)(client.Entity.X + Game.Map.XDir[rand]);
                            y = (ushort)(client.Entity.Y + Game.Map.YDir[rand]);
                        }
                        while (!client.Map.Floor[x, y, MapObjectType.Player]);
                        client.Entity.Facing = Kernel.GetAngle(
                            client.Entity.X, client.Entity.Y, x, y);
                        client.Entity.X = x;
                        client.Entity.Y = y;

                        client.SendScreen(
                            new TwoMovements()
                            {
                                EntityCount = 1,
                                Facing = client.Entity.Facing,
                                FirstEntity = client.Entity.UID,
                                WalkType = 9,
                                X = client.Entity.X,
                                Y = client.Entity.Y,
                                MovementType = TwoMovements.Walk
                            }, true);
                    }
                }
            }
            #endregion
            #region Stunned
            if (client.Entity.Stunned)
            {
                if (Now > client.Entity.StunStamp.AddMilliseconds(2000))
                {
                    client.Entity.Stunned = false;
                }
            }
            #endregion
            #region Frozen
            if (client.Entity.ContainsFlag(Update.Flags.Freeze) && client.Entity.MapID == 1950)
            {
                if (Now > client.Entity.FrozenStamp.AddSeconds(client.Entity.FrozenTime))
                {
                    client.Entity.FrozenD = false;
                    client.Entity.FrozenTime = 0;
                    client.Entity.RemoveFlag(Update.Flags.Freeze);

                    GameCharacterUpdates update = new GameCharacterUpdates(true);
                    update.UID = client.Entity.UID;
                    update.Remove(GameCharacterUpdates.Freeze);
                    client.SendScreen(update, true);
                }
            }
            #endregion
            #region Dizzy
            if (client.Entity.ContainsFlag(Update.Flags.Dizzy) && client.Entity.MapID == 1950)
            {
                if (client.RaceDizzy)
                {
                    if (Now > client.DizzyStamp.AddSeconds(5))
                    {
                        client.RaceDizzy = false;
                        {
                            GameCharacterUpdates update = new GameCharacterUpdates(true);
                            update.UID = client.Entity.UID;
                            update.Remove(GameCharacterUpdates.Dizzy);
                            client.SendScreen(update);
                        }
                        client.Entity.RemoveFlag(Update.Flags.Dizzy);
                    }
                }
            }
            #endregion
            #region Confused
            if (client.Entity.ContainsFlag(Update.Flags.Confused) && client.Entity.MapID == 1950)
            {
                if (Now > client.FrightenStamp.AddSeconds(15))
                {
                    client.RaceFrightened = false;
                    {
                        GameCharacterUpdates update = new GameCharacterUpdates(true);
                        update.UID = client.Entity.UID;
                        update.Remove(GameCharacterUpdates.Flustered);
                        client.SendScreen(update);
                    }
                    client.Entity.RemoveFlag(Update.Flags.Confused);
                }
            }
            #endregion
            #region IceBlock
            if (client.Entity.ContainsFlag(Network.GamePackets.Update.Flags.Frightened))
            {
                if (Now > client.FrightenStamp.AddSeconds(client.Entity.Fright))
                {
                    GameCharacterUpdates update = new GameCharacterUpdates(true);
                    update.UID = client.Entity.UID;
                    update.Remove(GameCharacterUpdates.Dizzy);
                    client.SendScreen(update, true);
                    client.Entity.RemoveFlag(Update.Flags.Frightened);
                }
                else
                {
                    int rand;
                    ushort x, y;
                    do
                    {
                        rand = Kernel.Random.Next(Game.Map.XDir.Length);
                        x = (ushort)(client.Entity.X + Game.Map.XDir[rand]);
                        y = (ushort)(client.Entity.Y + Game.Map.YDir[rand]);
                    }
                    while (!client.Map.Floor[x, y, MapObjectType.Player]);
                    client.Entity.Facing = Kernel.GetAngle(
                        client.Entity.X, client.Entity.Y, x, y);
                    client.Entity.X = x;
                    client.Entity.Y = y;

                    client.SendScreen(
                        new TwoMovements()
                        {
                            EntityCount = 1,
                            Facing = client.Entity.Facing,
                            FirstEntity = client.Entity.UID,
                            WalkType = 9,
                            X = client.Entity.X,
                            Y = client.Entity.Y,
                            MovementType = TwoMovements.Walk
                        }, true);
                }
            }
            if (client.Entity.ContainsFlag(Network.GamePackets.Update.Flags.Freeze))
            {
                if (Now > client.Entity.BlockStamp.AddSeconds(client.Entity.BlockTime))
                {
                    GameCharacterUpdates update = new GameCharacterUpdates(true);
                    update.UID = client.Entity.UID;
                    update.Remove(GameCharacterUpdates.Freeze);
                    client.SendScreen(update, true);
                    client.Entity.RemoveFlag(Network.GamePackets.Update.Flags.Freeze);
                }
            }
            if (client.Entity.ContainsFlag(Network.GamePackets.Update.Flags.Confused))
            {
                if (Now > client.ChaosStamp.AddSeconds(client.Entity.ChaosTime))
                {
                    GameCharacterUpdates update = new GameCharacterUpdates(true);
                    update.UID = client.Entity.UID;
                    update.Remove(GameCharacterUpdates.Flustered);
                    client.SendScreen(update);
                    client.Entity.RemoveFlag(Network.GamePackets.Update.Flags.Confused);
                }
            }
            #endregion
            #region Divine Shield
            if (client.Entity.ContainsFlag(Update.Flags.DivineShield) && client.Entity.MapID == 1950)
            {
                if (Now > client.GuardStamp.AddSeconds(10))
                {
                    client.RaceGuard = false;
                    {
                        GameCharacterUpdates update = new GameCharacterUpdates(true);
                        update.UID = client.Entity.UID;
                        update.Remove(GameCharacterUpdates.DivineShield);
                        client.SendScreen(update);
                    }
                    client.Entity.RemoveFlag(Update.Flags.DivineShield);
                }
            }
            #endregion
            #region Extra Speed
            if (client.Entity.ContainsFlag(Update.Flags.OrangeSparkles) && !client.InQualifier() && client.Entity.MapID == 1950)
            {
                if (Time32.Now > client.RaceExcitementStamp.AddSeconds(15))
                {
                    var upd = new GameCharacterUpdates(true)
                    {
                        UID = client.Entity.UID
                    };
                    upd.Remove(GameCharacterUpdates.Accelerated);
                    client.SendScreen(upd);
                    client.SpeedChange = null;
                    client.Entity.RemoveFlag(Update.Flags.OrangeSparkles);
                }
            }
            #endregion
            #region Decelerated
            if (client.Entity.ContainsFlag(Update.Flags.PurpleSparkles) && !client.InQualifier())
            {
                if (Time32.Now > client.DecelerateStamp.AddSeconds(10))
                {
                    {
                        client.RaceDecelerated = false;
                        var upd = new GameCharacterUpdates(true)
                        {
                            UID = client.Entity.UID
                        };
                        upd.Remove(GameCharacterUpdates.Decelerated);
                        client.SendScreen(upd);
                        client.SpeedChange = null;
                    }
                    client.Entity.RemoveFlag(Update.Flags.PurpleSparkles);
                }
            }
            #endregion
            #region Team Aura
            if (!client.TeamAura)
            {
                if (client.Team != null && !client.Entity.Dead && client.Team.Teammates != null)
                {
                    foreach (Client.GameClient pClient in client.Team.Teammates)
                    {
                        if (client.Entity.UID != pClient.Entity.UID && Kernel.GetDistance(client.Entity.X, client.Entity.Y, pClient.Entity.X, pClient.Entity.Y) <= Constants.pScreenDistance)
                        {
                            if (pClient.Entity.Aura_isActive && pClient.Socket.Alive && pClient.Entity.UID != client.Entity.UID && pClient.Entity.MapID == client.Entity.MapID)
                            {
                                if (pClient.Entity.Aura_actType == Update.Flags2.FendAura || pClient.Entity.Aura_actType == Update.Flags2.TyrantAura)
                                {
                                    client.TeamAura = true;
                                    client.TeamAuraOwner = pClient;
                                    client.TeamAuraStatusFlag = pClient.Entity.Aura_actType;
                                    client.TeamAuraPower = pClient.Entity.Aura_actPower;
                                    client.Entity.AddFlag2(client.TeamAuraStatusFlag);
                                    string type = "Critial Strikes";
                                    if (client.Entity.Aura_actType == 100) type = "Immunity";
                                    client.Send(new Message(type + " aumentou em " + client.TeamAuraPower + "%!", System.Drawing.Color.Red, Message.Agate));
                                    client.doAuraBonuses(client.TeamAuraStatusFlag, client.TeamAuraPower, 1);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                var pClient = client.TeamAuraOwner;
                string type = "Critial Strikes";
                if (client.Entity.Aura_actType == 100) type = "Immunity";
                if (pClient == null)
                {
                    client.TeamAura = false;
                    client.doAuraBonuses(client.TeamAuraStatusFlag, client.TeamAuraPower, -1);
                    client.Entity.RemoveFlag2(client.TeamAuraStatusFlag);
                    client.Send(new Message(type + " diminuiu em " + client.TeamAuraPower + "%!", System.Drawing.Color.Red, Message.Agate));
                    client.TeamAuraStatusFlag = 0;
                    client.TeamAuraPower = 0;
                }
                else
                {
                    if (!pClient.Entity.Aura_isActive || !pClient.Socket.Alive || pClient.Entity.Dead || pClient.Entity.MapID != client.Entity.MapID)
                    {
                        client.TeamAura = false;
                        client.doAuraBonuses(client.TeamAuraStatusFlag, client.TeamAuraPower, -1);
                        client.Entity.RemoveFlag2(client.TeamAuraStatusFlag);
                        client.Send(new Message(type + " diminuiu em " + client.TeamAuraPower + "%!", System.Drawing.Color.Red, Message.Agate));
                        client.TeamAuraStatusFlag = 0;
                        client.TeamAuraPower = 0;
                    }
                    else
                    {
                        if (client.Team == null || (pClient.Team == null || (pClient.Team != null && !pClient.Team.IsTeammate(client.Entity.UID))) || client.Entity.Dead || Kernel.GetDistance(client.Entity.X, client.Entity.Y, pClient.Entity.X, pClient.Entity.Y) > Constants.pScreenDistance)
                        {
                            client.TeamAura = false;
                            client.doAuraBonuses(client.TeamAuraStatusFlag, client.TeamAuraPower, -1);
                            client.Entity.RemoveFlag2(client.TeamAuraStatusFlag);
                            client.Send(new Message(type + " diminuiu em " + client.TeamAuraPower + "%!", System.Drawing.Color.Red, Message.Agate));
                            client.TeamAuraStatusFlag = 0;
                            client.TeamAuraPower = 0;
                        }
                    }
                }
            }
            #endregion
            #region CTF Flag
            if (client.Entity.ContainsFlag2(Update.Flags2.CarryingFlag))
            {
                if (Time32.Now > client.Entity.FlagStamp.AddSeconds(60))
                {
                    client.Entity.RemoveFlag2(Update.Flags2.CarryingFlag);
                }
            }
            #endregion
            #region SuperCyclone
            if (client.Entity.ContainsFlag3((uint)1UL << 0x16))
            {
                if (Time32.Now > client.Entity.SuperCyclone.AddSeconds(40))
                {
                    client.Entity.RemoveFlag3((uint)1UL << 0x16);
                }
            }
            #endregion  
        }
        private void CharactersCallback(GameClient client, int time)
        {
            if (!Valid(client)) return;
            Time32 Now32 = new Time32(time);
            DateTime Now64 = DateTime.Now;

            if (client.Entity.Titles.Count > 0)
            {
                foreach (var titles in client.Entity.Titles)
                {
                    if (Now64 > titles.Value)
                    {
                        client.Entity.Titles.Remove(titles.Key);
                        if (client.Entity.MyTitle == titles.Key)
                            client.Entity.MyTitle = Network.GamePackets.TitlePacket.Titles.None;

                        client.Entity.RemoveTopStatus((UInt64)titles.Key);
                    }
                }
            }
            #region Torneio Pk de profissões // funcionando normal
            /*if (DateTime.Now.DayOfWeek == DayOfWeek.Tuesday && Now64.Hour == 13 && Now64.Minute == 05 && Now64.Second == 30)
            {
                foreach (var pclint in Program.GamePool)
                    pclint.MessageBox("Classe PK Começou, ganhe Top e 500k de cps", // classe pk
                        (p) => { p.Entity.Teleport(9000, 436, 375); }, null, 60);
            }*/
            #endregion  
            /*#region CrossServer
            if (Now64.Hour == CrossServer.hour && Now64.Minute == 01 && Now64.Second == 5 && !CrossServer.IsWar)
            {
                CrossServer.Start();
                foreach (var pclint in Program.GamePool)
                    client.MessageBox("CrossServer CTF iniciou deseja ir?",
                           p => { p.Entity.Teleport(1002, 354, 337); }, null);
            }
            if (CrossServer.IsWar)
            {
                if (DateTime.Now > Game.CrossServer.StartTime.AddHours(1.0))
                {
                    CrossServer.End();
                }
            }
            if (Game.CrossServer.IsWar)
            {
                if (Time32.Now > Game.CrossServer.ScoreSendStamp.AddSeconds(3))
                {
                    Game.CrossServer.ScoreSendStamp = Time32.Now;
                    Game.CrossServer.SendScores();
                }
            }
            #endregion */ 
            #region Training points
            if (client.Entity.HeavenBlessing > 0 && !client.Entity.Dead)
            {
                if (Now32 > client.LastTrainingPointsUp.AddMinutes(10))
                {
                    client.OnlineTrainingPoints += 10;
                    if (client.OnlineTrainingPoints >= 30)
                    {
                        client.OnlineTrainingPoints -= 30;
                        client.IncreaseExperience(client.ExpBall / 100, false);
                    }
                    client.LastTrainingPointsUp = Now32;
                    client.Entity.Update(Network.GamePackets.Update.OnlineTraining, client.OnlineTrainingPoints, false);
                }
            }
            #endregion
            #region Extra treasure points
            if (client.AllowedTreasurePoints)
            {
                if (Now32 > client.LastTreasurePoints.AddSeconds(50))
                {
                    client.Entity.TreasuerPoints++;
                    client.LastTreasurePoints = Time32.Now;
                }
            }
            #endregion
            #region Minning
            if (client.Mining && !client.Entity.Dead)
            {
                if (Now32 >= client.MiningStamp.AddSeconds(2))
                {
                    client.MiningStamp = Now32;
                    Game.ConquerStructures.Mining.Mine(client);
                }
            }
            #endregion
            #region MentorPrizeSave
            if (Now32 > client.LastMentorSave.AddSeconds(5))
            {
                Database.KnownPersons.SaveApprenticeInfo(client.AsApprentice);
                client.LastMentorSave = Now32;
            }
            #endregion
            #region Attackable
            if (client.JustLoggedOn)
            {
                client.JustLoggedOn = false;
                client.ReviveStamp = Now32;
            }
            if (!client.Attackable)
            {
                if (Now32 > client.ReviveStamp.AddSeconds(5))
                {
                    client.Attackable = true;
                }
            }
            #endregion
            #region DoubleExperience
            if (client.Entity.DoubleExperienceTime > 0)
            {
                if (Now32 > client.Entity.DoubleExpStamp.AddMilliseconds(1000))
                {
                    client.Entity.DoubleExpStamp = Now32;
                    client.Entity.DoubleExperienceTime--;
                }
            }
            #endregion
            #region HeavenBlessing
            if (client.Entity.HeavenBlessing > 0)
            {
                if (Now32 > client.Entity.HeavenBlessingStamp.AddMilliseconds(1000))
                {
                    client.Entity.HeavenBlessingStamp = Now32;
                    client.Entity.HeavenBlessing--;
                }
            }
            #endregion
            #region Enlightment
            if (client.Entity.EnlightmentTime > 0)
            {
                if (Now32 >= client.Entity.EnlightmentStamp.AddMinutes(1))
                {
                    client.Entity.EnlightmentStamp = Now32;
                    client.Entity.EnlightmentTime--;
                    if (client.Entity.EnlightmentTime % 10 == 0 && client.Entity.EnlightmentTime > 0)
                        client.IncreaseExperience(Game.Attacking.Calculate.Percent((int)client.ExpBall, .10F), false);
                }
            }
            #endregion
            #region PKPoints
            if (Now32 >= client.Entity.PKPointDecreaseStamp.AddMinutes(5))
            {
                client.Entity.PKPointDecreaseStamp = Now32;
                if (client.Entity.PKPoints > 0)
                {
                    client.Entity.PKPoints--;
                }
                else
                    client.Entity.PKPoints = 0;
            }
            #endregion
            #region OverHP
            if (client.Entity.FullyLoaded)
            {
                if (client.Entity.Hitpoints > client.Entity.MaxHitpoints && client.Entity.MaxHitpoints > 1 && !client.Entity.Transformed)
                {
                    client.Entity.Hitpoints = client.Entity.MaxHitpoints;
                }
            }
            #endregion
            #region OverVigor
            if (client.Entity.FullyLoaded)
            {
                if (client.Vigor > client.Entity.ExtraVigor)
                {
                    client.Vigor = client.Entity.ExtraVigor;
                }
            }
            #endregion
            #region Die Delay
            if (client.Entity.Hitpoints == 0 && client.Entity.ContainsFlag(Network.GamePackets.Update.Flags.Dead) && !client.Entity.ContainsFlag(Network.GamePackets.Update.Flags.Ghost))
            {
                if (Now32 > client.Entity.DeathStamp.AddSeconds(2))
                {
                    client.Entity.AddFlag(Network.GamePackets.Update.Flags.Ghost);
                    if (client.Entity.Body % 10 < 3)
                        client.Entity.TransformationID = 99;
                    else
                        client.Entity.TransformationID = 98;

                    client.SendScreenSpawn(client.Entity, true);
                }
            }
            #endregion
            #region ChainBolt
            if (client.Entity.ContainsFlag2(Update.Flags2.ChainBoltActive))
                if (Now32 > client.Entity.ChainboltStamp.AddSeconds(client.Entity.ChainboltTime))
                    client.Entity.RemoveFlag2(Update.Flags2.ChainBoltActive);
            #endregion
            if (client.Entity.HasMagicDefender && Now32 >= client.Entity.MagicDefenderStamp.AddSeconds(client.Entity.MagicDefenderSecs))
            {
                client.Entity.RemoveMagicDefender();
            }
            if (Now32 >= client.Entity.BlackbeardsRageStamp.AddSeconds(60))
            {
                client.Entity.RemoveFlag2(Conquer_Online_Server.Network.GamePackets.Update.Flags2.BlackbeardsRage);
            }
            if (Now32 >= client.Entity.CannonBarrageStamp.AddSeconds(60))
            {
                client.Entity.RemoveFlag2(Conquer_Online_Server.Network.GamePackets.Update.Flags2.CannonBarrage);
            }
            if (Now32 >= client.Entity.FatigueStamp.AddSeconds(client.Entity.FatigueSecs))
            {
                client.Entity.RemoveFlag2(Conquer_Online_Server.Network.GamePackets.Update.Flags2.Fatigue);
                client.Entity.IsDefensiveStance = false;
            }
            if (Now32 > client.Entity.GuildRequest.AddSeconds(30))
            {
                client.GuildJoinTarget = 0;
            }



            #region Dis City
            if (Now64.DayOfWeek == DayOfWeek.Wednesday || Now64.DayOfWeek == DayOfWeek.Friday)
            {
                if ((Now64.Hour == 12 || Now64.Hour == 19) && Now64.Minute == 00 && Now64.Second <= 2)
                {
                    if (client.Entity.Level >= 110)
                    {
                        Kernel.SendWorldMessage(new Network.GamePackets.Message("DisCity iniciou vá até a cidade dos Macacos e procure  SolarSaint!", System.Drawing.Color.White, Network.GamePackets.Message.Center), Program.GamePool);
                        Game.Features.DisCity.Signup = true;
                        client.MessageBox("Dis City começou deseja ir?",
                            (p) => { p.Entity.Teleport(1020, 534, 484); },
                            (p) => { p.Send("Você pode juntar-se a SolarSaint em Ape City!"); }, 300);
                    }
                }
                if ((Now64.Hour == 12 || Now64.Hour == 19) && Now64.Minute == 45 && Now64.Second >= 00)
                {
                    if (client.Entity.MapID == 4023 || client.Entity.MapID == 4024)
                    {
                        Conquer_Online_Server.Kernel.SendWorldMessage(new Network.GamePackets.Message("Todos os jogadores do Estágio 3 de DisCity foram teletransportados para o Estágio Final! Boa sorte!", System.Drawing.Color.White, Network.GamePackets.Message.Center), Program.GamePool);
                        client.Entity.Teleport(4025, 150, 286);
                        client.Inventory.Add(723087, 0, 1);
                    }
                }
                if ((Now64.Hour == 12 || Now64.Hour == 19) && Now64.Minute == 59 && Now64.Second >= 30)
                {
                    if (client.Entity.MapID == 4023 || client.Entity.MapID == 4024 || client.Entity.MapID == 4025)
                    {
                        Conquer_Online_Server.Kernel.SendWorldMessage(new Network.GamePackets.Message("DisCity terminou. Ele vai começar às 12:00 ou 19:00 toda quarta-feira e sexta-feira!", System.Drawing.Color.White, Network.GamePackets.Message.Center), Program.GamePool);
                        client.Entity.Teleport(1002, 430, 378);
                    }
                }
            }
            #endregion
            #region Weekly PK 
            if (Now64.Second <= 2 && Now64.DayOfWeek == DayOfWeek.Monday && Now64.Hour == 16 && Now64.Minute == 00)
            {
                client.MessageBox("Guerra semanal PK começou deseja ir você só tem 1 minuto para entrar ",
                      (p) => { p.Entity.Teleport(1002, 453, 294); }, null, 60);
            }
            #endregion
            #region Team Qualifier
            if ((Now64.Hour == 11 || Now64.Hour == 19) && Now64.Minute == 15 && Now64.Second <= 2)
            {
                client.MessageBox("Team Arena Começou e ficara aberta durante duas horas, deseja ir",
                    (p) => { TeamArena.QualifyEngine.DoSignup(p); },
                    (p) => { p.Send("Você ainda pode entrar na Arena em Equipe pela interface da Arena!"); });
            }
            #endregion

            if (client.Fake)
            {
                if (!client.SignedUpForEPK)
                    ElitePKTournament.SignUp(client);
                if (client.ElitePKMatch != null)
                {
                    if (client.ElitePKMatch.OnGoing && client.ElitePKMatch.Inside)
                    {
                        if (Time32.Now > client.FakeQuit.AddSeconds(5))
                        {
                            client.FakeQuit = Time32.Now;
                            if (Kernel.Rate(1, 10))
                            {
                                client.ElitePKMatch.End(client);
                            }
                        }
                    }
                }
            }

#if OBSOLETE_CLASSPK
           /* if (Now64.Hour == 20 && Now64.Minute == 00 && Now64.DayOfWeek == DayOfWeek.Friday && Now64.Second == 30)
            {
                if (client.Entity.Class >= 41 && client.Entity.Class <= 45)
                {
                    //Database.EntityTable.UpdateArcherStatus(client);
                    //client.Entity.Status = 0;
                    Network.GamePackets.NpcReply npc = new Network.GamePackets.NpcReply(6, "ClassPkWar has Started! You Wana Join?");
                    npc.OptionID = 248;
                    client.Send(npc.ToArray());
                    //return;
                }
            }
            if (Now64.Hour == 20 && Now64.Minute == 00 && Now64.DayOfWeek == DayOfWeek.Wednesday && Now64.Second == 20)
            {
                if (client.Entity.Class >= 61 && client.Entity.Class <= 65)
                {
                    //Database.EntityTable.UpdateMonkStatus(client);
                    //client.Entity.Status = 0;
                    Network.GamePackets.NpcReply npc = new Network.GamePackets.NpcReply(6, "ClassPkWar has Started! You Wana Join?");
                    npc.OptionID = 248;
                    client.Send(npc.ToArray());
                    //return;
                }
            }

            if (Now64.Hour == 20 && Now64.Minute == 00 && Now64.DayOfWeek == DayOfWeek.Sunday && Now64.Second == 30)
            {
                if (client.Entity.Class >= 11 && client.Entity.Class <= 15)
                {
                    //Database.EntityTable.UpdateTrojanStatus(client);
                    //client.Entity.Status = 0;
                    Network.GamePackets.NpcReply npc = new Network.GamePackets.NpcReply(6, "ClassPkWar has Started! You Wana Join?");
                    npc.OptionID = 248;
                    client.Send(npc.ToArray());
                    //return;
                }
            }
            if (Now64.Hour == 20 && Now64.Minute == 00 && Now64.DayOfWeek == DayOfWeek.Sunday && Now64.Second == 30)
            {
                if (client.Entity.Class >= 21 && client.Entity.Class <= 25)
                {
                    //Database.EntityTable.UpdateWarriorStatus(client);
                    //client.Entity.Status = 0;
                    Network.GamePackets.NpcReply npc = new Network.GamePackets.NpcReply(6, "ClassPkWar has Started! You Wana Join?");
                    npc.OptionID = 248;
                    client.Send(npc.ToArray());
                    //return;
                }
            }
            if (Now64.Hour == 20 && Now64.Minute == 00 && Now64.DayOfWeek == DayOfWeek.Monday && Now64.Second == 30)
            {
                if (client.Entity.Class >= 142 && client.Entity.Class <= 145)
                {
                    //Database.EntityTable.UpdateFireStatus(client);
                    //client.Entity.Status = 0;
                    Network.GamePackets.NpcReply npc = new Network.GamePackets.NpcReply(6, "ClassPkWar has Started! You Wana Join?");
                    npc.OptionID = 248;
                    client.Send(npc.ToArray());
                    //return;
                }
            }
            if (Now64.Hour == 20 && Now64.Minute == 00 && Now64.DayOfWeek == DayOfWeek.Tuesday && Now64.Second == 30)
            {
                if (client.Entity.Class >= 51 && client.Entity.Class <= 55)
                {
                    //Database.EntityTable.UpdateNinjaStatus(client);
                    //client.Entity.Status = 0;
                    Network.GamePackets.NpcReply npc = new Network.GamePackets.NpcReply(6, "ClassPkWar has Started! You Wana Join?");
                    npc.OptionID = 248;
                    client.Send(npc.ToArray());
                    // return;
                }
            }
            if (Now64.Hour == 20 && Now64.Minute == 00 && Now64.DayOfWeek == DayOfWeek.Thursday && Now64.Second == 30)
            {
                if (client.Entity.Class >= 132 && client.Entity.Class <= 135)
                {
                    //Database.EntityTable.UpdateWaterStatus(client);
                    //client.Entity.Status = 0;
                    Network.GamePackets.NpcReply npc = new Network.GamePackets.NpcReply(6, "ClassPkWar has Started! You Wana Join?");
                    npc.OptionID = 248;
                    client.Send(npc.ToArray());
                    //return;
                }
            }*/
#endif
        }
        private void AutoAttackCallback(GameClient client, int time)
        {
            if (!Valid(client)) return;
            Time32 Now = new Time32(time);

            if (client.Entity.AttackPacket != null || client.Entity.VortexAttackStamp != null)
            {
                try
                {
                    if (client.Entity.ContainsFlag(Network.GamePackets.Update.Flags.ShurikenVortex))
                    {
                        if (client.Entity.VortexPacket != null && client.Entity.VortexPacket.ToArray() != null)
                        {
                            if (Now > client.Entity.VortexAttackStamp.AddMilliseconds(1400))
                            {
                                client.Entity.VortexAttackStamp = Now;
                                new Game.Attacking.Handle(client.Entity.VortexPacket, client.Entity, null);
                            }
                        }
                    }
                    else
                    {
                        client.Entity.VortexPacket = null;
                        var AttackPacket = client.Entity.AttackPacket;
                        if (AttackPacket != null && AttackPacket.ToArray() != null)
                        {
                            uint AttackType = AttackPacket.AttackType;
                            if (AttackType == Network.GamePackets.Attack.Magic || AttackType == Network.GamePackets.Attack.Melee || AttackType == Network.GamePackets.Attack.Ranged)
                            {
                                if (AttackType == Network.GamePackets.Attack.Magic)
                                {
                                    if (Now > client.Entity.AttackStamp.AddSeconds(1))
                                    {
                                        new Game.Attacking.Handle(AttackPacket, client.Entity, null);
                                    }
                                }
                                else
                                {
                                    int decrease = -300;
                                    if (client.Entity.OnCyclone())
                                        decrease = 700;
                                    if (client.Entity.OnSuperman())
                                        decrease = 200;
                                    if (Now > client.Entity.AttackStamp.AddMilliseconds((1000 - client.Entity.Agility - decrease) * (int)(AttackType == Network.GamePackets.Attack.Ranged ? 1 : 1)))
                                    {
                                        new Game.Attacking.Handle(AttackPacket, client.Entity, null);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Program.SaveException(e);
                    client.Entity.AttackPacket = null;
                    client.Entity.VortexPacket = null;
                }
            }
        }
        private void CompanionsCallback(GameClient client, int time)
        {
            if (!Valid(client)) return;
            Time32 Now = new Time32(time);

            if (client.Companion != null)
            {
                short distance = Kernel.GetDistance(client.Companion.X, client.Companion.Y, client.Entity.X, client.Entity.Y);
                if (distance >= 8)
                {
                    ushort X = (ushort)(client.Entity.X + Kernel.Random.Next(2));
                    ushort Y = (ushort)(client.Entity.Y + Kernel.Random.Next(2));
                    if (!client.Map.SelectCoordonates(ref X, ref Y))
                    {
                        X = client.Entity.X;
                        Y = client.Entity.Y;
                    }
                    client.Companion.X = X;
                    client.Companion.Y = Y;
                    Network.GamePackets.Data data = new Conquer_Online_Server.Network.GamePackets.Data(true);
                    data.ID = Network.GamePackets.Data.Jump;
                    data.dwParam = (uint)((Y << 16) | X);
                    data.wParam1 = X;
                    data.wParam2 = Y;
                    data.UID = client.Companion.UID;
                    client.Companion.MonsterInfo.SendScreen(data);
                }
                else if (distance > 4)
                {
                    Enums.ConquerAngle facing = Kernel.GetAngle(client.Companion.X, client.Companion.Y, client.Companion.Owner.Entity.X, client.Companion.Owner.Entity.Y);
                    if (!client.Companion.Move(facing))
                    {
                        facing = (Enums.ConquerAngle)Kernel.Random.Next(7);
                        if (client.Companion.Move(facing))
                        {
                            client.Companion.Facing = facing;
                            Network.GamePackets.GroundMovement move = new Conquer_Online_Server.Network.GamePackets.GroundMovement(true);
                            move.Direction = facing;
                            move.UID = client.Companion.UID;
                            move.GroundMovementType = Network.GamePackets.GroundMovement.Run;
                            client.Companion.MonsterInfo.SendScreen(move);
                        }
                    }
                    else
                    {
                        client.Companion.Facing = facing;
                        Network.GamePackets.GroundMovement move = new Conquer_Online_Server.Network.GamePackets.GroundMovement(true);
                        move.Direction = facing;
                        move.UID = client.Companion.UID;
                        move.GroundMovementType = Network.GamePackets.GroundMovement.Run;
                        client.Companion.MonsterInfo.SendScreen(move);
                    }
                }
                else
                {
                    var monster = client.Companion;
                    if (monster.MonsterInfo.InSight == 0)
                    {
                        if (client.Entity.AttackPacket != null)
                        {
                            if (client.Entity.AttackPacket.AttackType == Network.GamePackets.Attack.Magic)
                            {
                                if (client.Entity.AttackPacket.Decoded)
                                {
                                    if (Database.SpellTable.SpellInformations.ContainsKey((ushort)client.Entity.AttackPacket.Damage))
                                    {
                                        var info = Database.SpellTable.SpellInformations[(ushort)client.Entity.AttackPacket.Damage].Values.ToArray()[client.Spells[(ushort)client.Entity.AttackPacket.Damage].Level];
                                        if (info.CanKill)
                                        {
                                            monster.MonsterInfo.InSight = client.Entity.AttackPacket.Attacked;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                monster.MonsterInfo.InSight = client.Entity.AttackPacket.Attacked;
                            }
                        }
                    }
                    else
                    {
                        if (monster.MonsterInfo.InSight > 400000 && monster.MonsterInfo.InSight < 600000 || monster.MonsterInfo.InSight > 800000 && monster.MonsterInfo.InSight != monster.UID)
                        {
                            Entity attacked = null;

                            if (client.Screen.TryGetValue(monster.MonsterInfo.InSight, out attacked))
                            {
                                if (Now > monster.AttackStamp.AddMilliseconds(monster.MonsterInfo.AttackSpeed))
                                {
                                    monster.AttackStamp = Now;
                                    if (attacked.Dead)
                                    {
                                        monster.MonsterInfo.InSight = 0;
                                    }
                                    else
                                        new Game.Attacking.Handle(null, monster, attacked);
                                }
                            }
                            else
                                monster.MonsterInfo.InSight = 0;
                        }
                    }
                }
            }
        }
        private void PrayerCallback(GameClient client, int time)
        {
            if (!Valid(client)) return;
            Time32 Now = new Time32(time);

            if (client.Entity.Reborn > 1)
                return;

            if (!client.Entity.ContainsFlag(Network.GamePackets.Update.Flags.Praying))
            {
                foreach (Interfaces.IMapObject ClientObj in client.Screen.Objects)
                {
                    if (ClientObj != null)
                    {
                        if (ClientObj.MapObjType == Game.MapObjectType.Player)
                        {
                            var Client = ClientObj.Owner;
                            if (Client.Entity.ContainsFlag(Network.GamePackets.Update.Flags.CastPray))
                            {
                                if (Kernel.GetDistance(client.Entity.X, client.Entity.Y, ClientObj.X, ClientObj.Y) <= 3)
                                {
                                    client.Entity.AddFlag(Network.GamePackets.Update.Flags.Praying);
                                    client.PrayLead = Client;
                                    client.Entity.Action = Client.Entity.Action;
                                    Client.Prayers.Add(client);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (client.PrayLead != null)
                {
                    if (Kernel.GetDistance(client.Entity.X, client.Entity.Y, client.PrayLead.Entity.X, client.PrayLead.Entity.Y) > 4)
                    {
                        client.Entity.RemoveFlag(Network.GamePackets.Update.Flags.Praying);
                        client.PrayLead.Prayers.Remove(client);
                        client.PrayLead = null;
                    }
                }
            }
        }

        private void WorldTournaments(int time)
        {
            Time32 Now = new Time32(time);
            DateTime Now64 = DateTime.Now;
            #region Messages
            if (Now64.Minute == 16 && Now64.Second <= 2)
            {
                Kernel.SendWorldMessage(new Network.GamePackets.Message("Bem Vindos ao Oblivion-CO!", System.Drawing.Color.White, Network.GamePackets.Message.Center), Program.GamePool);
            }
            if (Now64.Minute == 27 && Now64.Second <= 2 || Now64.Minute == 34 && Now64.Second <= 2 || Now64.Minute == 42 && Now64.Second <= 2 || Now64.Minute == 53 && Now64.Second <= 2 || Now64.Minute == 14 && Now64.Second <= 2)
            {
                Kernel.SendWorldMessage(new Network.GamePackets.Message("Para o Servidor fica sempre 24 horas online ajude na nossa Loja! ", System.Drawing.Color.White, Network.GamePackets.Message.Center), Program.GamePool);
            }
            if (Now64.Minute == 32 && Now64.Second <= 20)
            {
                Kernel.SendWorldMessage(new Network.GamePackets.Message("Bem Vindos ao Oblivion-Co!", System.Drawing.Color.White, Network.GamePackets.Message.Center), Program.GamePool);
            }
            if (Now64.Minute == 07 && Now64.Second <= 20)
            {
                Kernel.SendWorldMessage(new Network.GamePackets.Message("Para o Servidor fica sempre 24 horas online ajude na nossa Loja! ", System.Drawing.Color.White, Network.GamePackets.Message.Center), Program.GamePool);
            }
            if (Now64.Minute == 02 && Now64.Second <= 2)
            {
                Kernel.SendWorldMessage(new Network.GamePackets.Message("Bem Vindos ao Oblivion-Co!", System.Drawing.Color.White, Network.GamePackets.Message.Center), Program.GamePool);
            }
            if (Now64.Minute == 42 && Now64.Second <= 2)
            {
                Kernel.SendWorldMessage(new Network.GamePackets.Message("Para o Servidor fica sempre 24 horas online ajude na nossa Loja!", System.Drawing.Color.White, Network.GamePackets.Message.Center), Program.GamePool);
            }
            if (Now64.Minute == 52 && Now64.Second <= 02)
            {
                Kernel.SendWorldMessage(new Network.GamePackets.Message("Bem Vindos ao Oblivion-Co!", System.Drawing.Color.White, Network.GamePackets.Message.Center), Program.GamePool);
            }
            #endregion


            #region PureLand
            if (Now64.DayOfWeek == DayOfWeek.Monday && Now64.Hour == 23 && Now64.Minute < 50 && !PureLand)
            {
                PureLand = true;
                Map map = Kernel.Maps[2065];
                if (map != null)
                {
                    foreach (var monster in map.Entities.Values)
                    {
                        if (monster.EntityFlag == EntityFlag.Monster)
                        {
                            monster.Hitpoints = 0;
                            monster.DeathStamp = Time32.Now.AddHours(-24);
                        }
                    }
                }
               // Kernel.SendWorldMessage(new Message("Unir-se na busca da terra perdida na cidade do dragão!", Message.Center), Program.GamePool);
                //foreach (var player in Program.GamePool)
                {
                   // player.MessageBox("Unir-se na busca da terra perdida na cidade do dragão [Premio: 10K cp]",
                       // (p) => { p.Entity.Teleport(1002, 419, 374); }, null);
                }
           }
            if (Now64.DayOfWeek == DayOfWeek.Monday && Now64.Hour == 23 && Now64.Minute >= 50 && PureLand)
            {
                PureLand = false;
                foreach (var player in Program.GamePool)
                    if (player.Entity.MapID == 2065)
                        player.Entity.Teleport(1002, 400, 400);
                Kernel.SendWorldMessage(new Message("", Message.Center), Program.GamePool);
            }
            #endregion
            #region Elite GW

            if (!Game.EliteGuildWar.IsWar)
            {
                if (Now64.Hour == 17 && Now64.Minute == 06 && Now64.Second == 15)
                {
                    Game.EliteGuildWar.Start();
                    foreach (var client in Program.GamePool)
                        if (client.Entity.GuildID != 0)
                            client.MessageBox("Elite Gw Começou premio 2kk, você deseja ir?",
                                p => { p.Entity.Teleport(1002, 414, 264); }, null);
                }
            }
            if (Game.EliteGuildWar.IsWar)
            {
                if (Time32.Now > Game.EliteGuildWar.ScoreSendStamp.AddSeconds(3))
                {
                    Game.EliteGuildWar.ScoreSendStamp = Time32.Now;
                    Game.EliteGuildWar.SendScores();
                }
                if (Now64.Hour == 17 && Now64.Minute == 50 && Now64.Second <= 15)
                {
                    Kernel.SendWorldMessage(new Network.GamePackets.Message("Faltam 10 minutos para o fim da Guerra de Guildas Elite! Corram para dominar o poste!", System.Drawing.Color.White, Network.GamePackets.Message.Center), Program.GamePool);
                }
            }

            if (Game.EliteGuildWar.IsWar)
            {
                if (Now64.Hour == 18 && Now64.Minute == 00 && Now64.Second == 02)
                {
                    Game.EliteGuildWar.End();
                    {
                        Kernel.SendWorldMessage(new Network.GamePackets.Message("A Guerra de Guildas Elite terminou!", System.Drawing.Color.White, Network.GamePackets.Message.Center), Program.GamePool);
                    }
                }
            }
            #endregion
            #region Couples PK War
            if (Now64.Hour == 20 && Now64.Minute == 01 && Now64.Second == 02)
            {
                Kernel.SendWorldMessage(new Network.GamePackets.Message("Top Spouse  esta começando ! você tem 5 minutos para registrar se em TC TOP SPOUSE !", System.Drawing.Color.White, Network.GamePackets.Message.Center), Program.GamePool);
                foreach (var client in Program.GamePool)
                    if (client.Entity.Spouse != "None")
                        client.MessageBox("TOP SPOUSE COMEÇOU PREMIO 500k! você deseja ir? ",
                            p => { p.Entity.Teleport(1002, 423, 291); }, null);
            }
            #endregion
            #region Dis City
            if (Now64.DayOfWeek == DayOfWeek.Wednesday || Now64.DayOfWeek == DayOfWeek.Friday)
            {
                if ((Now64.Hour == 12 || Now64.Hour == 19) && Now64.Minute == 05 && Now64.Second <= 2)
                {
                    Kernel.SendWorldMessage(new Network.GamePackets.Message("DisCity está fechada. Volte no próximo horário!", System.Drawing.Color.White, Network.GamePackets.Message.Center), Program.GamePool);
                    Game.Features.DisCity.Signup = false;
                }
            }
            #endregion
            #region Clan War
            if ((Now64.Hour == 21 || Now64.Hour == 15) && Now64.Minute == 02 && !ClanWar.IsWar)
            {
                Game.ClanWar.Start();
                ClanWarAI = false;
                if (Now64.Hour != 15)
                {
                    ClanWarAI = Now64.Hour != 15;
                    foreach (var client in Program.GamePool)
                        if (client.Entity.GuildID != 0)
                            client.MessageBox("ClanWar começou premio 2kk, você deseja ir?",
                                p => { p.Entity.Teleport(1002, 414, 244); }, null);
                }
            }
            if (Now64.Hour == 15 && Now64.Minute == 02 && !ClanWarAI)
            {
                ClanWarAI = true;
                foreach (var client in Program.GamePool)
                    if (client.Entity.GuildID != 0)
                        client.MessageBox("ClanWar começou premio 2kk, você deseja ir?",
                            p => { p.Entity.Teleport(1002, 414, 244); }, null);
            }
            if ((Now64.Hour == 22 || Now64.Hour == 16) && Now64.Minute == 01 && ClanWar.IsWar)
            {
                Game.ClanWar.End();
            }
            if (Game.ClanWar.IsWar)
            {
                if (Time32.Now > Game.ClanWar.ScoreSendStamp.AddSeconds(3))
                {
                    Game.ClanWar.ScoreSendStamp = Time32.Now;
                    Game.ClanWar.SendScores();
                }
            }

            #endregion
            #region Monthly PK
            if (Now64.Day <= 8 && Now64.DayOfWeek == DayOfWeek.Sunday)
            {
                if (Now64.Hour == 12 && Now64.Minute >= 50 && Now64.Second <= 2)
                {
                    int min = 60 - Now64.Minute;
                    Kernel.SendWorldMessage(new Message("Guerra PK Mensal começara " + min.ToString() + " minutos!", Color.Red, 2012));
                }
                if (Now64.Hour == 20 && Now64.Minute == 00 && Now64.Second <= 2)
                {
                    MonthlyPKWar = true;
                    Kernel.SendWorldMessage(new Message("Começo a guerra PK mensal!", Color.Red, 2012));
                    foreach (var client in Program.GamePool)
                        client.MessageBox("Guerra Mensal PK  começou premio 250k ! quer ir?",
                            p => { p.Entity.Teleport(1002, 429, 241); }, null);
                }
                if (Now64.Hour == 20 && Now64.Minute >= 15 && MonthlyPKWar)
                {
                    MonthlyPKWar = false;
                    Kernel.SendWorldMessage(new Message("fim da Guerra PK Mensal!", Color.Red, Message.Center));
                }
            }
            #endregion

            #region WORLD FELIPE SOOUZA
            #region LastManPK
            if ((Now64.Minute == 30) && (Now64.Second == 01))
            {
                Kernel.SendWorldMessage(new Message("LastMan Começou!", Color.White, Message.TopLeft), Program.GamePool);
                foreach (var client in Program.GamePool)
                    client.MessageBox("O Last Man Standing começou! Deseja entrar?",
                        p => { p.Entity.Teleport(1002, 477, 351); }, null, 60);
            }
            #endregion
            #region Daily PK
            if ((Now64.Minute == 00) && (Now64.Second == 15))
            {
                Kernel.SendWorldMessage(new Message("DailyPk COMEÇOU #00!!", Color.White, Message.TopLeft), Program.GamePool);
                foreach (var client in Program.GamePool)
                    client.MessageBox("O evento Daily PK começou! Deseja entrar?",
                        p => { p.Entity.Teleport(1002, 463, 352); }, null, 60);
            }
            #endregion
            #region TheHellPK
            if ((Now64.Minute == 42) && (Now64.Second == 08))
            {
                Kernel.SendWorldMessage(new Message("TheHellPK COMEÇOU #00!", Color.White, Message.TopLeft), Program.GamePool);
                foreach (var client in Program.GamePool)
                    client.MessageBox("O evento TheHell PK começou! Deseja entrar?",
                        p => { p.Entity.Teleport(1002, 466, 350); }, null, 60);
            }
            #endregion
            #region DamagePK
            if ((Now64.Minute == 26) && (Now64.Second == 08))
            {
                Kernel.SendWorldMessage(new Message("DamagePK COMEÇOU #00!!", Color.White, Message.TopLeft), Program.GamePool);
                foreach (var client in Program.GamePool)
                    client.MessageBox("O evento DamagePK começou! Deseja entrar?",
                        p => { p.Entity.Teleport(1002, 459, 350); }, null, 60);
            }
            #endregion
            #region BlackHeartPK
            if ((Now64.Minute == 36) && (Now64.Second == 08))
            {
                Kernel.SendWorldMessage(new Message("O evento BlackHeartPK começou!", Color.White, Message.TopLeft), Program.GamePool);
                foreach (var client in Program.GamePool)
                    client.MessageBox("O evento BlackHeartPK começou! Deseja entrar?",
                        p => { p.Entity.Teleport(1002, 470, 350); }, null, 60);
            }
            #endregion
            #region GuiltyGearPK
            if ((Now64.Minute == 50) && (Now64.Second == 08))
            {
                Kernel.SendWorldMessage(new Message("O evento GuiltyGearPK começou!", Color.White, Message.TopLeft), Program.GamePool);
                foreach (var client in Program.GamePool)
                    client.MessageBox("O evento GuiltyGearPK começou! Deseja entrar?",
                        p => { p.Entity.Teleport(1002, 473, 350); }, null, 60);
            }
            #endregion
            #region PK DOS MELHORS
            if ((Now64.Minute == 15) && (Now64.Second == 01))
            {
                Kernel.SendWorldMessage(new Message("O PK DOS MELHORES COMEÇOU!", Color.White, Message.TopLeft), Program.GamePool);
                foreach (var client in Program.GamePool)
                    client.MessageBox("O PK dos Melhores começou! Deseja entrar?",
                        p => { p.Entity.Teleport(1002, 452, 351); }, null, 60);
            }
            #endregion
            #region pk Campeao
            if ((Now64.Minute == 10) && (Now64.Second == 01))
            {
                Kernel.SendWorldMessage(new Message("O PK Campeão começou!", Color.White, Message.TopLeft), Program.GamePool);
                foreach (var client in Program.GamePool)
                    client.MessageBox("O PK Campeão começou! Deseja entrar?",
                        p => { p.Entity.Teleport(1002, 455, 352); }, null, 60);
            }
            #endregion
            #endregion







            #region GuildWar
            if (GuildWar.IsWar)
            {
                if (Time32.Now > GuildWar.ScoreSendStamp.AddSeconds(3))
                {
                    GuildWar.ScoreSendStamp = Time32.Now;
                    GuildWar.SendScores();
                }
                if (!GuildWar.Flame10th)
                {
                    if (Now64.DayOfWeek == DayOfWeek.Sunday && Now64.Hour == 19 && Now64.Minute == 00)
                    {
                        GuildWar.Flame10th = true;
                        Kernel.SendWorldMessage(new Message("Você já pode acender a 10ª Chama!", Color.White, Message.Center), Program.GamePool);
                    }
                }
            }
            if ((Now64.Hour >= 12 && Now64.Hour <= 19) && Now64.DayOfWeek == DayOfWeek.Sunday)
            {
                if (!GuildWar.IsWar)
                {
                    GuildWar.Start();
                    foreach (var client in Program.GamePool)
                        if (client.Entity.GuildID != 0)
                            client.MessageBox("GuildWar esta em andamento quer ir para esta guerra?",
                                p => { p.Entity.Teleport(1002, 350, 337); }, null);
                }
            }
            if (GuildWar.IsWar)
            {
                if ((Now64.Hour == 20 && Now64.Second <= 3) && Now64.DayOfWeek == DayOfWeek.Sunday)
                {
                    GuildWar.Flame10th = false;
                    GuildWar.End();
                }
            }
            #endregion
            #region DTM
            if (Now64.DayOfWeek == DayOfWeek.Tuesday && (Now64.Hour == 14 || Now64.Hour == 22) && Now64.Minute == 59 && Now64.Second <= 2)
            {
                DeathMatch.SendTimer();
                Kernel.SendWorldMessage(new Message("Team Death começara em 1 minuto!", Color.White, Message.TopLeft), Program.GamePool);
            }
            #endregion
            #region Quiz Show
            if (Now64.DayOfWeek == DayOfWeek.Saturday || Now64.DayOfWeek == DayOfWeek.Sunday)
            {
                if (Now64.Hour == 3 || Now64.Hour == 13 || Now64.Hour == 20)
                {
                    if (Now64.Minute == 55 && Now64.Second <= 2)
                    {
                        Kernel.SendWorldMessage(new Network.GamePackets.Message("Quiz show começa em 5 minutos!", System.Drawing.Color.White, Conquer_Online_Server.Network.GamePackets.Message.TopLeft), Program.GamePool);
                    }
                }
                if (Now64.Hour == 4 || Now64.Hour == 14 || Now64.Hour == 21)
                {
                    if (Now64.Minute == 0 && Now64.Second <= 2)
                    {
                        Kernel.QuizShow.Start();
                        Kernel.SendWorldMessage(new Network.GamePackets.Message("O Quiz Show começou!", System.Drawing.Color.White, Conquer_Online_Server.Network.GamePackets.Message.TopLeft), Program.GamePool);
                    }
                }
            }
            #endregion
            #region Elite PK Tournament 
            //#warning EPK FRIDAY NOT EVERYDAY AT 19 NOT 13 // aqui foi alterado agora toda sexta
            if (Now64.DayOfWeek == DayOfWeek.Friday && Now64.Hour == ElitePK.EventTime && Now64.Minute >= 55 && !ElitePKTournament.TimersRegistered)
            {
                ElitePKTournament.RegisterTimers();
                ElitePKBrackets brackets = new ElitePKBrackets(true, 0);
                brackets.Type = ElitePKBrackets.EPK_State;
                brackets.OnGoing = true;
                foreach (var client in Program.GamePool)
                {
                    client.Send(brackets);
                    client.MessageBox("Elite Tournament PK começou deseja ir",
                        p => { p.Entity.Teleport(1002, 431, 250); }, null);
                }
            }
            if (Now64.DayOfWeek == DayOfWeek.Thursday && Now64.Hour >= ElitePK.EventTime + 1 && ElitePKTournament.TimersRegistered)
            {
                bool done = true;
                foreach (var epk in ElitePKTournament.Tournaments)
                    if (epk.Players.Count != 0)
                        done = false;
                if (done)
                {
                    ElitePKTournament.TimersRegistered = false;
                    ElitePKBrackets brackets = new ElitePKBrackets(true, 0);
                    brackets.Type = ElitePKBrackets.EPK_State;
                    brackets.OnGoing = false;
                    foreach (var client in Program.GamePool)
                        client.Send(brackets);
                }
            }
            #endregion
            /*#region Capture the flag // região isolada capature the flag

            if (Now64.Hour == 21 && Now64.Minute >= 30 && !CaptureTheFlag.IsWar)
            {
                CaptureTheFlag.IsWar = true;
                CaptureTheFlag.StartTime = DateTime.Now;
                foreach (var guild in Kernel.Guilds.Values)
                {
                    guild.CTFFlagScore = 0;
                    guild.CTFPoints = 0;
                }
                foreach (var client in Program.GamePool)
                    if (client.Entity.GuildID != 0)
                        client.MessageBox("Capture the flag começou deseja ir",
                            p => { p.Entity.Teleport(1002, 414, 264); }, null);
            }

            if (CaptureTheFlag.IsWar)
            {
                //Program.World.CTF.SendUpdates();
                if (Now64 > CaptureTheFlag.StartTime.AddHours(1))
                {
                    CaptureTheFlag.IsWar = false;
                    CaptureTheFlag.Close();
                }
            }
            if (CTF != null)
                CTF.SpawnFlags();
            #endregion */
#if OBSOLETE_CLASSPK
           /* if (Now64.Hour == 20 && Now64.Minute == 00 && Now64.DayOfWeek == DayOfWeek.Sunday)
            {
                if (Now64.Minute == 00 && Now64.Second <= 2)
                {
                    Game.ClassPk.SignUp();
                    Conquer_Online_Server.Kernel.SendWorldMessage(new Conquer_Online_Server.Network.GamePackets.Message("A Guerra PK de Trojan começou! Fale com o ClassPkEnvoy em Twin City para se inscrever antes das 20:05!", System.Drawing.Color.White, Conquer_Online_Server.Network.GamePackets.Message.TopLeft), Program.GamePool);
                }
            }
            if (Now64.Hour == 20 && Now64.Minute == 00 && Now64.DayOfWeek == DayOfWeek.Sunday)
            {
                if (Now64.Minute == 00 && Now64.Second <= 2)
                {
                    Game.ClassPk.SignUp1();
                    Conquer_Online_Server.Kernel.SendWorldMessage(new Conquer_Online_Server.Network.GamePackets.Message("A Guerra PK de Warrior começou! Fale com o ClassPkEnvoy em Twin City para se inscrever antes das 20:05!", System.Drawing.Color.White, Conquer_Online_Server.Network.GamePackets.Message.TopLeft), Program.GamePool);
                }
            }
            if (Now64.Hour == 20 && Now64.Minute == 00 && Now64.DayOfWeek == DayOfWeek.Monday)
            {
                if (Now64.Minute == 00 && Now64.Second <= 2)
                {
                    Game.ClassPk.SignUp6();
                    Conquer_Online_Server.Kernel.SendWorldMessage(new Conquer_Online_Server.Network.GamePackets.Message("A Guerra PK de Fire Taoist começou! Fale com o ClassPkEnvoy em Twin City para se inscrever antes das 20:05!", System.Drawing.Color.White, Conquer_Online_Server.Network.GamePackets.Message.TopLeft), Program.GamePool);
                }
            }
            if (Now64.Hour == 20 && Now64.Minute == 00 && Now64.DayOfWeek == DayOfWeek.Tuesday)
            {
                if (Now64.Minute == 00 && Now64.Second <= 2)
                {
                    Game.ClassPk.SignUp3();
                    Conquer_Online_Server.Kernel.SendWorldMessage(new Conquer_Online_Server.Network.GamePackets.Message("A Guerra PK de Ninja começou! Fale com o ClassPkEnvoy em Twin City para se inscrever antes das 20:05!", System.Drawing.Color.White, Conquer_Online_Server.Network.GamePackets.Message.TopLeft), Program.GamePool);
                }
            }
            if (Now64.Hour == 8 && Now64.Minute == 00 && Now64.DayOfWeek == DayOfWeek.Monday)
            {
                Game.ClassPk.SignUp8();
                Conquer_Online_Server.Kernel.SendWorldMessage(new Conquer_Online_Server.Network.GamePackets.Message("A Guerra PK de Pirate começou! Fale com o ClassPkEnvoy em Twin City para se inscrever antes das 20:05!", System.Drawing.Color.White, Conquer_Online_Server.Network.GamePackets.Message.TopLeft), Program.GamePool);
            }
            if (Now64.Hour == 20 && Now64.Minute == 00 && Now64.DayOfWeek == DayOfWeek.Thursday)
            {
                if (Now64.Minute == 00 && Now64.Second <= 2)
                {
                    Game.ClassPk.SignUp5();
                    Conquer_Online_Server.Kernel.SendWorldMessage(new Conquer_Online_Server.Network.GamePackets.Message("A Guerra PK de Water Taoist começou! Fale com o ClassPkEnvoy em Twin City para se inscrever antes das 20:05!", System.Drawing.Color.White, Conquer_Online_Server.Network.GamePackets.Message.TopLeft), Program.GamePool);
                }
            }
            if (Now64.Hour == 20 && Now64.Minute == 00 && Now64.DayOfWeek == DayOfWeek.Wednesday)
            {
                if (Now64.Minute == 00 && Now64.Second <= 2)
                {
                    Game.ClassPk.SignUp4();
                    Conquer_Online_Server.Kernel.SendWorldMessage(new Conquer_Online_Server.Network.GamePackets.Message("A Guerra PK de Monk começou! Fale com o ClassPkEnvoy em Twin City para se inscrever antes das 20:05!", System.Drawing.Color.White, Conquer_Online_Server.Network.GamePackets.Message.TopLeft), Program.GamePool);
                }
            }
            if (Now64.Hour == 20 && Now64.Minute == 00 && Now64.DayOfWeek == DayOfWeek.Friday)
            {
                if (Now64.Minute == 00 && Now64.Second <= 2)
                {
                    Game.ClassPk.SignUp2();
                    Conquer_Online_Server.Kernel.SendWorldMessage(new Conquer_Online_Server.Network.GamePackets.Message("A Guerra PK de Archer começou! Fale com o ClassPkEnvoy em Twin City para se inscrever antes das 20:05!", System.Drawing.Color.White, Conquer_Online_Server.Network.GamePackets.Message.TopLeft), Program.GamePool);
                }
            }*/
#endif
        }
        private void ServerFunctions(int time)
        {
            var kvpArray = Kernel.GamePool.ToArray();
            foreach (var kvp in kvpArray)
                if (kvp.Value == null || kvp.Value.Entity == null)
                    Kernel.GamePool.Remove(kvp.Key);
            Program.GamePool = Kernel.GamePool.Values.ToArray();

            Console.Title = Constants.ServerName + " Players online: " + Kernel.GamePool.Count + "/" + Program.PlayerCap;

            new Database.MySqlCommand(Database.MySqlCommandType.UPDATE).Update("configuration").Set("GuildID", Game.ConquerStructures.Society.Guild.GuildCounter.Now).Set("ItemUID", Network.GamePackets.ConquerItem.ItemUID.Now).Where("Server", Constants.ServerName).Execute();
            Database.EntityVariableTable.Save(0, Program.Vars);
            if (Kernel.BlackSpoted.Values.Count > 0)
            {
                foreach (var spot in Kernel.BlackSpoted.Values)
                {
                    if (Time32.Now >= spot.BlackSpotStamp.AddSeconds(spot.BlackSpotStepSecs))
                    {
                        if (spot.Dead && spot.EntityFlag == EntityFlag.Player)
                        {
                            foreach (var h in Program.GamePool)
                            {
                                h.Send(Program.BlackSpotPacket.ToArray(false, spot.UID));
                            }
                            Kernel.BlackSpoted.Remove(spot.UID);
                            continue;
                        }
                        foreach (var h in Program.GamePool)
                        {
                            h.Send(Program.BlackSpotPacket.ToArray(false, spot.UID));
                        }
                        spot.IsBlackSpotted = false;
                        Kernel.BlackSpoted.Remove(spot.UID);
                    }
                }
            }
            DateTime Now = DateTime.Now;

            if (Now > Game.ConquerStructures.Broadcast.LastBroadcast.AddMinutes(1))
            {
                if (Game.ConquerStructures.Broadcast.Broadcasts.Count > 0)
                {
                    Game.ConquerStructures.Broadcast.CurrentBroadcast = Game.ConquerStructures.Broadcast.Broadcasts[0];
                    Game.ConquerStructures.Broadcast.Broadcasts.Remove(Game.ConquerStructures.Broadcast.CurrentBroadcast);
                    Game.ConquerStructures.Broadcast.LastBroadcast = Now;
                    Kernel.SendWorldMessage(new Network.GamePackets.Message(Game.ConquerStructures.Broadcast.CurrentBroadcast.Message, "ALLUSERS", Game.ConquerStructures.Broadcast.CurrentBroadcast.EntityName, System.Drawing.Color.Red, Network.GamePackets.Message.BroadcastMessage), Program.GamePool);
                }
                else
                    Game.ConquerStructures.Broadcast.CurrentBroadcast.EntityID = 1;
            }


            if (Now > Program.LastRandomReset.AddMinutes(30))
            {
                Program.LastRandomReset = Now;
                Kernel.Random = new FastRandom(Program.RandomSeed);
            }
            Program.Today = Now.DayOfWeek;
        }
        private void ArenaFunctions(int time)
        {
            Game.Arena.EngagePlayers();
            Game.Arena.CheckGroups();
            Game.Arena.VerifyAwaitingPeople();
            Game.Arena.Reset();
        }
        private void TeamArenaFunctions(int time)
        {
            Game.TeamArena.EngagePlayers();
            Game.TeamArena.CheckGroups();
            Game.TeamArena.VerifyAwaitingPeople();
            Game.TeamArena.Reset();
        }
        private void ChampionFunctions(int time)
        {
            Game.Champion.EngagePlayers();
            Game.Champion.CheckGroups();
            Game.Champion.VerifyAwaitingPeople();
            Game.Champion.Reset();
        }

        #region Funcs
        public static void Execute(Action<int> action, int timeOut = 0, ThreadPriority priority = ThreadPriority.Normal)
        {
            GenericThreadPool.Subscribe(new LazyDelegate(action, timeOut, priority));
        }
        public static void Execute<T>(Action<T, int> action, T param, int timeOut = 0, ThreadPriority priority = ThreadPriority.Normal)
        {
            GenericThreadPool.Subscribe<T>(new LazyDelegate<T>(action, timeOut, priority), param);
        }
        public static IDisposable Subscribe(Action<int> action, int period = 1, ThreadPriority priority = ThreadPriority.Normal)
        {
            return GenericThreadPool.Subscribe(new TimerRule(action, period, priority));
        }
        public static IDisposable Subscribe<T>(Action<T, int> action, T param, int timeOut = 0, ThreadPriority priority = ThreadPriority.Normal)
        {
            return GenericThreadPool.Subscribe<T>(new TimerRule<T>(action, timeOut, priority), param);
        }
        public static IDisposable Subscribe<T>(TimerRule<T> rule, T param, StandalonePool pool)
        {
            return pool.Subscribe<T>(rule, param);
        }
        public static IDisposable Subscribe<T>(TimerRule<T> rule, T param, StaticPool pool)
        {
            return pool.Subscribe<T>(rule, param);
        }
        public static IDisposable Subscribe<T>(TimerRule<T> rule, T param)
        {
            return GenericThreadPool.Subscribe<T>(rule, param);
        }
        #endregion
    }
}
