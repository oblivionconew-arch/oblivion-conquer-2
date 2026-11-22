using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ConquerServerDatGen
{
    public class ServerEntry
    {
        public int Id = 101;
        public string GroupName = "Conquer 2.0";
        public string ServerName = "Oblivion";
        public string ServerIP = "127.0.0.1";
        public int ServerPort = 9958;
        public string FlashName = "Oblivion";
        public string FlashIcon = "Group3/server8.swf";
        public string FlashHint = "";
    }

    public static class LegacyDatCipher
    {
        // Exact 128-byte TQ DatCryptor table (seed 0x2537) verified against DatCryptor.exe
        private static readonly byte[] Key = new byte[128]
        {
            0xAD, 0x6B, 0x4F, 0xFB, 0xDD, 0xB8, 0x0E, 0x09, 0x13, 0x33, 0x8F, 0xF5, 0x43, 0x09, 0x15, 0x88,
            0x5D, 0x80, 0xA3, 0x45, 0x2D, 0x42, 0x08, 0x56, 0x80, 0xF8, 0x19, 0xC5, 0x88, 0x1B, 0x3E, 0xEF,
            0x81, 0x07, 0x30, 0x36, 0x95, 0x52, 0x00, 0xF7, 0xFD, 0x5B, 0x5C, 0xBC, 0x6A, 0x26, 0x0E, 0xB2,
            0xA3, 0x67, 0xC5, 0x5D, 0x6F, 0xDC, 0x18, 0x8A, 0xB5, 0xE0, 0xC8, 0x85, 0xE2, 0x3E, 0x45, 0x8D,
            0x8B, 0x43, 0x74, 0x85, 0x54, 0x17, 0xB0, 0xEC, 0x10, 0x4D, 0x0F, 0x0F, 0x29, 0xB8, 0xE6, 0x7D,
            0x42, 0x80, 0x8F, 0xBC, 0x1C, 0x76, 0x69, 0x3A, 0xB6, 0xA5, 0x21, 0x86, 0xB9, 0x29, 0x30, 0xC0,
            0x12, 0x45, 0xA5, 0x4F, 0xE1, 0xAF, 0x25, 0xD1, 0x92, 0x2E, 0x30, 0x58, 0x49, 0x67, 0xA5, 0xD3,
            0x84, 0xF4, 0x89, 0xCA, 0xFC, 0xB7, 0x04, 0x4F, 0xCC, 0x6E, 0xAC, 0x31, 0xD4, 0x87, 0x07, 0x72
        };

        public static byte[] Encrypt(byte[] plain)
        {
            byte[] buf = new byte[plain.Length];
            for (int i = 0; i < plain.Length; i++)
            {
                int r = i % 8;
                byte b = plain[i];
                byte rot = (byte)(((b >> (8 - r)) | (b << r)) & 0xFF);
                buf[i] = (byte)(rot ^ Key[(byte)i % 128]);
            }
            return buf;
        }

        public static byte[] Decrypt(byte[] cipher)
        {
            byte[] buf = new byte[cipher.Length];
            for (int i = 0; i < cipher.Length; i++)
            {
                byte b = (byte)(cipher[i] ^ Key[(byte)i % 128]);
                int r = i % 8;
                buf[i] = (byte)(((b << (8 - r)) | (b >> r)) & 0xFF);
            }
            return buf;
        }
    }

    public static class RsaBlockCipher
    {
        // RSA-1024 block mode (128-byte output blocks matching Conquer 2.0 5517-6022+ server.dat layout)
        public static byte[] EncryptBlocks(byte[] plain, RSACryptoServiceProvider rsa)
        {
            const int maxChunk = 117; // PKCS#1 v1.5 max payload per 128-byte RSA-1024 block
            using (MemoryStream ms = new MemoryStream())
            {
                for (int offset = 0; offset < plain.Length; offset += maxChunk)
                {
                    int len = Math.Min(maxChunk, plain.Length - offset);
                    byte[] chunk = new byte[len];
                    Buffer.BlockCopy(plain, offset, chunk, 0, len);
                    byte[] enc = rsa.Encrypt(chunk, false);
                    ms.Write(enc, 0, enc.Length);
                }
                return ms.ToArray();
            }
        }

        public static byte[] DecryptBlocks(byte[] cipher, RSACryptoServiceProvider rsa)
        {
            if (cipher.Length % 128 != 0)
                throw new InvalidDataException("RSA-1024 server.dat size must be a multiple of 128 bytes.");
            using (MemoryStream ms = new MemoryStream())
            {
                for (int offset = 0; offset < cipher.Length; offset += 128)
                {
                    byte[] block = new byte[128];
                    Buffer.BlockCopy(cipher, offset, block, 0, 128);
                    byte[] dec = rsa.Decrypt(block, false);
                    ms.Write(dec, 0, dec.Length);
                }
                return ms.ToArray();
            }
        }
    }

    class Program
    {
        static string BuildFlashXml(ServerEntry entry)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine("<ServerList>");
            sb.AppendLine("  <Group name=\"" + EscapeXml(entry.GroupName) + "\" id=\"1\">");
            sb.AppendLine("    <Server>");
            sb.AppendLine("      <field name=\"id\">" + entry.Id + "</field>");
            sb.AppendLine("      <field name=\"ServerName\">" + EscapeXml(entry.ServerName) + "</field>");
            sb.AppendLine("      <field name=\"ServerIP\">" + EscapeXml(entry.ServerIP) + "</field>");
            sb.AppendLine("      <field name=\"ServerPort\">" + entry.ServerPort + "</field>");
            sb.AppendLine("      <field name=\"FlashName\">" + EscapeXml(entry.FlashName) + "</field>");
            sb.AppendLine("      <field name=\"FlashIcon\">" + EscapeXml(entry.FlashIcon) + "</field>");
            sb.AppendLine("      <field name=\"FlashHint\">" + EscapeXml(entry.FlashHint) + "</field>");
            sb.AppendLine("      <field name=\"Child\">0</field>");
            sb.AppendLine("    </Server>");
            sb.AppendLine("  </Group>");
            sb.AppendLine("</ServerList>");
            return sb.ToString();
        }

        static string BuildLegacyIni(ServerEntry entry)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Header]");
            sb.AppendLine("GroupAmount=1");
            sb.AppendLine("Group1=Group1");
            sb.AppendLine();
            sb.AppendLine("[Group1]");
            sb.AppendLine("ServerAmount=1");
            sb.AppendLine("Server1=" + entry.ServerName);
            sb.AppendLine("Ip1=" + entry.ServerIP);
            sb.AppendLine("Port1=" + entry.ServerPort);
            sb.AppendLine("ServerName1=" + entry.ServerName);
            sb.AppendLine("HintWord1=" + entry.FlashHint);
            sb.AppendLine("Pic1=Group3/server8.ani");
            sb.AppendLine("FlashName1=" + entry.FlashName);
            sb.AppendLine("FlashIcon1=" + entry.FlashIcon);
            return sb.ToString();
        }

        static string EscapeXml(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        static void PrintUsage()
        {
            Console.WriteLine("Conquer Online 2.0 Server.dat Generator & Cryptor");
            Console.WriteLine("Uso:");
            Console.WriteLine("  ServerDatGen.exe --generate --mode <xml|legacy-dat|rsa1024> --name <Nome> --ip <IP/Dominio> --port <9958> --out <server.dat> [--group <Grupo>] [--keyfile <rsa_key.xml>]");
            Console.WriteLine("  ServerDatGen.exe --encrypt-legacy --in <input.txt> --out <output.dat>");
            Console.WriteLine("  ServerDatGen.exe --decrypt-legacy --in <input.dat> --out <output.txt>");
            Console.WriteLine("  ServerDatGen.exe --self-test");
        }

        static int RunSelfTest()
        {
            var sample = new ServerEntry { ServerName = "Oblivion", ServerIP = "203.0.113.10", ServerPort = 9958, FlashName = "Oblivion" };

            // 1. Legacy DatCryptor round-trip
            byte[] iniBytes = Encoding.ASCII.GetBytes(BuildLegacyIni(sample));
            byte[] encLegacy = LegacyDatCipher.Encrypt(iniBytes);
            byte[] decLegacy = LegacyDatCipher.Decrypt(encLegacy);
            if (Encoding.ASCII.GetString(decLegacy) != Encoding.ASCII.GetString(iniBytes))
            {
                Console.Error.WriteLine("[FAIL] Legacy DatCryptor round-trip mismatch!");
                return 1;
            }
            Console.WriteLine("[OK] Legacy DatCryptor (0x2537) round-trip verified (" + encLegacy.Length + " bytes).");

            // 2. RSA-1024 block round-trip
            byte[] xmlBytes = Encoding.UTF8.GetBytes(BuildFlashXml(sample));
            using (var rsa = new RSACryptoServiceProvider(1024))
            {
                byte[] encRsa = RsaBlockCipher.EncryptBlocks(xmlBytes, rsa);
                if (encRsa.Length % 128 != 0)
                {
                    Console.Error.WriteLine("[FAIL] RSA-1024 output not aligned to 128-byte blocks!");
                    return 2;
                }
                byte[] decRsa = RsaBlockCipher.DecryptBlocks(encRsa, rsa);
                if (Encoding.UTF8.GetString(decRsa) != Encoding.UTF8.GetString(xmlBytes))
                {
                    Console.Error.WriteLine("[FAIL] RSA-1024 block round-trip mismatch!");
                    return 3;
                }
                Console.WriteLine("[OK] RSA-1024 128-byte block round-trip verified (" + encRsa.Length + " bytes = " + (encRsa.Length / 128) + " blocks).");
            }
            return 0;
        }

        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return 1;
            }

            var flags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("--"))
                {
                    string key = args[i];
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                    {
                        flags[key] = args[++i];
                    }
                    else
                    {
                        flags[key] = "true";
                    }
                }
            }

            if (flags.ContainsKey("--self-test"))
            {
                return RunSelfTest();
            }

            if (flags.ContainsKey("--encrypt-legacy"))
            {
                byte[] data = File.ReadAllBytes(flags["--in"]);
                File.WriteAllBytes(flags["--out"], LegacyDatCipher.Encrypt(data));
                Console.WriteLine("Encrypted (legacy 0x2537) -> " + flags["--out"]);
                return 0;
            }

            if (flags.ContainsKey("--decrypt-legacy"))
            {
                byte[] data = File.ReadAllBytes(flags["--in"]);
                File.WriteAllBytes(flags["--out"], LegacyDatCipher.Decrypt(data));
                Console.WriteLine("Decrypted (legacy 0x2537) -> " + flags["--out"]);
                return 0;
            }

            if (flags.ContainsKey("--generate"))
            {
                string mode = flags.ContainsKey("--mode") ? flags["--mode"].ToLowerInvariant() : "legacy-dat";
                string outPath = flags.ContainsKey("--out") ? flags["--out"] : "server.dat";

                var entry = new ServerEntry();
                if (flags.ContainsKey("--name")) entry.ServerName = entry.FlashName = flags["--name"];
                if (flags.ContainsKey("--flash-name")) entry.FlashName = flags["--flash-name"];
                if (flags.ContainsKey("--group")) entry.GroupName = flags["--group"];
                if (flags.ContainsKey("--ip")) entry.ServerIP = flags["--ip"];
                if (flags.ContainsKey("--port")) entry.ServerPort = int.Parse(flags["--port"]);
                if (flags.ContainsKey("--icon")) entry.FlashIcon = flags["--icon"];

                if (mode == "xml")
                {
                    string xml = BuildFlashXml(entry);
                    File.WriteAllText(outPath, xml, new UTF8Encoding(false));
                    Console.WriteLine("Generated Flash XML server.dat -> " + outPath + " (" + xml.Length + " bytes)");
                    return 0;
                }
                else if (mode == "legacy-dat")
                {
                    string ini = BuildLegacyIni(entry);
                    byte[] enc = LegacyDatCipher.Encrypt(Encoding.ASCII.GetBytes(ini));
                    File.WriteAllBytes(outPath, enc);
                    Console.WriteLine("Generated Legacy Encrypted server.dat -> " + outPath + " (" + enc.Length + " bytes)");
                    return 0;
                }
                else if (mode == "rsa1024")
                {
                    string xml = BuildFlashXml(entry);
                    using (var rsa = new RSACryptoServiceProvider(1024))
                    {
                        string keyFile = flags.ContainsKey("--keyfile") ? flags["--keyfile"] : (outPath + ".rsa_key.xml");
                        if (File.Exists(keyFile))
                        {
                            rsa.FromXmlString(File.ReadAllText(keyFile));
                        }
                        else
                        {
                            File.WriteAllText(keyFile, rsa.ToXmlString(true));
                            Console.WriteLine("Generated new RSA-1024 keypair -> " + keyFile);
                        }
                        byte[] enc = RsaBlockCipher.EncryptBlocks(Encoding.UTF8.GetBytes(xml), rsa);
                        File.WriteAllBytes(outPath, enc);
                        Console.WriteLine("Generated RSA-1024 server.dat -> " + outPath + " (" + enc.Length + " bytes, " + (enc.Length / 128) + " blocks)");
                    }
                    return 0;
                }
                else
                {
                    Console.Error.WriteLine("Modo invalido: " + mode + ". Use xml, legacy-dat ou rsa1024.");
                    return 2;
                }
            }

            PrintUsage();
            return 1;
        }
    }
}
