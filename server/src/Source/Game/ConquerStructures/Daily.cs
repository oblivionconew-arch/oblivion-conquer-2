using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Conquer_Online_Server.Network.GamePackets;
namespace Conquer_Online_Server.Game.ConquerStructures
{
    public class Daily
    {
        public static bool DailyPks = false;
        public static ushort Map = 8877;
        public static bool signup = false;
        public static int howmanyinmap = 0;
        public static int howmanyinmap2 = 0;
        public static int howmanyinmap3 = 0;
        public static int TopDlClaim = 0;
        public static int TopGlClaim = 0;
        public static void AddDl()
        {
            TopDlClaim++;
            //return;
        }
        public static void AddGl()
        {
            TopGlClaim++;
            //return;
        }
        public static void CheackAlive()
        {
            howmanyinmap = 0;
            foreach (Client.GameClient client in Program.GamePool)
            {
                if (client.Entity.MapID == 8877 && client.Entity.Hitpoints >= 1)
                {
                    howmanyinmap += 1;
                    Conquer_Online_Server.Kernel.SendWorldMessage(new Conquer_Online_Server.Network.GamePackets.Message("Jogadores vivos no Daily PK: " + howmanyinmap + " ", System.Drawing.Color.Black, Conquer_Online_Server.Network.GamePackets.Message.FirstRightCorner), Program.GamePool);
                }

            }
        }
        public static void CheackAlive2()
        {
            howmanyinmap2 = 0;
            foreach (Client.GameClient client in Program.GamePool)
            {
                if (client.Entity.MapID == 3333 && client.Entity.Hitpoints >= 1)
                {
                    howmanyinmap2 += 1;
                    Conquer_Online_Server.Kernel.SendWorldMessage(new Conquer_Online_Server.Network.GamePackets.Message("Jogadores vivos no Last Man Standing: " + howmanyinmap2 + " ", System.Drawing.Color.Black, Conquer_Online_Server.Network.GamePackets.Message.FirstRightCorner), Program.GamePool);
                }

            }
        }
        public static void CheackSpouse()
        {
            howmanyinmap3 = 0;
            foreach (Client.GameClient client in Program.GamePool)
            {
                if (client.Entity.MapID == 1090 && client.Entity.Hitpoints >= 1)
                {
                    if (client.Entity.Body == 1003 || client.Entity.Body == 1004)
                    {
                        howmanyinmap3 += 1;
                        Conquer_Online_Server.Kernel.SendWorldMessage(new Conquer_Online_Server.Network.GamePackets.Message("Duplas vivas no PK de Casais: " + howmanyinmap3 + " ", System.Drawing.Color.Black, Conquer_Online_Server.Network.GamePackets.Message.FirstRightCorner), Program.GamePool);

                    }
                }
            }
        }
        public static void SignUp()
        {
            foreach (Client.GameClient client in Program.GamePool)
                if (DateTime.Now.Minute == 00 && signup == false && client.Entity.Class >= 10 && client.Entity.Class <= 15)
                {
                    signup = true;
                    DailyPks = true;
                    client.Entity.Status = 0;
                    client.Entity.RemoveFlag(Conquer_Online_Server.Network.GamePackets.Update.Flags.TopTrojan);
                }
        }


        public static void End()
        {
            if (DateTime.Now.Minute == 30)
            {
                //signup = false;
                //DailyPks = false;
                foreach (Client.GameClient client in Program.GamePool)
                {
                    if (DateTime.Now.Minute == 30)
                    {
                        client.Entity.ConquerPoints += 150;
                        Conquer_Online_Server.Kernel.SendWorldMessage(new Conquer_Online_Server.Network.GamePackets.Message("[Oblivion]: O Daily PK terminou! Volte na próxima hora.", System.Drawing.Color.Red, Conquer_Online_Server.Network.GamePackets.Message.TopLeft), Program.GamePool);
                    }
                    if (client.Entity.MapID == 8877)
                    {
                        client.Entity.Teleport(1002, 400, 400);
                    }
                    client.Entity.RemoveFlag(Update.Flags.Flashy);
                }
            }
        }
    }
}
