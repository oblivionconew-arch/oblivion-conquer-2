using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Conquer_Online_Server.Network.Test
{
    public class JiangHuStatus : Conquer_Online_Server.Network.Writer
    {
        /*   public void SendStatus(Client.GameClient client, Client.GameClient Attacked)
           {
               var dictionary = Stagers.Where(p => p.Activate);
               using (JiangHuStatus stat = new JiangHuStatus((byte)dictionary.Count()))
               {
                   stat.Name = CustomizedName;
                   stat.FreeTimeTodey = 10;//FreeTimeTodey;
                   stat.Talent = Talent;
                   stat.Stage = Stage;
                   stat.RoundBuyPoints = RoundBuyPoints;
                   stat.FreeTimeTodeyUsed = (byte)FreeTimeTodeyUsed;
                   stat.StudyPoints = Attacked.Player.SubClass.StudyPoints;
                   if (client.Player.UID != Attacked.Player.UID)
                       stat.Timer = 13751297;
                   else
                       stat.Timer = 15500800;
                   stat.Apprend(dictionary.ToArray());
                   client.Enqueue(stat.ToArray());
               }
           }*/
        private Byte[] _buffer;
        public JiangHuStatus(uint count = 1)
        {
            _buffer = new byte[(ushort)56 + count * 18];
            WriteUInt16((ushort)(48 + count * 18), 0, _buffer);
            WriteUInt16(2701, 2, _buffer);//2701
            WriteUInt32(9999999, 39, _buffer);
        }
        public string Name
        {
            get
            {
                return Encoding.ASCII.GetString(_buffer, 4, _buffer[16]);
            }
            set
            {
                WriteString(value, 4, _buffer);
            }
        }
        public byte Stage { get { return _buffer[20]; } set { WriteByte(value, 20, _buffer); } }
        public byte Talent { get { return _buffer[21]; } set { WriteByte(value, 21, _buffer); } }
        public uint Timer { get { return BitConverter.ToUInt32(_buffer, 22); } set { WriteUInt32(value, 22, _buffer); } }
        public ulong StudyPoints { get { return BitConverter.ToUInt64(_buffer, 27); } set { WriteUInt64(value, 27, _buffer); } }
        public uint FreeTimeTodey { get { return BitConverter.ToUInt32(_buffer, 35); } set { WriteUInt32(value, 35, _buffer); } }

        public byte FreeTimeTodeyUsed { get { return _buffer[43]; } set { WriteByte(value, 43, _buffer); } }

        public uint RoundBuyPoints { get { return BitConverter.ToUInt32(_buffer, 44); } set { WriteUInt32(value, 44, _buffer); } }
        public ushort Positionx { get { return BitConverter.ToUInt16(_buffer, 48); } set { WriteUInt16(value, 48, _buffer); } }
        // private ushort Position = 48;
        public byte[] ToArray()
        {
            return _buffer;
        }
        /* public void Apprend(uint val)
         {
             for (byte x = 0; x < obj.Stars.Length; x++)
             {
                 if (Length < Position + 2)
                     break;
                 var star = obj.Stars[x];
                 if (!star.Activate)
                     break;
                 WriteUInt16(star.UID, Position);
                 Position += 2;
             }
            
             FreeTimeTodeyUsed = 0;

         }*/
    }
}
