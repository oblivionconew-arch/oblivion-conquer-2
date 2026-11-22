using System;
using Conquer_Online_Server.Network.GamePackets;

namespace Conquer_Online_Server.Game.Attacking
{
    public static class BlessEffect
    {
        public static void Effect(Entity client)
        {
            if (client == null || client.Owner == null || client.Owner.Equipment == null)
                return;

            byte bless1 = 0, bless3 = 0, bless5 = 0, bless7 = 0;
            for (byte i = 1; i < 12; i++)
            {
                if (i == ConquerItem.Bottle) continue;
                ConquerItem item = client.Owner.Equipment.TryGetItem(i);
                if (item == null || item.ID == 0) continue;

                switch (item.Bless)
                {
                    case 1: bless1++; break;
                    case 3: bless3++; break;
                    case 5: bless5++; break;
                    case 7: bless7++; break;
                }
            }

            string effectName = null;
            int denom = 25;
            if (bless7 > 0 && bless7 >= bless5 && bless7 >= bless3 && bless7 >= bless1)
            {
                effectName = "Aegis4";
                denom = 20;
            }
            else if (bless5 > 0 && bless5 >= bless3 && bless5 >= bless1)
            {
                effectName = "Aegis3";
            }
            else if (bless3 > 0 && bless3 >= bless1)
            {
                effectName = "Aegis2";
            }
            else if (bless1 > 0)
            {
                effectName = "Aegis1";
            }

            if (effectName != null && Kernel.Rate(5, denom))
            {
                _String str = new _String(true);
                str.UID = client.UID;
                str.TextsCount = 1;
                str.Type = _String.Effect;
                str.Texts.Add(effectName);
                client.Owner.SendScreen(str, true);
            }
        }
    }
}
