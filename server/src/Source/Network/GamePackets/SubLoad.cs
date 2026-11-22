using System;

namespace Conquer_Online_Server.Network.GamePackets
{
    public static class SubLoad
    {
        public static void SubClass(Client.GameClient client)
        {
            if (client == null || client.Entity == null || client.Entity.SubClasses == null || client.Entity.SubClasses.Classes == null)
                return;

            foreach (Conquer_Online_Server.Game.SubClass sub in client.Entity.SubClasses.Classes.Values)
            {
                if (sub == null) continue;

                switch (sub.ID)
                {
                    case 1: // MartialArtist -> CriticalStrike
                        client.Entity.CriticalStrike += GetTierBonus100To1500(sub.Phase);
                        break;
                    case 2: // Warlock -> SkillCStrike
                        client.Entity.SkillCStrike = (ushort)(client.Entity.SkillCStrike + GetTierBonus100To1500(sub.Phase));
                        break;
                    case 3: // ChiMaster -> Immunity
                        client.Entity.Immunity += GetTierBonus100To1500(sub.Phase);
                        break;
                    case 4: // Sage -> Penetration
                        client.Entity.Penetration = (ushort)(client.Entity.Penetration + GetTierBonus100To1500(sub.Phase));
                        break;
                    case 5: // Apothecary -> Detoxication
                        if (sub.Phase >= 1 && sub.Phase <= 9)
                            client.Entity.Detoxication = (ushort)(client.Entity.Detoxication + (sub.Phase * 8));
                        break;
                    case 6: // Performer -> Physical & Magic Attack
                        if (sub.Phase >= 1 && sub.Phase <= 9)
                        {
                            uint bonus = (sub.Phase == 9) ? 1000u : (uint)(sub.Phase * 100);
                            client.Entity.BaseMaxAttack += bonus;
                            client.Entity.BaseMinAttack += bonus;
                            client.Entity.BaseMagicAttack += bonus;
                        }
                        break;
                    case 9: // Wrangler -> Max HP
                        client.Entity.ItemHP = (ushort)(client.Entity.ItemHP + GetWranglerHpBonus(sub.Phase));
                        break;
                }
            }
        }

        private static int GetTierBonus100To1500(byte phase)
        {
            switch (phase)
            {
                case 1: return 100;
                case 2: return 200;
                case 3: return 300;
                case 4: return 400;
                case 5: return 600;
                case 6: return 800;
                case 7: return 1000;
                case 8: return 1200;
                case 9: return 1500;
                default: return 0;
            }
        }

        private static ushort GetWranglerHpBonus(byte phase)
        {
            switch (phase)
            {
                case 1: return 100;
                case 2: return 200;
                case 3: return 300;
                case 4: return 400;
                case 5: return 500;
                case 6: return 600;
                case 7: return 800;
                case 8: return 1000;
                case 9: return 1200;
                default: return 0;
            }
        }
    }
}
