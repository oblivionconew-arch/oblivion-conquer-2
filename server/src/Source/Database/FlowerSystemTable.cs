namespace Conquer_Online_Server.Database
{
    using Conquer_Online_Server.Game.Features.Flowers;
    using System;

    public class FlowerSystemTable
    {
        public static uint[] TopLilies = new uint[2];
        public static uint[] TopOrchids = new uint[2];
        public static uint[] TopRedRoses = new uint[2];
        public static uint[] TopTulips = new uint[2];

        private static bool Exists(uint id)
        {
            using (var cmd = new MySqlCommand(MySqlCommandType.SELECT).Select("flowers").Where("ID", id))
            using (var reader = new MySqlReader(cmd))
                return reader.Read();
        }

        public static void Flowers(Client.GameClient Client)
        {
            Client.Entity.Flowers = new Game.Features.Flowers.Flowers();
            if (!Exists(Client.Entity.UID))
                Insert(Client);
            using (var cmd = new MySqlCommand(MySqlCommandType.SELECT).Select("flowers").Where("ID", Client.Entity.UID))
            using (var reader = new MySqlReader(cmd))
            {
                if (reader.Read())
                {
                    Client.Entity.Flowers.id = Client.Entity.UID;
                    if (DateTime.TryParse(reader.ReadString("last_flower_sent"), out Client.Entity.Flowers._LastFlowerSent))
                    {
                        if ((Client.Entity.Body == 0x7d1) || (Client.Entity.Body == 0x7d2))
                        {
                            if (Client.Entity.Flowers.LastFlowerSent.AddDays(1) <= DateTime.Now)
                            {
                                Client.Entity.Flowers.LastFlowerSent = DateTime.Now;
                                Client.Entity.Flowers.RedRoses2day = 0;
                                Client.Entity.Flowers.Lilies2day = 0;
                                Client.Entity.Flowers.Tulips2day = 0;
                                Client.Entity.Flowers.Orchads2day = 0;
                            }
                            else
                            {
                                Client.Entity.Flowers.RedRoses2day = reader.ReadUInt32("Redrosestoday");
                                Client.Entity.Flowers.Lilies2day = reader.ReadUInt32("Liliestoday");
                                Client.Entity.Flowers.Tulips2day = reader.ReadUInt32("Tulipstoday");
                                Client.Entity.Flowers.Orchads2day = reader.ReadUInt32("Orchadstoday");
                            }
                            Client.Entity.Flowers.RedRoses = reader.ReadUInt32("Redroses");
                            Client.Entity.Flowers.Lilies = reader.ReadUInt32("Lilies");
                            Client.Entity.Flowers.Tulips = reader.ReadUInt32("Tulips");
                            Client.Entity.Flowers.Orchads = reader.ReadUInt32("Orchads");
                        }
                    }
                }
            }
        }

        public static void Insert(Client.GameClient client)
        {
            using(var cmd = new MySqlCommand(MySqlCommandType.INSERT).Insert("flowers"))
                cmd.Insert("id", client.Entity.UID).Insert("redroses", client.Entity.Flowers.RedRoses).Insert("redrosestoday", client.Entity.Flowers.RedRoses2day)
                    .Insert("lilies", client.Entity.Flowers.Lilies).Insert("liliestoday", client.Entity.Flowers.Lilies2day)
                    .Insert("tulips", client.Entity.Flowers.Tulips).Insert("tulipstoday", client.Entity.Flowers.Tulips2day)
                    .Insert("orchads", client.Entity.Flowers.Orchads).Insert("orchadstoday", client.Entity.Flowers.Orchads2day)
                    .Insert("last_flower_sent", DateTime.Now.AddDays(-1).ToString()).Execute();
        }

        public static void SaveFlowerTable(Client.GameClient client, MySql.Data.MySqlClient.MySqlConnection conn)
        {
            if(client.Entity.Flowers == null)
                client.Entity.Flowers = new Game.Features.Flowers.Flowers();
            using (var cmd = new MySqlCommand(MySqlCommandType.UPDATE).Update("flowers"))
                cmd.Set("redroses", client.Entity.Flowers.RedRoses).Set("redrosestoday", client.Entity.Flowers.RedRoses2day)
                    .Set("lilies", client.Entity.Flowers.Lilies).Set("liliestoday", client.Entity.Flowers.Lilies2day)
                    .Set("tulips", client.Entity.Flowers.Tulips).Set("tulipstoday", client.Entity.Flowers.Tulips2day)
                    .Set("orchads", client.Entity.Flowers.Orchads).Set("orchadstoday", client.Entity.Flowers.Orchads2day)
                    .Set("last_flower_sent", client.Entity.Flowers.LastFlowerSent.ToString()).Where("id", client.Entity.UID)
                    .Execute();
        }
    }
}

