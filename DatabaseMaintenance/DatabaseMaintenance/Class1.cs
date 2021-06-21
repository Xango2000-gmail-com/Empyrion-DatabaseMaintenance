using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eleon;
using Eleon.Modding;
using System.Data.SQLite;

namespace DatabaseMaintenance
{
    public class MyEmpyrionMod : IMod, ModInterface
    {
        internal static string ModShortName = "DatabaseMaintenance";
        public static string ModVersion = ModShortName + " v0.0.1 made by Xango2000 (3140)";
        public static string ModPath = "..\\Content\\Mods\\" + ModShortName + "\\";
        internal static IModApi modApi;
        internal static bool debug = false;
        internal static Dictionary<int, Storage.StorableData> SeqNrStorage = new Dictionary<int, Storage.StorableData> { };
        public int thisSeqNr = 8000;
        internal static SetupYaml.Root SetupYamlData = new SetupYaml.Root { };
        internal static ApplicationMode AppMode;
        internal static string BootupTimestamp = "Timestamp";

        //for the Delayed Database Inserts
        int Delay = 0;
        List<string> dbCache = new List<string> { };
        bool Run = false;

        static void CreateDB()
        {
            string SavePath = modApi.Application.GetPathFor(AppFolder.SaveGame);
            Uri newURI = new Uri(SavePath + "\\global2.db");
            string cs = "Data Source=" + newURI.ToString().Remove(0, 8);
            string stm = "SELECT SQLITE_VERSION()";
            if (!File.Exists(newURI.ToString().Remove(0, 8)))
            {
                SQLiteConnection.CreateFile(newURI.ToString().Remove(0, 8));
                using (var con = new SQLiteConnection(cs))
                {
                    con.Open();
                    using (var cmd = new SQLiteCommand(stm, con))
                    {
                        string version = cmd.ExecuteScalar().ToString();
                        cmd.CommandText = @"CREATE TABLE ChatLog(
                        key INTEGER PRIMARY KEY AUTOINCREMENT,
                        senderID INT, 
                        senderType TEXT,
                        channel TEXT,
                        recipientID INT,
                        message TEXT,
                        DateTimeStamp TEXT,
                        UnixTimestamp INT
                        )";
                        cmd.ExecuteNonQuery();

                        /*
                        cmd.CommandText = @"CREATE TABLE players(
                        playerID INTEGER PRIMARY KEY,
                        steamID TEXT,
                        factionID INT,
                        UnixTimestamp INT
                        )";
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = @"CREATE TABLE logOnOff(
                        key INTEGER PRIMARY KEY AUTOINCREMENT,
                        playerID INT,
                        status TEXT,
                        UnixTimestamp INT
                        )";
                        cmd.ExecuteNonQuery();
                        */

                        cmd.CommandText = @"CREATE TABLE factions(
                        factionID INTEGER PRIMARY KEY,
                        name TEXT,
                        abbr TEXT,
                        origin INT,
                        UnixTimestamp INT
                        )";
                        cmd.ExecuteNonQuery();

                        /*
                        cmd.CommandText = @"CREATE TABLE factionChangeLog(
                        key INTEGER PRIMARY KEY AUTOINCREMENT,
                        factionID INT,
                        entityID INT,
                        factionGroup BYTE,
                        UnixTimestamp INT
                        )";
                        cmd.ExecuteNonQuery();
                        */
                        con.Close();
                    }
                }
            }
        }

        public void Init(IModApi modAPI)
        {
            modApi = modAPI;
            BootupTimestamp = CommonFunctions.TimeStampFilename();
            CreateDB();
            string SavePath = modAPI.Application.GetPathFor(AppFolder.SaveGame);
            //CommonFunctions.LogFile("Test.txt", "SavePath = "+ SavePath);
            if (modApi.Application.Mode == ApplicationMode.DedicatedServer)
            {
                AppMode = modApi.Application.Mode;
                SetupYaml.Setup();
                if (SetupYamlData.Enabled)
                {
                    foreach (string Command in SetupYamlData.OnStartup)
                    {
                        DB.Remove(Command);
                        CommonFunctions.Log(Command + "   ...Complete", AppMode.ToString());
                    }
                    CommonFunctions.Log("All OnStartup Commands Complete", AppMode.ToString());
                }
                modApi.Application.ChatMessageSent += Application_ChatMessageSent;
            }
            else if (modApi.Application.Mode == ApplicationMode.PlayfieldServer)
            {
                //modApi.Application.OnPlayfieldLoaded += Application_OnPlayfieldLoaded;
            }
            //IEnumerable<int> Players = modApi.Application.GetPlayerEntityIds();
            //modApi.Application.GetStructure(1069, StructureData);
        }

        private void StructureData(GlobalStructureInfo Structure)
        {
            
        }

        private void Application_OnPlayfieldLoaded(IPlayfield playfield)
        {
            
        }

        private void Application_ChatMessageSent(MessageData chatMsgData)
        {
            if (modApi.Application.Mode == ApplicationMode.DedicatedServer && chatMsgData.Text == SetupYamlData.Trigger && SetupYamlData.Enabled)
            {
                foreach (string Command in SetupYamlData.OnTrigger)
                {
                    DB.Remove(Command);
                    CommonFunctions.Log(Command + "   ...Complete", AppMode.ToString());
                }
                CommonFunctions.Log("All OnTrigger Commands Complete", AppMode.ToString());
            }
            else if (modApi.Application.Mode == ApplicationMode.DedicatedServer && chatMsgData.Text == "/db reinit")
            {
                SetupYaml.Setup();
            }
            else if (modApi.Application.Mode == ApplicationMode.DedicatedServer && (chatMsgData.Text == "/mods" || chatMsgData.Text == "!mods"))
            {
                API.ServerTell(chatMsgData.SenderEntityId, ModShortName, ModVersion, true);
            }

            int recipient = 0;
            if (chatMsgData.RecipientEntityId != -1)
            {
                recipient = chatMsgData.RecipientEntityId;
            }
            string Sanitized = "";
            try
            {
                Sanitized = CommonFunctions.Sanitize(chatMsgData.Text);
            }
            catch
            {
                CommonFunctions.Log("Sanitizer Failed (" + chatMsgData.Text + " )");
                Sanitized = chatMsgData.Text;
            }
            string Timestamp = CommonFunctions.Sanitize(CommonFunctions.TimeStamp());
            string newInsertable = "INSERT INTO ChatLog(senderID, senderType, channel, recipientID, message, DateTimeStamp, UnixTimestamp) VALUES("
                        + chatMsgData.SenderEntityId + ", "
                        + "'" + chatMsgData.SenderType + "', "
                        + "'" + chatMsgData.Channel + "', "
                        + recipient + ", "
                        + "'" + Sanitized + "', "
                        + "'" + Timestamp + "', "
                        + (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds
                        + ")";
            CommonFunctions.Log(newInsertable);
            dbCache.Add(newInsertable);
            Run = true;
        }

        public void Shutdown()
        {
            if (modApi.Application.Mode == ApplicationMode.DedicatedServer && SetupYamlData.Enabled)
            {
                foreach (string Command in SetupYamlData.OnShutdown)
                {
                    DB.Remove(Command);
                    CommonFunctions.Log(Command + "   ...Complete", AppMode.ToString());
                }
                CommonFunctions.Log("All OnShutdown Commands Complete", AppMode.ToString());
            }
        }

        public void Game_Start(ModGameAPI dediAPI)
        {
        }

        public void Game_Update()
        {
            if(Delay > 10 && Run)
            {
                DBInsert();
                Delay = 0;
            }
            Delay++;
        }

        public void Game_Exit()
        {
        }

        public async Task DBInsert()
        {
            List<string> BlankList = new List<string> { };
            List<string> OnDeck = dbCache;
            Run = false;
            dbCache = BlankList;
            await DBInsert(OnDeck);
        }

        public async Task DBInsert(List<string> Inputable)
        {
            string SavePath = modApi.Application.GetPathFor(AppFolder.SaveGame);
            Uri newURI = new Uri(SavePath + "\\global2.db");
            string cs = "Data Source=" + newURI.ToString().Remove(0, 8);
            using (var con = new SQLiteConnection(cs))
            {
                string stm = "SELECT SQLITE_VERSION()";
                con.Open();
                using (var cmd = new SQLiteCommand(stm, con))
                {
                    foreach (string line in Inputable)
                    {
                        cmd.CommandText = line;
                        cmd.ExecuteNonQuery();
                        CommonFunctions.Log(line);
                    }
                    con.Close();
                }
            }
        }

        public void Game_Event(CmdId cmdId, ushort seqNr, object data)
        {
            try
            {
                switch (cmdId)
                {
                    case CmdId.Event_ChatMessage:
                        //Triggered when player says something in-game
                        ChatInfo Received_ChatInfo = (ChatInfo)data;
                        break;


                    case CmdId.Event_Player_Connected:
                        //Triggered when a player logs on
                        Id Received_PlayerConnected = (Id)data;
                        /*
                        string PlayerLoggingOn = "INSERT INTO logOnOff(playerID, status, UnixTimestamp) VALUES("
                        + Received_PlayerConnected.id + ", "
                        + "'ON', "
                        + (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds
                        + ")";
                        CommonFunctions.Log(PlayerLoggingOn);
                        dbCache.Add(PlayerLoggingOn);
                        Run = true;
                        */
                        break;


                    case CmdId.Event_Player_Disconnected:
                        //Triggered when a player logs off
                        Id Received_PlayerDisconnected = (Id)data;
                        /*
                        string PlayerLoggingOff = "INSERT INTO logOnOff(playerID, status, UnixTimestamp) VALUES("
                        + Received_PlayerDisconnected.id + ", "
                        + "'OFF', "
                        + (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds
                        + ")";
                        CommonFunctions.Log(PlayerLoggingOff);
                        dbCache.Add(PlayerLoggingOff);
                        Run = true;
                        */
                        break;


                    case CmdId.Event_Player_ChangedPlayfield:
                        //Triggered when a player changes playfield
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_ChangePlayfield, (ushort)CurrentSeqNr, new IdPlayfieldPositionRotation( [PlayerID], [Playfield Name], [PVector3 position], [PVector3 Rotation] ));
                        IdPlayfield Received_PlayerChangedPlayfield = (IdPlayfield)data;
                        break;


                    case CmdId.Event_Playfield_Loaded:
                        //Triggered when a player goes to a playfield that isnt currently loaded in memory
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Load_Playfield, (ushort)CurrentSeqNr, new PlayfieldLoad( [float nSecs], [string nPlayfield], [int nProcessId] ));
                        PlayfieldLoad Received_PlayfieldLoaded = (PlayfieldLoad)data;
                        break;


                    case CmdId.Event_Playfield_Unloaded:
                        //Triggered when there are no players left in a playfield
                        PlayfieldLoad Received_PlayfieldUnLoaded = (PlayfieldLoad)data;
                        break;


                    case CmdId.Event_Faction_Changed:
                        //Triggered when an Entity (player too?) changes faction
                        FactionChangeInfo Received_FactionChange = (FactionChangeInfo)data;
                        CommonFunctions.Log("Faction Change Event triggered");
                        /*
                        string newInsertable = "INSERT INTO factionChangeLog(factionID, entityID, factionGroup, UnixTimestamp) VALUES("
                        + Received_FactionChange.factionId + ", "
                        + Received_FactionChange.id + ", "
                        + Received_FactionChange.factionGroup + ", "
                        + (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds
                        + ")";
                        CommonFunctions.Log(newInsertable);
                        dbCache.Add(newInsertable);
                        Run = true;
                        */
                        break;


                    case CmdId.Event_Statistics:
                        //Triggered on various game events like: Player Death, Entity Power on/off, Remove/Add Core
                        StatisticsParam Received_EventStatistics = (StatisticsParam)data;
                        break;


                    case CmdId.Event_Player_DisconnectedWaiting:
                        //Triggered When a player is having trouble logging into the server
                        Id Received_PlayerDisconnectedWaiting = (Id)data;
                        break;


                    case CmdId.Event_TraderNPCItemSold:
                        //Triggered when a player buys an item from a trader
                        TraderNPCItemSoldInfo Received_TraderNPCItemSold = (TraderNPCItemSoldInfo)data;
                        break;


                    case CmdId.Event_Player_List:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_List, (ushort)CurrentSeqNr, null));
                        IdList Received_PlayerList = (IdList)data;
                        break;


                    case CmdId.Event_Player_Info:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_Info, (ushort)CurrentSeqNr, new Id( [playerID] ));
                        PlayerInfo Received_PlayerInfo = (PlayerInfo)data;
                        break;


                    case CmdId.Event_Player_Inventory:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_GetInventory, (ushort)CurrentSeqNr, new Id( [playerID] ));
                        Inventory Received_PlayerInventory = (Inventory)data;
                        break;


                    case CmdId.Event_Player_ItemExchange:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_ItemExchange, (ushort)CurrentSeqNr, new ItemExchangeInfo( [id], [title], [description], [buttontext], [ItemStack[]] ));
                        ItemExchangeInfo Received_ItemExchangeInfo = (ItemExchangeInfo)data;
                        break;


                    case CmdId.Event_DialogButtonIndex:
                        //All of This is a Guess
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_ShowDialog_SinglePlayer, (ushort)CurrentSeqNr, new IdMsgPrio( [int nId], [string nMsg], [byte nPrio], [float nTime] )); //for Prio: 0=Red, 1=Yellow, 2=Blue
                        //Save/Pos = 0, Close/Cancel/Neg = 1
                        IdAndIntValue Received_DialogButtonIndex = (IdAndIntValue)data;
                        break;


                    case CmdId.Event_Player_Credits:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_Credits, (ushort)CurrentSeqNr, new Id( [PlayerID] ));
                        IdCredits Received_PlayerCredits = (IdCredits)data;
                        break;


                    case CmdId.Event_Player_GetAndRemoveInventory:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_GetAndRemoveInventory, (ushort)CurrentSeqNr, new Id( [playerID] ));
                        Inventory Received_PlayerGetRemoveInventory = (Inventory)data;
                        break;


                    case CmdId.Event_Playfield_List:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Playfield_List, (ushort)CurrentSeqNr, null));
                        PlayfieldList Received_PlayfieldList = (PlayfieldList)data;
                        break;


                    case CmdId.Event_Playfield_Stats:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Playfield_Stats, (ushort)CurrentSeqNr, new PString( [Playfield Name] ));
                        PlayfieldStats Received_PlayfieldStats = (PlayfieldStats)data;
                        break;


                    case CmdId.Event_Playfield_Entity_List:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Playfield_Entity_List, (ushort)CurrentSeqNr, new PString( [Playfield Name] ));
                        PlayfieldEntityList Received_PlayfieldEntityList = (PlayfieldEntityList)data;
                        break;


                    case CmdId.Event_Dedi_Stats:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Dedi_Stats, (ushort)CurrentSeqNr, null));
                        DediStats Received_DediStats = (DediStats)data;
                        break;


                    case CmdId.Event_GlobalStructure_List:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_GlobalStructure_List, (ushort)CurrentSeqNr, null));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_GlobalStructure_Update, (ushort)CurrentSeqNr, new PString( [Playfield Name] ));
                        GlobalStructureList Received_GlobalStructureList = (GlobalStructureList)data;
                        break;


                    case CmdId.Event_Entity_PosAndRot:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_PosAndRot, (ushort)CurrentSeqNr, new Id( [EntityID] ));
                        IdPositionRotation Received_EntityPosRot = (IdPositionRotation)data;
                        break;


                    case CmdId.Event_Get_Factions:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Get_Factions, (ushort)CurrentSeqNr, new Id( [int] )); //Requests all factions from a certain Id onwards. If you want all factions use Id 1.
                        FactionInfoList Received_FactionInfoList = (FactionInfoList)data;
                        CommonFunctions.Log("Faction Data Received");
                        foreach (FactionInfo Faction in Received_FactionInfoList.factions)
                        {
                            string newFactionInfoInsertable = "INSERT INTO factions(factionID, name, abbr, origin, UnixTimestamp) VALUES("
                            + Faction.factionId + ", "
                            + "'" + Faction.name + "', "
                            + "'" + Faction.abbrev + "', "
                            + Faction.origin + ", "
                            + (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds
                            + ")";
                            CommonFunctions.Log(newFactionInfoInsertable);
                            dbCache.Add(newFactionInfoInsertable);
                        }
                        Run = true;
                        //***Received_FactionInfoList.factions
                        break;


                    case CmdId.Event_NewEntityId:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_NewEntityId, (ushort)CurrentSeqNr, null));
                        Id Request_NewEntityId = (Id)data;
                        break;


                    case CmdId.Event_Structure_BlockStatistics:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Structure_BlockStatistics, (ushort)CurrentSeqNr, new Id( [EntityID] ));
                        IdStructureBlockInfo Received_StructureBlockStatistics = (IdStructureBlockInfo)data;
                        break;


                    case CmdId.Event_AlliancesAll:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_AlliancesAll, (ushort)CurrentSeqNr, null));
                        AlliancesTable Received_AlliancesAll = (AlliancesTable)data;
                        break;


                    case CmdId.Event_AlliancesFaction:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_AlliancesFaction, (ushort)CurrentSeqNr, new AlliancesFaction( [int nFaction1Id], [int nFaction2Id], [bool nIsAllied] ));
                        AlliancesFaction Received_AlliancesFaction = (AlliancesFaction)data;
                        //***Received_AlliancesFaction.faction1Id
                        //***Received_AlliancesFaction.faction2Id
                        //***Received_AlliancesFaction.isAllied
                        break;


                    case CmdId.Event_BannedPlayers:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_GetBannedPlayers, (ushort)CurrentSeqNr, null ));
                        BannedPlayerData Received_BannedPlayers = (BannedPlayerData)data;
                        break;


                    case CmdId.Event_GameEvent:
                        //Triggered by PDA Events
                        GameEventData Received_GameEvent = (GameEventData)data;
                        break;


                    case CmdId.Event_Ok:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_SetInventory, (ushort)CurrentSeqNr, new Inventory(){ [changes to be made] });
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_AddItem, (ushort)CurrentSeqNr, new IdItemStack(){ [changes to be made] });
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_SetCredits, (ushort)CurrentSeqNr, new IdCredits( [PlayerID], [Double] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_AddCredits, (ushort)CurrentSeqNr, new IdCredits( [PlayerID], [+/- Double] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Blueprint_Finish, (ushort)CurrentSeqNr, new Id( [PlayerID] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Blueprint_Resources, (ushort)CurrentSeqNr, new BlueprintResources( [PlayerID], [List<ItemStack>], [bool ReplaceExisting?] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_Teleport, (ushort)CurrentSeqNr, new IdPositionRotation( [EntityId OR PlayerID], [Pvector3 Position], [Pvector3 Rotation] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_ChangePlayfield , (ushort)CurrentSeqNr, new IdPlayfieldPositionRotation( [EntityId OR PlayerID], [Playfield],  [Pvector3 Position], [Pvector3 Rotation] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_Destroy, (ushort)CurrentSeqNr, new Id( [EntityID] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_Destroy2, (ushort)CurrentSeqNr, new IdPlayfield( [EntityID], [Playfield] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_SetName, (ushort)CurrentSeqNr, new Id( [EntityID] )); Wait, what? This one doesn't make sense. This is what the Wiki says though.
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_Spawn, (ushort)CurrentSeqNr, new EntitySpawnInfo()); Doesn't make sense to me.
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Structure_Touch, (ushort)CurrentSeqNr, new Id( [EntityID] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_InGameMessage_SinglePlayer, (ushort)CurrentSeqNr, new IdMsgPrio( [int nId], [string nMsg], [byte nPrio], [float nTime] )); //for Prio: 0=Red, 1=Yellow, 2=Blue
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_InGameMessage_Faction, (ushort)CurrentSeqNr, new IdMsgPrio( [int nId], [string nMsg], [byte nPrio], [float nTime] )); //for Prio: 0=Red, 1=Yellow, 2=Blue
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_InGameMessage_AllPlayers, (ushort)CurrentSeqNr, new IdMsgPrio( [int nId], [string nMsg], [byte nPrio], [float nTime] )); //for Prio: 0=Red, 1=Yellow, 2=Blue
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_ConsoleCommand, (ushort)CurrentSeqNr, new PString( [Telnet Command] ));

                        //uh? Not Listed in Wiki... Received_ = ()data;
                        break;


                    case CmdId.Event_Error:
                        //Triggered when there is an error coming from the API
                        ErrorInfo Received_ErrorInfo = (ErrorInfo)data;
                        break;


                    case CmdId.Event_PdaStateChange:
                        //Triggered by PDA: chapter activated/deactivated/completed
                        PdaStateInfo Received_PdaStateChange = (PdaStateInfo)data;
                        break;


                    case CmdId.Event_ConsoleCommand:
                        //Triggered when a player uses a Console Command in-game
                        ConsoleCommandInfo Received_ConsoleCommandInfo = (ConsoleCommandInfo)data;
                        break;


                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                CommonFunctions.LogFile("ERROR.txt", "Message: " + ex.Message);
                CommonFunctions.LogFile("ERROR.txt", "Data: " + ex.Data);
                CommonFunctions.LogFile("ERROR.txt", "HelpLink: " + ex.HelpLink);
                CommonFunctions.LogFile("ERROR.txt", "InnerException: " + ex.InnerException);
                CommonFunctions.LogFile("ERROR.txt", "Source: " + ex.Source);
                CommonFunctions.LogFile("ERROR.txt", "StackTrace: " + ex.StackTrace);
                CommonFunctions.LogFile("ERROR.txt", "TargetSite: " + ex.TargetSite);
                CommonFunctions.LogFile("ERROR.txt", "");
            }

        }
    }
}
