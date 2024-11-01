using System.Collections.Concurrent;
using System.Net.WebSockets;
using static hololive_oficial_cardgame_server.SerializableObjects.MatchRoom;
using System.Text.Json;
using hololive_oficial_cardgame_server.SerializableObjects;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    internal class CheerChooseRequestHandler
    {
        private ConcurrentDictionary<string, WebSocket> playerConnections;
        private List<MatchRoom> matchRooms;
        private object _DuelAction;
        private PlayerRequest _ReturnData;

        public CheerChooseRequestHandler(ConcurrentDictionary<string, WebSocket> playerConnections, List<MatchRoom> matchRooms)
        {
            this.playerConnections = playerConnections;
            this.matchRooms = matchRooms;
        }

        internal async Task CheerChooseRequestHandleAsync(PlayerRequest playerRequest, WebSocket webSocket)
        {
            int matchnumber = MatchRoom.FindPlayerMatchRoom(matchRooms, playerRequest.playerID);
            MatchRoom cMatchRoom = matchRooms[matchnumber];

            DuelAction _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestObject);

            //we need to check the cheer count to see if the player can draw and assign a cheer, if he cant, the client send the call with no information, so we need to skip the validations
            int cheerCount = cMatchRoom.currentPlayerTurn == playerRequest.playerID ? cMatchRoom.playerACardCheer.Count : cMatchRoom.playerBCardCheer.Count;
            if (cheerCount > 0)
            {

                if (playerRequest.playerID != cMatchRoom.currentPlayerTurn)
                {
                    Lib.WriteConsoleMessage("Wrong player calling");
                    return;
                }

                if (cMatchRoom.currentGamePhase != GAMEPHASE.CheerStepChoose)
                {
                    Lib.WriteConsoleMessage("Wrong game phase to be called");
                    return;
                }

                bool hasAttached = false;

                if (cMatchRoom.playerA.PlayerID == playerRequest.playerID)
                {
                    cMatchRoom.playerAHand.RemoveAt(cMatchRoom.playerAHand.Count - 1);
                    hasAttached = Lib.AssignEnergyToZoneAsync(_DuelAction, cMatchRoom, cMatchRoom.playerAStage, cMatchRoom.playerACollaboration, cMatchRoom.playerABackPosition); // we are saving attached to the list only the name of the Cheer, add other information later i needded 
                }
                if (cMatchRoom.playerB.PlayerID == playerRequest.playerID)
                {
                    cMatchRoom.playerBHand.RemoveAt(cMatchRoom.playerBHand.Count - 1);
                    hasAttached = Lib.AssignEnergyToZoneAsync(_DuelAction, cMatchRoom, cMatchRoom.playerBStage, cMatchRoom.playerBCollaboration, cMatchRoom.playerBBackPosition);
                }

                if (!hasAttached) {
                    Lib.WriteConsoleMessage("Cannot attach the energy");
                    return;
                }
            }
            else
            {
                // we dont need to pass anything to the client if the user doesnt have energy, but lets just send some empty object
                _DuelAction = new();
            }
            _ReturnData = new PlayerRequest { type = "GamePhase", description = "CheerStepEnd", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };

            Lib.SendMessage(playerConnections[cMatchRoom.firstPlayer.ToString()], _ReturnData);
            Lib.SendMessage(playerConnections[cMatchRoom.secondPlayer.ToString()], _ReturnData);

            cMatchRoom.currentGamePhase = GAMEPHASE.MainStep;
            cMatchRoom.currentGameHigh++;
        }
    }
}