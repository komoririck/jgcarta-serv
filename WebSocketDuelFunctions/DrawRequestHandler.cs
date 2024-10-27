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


            RequestData ReturnData = new RequestData { type = "GamePhase", description = "DrawPhase", requestObject = "" };
            if (cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer)
            {
                if (cMatchRoom.playerADeck.Count == 0)
                    _ = Lib.EndDuelAsync(true, cMatchRoom);

                Lib.getCardFromDeck(cMatchRoom.playerADeck, cMatchRoom.playerAHand, 1);
                task = Lib.AddTopDeckToDrawObjectAsync(cMatchRoom.firstPlayer, cMatchRoom.playerAHand, true, cMatchRoom, ReturnData);
            }
            else
            {
                if (cMatchRoom.playerBDeck.Count == 0)
                    _ = Lib.EndDuelAsync(true, cMatchRoom);

                Lib.getCardFromDeck(cMatchRoom.playerBDeck, cMatchRoom.playerBHand, 1);
                task = Lib.AddTopDeckToDrawObjectAsync(cMatchRoom.secondPlayer, cMatchRoom.playerBHand, true, cMatchRoom, ReturnData);
            }

            cMatchRoom.currentGamePhase = GAMEPHASE.CheerStep;
            cMatchRoom.currentGameHigh++;
        }
    }
}