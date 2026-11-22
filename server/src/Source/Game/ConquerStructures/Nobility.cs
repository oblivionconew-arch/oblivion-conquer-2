using System;
using System.Linq;
using System.Collections.Generic;
using Conquer_Online_Server.Network.GamePackets;
using System.IO;


namespace Conquer_Online_Server.Game.ConquerStructures
{
    public class Nobility : Conquer_Online_Server.Network.Writer
    {
        public static SafeDictionary<uint, NobilityInformation> Board = new SafeDictionary<uint, NobilityInformation>(10000);
        public static List<NobilityInformation> BoardList = new List<NobilityInformation>(10000);
        public static void Handle(NobilityInfo information, Client.GameClient client)
        {
            switch (information.Type)
            {
                case NobilityInfo.Donate:
                    {
                        if (client.Trade.InTrade)
                            return;

                        ulong silvers = information.dwParam;
                        bool newDonator = false;
                        client.NobilityInformation.Gender = (byte)(client.Entity.Body % 10);
                        if (client.NobilityInformation.Donation == 0)
                            newDonator = true;
                        if (information.MoneyType == 1)
                        {
                            ulong cps = silvers / 50000;
                            if (client.Entity.ConquerPoints >= cps)
                            {
                                client.Entity.ConquerPoints -= (uint)cps;
                                client.NobilityInformation.Donation += silvers;
                            }
                        }
                        else
                        {
                            if (client.Entity.Money >= silvers)
                            {
                                client.Entity.Money -= (uint)silvers;
                                client.NobilityInformation.Donation += silvers;
                            }
                        }

                        if (!Board.ContainsKey(client.Entity.UID) && client.NobilityInformation.Donation == silvers && newDonator)
                        {
                            Board.Add(client.Entity.UID, client.NobilityInformation);
                            try
                            {
                                Database.NobilityTable.InsertNobilityInformation(client.NobilityInformation);
                            }
                            catch
                            {
                                Database.NobilityTable.UpdateNobilityInformation(client.NobilityInformation);
                            }
                        }
                        else
                        {
                            Database.NobilityTable.UpdateNobilityInformation(client.NobilityInformation);
                        }
                        Sort(client.Entity.UID);
                        break;
                    }
                case NobilityInfo.List:
                    {
                        byte[] packet = new byte[600 + 8];
                        WriteUInt16(600, 0, packet);
                        WriteUInt16(2064, 2, packet);
                        WriteUInt16(2, 4, packet);
                        WriteUInt16(5, 10, packet);
                        WriteUInt16(10, 12, packet);
                        int offset = 120;
                        for (int i = (int)(information.wParam1 * 10); i < information.wParam1 * 10 + 10 && i < BoardList.Count; i++)
                        {
                            var nob = BoardList[i];
                            WriteUInt32(nob.EntityUID, offset, packet);
                            offset += 4;
                            if (Kernel.GamePool.ContainsKey(nob.EntityUID))
                            {
                                WriteUInt32(1, offset, packet);
                                offset += 4;
                                WriteUInt32(nob.Mesh, offset, packet);
                                offset += 4;
                            }
                            else
                            {
                                offset += 8;
                            }
                            WriteString(nob.Name, offset, packet);
                            offset += 20;
                            WriteUInt64(nob.Donation, offset, packet);
                            offset += 8;
                            WriteUInt32((uint)nob.Rank, offset, packet);
                            offset += 4;
                            WriteUInt32((uint)i, offset, packet);
                            offset += 4;
                        }
                        client.Send(packet);

                        information.Type = NobilityInfo.NextRank;
                        ulong value = 0;
                        information.dwParam2 = 0;
                        lock (BoardList)
                        {
                            if (client.NobilityInformation.Rank == NobilityRank.Prince)
                                value = (ulong)(BoardList[02].Donation - client.NobilityInformation.Donation + 1);
                            if (client.NobilityInformation.Rank == NobilityRank.Duke)
                                value = (ulong)(BoardList[14].Donation - client.NobilityInformation.Donation + 1);
                            if (client.NobilityInformation.Rank == NobilityRank.Earl)
                                value = (ulong)(BoardList[49].Donation - client.NobilityInformation.Donation + 1);
                        }
                        Network.Writer.WriteUInt64(value, 8, information.ToArray());
                        information.dwParam3 = 60;
                        information.dwParam4 = uint.MaxValue;
                        client.Send(information);
                        break;
                    }
            }
        }
        public static void Donate(ulong silvers, Client.GameClient client)
        {
            bool newDonator = false;
            client.NobilityInformation.Gender = (byte)(client.Entity.Body % 10);
            if (client.NobilityInformation.Donation == 0)
                newDonator = true;

            client.NobilityInformation.Donation += silvers;
              
            if (!Board.ContainsKey(client.Entity.UID) && client.NobilityInformation.Donation == silvers && newDonator)
            {
                Board.Add(client.Entity.UID, client.NobilityInformation);
                try
                {
                    Database.NobilityTable.InsertNobilityInformation(client.NobilityInformation);
                }
                catch
                {
                    Database.NobilityTable.UpdateNobilityInformation(client.NobilityInformation);
                }
            }
            else
            {
                Database.NobilityTable.UpdateNobilityInformation(client.NobilityInformation);
            }
            Sort(client.Entity.UID);
        }
        public static void Sort(uint updateUID)
        {
            SafeDictionary<uint, NobilityInformation> sortedBoard = new SafeDictionary<uint, NobilityInformation>();

            int Place = 0;
            foreach (NobilityInformation entry in Board.Values.OrderByDescending((p) => p.Donation))
            {
                Client.GameClient client = null;
                try
                {
                    int previousPlace = entry.Position;
                    entry.Position = Place;
                    NobilityRank Rank = NobilityRank.Serf;

                    if (Place >= 50)
                    {
                        if (entry.Donation >= 200000000)
                        {
                            Rank = NobilityRank.Earl;
                        }
                        else if (entry.Donation >= 100000000)
                        {
                            Rank = NobilityRank.Baron;
                        }
                        else if (entry.Donation >= 30000000)
                        {
                            Rank = NobilityRank.Knight;
                        }
                    }
                    else
                    {
                        if (Place < 5)//server rank
                        {
                            Rank = NobilityRank.King; //serverrank
                        }
                        else if (Place < 20)
                        {
                            Rank = NobilityRank.Prince;
                        }
                        else
                        {
                            Rank = NobilityRank.Duke;
                        }
                    }
                    var oldRank = entry.Rank;
                    entry.Rank = Rank;
                    if (Kernel.GamePool.TryGetValue(entry.EntityUID, out client))
                    {
                        bool updateTheClient = false;
                        if (oldRank != Rank)
                        {
                            updateTheClient = true;
                            if (Rank == NobilityRank.Baron)
                            {
                                Kernel.SendWorldMessage(new Message("Parabens! " + client.Entity.Name + " Se tornou Barão .", System.Drawing.Color.White, Message.TopLeft), Program.GamePool);
                            }
                            if (Rank == NobilityRank.Earl)
                            {
                                Kernel.SendWorldMessage(new Message("Parabens! " + client.Entity.Name + " Se tornou Earl .", System.Drawing.Color.White, Message.TopLeft), Program.GamePool);
                            }
                            if (Rank == NobilityRank.Duke)
                            {
                                Kernel.SendWorldMessage(new Message("Parabens! " + client.Entity.Name + " Se tornou Duque.", System.Drawing.Color.White, Message.Center), Program.GamePool);
                            }
                            if (Rank == NobilityRank.Prince)
                            {
                                Kernel.SendWorldMessage(new Message("Parabens! " + client.Entity.Name + " Se tornou Principe.", System.Drawing.Color.White, Message.Center), Program.GamePool);
                            }
                            if (Rank == NobilityRank.King)
                            {
                                Kernel.SendWorldMessage(new Message("Parabens! " + client.Entity.Name + " Se tornou Rei/Rainha.", System.Drawing.Color.White, Message.Center), Program.GamePool);
                            }
                            if (Rank == NobilityRank.Knight)
                            {
                                Kernel.SendWorldMessage(new Message("Parabens! " + client.Entity.Name + " Se tornou Knight.", System.Drawing.Color.White, Message.TopLeft), Program.GamePool);
                            }
                        }
                        else
                        {
                            if (previousPlace != Place)
                            {
                                updateTheClient = true;
                            }
                        }
                        if (updateTheClient || client.Entity.UID == updateUID)
                        {
                            NobilityInfo update = new NobilityInfo(true);
                            update.Type = NobilityInfo.Icon;
                            update.dwParam = entry.EntityUID;
                            update.UpdateString(entry);
                            client.SendScreen(update, true);
                            client.Entity.NobilityRank = entry.Rank;
                        }
                    }
                    sortedBoard.Add(entry.EntityUID, entry);
                    Place++;
                }
                catch { }
            }
            Board = sortedBoard;
            lock (BoardList)
                BoardList = Board.Values.ToList();
        }
        public static uint BoundCps { get; set; }
    }
    public class NobilityInformation
    {
        public string Name;
        public uint EntityUID;
        public uint Mesh;
        public ulong Donation;
        public byte Gender;
        public int Position;
        public NobilityRank Rank;
    }

    public enum NobilityRank : byte
    {
        Serf = 0,
        Knight = 1,
        Baron = 3,
        Earl = 5,
        Duke = 7,
        Prince = 9,
        King = 12
    }
}
