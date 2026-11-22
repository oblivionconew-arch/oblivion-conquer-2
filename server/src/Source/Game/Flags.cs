using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Conquer_Online_Server.Database;
using Conquer_Online_Server.Network.GamePackets;

namespace Conquer_Online_Server.Game
{
    class Flags//mrbogy
    {
        public static string TopArcher = "";
        public static string TopClassPk2 = "";
        public static string TopWarrior = "";
        public static string TopNinja = "";
        public static string TopMonk = "";
        public static string TopTrojan = "";
        public static string TopWaterTaoist = "";
        public static string TopFireTaoist = "";
        public static string TopPirate = "";
        public static string Top2Archer = "";
        public static string Top2Fire = "";
        public static string Top2Monk = "";
        public static string Top2Warrior = "";
        public static string Top2Ninja = "";
        public static string TopTeamPK = "";
        public static string TopGuildLeader = "";
        public static string TopDeputyLeader = "";
        public static string TopDeputyLeader2 = "";
        public static string TopDeputyLeader3 = "";
        public static string TopDeputyLeader4 = "";
        public static string TopDeputyLeader5 = "";
        public static string MonthlyPKChampion = "";
        public static string WeeklyPKChampion = "";
        public static string TopSpouse = "";
        public static void LoadFlags()
        {

            MySqlCommand cmd = new MySqlCommand(MySqlCommandType.SELECT).Select("flags");
            Conquer_Online_Server.Database.MySqlReader r = new Conquer_Online_Server.Database.MySqlReader(cmd);
            if (r.Read())
            {
                TopSpouse = r.ReadString("TopSpouse");
                TopClassPk2 = r.ReadString("TopClassPk2");
                TopArcher = r.ReadString("TopArcher");
                Top2Archer = r.ReadString("Top2Archer");
                Top2Fire = r.ReadString("Top2Fire");
                Top2Monk = r.ReadString("Top2Monk");
                Top2Warrior = r.ReadString("Top2Warrior");
                Top2Ninja = r.ReadString("Top2Ninja");
                TopTeamPK = r.ReadString("TopTeamPK");
                TopWarrior = r.ReadString("TopWarrior");
                TopNinja = r.ReadString("TopNinja");
                TopWaterTaoist = r.ReadString("TopWaterTaoist");
                TopFireTaoist = r.ReadString("TopFireTaoist");
                TopTrojan = r.ReadString("TopTrojan");
                TopGuildLeader = r.ReadString("TopGuildLeader");
                TopDeputyLeader = r.ReadString("TopDeputyLeader");
                WeeklyPKChampion = r.ReadString("WeeklyPKChampion");
                MonthlyPKChampion = r.ReadString("MonthlyPKChampion");
                TopMonk = r.ReadString("TopMonk");
                TopDeputyLeader2 = r.ReadString("TopDeputyLeader2");
                TopDeputyLeader3 = r.ReadString("TopDeputyLeader3");
                TopDeputyLeader4 = r.ReadString("TopDeputyLeader4");
                TopDeputyLeader5 = r.ReadString("TopDeputyLeader5");
                TopPirate = r.ReadString("TopPirate");

            }
            r.Close();
            r.Dispose();
        }
        public static void AddTopTrojan(Client.GameClient client)
        {

            new MySqlCommand(MySqlCommandType.UPDATE).Update("flags").Set("TopTrojan", client.Entity.Name).Execute();
            TopTrojan = client.Entity.Name;
            return;
        }
        public static void AddTeamPK(Client.GameClient client)
        {
            new MySqlCommand(MySqlCommandType.UPDATE).Update("flags").Set("TopTeamPK", client.Entity.Name).Execute();
            TopTeamPK = client.Entity.Name;
        }
        public static void AddTopWarrior(Client.GameClient client)
        {
            new MySqlCommand(MySqlCommandType.UPDATE).Update("flags").Set("TopWarrior", client.Entity.Name).Execute();
            TopWarrior = client.Entity.Name;
            return;
        }
        public static void ClassPk2(Client.GameClient client)
        {
            new MySqlCommand(MySqlCommandType.UPDATE).Update("flags").Set("TopClassPk2", client.Entity.Name).Execute();
            TopClassPk2 = client.Entity.Name;
        }
        public static void AddTopArcher(Client.GameClient client)
        {
            new MySqlCommand(MySqlCommandType.UPDATE).Update("flags").Set("TopArcher", client.Entity.Name).Execute();
            TopArcher = client.Entity.Name;
            return;
        }
        public static void Add2Archer(Client.GameClient client)
        {
            new MySqlCommand(MySqlCommandType.UPDATE).Update("flags").Set("Top2Archer", client.Entity.Name).Execute();
            Top2Archer = client.Entity.Name;
        }
        public static void Add2Ninja(Client.GameClient client)
        {
            new MySqlCommand(MySqlCommandType.UPDATE).Update("flags").Set("Top2Ninja", client.Entity.Name).Execute();
            Top2Ninja = client.Entity.Name;
        }
        public static void Add2Fire(Client.GameClient client)
        {
            new MySqlCommand(MySqlCommandType.UPDATE).Update("flags").Set("Top2Fire", client.Entity.Name).Execute();
            Top2Fire = client.Entity.Name;
        }
        public static void Add2Warrior(Client.GameClient client)
        {
            new MySqlCommand(MySqlCommandType.UPDATE).Update("flags").Set("Top2Warrior", client.Entity.Name).Execute();
            Top2Warrior = client.Entity.Name;
        }
        public static void Add2Monk(Client.GameClient client)
        {
            new MySqlCommand(MySqlCommandType.UPDATE).Update("flags").Set("Top2Monk", client.Entity.Name).Execute();
            Top2Monk = client.Entity.Name;
        }
        public static void AddTopNinja(Client.GameClient client)
        {
            new MySqlCommand(MySqlCommandType.UPDATE).Update("flags").Set("TopNinja", client.Entity.Name).Execute();
            TopNinja = client.Entity.Name;

            return;
        }
        public static void AddTopMonk(Client.GameClient client)
        {
            new MySqlCommand(MySqlCommandType.UPDATE).Update("flags").Set("TopMonk", client.Entity.Name).Execute();
            TopMonk = client.Entity.Name;
            return;
        }
        public static void AddTopWater(Client.GameClient client)
        {
            new MySqlCommand(MySqlCommandType.UPDATE).Update("flags").Set("TopWaterTaoist", client.Entity.Name).Execute();
            TopWaterTaoist = client.Entity.Name;

            return;
        }
        public static void AddTopFire(Client.GameClient client)
        {
            new MySqlCommand(MySqlCommandType.UPDATE).Update("flags").Set("TopFireTaoist", client.Entity.Name).Execute();
            TopFireTaoist = client.Entity.Name;
            return;
        }
        public static void AddTopPirate(Client.GameClient client)
        {
            new MySqlCommand(MySqlCommandType.UPDATE).Update("flags").Set("TopPirate", client.Entity.Name).Execute();
            TopPirate = client.Entity.Name;
            return;
        }
        public static void AddGuildLeader(Client.GameClient client)
        {
            new MySqlCommand(MySqlCommandType.UPDATE).Update("flags").Set("TopGuildLeader", client.Entity.Name).Execute();
            TopGuildLeader = client.Entity.Name;
            return;
        }
        public static void AddGuildDeaputy(Client.GameClient client)
        {
            new MySqlCommand(MySqlCommandType.UPDATE).Update("flags").Set("TopDeputyLeader", client.Entity.Name).Execute();
            TopDeputyLeader = client.Entity.Name;
            return;
        }
        public static void AddWeekly(Client.GameClient client)
        {
            new MySqlCommand(MySqlCommandType.UPDATE).Update("flags").Set("WeeklyPKChampion", client.Entity.Name).Execute();
            WeeklyPKChampion = client.Entity.Name;
            return;
        }
        public static void AddMonthly(Client.GameClient client)
        {
            new MySqlCommand(MySqlCommandType.UPDATE).Update("flags").Set("MonthlyPKChampion", client.Entity.Name).Execute();
            MonthlyPKChampion = client.Entity.Name;
            return;
        }
        public static void AddSpouse(Client.GameClient client)
        {
            new MySqlCommand(MySqlCommandType.UPDATE).Update("flags").Set("TopSpouse", client.Entity.Name).Execute();
            TopSpouse = client.Entity.Name;
            return;
        }
        public static void AddGuildDeaputy2(Client.GameClient client)
        {
            new MySqlCommand(MySqlCommandType.UPDATE).Update("flags").Set("TopDeputyLeader2", client.Entity.Name).Execute();
            TopDeputyLeader2 = client.Entity.Name;
            return;
        }
        public static void AddGuildDeaputy3(Client.GameClient client)
        {
            new MySqlCommand(MySqlCommandType.UPDATE).Update("flags").Set("TopDeputyLeader3", client.Entity.Name).Execute();
            TopDeputyLeader3 = client.Entity.Name;
            return;
        }
        public static void AddGuildDeaputy4(Client.GameClient client)
        {
            new MySqlCommand(MySqlCommandType.UPDATE).Update("flags").Set("TopDeputyLeader4", client.Entity.Name).Execute();
            TopDeputyLeader4 = client.Entity.Name;
            return;
        }
        public static void AddGuildDeaputy5(Client.GameClient client)
        {
            new MySqlCommand(MySqlCommandType.UPDATE).Update("flags").Set("TopDeputyLeader5", client.Entity.Name).Execute();
            TopDeputyLeader5 = client.Entity.Name;
            return;
        }

    }
}