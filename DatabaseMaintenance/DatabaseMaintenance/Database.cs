using System;
using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
using System.Data.SQLite;
//using System.Data.Common;
using Eleon.Modding;

namespace DatabaseMaintenance
{
    class DB
    {
        static SQLiteConnection sqliteCon = new SQLiteConnection(@"Data Source=..\\Saves\\Games\\" + MyEmpyrionMod.SetupYamlData.SaveGameName + "\\global.db");

        internal class EntityData
        {
            public string Name;
            public int EntityID;
            public int PosX;
            public int PosY;
            public int PosZ;
            public int RotX;
            public int RotY;
            public int RotZ;
            public int pfid;
            public int facgroup;
            public int facid;
            public int etype;
            public int Health;
            public int isStructure;
            public int isProxy;
            public int isPoi;
            public int isRemoved;
            public int BelongsTo;
            public int DockedTo;
        }

        internal class PlayfieldData
        {
            public int pfid;
            public string Name;
            public string pfType;
            public int mapType;
            public int size;
            public int pvp;
        }

        public static void Query(string CommandText)
        {
            //for DB.Read("SELECT entityid, name FROM Entities WHERE pfid = 8 and fid = 100 ");
            sqliteCon.Open();
            SQLiteCommand selectCommand = new SQLiteCommand(CommandText, sqliteCon);
            SQLiteDataReader dataReader = selectCommand.ExecuteReader();
            Dictionary<string, string> QueryData = new Dictionary<string, string> { };
            while (dataReader.Read())
            {
                CommonFunctions.Log("EmpyrionID: " + dataReader.GetFieldValue<Int64>(0) + "  EntityName= " + dataReader.GetFieldValue<string>(1));
            }
            dataReader.Close();
            sqliteCon.Close();
        }

        public static EntityData LookupPlayer(int EmpyrionID)
        {
            sqliteCon.Open();
            string CommandText = "SELECT * FROM Entities WHERE entityid = " + EmpyrionID + " AND etype = 1";
            SQLiteCommand selectCommand = new SQLiteCommand(CommandText, sqliteCon);
            SQLiteDataReader dataReader = selectCommand.ExecuteReader();
            EntityData QueryData = new EntityData() { };
            while (dataReader.Read())
            {
                QueryData.Name = Convert.ToString(dataReader.GetValue(2));
                QueryData.EntityID = Convert.ToInt32(dataReader.GetValue(0));
                QueryData.facid = Convert.ToInt32(dataReader.GetValue(5));
                QueryData.facgroup = Convert.ToInt32(dataReader.GetValue(4));
                QueryData.pfid = Convert.ToInt32(dataReader.GetValue(1));
                QueryData.PosX = Convert.ToInt32(dataReader.GetValue(6));
                QueryData.PosY = Convert.ToInt32(dataReader.GetValue(7));
                QueryData.PosZ = Convert.ToInt32(dataReader.GetValue(8));
                QueryData.RotX = Convert.ToInt32(dataReader.GetValue(9));
                QueryData.RotY = Convert.ToInt32(dataReader.GetValue(10));
                QueryData.RotZ = Convert.ToInt32(dataReader.GetValue(11));
                QueryData.Health = Convert.ToInt32(dataReader.GetValue(12));
                /*
                for (int i = 0; i <= dataReader.FieldCount; i++)
                {
                    CommonFunctions.Log(dataReader.GetName(i) + " = " + dataReader.GetValue(i));
                }
                */
            }
            dataReader.Close();
            sqliteCon.Close();
            return QueryData;
        }

        public static EntityData LookupEntity(int EntityID)
        {
            sqliteCon.Open();
            string CommandText = "SELECT * FROM Entities WHERE entityid = " + EntityID;
            SQLiteCommand selectCommand = new SQLiteCommand(CommandText, sqliteCon);
            SQLiteDataReader dataReader = selectCommand.ExecuteReader();
            EntityData QueryData = new EntityData() { };
            while (dataReader.Read())
            {
                QueryData.EntityID = Convert.ToInt32(dataReader.GetValue(0));
                QueryData.pfid = Convert.ToInt32(dataReader.GetValue(1));
                QueryData.Name = Convert.ToString(dataReader.GetValue(2));
                QueryData.etype = Convert.ToInt32(dataReader.GetValue(3));
                QueryData.facgroup = Convert.ToInt32(dataReader.GetValue(4));
                QueryData.facid = Convert.ToInt32(dataReader.GetValue(5));
                QueryData.PosX = Convert.ToInt32(dataReader.GetValue(6));
                QueryData.PosY = Convert.ToInt32(dataReader.GetValue(7));
                QueryData.PosZ = Convert.ToInt32(dataReader.GetValue(8));
                QueryData.RotX = Convert.ToInt32(dataReader.GetValue(9));
                QueryData.RotY = Convert.ToInt32(dataReader.GetValue(10));
                QueryData.RotZ = Convert.ToInt32(dataReader.GetValue(11));
                QueryData.Health = Convert.ToInt32(dataReader.GetValue(12));
                QueryData.isStructure = Convert.ToInt32(dataReader.GetValue(13));
                QueryData.isRemoved = Convert.ToInt32(dataReader.GetValue(14));
                QueryData.isProxy = Convert.ToInt32(dataReader.GetValue(16));
                QueryData.isPoi = Convert.ToInt32(dataReader.GetValue(17));
                try
                {
                    QueryData.BelongsTo = Convert.ToInt32(dataReader.GetValue(18));

                }
                catch
                {
                    QueryData.BelongsTo = 0;
                }
                try
                {
                    QueryData.DockedTo = Convert.ToInt32(dataReader.GetValue(22));
                }
                catch
                {
                    QueryData.DockedTo = 0;
                }
            }
            dataReader.Close();
            sqliteCon.Close();
            return QueryData;
        }

        public static ItemStack[] StructureDeviceCounts(int EntityID)
        {
            List<ItemStack> DeviceCounts = new List<ItemStack> { };
            sqliteCon.Open();
            string CommandText = "SELECT * FROM Entities WHERE entityid = " + EntityID;
            SQLiteCommand selectCommand = new SQLiteCommand(CommandText, sqliteCon);
            SQLiteDataReader dataReader = selectCommand.ExecuteReader();
            EntityData QueryData = new EntityData() { };
            while (dataReader.Read())
            {
                ItemStack DeviceCount = new ItemStack
                {
                    id = Convert.ToInt32(dataReader.GetFieldValue<Int64>(0)),
                    count = Convert.ToInt32(dataReader.GetFieldValue<Int64>(1)),
                    ammo = 0,
                    slotIdx = 0,
                    decay = 0
                };
                DeviceCounts.Add(DeviceCount);
                //dataReader.GetFieldValue<Int64>(0) + dataReader.GetFieldValue<Int64>(1));
            }
            ItemStack[] ItemStackArray = DeviceCounts.ToArray();
            return ItemStackArray;
        }

        public static List<int> Docked(int EntityID)
        {
            sqliteCon.Open();
            string CommandText = "SELECT entityid FROM Entities WHERE dockedto = " + EntityID;
            SQLiteCommand selectCommand = new SQLiteCommand(CommandText, sqliteCon);
            SQLiteDataReader dataReader = selectCommand.ExecuteReader();
            List<int> QueryData = new List<int> { };
            while (dataReader.Read())
            {
                try
                {
                    QueryData.Add(Convert.ToInt32(dataReader.GetFieldValue<Int64>(0)));
                }
                catch { };
            }
            dataReader.Close();
            sqliteCon.Close();
            return QueryData;
        }

        public static List<EntityData> Motorcycle()
        {
            sqliteCon.Open();
            string CommandText = "SELECT * FROM Entities WHERE name = 'PlayerBike' AND isremoved = 0";
            SQLiteCommand selectCommand = new SQLiteCommand(CommandText, sqliteCon);
            SQLiteDataReader dataReader = selectCommand.ExecuteReader();
            List<EntityData> ReturnData = new List<EntityData> { };
            while (dataReader.Read())
            {
                try
                {
                    EntityData QueryData = new EntityData { };
                    QueryData.EntityID = Convert.ToInt32(dataReader.GetValue(0));
                    QueryData.pfid = Convert.ToInt32(dataReader.GetValue(1));
                    QueryData.Name = Convert.ToString(dataReader.GetValue(2));
                    QueryData.etype = Convert.ToInt32(dataReader.GetValue(3));
                    QueryData.facgroup = Convert.ToInt32(dataReader.GetValue(4));
                    QueryData.facid = Convert.ToInt32(dataReader.GetValue(5));
                    QueryData.PosX = Convert.ToInt32(dataReader.GetValue(6));
                    QueryData.PosY = Convert.ToInt32(dataReader.GetValue(7));
                    QueryData.PosZ = Convert.ToInt32(dataReader.GetValue(8));
                    QueryData.RotX = Convert.ToInt32(dataReader.GetValue(9));
                    QueryData.RotY = Convert.ToInt32(dataReader.GetValue(10));
                    QueryData.RotZ = Convert.ToInt32(dataReader.GetValue(11));
                    QueryData.Health = Convert.ToInt32(dataReader.GetValue(12));
                    QueryData.isStructure = Convert.ToInt32(dataReader.GetValue(13));
                    QueryData.isRemoved = Convert.ToInt32(dataReader.GetValue(14));
                    QueryData.isProxy = Convert.ToInt32(dataReader.GetValue(16));
                    QueryData.isPoi = Convert.ToInt32(dataReader.GetValue(17));
                    QueryData.BelongsTo = Convert.ToInt32(dataReader.GetValue(18));
                    ReturnData.Add(QueryData);
                }
                catch { };
            }
            dataReader.Close();
            sqliteCon.Close();
            return ReturnData;
        }

        public static PlayfieldData LookupPlayfield(int pfID)
        {
            sqliteCon.Open();
            string CommandText = "SELECT * FROM Playfields WHERE pfid = " + pfID;
            SQLiteCommand selectCommand = new SQLiteCommand(CommandText, sqliteCon);
            SQLiteDataReader dataReader = selectCommand.ExecuteReader();
            PlayfieldData ReturnData = new PlayfieldData { };
            while (dataReader.Read())
            {
                try
                {
                    ReturnData.pfid = Convert.ToInt32(dataReader.GetValue(1));
                    ReturnData.pfType = Convert.ToString(dataReader.GetValue(3));
                    ReturnData.Name = Convert.ToString(dataReader.GetValue(4));
                    ReturnData.size = Convert.ToInt32(dataReader.GetValue(5));
                    ReturnData.mapType = Convert.ToInt32(dataReader.GetValue(6));
                    ReturnData.pvp = Convert.ToInt32(dataReader.GetValue(9));
                }
                catch { };
            }
            dataReader.Close();
            sqliteCon.Close();
            return ReturnData;
        }

        public static int LookupPlayfieldID(string PlayfieldName)
        {
            sqliteCon.Open();
            string CommandText = "SELECT pfid FROM Playfields WHERE name = \'" + PlayfieldName + "\'";
            SQLiteCommand selectCommand = new SQLiteCommand(CommandText, sqliteCon);
            SQLiteDataReader dataReader = selectCommand.ExecuteReader();
            int pfID = 0;
            while (dataReader.Read())
            {
                try
                {
                    pfID = Convert.ToInt32(dataReader.GetValue(0));
                }
                catch { };
            }
            dataReader.Close();
            sqliteCon.Close();

            return pfID;
        }

        public static bool Remove(string Command)
        {
            sqliteCon.Open();
            SQLiteCommand selectCommand = new SQLiteCommand(Command, sqliteCon);
            SQLiteDataReader dataReader = selectCommand.ExecuteReader();
            dataReader.Close();
            sqliteCon.Close();
            return true;
        }
    }
}
