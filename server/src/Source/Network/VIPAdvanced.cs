using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Conquer_Online_Server.Network;

namespace ConquerOnline.Network.KongFu
{
    public class Main : Writer, Conquer_Online_Server.Interfaces.IPacket
    {
        public Main(bool Create)
        {
            if (Create)
            {
                Buffer = new byte[80 + 8];
                WriteUInt16(80, 0, Buffer);
                WriteUInt16(2700, 2, Buffer);
                //Writer.WriteUInt32((uint)Time32.Now.GetHashCode(), 4, Buffer);
            }
        }
        byte[] Buffer;
        public byte Type
        {
            get { return Buffer[4]; }
            set { Buffer[4] = value; }
        }
        public byte StatusType
        {
            get { return Buffer[5]; }
            set { Buffer[5] = value; }
        }
        public byte UnknownType
        {
            get { return Buffer[6]; }
            set { Buffer[6] = value; }
        }
        public string Text
        {
            get { return Encoding.ASCII.GetString(Buffer, 7, Buffer[6]); }
            set
            {
                int realloc = value.Length + 8 + 7;
                if (realloc != Buffer.Length)
                {
                    byte[] new_Packet = new byte[realloc];
                    System.Buffer.BlockCopy(Buffer, 0, new_Packet, 0, 7);
                    Buffer = new_Packet;
                }
                WriteStringWithLength(value, 7, Buffer);
            }
        }
        public void Deserialize(byte[] buffer)
        {
            this.Buffer = buffer;
        }
        public byte[] ToArray()
        {
            return Buffer;
        }
        public void Send(Conquer_Online_Server.Client.GameClient c)
        {
            c.Send(Buffer);
        }
    }
}
namespace Conquer_Online_Server.Network.GamePackets
{
    public class p1128 : Writer, Interfaces.IPacket
    {
        public const byte
           Purify1 = 0,
           Purify = 1,
                           Stabilaze = 2;
        public p1128(bool Create)
        {
            if (Create)
            {
                Buffer = new byte[40];
                WriteUInt16(32, 0, Buffer);
                WriteUInt16(1128, 2, Buffer);
            }
        }
        byte[] Buffer;
        public uint UID
        {
            get { return BitConverter.ToUInt32(Buffer, 4); }
            set { Writer.WriteUInt32(value, 4, Buffer); }
        }
        public uint UID2
        {
            get { return BitConverter.ToUInt32(Buffer, 8); }
            set { Writer.WriteUInt32(value, 8, Buffer); }
        }
        public void Deserialize(byte[] buffer)
        {
            this.Buffer = buffer;
        }
        public byte[] ToArray()
        {
            return Buffer;
        }
        public void Send(Client.GameClient client)
        {
            client.Send(Buffer);
        }
    }
}