using MySqlX.XDevAPI.Common;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using static hololive_oficial_cardgame_server.MatchRoom;
using System.Text;
using Microsoft.OpenApi.Extensions;
using System.Text.Json;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    internal class ResetRequestHandler
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
                return;

            if (cMatchRoom.currentGamePhase != GAMEPHASE.ResetStep)
                return;

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

            if (!string.IsNullOrEmpty(card.cardNumber))
                cMatchRoom.currentGamePhase = GAMEPHASE.DrawStep;
            else
                cMatchRoom.currentGamePhase = GAMEPHASE.ResetStepReSetStage;


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
            stage.playedThisTurn = false;
            if (stage.suspended)
                stage.suspended = false;
            collab.playedThisTurn = false;
            if (collab.suspended)
                collab.suspended = false;
        }



        string AssignCardToBackStage(List<bool> places, List<Card> backPosition, Card collaborationCard)
        {
            for (int i = 0; i < places.Count; i++)
            {
                if (!places[i])
                {
                    collaborationCard.cardPosition = $"BackStage{i + 1}";
                    backPosition.Add(collaborationCard);
                    return $"BackStage{i + 1}";
                }
            }
            return "failToAssignToBackStage";
        }


        List<bool> GetBackStageAvailability(List<Card> backPosition)
        {
            List<bool> places = new List<bool> { false, false, false, false, false };
            foreach (Card _card in backPosition)
            {
                switch (_card.cardPosition)
                {
                    case "BackStage1":
                        places[0] = true;
                        break;
                    case "BackStage2":
                        places[1] = true;
                        break;
                    case "BackStage3":
                        places[2] = true;
                        break;
                    case "BackStage4":
                        places[3] = true;
                        break;
                    case "BackStage5":
                        places[4] = true;
                        break;
                }
            }
            return places;
        }
    }
}