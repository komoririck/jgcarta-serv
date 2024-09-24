using hololive_oficial_cardgame_server.Controllers;
using MySql.Data.MySqlClient;
using Mysqlx.Crud;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Data;
using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace hololive_oficial_cardgame_server
{

    public class DBConnection
    {
        public bool debug = true;

        private string connectionString = "Server=localhost;Database=hololive-official-cardgame;User ID=root;Password=;Pooling=true;";
        private object dataTable;

        public void ClientAuthentication(string hash)
        {
        }

        public void AccountAuthentication(int playerId, string hash)
        {
        }

        public CreateAccount CreateAccount()
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                CreateAccount _CreateAccount = new CreateAccount();
                connection.Open();
                string hash = QuickHash();
                long lastInsertId = 0;

                // Use a transaction to ensure consistency
                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Insert statement
                        string insertQuery = "INSERT INTO `player` (`PlayerID`, `PlayerName`, `PlayerIcon`, `HoloCoins`, `HoloGold`, `NNMaterial`, `RRMaterial`, `SRMaterial`, `URMaterial`, `MatchVictory`, `MatchLoses`, `MatchesTotal`, `Email`, `Password`, `AccountLink`, `RegDate`) " +
                                                "VALUES (NULL, '', '', '', '', '', '', '', '', '', '', '', '', @hash, '', NOW());";

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
                        _CreateAccount.Password = hash;
                        _CreateAccount.PlayerID =  (int)lastInsertId;
                        return _CreateAccount;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + ex.Message);
                        // Rollback transaction in case of error
                        transaction.Rollback();
                        return null;
                    }
                    return null;
                }
            }
        }

        public PlayerInfo GetPlayerInfo(int playerid, string playerpassword)
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


                            PrintDataTable(dataTable);

                            foreach (DataRow row in dataTable.Rows)
                            {
                                var playerInfo = new PlayerInfo
                                {
                                    PlayerID = row.Field<int>("PlayerID"),
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

                        return playerInfoList[0];
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + ex.Message);
                        transaction.Rollback();
                        return null;
                    }
                    return null;
                }
            }
        }

        public PlayerInfo LoginAccount(string email, string password)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                List<PlayerInfo> playerInfoList = new List<PlayerInfo>();

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string insertQuery = "SELECT * FROM player WHERE Email=@email AND Password=@password LIMIT 1;";

                        using (MySqlCommand insertCommand = new MySqlCommand(insertQuery, connection, transaction))
                        {
                            insertCommand.Parameters.AddWithValue("@email", email);
                            insertCommand.Parameters.AddWithValue("@password", password);

                            var result = insertCommand.ExecuteReader();
                            var dataTable = new DataTable();
                            dataTable.Load(result);


                            PrintDataTable(dataTable);

                            foreach (DataRow row in dataTable.Rows)
                            {
                                var playerInfo = new PlayerInfo
                                {
                                    PlayerID = row.Field<int>("PlayerID"),
                                    Password = row.Field<string>("Password")
                                };

                                playerInfoList.Add(playerInfo);
                            }

                        }

                        transaction.Commit();

                        return playerInfoList[0];
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + ex.Message);
                        transaction.Rollback();
                        return null;
                    }
                    return null;
                }
            }
        }

        public ReturnMessage UpdatePlayerName(PlayerInfo values)//(List<Dictionary<string, string>> values)
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
                            insertCommand.Parameters.AddWithValue("@playerpassword", values.Password);
                            insertCommand.Parameters.AddWithValue("@playername", values.PlayerName);
                            insertCommand.Parameters.AddWithValue("@playerid", values.PlayerID);
                            insertCommand.ExecuteNonQuery();

                            transaction.Commit();
                            return new ReturnMessage("success");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + ex.Message);
                        transaction.Rollback();
                        return null;
                    }
                }
            }
        }

        public ReturnMessage UpdatePlayerProfilePicture(PlayerInfo values)//(List<Dictionary<string, string>> values)
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
                            insertCommand.Parameters.AddWithValue("@playerpassword", values.Password);
                            insertCommand.Parameters.AddWithValue("@PlayerIcon", values.PlayerIcon);
                            insertCommand.Parameters.AddWithValue("@playerid", values.PlayerID);
                            insertCommand.ExecuteNonQuery();

                            transaction.Commit();

                            return new ReturnMessage("success");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + ex.Message);
                        transaction.Rollback();
                        return null;
                    }
                }
            }
        }

        public List<PlayerBadge> GetPlayerBadgesV2(int playerid)//(List<Dictionary<string, string>> values)
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
                        Console.WriteLine("Error: " + ex.Message);
                        transaction.Rollback();
                        return null;
                    }
                }
            }
        }


        public ReturnMessage JoinMatchQueue(GenericPlayerCommunication _GenericPlayerCommunication)
        {
            string PlayerIDSQL = "( SELECT PlayerID FROM `hololive-official-cardgame`.`player` WHERE PlayerID = @playerid AND Password =@password )";
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        using (MySqlCommand idCommand = new MySqlCommand("SELECT PlayerID, Status FROM `hololive-official-cardgame`.`matchpool` WHERE PlayerID = " + PlayerIDSQL + " AND Status = 'A' UNION SELECT  PlayerID, Status FROM `hololive-official-cardgame`.`matchroompool` WHERE PlayerID = " + PlayerIDSQL + " AND Status != 'I';", connection, transaction))
                        {
                            idCommand.Parameters.AddWithValue("@playerid", _GenericPlayerCommunication.PlayerID);
                            idCommand.Parameters.AddWithValue("@password", _GenericPlayerCommunication.Password);
                            var dataTable = new DataTable();
                            Debug.WriteLine(GetQueryWithParameters(idCommand));
                            dataTable.Load(idCommand.ExecuteReader());

                            if(dataTable.Rows.Count > 0)
                            {
                                using (MySqlCommand updateCommand = new MySqlCommand("DELETE FROM matchpool WHERE playerID=( SELECT PlayerID FROM `hololive-official-cardgame`.`player` WHERE PlayerID =@playerid AND Password =@password ) AND Status='A'; UPDATE matchroompool SET Status='A', Board = '0', Chair='0' WHERE playerID=( SELECT PlayerID FROM `hololive-official-cardgame`.`player` WHERE PlayerID =@playerid AND Password =@password ) AND Status != 'I' ", connection, transaction))
                                {
                                    updateCommand.Parameters.AddWithValue("@playerid", _GenericPlayerCommunication.PlayerID);
                                    updateCommand.Parameters.AddWithValue("@password", _GenericPlayerCommunication.Password);
                                    Debug.WriteLine(GetQueryWithParameters(updateCommand));
                                    updateCommand.ExecuteNonQuery();
                                }
                            }
                        }

                        string insertQuery = "INSERT INTO `matchpool` (`MatchPoolID`, `PlayerID`, `RegDate`, `Type`, `Status`, `Code`) VALUES (UUID(), @playerid, NOW(), @type, 'A', @code);";

                        using (MySqlCommand insertCommand = new MySqlCommand(insertQuery, connection, transaction))
                        {

                            insertCommand.Parameters.AddWithValue("@playerid", _GenericPlayerCommunication.PlayerID);
                            insertCommand.Parameters.AddWithValue("@type", _GenericPlayerCommunication.RequestData.description);
                            insertCommand.Parameters.AddWithValue("@code", _GenericPlayerCommunication.RequestData.requestObject);
                            //insertCommand.Parameters.AddWithValue("@uuidv7", GenerateUuidV7());
                            Debug.WriteLine(GetQueryWithParameters(insertCommand));
                            insertCommand.ExecuteNonQuery();
                        }

                        transaction.Commit();

                        return new ReturnMessage("success");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + ex.Message);
                        transaction.Rollback();
                        return null;
                    }
                    return null;
                }
            }
        }

        public ReturnMessage CancelMatchQueue(GenericPlayerCommunication _GenericPlayerCommunication)
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
                            updateCommand.Parameters.AddWithValue("@playerid", _GenericPlayerCommunication.PlayerID);
                            updateCommand.Parameters.AddWithValue("@password", _GenericPlayerCommunication.Password);
                            updateCommand.ExecuteNonQuery();
                        }
                        transaction.Commit();

                        return new ReturnMessage("success");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + ex.Message);
                        transaction.Rollback();
                        return null;
                    }
                    return null;
                }
            }
        }


        public PlayerMatchRoom CreateMatchRoomQueue(GenericPlayerCommunication _GenericPlayerCommunication)
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
                            idCommand.Parameters.AddWithValue("@playerid", _GenericPlayerCommunication.PlayerID);
                            idCommand.Parameters.AddWithValue("@password", _GenericPlayerCommunication.Password);

                            dataT.Load(idCommand.ExecuteReader());
                        }
                        Random random = new Random();
                        int code = random.Next(100000, 1000000);
                        string uuidv7 = "UUID()"; // GenerateUuidV7();
                        // if not has a room, check if player is active in another queue
                        if (dataT.Rows.Count == 0) {

                            using (MySqlCommand idCommand = new MySqlCommand("SELECT PlayerID, Status FROM `hololive-official-cardgame`.`matchpool` WHERE PlayerID = ( SELECT PlayerID FROM `hololive-official-cardgame`.`player` WHERE PlayerID =@playerid AND Password =@password ) AND Status = 'A' UNION SELECT  PlayerID, Status  FROM `hololive-official-cardgame`.`matchroompool` WHERE PlayerID = ( SELECT PlayerID FROM `hololive-official-cardgame`.`player` WHERE PlayerID = @playerid AND Password = @password ) AND Status = 'D';", connection, transaction))
                            {
                                idCommand.Parameters.AddWithValue("@playerid", _GenericPlayerCommunication.PlayerID);
                                idCommand.Parameters.AddWithValue("@password", _GenericPlayerCommunication.Password);
                                var dataTable = new DataTable();
                                Debug.WriteLine(GetQueryWithParameters(idCommand));
                                dataTable.Load(idCommand.ExecuteReader());
                                // if is active in another queue, remove from all the queues
                                if (dataTable.Rows.Count > 0)
                                {
                                    using (MySqlCommand updateCommand = new MySqlCommand("DELETE FROM matchpool WHERE playerID=( SELECT PlayerID FROM `hololive-official-cardgame`.`player` WHERE PlayerID =@playerid AND Password =@password ) AND Status='A' LIMIT 2; UPDATE matchroompool SET Status='R' WHERE PlayerID = ( SELECT PlayerID FROM `hololive-official-cardgame`.`player` WHERE PlayerID = @playerid AND Password = @password ) AND Status='D' LIMIT 2", connection, transaction))
                                    {
                                        updateCommand.Parameters.AddWithValue("@playerid", _GenericPlayerCommunication.PlayerID);
                                        updateCommand.Parameters.AddWithValue("@password", _GenericPlayerCommunication.Password);
                                        Debug.WriteLine(GetQueryWithParameters(updateCommand));
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
                                insertCommand.Parameters.AddWithValue("@playerid", _GenericPlayerCommunication.PlayerID);
                                insertCommand.ExecuteNonQuery();
                            }

                        }
                        //since has a room, get the room data
                        PlayerMatchRoom _PlayerMatchRoom = new PlayerMatchRoom();

                        using (MySqlCommand selectCommand = new MySqlCommand("SELECT * FROM `hololive-official-cardgame`.`matchroom` WHERE OwnerID = ( SELECT PlayerID FROM `hololive-official-cardgame`.`player` WHERE PlayerID =@playerid AND Password =@password )", connection, transaction))
                        {
                            selectCommand.Parameters.AddWithValue("@playerid", _GenericPlayerCommunication.PlayerID);
                            selectCommand.Parameters.AddWithValue("@password", _GenericPlayerCommunication.Password);
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
                            Debug.WriteLine(GetQueryWithParameters(selectCommand));

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
                        Console.WriteLine("Error: " + ex.Message);
                        transaction.Rollback();
                        return null;
                    }
                    return null;
                }
            }
        }

        public PlayerMatchRoom JoinMatchRoomQueue(GenericPlayerCommunication _GenericPlayerCommunication)
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
                            idCommand.Parameters.AddWithValue("@roomcode", _GenericPlayerCommunication.RequestData.description);

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
                            poolDataCommand.Parameters.AddWithValue("@playerid", _GenericPlayerCommunication.PlayerID);

                            pollsData.Load(poolDataCommand.ExecuteReader());
                        }

                        if (pollsData.Rows.Count == 0)
                        {
                            //the room exist, then remove player from all other queues
                            using (MySqlCommand updateCommand = new MySqlCommand("UPDATE matchpool SET Status='I' WHERE playerID = " + PlayerIDSQL + " AND Status='A'; UPDATE matchroompool SET Status='I' WHERE playerID = " + PlayerIDSQL + "  AND Status='R'", connection, transaction))
                            {
                                updateCommand.Parameters.AddWithValue("@playerid", _GenericPlayerCommunication.PlayerID);
                                updateCommand.Parameters.AddWithValue("@password", _GenericPlayerCommunication.Password);
                                updateCommand.ExecuteNonQuery();
                            }

                            //Insert user into the room
                            string insertQuery = "INSERT INTO `matchroompool` (`MRPID`, `PlayerID`, `Board`, `Status`, `MatchRoomID`, `RegDate`, `LastActionDate`) VALUES ((UUID(), @playerid, '0', 'A', @roomid, current_timestamp(), current_timestamp());";
                            using (MySqlCommand insertCommand = new MySqlCommand(insertQuery, connection, transaction))
                            {
                                //insertCommand.Parameters.AddWithValue("@mrpid", GenerateUuidV7());
                                insertCommand.Parameters.AddWithValue("@playerid", _GenericPlayerCommunication.PlayerID);
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
                        Console.WriteLine("Error: " + ex.Message);
                        transaction.Rollback();
                        return null;
                    }
                    return null;
                }
            }
        }

        public ReturnMessage DismissMatchRoom(GenericPlayerCommunication _GenericPlayerCommunication)
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
                            idCommand.Parameters.AddWithValue("@playerid", _GenericPlayerCommunication.PlayerID);
                            idCommand.Parameters.AddWithValue("@password", _GenericPlayerCommunication.Password);

                            dataT.Load(idCommand.ExecuteReader());
                            
                            //checking if there's someone else in the room to become the owner
                            if (dataT.Rows.Count > 0)
                            {
                                using (MySqlCommand Command = new MySqlCommand("SELECT * FROM `matchroompool` WHERE `MatchRoomID` = @roomid AND PlayerID != @ownerid ;", connection, transaction))
                                {
                                    Command.Parameters.AddWithValue("@roomid", dataT.Rows[0].Field<string>("RoomID"));
                                    Command.Parameters.AddWithValue("@ownerid", dataT.Rows[0].Field<int>("OwnerID"));
                                    var dataTable = new DataTable();
                                    Debug.WriteLine(GetQueryWithParameters(Command));
                                    dataTable.Load(Command.ExecuteReader());

                                    // if is active in another queue, remove from all the queues
                                    if (dataTable.Rows.Count != 0)
                                    {
                                        using (MySqlCommand updateCommand = new MySqlCommand("UPDATE `matchroom` SET `OwnerID` = @newowner WHERE `matchroom`.`RoomID` = @roomid; UPDATE matchroompool SET Status='I' WHERE playerID=( SELECT PlayerID FROM `hololive-official-cardgame`.`player` WHERE PlayerID =@playerid AND Password =@password ) AND Status='A' ", connection, transaction))
                                        {
                                            updateCommand.Parameters.AddWithValue("@newowner", dataTable.Rows[0].Field<int>("PlayerID"));
                                            updateCommand.Parameters.AddWithValue("@roomid", dataTable.Rows[0].Field<string>("MatchRoomID"));
                                            updateCommand.Parameters.AddWithValue("@playerid", _GenericPlayerCommunication.PlayerID);
                                            updateCommand.Parameters.AddWithValue("@password", _GenericPlayerCommunication.Password);
                                            Debug.WriteLine(GetQueryWithParameters(updateCommand));
                                            updateCommand.ExecuteNonQuery();
                                        }
                                        transaction.Commit();
                                        return new ReturnMessage("success");
                                    }
                                }
                            }
                            //we do not have another players in the room, so we can just eraseit
                            using (MySqlCommand updateCommand = new MySqlCommand("UPDATE `matchroom` SET `OwnerID` = '0' WHERE `matchroom`.`RoomID` =@roomid; UPDATE matchroompool SET Status='I' WHERE playerID=( SELECT PlayerID FROM `hololive-official-cardgame`.`player` WHERE PlayerID =@playerid AND Password =@password ) AND Status !='I' ", connection, transaction))
                            {
                                updateCommand.Parameters.AddWithValue("@roomid", dataT.Rows[0].Field<string>("RoomID"));
                                updateCommand.Parameters.AddWithValue("@playerid", _GenericPlayerCommunication.PlayerID);
                                updateCommand.Parameters.AddWithValue("@password", _GenericPlayerCommunication.Password);
                                Debug.WriteLine(GetQueryWithParameters(updateCommand));
                                updateCommand.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        return new ReturnMessage("success");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + ex.Message);
                        transaction.Rollback();
                        return null;
                    }
                    return null;
                }
            }
        }

        public ReturnMessage LeaveMatchRoom(GenericPlayerCommunication _GenericPlayerCommunication)
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
                            updateCommand.Parameters.AddWithValue("@playerid", _GenericPlayerCommunication.PlayerID);
                            updateCommand.Parameters.AddWithValue("@password", _GenericPlayerCommunication.Password);
                            updateCommand.ExecuteNonQuery();
                        }
                        transaction.Commit();
                        return new ReturnMessage("success");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + ex.Message);
                        transaction.Rollback();
                        return null;
                    }
                    return null;
                }
            }
        }
        public PlayerMatchRoom JoinTable(GenericPlayerCommunication _GenericPlayerCommunication)
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
                            idCommand.Parameters.AddWithValue("@playerid", _GenericPlayerCommunication.PlayerID);
                            idCommand.Parameters.AddWithValue("@password", _GenericPlayerCommunication.Password);
                            Debug.WriteLine(GetQueryWithParameters(idCommand));
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
                            idCommand.Parameters.AddWithValue("@playerid", _GenericPlayerCommunication.PlayerID);
                            idCommand.Parameters.AddWithValue("@password", _GenericPlayerCommunication.Password);
                            idCommand.Parameters.AddWithValue("@roomid", dataT.Rows[0].Field<string>("RoomID"));

                            Debug.WriteLine(GetQueryWithParameters(idCommand));
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
                            updateCommand.Parameters.AddWithValue("@playerid", _GenericPlayerCommunication.PlayerID);
                            updateCommand.Parameters.AddWithValue("@password", _GenericPlayerCommunication.Password);
                            updateCommand.Parameters.AddWithValue("@board", _GenericPlayerCommunication.RequestData.description);
                            updateCommand.Parameters.AddWithValue("@chair", TablePosition);

                            Debug.WriteLine(GetQueryWithParameters(updateCommand));
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
                        Console.WriteLine("Error: " + ex.Message);
                        transaction.Rollback();
                        return null;
                    }
                    return null;
                }
            }
        }

        public ReturnMessage LeaveTable(GenericPlayerCommunication _GenericPlayerCommunication)
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
                            updateCommand.Parameters.AddWithValue("@playerid", _GenericPlayerCommunication.PlayerID);
                            updateCommand.Parameters.AddWithValue("@password", _GenericPlayerCommunication.Password);
                            updateCommand.ExecuteNonQuery();
                        }
                        transaction.Commit();
                        return new ReturnMessage("success");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + ex.Message);
                        transaction.Rollback();
                        return null;
                    }
                    return null;
                }
            }
        }



        public ReturnMessage LockTable(GenericPlayerCommunication _GenericPlayerCommunication)
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
                            updateCommand.Parameters.AddWithValue("@playerid", _GenericPlayerCommunication.PlayerID);
                            updateCommand.Parameters.AddWithValue("@password", _GenericPlayerCommunication.Password);
                            updateCommand.ExecuteNonQuery();
                        }
                        transaction.Commit();
                        return new ReturnMessage("success");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + ex.Message);
                        transaction.Rollback();
                        return null;
                    }
                    return null;
                }
            }
        }



        public ReturnMessage UnlockTable(GenericPlayerCommunication _GenericPlayerCommunication)
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
                            updateCommand.Parameters.AddWithValue("@playerid", _GenericPlayerCommunication.PlayerID);
                            updateCommand.Parameters.AddWithValue("@password", _GenericPlayerCommunication.Password);
                            updateCommand.ExecuteNonQuery();
                        }
                        transaction.Commit();
                        return new ReturnMessage("success");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + ex.Message);
                        transaction.Rollback();
                        return null;
                    }
                    return null;
                }
            }
        }


        public PlayerMatchRoom UpdateRoom(GenericPlayerCommunication _GenericPlayerCommunication)
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
                            selectCommand.Parameters.AddWithValue("@playerid", _GenericPlayerCommunication.PlayerID);
                            selectCommand.Parameters.AddWithValue("@password", _GenericPlayerCommunication.Password);
                            Debug.WriteLine(GetQueryWithParameters(selectCommand));
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
                            Debug.WriteLine(GetQueryWithParameters(idCommand));
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
                        Console.WriteLine("Error: " + ex.Message);
                        transaction.Rollback();
                        return null;
                    }
                    return null;
                }
            }
        }




        // MATCH MAKER

        // we check if there's two players in the matchpool ready to play 'A' if so, get the oldest in the list and match with the one that just joined
        public List<PlayerInfo> CheckForAvaliablePlayers()
        {
            string PlayerIDSQL = "";

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
                            if (debug)
                                Debug.WriteLine(GetQueryWithParameters(selectCommand));

                            dataTable.Load(selectCommand.ExecuteReader());

                            if (dataTable.Rows.Count < 2)
                                return playerInfoList;


                            var dataT = new DataTable();
                            using (MySqlCommand SelectUserCommand = new MySqlCommand("SELECT * FROM player WHERE PlayerID IN (@p1, @p2);", connection, transaction))
                            {

                                SelectUserCommand.Parameters.AddWithValue("@p1", dataTable.Rows[0].Field<int>("PlayerID"));
                                SelectUserCommand.Parameters.AddWithValue("@p2", dataTable.Rows[1].Field<int>("PlayerID"));

                                if (debug)
                                    Debug.WriteLine(GetQueryWithParameters(SelectUserCommand));

                                dataT.Load(SelectUserCommand.ExecuteReader());
                            }

                            foreach (DataRow row in dataT.Rows)
                            {
                                var playerInfo = new PlayerInfo
                                {
                                    PlayerID = row.Field<int>("PlayerID"),
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
                        Console.WriteLine("Error: " + ex.Message);
                        transaction.Rollback();
                        return null;
                    }
                    return null;
                }
            }
        }


        public bool CofirmMatchForAvaliablePlayers(int p1, int p2)
        {
            string PlayerIDSQL = "";

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
                            if (debug)
                                Debug.WriteLine(GetQueryWithParameters(updateCommand));
                            updateCommand.ExecuteNonQuery();
                        }
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + ex.Message);
                        transaction.Rollback();
                    }
                    return false;
                }
            }
        }

        public List<List<Card>> GetMatchPlayersDeck(int p1, int p2)
        {
            string PlayerIDSQL = "";

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var dataTable = new DataTable();
                        using (MySqlCommand updateCommand = new MySqlCommand("SELECT * FROM `playerdeck` WHERE PlayerID IN (@p1, @p2) AND Status = 'A' LIMIT 2;", connection, transaction))
                        {
                            updateCommand.Parameters.AddWithValue("@p1", p1);
                            updateCommand.Parameters.AddWithValue("@p2", p2);

                            if (debug)
                                Debug.WriteLine(GetQueryWithParameters(updateCommand));

                            dataTable.Load(updateCommand.ExecuteReader());
                        }

                        List<Card> DeckP1 = new List<Card>();
                        foreach (string s in dataTable.Rows[0].Field<string>("MainDeck").Split(',')) {
                            DeckP1.Add(new Card() { cardNumber = s });
                        }
                        List<Card> CheerDeckP1 = new List<Card>();
                        foreach (string s in dataTable.Rows[0].Field<string>("CheerDeck").Split(','))
                        {
                            CheerDeckP1.Add(new Card() { cardNumber = s });
                        }

                        List<Card> OshiP1 = new List<Card>();
                        OshiP1.Add(new Card() { cardNumber = dataTable.Rows[0].Field<string>("OshiCard") });

                        List<Card> DeckP2 = new List<Card>();
                        foreach (string s in dataTable.Rows[1].Field<string>("MainDeck").Split(','))
                        {
                            DeckP2.Add(new Card() { cardNumber = s });
                        }
                        List<Card> CheerDeckP2 = new List<Card>();
                        foreach (string s in dataTable.Rows[1].Field<string>("CheerDeck").Split(','))
                        {
                            CheerDeckP2.Add(new Card() { cardNumber = s });
                        }

                        List<Card> OshiP2 = new List<Card>();
                        OshiP2.Add(new Card() { cardNumber = dataTable.Rows[1].Field<string>("OshiCard") });

                        transaction.Commit();
                        List<List<Card>> ret = new List<List<Card>>();
                        ret.Add(DeckP1);
                        ret.Add(CheerDeckP1);
                        ret.Add(OshiP1);
                        ret.Add(DeckP2);
                        ret.Add(CheerDeckP2);
                        ret.Add(OshiP2);
                        return ret;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + ex.Message);
                        transaction.Rollback();
                    }
                    return null;
                }
            }
        }


        //start getting player list
        private List<T> FetchData<T>(string query, int playerid, Func<DataRow, T> mapFunction)
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
                        Console.WriteLine("Error: " + ex.Message);
                        transaction.Rollback();
                        return null;
                    }
                }
            }
        }

        public List<PlayerItemBox> GetPlayerItemBox(int playerid)
        {
            string query = "SELECT * FROM playeritembox WHERE PlayerID=@playerid ORDER BY ExpirationDate DESC;";
            return FetchData<PlayerItemBox>(query, playerid, MapPlayerItemBox);
        }

        public List<PlayerMission> GetPlayerMission(int playerid)
        {
            string query = "SELECT * FROM playermissionlist WHERE PlayerID=@playerid ORDER BY ObtainedDate DESC;";
            return FetchData<PlayerMission>(query, playerid, MapPlayerMission);
        }

        public List<PlayerMessageBox> GetPlayerMessageBox(int playerid)
        {
            string query = "SELECT * FROM playermessagebox WHERE PlayerID=@playerid ORDER BY ObtainedDate DESC;";
            return FetchData<PlayerMessageBox>(query, playerid, MapPlayerMessageBox);
        }
        public List<PlayerBadge> GetPlayerBadges(int playerid)
        {
            string query = "SELECT * FROM playerbadgelist WHERE PlayerID=@playerid ORDER BY ObtainedDate DESC LIMIT 5;";
            return FetchData<PlayerBadge>(query, playerid, MapPlayerBadges);
        }
        public List<PlayerTitle> GetPlayerTitles(int playerid)
        {
            string query = "SELECT * FROM playertitle WHERE PlayerID=@playerid ORDER BY ObtainedDate DESC;";
            return FetchData<PlayerTitle>(query, playerid, MapPlayerTitles);
        }

        private PlayerItemBox MapPlayerItemBox(DataRow row)
        {
            return new PlayerItemBox
            {
                PlayerItemBoxID = row.Field<int>("PlayerItemBoxID"),
                PlayerID = row.Field<int>("PlayerID"),
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
                PlayerID = row.Field<int>("PlayerID"),
                MissionID = row.Field<int>("MissionID")
            };
        }

        private PlayerMessageBox MapPlayerMessageBox(DataRow row)
        {
            return new PlayerMessageBox
            {
                MessageID = row.Field<int>("MessageID"),
                PlayerID = row.Field<int>("PlayerID"),
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
                PlayerID = row.Field<int>("PlayerID"),
                Rank = row.Field<int>("Rank"),
                ObtainedDate = row.Field<DateTime>("ObtainedDate")
            };
        }
        private PlayerTitle MapPlayerTitles(DataRow row)
        {
            return new PlayerTitle
            {
                TitleID = row.Field<int>("TitleID"),
                PlayerID = row.Field<int>("PlayerID"),
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

            if (isValid){ return input;}
            else { return "error";}
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
            Console.WriteLine();

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
                Console.WriteLine();
            }
        }
        /*
        public static string GenerateUuidV7()
        {
            // Get current timestamp in milliseconds since Unix epoch
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Generate a random 48-bit value
            byte[] randomBytes = new byte[6];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            // Create the UUIDv7 byte array
            byte[] uuidBytes = new byte[16];

            // Copy timestamp (48 bits) into the first 6 bytes
            BitConverter.GetBytes(timestamp).CopyTo(uuidBytes, 0);

            // Insert version (0x7) into the 7th byte
            uuidBytes[6] = (byte)((uuidBytes[6] & 0x0F) | 0x70);

            // Copy random bytes into the last 6 bytes
            Array.Copy(randomBytes, 0, uuidBytes, 8, 6);

            // Create the UUID string in the standard format
            Guid uuid = new Guid(uuidBytes);
            return uuid.ToString();
        }*/


        public class ReturnMessage
        {
            public string RequestReturn { get; set; }

            public ReturnMessage(string returnMessage)
            {
                this.RequestReturn = returnMessage;
            }
            void SetReturnMessage(string returnMessage)
            {
                this.RequestReturn = returnMessage;
            }
        }
    }
}