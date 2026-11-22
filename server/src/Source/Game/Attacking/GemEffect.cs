using System;
using Conquer_Online_Server.Network.GamePackets;

namespace Conquer_Online_Server.Game.Attacking
{
    public static class GemEffect
    {
        public static void Effect(Entity client)
        {
            if (client == null || client.Owner == null || client.Owner.Equipment == null)
                return;

            for (uint i = 1; i < 12; i++)
            {
                if (i == ConquerItem.Bottle) continue;
                ConquerItem item = client.Owner.Equipment.TryGetItem(i);
                if (item == null || item.ID == 0) continue;

                if (item.SocketOne != 0 && TryTriggerGem(client, item.SocketOne, false))
                    return;
                if (item.SocketTwo != 0 && TryTriggerGem(client, item.SocketTwo, true))
                    return;
            }
        }

        private static bool TryTriggerGem(Entity client, Enums.Gem gem, bool isSecondSocket)
        {
            string effect = null;
            int denom = isSecondSocket ? 100 : 200;

            switch (gem)
            {
                case Enums.Gem.SuperVioletGem:   effect = "purpleray"; break;
                case Enums.Gem.SuperPhoenixGem:  effect = "phoenix"; break;
                case Enums.Gem.SuperDragonGem:   effect = "goldendragon"; if (!isSecondSocket) denom = 300; break;
                case Enums.Gem.SuperRainbowGem:  effect = "rainbow"; break;
                case Enums.Gem.SuperMoonGem:     effect = "moon"; break;
                case Enums.Gem.SuperFuryGem:     effect = "fastflash"; break;
                case Enums.Gem.SuperKylinGem:    effect = "goldenkylin"; break;
                case Enums.Gem.SuperTortoiseGem: effect = "recovery"; break;
                default: return false;
            }

            if (Kernel.Rate(5, denom))
            {
                _String str = new _String(true);
                str.UID = client.UID;
                str.TextsCount = 1;
                str.Type = _String.Effect;
                str.Texts.Add(effect);
                client.Owner.SendScreen(str, true);
                return true;
            }
            return false;
        }
    }
}
