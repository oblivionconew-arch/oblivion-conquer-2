/*using System.IO;

namespace Conquer_Online_Server.Network.GamePackets
{
    public class Advertise : Writer, Interfaces.IPacket
    {
        byte[] Buffer;

        public Advertise(Client.GameClient client)
        {
            MemoryStream output = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(output);
            this.Buffer = new byte[724];
            WriteUInt16(716, 0, this.Buffer);
            WriteUInt16(2226, 2, this.Buffer);
            WriteUInt32(2, 8, this.Buffer);
            if (Game.GuildAdvertise.GuildsList.Count >= 2)
            {
                WriteUInt16(4, 12, this.Buffer);
            }
            else if (Game.GuildAdvertise.GuildsList.Count == 1)
            {
                WriteUInt16(2, 12, this.Buffer);
            }
            else
            {
                WriteUInt16(0, 12, this.Buffer);
            }
            WriteUInt16(1, 16, this.Buffer);
            int num = 24;
            for (int i = 0; i < Game.GuildAdvertise.GuildsList.Count; i++)
            {
                if (i >= 2)
                {
                    break;
                }
                WriteUInt32(Game.GuildAdvertise.GuildsList[i].GuildID, num, this.Buffer);
                num += 4;
                WriteString(Game.GuildAdvertise.GuildsList[i].Message, num, this.Buffer);
                num += 255;
                WriteString(Kernel.Guilds[Game.GuildAdvertise.GuildsList[i].GuildID].Name, num, this.Buffer);
                num += 36;
                WriteString(Kernel.Guilds[Game.GuildAdvertise.GuildsList[i].GuildID].LeaderName, num, this.Buffer);
                num += 17;
                WriteUInt32(Kernel.Guilds[Game.GuildAdvertise.GuildsList[i].GuildID].Level, num, this.Buffer);
                num += 4;
                WriteUInt64(Kernel.Guilds[Game.GuildAdvertise.GuildsList[i].GuildID].MemberCount, num, this.Buffer);
                num += 4;
                WriteUInt64(Kernel.Guilds[Game.GuildAdvertise.GuildsList[i].GuildID].SilverFund, num, this.Buffer);
                num += 8;
                WriteByte(Game.GuildAdvertise.GuildsList[i].level, num, this.Buffer);
                num += 2;
                WriteByte(Game.GuildAdvertise.GuildsList[i].reborn, num, this.Buffer);
                num += 14;
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