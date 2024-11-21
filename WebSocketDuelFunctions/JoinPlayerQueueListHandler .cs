using hololive_oficial_cardgame_server.SerializableObjects;
using MySqlX.XDevAPI.CRUD;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using static hololive_oficial_cardgame_server.SerializableObjects.MatchRoom;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    public class JoinPlayerQueueListHandler
    {
        private readonly ConcurrentDictionary<string, WebSocket> playerConnections;
        private readonly List<MatchRoom> _MatchRooms;
        JsonSerializerOptions options = new JsonSerializerOptions {DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull};

        public JoinPlayerQueueListHandler(ConcurrentDictionary<string, WebSocket> connections, List<MatchRoom> matchRooms)
        {
            playerConnections = connections;
            _MatchRooms = matchRooms;
        }

        public async Task JoinPlayerQueueListHandleAsync(PlayerRequest playerRequest, WebSocket webSocket)
        {
            // Add the WebSocket connection to the dictionary with player ID as key
            playerConnections[playerRequest.playerID] = webSocket;

            // create the user response
            PlayerRequest returnData = new() { type = "Waitingforopponent", description = "Waitingforopponent", requestObject = "" };

            // Send the data as a text message
            Lib.SendMessage(webSocket, returnData);

            // Add the WebSocket connection to the dictionary with player ID as key
            playerConnections[playerRequest.playerID] = webSocket;


            var room = MatchRoom.FindPlayerMatchRoom(playerRequest.playerID);
            if (room != null) {
                RemoveRoom(room);
            }

            //getting avalible players from the pool
            List<PlayerInfo> pList = new DBConnection().CheckForAvaliablePlayers();

            if (pList.Count < 2)
            {
                returnData.type = "processing";
                returnData.description = "cod:422446";
                Lib.SendMessage(webSocket, returnData);
                return;

            }

            if (pList[0].PlayerID != playerRequest.playerID && pList[1].PlayerID != playerRequest.playerID)
            {
                returnData.type = "error";
                returnData.description = "cod:651152";
                Lib.SendMessage(webSocket, returnData);
                return;
            }

            if (!(playerConnections.ContainsKey(pList[0].PlayerID.ToString()) && playerConnections.ContainsKey(pList[1].PlayerID.ToString())))
            {
                returnData.type = "waiting";
                returnData.description = "cod:694353";
                Lib.SendMessage(webSocket, returnData);
                return;
            }

            // since we passed the verification, players are ready in the base and in the socket


            //we need to update the socket, so the players can be paired or enter the pool again:
            bool isPlayersnew = new DBConnection().LockPlayersForAMatch(pList[0].PlayerID, pList[1].PlayerID);

            if (!isPlayersnew)
                return;

            returnData = new PlayerRequest { type = "matchFound", description = "matchFound", requestObject = "matchFound" };
            Lib.SendMessage(MessageDispatcher.playerConnections[pList[0].PlayerID.ToString()], returnData);
            Lib.SendMessage(MessageDispatcher.playerConnections[pList[1].PlayerID.ToString()], returnData);


            var matchRoom = CreateMatchRoom(pList);
            _MatchRooms.Add(matchRoom);

            await NotifyPlayersOfMatch(pList, matchRoom);

            // Handle drawing cards and sending initial hands
            await SetupInitialHands(matchRoom);
            Lib.PrintPlayerHand(matchRoom);


            matchRoom.StartOrResetTimer(matchRoom.firstPlayer.ToString(), enduel => Lib.EndDuelAsync(matchRoom, MatchRoom.GetOtherPlayer(matchRoom, matchRoom.firstPlayer)));
            matchRoom.StartOrResetTimer(matchRoom.secondPlayer.ToString(), enduel => Lib.EndDuelAsync(matchRoom, MatchRoom.GetOtherPlayer(matchRoom, matchRoom.secondPlayer)));
        }

        private MatchRoom CreateMatchRoom(List<PlayerInfo> players)
        {
            var playerADeck = new DBConnection().GetMatchPlayersDeck(players[0].PlayerID);
            var playerBDeck = new DBConnection().GetMatchPlayersDeck(players[1].PlayerID);

            if (!(playerADeck.Count == 3) || !(playerBDeck.Count == 3))
            {
                Lib.WriteConsoleMessage($"not able to get players decks");
                return null;
            }

            return new MatchRoom
            {
                playerA = players[0],
                playerB = players[1],
                currentPlayerTurn = players[0].PlayerID,
                firstPlayer = players[0].PlayerID,
                secondPlayer = players[1].PlayerID,
                currentGamePhase = GAMEPHASE.StartMatch,
                nextGamePhase = GAMEPHASE.DrawStep,
                playerADeck = playerADeck[0],
                playerACardCheer = playerADeck[1],
                playerAOshi = playerADeck[2][0],
                playerBDeck = playerBDeck[0],
                playerBCardCheer = playerBDeck[1],
                playerBOshi = playerBDeck[2][0],
                playerAHand = new List<Card>(),
                playerAArquive = new List<Card>(),
                playerAHoloPower = new List<Card>(),
                playerABackPosition = new List<Card>(),
                playerBHand = new List<Card>(),
                playerBArquive = new List<Card>(),
                playerBHoloPower = new List<Card>(),
                playerBBackPosition = new List<Card>(),
            };
        }

        private async Task NotifyPlayersOfMatch(List<PlayerInfo> players, MatchRoom matchRoom)
        {
            var matchData = new PlayerRequest { type = "matchFound", description = "matchFound", requestObject = ""};
            Lib.SendMessage(MessageDispatcher.playerConnections[players[0].PlayerID.ToString()], matchData);
            Lib.SendMessage(MessageDispatcher.playerConnections[players[1].PlayerID.ToString()], matchData);
        }

        private async Task SetupInitialHands(MatchRoom cMatchRoom)
        {
            DuelAction draw;
            DuelAction drawDummy;
            List<Record> cardlist;
            DuelFieldData _DuelFieldDataA = new();
            DuelFieldData _DuelFieldDataB = new();
            PlayerRequest pReturnData = new();

            // SUFFLE ENERGY
            cMatchRoom.playerACardCheer = cMatchRoom.ShuffleCards(cMatchRoom.playerACardCheer);
            cMatchRoom.playerBCardCheer = cMatchRoom.ShuffleCards(cMatchRoom.playerBCardCheer);
            //SUFFLE DECK
            cMatchRoom.playerADeck = cMatchRoom.ShuffleCards(cMatchRoom.playerADeck);
            cMatchRoom.playerBDeck = cMatchRoom.ShuffleCards(cMatchRoom.playerBDeck);

            _DuelFieldDataA = new DuelFieldData
            {
                playerAFavourite = cMatchRoom.playerAFavourite,
                playerACardCheer = cMatchRoom.FillCardListWithEmptyCards(cMatchRoom.playerACardCheer),

                firstPlayer = cMatchRoom.firstPlayer,
                secondPlayer = cMatchRoom.secondPlayer,
                currentPlayerTurn = cMatchRoom.firstPlayer
            };

            _DuelFieldDataB = new DuelFieldData
            {
                playerBFavourite = cMatchRoom.playerBFavourite,
                playerBCardCheer = cMatchRoom.FillCardListWithEmptyCards(cMatchRoom.playerBCardCheer),

                firstPlayer = cMatchRoom.firstPlayer,
                secondPlayer = cMatchRoom.secondPlayer,
                currentPlayerTurn = cMatchRoom.firstPlayer
            };


            //since we were able to update the users table to lock the match, send both players to the match
            pReturnData = new PlayerRequest { type = "goToRoom", description = "goToRoom", requestObject = JsonSerializer.Serialize(_DuelFieldDataA, options) };
             Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer.ToString()], pReturnData);

            pReturnData = new PlayerRequest { type = "goToRoom", description = "goToRoom", requestObject = JsonSerializer.Serialize(_DuelFieldDataB, options) };
             Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer.ToString()], pReturnData);


            cMatchRoom.currentGameHigh = 1;
            //END OF LOADING GAME SCENE


            //GET PLAYERS STARTER HAND
            Lib.getCardFromDeck(cMatchRoom.playerADeck, cMatchRoom.playerAHand, 7);
            Lib.getCardFromDeck(cMatchRoom.playerBDeck, cMatchRoom.playerBHand, 7);


            //BEGIN - SEND FIRST PLAYER STARTER HAND 
            draw = new DuelAction()
            {
                playerID = cMatchRoom.firstPlayer,
                suffle = false,
                zone = "Deck",
                cardList = cMatchRoom.playerAHand
            };
            pReturnData = new PlayerRequest { type = "DuelUpdate", description = "InitialDraw", requestObject = JsonSerializer.Serialize(draw, options) };
            Lib.WriteConsoleMessage(pReturnData.requestObject);
             Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer.ToString()], pReturnData);

            draw.cardList = cMatchRoom.FillCardListWithEmptyCards(cMatchRoom.playerAHand);
            pReturnData = new PlayerRequest { type = "DuelUpdate", description = "InitialDraw", requestObject = JsonSerializer.Serialize(draw, options) };
            Lib.WriteConsoleMessage(pReturnData.requestObject);
             Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer.ToString()], pReturnData);

            cMatchRoom.currentGameHigh = 2;

            //P2 START HAND
            draw = new DuelAction()
            {
                playerID = cMatchRoom.secondPlayer,
                suffle = false,
                zone = "Deck",
                cardList = cMatchRoom.playerBHand
            };

            pReturnData = new PlayerRequest { type = "DuelUpdate", description = "InitialDrawP2", requestObject = JsonSerializer.Serialize(draw, options) };
            Lib.WriteConsoleMessage(pReturnData.requestObject);
             Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.playerB.PlayerID.ToString()], pReturnData);

            draw.cardList = cMatchRoom.FillCardListWithEmptyCards(cMatchRoom.playerBHand);
            pReturnData = new PlayerRequest { type = "DuelUpdate", description = "InitialDrawP2", requestObject = JsonSerializer.Serialize(draw, options) };
            Lib.WriteConsoleMessage(pReturnData.requestObject);
             Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.playerA.PlayerID.ToString()], pReturnData);

            cMatchRoom.currentGameHigh = 3;

            
            
        }
    }
}
