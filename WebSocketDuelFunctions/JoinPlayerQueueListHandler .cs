using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static hololive_oficial_cardgame_server.MatchRoom;

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
           

            if (playerConnections.ContainsKey(playerRequest.playerID))
                return;

            // Add the WebSocket connection to the dictionary with player ID as key
            playerConnections[playerRequest.playerID] = webSocket;

            // create the user response
            RequestData returnData = new() { type = "Waitingforopponent", description = "Waitingforopponent", requestObject = "" };

            // Send the data as a text message
            Lib.SendMessage(webSocket, returnData);

            // Add the WebSocket connection to the dictionary with player ID as key
            playerConnections[playerRequest.playerID] = webSocket;

            List<PlayerInfo> pList = new DBConnection().CheckForAvaliablePlayers();

            if (pList.Count < 2)
            {
                returnData.type = "processing";
                returnData.description = "cod:422446";
                Lib.SendMessage(webSocket, returnData);
                return;

            }

            if (pList[0].PlayerID != int.Parse(playerRequest.playerID) && pList[1].PlayerID != int.Parse(playerRequest.playerID))
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
            returnData = new RequestData { type = "matchFound", description = "matchFound", requestObject = "matchFound" };
            Lib.SendMessage(playerConnections[pList[0].PlayerID.ToString()], returnData);
            Lib.SendMessage(playerConnections[pList[1].PlayerID.ToString()], returnData);


            List<List<Card>> decksinfo = new DBConnection().GetMatchPlayersDeck(pList[0].PlayerID, pList[1].PlayerID);
            if (!(decksinfo.Count == 6))
            {
                Lib.WriteConsoleMessag($"not able to get players decks");
                return;
            }

            var matchRoom = CreateMatchRoom(pList);
            _MatchRooms.Add(matchRoom);

            await NotifyPlayersOfMatch(pList, matchRoom);

            // Handle drawing cards and sending initial hands
            await SetupInitialHands(matchRoom);
        }

        private MatchRoom CreateMatchRoom(List<PlayerInfo> players)
        {
            var decksinfo = new DBConnection().GetMatchPlayersDeck(players[0].PlayerID, players[1].PlayerID);

            if (!(decksinfo.Count == 6))
            {
                Lib.WriteConsoleMessag($"not able to get players decks");
                return null;
            }

            return new MatchRoom
            {
                playerA = players[0],
                playerB = players[1],
                startPlayer = players[0].PlayerID,
                currentPlayerActing = players[0].PlayerID,
                currentPlayerTurn = players[0].PlayerID,
                firstPlayer = players[0].PlayerID,
                secondPlayer = players[1].PlayerID,
                currentGamePhase = GAMEPHASE.StartMatch,
                nextGamePhase = GAMEPHASE.DrawStep,
                playerADeck = decksinfo[0],
                playerACardCheer = decksinfo[1],
                playerAOshi = decksinfo[2][0],
                playerBDeck = decksinfo[3],
                playerBCardCheer = decksinfo[4],
                playerBOshi = decksinfo[5][0],
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
            var matchData = new RequestData { type = "matchFound", description = "matchFound", requestObject = "", extraRequestObject = ""};
            Lib.SendMessage(playerConnections[players[0].PlayerID.ToString()], matchData);
            Lib.SendMessage(playerConnections[players[1].PlayerID.ToString()], matchData);
        }

        private async Task SetupInitialHands(MatchRoom cMatchRoom)
        {
            Draw draw;
            Draw drawDummy;
            List<Record> cardlist;
            DuelFieldData _DuelFieldDataA = new();
            DuelFieldData _DuelFieldDataB = new();
            RequestData pReturnData = new();

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
            pReturnData = new RequestData { type = "goToRoom", description = "goToRoom", requestObject = JsonSerializer.Serialize(_DuelFieldDataA, options) };
             Lib.SendMessage(playerConnections[cMatchRoom.startPlayer.ToString()], pReturnData);

            pReturnData = new RequestData { type = "goToRoom", description = "goToRoom", requestObject = JsonSerializer.Serialize(_DuelFieldDataB, options) };
             Lib.SendMessage(playerConnections[cMatchRoom.secondPlayer.ToString()], pReturnData);


            cMatchRoom.currentGameHigh = 1;
            //END OF LOADING GAME SCENE


            //GET PLAYERS STARTER HAND
            Lib.getCardFromDeck(cMatchRoom.playerADeck, cMatchRoom.playerAHand, 7);
            Lib.getCardFromDeck(cMatchRoom.playerBDeck, cMatchRoom.playerBHand, 7);


            //BEGIN - SEND FIRST PLAYER STARTER HAND 
            draw = new Draw()
            {
                playerID = cMatchRoom.startPlayer,
                suffle = false,
                zone = "Deck",
                cardList = cMatchRoom.playerAHand
            };
            pReturnData = new RequestData { type = "duelUpdate", description = "InitialDraw", requestObject = JsonSerializer.Serialize(draw, options) };
            Lib.WriteConsoleMessag(pReturnData.requestObject);
             Lib.SendMessage(playerConnections[cMatchRoom.startPlayer.ToString()], pReturnData);

            draw.cardList = cMatchRoom.FillCardListWithEmptyCards(cMatchRoom.playerAHand);
            pReturnData = new RequestData { type = "duelUpdate", description = "InitialDraw", requestObject = JsonSerializer.Serialize(draw, options) };
            Lib.WriteConsoleMessag(pReturnData.requestObject);
             Lib.SendMessage(playerConnections[cMatchRoom.secondPlayer.ToString()], pReturnData);

            cMatchRoom.currentGameHigh = 2;

            //P2 START HAND
            draw = new Draw()
            {
                playerID = GetOtherPlayer(cMatchRoom, cMatchRoom.startPlayer),
                suffle = false,
                zone = "Deck",
                cardList = cMatchRoom.playerBHand
            };

            pReturnData = new RequestData { type = "duelUpdate", description = "InitialDrawP2", requestObject = JsonSerializer.Serialize(draw, options) };
            Lib.WriteConsoleMessag(pReturnData.requestObject);
             Lib.SendMessage(playerConnections[cMatchRoom.playerB.PlayerID.ToString()], pReturnData);

            draw.cardList = cMatchRoom.FillCardListWithEmptyCards(cMatchRoom.playerBHand);
            pReturnData = new RequestData { type = "duelUpdate", description = "InitialDrawP2", requestObject = JsonSerializer.Serialize(draw, options) };
            Lib.WriteConsoleMessag(pReturnData.requestObject);
             Lib.SendMessage(playerConnections[cMatchRoom.playerA.PlayerID.ToString()], pReturnData);

            cMatchRoom.currentGameHigh = 3;

            cMatchRoom.playerAGameHigh = cMatchRoom.currentGameHigh;
            cMatchRoom.playerBGameHigh = cMatchRoom.currentGameHigh;
        }
    }
}
