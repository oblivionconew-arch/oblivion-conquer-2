using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Conquer_Online_Server.GamePackets;

namespace Conquer_Online_Server.Database
{
    class JiangTable
    {

        public static void InsertJi(Conquer_Online_Server.Client.GameClient Client)
        {
            MySqlCommand cmds = new MySqlCommand(MySqlCommandType.INSERT);
            cmds.Insert("jianghu").Insert("EntityID", Client.Entity.UID).Execute();

        }
        public static void LoadJi()
        {
            MySqlCommand cmd = new MySqlCommand(MySqlCommandType.SELECT);
            cmd.Select("jianghu");
            MySqlReader r = new MySqlReader(cmd);
            if (r.Read())
            {

                JiangHu ji = new JiangHu();
                ji.CustomizedName = r.ReadString("Name");
                ji.UID = r.ReadUInt32("EntityID");
                r.Close();
                Console.WriteLine("Jiang Table Loaded");
            }
        }
        public static void Save(Client.GameClient Client)
        {
            MySqlCommand cmd = new MySqlCommand(MySqlCommandType.UPDATE);
            cmd.Update("jianghu")
                .Set("Name", Client.Entity.MyJiang.CustomizedName)
                .Where("EntityID", Client.Entity.UID).Execute();
        }
    }
}
