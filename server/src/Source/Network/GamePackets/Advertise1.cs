/*using System.IO;

namespace Conquer_Online_Server.Network.GamePackets
{
    public class Advertise1 : Writer, Interfaces.IPacket
    {
        byte[] Buffer;
        public Advertise1(Client.GameClient client)
        {
            MemoryStream output = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(output);
            this.Buffer = new byte[0x2d4];
            WriteUInt16(716, 0, this.Buffer);
            WriteUInt16(2226, 2, this.Buffer);
            WriteUInt32(2, 8, this.Buffer);
            if (Game.GuildAdvertise.GuildsList.Count >= 4)
            {
                WriteUInt16(4, 12, this.Buffer);
            }
            else if (Game.GuildAdvertise.GuildsList.Count == 3)
            {
                WriteUInt16(2, 12, this.Buffer);
            }
            else
            {
                WriteUInt16(0, 12, this.Buffer);
            }
            WriteUInt16(0, 16, this.Buffer);
            int num = 24;
            for (int i = 0; i < Game.GuildAdvertise.GuildsList.Count; i++)
            {
                switch (i)
                {
                    case 2:
                    case 3:
                        WriteUInt32(Game.GuildAdvertise.GuildsList[i].GuildID, num, this.Buffer);
                        num += 4;
                        WriteString(Game.GuildAdvertise.GuildsList[i].Message, num, this.Buffer);
                        num += 255;
                        WriteString(Kernel.Guilds[Game.GuildAdvertise.GuildsList[i].GuildID].Name, num, this.Buffer);
                        num += 36;
                        WriteString(Kernel.Guilds[Game.GuildAdvertise.GuildsList[i].GuildID].LeaderName, num, this.Buffer);
                        num += 17;
                        WriteUInt16(Kernel.Guilds[Game.GuildAdvertise.GuildsList[i].GuildID].Level, num, this.Buffer);
                        num += 4;
                        WriteUInt64(Kernel.Guilds[Game.GuildAdvertise.GuildsList[i].GuildID].MemberCount, num, this.Buffer);
                        num += 4;
                        WriteUInt64(Kernel.Guilds[Game.GuildAdvertise.GuildsList[i].GuildID].SilverFund, num, this.Buffer);
                        num += 8;
                        WriteUInt64(1L, num, this.Buffer);
                        num += 2;
                        WriteUInt64(1L, num, this.Buffer);
                        num += 14;
                        break;
                }
                if (i > 3)
                {
                    break;
                }
            }
        }

        public void Send(Client.GameClient client)
        {
            client.Send(Buffer);
        }

        public void Deserialize(byte[] buffer)
        {
            Buffer = buffer;
        }

        public byte[] ToArray()
        {
            return Buffer;
        }
    }
}
*/