using MySqlX.XDevAPI.Common;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using static hololive_oficial_cardgame_server.MatchRoom;
using Microsoft.OpenApi.Extensions;
using System.Text.Json;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    internal class CheerChooseRequestHandler
    {
        private ConcurrentDictionary<string, WebSocket> playerConnections;
        private List<MatchRoom> matchRooms;
        private object _DuelAction;
        private RequestData _ReturnData;

        public CheerChooseRequestHandler(ConcurrentDictionary<string, WebSocket> playerConnections, List<MatchRoom> matchRooms)
        {
            this.playerConnections = playerConnections;
            this.matchRooms = matchRooms;
        }

        internal async Task CheerChooseRequestHandleAsync(PlayerRequest playerRequest, WebSocket webSocket)
        {
            int matchnumber = MatchRoom.FindPlayerMatchRoom(matchRooms, playerRequest.playerID);
            MatchRoom cMatchRoom = matchRooms[matchnumber];
            int playerA = cMatchRoom.firstPlayer;
            int playerB = cMatchRoom.secondPlayer;

            DuelAction _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestData.extraRequestObject);

            //we need to check the cheer count to see if the player can draw and assign a cheer, if he cant, the client send the call with no information, so we need to skip the validations
            int cheerCount = cMatchRoom.currentPlayerTurn == int.Parse(playerRequest.playerID) ? cMatchRoom.playerACardCheer.Count : cMatchRoom.playerBCardCheer.Count;
            if (cheerCount > 0)
            {

                if (cMatchRoom.currentGamePhase == GAMEPHASE.CheerStepChoose)
                {
                    if (int.Parse(playerRequest.playerID) != cMatchRoom.currentPlayerTurn)
                        return;
                }

                if (cMatchRoom.currentGamePhase != GAMEPHASE.CheerStepChoose)
                    return;

                bool hasAttached = false;

                if (cMatchRoom.playerA.PlayerID == int.Parse(playerRequest.playerID))
                {
                    cMatchRoom.playerAHand.RemoveAt(cMatchRoom.playerAHand.Count - 1);
                    hasAttached = Lib.GamePhaseCheerChoosedAsync(_DuelAction, cMatchRoom, cMatchRoom.playerAStage, cMatchRoom.playerACollaboration, cMatchRoom.playerABackPosition); // we are saving attached to the list only the name of the Cheer, add other information later i needded 
                }
                if (cMatchRoom.playerB.PlayerID == int.Parse(playerRequest.playerID))
                {
                    cMatchRoom.playerBHand.RemoveAt(cMatchRoom.playerBHand.Count - 1);
                    hasAttached = Lib.GamePhaseCheerChoosedAsync(_DuelAction, cMatchRoom, cMatchRoom.playerBStage, cMatchRoom.playerBCollaboration, cMatchRoom.playerBBackPosition);
                }

                if (!hasAttached)
                    return;
                }
            else
            {
                // we dont need to pass anything to the client if the user doesnt have energy, but lets just send some empty object
                _DuelAction = new();
            }
            _ReturnData = new RequestData { type = "GamePhase", description = "CheerStepEnd", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };

            Lib.SendMessage(playerConnections[cMatchRoom.firstPlayer.ToString()], _ReturnData);
            Lib.SendMessage(playerConnections[cMatchRoom.secondPlayer.ToString()], _ReturnData);

            cMatchRoom.currentGamePhase = GAMEPHASE.MainStep;
            cMatchRoom.currentGameHigh++;
        }
    }
}