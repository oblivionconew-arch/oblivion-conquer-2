using System;
using System.Collections.Generic;
using System.IO;
using Conquer_Online_Server.Network.GamePackets;


namespace Conquer_Online_Server.Database
{
    public class DROP_SOULS
    {
        public static Dictionary<uint, Items_drop> Souls = new Dictionary<uint, Items_drop>();
        public static uint Count_souls = 0;

        public class Items_drop
        {
            public uint item_id;
            public string Item_denumire;
            public uint item_rand;
        }

        public static void LoadDrops()
        {
            /*Load Souls Drop p1 / p2 /p3 */
            string[] aFMobs = File.ReadAllLines("database\\sopuls.txt");
            for (int i = 0; i < aFMobs.Length; i++)
            {
                if (aFMobs[i][0] != '*')
                {
                    string[] Info = aFMobs[i].Split(' ');
                    Items_drop Item = new Items_drop();
                    Item.Item_denumire = "SOULS";
                    Item.item_id = uint.Parse(Info[0]);
                    Count_souls += 1;

                    Souls.Add(Count_souls, Item);
                }
            }
            Console.WriteLine("Souls loading " + Souls.Count);
        }
    }
    public class MonsterInformation
    {
        private struct SpecialItemDrop
        {
            public int ItemID, Rate, Discriminant, Map;
        }
        private static List<SpecialItemDrop> SpecialItemDropList = new List<SpecialItemDrop>();
        public Game.Entity Owner;

        public uint ExcludeFromSend = 0;
        private bool LabirinthDrop = false;
        public bool Guard, Reviver;
        public uint ID;
        public ushort Mesh;
        public byte Level;
        public uint Type;
        public string Name;
        public uint Hitpoints;
        public ushort Defence;
        public ushort ViewRange;
        public ushort AttackRange;
        public int RespawnTime;
        public uint MinAttack, MaxAttack;
        public byte AttackType;
        public ushort SpellID;
        public uint InSight;
        public bool ISLava = false;
        public bool Boss;
        public Time32 LastMove;
        public int MoveSpeed;
        public int RunSpeed;
        public int OwnItemID, OwnItemRate;
        public int HPPotionID, MPPotionID;
        public int AttackSpeed;
        public int MinimumSpeed
        {
            get
            {
                int min = 10000000;
                if (min > MoveSpeed)
                    min = MoveSpeed;
                if (min > RunSpeed)
                    min = RunSpeed;
                if (min > AttackSpeed)
                    min = AttackSpeed;
                return min;
            }
        }
        public uint ExtraExperience;
        public uint MinMoneyDropAmount;
        public uint MaxMoneyDropAmount;

        public ushort BoundX, BoundY;
        public ushort BoundCX, BoundCY;

        public static SafeDictionary<byte, List<uint>> ItemDropCache = new SafeDictionary<byte, List<uint>>(3000);
        public static SafeDictionary<byte, List<uint>> SoulItemCache = new SafeDictionary<byte, List<uint>>(3000);

        public void SendScreen(byte[] buffer)
        {
            foreach (Client.GameClient client in Program.GamePool)
            {
                if (client != null)
                {
                    if (client.Entity != null)
                    {
                        if (client.Entity.UID != ExcludeFromSend)
                        {
                            if (Kernel.GetDistance(client.Entity.X, client.Entity.Y, Owner.X, Owner.Y) > 18)
                            {
                                continue;
                            }
                            client.Send(buffer);
                        }
                    }
                }
            }
        }
        public void SendScreen(Interfaces.IPacket buffer)
        {
            SendScreen(buffer.ToArray());
        }
        public void SendScreenSpawn(Interfaces.IMapObject _object)
        {
            foreach (Client.GameClient client in Program.GamePool)
            {
                if (client != null)
                {
                    if (client.Entity != null)
                    {
                        if (client.Entity.UID != ExcludeFromSend)
                        {
                            if (client.Map.ID == Owner.MapID)
                            {
                                if (Kernel.GetDistance(client.Entity.X, client.Entity.Y, Owner.X, Owner.Y) > 25)
                                {
                                    continue;
                                }
                                _object.SendSpawn(client, false);
                            }
                        }
                    }
                }
            }
        }
        public static bool ItemsInInventory = false;
        public void Drop(Game.Entity killer)
        {
            if (Owner.Name.Contains("Guard"))
                return;

            #region Dragon King
            if (Owner.Name == "Dragon King" && Program.World.PureLand)
            {
//#warning PURE LAND PRIZE ( THE PORTAL )
                Kernel.SendWorldMessage(new Message(killer.Owner.Entity.Name + " derrotou o Rei Dragão da Terra Pura e ganhou um grande prêmio!", Message.Center));
                killer.Owner.Entity.ConquerPoints += 1000;
            }
            #endregion
            #region HalfOffToken
            if (killer.EntityFlag == Game.EntityFlag.Player)
            {
                DateTime halfOffDateTime = killer.Owner["halfoffdate"];
                if (killer.Owner["halfoff"])
                    if (DateTime.Now.DayOfYear != halfOffDateTime.DayOfYear)
                        killer.Owner["halfoff"] = false;
                if (!killer.Owner["halfoff"])
                {
                    if (Kernel.Rate(1, 10000))
                    {
                        killer.Owner["halfoff"] = true;
                        killer.Owner["halfoffdate"] = DateTime.Now;

                        uint ID = 480339;

                        var infos = Database.ConquerItemInformation.BaseInformations[ID];
                        if (infos == null) return;
                        ushort X = Owner.X, Y = Owner.Y;
                        Game.Map Map = Kernel.Maps[Owner.MapID];
                        if (Map.SelectCoordonates(ref X, ref Y))
                        {
                            FloorItem floorItem = new FloorItem(true);
                            floorItem.Item = new Network.GamePackets.ConquerItem(true);
                            floorItem.Item.Color = (Game.Enums.Color)Kernel.Random.Next(4, 8);
                            floorItem.Item.ID = ID;
                            floorItem.Item.MaximDurability = infos.Durability;

                            floorItem.Item.Durability = (ushort)(Kernel.Random.Next(infos.Durability / 10));

                            if (!Network.PacketHandler.IsEquipment(ID) && infos.ConquerPointsWorth == 0)
                            {
                                floorItem.Item.StackSize = 1;
                                floorItem.Item.MaxStackSize = infos.StackSize;
                            }
                            floorItem.Item.MobDropped = true;
                            floorItem.ValueType = FloorItem.FloorValueType.Item;
                            floorItem.ItemID = ID;
                            floorItem.MapID = Owner.MapID;
                            floorItem.MapObjType = Game.MapObjectType.Item;
                            floorItem.X = X;
                            floorItem.Y = Y;
                            floorItem.Owner = killer.Owner;
                            floorItem.Type = FloorItem.Drop;
                            floorItem.OnFloor = Time32.Now;
                            floorItem.ItemColor = floorItem.Item.Color;
                            floorItem.UID = FloorItem.FloorUID.Next;
                            while (Map.Npcs.ContainsKey(floorItem.UID))
                                floorItem.UID = FloorItem.FloorUID.Next;
                            Map.AddFloorItem(floorItem);
                            SendScreenSpawn(floorItem);
                        }
                    }
                }
            }
            #endregion
            if (ItemsInInventory)
            {
                #region [Organizer-Stars 1]
                if (Name == "Apparition")
                {
                    if (Kernel.Rate(1, 5))
                    {
                        if (killer.Name.Contains("Guard"))
                        {
                            return;
                        }
                        killer.Owner.Inventory.Add(710222, 0, 1);
                        killer.Owner.Send(new Network.GamePackets.Message("Congratulation You Have Got The Item From Here Go to Another Map", System.Drawing.Color.White, 255));
                    }
                }
                #endregion
                #region [Organizer-Stars 2]
                if (Name == "Bandit")
                {
                    if (Kernel.Rate(1, 5))
                    {
                        if (killer.Name.Contains("Guard"))
                        {
                            return;
                        }
                        killer.Owner.Inventory.Add(710852, 0, 1);
                        killer.Owner.Send(new Network.GamePackets.Message("Congratulation You Have Got The Item From Here Go to Another Map", System.Drawing.Color.Yellow, 255));
                    }
                }
                #endregion
                #region [Organizer-Stars 3]
                if (Name == "HillMonster")
                {
                    if (Kernel.Rate(1, 5))
                    {
                        if (killer.Name.Contains("Guard"))
                        {
                            return;
                        }
                        killer.Owner.Inventory.Add(711138, 0, 1);
                        killer.Owner.Send(new Network.GamePackets.Message("Congratulation You Have Got The Item From Here Go to Another Map", System.Drawing.Color.Yellow, 255));
                    }
                }
                #endregion
                #region [Organizer-Stars 4]
                if (Name == "Robin")
                {
                    if (Kernel.Rate(1, 5))
                    {
                        if (killer.Name.Contains("Guard"))
                        {
                            return;
                        }
                        killer.Owner.Inventory.Add(711252, 0, 1);
                        killer.Owner.Send(new Network.GamePackets.Message("Congratulation You Have Got The Item From Here Go to Another Map", System.Drawing.Color.Yellow, 255));
                    }
                }
                #endregion
                #region [OrganizerStars 5]
                if (Name == "Ratling")
                {
                    if (Kernel.Rate(1, 5))
                    {
                        if (killer.Name.Contains("Guard"))
                        {
                            return;
                        }
                        killer.Owner.Inventory.Add(711253, 0, 1);
                        killer.Owner.Send(new Network.GamePackets.Message("Congratulation You Have Got The Item From Here Go to Another Map", System.Drawing.Color.Yellow, 255));
                    }
                }
                #endregion
                #region [Organizer-Stars 6]
                if (Name == "BladeGhost")
                {
                    if (Kernel.Rate(1, 5))
                    {
                        if (killer.Name.Contains("Guard"))
                        {
                            return;
                        }
                        killer.Owner.Inventory.Add(722682, 0, 1);
                        killer.Owner.Send(new Network.GamePackets.Message("Congratulation You Have Got The Item From Here Go to Another Map", System.Drawing.Color.Yellow, 255));
                    }
                }
                #endregion
                #region [Organizer-Stars 7]
                if (Name == "Birdman")
                {
                    /*if (Kernel.Rate(1, 5))
                    {
                        if (killer.Name.Contains("Conquistando"))
                        {
                            return;
                        }
                        killer.Owner.Inventory.Add(722894, 0, 1);
                        killer.Owner.Send(new Network.GamePackets.Message("Congratulation You Have Got The Item From Here Go to Another Map", System.Drawing.Color.Yellow, 255));
                    }*/
                }
                #endregion
                #region [Organizer-Stars 8]
                if (Name == "HawKing")
                {
                    if (Kernel.Rate(1, 5))
                    {
                        if (killer.Name.Contains("Guard"))
                        {
                            return;
                        }
                        killer.Owner.Inventory.Add(711021, 0, 1);
                        killer.Owner.Send(new Network.GamePackets.Message("Congratulation You Have Got The Item From Here Go to Another Map", System.Drawing.Color.Yellow, 255));
                    }
                }
                #endregion
                #region [Organizer-Stars 9]
                if (Name == "Macaque")
                {
                    if (Kernel.Rate(1, 5))
                    {
                        if (killer.Name.Contains("Guard"))
                        {
                            return;
                        }
                        killer.Owner.Inventory.Add(722895, 0, 1);
                        killer.Owner.Send(new Network.GamePackets.Message("Congratulation You Have Got The Item From Here Go to Another Map", System.Drawing.Color.Yellow, 255));
                    }
                }
                #endregion
            }
            #region DemonBoxs
            if (killer.EntityFlag == Game.EntityFlag.Player || killer.Companion)
            {
                uint cps = 0;
                if (Name == "Demon")
                {
                    if (Kernel.Rate(30, 1000)) cps += 1;
                    else if (Kernel.Rate(40, 1000)) cps += 1;
                    else if (Kernel.Rate(70, 1000)) cps += 1;
                    else if (Kernel.Rate(80, 1000)) cps += 1;
                    else cps += 1;

                    killer.Owner.Entity.ConquerPoints += cps;
                    Kernel.SendWorldMessage(new Message(killer.Owner.Entity.Name + " ganhou " + cps.ToString() + " CPs ao derrotar o monstro " + Name + "!", System.Drawing.Color.Yellow, 2000));
                }
                if (Name == "AncientDemon")
                {
                    if (Kernel.Rate(30, 1000)) cps += 1;
                    else if (Kernel.Rate(40, 1000)) cps += 1;
                    else if (Kernel.Rate(70, 1000)) cps += 1;
                    else if (Kernel.Rate(80, 1000)) cps += 1;
                    else cps += 1;

                    killer.Owner.Entity.ConquerPoints += cps;
                    Kernel.SendWorldMessage(new Message(killer.Owner.Entity.Name + " ganhou " + cps.ToString() + " CPs ao derrotar o monstro " + Name + "!", System.Drawing.Color.Yellow, 2000));
                }
                if (Name == "FloodDemon")
                {
                    if (Kernel.Rate(30, 1000)) cps += 1;
                    else if (Kernel.Rate(40, 1000)) cps += 1;
                    else if (Kernel.Rate(70, 1000)) cps += 1;
                    else if (Kernel.Rate(80, 1000)) cps += 1;
                    else cps += 1;

                    killer.Owner.Entity.ConquerPoints += cps;
                    Kernel.SendWorldMessage(new Message(killer.Owner.Entity.Name + " ganhou " + cps.ToString() + " CPs ao derrotar o monstro " + Name + "!", System.Drawing.Color.Yellow, 2000));
                }
                if (Name == "HeavenDemon")
                {
                    if (Kernel.Rate(30, 1000)) cps += 1;
                    else if (Kernel.Rate(40, 1000)) cps += 1;
                    else if (Kernel.Rate(70, 1000)) cps += 1;
                    else if (Kernel.Rate(80, 1000)) cps += 1;
                    else cps += 1;

                    killer.Owner.Entity.ConquerPoints += cps;
                    Kernel.SendWorldMessage(new Message(killer.Owner.Entity.Name + " ganhou " + cps.ToString() + " CPs ao derrotar o monstro " + Name + "!", System.Drawing.Color.Yellow, 2000));
                }
                if (Name == "ChaosDemon")
                {
                    if (Kernel.Rate(30, 1000)) cps += 1;
                    else if (Kernel.Rate(40, 1000)) cps += 1;
                    else if (Kernel.Rate(70, 1000)) cps += 1;
                    else if (Kernel.Rate(80, 1000)) cps += 1;
                    else cps += 1;

                    killer.Owner.Entity.ConquerPoints += cps;
                    Kernel.SendWorldMessage(new Message(killer.Owner.Entity.Name + " ganhou " + cps.ToString() + " CPs ao derrotar o monstro " + Name + "!", System.Drawing.Color.Yellow, 2000));
                }
                if (Name == "ScaredDemon")
                {
                    if (Kernel.Rate(30, 1000)) cps += 1;
                    else if (Kernel.Rate(40, 1000)) cps += 1;
                    else if (Kernel.Rate(70, 1000)) cps += 1;
                    else if (Kernel.Rate(80, 1000)) cps += 1;
                    else cps += 1;

                    killer.Owner.Entity.ConquerPoints += cps;
                    Kernel.SendWorldMessage(new Message(killer.Owner.Entity.Name + " ganhou " + cps.ToString() + " CPs ao derrotar o monstro " + Name + "!", System.Drawing.Color.Yellow, 2000));
                }
            }
            #endregion
            if (Name == "Naga")
            {
                {
                    killer.DisKO += 1;
                    killer.Owner.Send(new Message("Parabéns! Você ganhou 1 ponto e agora tem " + killer.DisKO + " pontos de DisCity!", System.Drawing.Color.Azure, Message.TopLeft));
                    return;
                }
            }
            if (Name == "ThrillingSpook")
            {
                {
                    killer.ConquerPoints += 5000;
                    killer.TreasuerPoints += 1000;
                    killer.SubClasses.StudyPoints += 500;
                    Conquer_Online_Server.Kernel.SendWorldMessage(new Conquer_Online_Server.Network.GamePackets.Message("Parabens! " + killer.Name + " Matou ThrillingSpook E ganhou 5k de cps e 1000 PONTOS !", System.Drawing.Color.IndianRed, 2011), Program.GamePool);
                    return;
                }
            }
            if (Name == "Temptress")
            {
                {
                    killer.DisKO += 1;
                    killer.Owner.Send(new Message("Parabéns! Você ganhou 1 ponto e agora tem " + killer.DisKO + " pontos de DisCity!", System.Drawing.Color.Azure, Message.TopLeft));
                    return;
                }
            }
            if (Name == "Centicore")
            {
                {
                    killer.DisKO += 1;
                    killer.Owner.Send(new Message("Parabéns! Você ganhou 1 ponto e agora tem " + killer.DisKO + " pontos de DisCity!", System.Drawing.Color.Azure, Message.TopLeft));
                    return;
                }
            }
            if (Name == "HellTroll")
            {
                {
                    killer.DisKO += 3;
                    killer.Owner.Send(new Message("Parabéns! Você ganhou 3 pontos e agora tem " + killer.DisKO + " pontos de DisCity!", System.Drawing.Color.Azure, Message.TopLeft));
                    return;
                }
            }
            #region BOSS FELIPE SOOUZA & DROP
            #region LordTiger
            if (Name == "LordTiger")
            {
                byte times = (byte)Kernel.Random.Next(1, 1);
                for (byte i = 0; i < times; i++)
                {
                    uint Uid = 0;
                    byte type = (byte)Kernel.Random.Next(1, 1);
                    switch (type)
                    {
                        case 1:
                            //Uid = 711610;
                            break;

                    }

                    if (Uid != 0)
                    {
                        killer.Owner.Inventory.Add(Uid, 0, 1);
                    }
                }
                Kernel.Spawn = false;
                killer.ConquerPoints += 150000;
                killer.TreasuerPoints += 1000;
                killer.SubClasses.StudyPoints += 500;
                Conquer_Online_Server.Kernel.SendWorldMessage(new Message("Parabens " + killer.Name + " Matou o boss " + Name + " e Dropou 150k de cps e 1000 PONTOS ", System.Drawing.Color.Black, Message.Center), Program.GamePool);
                return;
            }
            #endregion
            #region BOSS_GLOBAL
            if (Name == "BOSS_GLOBAL")
            {
                byte times = (byte)Kernel.Random.Next(1, 1);
                for (byte i = 0; i < times; i++)
                {
                    uint Uid = 0;
                    byte type = (byte)Kernel.Random.Next(1, 1);
                    switch (type)
                    {
                        case 1:
                           // Uid = 711610;
                            break;

                    }

                    if (Uid != 0)
                    {
                        killer.Owner.Inventory.Add(Uid, 0, 1);
                    }
                }
                Kernel.Spawn = false;
                killer.ConquerPoints += 150000;
                killer.TreasuerPoints += 1000;
                killer.SubClasses.StudyPoints += 500;
                Conquer_Online_Server.Kernel.SendWorldMessage(new Message("Parabens " + killer.Name + " Matou o boss " + Name + " e Dropou 150k de cps e 1000 PONTOS ", System.Drawing.Color.Black, Message.Center), Program.GamePool);
                return;
            }
            #endregion
            #region CDG_GLOBAL
            if (Name == "CDG_GLOBAL")
            {
                byte times = (byte)Kernel.Random.Next(1, 1);
                for (byte i = 0; i < times; i++)
                {
                    uint Uid = 0;
                    byte type = (byte)Kernel.Random.Next(1, 26);
                    switch (type)
                    {
                        case 1:
                            Uid = 800020;
                            break;

                        case 2:
                            Uid = 800111;
                            break;

                        case 3:
                            Uid = 800142;
                            break;

                        case 4:
                            Uid = 800215;
                            break;

                        case 5:
                            Uid = 800255;
                            break;

                        case 6:
                            Uid = 800422;
                            break;

                        case 7:
                            Uid = 800522;
                            break;

                        case 8:
                            Uid = 800618;
                            break;

                        case 9:
                            Uid = 800725;
                            break;

                        case 10:
                            Uid = 800810;
                            break;

                        case 11:
                            Uid = 800811;
                            break;

                        case 12:
                            Uid = 800917;
                            break;

                        case 13:
                            Uid = 820073;
                            break;

                        case 14:
                            Uid = 820074;
                            break;

                        case 15:
                            Uid = 820075;
                            break;

                        case 16:
                            Uid = 820076;
                            break;

                        case 17:
                            Uid = 821033;
                            break;

                        case 18:
                            Uid = 821034;
                            break;

                        case 19:
                            Uid = 822071;
                            break;

                        case 20:
                            Uid = 822072;
                            break;

                        case 21:
                            Uid = 823058;
                            break;

                        case 22:
                            Uid = 823059;
                            break;

                        case 23:
                            Uid = 823060;
                            break;

                        case 24:
                            Uid = 824018;
                            break;

                        case 25:
                            Uid = 824019;
                            break;

                        case 26:
                            Uid = 824020;
                            break;
                    }
                    if (Uid != 0)
                    {
                        killer.Owner.Inventory.Add(Uid, 0, 1);
                    }
                }
                Kernel.Spawn = false;
                killer.SubClasses.StudyPoints += 500;
                Conquer_Online_Server.Kernel.SendWorldMessage(new Message("Parabens " + killer.Name + " Matou o boss " + Name + "e 1 soul P7 #00 ", System.Drawing.Color.Black, Message.Center), Program.GamePool);
                return;
            }
            #endregion
            #region Rei_Tigre
            if (Name == "Rei_Tigre")
            {
                byte times = (byte)Kernel.Random.Next(1, 1);
                for (byte i = 0; i < times; i++)
                {
                    uint Uid = 0;
                    byte type = (byte)Kernel.Random.Next(1, 1);
                    switch (type)
                    {
                        case 1:
                           // Uid = 711610;
                            break;

                    }

                    if (Uid != 0)
                    {
                        killer.Owner.Inventory.Add(Uid, 0, 1);
                    }
                }
                Kernel.Spawn = false;
                killer.ConquerPoints += 150000;
                killer.TreasuerPoints += 1000;
                killer.SubClasses.StudyPoints += 500;
                Conquer_Online_Server.Kernel.SendWorldMessage(new Message("Parabens " + killer.Name + " Matou o boss " + Name + " e Dropou 150k de cps e 1000 PONTOS ", System.Drawing.Color.Black, Message.Center), Program.GamePool);
                return;
            }
            #endregion
            #region FatalBoss
            if (Name == "FatalBoss")
            {
                byte times = (byte)Kernel.Random.Next(1, 1);
                for (byte i = 0; i < times; i++)
                {
                    uint Uid = 0;
                    byte type = (byte)Kernel.Random.Next(1, 1);
                    switch (type)
                    {
                        case 1:
                        //    Uid = 711610;
                            break;

                    }

                    if (Uid != 0)
                    {
                        killer.Owner.Inventory.Add(Uid, 0, 1);
                    }
                }
                Kernel.Spawn = false;
                killer.ConquerPoints += 150000;
                killer.TreasuerPoints += 1000;
                killer.SubClasses.StudyPoints += 500;
                Conquer_Online_Server.Kernel.SendWorldMessage(new Message("Parabens " + killer.Name + " Matou o boss " + Name + " E Dropou 150k de cps e 1000 PONTOS ", System.Drawing.Color.Black, Message.Center), Program.GamePool);
                return;
            }
            #endregion
            #region MrBoss
            if (Name == "MrBoss")
            {
                byte times = (byte)Kernel.Random.Next(1, 1);
                for (byte i = 0; i < times; i++)
                {
                    uint Uid = 0;
                    byte type = (byte)Kernel.Random.Next(1, 1);
                    switch (type)
                    {
                        case 1:
                        //    Uid = 711610;
                            break;

                    }

                    if (Uid != 0)
                    {
                        killer.Owner.Inventory.Add(Uid, 0, 1);
                    }
                }
                Kernel.Spawn = false;
                killer.ConquerPoints += 150000;
                killer.TreasuerPoints += 1000;
                killer.SubClasses.StudyPoints += 500;
                Conquer_Online_Server.Kernel.SendWorldMessage(new Message("Parabens " + killer.Name + " Matou o boss " + Name + " e Dropou 150k de cps e 1000 PONTOS ", System.Drawing.Color.Black, Message.Center), Program.GamePool);
                return;
            }
            #endregion
            #region LevelBoss
            if (Name == "LevelBoss")
            {
                byte times = (byte)Kernel.Random.Next(1, 1);
                byte ref_times = (byte)Kernel.Random.Next(1, 1);
                for (byte i = 0; i < times; i++)
                {
                    uint Uid = 0;
                    byte type = (byte)Kernel.Random.Next(1, 1);
                    switch (type)
                    {
                        case 1:
                       //     Uid = 711252;
                            break;

                    }

                    if (Uid != 0)
                    {
                        killer.Owner.Inventory.Add(Uid, 0, 2);
                    }
                }
                Kernel.Spawn = false;
                killer.ConquerPoints += 1000;
                killer.TreasuerPoints += 150;
                Conquer_Online_Server.Kernel.SendWorldMessage(new Conquer_Online_Server.Network.GamePackets.Message("Parabens " + killer.Name + " Matou o " + Name + " e Pegou 2 LevelCard e 150 PONTOS ", Message.TopLeft));
                return;
            }
            #endregion
            #region GanodermaL72
            if (Name == "GanodermaL72")
            {
                byte times = (byte)Kernel.Random.Next(1, 1);
                for (byte i = 0; i < times; i++)
                {
                    uint Uid = 0;
                    byte type = (byte)Kernel.Random.Next(1, 1);
                    switch (type)
                    {
                        case 1:
                         //   Uid = 711610;
                            break;

                    }

                    if (Uid != 0)
                    {
                        killer.Owner.Inventory.Add(Uid, 0, 1);
                    }
                }
                Kernel.Spawn = false;
                killer.ConquerPoints += 5000;
                killer.TreasuerPoints += 1000;
                killer.SubClasses.StudyPoints += 500;
                Conquer_Online_Server.Kernel.SendWorldMessage(new Conquer_Online_Server.Network.GamePackets.Message("Parabens " + killer.Name + " Matou o boss " + Name + "e Dropou 5k de cps e 1000 PONTOS ", Message.TopLeft));
                return;
            }
            #endregion
            #region Demonio Gordo
            if (Name == "Demonio Gordo")
            {
                byte times = (byte)Kernel.Random.Next(1, 1);
                for (byte i = 0; i < times; i++)
                {
                    uint Uid = 0;
                    byte type = (byte)Kernel.Random.Next(1, 1);
                    switch (type)
                    {
                        case 1:
                        //    Uid = 711610;
                            break;

                    }

                    if (Uid != 0)
                    {
                        killer.Owner.Inventory.Add(Uid, 0, 1);
                    }
                }
                Kernel.Spawn = false;
                killer.ConquerPoints += 150000;
                killer.TreasuerPoints += 1000;
                killer.SubClasses.StudyPoints += 500;
                Conquer_Online_Server.Kernel.SendWorldMessage(new Message("Parabens " + killer.Name + " Matou o boss " + Name + " e Dropou 150k de cps e 1000 PONTOS ", System.Drawing.Color.Black, Message.Center), Program.GamePool);
                return;
            }
            #endregion
            #endregion

            #region Sistema De Drop Cps Phaesant
            if (Name == "Pheasant")
                if (killer.EntityFlag == Game.EntityFlag.Player)
                    if (Kernel.Rate(100))
                    {
                        if (killer.Name.Contains("Guard"))
                        {
                            return;
                        }
                        killer.ConquerPoints += 200;
                        killer.SubClasses.StudyPoints += 0;
                        killer.Owner.Send(new Network.GamePackets.Message("#36Voce Ganhou 200Cps #36#35Scorpions-Games#35", System.Drawing.Color.Blue, 2005));
                    }
            #endregion
            #region Sistema De Drop Cps Turtledove
            if (Name == "Turtledove")
                if (killer.EntityFlag == Game.EntityFlag.Player)
                    if (Kernel.Rate(100))
                    {
                        if (killer.Name.Contains("Guard"))
                        {
                            return;
                        }
                        killer.ConquerPoints += 200;
                        killer.SubClasses.StudyPoints += 0;
                        killer.Owner.Send(new Network.GamePackets.Message("#36Voce Ganhou 200Cps #36#35Scorpions-Games#35", System.Drawing.Color.Blue, 2005));
                    }
            #endregion
            #region Sistema De Drop Cps Espectro
            if (Name == "Espectro")
                if (killer.EntityFlag == Game.EntityFlag.Player)
                    if (Kernel.Rate(100))
                    {
                        if (killer.Name.Contains("Guard"))
                        {
                            return;
                        }
                        killer.ConquerPoints += 200;
                        killer.SubClasses.StudyPoints += 0;
                        killer.Owner.Send(new Network.GamePackets.Message("#36Voce Ganhou 200Cps #36#35Scorpions-Games#35", System.Drawing.Color.Blue, 2005));
                    }
            #endregion
            #region Sistema De Drop Cps Macacos
            if (Name == "Macaco")
                if (killer.EntityFlag == Game.EntityFlag.Player)
                    if (Kernel.Rate(100))
                    {
                        if (killer.Name.Contains("Guard"))
                        {
                            return;
                        }
                        killer.ConquerPoints += 200;
                        killer.SubClasses.StudyPoints += 0;
                        killer.Owner.Send(new Network.GamePackets.Message("#36Voce Ganhou 200Cps #36#35Scorpions-Games#35", System.Drawing.Color.Blue, 2005));
                    }
            #endregion
            #region Sistema De Drop Cps Birdman
            if (Name == "Birdman")
                if (killer.EntityFlag == Game.EntityFlag.Player)
                    if (Kernel.Rate(100))
                    {
                        if (killer.Name.Contains("Guard"))
                        {
                            return;
                        }
                        killer.ConquerPoints += 200;
                        killer.SubClasses.StudyPoints += 0;
                        killer.Owner.Send(new Network.GamePackets.Message("#36Voce Ganhou 200Cps #36#35Scorpions-Games#35", System.Drawing.Color.Blue, 2005));
                    }
            #endregion
            #region Sistema De Drop Cps VIP TombBat
            if (Name == "Murcielago")
                if (killer.EntityFlag == Game.EntityFlag.Player)
                    if (Kernel.Rate(100))
                    {
                        if (killer.Name.Contains("Guard"))
                        {
                            return;
                        }
                        killer.ConquerPoints += 200;
                        killer.SubClasses.StudyPoints += 0;
                        killer.Owner.Send(new Network.GamePackets.Message("#36Voce Ganhou 200Cps #36#35Scorpions-Games#35", System.Drawing.Color.Blue, 2005));
                    }
            #endregion
            #region Sistema De Drop Cps VIP BloodyBat
            if (Name == "BloodyBat")
                if (killer.EntityFlag == Game.EntityFlag.Player)
                    if (Kernel.Rate(100))
                    {
                        if (killer.Name.Contains("Guard"))
                        {
                            return;
                        }
                        killer.ConquerPoints += 200;
                        killer.SubClasses.StudyPoints += 0;
                        killer.Owner.Send(new Network.GamePackets.Message("#36Voce Ganhou 200Cps #36#35Scorpions-Games#35", System.Drawing.Color.Blue, 2005));
                    }
            #endregion
            #region Sistema De Drop Cps VIP RedDevilL117
            if (Name == "RedDevilL117")
                if (killer.EntityFlag == Game.EntityFlag.Player)
                    if (Kernel.Rate(100))
                    {
                        if (killer.Name.Contains("Guard"))
                        {
                            return;
                        }

                        killer.ConquerPoints += 200;
                        killer.SubClasses.StudyPoints += 0;
                        killer.Owner.Send(new Network.GamePackets.Message("#36Voce Ganhou 200Cps #36#35Scorpions-Games#35)", System.Drawing.Color.Blue, 2005));
                    }
            #endregion
            #region Sistema De Drop Cps VIP DevilKingL120
            if (Name == "DevilKingL120")
                if (killer.EntityFlag == Game.EntityFlag.Player)
                    if (Kernel.Rate(100))
                    {
                        if (killer.Name.Contains("Guard"))
                        {
                            return;
                        }

                        killer.ConquerPoints += 200;
                        killer.SubClasses.StudyPoints += 0;
                        killer.Owner.Send(new Network.GamePackets.Message("#36Voce Ganhou 200Cps #36#35Scorpions-Games#35", System.Drawing.Color.Blue, 2005));
                    }
            #endregion
            #region Sistema De Drop Cps VIP BullMonster
            if (Name == "BullMonster")
                if (killer.EntityFlag == Game.EntityFlag.Player)
                    if (Kernel.Rate(100))
                    {
                        if (killer.Name.Contains("Guard"))
                        {
                            return;
                        }

                        killer.ConquerPoints += 200;
                        killer.SubClasses.StudyPoints += 200;
                        killer.Owner.Send(new Network.GamePackets.Message("#36Voce Ganhou 200Cps #36#35Scorpions-Games#35", System.Drawing.Color.Blue, 2005));
                    }
            #endregion
            #region Sistema De Drop Cps WingedSnake MAPA VIP
            if (Name == "WingedSnake")
                if (killer.EntityFlag == Game.EntityFlag.Player)
                    if (Kernel.Rate(100))
                    {
                        if (killer.Name.Contains("Guard"))
                        {
                            return;
                        }
                        killer.ConquerPoints += 200;
                        killer.TreasuerPoints += 0;
                        killer.Owner.Send(new Network.GamePackets.Message("#36Voce Ganhou 200Cps #36#35Scorpions-Games#35 1 Ponto POR SER VIP ;)", System.Drawing.Color.Blue, 2005));
                    }
            #endregion

            if (Name == "TeratoDragon")
            {
                byte times = (byte)Kernel.Random.Next(1, 3);
                byte ref_times = (byte)Kernel.Random.Next(1, 6);
                for (byte i = 0; i < times; i++)
                {
                    uint Uid = 0;
                    byte type = (byte)Kernel.Random.Next(1, 50);
                    switch (type)
                    {
                        case 1:
                      //      Uid = 711610;
                            break;

                    }

                    if (Uid != 0)
                    {
                        killer.SubClasses.StudyPoints += 5000;
                        killer.Owner.Inventory.Add(Uid, 0, 1);
                        killer.ConquerPoints += 200000;
                        killer.TreasuerPoints += 1500;
                        Conquer_Online_Server.Kernel.SendWorldMessage(new Conquer_Online_Server.Network.GamePackets.Message("Parabens! " + killer.Name + " Você matou " + Name + " ganhou 200k de cps, 1500 PONTOS e derrubou o item!! " + Database.ConquerItemInformation.BaseInformations[Uid].Name + "!", System.Drawing.Color.White, 2011), Program.GamePool);
                        return;
                    }
                }
            }

            int Rand = Conquer_Online_Server.Kernel.Random.Next(1, 1200);
            if (Rand >= 529 && Rand <= 540)
            {
                uint randsouls = (uint)Conquer_Online_Server.Kernel.Random.Next(1, (int)Database.DROP_SOULS.Count_souls);
                uint ItemID = Database.DROP_SOULS.Souls[randsouls].item_id;
                var infos = Database.ConquerItemInformation.BaseInformations[ItemID];
                if (infos == null) return;
                ushort X = Owner.X, Y = Owner.Y;
                Game.Map Map = Kernel.Maps[Owner.MapID];
                if (Map.SelectCoordonates(ref X, ref Y))
                {
                    Network.GamePackets.FloorItem floorItem = new Network.GamePackets.FloorItem(true);
                    floorItem.Item = new Network.GamePackets.ConquerItem(true);
                    floorItem.Item.Color = (Conquer_Online_Server.Game.Enums.Color)Kernel.Random.Next(4, 8);
                    floorItem.Item.ID = ItemID;
                    floorItem.Item.MaximDurability = infos.Durability;

                    floorItem.Item.Durability = (ushort)(Kernel.Random.Next(infos.Durability / 10));

                    if (!Network.PacketHandler.IsEquipment(ItemID) && infos.ConquerPointsWorth == 0)
                    {
                        floorItem.Item.StackSize = 1;
                        floorItem.Item.MaxStackSize = infos.StackSize;
                    }
                    floorItem.Item.MobDropped = true;
                    floorItem.ValueType = Network.GamePackets.FloorItem.FloorValueType.Item;
                    floorItem.ItemID = ItemID;
                    floorItem.MapID = Owner.MapID;
                    floorItem.MapObjType = Game.MapObjectType.Item;
                    floorItem.X = X;
                    floorItem.Y = Y;
                    floorItem.Owner = killer.Owner;
                    floorItem.Type = Network.GamePackets.FloorItem.Drop;
                    floorItem.OnFloor = Time32.Now;
                    floorItem.ItemColor = floorItem.Item.Color;
                    floorItem.UID = Network.GamePackets.FloorItem.FloorUID.Next;
                    while (Map.Npcs.ContainsKey(floorItem.UID))
                        floorItem.UID = Network.GamePackets.FloorItem.FloorUID.Next;
                    Map.AddFloorItem(floorItem);
                    SendScreenSpawn(floorItem);
                }
            }


            byte morepercent = 0;
            byte lessrate = 0;
            if (killer.VIPLevel > 0)
                morepercent = (byte)(killer.VIPLevel * 5);
            if (killer.Level <= 10 && killer.MapID == 1002)
                morepercent += 100;
            if (killer.VIPLevel != 6 && killer.Class >= 40 && killer.Class <= 45)
                lessrate = 3;
            if (killer.VIPLevel != 6 && killer.Level >= 132 && killer.ContainsFlag(Network.GamePackets.Update.Flags.ShurikenVortex))
                lessrate = 3;

            if (Kernel.Rate(Constants.MoneyDropRate - lessrate + morepercent))
            {

                uint amount = (uint)Kernel.Random.Next((int)MinMoneyDropAmount, (int)MaxMoneyDropAmount);
                amount *= Constants.MoneyDropMultiple;

                if (amount > 10)
                    amount = 60;

                if (amount == 0)
                    return;
                if (killer.VIPLevel > 0)
                {
                    int percent = 10;
                    percent += killer.VIPLevel * 5 - 5;
                    amount += (uint)(amount * percent / 100);
                }
                if (killer.VIPLevel > 4)
                {
                    killer.Money += amount;
                    goto next;
                }
                uint ItemID = Network.PacketHandler.MoneyItemID(amount);
                ushort X = Owner.X, Y = Owner.Y;
                Game.Map Map = Kernel.Maps[Owner.MapID];
                if (Map.SelectCoordonates(ref X, ref Y))
                {
                    Network.GamePackets.FloorItem floorItem = new Network.GamePackets.FloorItem(true);
                    floorItem.ValueType = Network.GamePackets.FloorItem.FloorValueType.Money;
                    floorItem.Value = amount;
                    floorItem.ItemID = ItemID;
                    floorItem.MapID = Owner.MapID;
                    floorItem.MapObjType = Game.MapObjectType.Item;
                    floorItem.X = X;
                    floorItem.Y = Y;
                    floorItem.Type = Network.GamePackets.FloorItem.Drop;
                    floorItem.OnFloor = Time32.Now;
                    floorItem.UID = Network.GamePackets.FloorItem.FloorUID.Next;
                    while (Map.Npcs.ContainsKey(floorItem.UID))
                        floorItem.UID = Network.GamePackets.FloorItem.FloorUID.Next;
                    Map.AddFloorItem(floorItem);
                    SendScreenSpawn(floorItem);
                }
            }
        next:

            if (Kernel.Rate(Constants.ConquerPointsDropRate - lessrate))
            {
                uint amount = (uint)Level * 6;//serverdrop
                if (amount <50) amount = 1;
                //if (amount > 200) amount = 3000;
                if (killer != null && killer.Owner != null)
                {
                    if (killer.Owner.Map.BaseID == 1354)
                    {
                        amount = 0;
                    }
                }
                if (killer != null && killer.Owner != null)
                {
                    //killer.Owner.Send(Constants.PickupConquerPoints(amount));
                    //killer.ConquerPoints += (uint)amount;
                   //killer.Owner.Send(new Network.GamePackets.Message("Você recebeu "+amount+" ConquerPoints! Por matar " + Name + "", System.Drawing.Color.Red, Network.GamePackets.Message.TopLeft).ToArray());
                    return;
                }
                //  }

                #region CPBag

                //uint ItemID = 729911;
                //ushort X = Owner.X, Y = Owner.Y;
                //Game.Map Map = Kernel.Maps[Owner.MapID];
                //if (Map.SelectCoordonates(ref X, ref Y))
                //{
                    //Network.GamePackets.FloorItem floorItem = new Network.GamePackets.FloorItem(true);
                    //floorItem.ValueType = Network.GamePackets.FloorItem.FloorValueType.ConquerPoints;
                    //floorItem.Value = amount;
                    //floorItem.ItemID = ItemID;
                    //floorItem.MapID = Owner.MapID;
                    //floorItem.MapObjType = Game.MapObjectType.Item;
                    //floorItem.X = X;
                    //floorItem.Y = Y;
                    //floorItem.Type = Network.GamePackets.FloorItem.Drop;
                    //floorItem.OnFloor = Time32.Now;
                    //floorItem.UID = Network.GamePackets.FloorItem.FloorUID.Next;
                    //while (Map.Npcs.ContainsKey(floorItem.UID))
                    //floorItem.UID = Network.GamePackets.FloorItem.FloorUID.Next;
                    //Map.AddFloorItem(floorItem);
                   // SendScreenSpawn(floorItem);
                //}
                #endregion
            }
            else if (Kernel.Rate(OwnItemRate + morepercent) && OwnItemID != 0)
            {
                if (killer.VIPLevel > 4)
                {
                    if (killer.Owner.Inventory.Count <= 39)
                    {
                        killer.Owner.Inventory.Add((uint)OwnItemID, 0, 1);
                        return;
                    }
                }
                var infos = Database.ConquerItemInformation.BaseInformations[(uint)OwnItemID];
                ushort X = Owner.X, Y = Owner.Y;
                Game.Map Map = Kernel.Maps[Owner.MapID];
                if (Map.SelectCoordonates(ref X, ref Y))
                {
                    Network.GamePackets.FloorItem floorItem = new Network.GamePackets.FloorItem(true);
                    floorItem.Item = new Network.GamePackets.ConquerItem(true);
                    floorItem.Item.Color = (Conquer_Online_Server.Game.Enums.Color)Kernel.Random.Next(4, 8);
                    floorItem.Item.ID = (uint)OwnItemID;
                    floorItem.Item.MaximDurability = infos.Durability;
                    if (!Network.PacketHandler.IsEquipment(OwnItemID) && infos.ConquerPointsWorth == 0)
                    {
                        floorItem.Item.StackSize = 1;
                        floorItem.Item.MaxStackSize = infos.StackSize;
                    }
                    floorItem.Item.MobDropped = true;
                    floorItem.ValueType = Network.GamePackets.FloorItem.FloorValueType.Item;
                    floorItem.ItemID = (uint)OwnItemID;
                    floorItem.MapID = Owner.MapID;
                    floorItem.MapObjType = Game.MapObjectType.Item;
                    floorItem.X = X;
                    floorItem.Y = Y;
                    floorItem.Owner = killer.Owner;
                    floorItem.Type = Network.GamePackets.FloorItem.Drop;
                    floorItem.OnFloor = Time32.Now;
                    floorItem.ItemColor = floorItem.Item.Color;
                    floorItem.UID = Network.GamePackets.FloorItem.FloorUID.Next;
                    while (Map.Npcs.ContainsKey(floorItem.UID))
                        floorItem.UID = Network.GamePackets.FloorItem.FloorUID.Next;
                    Map.AddFloorItem(floorItem);
                    SendScreenSpawn(floorItem);
                }
            }
            else if (Kernel.Rate(Constants.ItemDropRate + morepercent))
            {
                int quality = 3;
                for (int count = 0; count < 5; count++)
                {
                    int rate = int.Parse(Constants.ItemDropQualityRates[count]);
                    if (Kernel.Rate(rate, 1000))
                    {
                        quality = count + 5;
                        break;
                    }
                }
                int times = 50;
                byte lvl = Owner.Level;
                if (LabirinthDrop)
                    lvl = 20;
                List<uint> itemdroplist = ItemDropCache[lvl];
                if (Boss)
                    itemdroplist = SoulItemCache[lvl];
                if (itemdroplist != null)
                {
                retry:
                    times--;
                    int generateItemId = Kernel.Random.Next(itemdroplist.Count);
                    uint id = itemdroplist[generateItemId];
                    uint idb = id;
                    if (!Boss)
                    {
                        if (Database.ConquerItemInformation.BaseInformations[id].Level > 121 && times > 0)
                            goto retry;
                        id = (id / 10) * 10 + (uint)quality;
                    }
                    if (!Database.ConquerItemInformation.BaseInformations.ContainsKey(id))
                        id = idb;
                    
                    if (killer.VIPLevel < 0)
                    {
                        if (killer.Owner.Inventory.Count <= 39)
                        {
                            if (id % 10 > 7)
                            {
                                killer.Owner.Inventory.Add(id, 0, 1);
                                return;
                            }
                        }
                    }
                    var infos = Database.ConquerItemInformation.BaseInformations[id];
                    ushort X = Owner.X, Y = Owner.Y;
                    Game.Map Map = Kernel.Maps[Owner.MapID];
                    if (Map.SelectCoordonates(ref X, ref Y))
                    {
                        Network.GamePackets.FloorItem floorItem = new Network.GamePackets.FloorItem(true);
                        floorItem.Item = new Network.GamePackets.ConquerItem(true);
                        floorItem.Item.Color = (Conquer_Online_Server.Game.Enums.Color)Kernel.Random.Next(4, 8);
                        floorItem.Item.ID = id;
                        floorItem.Item.MaximDurability = infos.Durability;
                        if (quality >= 6)
                            floorItem.Item.Durability = (ushort)(infos.Durability - Kernel.Random.Next(500));
                        else
                            floorItem.Item.Durability = (ushort)(Kernel.Random.Next(infos.Durability / 10));
                        if (!Network.PacketHandler.IsEquipment(id) && infos.ConquerPointsWorth == 0)
                        {
                            floorItem.Item.StackSize = 1;
                            floorItem.Item.MaxStackSize = infos.StackSize;
                        }
                        floorItem.Item.MobDropped = true;
                        floorItem.ValueType = Network.GamePackets.FloorItem.FloorValueType.Item;
                        floorItem.ItemID = id;
                        floorItem.MapID = Owner.MapID;
                        floorItem.MapObjType = Game.MapObjectType.Item;
                        floorItem.X = X;
                        floorItem.Y = Y;
                        floorItem.Type = Network.GamePackets.FloorItem.Drop;
                        floorItem.OnFloor = Time32.Now;
                        floorItem.Owner = killer.Owner;
                        floorItem.ItemColor = floorItem.Item.Color;
                        floorItem.UID = Network.GamePackets.FloorItem.FloorUID.Next;
                        while (Map.Npcs.ContainsKey(floorItem.UID))
                            floorItem.UID = Network.GamePackets.FloorItem.FloorUID.Next;
                        Map.AddFloorItem(floorItem);
                        SendScreenSpawn(floorItem);
                    }
                }
            }
            else if (Kernel.Rate(20 + morepercent))
            {
                if (HPPotionID == 0)
                    return;
                var infos = Database.ConquerItemInformation.BaseInformations[(uint)HPPotionID];
                ushort X = Owner.X, Y = Owner.Y;
                Game.Map Map = Kernel.Maps[Owner.MapID];
                if (Map.SelectCoordonates(ref X, ref Y))
                {
                    Network.GamePackets.FloorItem floorItem = new Network.GamePackets.FloorItem(true);
                    floorItem.Item = new Network.GamePackets.ConquerItem(true);
                    floorItem.Item.Color = (Conquer_Online_Server.Game.Enums.Color)Kernel.Random.Next(4, 8);
                    floorItem.Item.ID = (uint)HPPotionID;
                    floorItem.Item.MobDropped = true;
                    floorItem.Item.MaximDurability = infos.Durability;
                    floorItem.ValueType = Network.GamePackets.FloorItem.FloorValueType.Item;
                    if (!Network.PacketHandler.IsEquipment(HPPotionID))
                    {
                        floorItem.Item.StackSize = 1;
                        floorItem.Item.MaxStackSize = infos.StackSize;
                    }
                    floorItem.ItemID = (uint)HPPotionID;
                    floorItem.MapID = Owner.MapID;
                    floorItem.MapObjType = Game.MapObjectType.Item;
                    floorItem.X = X;
                    floorItem.Y = Y;
                    floorItem.Type = Network.GamePackets.FloorItem.Drop;
                    floorItem.OnFloor = Time32.Now;
                    floorItem.Owner = killer.Owner;
                    floorItem.ItemColor = floorItem.Item.Color;
                    floorItem.UID = Network.GamePackets.FloorItem.FloorUID.Next;
                    while (Map.Npcs.ContainsKey(floorItem.UID))
                        floorItem.UID = Network.GamePackets.FloorItem.FloorUID.Next;
                    Map.AddFloorItem(floorItem);
                    SendScreenSpawn(floorItem);
                }
            }
            else if (Kernel.Rate(20 + morepercent))
            {
                if (MPPotionID == 0)
                    return;
                var infos = Database.ConquerItemInformation.BaseInformations[(uint)MPPotionID];
                ushort X = Owner.X, Y = Owner.Y;
                Game.Map Map = Kernel.Maps[Owner.MapID];
                if (Map.SelectCoordonates(ref X, ref Y))
                {
                    Network.GamePackets.FloorItem floorItem = new Network.GamePackets.FloorItem(true);
                    floorItem.Item = new Network.GamePackets.ConquerItem(true);
                    floorItem.Item.Color = (Conquer_Online_Server.Game.Enums.Color)Kernel.Random.Next(4, 8);
                    floorItem.Item.ID = (uint)MPPotionID;
                    floorItem.Item.MaximDurability = infos.Durability;
                    floorItem.Item.MobDropped = true;
                    floorItem.ValueType = Network.GamePackets.FloorItem.FloorValueType.Item;
                    if (!Network.PacketHandler.IsEquipment(MPPotionID))
                    {
                        floorItem.Item.StackSize = 1;
                        floorItem.Item.MaxStackSize = infos.StackSize;
                    }
                    floorItem.ItemID = (uint)MPPotionID;
                    floorItem.MapID = Owner.MapID;
                    floorItem.MapObjType = Game.MapObjectType.Item;
                    floorItem.X = X;
                    floorItem.Y = Y;
                    floorItem.Type = Network.GamePackets.FloorItem.Drop;
                    floorItem.OnFloor = Time32.Now;
                    floorItem.Owner = killer.Owner;
                    floorItem.ItemColor = floorItem.Item.Color;
                    floorItem.UID = Network.GamePackets.FloorItem.FloorUID.Next;
                    while (Map.Npcs.ContainsKey(floorItem.UID))
                        floorItem.UID = Network.GamePackets.FloorItem.FloorUID.Next;
                    Map.AddFloorItem(floorItem);
                    SendScreenSpawn(floorItem);
                }
            }
            else
            {
                foreach (SpecialItemDrop sitem in SpecialItemDropList)
                {
                    if (sitem.Map != 0 && Owner.MapID != sitem.Map)
                        continue;
                    if (Kernel.Rate(sitem.Rate + morepercent, sitem.Discriminant))
                    {
                        if (killer.VIPLevel < 0)
                        {
                            if (killer.Owner.Inventory.Count <= 39)
                            {
                                killer.Owner.Inventory.Add((uint)sitem.ItemID, 0, 1);
                                return;
                            }
                        }
                        var infos = Database.ConquerItemInformation.BaseInformations[(uint)sitem.ItemID];
                        ushort X = Owner.X, Y = Owner.Y;
                        Game.Map Map = Kernel.Maps[Owner.MapID];
                        if (Map.SelectCoordonates(ref X, ref Y))
                        {
                            Network.GamePackets.FloorItem floorItem = new Network.GamePackets.FloorItem(true);
                            floorItem.Item = new Network.GamePackets.ConquerItem(true);
                            floorItem.Item.Color = (Conquer_Online_Server.Game.Enums.Color)Kernel.Random.Next(4, 8);
                            floorItem.Item.ID = (uint)sitem.ItemID;
                            floorItem.Item.MaximDurability = infos.Durability;
                            floorItem.Item.MobDropped = true;
                            if (!Network.PacketHandler.IsEquipment(sitem.ItemID) && infos.ConquerPointsWorth == 0)
                            {
                                floorItem.Item.StackSize = 1;
                                floorItem.Item.MaxStackSize = infos.StackSize;
                            }
                            floorItem.ValueType = Network.GamePackets.FloorItem.FloorValueType.Item;
                            floorItem.ItemID = (uint)sitem.ItemID;
                            floorItem.MapID = Owner.MapID;
                            floorItem.MapObjType = Game.MapObjectType.Item;
                            floorItem.X = X;
                            floorItem.Y = Y;
                            floorItem.Type = Network.GamePackets.FloorItem.Drop;
                            floorItem.OnFloor = Time32.Now;
                            floorItem.ItemColor = floorItem.Item.Color;
                            floorItem.Owner = killer.Owner;
                            floorItem.UID = Network.GamePackets.FloorItem.FloorUID.Next;
                            while (Map.Npcs.ContainsKey(floorItem.UID))
                                floorItem.UID = Network.GamePackets.FloorItem.FloorUID.Next;
                            Map.AddFloorItem(floorItem);
                            SendScreenSpawn(floorItem);
                            break;
                        }
                    }
                }
            }
        }

        public const int ReviverID = 9879;

        public static SafeDictionary<uint, MonsterInformation> MonsterInformations = new SafeDictionary<uint, MonsterInformation>(8000);

        public static void Load()
        {
            using(var cmd = new MySqlCommand(MySqlCommandType.SELECT).Select("specialdrops"))
            using (var rdr = cmd.CreateReader())
            {
                while (rdr.Read())
                {
                    SpecialItemDrop sitem = new SpecialItemDrop();
                    sitem.ItemID = rdr.ReadInt32("itemid");
                    sitem.Rate = rdr.ReadInt32("rate");
                    sitem.Discriminant = rdr.ReadInt32("discriminant");
                    sitem.Map = rdr.ReadInt32("map");
                    SpecialItemDropList.Add(sitem);
                }
            }
            using (var command = new MySqlCommand(MySqlCommandType.SELECT))
            {
                command.Select("monsterinfos");
                using (var reader = command.CreateReader())
                {
                    while (reader.Read())
                    {
                        MonsterInformation mf = new MonsterInformation();
                        mf.ID = reader.ReadUInt32("id");
                        mf.Name = reader.ReadString("name");
                        mf.Type = reader.ReadUInt32("type");
                        mf.Mesh = reader.ReadUInt16("lookface");
                        mf.Level = reader.ReadByte("level");
                        mf.Hitpoints = reader.ReadUInt32("life");
                        IniFile IniFile = new IniFile(Constants.MonstersPath);
                        if (IniFile.ReadString(mf.Name, "MaxLife") != "")
                        {
                            if (uint.Parse(IniFile.ReadString(mf.Name, "MaxLife")) != 0)
                            {
                                mf.Hitpoints = uint.Parse(IniFile.ReadString(mf.Name, "MaxLife"));
                                byte boss = byte.Parse(IniFile.ReadString(mf.Name, "Boss"));
                                if (boss == 0)
                                    mf.Boss = false;
                                else mf.Boss = true;
                            }
                        }
                        mf.Guard = mf.Name.Contains("Guard");
                        mf.Reviver = mf.ID == ReviverID;
                        mf.ViewRange = reader.ReadUInt16("view_range");
                        mf.AttackRange = reader.ReadUInt16("attack_range");
                        mf.Defence = reader.ReadUInt16("defence");
                        mf.AttackType = reader.ReadByte("attack_user");
                        mf.MinAttack = reader.ReadUInt32("attack_min");
                        mf.MaxAttack = reader.ReadUInt32("attack_max");
                        mf.SpellID = reader.ReadUInt16("magic_type");
                        mf.MoveSpeed = reader.ReadInt32("move_speed");
                        mf.RunSpeed = reader.ReadInt32("run_speed");
                        mf.OwnItemID = reader.ReadInt32("ownitem");
                        mf.HPPotionID = reader.ReadInt32("drop_hp");
                        mf.MPPotionID = reader.ReadInt32("drop_mp");
                        mf.OwnItemRate = reader.ReadInt32("ownitemrate");
                        mf.AttackSpeed = reader.ReadInt32("attack_speed");
                        mf.ExtraExperience = reader.ReadUInt32("extra_exp");
                        uint MoneyDropAmount = reader.ReadUInt16("level");
                        if (MoneyDropAmount != 0)
                        {
                            mf.MaxMoneyDropAmount = MoneyDropAmount * 25;
                            if (mf.MaxMoneyDropAmount != 0)
                                mf.MinMoneyDropAmount = 1;
                        }
                        if (mf.MoveSpeed <= 500)
                            mf.MoveSpeed += 500;
                        if (mf.AttackSpeed <= 500)
                            mf.AttackSpeed += 500;
                        MonsterInformations.Add(mf.ID, mf);
                        byte lvl = mf.Level;
                        if (mf.Name == "Slinger" ||
                            mf.Name == "GoldGhost" ||
                            mf.Name == "AgileRat" ||
                            mf.Name == "Bladeling" ||
                            mf.Name == "BlueBird" ||
                            mf.Name == "BlueFiend" ||
                            mf.Name == "MinotaurL120")
                        {
                            mf.LabirinthDrop = true;
                            lvl = 20;
                        }
                        if (!ItemDropCache.ContainsKey(lvl))
                        {
                            List<uint> itemdroplist = new List<uint>();
                            foreach (ConquerItemBaseInformation itemInfo in ConquerItemInformation.BaseInformations.Values)
                            {

                                if (itemInfo.ID >= 800000 && itemInfo.ID <= 824014)
                                    continue;
                                ushort position = Network.PacketHandler.ItemPosition(itemInfo.ID);
                                if (Network.PacketHandler.IsArrow(itemInfo.ID) || itemInfo.Level == 0 || itemInfo.Level > 121)
                                    continue;
                                if (position < 9 && position != 7)
                                {
                                    if (itemInfo.Level == 100)
                                        if (itemInfo.Name.Contains("Dress"))
                                            continue;
                                    if (itemInfo.Level > 121)
                                        continue;
                                    int diff = (int)lvl - (int)itemInfo.Level;
                                    if (!(diff > 10 || diff < -10))
                                    {
                                        itemdroplist.Add(itemInfo.ID);
                                    }
                                }
                                if (position == 10 || position == 11 && lvl >= 70)
                                    itemdroplist.Add(itemInfo.ID);
                            }
                            ItemDropCache.Add(lvl, itemdroplist);
                        }
                        if (mf.Boss)
                        {
                            List<uint> itemdroplist = new List<uint>();
                            foreach (ConquerItemBaseInformation itemInfo in ConquerItemInformation.BaseInformations.Values)
                            {
                                if (itemInfo.ID >= 800000 && itemInfo.ID <= 824014)
                                {
                                    if (itemInfo.PurificationLevel <= 3)
                                    {
                                        int diff = (int)mf.Level - (int)itemInfo.Level;
                                        if (!(diff > 10 || diff < -10))
                                        {
                                            itemdroplist.Add(itemInfo.ID);
                                        }
                                    }
                                }
                            }
                            SoulItemCache.Add(lvl, itemdroplist);
                        }
                    }
                }
            }
            Console.WriteLine("Monster information loaded.");
            Console.WriteLine("Monster drops generated.");
        }

        public MonsterInformation Copy()
        {
            MonsterInformation mf = new MonsterInformation();
            mf.ID = this.ID;
            mf.Name = this.Name;
            mf.Mesh = this.Mesh;
            mf.Level = this.Level;
            mf.Hitpoints = this.Hitpoints;
            mf.ViewRange = this.ViewRange;
            mf.AttackRange = this.AttackRange;
            mf.AttackType = this.AttackType;
            mf.MinAttack = this.MinAttack;
            mf.MaxAttack = this.MaxAttack;
            mf.SpellID = this.SpellID;
            mf.MoveSpeed = this.MoveSpeed;
            mf.RunSpeed = this.RunSpeed;
            mf.AttackSpeed = this.AttackSpeed;
            mf.BoundX = this.BoundX;
            mf.BoundY = this.BoundY;
            mf.BoundCX = this.BoundCX;
            mf.BoundCY = this.BoundCY;
            mf.RespawnTime = this.RespawnTime;
            mf.ExtraExperience = this.ExtraExperience;
            mf.MaxMoneyDropAmount = this.MaxMoneyDropAmount;
            mf.MinMoneyDropAmount = this.MinMoneyDropAmount;
            mf.OwnItemID = this.OwnItemID;
            mf.HPPotionID = this.HPPotionID;
            mf.MPPotionID = this.MPPotionID;
            mf.OwnItemRate = this.OwnItemRate;
            mf.LabirinthDrop = this.LabirinthDrop;
            mf.Boss = this.Boss;
            mf.Guard = this.Guard;
            mf.Defence = this.Defence;
            mf.Reviver = this.Reviver;
            return mf;
        }
    }
}
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                         