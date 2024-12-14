using hololive_oficial_cardgame_server.SerializableObjects;
using MySql.Data.MySqlClient;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace hololive_oficial_cardgame_server
{

    public class DBConnection
    {
        public bool DebugVatiable = true;

        private string connectionString = "Server=localhost;Database=hololive-official-cardgame;User ID=root;Password=;Pooling=true;";
        private object dataTable;

        public PlayerRequest CreateAccount()
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                PlayerRequest _CreateAccount = new PlayerRequest();
                connection.Open();
                string hash = QuickHash();
                long lastInsertId = 0;

                // Use a transaction to ensure consistency
                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Insert statement
                        string insertQuery = "INSERT INTO `player` (`PlayerID`, `PlayerName`, `PlayerIcon`, `HoloCoins`, `HoloGold`, `NNMaterial`, `RRMaterial`, `SRMaterial`, `URMaterial`, `MatchVictory`, `MatchLoses`, `MatchesTotal`, `Email`, `Password`, `AccountLink`, `RegDate`, `LoginPassword`) " +
                                                "VALUES (NULL, '', '', '', '', '', '', '', '', '', '', '', '', '', '', NOW(), @hash);";

                        using (MySqlCommand insertCommand = new MySqlCommand(insertQuery, connection, transaction))
                        {
                            insertCommand.Parameters.AddWithValue("@hash", hash);
                            insertCommand.ExecuteNonQuery();
                        }

                        // Retrieve the last inserted ID
                        using (MySqlCommand idCommand = new MySqlCommand("SELECT LAST_INSERT_ID();", connection, transaction))
                        {
                            object result = idCommand.ExecuteScalar();
                            lastInsertId = Convert.ToInt64(result);
                        }

                        transaction.Commit();
                        _CreateAccount.password = hash;
                        _CreateAccount.playerID = lastInsertId.ToString();
                        return _CreateAccount;
                    }
                    catch (Exception ex)
                    {
                        Lib.WriteConsoleMessage("\nError: " + ex.Message);
                        // Rollback transaction in case of error
                        transaction.Rollback();
                        return null;
                    }
                }
            }
        }

        public PlayerInfo GetPlayerInfo(string playerid, string playerpassword)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                List<PlayerInfo> playerInfoList = new List<PlayerInfo>();

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string insertQuery = "SELECT * FROM player WHERE PlayerID=@playerid AND Password=@playerpassword LIMIT 1;";

                        using (MySqlCommand insertCommand = new MySqlCommand(insertQuery, connection, transaction))
                        {
                            insertCommand.Parameters.AddWithValue("@playerid", playerid);
                            insertCommand.Parameters.AddWithValue("@playerpassword", playerpassword);

                            var result = insertCommand.ExecuteReader();
                            var dataTable = new DataTable();
                            dataTable.Load(result);

                            foreach (DataRow row in dataTable.Rows)
                            {
                                var playerInfo = new PlayerInfo
                                {
                                    PlayerID = row.Field<int>("PlayerID").ToString(),
                                    PlayerName = row.Field<string>("PlayerName"),
                                    PlayerIcon = row.Field<int>("PlayerIcon"),
                                    HoloCoins = row.Field<int>("HoloCoins"),
                                    HoloGold = row.Field<int>("HoloGold"),
                                    NNMaterial = row.Field<int>("NNMaterial"),
                                    RRMaterial = row.Field<int>("RRMaterial"),
                                    SRMaterial = row.Field<int>("SRMaterial"),
                                    URMaterial = row.Field<int>("URMaterial"),
                                    MatchVictory = row.Field<int>("MatchVictory"),
                                    MatchLoses = row.Field<int>("MatchLoses"),
                                    MatchesTotal = row.Field<int>("MatchesTotal"),
                                    Email = row.Field<string>("Email"),
                                    Password = row.Field<string>("Password")
                                };

                                playerInfoList.Add(playerInfo);
                            }

                        }

                        transaction.Commit();

                        return playerInfoList[0] == null ? null : playerInfoList[0];
                    }
                    catch (Exception ex)
                    {
                        Lib.WriteConsoleMessage("\nError: " + ex.Message);
                        transaction.Rollback();
                        return null;
                    }
                }
            }
        }
        public PlayerInfo LoginSession(string id, string password)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                List<PlayerInfo> playerInfoList = new List<PlayerInfo>();

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string insertQuery = "SELECT * FROM player WHERE PlayerId=@id AND Password=@password LIMIT 1;";

                        using (MySqlCommand insertCommand = new MySqlCommand(insertQuery, connection, transaction))
                        {
                            insertCommand.Parameters.AddWithValue("@id", id);
                            insertCommand.Parameters.AddWithValue("@password", password);

                            var result = insertCommand.ExecuteReader();
                            var dataTable = new DataTable();
                            dataTable.Load(result);

                            foreach (DataRow row in dataTable.Rows)
                            {
                                var playerInfo = new PlayerInfo
                                {
                                    PlayerID = row.Field<int>("PlayerID").ToString(),
                                    Password = row.Field<string>("Password")
                                };

                                playerInfoList.Add(playerInfo);
                            }

                        }

                        transaction.Commit();

                        return playerInfoList[0] == null ? null : playerInfoList[0];
                    }
                    catch (Exception ex)
                    {
                        Lib.WriteConsoleMessage("\nError: " + ex.Message);
                        transaction.Rollback();
                        return null;
                    }
                }
            }
        }

        public PlayerInfo LoginAccount(string email, string password, string id = "")
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                List<PlayerInfo> playerInfoList = new List<PlayerInfo>();

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string insertQuery = "";
                        if (string.IsNullOrEmpty(email))
                            insertQuery = "SELECT * FROM player WHERE PlayerId=@playerid AND LoginPassword=@password LIMIT 1;";
                        else
                            insertQuery = "SELECT * FROM player WHERE Email=@email AND LoginPassword=@password LIMIT 1;";

                        using (MySqlCommand insertCommand = new MySqlCommand(insertQuery, connection, transaction))
                        {
                            insertCommand.Parameters.AddWithValue("@email", email);
                            insertCommand.Parameters.AddWithValue("@playerid", id);
                            insertCommand.Parameters.AddWithValue("@password", password);

                            var result = insertCommand.ExecuteReader();
                            var dataTable = new DataTable();
                            dataTable.Load(result);

                            foreach (DataRow row in dataTable.Rows)
                            {
                                var playerInfo = new PlayerInfo
                                {
                                    PlayerID = row.Field<int>("PlayerID").ToString(),
                                    Password = row.Field<string>("Password")
                                };

                                playerInfoList.Add(playerInfo);
                            }

                        }

                        transaction.Commit();

                        return playerInfoList[0] == null ? null : playerInfoList[0];
                    }
                    catch (Exception ex)
                    {
                        Lib.WriteConsoleMessage("\nError: " + ex.Message);
                        transaction.Rollback();
                        return null;
                    }
                }
            }
        }
        public bool UpdateSessionPassword(string id, string sessionPassword)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string insertQuery = "UPDATE player SET Password=@password WHERE PlayerID=@playerid LIMIT 1;";

                        using (MySqlCommand insertCommand = new MySqlCommand(insertQuery, connection, transaction))
                        {
                            insertCommand.Parameters.AddWithValue("@playerid", id);
                            insertCommand.Parameters.AddWithValue("@password", sessionPassword);

                            insertCommand.ExecuteNonQuery();

                            transaction.Commit();
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Lib.WriteConsoleMessage("\nError: " + ex.Message);
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }

        public bool UpdatePlayerName(PlayerRequest values)//(List<Dictionary<string, string>> values)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string insertQuery = $"UPDATE player SET PlayerName=@playername WHERE PlayerID=@playerid AND Password=@playerpassword LIMIT 1;";

                        using (MySqlCommand insertCommand = new MySqlCommand(insertQuery, connection, transaction))
                        {
                            insertCommand.Parameters.AddWithValue("@playerpassword", values.password);
                            insertCommand.Parameters.AddWithValue("@playername", values.requestObject);
                            insertCommand.Parameters.AddWithValue("@playerid", values.playerID);
                            insertCommand.ExecuteNonQuery();

                            transaction.Commit();
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Lib.WriteConsoleMessage("\nError: " + ex.Message);
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }

        public bool UpdatePlayerProfilePicture(PlayerRequest values)//(List<Dictionary<string, string>> values)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string insertQuery = $"UPDATE player SET PlayerIcon=@playericon WHERE PlayerID=@playerid AND Password=@playerpassword LIMIT 1;";


                        using (MySqlCommand insertCommand = new MySqlCommand(insertQuery, connection, transaction))
                        {
                            insertCommand.Parameters.AddWithValue("@playerpassword", values.playerID);
                            insertCommand.Parameters.AddWithValue("@PlayerIcon", values.requestObject);
                            insertCommand.Parameters.AddWithValue("@playerid", values.playerID);
                            insertCommand.ExecuteNonQuery();

                            transaction.Commit();

                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Lib.WriteConsoleMessage("\nError: " + ex.Message);
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }

        public List<PlayerBadge> GetPlayerBadgesV2(string playerid)//(List<Dictionary<string, string>> values)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string query = "SELECT * FROM playerbadgelist WHERE MONTH(ObtainedDate) = MONTH(CURRENT_DATE) AND PlayerID=@playerid ORDER BY ObtainedDate DESC LIMIT 1;";

                        using (MySqlCommand command = new MySqlCommand(query, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@playerid", playerid);
                            var reader = command.ExecuteReader();
                            var dataTable = new DataTable();
                            dataTable.Load(reader);
                            if (dataTable.Rows.Count == 0)
                            {
                                string insertQuery = "INSERT INTO `playerbadgelist` (`BadgeID`, `Rank`, `ObtainedDate`, `PlayerID`) VALUES (NULL, '0', (CURRENT_DATE), @playerid);";
                                using (MySqlCommand insertCommand = new MySqlCommand(insertQuery, connection, transaction))
                                {
                                    insertCommand.Parameters.AddWithValue("@playerid", playerid);
                                    insertCommand.ExecuteNonQuery();
                                }

                            }
                        }

                        List<PlayerBadge> BadgeList = new List<PlayerBadge>();
                        query = "SELECT * FROM playerbadgelist WHERE  PlayerID=@playerid AND ObtainedDate BETWEEN DATE_SUB(CURRENT_DATE, INTERVAL 5 MONTH) AND CURRENT_DATE ORDER BY ObtainedDate DESC LIMIT 5;";

                        using (MySqlCommand command = new MySqlCommand(query, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@playerid", playerid);
                            var result = command.ExecuteReader();
                            var dataTable = new DataTable();
                            dataTable.Load(result);
                            PrintDataTable(dataTable);

                            foreach (DataRow row in dataTable.Rows)
                            {
                                BadgeList.Add(MapPlayerBadges(row));
                            }
                        }

                        transaction.Commit();

                        return BadgeList;
                    }
                    catch (Exception ex)
                    {
                        Lib.WriteConsoleMessage("\nError: " + ex.Message);
                        transaction.Rollback();
                        return null;
                    }
                }
            }
        }


        public bool JoinMatchQueue(PlayerRequest _PlayerRequest)
        {
            string PlayerIDSQL = "( SELECT PlayerID FROM `hololive-official-cardgame`.`player` WHERE PlayerID = @playerid AND Password =@password )";
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        using (MySqlCommand idCommand = new MySqlCommand("SELECT PlayerID, Status FROM `hololive-official-cardgame`.`matchpool` WHERE PlayerID = " + PlayerIDSQL + " AND (Status = 'A' OR Status = 'D') UNION SELECT  PlayerID, Status FROM `hololive-official-cardgame`.`matchroompool` WHERE PlayerID = " + PlayerIDSQL + " AND Status != 'I';", connection, transaction))
                        {
                            idCommand.Parameters.AddWithValue("@playerid", _PlayerRequest.playerID);
                            idCommand.Parameters.AddWithValue("@password", _PlayerRequest.password);
                            var dataTable = new DataTable();
                            Lib.WriteConsoleMessage(GetQueryWithParameters(idCommand));
                            dataTable.Load(idCommand.ExecuteReader());

                            foreach (DataRow r in dataTable.Rows)
                            {
                                if (r[1].Equals("D"))
                                {
                                    throw new Exception("Player already in a match");
                                }
                            }

                            if (dataTable.Rows.Count > 0)
                            {
                                using (MySqlCommand updateCommand = new MySqlCommand("DELETE FROM matchpool WHERE playerID=( SELECT PlayerID FROM `hololive-official-cardgame`.`player` WHERE PlayerID =@playerid AND Password =@password ) AND Status='A'; UPDATE matchroompool SET Status='A', Board = '0', Chair='0' WHERE playerID=( SELECT PlayerID FROM `hololive-official-cardgame`.`player` WHERE PlayerID =@playerid AND Password =@password ) AND Status != 'I' ", connection, transaction))
                                {
                                    updateCommand.Parameters.AddWithValue("@playerid", _PlayerRequest.playerID);
                                    updateCommand.Parameters.AddWithValue("@password", _PlayerRequest.password);
                                    Lib.WriteConsoleMessage(GetQueryWithParameters(updateCommand));
                                    updateCommand.ExecuteNonQuery();
                                }
                            }
                        }

                        string insertQuery = "INSERT INTO `matchpool` (`MatchPoolID`, `PlayerID`, `RegDate`, `Type`, `Status`, `Code`) VALUES (UUID(), @playerid, NOW(), @type, 'A', @code);";

                        using (MySqlCommand insertCommand = new MySqlCommand(insertQuery, connection, transaction))
                        {

                            insertCommand.Parameters.AddWithValue("@playerid", _PlayerRequest.playerID);
                            insertCommand.Parameters.AddWithValue("@type", _PlayerRequest.description);
                            insertCommand.Parameters.AddWithValue("@code", string.IsNullOrEmpty(_PlayerRequest.requestObject) ? "" : _PlayerRequest.requestObject);
                            //insertCommand.Parameters.AddWithValue("@uuidv7", GenerateUuidV7());
                            Lib.WriteConsoleMessage(GetQueryWithParameters(insertCommand));
                            insertCommand.ExecuteNonQuery();
                        }

                        transaction.Commit();

                        return true;
                    }
                    catch (Exception ex)
                    {
                        Lib.WriteConsoleMessage("\nError: " + ex.Message);
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }

        public bool CancelMatchQueue(PlayerRequest _PlayerRequest)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        using (MySqlCommand updateCommand = new MySqlCommand("UPDATE matchpool SET Status='I' WHERE playerID=( SELECT PlayerID FROM `hololive-official-cardgame`.`player` WHERE PlayerID =@playerid AND Password =@password ) AND Status='A' ", connection, transaction))
                        {
                            updateCommand.Parameters.AddWithValue("@playerid", _PlayerRequest.playerID);
                            updateCommand.Parameters.AddWithValue("@password", _PlayerRequest.password);
                            updateCommand.ExecuteNonQuery();
                        }
                        transaction.Commit();

                        return true;
                    }
                    catch (Exception ex)
                    {
                        Lib.WriteConsoleMessage("\nError: " + ex.Message);
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }


        public PlayerMatchRoom CreateMatchRoomQueue(PlayerRequest _PlayerRequest)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        //check if player has a room
                        var dataT = new DataTable();
                        using (MySqlCommand idCommand = new MySqlCommand("SELECT * FROM `hololive-official-cardgame`.`matchroom` WHERE OwnerID = ( SELECT PlayerID FROM `hololive-official-cardgame`.`player` WHERE PlayerID =@playerid AND Password =@password );", connection, transaction))
                        {
                            idCommand.Parameters.AddWithValue("@playerid", _PlayerRequest.playerID);
                            idCommand.Parameters.AddWithValue("@password", _PlayerRequest.password);

                            dataT.Load(idCommand.ExecuteReader());
                        }
                        Random random = new Random();
                        int code = random.Next(100000, 1000000);
                        string uuidv7 = "UUID()"; // GenerateUuidV7();
                        // if not has a room, check if player is active in another queue
                        if (dataT.Rows.Count == 0)
                        {

                            using (MySqlCommand idCommand = new MySqlCommand("SELECT PlayerID, Status FROM `hololive-official-cardgame`.`matchpool` WHERE PlayerID = ( SELECT PlayerID FROM `hololive-official-cardgame`.`player` WHERE PlayerID =@playerid AND Password =@password ) AND Status = 'A' UNION SELECT  PlayerID, Status  FROM `hololive-official-cardgame`.`matchroompool` WHERE PlayerID = ( SELECT PlayerID FROM `hololive-official-cardgame`.`player` WHERE PlayerID = @playerid AND Password = @password ) AND Status = 'D';", connection, transaction))
                            {
                                idCommand.Parameters.AddWithValue("@playerid", _PlayerRequest.playerID);
                                idCommand.Parameters.AddWithValue("@password", _PlayerRequest.password);
                                var dataTable = new DataTable();
                                Lib.WriteConsoleMessage(GetQueryWithParameters(idCommand));
                                dataTable.Load(idCommand.ExecuteReader());
                                // if is active in another queue, remove from all the queues
                                if (dataTable.Rows.Count > 0)
                                {
                                    using (MySqlCommand updateCommand = new MySqlCommand("DELETE FROM matchpool WHERE playerID=( SELECT PlayerID FROM `hololive-official-cardgame`.`player` WHERE PlayerID =@playerid AND Password =@password ) AND Status='A' LIMIT 2; UPDATE matchroompool SET Status='R' WHERE PlayerID = ( SELECT PlayerID FROM `hololive-official-cardgame`.`player` WHERE PlayerID = @playerid AND Password = @password ) AND Status='D' LIMIT 2", connection, transaction))
                                    {
                                        updateCommand.Parameters.AddWithValue("@playerid", _PlayerRequest.playerID);
                                        updateCommand.Parameters.AddWithValue("@password", _PlayerRequest.password);
                                        Lib.WriteConsoleMessage(GetQueryWithParameters(updateCommand));
                                        updateCommand.ExecuteNonQuery();
                                    }
                                }
                            }
                            //create the room, and inser in the room queue
                            string insertQuery = "INSERT INTO `matchroom` (`RoomID`, `RegDate`, `RoomCode`, `MaxPlayer`, `OwnerID`) VALUES (@roomid, current_timestamp(), @roomcode, '50', @playerid); INSERT INTO `matchroompool` (`MRPID`, `PlayerID`, `Board`, `Status`, `MatchRoomID`, `RegDate`, `LastActionDate`) VALUES ((UUID(), @playerid, '0', 'A', UUID(), current_timestamp(), current_timestamp());";

                            using (MySqlCommand insertCommand = new MySqlCommand(insertQuery, connection, transaction))
                            {
                                //insertCommand.Parameters.AddWithValue("@mrpid", GenerateUuidV7());
                                //insertCommand.Parameters.AddWithValue("@roomid", uuidv7);
                                insertCommand.Parameters.AddWithValue("@roomcode", code);
                                insertCommand.Parameters.AddWithValue("@playerid", _PlayerRequest.playerID);
                                insertCommand.ExecuteNonQuery();
                            }

                        }
                        //since has a room, get the room data
                        PlayerMatchRoom _PlayerMatchRoom = new PlayerMatchRoom();

                        using (MySqlCommand selectCommand = new MySqlCommand("SELECT * FROM `hololive-official-cardgame`.`matchroom` WHERE OwnerID = ( SELECT PlayerID FROM `hololive-official-cardgame`.`player` WHERE PlayerID =@playerid AND Password =@password )", connection, transaction))
                        {
                            selectCommand.Parameters.AddWithValue("@playerid", _PlayerRequest.playerID);
                            selectCommand.Parameters.AddWithValue("@password", _PlayerRequest.password);
                            var dataTable = new DataTable();
                            dataTable.Load(selectCommand.ExecuteReader());

                            foreach (DataRow row in dataTable.Rows)
                            {
                                var pmr = new PlayerMatchRoom
                                {
                                    RoomID = row.Field<string>("RoomID"),
                                    RegDate = row.Field<DateTime>("RegDate"),
                                    RoomCode = row.Field<int>("RoomCode"),
                                    MaxPlayer = row.Field<int>("MaxPlayer"),
                                    OwnerID = row.Field<int>("OwnerID")
                                };
                                _PlayerMatchRoom = pmr;
                            }
                        }

                        List<PlayerMatchRoomPool> _PlayerMatchRoomPool = new List<PlayerMatchRoomPool>();

                        if (dataT.Rows.Count != 0)
                            uuidv7 = dataT.Rows[0].Field<string>("RoomID");

                        using (MySqlCommand selectCommand = new MySqlCommand("SELECT * FROM `hololive-official-cardgame`.`matchroompool` WHERE MatchRoomID = @roomid", connection, transaction))
                        {
                            selectCommand.Parameters.AddWithValue("@roomid", uuidv7);
                            var dataTable = new DataTable();
                            dataTable.Load(selectCommand.ExecuteReader());
                            Lib.WriteConsoleMessage(GetQueryWithParameters(selectCommand));

                            foreach (DataRow row in dataTable.Rows)
                            {
                                var pmr = new PlayerMatchRoomPool
                                {
                                    MRPID = row.Field<string>("MRPID"),
                                    PlayerID = row.Field<int>("PlayerID"),
                                    Board = row.Field<int>("Board"),
                                    Status = row.Field<string>("Status"),
                                    MatchRoomID = row.Field<string>("MatchRoomID"),
                                    RegDate = row.Field<DateTime>("RegDate"),
                                    LasActionDate = row.Field<DateTime>("LastActionDate")
                                };

                                _PlayerMatchRoomPool.Add(pmr);
                            }
                        }

                        _PlayerMatchRoom.PlayerMatchRoomPool = _PlayerMatchRoomPool;

                        transaction.Commit();

                        return _PlayerMatchRoom;
                    }
                    catch (Exception ex)
                    {
                        Lib.WriteConsoleMessage("\nError: " + ex.Message);
                        transaction.Rollback();
                        return null;
                    }
                }
            }
        }

        public PlayerMatchRoom JoinMatchRoomQueue(PlayerRequest _PlayerRequest)
        {
            string PlayerIDSQL = "( SELECT PlayerID FROM `hololive-official-cardgame`.`player` WHERE PlayerID = @playerid AND Password =@password )";
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        //check if the room exist
                        var dataT = new DataTable();
                        using (MySqlCommand idCommand = new MySqlCommand("SELECT * FROM `hololive-official-cardgame`.`matchroom` WHERE RoomCode = @roomcode AND OwnerID != 0;", connection, transaction))
                        {
                            idCommand.Parameters.AddWithValue("@roomcode", _PlayerRequest.description);

                            dataT.Load(idCommand.ExecuteReader());
                        }

                        if (dataT.Rows.Count == 0)
                        {
                            return null;
                        }
                        //check if player inst already in the room
                        var pollsData = new DataTable();
                        using (MySqlCommand poolDataCommand = new MySqlCommand("SELECT * FROM `hololive-official-cardgame`.`matchroompool` WHERE PlayerID = @playerid AND Status != 'I' ;", connection, transaction))
                        {
                            poolDataCommand.Parameters.AddWithValue("@playerid", _PlayerRequest.playerID);

                            pollsData.Load(poolDataCommand.ExecuteReader());
                        }

                        if (pollsData.Rows.Count == 0)
                        {
                            //the room exist, then remove player from all other queues
                            using (MySqlCommand updateCommand = new MySqlCommand("UPDATE matchpool SET Status='I' WHERE playerID = " + PlayerIDSQL + " AND Status='A'; UPDATE matchroompool SET Status='I' WHERE playerID = " + PlayerIDSQL + "  AND Status='R'", connection, transaction))
                            {
                                updateCommand.Parameters.AddWithValue("@playerid", _PlayerRequest.playerID);
                                updateCommand.Parameters.AddWithValue("@password", _PlayerRequest.password);
                                updateCommand.ExecuteNonQuery();
                            }

                            //Insert user into the room
                            string insertQuery = "INSERT INTO `matchroompool` (`MRPID`, `PlayerID`, `Board`, `Status`, `MatchRoomID`, `RegDate`, `LastActionDate`) VALUES ((UUID(), @playerid, '0', 'A', @roomid, current_timestamp(), current_timestamp());";
                            using (MySqlCommand insertCommand = new MySqlCommand(insertQuery, connection, transaction))
                            {
                                //insertCommand.Parameters.AddWithValue("@mrpid", GenerateUuidV7());
                                insertCommand.Parameters.AddWithValue("@playerid", _PlayerRequest.playerID);
                                insertCommand.Parameters.AddWithValue("@roomid", dataT.Rows[0].Field<string>("RoomID"));
                                insertCommand.ExecuteNonQuery();
                            }
                        }

                        //since user it's a room, get the room data
                        PlayerMatchRoom _PlayerMatchRoom = new PlayerMatchRoom();

                        foreach (DataRow row in dataT.Rows)
                        {
                            var pmr = new PlayerMatchRoom
                            {
                                RoomID = row.Field<string>("RoomID"),
                                RegDate = row.Field<DateTime>("RegDate"),
                                RoomCode = row.Field<int>("RoomCode"),
                                MaxPlayer = row.Field<int>("MaxPlayer"),
                                OwnerID = row.Field<int>("OwnerID")
                            };
                            _PlayerMatchRoom = pmr;
                        }

                        List<PlayerMatchRoomPool> _PlayerMatchRoomPool = new List<PlayerMatchRoomPool>();

                        using (MySqlCommand selectCommand = new MySqlCommand("SELECT * FROM `hololive-official-cardgame`.`matchroompool` WHERE MatchRoomID = @roomid AND Status='A'", connection, transaction))
                        {
                            var dataTable = new DataTable();
                            selectCommand.Parameters.AddWithValue("@roomid", dataT.Rows[0].Field<string>("RoomID"));
                            dataTable.Load(selectCommand.ExecuteReader());

                            foreach (DataRow row in dataTable.Rows)
                            {
                                var pmr = new PlayerMatchRoomPool
                                {
                                    MRPID = row.Field<string>("MRPID"),
                                    PlayerID = row.Field<int>("PlayerID"),
                                    Board = row.Field<int>("Board"),
                                    Status = row.Field<string>("Status"),
                                    MatchRoomID = row.Field<string>("MatchRoomID"),
                                    RegDate = row.Field<DateTime>("RegDate"),
                                    LasActionDate = row.Field<DateTime>("LastActionDate")
                                };

                                _PlayerMatchRoomPool.Add(pmr);
                            }
                        }

                        _PlayerMatchRoom.PlayerMatchRoomPool = _PlayerMatchRoomPool;

                        transaction.Commit();

                        return _PlayerMatchRoom;
                    }
                    catch (Exception ex)
                    {
                        Lib.WriteConsoleMessage("\nError: " + ex.Message);
                        transaction.Rollback();
                        return null;
                    }
                }
            }
        }

        public bool DismissMatchRoom(PlayerRequest _PlayerRequest)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        //check if player has a room
                        var dataT = new DataTable();
                        using (MySqlCommand idCommand = new MySqlCommand("SELECT * FROM `hololive-official-cardgame`.`matchroom` WHERE OwnerID = ( SELECT PlayerID FROM `hololive-official-cardgame`.`player` WHERE PlayerID =@playerid AND Password =@password );", connection, transaction))
                        {
                            idCommand.Parameters.AddWithValue("@playerid", _PlayerRequest.playerID);
                            idCommand.Parameters.AddWithValue("@password", _PlayerRequest.password);

                            dataT.Load(idCommand.ExecuteReader());

                            //checking if there's someone else in the room to become the owner
                            if (dataT.Rows.Count > 0)
                            {
                                using (MySqlCommand Command = new MySqlCommand("SELECT * FROM `matchroompool` WHERE `MatchRoomID` = @roomid AND PlayerID != @ownerid ;", connection, transaction))
                                {
                                    Command.Parameters.AddWithValue("@roomid", dataT.Rows[0].Field<string>("RoomID"));
                                    Command.Parameters.AddWithValue("@ownerid", dataT.Rows[0].Field<int>("OwnerID"));
                                    var dataTable = new DataTable();
                                    Lib.WriteConsoleMessage(GetQueryWithParameters(Command));
                                    dataTable.Load(Command.ExecuteReader());

                                    // if is active in another queue, remove from all the queues
                                    if (dataTable.Rows.Count != 0)
                                    {
                                        using (MySqlCommand updateCommand = new MySqlCommand("UPDATE `matchroom` SET `OwnerID` = @newowner WHERE `matchroom`.`RoomID` = @roomid; UPDATE matchroompool SET Status='I' WHERE playerID=( SELECT PlayerID FROM `hololive-official-cardgame`.`player` WHERE PlayerID =@playerid AND Password =@password ) AND Status='A' ", connection, transaction))
                                        {
                                            updateCommand.Parameters.AddWithValue("@newowner", dataTable.Rows[0].Field<int>("PlayerID"));
                                            updateCommand.Parameters.AddWithValue("@roomid", dataTable.Rows[0].Field<string>("MatchRoomID"));
                                            updateCommand.Parameters.AddWithValue("@playerid", _PlayerRequest.playerID);
                                            updateCommand.Parameters.AddWithValue("@password", _PlayerRequest.password);
                                            Lib.WriteConsoleMessage(GetQueryWithParameters(updateCommand));
                                            updateCommand.ExecuteNonQuery();
                                        }
                                        transaction.Commit();
                                        return true;
                                    }
                                }
                            }
                            //we do not have another players in the room, so we can just eraseit
                            using (MySqlCommand updateCommand = new MySqlCommand("UPDATE `matchroom` SET `OwnerID` = '0' WHERE `matchroom`.`RoomID` =@roomid; UPDATE matchroompool SET Status='I' WHERE playerID=( SELECT PlayerID FROM `hololive-official-cardgame`.`player` WHERE PlayerID =@playerid AND Password =@password ) AND Status !='I' ", connection, transaction))
                            {
                                updateCommand.Parameters.AddWithValue("@roomid", dataT.Rows[0].Field<string>("RoomID"));
                                updateCommand.Parameters.AddWithValue("@playerid", _PlayerRequest.playerID);
                                updateCommand.Parameters.AddWithValue("@password", _PlayerRequest.password);
                                Lib.WriteConsoleMessage(GetQueryWithParameters(updateCommand));
                                updateCommand.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Lib.WriteConsoleMessage("\nError: " + ex.Message);
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }

        public bool LeaveMatchRoom(PlayerRequest _PlayerRequest)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        //Just leave the room
                        using (MySqlCommand updateCommand = new MySqlCommand("UPDATE matchroompool SET Status='I' WHERE playerID=( SELECT PlayerID FROM `hololive-official-cardgame`.`player` WHERE PlayerID =@playerid AND Password =@password ) AND Status ='A' LIMIT 1", connection, transaction))
                        {
                            updateCommand.Parameters.AddWithValue("@playerid", _PlayerRequest.playerID);
                            updateCommand.Parameters.AddWithValue("@password", _PlayerRequest.password);
                            updateCommand.ExecuteNonQuery();
                        }
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Lib.WriteConsoleMessage("\nError: " + ex.Message);
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }
        public PlayerMatchRoom? JoinTable(PlayerRequest _PlayerRequest)
        {
            string PlayerIDSQL = "( SELECT PlayerID FROM `hololive-official-cardgame`.`player` WHERE PlayerID = @playerid AND Password =@password )";
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        int TablePosition = 1;
                        //check if the room exist and the table os empty
                        var dataT = new DataTable();
                        using (MySqlCommand idCommand = new MySqlCommand("SELECT * FROM `hololive-official-cardgame`.`matchroom` WHERE RoomID =(SELECT MatchRoomID FROM `hololive-official-cardgame`.`matchroompool` WHERE PlayerID = " + PlayerIDSQL + " AND Status ='A' LIMIT 1) ;", connection, transaction))
                        {
                            idCommand.Parameters.AddWithValue("@playeridsql", PlayerIDSQL);
                            idCommand.Parameters.AddWithValue("@playerid", _PlayerRequest.playerID);
                            idCommand.Parameters.AddWithValue("@password", _PlayerRequest.password);
                            Lib.WriteConsoleMessage(GetQueryWithParameters(idCommand));
                            dataT.Load(idCommand.ExecuteReader());
                            //PrintDataTable(dataT);

                            if (dataT.Rows.Count > 1)
                            {
                                return null;
                            }

                            if (dataT.Rows.Count == 1)
                            {
                                TablePosition = 2;
                            }
                        }
                        //if the player is in the room
                        var dataTable = new DataTable();
                        using (MySqlCommand idCommand = new MySqlCommand("SELECT * FROM `hololive-official-cardgame`.`matchroompool` WHERE MatchRoomID = @roomid AND PlayerID = " + PlayerIDSQL + " AND Status = 'A';", connection, transaction))
                        {
                            idCommand.Parameters.AddWithValue("@playeridsql", PlayerIDSQL);
                            idCommand.Parameters.AddWithValue("@playerid", _PlayerRequest.playerID);
                            idCommand.Parameters.AddWithValue("@password", _PlayerRequest.password);
                            idCommand.Parameters.AddWithValue("@roomid", dataT.Rows[0].Field<string>("RoomID"));

                            Lib.WriteConsoleMessage(GetQueryWithParameters(idCommand));
                            dataTable.Load(idCommand.ExecuteReader());

                            if (dataTable.Rows.Count == 0)
                            {
                                return null;
                            }
                        }
                        //enter the room
                        using (MySqlCommand updateCommand = new MySqlCommand("UPDATE `matchroompool` SET `Status` = 'R', `Board` = @board, `Chair` = @chair WHERE PlayerID = " + PlayerIDSQL + " AND Status = 'A' LIMIT 1;", connection, transaction))
                        {
                            updateCommand.Parameters.AddWithValue("@playeridsql", PlayerIDSQL);
                            updateCommand.Parameters.AddWithValue("@playerid", _PlayerRequest.playerID);
                            updateCommand.Parameters.AddWithValue("@password", _PlayerRequest.password);
                            updateCommand.Parameters.AddWithValue("@board", _PlayerRequest.description);
                            updateCommand.Parameters.AddWithValue("@chair", TablePosition);

                            Lib.WriteConsoleMessage(GetQueryWithParameters(updateCommand));
                            updateCommand.ExecuteNonQuery();
                        }

                        //getting the new info for the room
                        List<PlayerMatchRoomPool> _PlayerMatchRoomPool = new List<PlayerMatchRoomPool>();


                        using (MySqlCommand selectCommand = new MySqlCommand("SELECT * FROM `hololive-official-cardgame`.`matchroompool` WHERE MatchRoomID =@roomid AND Status !='I' ", connection, transaction))
                        {
                            selectCommand.Parameters.AddWithValue("@roomid", dataT.Rows[0].Field<string>("RoomID"));
                            dataTable = new DataTable();
                            dataTable.Load(selectCommand.ExecuteReader());

                            foreach (DataRow row in dataTable.Rows)
                            {
                                var pmr = new PlayerMatchRoomPool
                                {
                                    MRPID = row.Field<string>("MRPID"),
                                    PlayerID = row.Field<int>("PlayerID"),
                                    Board = row.Field<int>("Board"),
                                    Status = row.Field<string>("Status"),
                                    MatchRoomID = row.Field<string>("MatchRoomID"),
                                    RegDate = row.Field<DateTime>("RegDate"),
                                    LasActionDate = row.Field<DateTime>("LastActionDate")
                                };

                                _PlayerMatchRoomPool.Add(pmr);
                            }
                        }

                        PlayerMatchRoom _PlayerMatchRoom = new PlayerMatchRoom();

                        using (MySqlCommand selectCommand = new MySqlCommand("SELECT * FROM `hololive-official-cardgame`.`matchroom` WHERE RoomID =@roomid ", connection, transaction))
                        {
                            selectCommand.Parameters.AddWithValue("@roomid", dataT.Rows[0].Field<string>("RoomID"));
                            dataTable = new DataTable();
                            dataTable.Load(selectCommand.ExecuteReader());

                            foreach (DataRow row in dataTable.Rows)
                            {
                                var pmr = new PlayerMatchRoom
                                {
                                    RoomID = row.Field<string>("RoomID"),
                                    RegDate = row.Field<DateTime>("RegDate"),
                                    RoomCode = row.Field<int>("RoomCode"),
                                    MaxPlayer = row.Field<int>("MaxPlayer"),
                                    OwnerID = row.Field<int>("OwnerID")
                                };
                                _PlayerMatchRoom = pmr;
                            }
                        }


                        _PlayerMatchRoom.PlayerMatchRoomPool = _PlayerMatchRoomPool;

                        transaction.Commit();

                        return _PlayerMatchRoom;
                    }
                    catch (Exception ex)
                    {
                        Lib.WriteConsoleMessage("\nError: " + ex.Message);
                        transaction.Rollback();
                        return null;
                    }
                }
            }
        }

        public bool LeaveTable(PlayerRequest _PlayerRequest)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        //Just leave the room
                        using (MySqlCommand updateCommand = new MySqlCommand("UPDATE matchroompool SET Status='A', Board='0', Chair='0' WHERE playerID=( SELECT PlayerID FROM `hololive-official-cardgame`.`player` WHERE PlayerID =@playerid AND Password =@password ) AND Status ='R' LIMIT 1 ", connection, transaction))
                        {
                            updateCommand.Parameters.AddWithValue("@playerid", _PlayerRequest.playerID);
                            updateCommand.Parameters.AddWithValue("@password", _PlayerRequest.password);
                            updateCommand.ExecuteNonQuery();
                        }
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Lib.WriteConsoleMessage("\nError: " + ex.Message);
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }



        public bool LockTable(PlayerRequest _PlayerRequest)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        //Just leave the room
                        using (MySqlCommand updateCommand = new MySqlCommand("UPDATE matchroompool SET Status='D' WHERE playerID=( SELECT PlayerID FROM `hololive-official-cardgame`.`player` WHERE PlayerID =@playerid AND Password =@password ) AND Status ='R' LIMIT 1 ", connection, transaction))
                        {
                            updateCommand.Parameters.AddWithValue("@playerid", _PlayerRequest.playerID);
                            updateCommand.Parameters.AddWithValue("@password", _PlayerRequest.password);
                            updateCommand.ExecuteNonQuery();
                        }
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Lib.WriteConsoleMessage("\nError: " + ex.Message);
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }



        public bool UnlockTable(PlayerRequest _PlayerRequest)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        //Just leave the room
                        using (MySqlCommand updateCommand = new MySqlCommand("UPDATE matchroompool SET Status='R' WHERE playerID=( SELECT PlayerID FROM `hololive-official-cardgame`.`player` WHERE PlayerID =@playerid AND Password =@password ) AND Status ='D' LIMIT 1 ", connection, transaction))
                        {
                            updateCommand.Parameters.AddWithValue("@playerid", _PlayerRequest.playerID);
                            updateCommand.Parameters.AddWithValue("@password", _PlayerRequest.password);
                            updateCommand.ExecuteNonQuery();
                        }
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Lib.WriteConsoleMessage("\nError: " + ex.Message);
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }


        public PlayerMatchRoom UpdateRoom(PlayerRequest _PlayerRequest)
        {
            string PlayerIDSQL = "( SELECT PlayerID FROM `hololive-official-cardgame`.`player` WHERE PlayerID = @playerid AND Password = @password )";
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // getting the data from which table the user are
                        List<PlayerMatchRoomPool> _PlayerMatchRoomPool = new List<PlayerMatchRoomPool>();

                        var dataTable = new DataTable();
                        using (MySqlCommand selectCommand = new MySqlCommand("SELECT * FROM `hololive-official-cardgame`.`matchroompool` WHERE MatchRoomID =(SELECT MatchRoomID FROM `hololive-official-cardgame`.`matchroompool` WHERE PlayerID = " + PlayerIDSQL + " AND Status !='I' ) AND Status !='I'", connection, transaction))
                        {
                            selectCommand.Parameters.AddWithValue("@playerid", _PlayerRequest.playerID);
                            selectCommand.Parameters.AddWithValue("@password", _PlayerRequest.password);
                            Lib.WriteConsoleMessage(GetQueryWithParameters(selectCommand));
                            dataTable.Load(selectCommand.ExecuteReader());

                            foreach (DataRow row in dataTable.Rows)
                            {
                                var pmr = new PlayerMatchRoomPool
                                {
                                    MRPID = row.Field<string>("MRPID"),
                                    PlayerID = row.Field<int>("PlayerID"),
                                    Board = row.Field<int>("Board"),
                                    Status = row.Field<string>("Status"),
                                    MatchRoomID = row.Field<string>("MatchRoomID"),
                                    RegDate = row.Field<DateTime>("RegDate"),
                                    LasActionDate = row.Field<DateTime>("LastActionDate")
                                };

                                _PlayerMatchRoomPool.Add(pmr);
                            }
                        }

                        //getting the room info from which the user are
                        var dataT = new DataTable();
                        using (MySqlCommand idCommand = new MySqlCommand("SELECT * FROM `hololive-official-cardgame`.`matchroom` WHERE RoomID = @roomcode AND OwnerID != 0;", connection, transaction))
                        {
                            idCommand.Parameters.AddWithValue("@roomcode", dataTable.Rows[0].Field<string>("MatchRoomID"));
                            Lib.WriteConsoleMessage(GetQueryWithParameters(idCommand));
                            dataT.Load(idCommand.ExecuteReader());
                        }

                        if (dataT.Rows.Count == 0)
                        {
                            return null;
                        }

                        //since user it's a room, get the room data
                        PlayerMatchRoom _PlayerMatchRoom = new PlayerMatchRoom();

                        foreach (DataRow row in dataT.Rows)
                        {
                            var pmr = new PlayerMatchRoom
                            {
                                RoomID = row.Field<string>("RoomID"),
                                RegDate = row.Field<DateTime>("RegDate"),
                                RoomCode = row.Field<int>("RoomCode"),
                                MaxPlayer = row.Field<int>("MaxPlayer"),
                                OwnerID = row.Field<int>("OwnerID")
                            };
                            _PlayerMatchRoom = pmr;
                        }


                        _PlayerMatchRoom.PlayerMatchRoomPool = _PlayerMatchRoomPool;

                        transaction.Commit();

                        return _PlayerMatchRoom;
                    }
                    catch (Exception ex)
                    {
                        Lib.WriteConsoleMessage("\nError: " + ex.Message);
                        transaction.Rollback();
                        return null;
                    }
                }
            }
        }




        // MATCH MAKER

        // we check if there's two players in the matchpool ready to play 'A' if so, get the oldest in the list and match with the one that just joined
        public List<PlayerInfo> CheckForAvaliablePlayers()
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {

                        List<PlayerInfo> playerInfoList = new List<PlayerInfo>();

                        var dataTable = new DataTable();
                        using (MySqlCommand selectCommand = new MySqlCommand("SELECT * FROM `hololive-official-cardgame`.`matchpool` WHERE Status = 'A' ORDER BY `RegDate` ASC LIMIT 2;", connection, transaction))
                        {
                            if (DebugVatiable)
                                Lib.WriteConsoleMessage(GetQueryWithParameters(selectCommand));

                            dataTable.Load(selectCommand.ExecuteReader());

                            if (dataTable.Rows.Count < 2)
                                return playerInfoList;


                            var dataT = new DataTable();
                            using (MySqlCommand SelectUserCommand = new MySqlCommand("SELECT * FROM player WHERE PlayerID IN (@p1, @p2);", connection, transaction))
                            {

                                SelectUserCommand.Parameters.AddWithValue("@p1", dataTable.Rows[0].Field<int>("PlayerID"));
                                SelectUserCommand.Parameters.AddWithValue("@p2", dataTable.Rows[1].Field<int>("PlayerID"));

                                if (DebugVatiable)
                                    Lib.WriteConsoleMessage(GetQueryWithParameters(SelectUserCommand));

                                dataT.Load(SelectUserCommand.ExecuteReader());
                            }

                            foreach (DataRow row in dataT.Rows)
                            {
                                var playerInfo = new PlayerInfo
                                {
                                    PlayerID = row.Field<int>("PlayerID").ToString(),
                                    PlayerName = row.Field<string>("PlayerName"),
                                    PlayerIcon = row.Field<int>("PlayerIcon"),
                                    HoloCoins = row.Field<int>("HoloCoins"),
                                    HoloGold = row.Field<int>("HoloGold"),
                                    NNMaterial = row.Field<int>("NNMaterial"),
                                    RRMaterial = row.Field<int>("RRMaterial"),
                                    SRMaterial = row.Field<int>("SRMaterial"),
                                    URMaterial = row.Field<int>("URMaterial"),
                                    MatchVictory = row.Field<int>("MatchVictory"),
                                    MatchLoses = row.Field<int>("MatchLoses"),
                                    MatchesTotal = row.Field<int>("MatchesTotal"),
                                    Email = row.Field<string>("Email"),
                                    Password = row.Field<string>("Password")
                                };

                                playerInfoList.Add(playerInfo);
                            }


                            transaction.Commit();
                            return playerInfoList;
                        }
                    }
                    catch (Exception ex)
                    {
                        Lib.WriteConsoleMessage("\nError: " + ex.Message);
                        transaction.Rollback();
                        return null;
                    }
                }
            }
        }

        //we call this function after both players have entered the matchpool and their sockets are in the socket list
        public bool LockPlayersForAMatch(string playerOneId, string playerTwoId)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        using (MySqlCommand updateCommand = new MySqlCommand("UPDATE matchpool SET Status='D' WHERE playerID IN (@playeroneid, @playertwoid) AND Status='A' LIMIT 2", connection, transaction))
                        {
                            updateCommand.Parameters.AddWithValue("@playeroneid", playerOneId);
                            updateCommand.Parameters.AddWithValue("@playertwoid", playerTwoId);
                            updateCommand.ExecuteNonQuery();
                        }
                        transaction.Commit();

                        return true;
                    }
                    catch (Exception ex)
                    {
                        Lib.WriteConsoleMessage("\nError: " + ex.Message);
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }
        //unlock the players and insert the winner into the base
        public bool SetWinnerForMatch(string WinnerID, string LoserID)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        using (MySqlCommand updateCommand = new MySqlCommand("UPDATE matchpool SET Status='I' WHERE playerID=@playeroneid OR playerID=@playertwoid AND Status='D' ", connection, transaction))
                        {
                            updateCommand.Parameters.AddWithValue("@playeroneid", WinnerID);
                            updateCommand.Parameters.AddWithValue("@playertwoid", LoserID);
                            updateCommand.ExecuteNonQuery();
                        }
                        transaction.Commit();

                        return true;
                    }
                    catch (Exception ex)
                    {
                        Lib.WriteConsoleMessage("\nError: " + ex.Message);
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }


        static public void StartServerClearQueue()
        {
            using (MySqlConnection connection = new MySqlConnection("Server=localhost;Database=hololive-official-cardgame;User ID=root;Password=;Pooling=true;"))
            {
                connection.Open();

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        using (MySqlCommand updateCommand = new MySqlCommand("UPDATE matchpool SET Status='I' WHERE Status !='I' ", connection, transaction))
                        {
                            updateCommand.ExecuteNonQuery();
                        }
                        transaction.Commit();

                        return;
                    }
                    catch (Exception ex)
                    {
                        Lib.WriteConsoleMessage("\nError cleaning the list before start the server: " + ex.Message);
                        transaction.Rollback();
                        return;
                    }
                }
            }
        }


        public bool CofirmMatchForAvaliablePlayers(int p1, int p2)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        using (MySqlCommand updateCommand = new MySqlCommand("UPDATE `matchpool` SET `Status` = 'D' WHERE `matchpool`.`PlayerID` IN (@p1, @p2) AND Status = 'A';", connection, transaction))
                        {

                            updateCommand.Parameters.AddWithValue("@p1", p1);
                            updateCommand.Parameters.AddWithValue("@p2", p2);
                            if (DebugVatiable)
                                Lib.WriteConsoleMessage(GetQueryWithParameters(updateCommand));
                            updateCommand.ExecuteNonQuery();
                        }
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Lib.WriteConsoleMessage("\nError: " + ex.Message);
                        transaction.Rollback();
                    }
                    return false;
                }
            }
        }

        public List<List<Card>> GetMatchPlayersDeck(string p1)
        {

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var dataTable = new DataTable();
                        using (MySqlCommand updateCommand = new MySqlCommand("SELECT * FROM `playerdeck` WHERE PlayerID IN (@p1) AND Status = 'A' LIMIT 2;", connection, transaction))
                        {
                            updateCommand.Parameters.AddWithValue("@p1", p1);

                            if (DebugVatiable)
                                Lib.WriteConsoleMessage(GetQueryWithParameters(updateCommand));

                            dataTable.Load(updateCommand.ExecuteReader());
                        }

                        List<Card> DeckP1 = new List<Card>();
                        foreach (string s in dataTable.Rows[0].Field<string>("MainDeck").Split(','))
                        {
                            DeckP1.Add(new Card(s));
                        }
                        List<Card> CheerDeckP1 = new List<Card>();
                        foreach (string s in dataTable.Rows[0].Field<string>("CheerDeck").Split(','))
                        {
                            CheerDeckP1.Add(new Card(s));
                        }

                        List<Card> OshiP1 = new List<Card>();
                        OshiP1.Add(new Card(dataTable.Rows[0].Field<string>("OshiCard")));

                        transaction.Commit();
                        List<List<Card>> ret = new List<List<Card>>();
                        ret.Add(DeckP1);
                        ret.Add(CheerDeckP1);
                        ret.Add(OshiP1);
                        return ret;
                    }
                    catch (Exception ex)
                    {
                        Lib.WriteConsoleMessage("\nError: " + ex.Message);
                        transaction.Rollback();
                    }
                    return null;
                }
            }
        }

        static public bool SetDeckAsActive(PlayerRequest PlayerInfo)
        {
            string jsonDeckData = PlayerInfo.jsonObject.ToString();
            if (PlayerInfo.jsonObject is JsonElement element && element.ValueKind == JsonValueKind.Object)
                jsonDeckData = element.GetRawText();
            DeckData _DeckData;
            try
            {
                 _DeckData = JsonSerializer.Deserialize<DeckData>(jsonDeckData);
            }
            catch (Exception e) {
                return false;
            }
            using (MySqlConnection connection = new MySqlConnection("Server=localhost;Database=hololive-official-cardgame;User ID=root;Password=;Pooling=true;"))
            {
                connection.Open();

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        using (MySqlCommand updateCommand = new MySqlCommand("UPDATE playerdeck SET Status='' WHERE PlayerID=@playerid ", connection, transaction))
                        {
                            updateCommand.Parameters.AddWithValue("@playerid", PlayerInfo.playerID);
                            updateCommand.ExecuteNonQuery();
                        }
                        using (MySqlCommand updateCommand = new MySqlCommand("UPDATE playerdeck SET Status='A' WHERE DeckID=@deckid AND PlayerID=@playerid ", connection, transaction))
                        {
                            updateCommand.Parameters.AddWithValue("@playerid", PlayerInfo.playerID);
                            updateCommand.Parameters.AddWithValue("@deckid", _DeckData.deckId);
                            updateCommand.ExecuteNonQuery();
                        }

                        transaction.Commit();

                        return true;
                    }
                    catch (Exception ex)
                    {
                        Lib.WriteConsoleMessage("\nError cleaning the list before start the server: " + ex.Message);
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }
        static public bool UpdateDeckInfo(PlayerRequest PlayerInfo)
        {
            string jsonDeckData = PlayerInfo.jsonObject.ToString();
            if (PlayerInfo.jsonObject is JsonElement element && element.ValueKind == JsonValueKind.Object)
            {
                jsonDeckData = element.GetRawText();
            }
            DeckData _DeckData;
            try
            {
                _DeckData = JsonSerializer.Deserialize<DeckData>(jsonDeckData);
            }
            catch (Exception e)
            {
                return false;
            }

            using (MySqlConnection connection = new MySqlConnection("Server=localhost;Database=hololive-official-cardgame;User ID=root;Password=;Pooling=true;"))
            {
                connection.Open();

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        int rowsAffected = 0;
                        using (MySqlCommand updateCommand = new MySqlCommand("UPDATE playerdeck SET Name=@name, MainDeck=@maindeck, CheerDeck=@cheerdeck, OshiCard=@oshicard WHERE DeckID=@deckid AND PlayerID=@playerid ", connection, transaction))
                        {
                            updateCommand.Parameters.AddWithValue("@name", _DeckData.deckName);
                            updateCommand.Parameters.AddWithValue("@maindeck", _DeckData.main);
                            updateCommand.Parameters.AddWithValue("@cheerdeck", _DeckData.energy);
                            updateCommand.Parameters.AddWithValue("@oshicard", _DeckData.oshi);
                            updateCommand.Parameters.AddWithValue("@playerid", PlayerInfo.playerID);
                            updateCommand.Parameters.AddWithValue("@deckid", _DeckData.deckId);
                            rowsAffected = updateCommand.ExecuteNonQuery();
                        }

                        if (rowsAffected == 0)
                        {
                            using (MySqlCommand insertCommand = new MySqlCommand("INSERT INTO `playerdeck` (`DeckID`, `PlayerID`, `Name`, `MainDeck`, `CheerDeck`, `OshiCard`, `Status`) VALUES ('', @playerid, @name, @maindeck, @cheerdeck, @oshicard, '')", connection, transaction))
                            {
                                insertCommand.Parameters.AddWithValue("@name", _DeckData.deckName);
                                insertCommand.Parameters.AddWithValue("@maindeck", _DeckData.main);
                                insertCommand.Parameters.AddWithValue("@cheerdeck", _DeckData.energy);
                                insertCommand.Parameters.AddWithValue("@oshicard", _DeckData.oshi);
                                insertCommand.Parameters.AddWithValue("@playerid", PlayerInfo.playerID);
                                insertCommand.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();

                        return true;
                    }
                    catch (Exception ex)
                    {
                        Lib.WriteConsoleMessage("\nError cleaning the list before start the server: " + ex.Message);
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }
        public List<DeckData> GetDeckInfo(PlayerRequest PlayerInfo, bool AllDecks)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                List<PlayerInfo> playerInfoList = new List<PlayerInfo>();

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string queryComplement = !AllDecks ? "Status ='A' AND" : "";
                        string insertQuery = $"SELECT * FROM playerdeck WHERE {queryComplement} PlayerID=( SELECT PlayerID FROM `hololive-official-cardgame`.`player` WHERE PlayerID =@playerid AND Password =@password ) LIMIT 1;";

                        List<DeckData> ListDeckData = new();
                        using (MySqlCommand insertCommand = new MySqlCommand(insertQuery, connection, transaction))
                        {
                            insertCommand.Parameters.AddWithValue("@playerid", PlayerInfo.playerID);
                            insertCommand.Parameters.AddWithValue("@password", PlayerInfo.password);

                            var result = insertCommand.ExecuteReader();
                            DataTable dataTable = new ();
                            dataTable.Load(result);

                            if (dataTable.Rows.Count == 0)
                            {
                                string hash = QuickHash();

                                using (MySqlCommand updateCommand = new MySqlCommand("INSERT INTO `playerdeck` (`DeckID`, `PlayerID`, `Name`, `MainDeck`, `CheerDeck`, `OshiCard`, `Status`) VALUES (@hash, ( SELECT PlayerID FROM `hololive-official-cardgame`.`player` WHERE PlayerID =@playerid AND Password =@password ), 'Deck', 'hSD01-003,hSD01-003,hSD01-003,hSD01-003,hSD01-004,hSD01-004,hSD01-004,hSD01-005,hSD01-005,hSD01-005,hSD01-006,hSD01-006,hSD01-007,hSD01-007,hSD01-008,hSD01-008,hSD01-008,hSD01-008,hSD01-009,hSD01-009,hSD01-009,hSD01-010,hSD01-010,hSD01-010,hSD01-011,hSD01-011,hSD01-012,hSD01-012,hSD01-013,hSD01-013,hSD01-014,hSD01-014,hSD01-015,hSD01-015,hSD01-018,hSD01-018,hSD01-018,hSD01-019,hSD01-019,hSD01-019,hBP01-106,hBP01-106,hBP01-108,hBP01-108,hSD01-020,hSD01-020,hSD01-020,hSD01-019,hSD01-016,hSD01-017', 'hY01-001,hY01-001,hY01-001,hY01-001,hY01-001,hY01-001,hY01-001,hY01-001,hY01-001,hY01-001,hY02-001,hY02-001,hY02-001,hY02-001,hY02-001,hY02-001,hY02-001,hY02-001,hY02-001,hY02-001', 'hSD01-001', 'A')", connection, transaction))
                                {
                                    updateCommand.Parameters.AddWithValue("@playerid", PlayerInfo.playerID);
                                    updateCommand.Parameters.AddWithValue("@password", PlayerInfo.password);
                                    updateCommand.Parameters.AddWithValue("@hash", hash);
                                    updateCommand.ExecuteNonQuery();


                                }
                                using (MySqlCommand GetCommandTwo = new MySqlCommand(insertQuery, connection, transaction))
                                {
                                    GetCommandTwo.Parameters.AddWithValue("@playerid", PlayerInfo.playerID);
                                    GetCommandTwo.Parameters.AddWithValue("@password", PlayerInfo.password);
                                    result = GetCommandTwo.ExecuteReader();

                                    dataTable = new DataTable();
                                    dataTable.Load(result);
                                }
                            }

                            foreach (DataRow row in dataTable.Rows)
                            {
                                DeckData deckData = new (){
                                    deckId = row.Field<string>("DeckID"),
                                    deckName = row.Field<string>("Name"),
                                    main = row.Field<string>("MainDeck"),
                                    energy = row.Field<string>("CheerDeck"),
                                    status = row.Field<string>("Status"),
                                    oshi = row.Field<string>("OshiCard")
                                };
                                ListDeckData.Add(deckData);
                            }
                        }

                        transaction.Commit();
                        return ListDeckData;
                    }
                    catch (Exception ex)
                    {
                        Lib.WriteConsoleMessage("\nError: " + ex.Message);
                        transaction.Rollback();
                        return null;
                    }
                }
            }
        }




        //start getting player list
        private List<T> FetchData<T>(string query, string playerid, Func<DataRow, T> mapFunction)
        {
            List<T> results = new List<T>();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        using (MySqlCommand command = new MySqlCommand(query, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@playerid", playerid);

                            var reader = command.ExecuteReader();
                            var dataTable = new DataTable();
                            dataTable.Load(reader);

                            PrintDataTable(dataTable);

                            foreach (DataRow row in dataTable.Rows)
                            {
                                results.Add(mapFunction(row));
                            }
                        }

                        transaction.Commit();
                        return results;
                    }
                    catch (Exception ex)
                    {
                        Lib.WriteConsoleMessage("\nError: " + ex.Message);
                        transaction.Rollback();
                        return null;
                    }
                }
            }
        }

        public List<PlayerItemBox> GetPlayerItemBox(string playerid)
        {
            string query = "SELECT * FROM playeritembox WHERE PlayerID=@playerid ORDER BY ExpirationDate DESC;";
            return FetchData<PlayerItemBox>(query, playerid, MapPlayerItemBox);
        }

        public List<PlayerMission> GetPlayerMission(string playerid)
        {
            string query = "SELECT * FROM playermissionlist WHERE PlayerID=@playerid ORDER BY ObtainedDate DESC;";
            return FetchData<PlayerMission>(query, playerid, MapPlayerMission);
        }

        public List<PlayerMessageBox> GetPlayerMessageBox(string playerid)
        {
            string query = "SELECT * FROM playermessagebox WHERE PlayerID=@playerid ORDER BY ObtainedDate DESC;";
            return FetchData<PlayerMessageBox>(query, playerid, MapPlayerMessageBox);
        }
        public List<PlayerBadge> GetPlayerBadges(string playerid)
        {
            string query = "SELECT * FROM playerbadgelist WHERE PlayerID=@playerid ORDER BY ObtainedDate DESC LIMIT 5;";
            return FetchData<PlayerBadge>(query, playerid, MapPlayerBadges);
        }
        public List<PlayerTitle> GetPlayerTitles(string playerid)
        {
            string query = "SELECT * FROM playertitle WHERE PlayerID=@playerid ORDER BY ObtainedDate DESC;";
            return FetchData<PlayerTitle>(query, playerid, MapPlayerTitles);
        }

        private PlayerItemBox MapPlayerItemBox(DataRow row)
        {
            return new PlayerItemBox
            {
                PlayerItemBoxID = row.Field<int>("PlayerItemBoxID"),
                PlayerID = row.Field<int>("PlayerID").ToString(),
                ItemID = row.Field<int>("ItemID"),
                Amount = row.Field<int>("Amount"),
                ObtainedDate = row.Field<DateTime>("ObtainedDate"),
                ExpirationDate = row.Field<DateTime>("ExpirationDate")
            };
        }

        private PlayerMission MapPlayerMission(DataRow row)
        {
            return new PlayerMission
            {
                PlayerMissionListID = row.Field<int>("PlayerMissionListID"),
                ObtainedDate = row.Field<DateTime>("ObtainedDate"),
                ClearData = row.Field<DateTime>("ClearData"),
                PlayerID = row.Field<int>("PlayerID").ToString(),
                MissionID = row.Field<int>("MissionID")
            };
        }

        private PlayerMessageBox MapPlayerMessageBox(DataRow row)
        {
            return new PlayerMessageBox
            {
                MessageID = row.Field<int>("MessageID"),
                PlayerID = row.Field<int>("PlayerID").ToString(),
                Title = row.Field<string>("Title"),
                ObtainedDate = row.Field<DateTime>("ObtainedDate"),
                Description = row.Field<string>("Description")
            };
        }

        private PlayerBadge MapPlayerBadges(DataRow row)
        {
            return new PlayerBadge
            {
                BadgeID = row.Field<int>("BadgeID"),
                PlayerID = row.Field<int>("PlayerID").ToString(),
                Rank = row.Field<int>("Rank"),
                ObtainedDate = row.Field<DateTime>("ObtainedDate")
            };
        }
        private PlayerTitle MapPlayerTitles(DataRow row)
        {
            return new PlayerTitle
            {
                TitleID = row.Field<int>("TitleID"),
                PlayerID = row.Field<int>("PlayerID").ToString(),
                TitleName = row.Field<string>("TitleName"),
                TitleDescription = row.Field<string>("TitleDescription"),
                ObtainedDate = row.Field<DateTime>("ObtainedDate")
            };
        }

        //End getting player list

        static string GetQueryWithParameters(MySqlCommand command)
        {
            string commandText = command.CommandText;

            foreach (MySqlParameter param in command.Parameters)
            {
                string value = param.Value == DBNull.Value ? "NULL" :
                                param.DbType == System.Data.DbType.String ? $"'{param.Value.ToString().Replace("'", "''")}'" :
                                param.Value.ToString();
                commandText = commandText.Replace(param.ParameterName, value);
            }

            return commandText;
        }

        public static string ValidateInput(string input)
        {
            string pattern = @"^[a-zA-Z0-9\s\.,_-]+$";
            bool isValid = Regex.IsMatch(input, pattern);

            if (isValid) { return input; }
            else { return "error"; }
        }
        static void ThrowException(string error)
        {
            throw new InvalidOperationException("Something went wrong, error: " + error);
        }

        private string QuickHash() // function to generate random password for players new accounts
        {
            Random _rnd = new Random();
            int n = _rnd.Next(1, 9999);
            string input = n + DateTime.Now.ToString("o") + "Konpeko";

            byte[] inputBytes = Encoding.UTF8.GetBytes(input);

            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(inputBytes);

                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        public void PrintDataTable(DataTable dataTable)
        {
            foreach (DataColumn column in dataTable.Columns)
            {
                Console.Write(column.ColumnName + "\t");
            }

            foreach (DataRow row in dataTable.Rows)
            {
                foreach (var item in row.ItemArray)
                {
                    if (item is DateTime dateTime)
                    {
                        Console.Write(dateTime.ToString("yyyy-MM-dd HH:mm:ss") + "\t");
                    }
                    else
                    {
                        Console.Write(item + "\t");
                    }
                }
            }
        }
    }
}