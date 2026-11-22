namespace Conquer_Online_Server.Network.GamePackets
{
    using Conquer_Online_Server;
    using Conquer_Online_Server.Client;
    using Conquer_Online_Server.Interfaces;
    using Conquer_Online_Server.Network;
    using System;

    public class Spell : Writer, IPacket, ISkill
    {
        private bool _Available;
        private byte _PreviousLevel;
        private byte[] Buffer = new byte[0x18 + 4];

        public Spell(bool Create)
        {
            Writer.WriteUInt16(0x10 + 4, 0, this.Buffer);
            Writer.WriteUInt16(0x44f, 2, this.Buffer);
            this._Available = false;
        }

        public void Deserialize(byte[] buffer)
        {
            throw new NotImplementedException();
        }

        public void Send(Client.GameClient client)
        {
            client.Send(this);
        }

        public byte[] ToArray()
        {
            return this.Buffer;
        }

        public bool Available
        {
            get
            {
                return this._Available;
            }
            set
            {
                this._Available = value;
            }
        }

        public uint Experience
        {
            get
            {
                return Conquer_Online_Server.BitConverter.ToUInt32(this.Buffer, 4 + 4);
            }
            set
            {
                Writer.WriteUInt32(value, 4 + 4, this.Buffer);
            }
        }

        public ushort ID
        {
            get
            {
                return Conquer_Online_Server.BitConverter.ToUInt16(this.Buffer, 8 + 4);
            }
            set
            {
                Writer.WriteUInt16(value, 8 + 4, this.Buffer);
            }
        }

        public byte Level
        {
            get
            {
                return (byte)Conquer_Online_Server.BitConverter.ToUInt32(this.Buffer, 10 + 4);
            }
            set
            {
                Writer.WriteUInt16(value, 10 + 4, this.Buffer);
            }
        }

        public byte PreviousLevel
        {
            get
            {
                return this._PreviousLevel;
            }
            set
            {
                this._PreviousLevel = value;
            }
        }

        public byte TempLevel
        {
            get
            {
                return (byte)Conquer_Online_Server.BitConverter.ToUInt16(this.Buffer, 0x10 + 4);
            }
            set
            {
                Writer.WriteUInt16(value, 0x10 + 4, this.Buffer);
            }
        }
    }
}

