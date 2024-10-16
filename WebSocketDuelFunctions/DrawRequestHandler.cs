using MySqlX.XDevAPI.Common;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using static hololive_oficial_cardgame_server.MatchRoom;
using System.Text.Json;
using System.Text;
using Microsoft.OpenApi.Extensions;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    internal class DrawRequestHandler
    {
        private ConcurrentDictionary<string, WebSocket> playerConnections;
        private List<MatchRoom> matchRooms;
        private Task task;

        public DrawRequestHandler(ConcurrentDictionary<string, WebSocket> playerConnections, List<MatchRoom> matchRooms)
        {
            this.playerConnections = playerConnections;
            this.matchRooms = matchRooms;
        }

        internal async Task DrawRequestHandleAsync(PlayerRequest playerRequest, WebSocket webSocket)
        {
            int matchnumber = MatchRoom.FindPlayerMatchRoom(matchRooms, playerRequest.playerID);
            MatchRoom cMatchRoom = matchRooms[matchnumber];


            if (int.Parse(playerRequest.playerID) != cMatchRoom.currentPlayerTurn)
                return;

            if (cMatchRoom.currentGamePhase != GAMEPHASE.DrawStep)
                return;

            if (cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer)
            {
                if (cMatchRoom.playerADeck.Count == 0)
                    _ = Lib.EndDuelAsync(true, cMatchRoom);

                Lib.getCardFromDeck(cMatchRoom.playerADeck, cMatchRoom.playerAHand, 1);
                task = GamePhaseDrawAsync(cMatchRoom.firstPlayer, cMatchRoom.playerAHand, true, cMatchRoom);
            }
            else
            {
                if (cMatchRoom.playerBDeck.Count == 0)
                    _ = Lib.EndDuelAsync(true, cMatchRoom);

                Lib.getCardFromDeck(cMatchRoom.playerBDeck, cMatchRoom.playerBHand, 1);
                task = GamePhaseDrawAsync(cMatchRoom.secondPlayer, cMatchRoom.playerBHand, true, cMatchRoom);
            }

            cMatchRoom.currentGamePhase = GAMEPHASE.CheerStep;
            cMatchRoom.currentGameHigh++;
        }



        async Task GamePhaseDrawAsync(int playerid, List<Card> PlayerHand, Boolean result, MatchRoom mr)
        {
            Draw newDraw = new Draw();
            newDraw.playerID = playerid;
            newDraw.zone = "Deck";

            RequestData ReturnData = new RequestData { type = "GamePhase", description = "DrawPhase", requestObject = "" };

            newDraw.cardList = new List<Card>() { PlayerHand[PlayerHand.Count - 1] };
            ReturnData.requestObject = JsonSerializer.Serialize(newDraw, Lib.options);

            Lib.SendMessage(MessageDispatcher.playerConnections[newDraw.playerID.ToString()], ReturnData);

            newDraw.cardList = new List<Card>() { new Card() };
            ReturnData.requestObject = JsonSerializer.Serialize(newDraw, Lib.options);
            Lib.SendMessage(MessageDispatcher.playerConnections[GetOtherPlayer(mr, newDraw.playerID).ToString()], ReturnData);
        }


    }
}