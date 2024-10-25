using MySqlX.XDevAPI.Common;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using static hololive_oficial_cardgame_server.MatchRoom;
using System.Text;
using Microsoft.OpenApi.Extensions;
using System.Text.Json;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    internal class ResetRequestHandler : Lib
    {
        private ConcurrentDictionary<string, WebSocket> playerConnections;
        private List<MatchRoom> matchRooms;
        private RequestData _ReturnData;

        public ResetRequestHandler(ConcurrentDictionary<string, WebSocket> playerConnections, List<MatchRoom> matchRooms)
        {
            this.playerConnections = playerConnections;
            this.matchRooms = matchRooms;
        }

        internal async Task ResetRequestHandleAsync(PlayerRequest playerRequest, WebSocket webSocket)
        {
            int matchnumber = MatchRoom.FindPlayerMatchRoom(matchRooms, playerRequest.playerID);
            MatchRoom cMatchRoom = matchRooms[matchnumber];
            int playerA = cMatchRoom.firstPlayer;
            int playerB = cMatchRoom.secondPlayer;

            if (int.Parse(playerRequest.playerID) != cMatchRoom.currentPlayerTurn)
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

            if (!string.IsNullOrEmpty(currentCollabCardd.cardNumber))
            { // client send null when theres no card in the collab

                List<bool> places = GetBackStageAvailability(currentBackStageCardd);

                currentCollabCardd.suspended = true;

                string locall = AssignCardToBackStage(places, currentBackStageCardd, currentCollabCardd);
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
                currentCollabCardd = null;
            }

            //validating if player need to ReSet his card at main stage position
            Card card = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerAStage : cMatchRoom.playerBStage;

            if (card != null)
                cMatchRoom.currentGamePhase = GAMEPHASE.DrawStep;
            else
                cMatchRoom.currentGamePhase = GAMEPHASE.ResetStepReSetStage;


            //cleaning effects that should end at the end of the turn
            CollabEffects.currentActivatedTurnEffect.Clear();

           _ReturnData = new RequestData { type = "GamePhase", description = "ResetStep", requestObject = JsonSerializer.Serialize(duelAction, Lib.options) };

            Lib.SendMessage(playerConnections[cMatchRoom.firstPlayer.ToString()], _ReturnData);
            Lib.SendMessage(playerConnections[cMatchRoom.secondPlayer.ToString()], _ReturnData);

            cMatchRoom.currentGameHigh++;
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