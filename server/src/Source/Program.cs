using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Conquer_Online_Server.Network;
using Conquer_Online_Server.Database;
using Conquer_Online_Server.Network.Sockets;
using Conquer_Online_Server.Network.AuthPackets;
using Conquer_Online_Server.Game.ConquerStructures.Society;
using Conquer_Online_Server.Game;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Conquer_Online_Server.Interfaces;
using System.Text;
using Conquer_Online_Server.Network.GamePackets;

namespace Conquer_Online_Server
{
    class Program
    {
        public static void WriteError(Exception ex)
        {
            Console.WriteLine(ex.ToString() + " (Reportar cualquier bug a Privadoco Server)");
        }
        public static void WriteLine(Exception Exc)
        {
            try
            {
                Console.WriteLine(Exc.ToString());

            }
            catch { }
        }
        public static void WriteLine(string Line)
        {
            try
            {
                Console.WriteLine(Line);
            }
            catch { }
        }
        public static void WriteLine()
        {
            try
            {
                Console.WriteLine("");
            }
            catch { }
        }
        //1067 = news window

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public static int PlayerCap = 400;
        public static ServerSocket AuthServer;
        public static ServerSocket GameServer;
        public static Counter EntityUID;
        public static string GameIP;
        public static string GameCoder;
        public static DayOfWeek Today;
        public static ushort GamePort;
        public static ushort AuthPort;
        public static DateTime StartDate;
        public static DateTime RestartDate = DateTime.Now.AddHours(24);
        public static World World;
        public static Client.GameClient[] GamePool = new Client.GameClient[0];
        public static VariableVault Vars;

        public static int RandomSeed = 0;
        private static readonly ManualResetEvent ShutdownEvent = new ManualResetEvent(false);
        private static int ShutdownStarted = 0;

        private static void GracefulShutdown()
        {
            if (Interlocked.Exchange(ref ShutdownStarted, 1) != 0)
                return;
            try
            {
                Console.WriteLine("Shutting down server gracefully...");
                if (GameServer != null) GameServer.Disable();
                if (AuthServer != null) AuthServer.Disable();

                var pool = Program.GamePool;
                if (pool != null && pool.Length > 0)
                {
                    var snapshot = pool.ToArray();
                    try
                    {
                        using (var conn = Database.DataHolder.MySqlConnection)
                        {
                            conn.Open();
                            foreach (Client.GameClient client in snapshot)
                            {
                                try
                                {
                                    if (client == null || client.Entity == null) continue;
                                    if (client.Account != null) client.Account.Save();
                                    Database.EntityTable.UpdateOnlineStatus(client, false, conn);
                                    Database.EntityTable.SaveEntity(client, conn);
                                    Database.SkillTable.SaveProficiencies(client, conn);
                                    Database.SkillTable.SaveSpells(client, conn);
                                    if (client.ArenaStatistic != null) Database.ArenaTable.SaveArenaStatistics(client.ArenaStatistic, conn);
                                    if (client.TeamArenaStatistic != null) Database.TeamArenaTable.SaveArenaStatistics(client.TeamArenaStatistic, conn);
                                    if (client.ChampionStats != null) Database.ChampionTable.SaveStatistics(client.ChampionStats, conn);
                                }
                                catch { }
                            }
                        }
                    }
                    catch { }
                    foreach (Client.GameClient client in snapshot)
                    {
                        try { client.Disconnect(); } catch { }
                    }
                }
                if (GuildWar.IsWar)
                {
                    try { GuildWar.End(); } catch { }
                }
                if (Network.GamePackets.ConquerItem.ItemUID != null && !string.IsNullOrEmpty(Constants.ServerName))
                {
                    new Database.MySqlCommand(Database.MySqlCommandType.UPDATE)
                        .Update("configuration")
                        .Set("ItemUID", Network.GamePackets.ConquerItem.ItemUID.Now)
                        .Where("Server", Constants.ServerName)
                        .Execute();
                }
                if (Vars != null)
                {
                    Database.EntityVariableTable.Save(0, Vars);
                }
                Console.WriteLine("Graceful shutdown complete.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Shutdown save warning: " + ex.Message);
            }
            finally
            {
                ShutdownEvent.Set();
            }
        }

        static void Main(string[] args)
        {
            // Ensure working directory matches executable directory (critical for systemd/Wine)
            try { Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory); } catch { }

            AppDomain.CurrentDomain.ProcessExit += (s, e) => GracefulShutdown();
            System.Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                GracefulShutdown();
                Environment.Exit(0);
            };

            Time32 Start = Time32.Now;
            RandomSeed = Convert.ToInt32(DateTime.Now.Ticks.ToString().Remove(DateTime.Now.Ticks.ToString().Length / 2));
            Kernel.Random = new FastRandom(RandomSeed);
            StartDate = DateTime.Now;
            Console.Title = "Conquer Online 2.0 Server";
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("----- Conquer Online 2.0 Production Server (Release) -----");
            Console.WriteLine("Loading server configuration...");
            ALEXPC = true;
            string ConfigFileName = "configuration.ini";
            IniFile IniFile = new IniFile(ConfigFileName);
            GameIP = IniFile.ReadString("configuration", "IP").Trim();
            GameCoder = IniFile.ReadString("configuration", "GameCoder");
            GamePort = IniFile.ReadUInt16("configuration", "GamePort");
            AuthPort = IniFile.ReadUInt16("configuration", "AuthPort");
            Constants.ServerName = IniFile.ReadString("configuration", "ServerName").Trim();
            ServerSocket.ConnectionCooldownMs = IniFile.ReadInt32("configuration", "ConnectionCooldownMs", 0);

            if (AuthPort == 0) AuthPort = 9958;
            if (GamePort == 0) GamePort = 5816;
            if (string.IsNullOrEmpty(GameIP))
            {
                Console.WriteLine("ERROR: Missing [configuration] IP in configuration.ini");
                Environment.Exit(1);
                return;
            }
            System.Net.IPAddress parsedIp;
            if (!System.Net.IPAddress.TryParse(GameIP, out parsedIp))
            {
                try
                {
                    var addrs = System.Net.Dns.GetHostAddresses(GameIP);
                    var ipv4 = addrs.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                    if (ipv4 != null)
                    {
                        Console.WriteLine("Resolved domain '" + GameIP + "' -> " + ipv4.ToString());
                        GameIP = ipv4.ToString();
                    }
                }
                catch (Exception dnsEx)
                {
                    Console.WriteLine("WARNING: Could not resolve GameIP '" + GameIP + "': " + dnsEx.Message);
                }
            }
            Console.WriteLine(string.Format("Config: ServerName={0} | ExternalIP={1} | AuthPort={2} | GamePort={3} | CooldownMs={4}",
                Constants.ServerName, GameIP, AuthPort, GamePort, ServerSocket.ConnectionCooldownMs));

            Database.DataHolder.CreateConnection();
            EntityUID = new Counter(0);
            try
            {
                using (MySqlCommand cmd = new MySqlCommand(MySqlCommandType.SELECT))
                {
                    cmd.Select("configuration").Where("Server", Constants.ServerName);
                    using (MySqlReader r = new MySqlReader(cmd))
                    {
                        if (r.Read())
                        {
                            EntityUID = new Counter(r.ReadUInt32("EntityID"));
                            Game.ConquerStructures.Society.Guild.GuildCounter = new Conquer_Online_Server.Counter(r.ReadUInt32("GuildID"));
                            Network.GamePackets.ConquerItem.ItemUID = new Conquer_Online_Server.Counter(r.ReadUInt32("ItemUID"));
                            Constants.ExtraExperienceRate = r.ReadUInt32("ExperienceRate");
                            Constants.ExtraSpellRate = r.ReadUInt32("ProficiencyExperienceRate");
                            Constants.ExtraProficiencyRate = r.ReadUInt32("SpellExperienceRate");
                            Constants.MoneyDropRate = r.ReadUInt32("MoneyDropRate");
                            Constants.MoneyDropMultiple = r.ReadUInt32("MoneyDropMultiple");
                            Constants.ConquerPointsDropRate = r.ReadUInt32("ConquerPointsDropRate");
                            Constants.ConquerPointsDropMultiple = r.ReadUInt32("ConquerPointsDropMultiple");
                            Constants.ItemDropRate = r.ReadUInt32("ItemDropRate");
                            Constants.ItemDropQualityRates = r.ReadString("ItemDropQualityString").Split('~');
                            Constants.WebAccExt = r.ReadString("AccountWebExt");
                            Constants.WebVoteExt = r.ReadString("VoteWebExt");
                            Constants.WebDonateExt = r.ReadString("DonateWebExt");
                            Constants.ServerWebsite = r.ReadString("ServerWebsite");
                            PlayerCap = r.ReadInt32("PlayerCap");
                            Database.EntityVariableTable.Load(0, out Vars);
                        }
                    }
                }
            }
            catch (Exception dbEx)
            {
                Console.WriteLine("Database connection error: " + dbEx.Message);
                Environment.Exit(1);
                return;
            }

            if (EntityUID.Now == 0)
            {
                Console.WriteLine("Database error: configuration row not found for Server='" + Constants.ServerName + "' or MySQL unreachable. Exiting.");
                Environment.Exit(1);
                return;
            }
            Console.WriteLine("Initializing database.");
            World = new World();
            World.Init();
            Database.ConquerItemTable.ClearNulledItems();
            Game.Flags.LoadFlags();
            Database.NameChange.UpdateNames();
            Database.ConquerItemInformation.Load();
            SubClassTable.LoadKungFuRanks();
            //Database.ItemLog.CleanUp();
            Database.DataHolder.ReadStats();
            Database.MonsterInformation.Load();
            Database.IPBan.Load();
            Database.SpellTable.Load();
            Database.ShopFile.Load();
            Database.HonorShop.Load();
            Database.RacePointShop.Load();
            Database.ChampionShop.Load();
            Database.EShopFile.Load();
            Database.EShopV2File.Load();
            Database.MapsTable.Load();
            Database.NobilityTable.Load();
            Database.ArenaTable.Load();
            Database.TeamArenaTable.Load();
            Database.ChampionTable.Load();
            //Database.AddingInformationTable.Load();
            Database.GuildTable.Load();
            // Database.Advertise.Load();
            Database.LotteryTable.Load();
            Database.DROP_SOULS.LoadDrops();
            Database.ChiTable.LoadAllChi();
            Database.JiangTable.LoadJi();
            Database.PoketTables.LoadTables();
            Kernel.QuizShow = new Game.ConquerStructures.QuizShow();
            Refinery.Load();
            Map.CreateTimerFactories();
            Database.DMaps.Load();
            Game.Screen.CreateTimerFactories();
            GamePool = new Client.GameClient[0];
            new Game.Map(1002, Database.DMaps.MapPaths[1002]);
            new Game.Map(1038, Database.DMaps.MapPaths[1038]);
            new Game.Map(2071, Database.DMaps.MapPaths[2071]);
            Game.GuildWar.Initiate();
            new Game.Map(CrossServer.mapid, Database.DMaps.MapPaths[CrossServer.mapid]);
            Game.CrossServer.Initiate();
            CrossServer.Load();
            new Game.Map(1509, Database.DMaps.MapPaths[1509]);
            new Game.Map(10002, 2021, Database.DMaps.MapPaths[2021]);
            new Game.Map(8883, 1004, Database.DMaps.MapPaths[1004]);
            Constants.PKFreeMaps.Add(8883);
            Constants.PKFreeMaps.Add(CrossServer.mapid);
            Game.ClanWar.Initiate();
            Console.WriteLine("Guild war initializated.");
            Game.EliteGuildWar.EliteGwint();
            Console.WriteLine("Elite Guild war initializated.");
            Console.WriteLine("Loading Game Clans.");
            Clan.LoadClans();
            Console.WriteLine("House Table Loaded.");
            Database.ReincarnationTable.Load();
            Console.WriteLine("Reincarnat Table Loaded.");
            //Database.flower.Loadflower();
            Console.WriteLine("Flower Table Loaded.");

            Network.Cryptography.AuthCryptography.PrepareAuthCryptography();
            World.CreateTournaments();

            new MySqlCommand(MySqlCommandType.UPDATE).Update("entities").Set("Online", 0).Execute();
            Console.WriteLine("Initializing sockets.");

            AuthServer = new ServerSocket();
            AuthServer.OnClientConnect += AuthServer_OnClientConnect;
            AuthServer.OnClientReceive += AuthServer_OnClientReceive;
            AuthServer.OnClientDisconnect += AuthServer_OnClientDisconnect;
            AuthServer.Enable(AuthPort, "0.0.0.0");

            GameServer = new ServerSocket();
            GameServer.OnClientConnect += GameServer_OnClientConnect;
            GameServer.OnClientReceive += GameServer_OnClientReceive;
            GameServer.OnClientDisconnect += GameServer_OnClientDisconnect;
            GameServer.Enable(GamePort, "0.0.0.0");

            Console.WriteLine("Auth server online.");
            Console.WriteLine("Game server online.");

            Console.WriteLine();
            Console.WriteLine("----- Oblivion------");
            Console.WriteLine("Server loaded in " + (Time32.Now - Start) + " milliseconds.");


            GC.Collect();
            WorkConsole();
            //}
        }

        #region Exceptions & Logs
        public static void AddVendorLog(String vendor, string buying, string moneyamount, ConquerItem Item)
        {
            String folderN = DateTime.Now.Year + "-" + DateTime.Now.Month,
                Path = "gmlogs\\VendorLogs\\",
                NewPath = System.IO.Path.Combine(Path, folderN);
            if (!File.Exists(NewPath + folderN))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(Path, folderN));
            }
            if (!File.Exists(NewPath + "\\" + DateTime.Now.Day + ".txt"))
            {
                using (System.IO.FileStream fs = System.IO.File.Create(NewPath + "\\" + DateTime.Now.Day + ".txt"))
                {
                    fs.Close();
                }
            }

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(NewPath + "\\" + DateTime.Now.Day + ".txt", true))
            {
                file.WriteLine("------------------------------------------------------------------------------------");
                file.WriteLine("{0} HAS BOUGHT AN ITEM : {2} FROM {1} SHOP - for {3}", vendor, buying, Item.ToLog(), moneyamount);
                file.WriteLine("------------------------------------------------------------------------------------");
            }
        }
        public static void AddGMCommand(string gm, string commandStr)
        {
            String folderN = DateTime.Now.Year + "-" + DateTime.Now.Month,
                Path = "gmlogs\\GMCommandsLog\\",
                NewPath = System.IO.Path.Combine(Path, folderN);
            if (!File.Exists(NewPath + folderN))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(Path, folderN));
            }
            if (!File.Exists(NewPath + "\\" + DateTime.Now.Day + ".txt"))
            {
                using (System.IO.FileStream fs = System.IO.File.Create(NewPath + "\\" + DateTime.Now.Day + ".txt"))
                {
                    fs.Close();
                }
            }

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(NewPath + "\\" + DateTime.Now.Day + ".txt", true))
            {
                file.WriteLine(gm + commandStr);
            }
        }
        public static void AddDropLog(String Name, ConquerItem Item)
        {
            String folderN = DateTime.Now.Year + "-" + DateTime.Now.Month,
                Path = "gmlogs\\droplogs\\",
                NewPath = System.IO.Path.Combine(Path, folderN);
            if (!File.Exists(NewPath + folderN))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(Path, folderN));
            }
            string path = NewPath + "\\" + DateTime.Now.Day + ".txt";
            if (!File.Exists(path)) File.AppendAllText(path, "");

            string text = "------------------------------------------------------------------------------------"
                + Environment.NewLine + string.Format("Player {0} HAS DROPPED AN ITEM : {1} -", Name, Item.ToLog())
                + Environment.NewLine + "------------------------------------------------------------------------------------";
            File.AppendAllText(path, text);
        }
        public static void AddTradeLog(Conquer_Online_Server.Game.ConquerStructures.Trade first, String firstN, Conquer_Online_Server.Game.ConquerStructures.Trade second, String secondN)
        {
            String folderN = DateTime.Now.Year + "-" + DateTime.Now.Month,
                Path = "gmlogs\\tradelogs\\",
                NewPath = System.IO.Path.Combine(Path, folderN);
            if (!File.Exists(NewPath + folderN))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(Path, folderN));
            }
            if (!File.Exists(NewPath + "\\" + DateTime.Now.Day + ".txt"))
            {
                using (System.IO.FileStream fs = System.IO.File.Create(NewPath + "\\" + DateTime.Now.Day + ".txt"))
                {
                    fs.Close();
                }
            }

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(NewPath + "\\" + DateTime.Now.Day + ".txt", true))
            {
                file.WriteLine("************************************************************************************");
                file.WriteLine("First Person TradeLog ( {0} ) -", firstN);
                file.WriteLine("Gold Traded: " + first.Money);
                file.WriteLine("Conquer Points Traded: " + first.ConquerPoints);

                for (int i = 0; i < first.Items.Count; i++)
                {
                    file.WriteLine("------------------------------------------------------------------------------------");
                    file.WriteLine("Item : " + first.Items[i].ToLog());
                    file.WriteLine("------------------------------------------------------------------------------------");
                }
                file.WriteLine("Second Person TradeLog ( {0} ) -", secondN);
                file.WriteLine("Gold Traded: " + second.Money);
                file.WriteLine("Conquer Points Traded: " + second.ConquerPoints);

                for (int i = 0; i < second.Items.Count; i++)
                {
                    file.WriteLine("------------------------------------------------------------------------------------");
                    file.WriteLine("Item : " + second.Items[i].ToLog());
                    file.WriteLine("------------------------------------------------------------------------------------");
                }
                file.WriteLine("************************************************************************************");
            }
        }
        public static void AddWarLog(string War, string CPs, string name)
        {
            String folderN = DateTime.Now.Year + "-" + DateTime.Now.Month,
                    Path = "gmlogs\\Warlogs\\",
                    NewPath = System.IO.Path.Combine(Path, folderN);
            if (!File.Exists(NewPath + folderN))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(Path, folderN));
            }
            if (!File.Exists(NewPath + "\\" + DateTime.Now.Day + ".txt"))
            {
                using (System.IO.FileStream fs = System.IO.File.Create(NewPath + "\\" + DateTime.Now.Day + ".txt"))
                {
                    fs.Close();
                }
            }

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(NewPath + "\\" + DateTime.Now.Day + ".txt", true))
            {
                file.WriteLine(name + " got " + CPs + " CPs from the [" + War + "] as prize at " + DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second);
            }
        }
        static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            SaveException(e.Exception);
        }

        public static void SaveException(Exception e)
        {
            if (e == null) return;
            string siteName = (e.TargetSite != null && !string.IsNullOrEmpty(e.TargetSite.Name)) ? e.TargetSite.Name : "Unknown";
            if (siteName == "ThrowInvalidOperationException")
                return;
            if (e.Message != null && e.Message.Contains("String reference not set"))
                return;

            try
            {
                var dt = DateTime.Now;
                string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.UnhandledExceptionsPath.TrimStart('\\', '/'));
                string targetDir = Path.Combine(Path.Combine(baseDir, dt.Month + "-" + dt.Day), siteName);
                Directory.CreateDirectory(targetDir);

                string logFile = Path.Combine(targetDir, dt.Hour + "-" + dt.Minute + ".txt");
                List<string> Lines = new List<string>();
                Lines.Add("----Exception message----");
                Lines.Add(e.Message ?? string.Empty);
                Lines.Add("----End of exception message----");
                Lines.Add("----Stack trace----");
                Lines.Add(e.StackTrace ?? string.Empty);
                Lines.Add("----End of stack trace----");
                File.AppendAllLines(logFile, Lines);
            }
            catch { }
        }
        #endregion

        private static void WorkConsole()
        {
            while (!ShutdownEvent.WaitOne(0))
            {
                try
                {
                    string line = Console.ReadLine();
                    if (line == null)
                    {
                        // Stdin closed (headless / systemd / Wine background service): wait without burning CPU
                        Console.WriteLine("Running in headless mode (no interactive stdin). Waiting for stop signal...");
                        ShutdownEvent.WaitOne();
                        break;
                    }
                    CommandsAI(line);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Console loop error: " + e.Message);
                    Thread.Sleep(500);
                }
            }
        }
        public static DateTime LastRandomReset = DateTime.Now;
        public static Network.GamePackets.BlackSpotPacket BlackSpotPacket = new Network.GamePackets.BlackSpotPacket();
        public static bool ALEXPC = false;

        public static void CommandsAI(string command)
        {
            if (command == null)
                return;
            string[] data = command.Split(' ');
            switch (data[0])
            {

                case "@flushbans":
                    {
                        Database.IPBan.Load();
                        break;
                    }
               
                case "@alivetime":
                    {
                        DateTime now = DateTime.Now;
                        TimeSpan t2 = new TimeSpan(StartDate.ToBinary());
                        TimeSpan t1 = new TimeSpan(now.ToBinary());
                        Console.WriteLine("The server has been online " + (int)(t1.TotalHours - t2.TotalHours) + " hours, " + (int)((t1.TotalMinutes - t2.TotalMinutes) % 60) + " minutes.");
                        break;
                    }
                case "@online":
                    {
                        Console.WriteLine("Online players count: " + Kernel.GamePool.Count);
                        string line = "";
                        foreach (Client.GameClient pClient in Program.GamePool)
                            line += pClient.Entity.Name + ",";
                        if (line != "")
                        {
                            line = line.Remove(line.Length - 1);
                            Console.WriteLine("Players: " + line);
                        }
                        break;
                    }
                case "@memoryusage":
                    {
                        var proc = System.Diagnostics.Process.GetCurrentProcess();
                        Console.WriteLine("Thread count: " + proc.Threads.Count);
                        Console.WriteLine("Memory set(MB): " + ((double)((double)proc.WorkingSet64 / 1024)) / 1024);
                        proc.Close();
                        break;
                    }
                case "@save":
                    {
                        using (var conn = Database.DataHolder.MySqlConnection)
                        {
                            conn.Open();
                            foreach (Client.GameClient client in Program.GamePool)
                            {
                                client.Account.Save();
                                Database.EntityTable.SaveEntity(client, conn);
                                Database.SkillTable.SaveProficiencies(client,conn);
                                Database.SkillTable.SaveSpells(client,conn);
                                Database.ArenaTable.SaveArenaStatistics(client.ArenaStatistic, conn);
                                Database.TeamArenaTable.SaveArenaStatistics(client.TeamArenaStatistic, conn);
                                Database.ChampionTable.SaveStatistics(client.ChampionStats, conn);
                            }
                        }
                        new Database.MySqlCommand(Database.MySqlCommandType.UPDATE).Update("configuration").Set("ItemUID", Network.GamePackets.ConquerItem.ItemUID.Now).Where("Server", Constants.ServerName).Execute();
                    }
                    break;
                case "@playercap":
                    {
                        try
                        {
                            PlayerCap = int.Parse(data[1]);
                        }
                        catch
                        {

                        }
                        break;
                    }
                case "@exit":
                    {
                        new Database.MySqlCommand(Database.MySqlCommandType.UPDATE).Update("configuration").Set("ItemUID", Network.GamePackets.ConquerItem.ItemUID.Now).Where("Server", Constants.ServerName).Execute();
                        Database.EntityVariableTable.Save(0, Vars);

                        GameServer.Disable();
                        AuthServer.Disable();

                        var WC = Program.GamePool.ToArray();
                        foreach (Client.GameClient client in WC)
                            client.Disconnect();

                        if (GuildWar.IsWar)
                            GuildWar.End();
                        new Database.MySqlCommand(Database.MySqlCommandType.UPDATE).Update("configuration").Set("ItemUID", Network.GamePackets.ConquerItem.ItemUID.Now).Where("Server", Constants.ServerName).Execute();

                        Environment.Exit(0);
                    }
                    break;
                case "@showips":
                    {
                        bool f = !AuthServer.PrintoutIPs;
                        AuthServer.PrintoutIPs = GameServer.PrintoutIPs = f;
                        Console.WriteLine(string.Format("Showing forcing IPs? {0}", f));
                        break;
                    }
                case "@pressure":
                    {
                        Console.WriteLine("Genr: " + World.GenericThreadPool.ToString());
                        Console.WriteLine("Send: " + World.SendPool.ToString());
                        Console.WriteLine("Recv: " + World.ReceivePool.ToString());
                        break;
                    }
                case "@restart":
                    {
                        new Database.MySqlCommand(Database.MySqlCommandType.UPDATE).Update("configuration").Set("ItemUID", Network.GamePackets.ConquerItem.ItemUID.Now).Where("Server", Constants.ServerName).Execute();

                        GameServer.Disable();
                        AuthServer.Disable();

                        var WC = Program.GamePool.ToArray();
                        foreach (Client.GameClient client in WC)
                            client.Disconnect();

                        if (GuildWar.IsWar)
                            GuildWar.End();
                        new Database.MySqlCommand(Database.MySqlCommandType.UPDATE).Update("configuration").Set("ItemUID", Network.GamePackets.ConquerItem.ItemUID.Now).Where("Server", Constants.ServerName).Execute();

                        Application.Restart();
                        Environment.Exit(0);
                    }
                    break;
                case "@account":
                    {
                        Database.AccountTable account = new AccountTable(data[1]);
                        account.Password = data[2];
                        account.State = AccountTable.AccountState.Player;
                        account.Save();
                    }
                    break;
                case "@process":
                    {
                        HandleClipboardPacket(command);
                        break;
                    }
            }
        }
        public static void HandleClipboardPacket(string cmd)
        {
            string[] pData = cmd.Split(' ');
            long off = 0, type = 0, val = 0;
            if (pData.Length > 1)
            {
                //@process a:b:c
                //a: offset to modify
                //b: type: 1,2,4,8,u
                //c: value
                string[] oData = pData[1].Split(':');
                if (oData.Length == 3)
                {
                    off = long.Parse(oData[0]);
                    type = long.Parse(oData[1]);
                    if (oData[2] == "u")
                        val = 1337;
                    else
                        val = long.Parse(oData[2]);
                }
            }
            string Data = OSClipboard.GetText();
            //Data = Data.Substring(Data.IndexOf('{') + 1);
            //Data = Data.Replace("};", "").Replace(",", "").Replace("\r", "").Replace("\n", "");
            string[] num = Data.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            byte[] packet = new byte[num.Length + 8];
            for (int i = 0; i < num.Length; i++)
                packet[i] = byte.Parse(num[i], System.Globalization.NumberStyles.HexNumber);
            Writer.WriteUInt16((ushort)(packet.Length - 8), 0, packet);
            if (off != 0)
            {
                switch (type)
                {
                    case 1:
                        {
                            packet[(int)off] = (byte)val;
                            break;
                        }
                    case 2:
                        {
                            Writer.WriteUInt16((ushort)val, (int)off, packet);
                            break;
                        }
                    case 4:
                        {
                            Writer.WriteUInt32((uint)val, (int)off, packet);
                            break;
                        }
                    case 8:
                        {
                            Writer.WriteUInt64((ulong)val, (int)off, packet);
                            break;
                        }
                }
            }
            foreach (var client in Program.GamePool)
            {
                if (val == 1337 && type == 4)
                    Writer.WriteUInt32(client.Entity.UID, (int)off, packet);
                client.Send(packet);
            }
        }
        static void GameServer_OnClientReceive(byte[] buffer, int length, ClientWrapper obj)
        {
            if (obj.Connector == null)
            {
                obj.Disconnect();
                return;
            }

            Client.GameClient Client = obj.Connector as Client.GameClient;

            if (Client.Exchange)
            {
                Client.Exchange = false;
                Client.Action = 1;
                var crypto = new Network.Cryptography.GameCryptography(System.Text.Encoding.Default.GetBytes(Constants.GameCryptographyKey));
                byte[] otherData = new byte[length];
                Array.Copy(buffer, otherData, length);
                crypto.Decrypt(otherData, length);

                bool extra = false;
                int pos = 0;
                if (BitConverter.ToInt32(otherData, length - 140) == 128)//no extra packet
                {
                    pos = length - 140;
                    Client.Cryptography.Decrypt(buffer, length);
                }
                else if (BitConverter.ToInt32(otherData, length - 176) == 128)//extra packet
                {
                    pos = length - 176;
                    extra = true;
                    Client.Cryptography.Decrypt(buffer, length - 36);
                }
                int len = BitConverter.ToInt32(buffer, pos); pos += 4;
                if (len != 128)
                {
                    Client.Disconnect();
                    return;
                }
                byte[] pubKey = new byte[128];
                for (int x = 0; x < len; x++, pos++) pubKey[x] = buffer[pos];

                string PubKey = System.Text.Encoding.Default.GetString(pubKey);
                Client.Cryptography = Client.DHKeyExchange.HandleClientKeyPacket(PubKey, Client.Cryptography);

                if (extra)
                {
                    byte[] data = new byte[36];
                    Buffer.BlockCopy(buffer, length - 36, data, 0, 36);
                    processData(data, 36, Client);
                }
            }
            else
            {
                processData(buffer, length, Client);
            }
        }

        private static void processData(byte[] buffer, int length, Client.GameClient Client)
        {
            Client.Cryptography.Decrypt(buffer, length);
            Client.Queue.Enqueue(buffer, length);
            if (Client.Queue.CurrentLength > 1224)
            {
                Console.WriteLine("[Disconnect]Reason:The packet size is too big. " + Client.Queue.CurrentLength);
                Client.Disconnect();
                return;
            }
            while (Client.Queue.CanDequeue())
            {
                byte[] data = Client.Queue.Dequeue();
                Network.PacketHandler.HandlePacket(data, Client);
            }
        }


        static void GameServer_OnClientConnect(ClientWrapper obj)
        {
            Client.GameClient client = new Client.GameClient(obj);
            client.Send(client.DHKeyExchange.CreateServerKeyPacket());
            obj.Connector = client;
        }

        static void GameServer_OnClientDisconnect(ClientWrapper obj)
        {
            if (obj.Connector != null)
                (obj.Connector as Client.GameClient).Disconnect();
            else
                obj.Disconnect();
        }

        static void AuthServer_OnClientReceive(byte[] buffer, int length, ClientWrapper arg3)
        {
            var player = arg3.Connector as Client.AuthClient;

            player.Cryptographer.Decrypt(buffer, length);
            player.Queue.Enqueue(buffer, length);
            while (player.Queue.CanDequeue())
            {
                byte[] packet = player.Queue.Dequeue();

                ushort len = BitConverter.ToUInt16(packet, 0);
                ushort id = BitConverter.ToUInt16(packet, 2);

                if (len == 312)
                {
                    player.Info = new Authentication();
                    player.Info.Deserialize(packet);
                    player.Account = new AccountTable(player.Info.Username);
                    msvcrt.msvcrt.srand(player.PasswordSeed);

                    Forward Fw = new Forward();
                    if (player.Account.Password == player.Info.Password && player.Account.exists)
                        Fw.Type = Forward.ForwardType.Ready;
                    else Fw.Type = Forward.ForwardType.InvalidInfo;
                    if (IPBan.IsBanned(arg3.IP))
                    {
                        Fw.Type = Forward.ForwardType.Banned;
                        player.Send(Fw);
                        return;
                    }
                    if (Fw.Type == Network.AuthPackets.Forward.ForwardType.Ready)
                    {
                        Fw.Identifier = player.Account.GenerateKey();
                        Kernel.AwaitingPool[Fw.Identifier] = player.Account;
                        Fw.IP = GameIP;
                        Fw.Port = GamePort;
                    }
                    player.Send(Fw);
                }
            }
        }

        static void AuthServer_OnClientDisconnect(ClientWrapper obj)
        {
            obj.Disconnect();
        }

        static void AuthServer_OnClientConnect(ClientWrapper obj)
        {
            Client.AuthClient authState;
            obj.Connector = (authState = new Client.AuthClient(obj));
            authState.Cryptographer = new Network.Cryptography.AuthCryptography();
            Network.AuthPackets.PasswordCryptographySeed pcs = new PasswordCryptographySeed();
            pcs.Seed = Kernel.Random.Next();
            authState.PasswordSeed = pcs.Seed;
            authState.Send(pcs);
        }

        internal static Client.GameClient FindClient(string name)
        {
            return GamePool.FirstOrDefault(p => p.Entity.LoweredName == name);
        }
    }
}