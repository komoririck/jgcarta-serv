using System.Collections.Concurrent;
using System.Net.WebSockets;
using static hololive_oficial_cardgame_server.SerializableObjects.MatchRoom;
using System.Text.Json;
using hololive_oficial_cardgame_server.EffectControllers;
using hololive_oficial_cardgame_server.SerializableObjects;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    internal class ResetRequestHandler : Lib
    {
        private PlayerRequest _ReturnData;


        internal async Task ResetRequestHandleAsync(PlayerRequest playerRequest, MatchRoom cMatchRoom)
        {
            if (playerRequest.playerID != cMatchRoom.currentPlayerTurn)
            {
                Lib.WriteConsoleMessage("Wrong player calling");
                return;
            }

            if (cMatchRoom.currentGamePhase != GAMEPHASE.ResetStep)
            {
                Lib.WriteConsoleMessage("Wrong game phase to be called");
                return;
            }

            DuelAction duelAction = new DuelAction() { playerID = cMatchRoom.firstPlayer };

            Card currentStageCardd = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerAStage : cMatchRoom.playerBStage;
            Card currentCollabCardd = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerACollaboration : cMatchRoom.playerBCollaboration;
            List<Card> currentBackStageCardd = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerABackPosition : cMatchRoom.playerBBackPosition;


            ResetCardTurnStatusForPlayer(currentBackStageCardd, currentStageCardd, currentCollabCardd);

            if (currentCollabCardd != null)
            { // client send null when theres no card in the collab

                List<bool> places = GetBackStageAvailability(currentBackStageCardd);

                currentCollabCardd.suspended = true;

                string locall = AssignCardToBackStage(places, cMatchRoom);
                if (locall.Equals("failToAssignToBackStage")) {
                    WriteConsoleMessage("Error assign the card to the backposition");
                    return;
                }

                duelAction = new DuelAction
                {
                    playerID = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.firstPlayer : cMatchRoom.secondPlayer,
                    usedCard = currentCollabCardd,
                    playedFrom = "Collaboration",
                    actionType = "UndoCollab"
                };
                duelAction.usedCard.cardPosition = locall;

                if (cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID)
                    cMatchRoom.playerACollaboration = null;
                else
                    cMatchRoom.playerBCollaboration = null;
            }

            //validating if player need to ReSet his card at main stage position
            Card card = cMatchRoom.currentPlayerTurn.Equals(cMatchRoom.firstPlayer) ? cMatchRoom.playerAStage : cMatchRoom.playerBStage;

            if (card != null)
                cMatchRoom.currentGamePhase = GAMEPHASE.DrawStep;
            else
                cMatchRoom.currentGamePhase = GAMEPHASE.ResetStepReSetStage;


            //cleaning effects that should end at the end of the turn
            cMatchRoom.ActiveEffects.Clear();

            //string whichStep = cMatchRoom.currentGamePhase == GAMEPHASE.ResetStepReSetStage ? "ReSetStage" : "ResetStep";

            _ReturnData = new PlayerRequest { type = "DuelUpdate", description = "ResetStep", requestObject = JsonSerializer.Serialize(duelAction, Lib.options) };

            await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer.ToString()], _ReturnData);
            await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer.ToString()], _ReturnData);

            cMatchRoom.currentGameHigh++;

            cMatchRoom.StopTimer(MatchRoom.GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn));
            cMatchRoom.StartOrResetTimer(cMatchRoom.currentPlayerTurn.ToString(), enduel => Lib.EndDuelAsync(cMatchRoom, MatchRoom.GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn)));
        }


        void ResetCardTurnStatusForPlayer(List<Card> backstg, Card stage, Card collab)
        {
            foreach (Card eachCard in backstg)
            {
                eachCard.playedThisTurn = false;
                if (eachCard.suspended)
                    eachCard.suspended = false;
            }
            if (stage != null)
            {      
                stage.playedThisTurn = false;
                if (stage.suspended)
                    stage.suspended = false;
            }
            if (collab != null) { 
                collab.playedThisTurn = false;
                if (collab.suspended)
                    collab.suspended = false;
            }
        }
    }
}