using System.Collections.Concurrent;
using System.Net.WebSockets;
using static hololive_oficial_cardgame_server.SerializableObjects.MatchRoom;
using System.Text.Json;
using hololive_oficial_cardgame_server.SerializableObjects;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    internal class CheerChooseRequestHolomemDownHandler
    {
        private object _DuelAction;
        private PlayerRequest _ReturnData;

        internal async Task CheerChooseRequestHolomemDownHandleAsync(PlayerRequest playerRequest, MatchRoom cMatchRoom)
        {
                DuelAction _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestObject);

            //we need to check the cheer count to see if the player can draw and assign a cheer, if he cant, the client send the call with no information, so we need to skip the validations
            int cheerCount = cMatchRoom.currentPlayerTurn == playerRequest.playerID ? cMatchRoom.playerACardCheer.Count : cMatchRoom.playerBCardCheer.Count;
            if (cheerCount > 0)
            {
                if (playerRequest.playerID != GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn))
                        return;

                if (cMatchRoom.currentGamePhase != GAMEPHASE.HolomemDefeatedCheerChoose)
                    return;

                // if  player calling this is the current player something may be wrong!!!
                if (cMatchRoom.currentGamePhase == GAMEPHASE.HolomemDefeatedCheerChoose)
                {
                    if (playerRequest.playerID == cMatchRoom.currentPlayerTurn)
                        return;
                }

                bool hasAttached = false;

                if (cMatchRoom.playerA.PlayerID == playerRequest.playerID)
                {
                    cMatchRoom.playerAHand.RemoveAt(cMatchRoom.playerAHand.Count - 1);
                    hasAttached = cMatchRoom.AssignEnergyToZone(_DuelAction, cMatchRoom.playerAStage, cMatchRoom.playerACollaboration, cMatchRoom.playerABackPosition); // we are saving attached to the list only the name of the Cheer, add other information later i needded 
                }
                if (cMatchRoom.playerB.PlayerID == playerRequest.playerID)
                {
                    cMatchRoom.playerBHand.RemoveAt(cMatchRoom.playerBHand.Count - 1);
                    hasAttached = cMatchRoom.AssignEnergyToZone(_DuelAction, cMatchRoom.playerBStage, cMatchRoom.playerBCollaboration, cMatchRoom.playerBBackPosition);
                }

                if (!hasAttached)
                    return;
            }
            else
            {
                // we need to send a list with the same amount of objects as the draw needed, so the client can use in a .count of the draw list
                _DuelAction = new();
            }

            _ReturnData = new PlayerRequest { type = "DuelUpdate", description = "CheerStepEndDefeatedHolomem", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.jsonOptions) };


            cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayers(), _ReturnData));
            cMatchRoom.PushPlayerAnswer();


            cMatchRoom.currentGameHigh++;

            //if player need to assign more
            //, for exemple, because of a buzzholomemdown, we change the phase acordinly, else we send to mainstep
            if (cMatchRoom.cheersAssignedThisChainAmount < cMatchRoom.cheersAssignedThisChainTotal) {
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