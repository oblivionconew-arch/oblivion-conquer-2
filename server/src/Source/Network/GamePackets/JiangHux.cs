
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Conquer_Online_Server.Client;
using Conquer_Online_Server;
using Conquer_Online_Server.Network.Test;

namespace Conquer_Online_Server.GamePackets
{
    public class JiangHux : Network.Writer
    {
        public const byte IconBar = 0, UpdateTalent = 5, UpdateStar = 11, OpenStage = 12, UpdateTime = 13, SetName = 14;

        //  private ServerSocket.Packet packet = null;
        private Byte[] packet;
        private uint Leng(string[] dat)
        {
            uint len = 0;
            foreach (string line in dat)
                len += (byte)line.Length;
            return (uint)(len + dat.Length);
        }
        public JiangHux()
        {
            Texts = new List<string>();

        }
        public List<string> Texts;
        public byte Mode = 0;
        public void CreateArray()
        {
            packet = new byte[(ushort)(Leng(Texts.ToArray()) + 6 + 8)];
            WriteUInt16((ushort)(packet.Length - 8), 0, packet);
            WriteUInt16(2700, 2, packet);
            WriteByte(Mode, 4, packet);
            WriteByte((byte)(Texts.Count), 5, packet);
            ushort position = 6;
            for (ushort x = 0; x < Texts.Count; x++)
            {
                string text = Texts[x];
                WriteByte((byte)text.Length, position, packet);
                WriteString(text, (ushort)(position + 1), packet);
                position += (ushort)(text.Length + 1);
            }
        }
        public void Clear()
        {
            // packet.Dispose();
            Texts.Clear();
        }
        public void Send(GameClient client)
        {
            if (packet != null)
            {
                client.Send(packet.ToArray());
            }
        }
        public byte[] ToArray()
        {
            return packet;
        }
    }
    public class JiangHu : Network.Writer, Interfaces.IPacket
    {
        public const byte IconBar = 0, UpdateTalent = 5, UpdateStar = 11, OpenStage = 12, UpdateTime = 13, SetName = 14;

        private byte[] Buffer;
        public JiangHu()
        {
            Positions = new List<string>();

        }
        public List<string> Positions;
        public byte Mode = 0;
        public static byte Stage = 9;
        public static byte Star = 1;
        public static byte FreeCourse = 10;
        public static byte Time = 1;
        public static byte Talent = 5;
        public byte myStage = 5;
        public byte myStar = 1;
        public byte myFreeCourse = 10;
        public byte myTime = 0;
        public byte myTalent = 5;
        public byte MyOldStar = 1;
        public string OwnName = "";
        public string CustomizedName = "";
        public bool OnJiangMode = true;
        public byte myFreeTimeTodey = 1;

        public void Set()
        {
            byte[] Saved = Buffer;
            int len = 0;
            foreach (var Pos in Positions)
                len += Pos.Length;

            this.Buffer = new byte[6 + 8 + len + (Positions.Count)];
            //if (Saved != null) Saved.CopyTo(Buffer, 0);
            WriteUInt16((ushort)(Buffer.Length - 8), 0, Buffer);
            WriteUInt16(2700, 2, Buffer);
            WriteByte(Mode, 4, Buffer);
            Buffer[5] = (byte)(Positions.Count);
            byte start = 6;
            foreach (var Pos in Positions)
            {
                Buffer[start] = (byte)Pos.Length;
                WriteString(Pos, (start + 1), Buffer);
                start += (byte)(Pos.Length + 1);
            }

        }
        public void Deserialize(byte[] Data)
        {
            Buffer = Data;
        }
        public byte[] ToArray()
        {
            return Buffer;
        }
        public void Send(Client.GameClient client)
        {
            client.Send(Buffer);
        }
        public void SendInfo(Client.GameClient client, byte mode = 1, params string[] data)
        {
            JiangHu jinag = new JiangHu();
            jinag.Mode = mode;
            jinag.Positions = new List<string>(data);
            jinag.Set();
            jinag.Send(client);
        }

        public void SendStatus(Client.GameClient client, Client.GameClient Attacked)
        {
            //var dictionary = Stagers.Where(p => p.Activate);           
            Network.Test.JiangHuStatus stat = new Network.Test.JiangHuStatus(1);

            stat.Name = client.Entity.Name;
            stat.FreeTimeTodey = 10;//FreeTimeTodey;
            stat.Talent = 1;
            stat.Stage = 1;
            stat.RoundBuyPoints = 1;
            stat.FreeTimeTodeyUsed = (byte)1;
            stat.StudyPoints = Attacked.Entity.SubClasses.StudyPoints;
            stat.Positionx = 1;
            if (client.Entity.UID != Attacked.Entity.UID)
                stat.Timer = 13751297;
            else
                stat.Timer = 15500800;

            client.Send(stat.ToArray());
        }


        public uint UID { get; set; }
    }
    public class JiangHu2
    {
        public const ushort SetName = 1, RestoreStar = 10, UpdateStar = 11, Open = 13, OpenView = 9;
        private static bool InvalidCharacters(string Name)
        {
            foreach (char ch in Name)
            {
                if (Kernel.InvalidCharacters.Contains(ch) || (((byte)ch) < 0x30))
                {
                    return true;
                }
            }
            return false;
        }
        public static void Execute(GameClient client, byte[] packet)
        {
            ushort Mode = packet[4];
            switch (Mode)
            {
                case RestoreStar:
                    {
                        if (client.Entity.MyJiang != null/* && client.Entity.MyJiang.MyOldStar != null*/ && client.Entity.ConquerPoints > 20)
                        {
                            client.Entity.ConquerPoints -= 20;
                            //  client.Entity.MyJiang.GetRoll(client, client.Entity.MyJiang.MyOldStar.PositionStar, client.Entity.MyJiang.MyOldStar.Stage, true);
                        }
                        break;
                    }
                case UpdateStar:
                    {
                        //asta e pentru replace star
                        //nu o folosesc ca ii dau update atutomat de prima data
                        break;
                    }
                case OpenView:
                    {
                        string Name = System.Text.ASCIIEncoding.ASCII.GetString(packet, 7, packet[6]);
                        uint UID = 0;
                        if (uint.TryParse(Name, out UID))
                        {
                            GameClient pClient;
                            if (Kernel.GamePool.TryGetValue(UID, out pClient))
                            {
                                if (pClient.Entity.MyJiang != null)
                                    pClient.Entity.MyJiang.SendStatus(client, pClient);
                                else
                                {
                                    JiangHuStatus stat = new JiangHuStatus(0);
                                    stat.Name = Name;
                                    client.Send(stat.ToArray());

                                }
                            }
                        }
                        break;
                    }
                case Open:
                    {
                        if (client.Entity.MyJiang != null)
                        {
                            client.Entity.MyJiang.SendInfo(client, JiangHu.UpdateTime, client.Entity.MyJiang.myFreeCourse.ToString(), client.Entity.MyJiang.myTime.ToString());
                        }
                        break;
                    }
                case SetName:
                    {
                        if (client.Entity.MyJiang == null)
                        {
                            byte NameLeng = packet[6];
                            if (NameLeng > 16)
                            {
                                client.Send(new Network.GamePackets.Message("You used a name high!", System.Drawing.Color.CadetBlue, Network.GamePackets.Message.Talk));

                                return;
                            }

                            string Name = System.Text.ASCIIEncoding.ASCII.GetString(packet, 7, NameLeng);
                            if (Name == "")
                            {
                                return;
                            }
                            if (InvalidCharacters(Name))
                            {
                                client.Send(new Network.GamePackets.Message("Invalid characters inside the name.", System.Drawing.Color.CadetBlue, Network.GamePackets.Message.Talk));
                                return;
                            }
                            else
                            {
                                client.Entity.MyJiang = new JiangHu();
                                client.Entity.MyJiang.OwnName = client.Entity.Name;
                                client.Entity.MyJiang.CustomizedName = Name;
                                client.Entity.MyJiang.OnJiangMode = true;
                                client.Entity.MyJiang.SendInfo(client, JiangHu.SetName, client.Entity.UID.ToString()
                                    , client.Entity.MyJiang.myStage.ToString()
                                    , client.Entity.MyJiang.myStar.ToString());
                                JiangHuStatus jiang = new JiangHuStatus();
                                jiang.Name = Name;
                                jiang.Talent = client.Entity.MyJiang.myStar;
                                jiang.Stage = client.Entity.MyJiang.myStage;
                                jiang.StudyPoints = client.Entity.SubClasses.StudyPoints;
                                jiang.FreeTimeTodey = client.Entity.MyJiang.myFreeTimeTodey;
                                client.Send(jiang.ToArray());



                                //  client.Entity.MyJiang.CreateTime();
                                //  client.Entity.MyJiang.UpdateStundyPoints(client, 100);
                                //  client.Entity.MyJiang.Stagers[0].Activate = true;
                                //  client.Entity.MyJiang.Level = client.Entity.Level;

                                client.Entity.MyJiang.SendInfo(client, JiangHu.UpdateTime, client.Entity.MyJiang.myFreeCourse.ToString(), client.Entity.MyJiang.myTime.ToString());

                                client.Entity.MyJiang.SendInfo(client, JiangHu.UpdateTalent, client.Entity.UID.ToString(), client.Entity.MyJiang.myTalent.ToString());

                                client.Entity.MyJiang.SendInfo(client, 7, client.Entity.UID.ToString(), client.Entity.MyJiang.myStage.ToString(), client.Entity.MyJiang.myStar.ToString());

                            }

                        }
                        break;
                    }
            }
        }
    }


}
