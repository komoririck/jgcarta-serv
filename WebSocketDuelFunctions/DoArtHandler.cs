﻿using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using hololive_oficial_cardgame_server.EffectControllers;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    internal class MainDoActionRequestDoArtHandler
    {
        private ConcurrentDictionary<string, WebSocket> playerConnections;
        private List<MatchRoom> matchRooms;
        private DuelAction _DuelAction;
        private RequestData pReturnData;




        public MainDoActionRequestDoArtHandler(ConcurrentDictionary<string, WebSocket> playerConnections, List<MatchRoom> matchRooms)
        {
            this.playerConnections = playerConnections;
            this.matchRooms = matchRooms;
        }

        internal async Task MainDoActionRequestDoArtHandleAsync(PlayerRequest playerRequest, WebSocket webSocket)
        {
            Card currentCollabOponnentCard = null;
            Card currentOponnentCard = null;
            Card currentStageOponnentCard = null;

            int matchnumber = MatchRoom.FindPlayerMatchRoom(matchRooms, playerRequest.playerID);
            MatchRoom cMatchRoom = matchRooms[matchnumber];

            _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestData.extraRequestObject);

            Card currentStageCard = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerAStage : cMatchRoom.playerBStage;
            Card currentCollabCard = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerACollaboration : cMatchRoom.playerBCollaboration;

            bool validCard = false;

            if (_DuelAction.usedCard.cardPosition.Equals("Stage"))
                if (currentStageCard.cardNumber.Equals(_DuelAction.usedCard.cardNumber))
                    validCard = true;


            if (_DuelAction.usedCard.cardPosition.Equals("Collaboration"))
                if (currentCollabCard.cardNumber.Equals(_DuelAction.usedCard.cardNumber))
                    validCard = true;


            if ((_DuelAction.usedCard.cardPosition.Equals("Stage") && cMatchRoom.centerStageArtUsed) || (_DuelAction.usedCard.cardPosition.Equals("Collaboration") && cMatchRoom.collabStageArtUsed))
                validCard = false;


            if (!validCard)
                return;

            validCard = false;

            currentStageOponnentCard = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerBStage : cMatchRoom.playerAStage;
            currentCollabOponnentCard = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerBCollaboration : cMatchRoom.playerACollaboration;

            if (currentStageOponnentCard.cardNumber.Equals(_DuelAction.targetCard.cardNumber))
            {
                currentOponnentCard = currentStageOponnentCard;
                validCard = true;
            }
            else
            if (currentCollabOponnentCard.cardNumber.Equals(_DuelAction.targetCard.cardNumber))
            {
                currentOponnentCard = currentCollabOponnentCard;
                validCard = true;
            }


            if (!validCard)
                return;

            Art usedArt = new();

            _DuelAction.targetCard.GetCardInfo(_DuelAction.targetCard.cardNumber);
            _DuelAction.usedCard.GetCardInfo(_DuelAction.usedCard.cardNumber);

            foreach (Art art in _DuelAction.usedCard.Arts)
            {
                if (art.Name.Equals(_DuelAction.selectedSkill))
                {
                    usedArt = art;
                    break;
                }
            }

            //check the attacking effect
            cMatchRoom.currentArtResolving = usedArt;
            cMatchRoom.currentCardResolving = _DuelAction.usedCard.cardNumber;
            cMatchRoom.extraInfo.Add(JsonSerializer.Serialize(_DuelAction, Lib.options));
            ArtEffects.OnArtEffectResolutionAsync(playerConnections, MessageDispatcher._MatchRooms, playerRequest, webSocket);


            cMatchRoom.currentGameHigh++;
            return;
        }
    }
}