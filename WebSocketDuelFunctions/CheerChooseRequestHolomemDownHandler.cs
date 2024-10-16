using System.Collections.Concurrent;
using System.Net.WebSockets;
using static hololive_oficial_cardgame_server.MatchRoom;
using System.Text.Json;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    internal class CheerChooseRequestHolomemDownHandler
    {
        public CheerChooseRequestHolomemDownHandler(ConcurrentDictionary<string, WebSocket> playerConnections, List<MatchRoom> matchRooms)
        {
            this.playerConnections = playerConnections;
            this.matchRooms = matchRooms;
        }
        private ConcurrentDictionary<string, WebSocket> playerConnections;
        private List<MatchRoom> matchRooms;
        private object _DuelAction;
        private RequestData _ReturnData;

        internal async Task CheerChooseRequestHolomemDownHandleAsync(PlayerRequest playerRequest, WebSocket webSocket)
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
                if (int.Parse(playerRequest.playerID) != GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn))
                        return;

                if (cMatchRoom.currentGamePhase != GAMEPHASE.HolomemDefeatedCheerChoose)
                    return;

                // if  player calling this is the current player something may be wrong!!!
                if (cMatchRoom.currentGamePhase == GAMEPHASE.HolomemDefeatedCheerChoose)
                {
                    if (int.Parse(playerRequest.playerID) == cMatchRoom.currentPlayerTurn)
                        return;
                }

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
                // we need to send a list with the same amount of objects as the draw needed, so the client can use in a .count of the draw list
                _DuelAction = new();
            }

            _ReturnData = new RequestData { type = "GamePhase", description = "CheerStepEndDefeatedHolomem", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };

            Lib.SendMessage(playerConnections[cMatchRoom.firstPlayer.ToString()], _ReturnData);
            Lib.SendMessage(playerConnections[cMatchRoom.secondPlayer.ToString()], _ReturnData);


            cMatchRoom.currentGameHigh++;

            //if player need to assign more energy, for exemple, because of a buzzholomemdown, we change the phase acordinly, else we send to mainstep
            if (cMatchRoom.cheersAssignedThisChainTotal > 1 && cMatchRoom.cheersAssignedThisChainAmount < cMatchRoom.cheersAssignedThisChainTotal) {
                    cMatchRoom.currentGamePhase = GAMEPHASE.HolomemDefeatedCheerChoose;
                    cMatchRoom.cheersAssignedThisChainAmount++;
            }

            if (cMatchRoom.cheersAssignedThisChainAmount == cMatchRoom.cheersAssignedThisChainTotal) { 
                cMatchRoom.currentGamePhase = GAMEPHASE.MainStep;
                cMatchRoom.cheersAssignedThisChainAmount = 0;
                cMatchRoom.cheersAssignedThisChainTotal = 0;
            }
        }
    }
}