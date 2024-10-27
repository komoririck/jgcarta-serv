﻿using System.Collections.Concurrent;
using System.Net.WebSockets;
using static hololive_oficial_cardgame_server.MatchRoom;
using System.Text.Json;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    internal class ReSetCardAtStageHandler
    {
        private ConcurrentDictionary<string, WebSocket> playerConnections;
        private List<MatchRoom> matchRooms;

        public ReSetCardAtStageHandler(ConcurrentDictionary<string, WebSocket> playerConnections, List<MatchRoom> matchRooms)
        {
            this.playerConnections = playerConnections;
            this.matchRooms = matchRooms;
        }

        internal async Task MainDoActionRequestReSetCardAtStageHandleAsync(PlayerRequest playerRequest, WebSocket webSocket)
        {
            int matchnumber = FindPlayerMatchRoom(matchRooms, playerRequest.playerID);
            MatchRoom cMatchRoom = matchRooms[matchnumber];

            //if not the current player, return, since only at the begnning of a new turn a player can reSet a card at the main stage
            if (int.Parse(playerRequest.playerID) != cMatchRoom.currentPlayerTurn)
                return;

            if (cMatchRoom.currentGamePhase != GAMEPHASE.ResetStepReSetStage)
                return;

            DuelAction _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestData.extraRequestObject);
            Card currentStageCard = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerAStage : cMatchRoom.playerBStage;
            List<Card> currentBackStageCard = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerABackPosition : cMatchRoom.playerBBackPosition;

            // if player stage is null, proced 
            if (currentStageCard != null)
                    return;

            //checking if player has the card he send to the server at backstage 
            int i = -1;
            for (int n = 0; n < currentBackStageCard.Count; n++)
            {
                if (_DuelAction.playedFrom.Equals(currentBackStageCard[n].cardPosition))
                {
                    i = n;
                    break;
                }
                i++;
            }

            if (i == -1)
                return;


            // adding card to the stage
            string position = currentBackStageCard[i].cardPosition;
            currentStageCard = currentBackStageCard[i];
            currentStageCard.cardPosition = "Stage";
            currentStageCard.suspended = false;

            //assigning the card to the new position and removing from the backstage
            if (cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer)
            {
                cMatchRoom.playerAStage = currentBackStageCard[i];
                cMatchRoom.playerAStage.cardPosition = "Stage";
                cMatchRoom.playerABackPosition.RemoveAt(i);
            }
            else
            {
                cMatchRoom.playerBStage = currentBackStageCard[i];
                cMatchRoom.playerBStage.cardPosition = "Stage";
                cMatchRoom.playerBBackPosition.RemoveAt(i);
            }

            DuelAction duelAction = new DuelAction
            {
                playerID = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.firstPlayer : cMatchRoom.secondPlayer,
                usedCard = currentStageCard,
                playedFrom = position,
                actionType = "ReSetStage"
            };

            cMatchRoom.currentGamePhase = GAMEPHASE.DrawStep;

            RequestData _ReturnData = new RequestData { type = "GamePhase", description = "ReSetStage", requestObject = JsonSerializer.Serialize(duelAction, Lib.options) };

            Lib.SendMessage(playerConnections[cMatchRoom.firstPlayer.ToString()], _ReturnData);
            Lib.SendMessage(playerConnections[cMatchRoom.secondPlayer.ToString()], _ReturnData);

            cMatchRoom.currentGameHigh++;
            return;
        }
    }
}