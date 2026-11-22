using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Conquer_Online_Server
{
    public static class Console
    {
        private static DateTime NOW = DateTime.Now;
        private static Time32 NOW32 = Time32.Now;
        public static string Title
        {
            get
            {
                try { return System.Console.Title; } catch { return string.Empty; }
            }
            set
            {
                try { System.Console.Title = value; } catch { }
            }
        }

        public static int WindowWidth
        {
            get
            {
                try { return System.Console.WindowWidth; } catch { return 80; }
            }
            set
            {
                try { System.Console.WindowWidth = value; } catch { }
            }
        }

        public static int WindowHeight
        {
            get
            {
                try { return System.Console.WindowHeight; } catch { return 25; }
            }
            set
            {
                try { System.Console.WindowHeight = value; } catch { }
            }
        }

        public static void WriteLine(object line)
        {
            if (line == null)
            {
                System.Console.WriteLine();
                return;
            }
            if (line.ToString() == "" || line.ToString() == " ")
                System.Console.WriteLine();
            else
            {
                ForegroundColor = ConsoleColor.Green;
                System.Console.Write(TimeStamp() + " ");
                ForegroundColor = ConsoleColor.Gray;
                System.Console.Write(line + "\n");
            }
        }

        public static void WriteLine()
        {
            System.Console.WriteLine();
        }

        public static string ReadLine()
        {
            try { return System.Console.ReadLine(); } catch { return null; }
        }

        public static ConsoleColor BackgroundColor
        {
            get
            {
                try { return System.Console.BackgroundColor; } catch { return ConsoleColor.Black; }
            }
            set
            {
                try { System.Console.BackgroundColor = value; } catch { }
            }
        }

        public static void Clear()
        {
            try { System.Console.Clear(); } catch { }
        }

        public static ConsoleColor ForegroundColor
        {
            get
            {
                try { return System.Console.ForegroundColor; } catch { return ConsoleColor.Gray; }
            }
            set
            {
                try { System.Console.ForegroundColor = value; } catch { }
            }
        }

        public static string TimeStamp()
        {
            return "[" + NOW.AddMilliseconds((Time32.Now - NOW32).GetHashCode()).ToString("hh:mm:ss") + "]";
        }
    }
}
