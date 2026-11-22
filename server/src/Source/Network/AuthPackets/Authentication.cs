using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Conquer_Online_Server.Network.Cryptography;

namespace Conquer_Online_Server.Network.AuthPackets
{
    public class Authentication : Interfaces.IPacket
    {
        public string Username;
        public string Password;
        public string Server;
        public byte[] RawPacket;

        public Authentication()
        {
        }

        private static string CleanNullString(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return "";
            int len = 0;
            while (len < bytes.Length && bytes[len] != 0) len++;
            return Encoding.Default.GetString(bytes, 0, len).Trim();
        }

        private static string TryDecryptAtOffset(byte[] buffer, int offset, string username, int passwordSeed, bool useScanCodeCipher)
        {
            try
            {
                if (buffer == null || offset < 0 || offset + 16 > buffer.Length) return null;
                byte[] encpw = new byte[16];
                Buffer.BlockCopy(buffer, offset, encpw, 0, 16);

                msvcrt.msvcrt.srand(passwordSeed);
                byte[] rc5Key = new byte[16];
                for (int i = 0; i < 16; i++)
                    rc5Key[i] = (byte)msvcrt.msvcrt.rand();

                byte[] dec = new RC5(rc5Key).Decrypt(encpw);
                if (useScanCodeCipher && !string.IsNullOrEmpty(username))
                {
                    dec = new ConquerPasswordCryptpographer(username).Decrypt(dec);
                }
                return CleanNullString(dec);
            }
            catch
            {
                return null;
            }
        }

        public List<string> GetPasswordCandidates(int passwordSeed)
        {
            var candidates = new List<string>();
            if (!string.IsNullOrEmpty(Password))
                candidates.Add(Password);

            if (RawPacket != null && RawPacket.Length >= 148)
            {
                // Conquer v5517-6022 (packet 1542 / 312 bytes):
                // Tenta descriptografia RC5 + ScanCodeCipher e RC5 puro nos offsets 72 e 132
                int[] offsets = new int[] { 72, 132 };
                foreach (int off in offsets)
                {
                    string c1 = TryDecryptAtOffset(RawPacket, off, Username, passwordSeed, true);
                    if (!string.IsNullOrEmpty(c1) && !candidates.Contains(c1)) candidates.Add(c1);

                    string c2 = TryDecryptAtOffset(RawPacket, off, Username, passwordSeed, false);
                    if (!string.IsNullOrEmpty(c2) && !candidates.Contains(c2)) candidates.Add(c2);
                }
            }
            return candidates;
        }

        public void Deserialize(byte[] buffer)
        {
            RawPacket = buffer;
            if (buffer.Length == 312)
            {
                ushort length = BitConverter.ToUInt16(buffer, 0);

                if (length == 312)
                {
                    ushort type = BitConverter.ToUInt16(buffer, 2);
                    if (type == 1542 || type == 1086 || type == 1051)
                    {
                        MemoryStream MS = new MemoryStream(buffer);
                        BinaryReader BR = new BinaryReader(MS);

                        BR.ReadUInt16();
                        BR.ReadUInt16();
                        Username = CleanNullString(BR.ReadBytes(32));
                        BR.ReadBytes(36);
                        Password = CleanNullString(BR.ReadBytes(32));
                        BR.ReadBytes(32);
                        Server = CleanNullString(BR.ReadBytes(32));
                        BR.Close();
                        MS.Close();
                    }
                }
            }
        }

        public byte[] ToArray()
        {
            throw new NotImplementedException();
        }
        public void Send(Client.GameClient client)
        {
            throw new NotImplementedException();
        }
    }
}
