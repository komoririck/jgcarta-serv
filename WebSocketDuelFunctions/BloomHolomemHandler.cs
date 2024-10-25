﻿using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    internal class BloomHolomemHandler
    {
        private ConcurrentDictionary<string, WebSocket> playerConnections;
        private List<MatchRoom> matchRooms;

        public BloomHolomemHandler(ConcurrentDictionary<string, WebSocket> playerConnections, List<MatchRoom> matchRooms)
        {
            this.playerConnections = playerConnections;
            this.matchRooms = matchRooms;
        }

        internal async Task MainDoActionRequestBloomHolomemHandleAsync(PlayerRequest playerRequest, WebSocket webSocket)
        {

            int handPos = -1;

            int matchnumber = MatchRoom.FindPlayerMatchRoom(matchRooms, playerRequest.playerID);
            MatchRoom cMatchRoom = matchRooms[matchnumber];


            DuelAction _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestData.extraRequestObject);

            if (_DuelAction.targetCard != null)
                _DuelAction.targetCard.GetCardInfo(_DuelAction.targetCard.cardNumber);
            if (_DuelAction.usedCard != null)
                _DuelAction.usedCard.GetCardInfo(_DuelAction.usedCard.cardNumber);
            if (_DuelAction.cheerCostCard != null)
                _DuelAction.cheerCostCard.GetCardInfo(_DuelAction.cheerCostCard.cardNumber);


            bool canContinueBloomHolomem = false;

            if (!_DuelAction.usedCard.bloomLevel.Equals("Debut") || !_DuelAction.usedCard.bloomLevel.Equals("1st"))
                canContinueBloomHolomem = true;

            if (!canContinueBloomHolomem)
                return;

            List<Record> validCardToBloom = FileReader.QueryBloomableCard(_DuelAction.targetCard.name, _DuelAction.targetCard.bloomLevel);

            //if not break possible to bloom, break
            if (validCardToBloom.Count < 1)
                return;

            //checking if the player has the card in the hand and getting the pos
            handPos = -1;
            if (int.Parse(playerRequest.playerID) == cMatchRoom.firstPlayer)
            {
                int nn = 0;
                foreach (Card inHand in cMatchRoom.playerAHand)
                {
                    if (inHand.cardNumber.Equals(_DuelAction.usedCard.cardNumber))
                        handPos = nn;
                    nn++;
                }
            }
            else
            {
                int nn = 0;
                foreach (Card inHand in cMatchRoom.playerBHand)
                {
                    if (inHand.cardNumber.Equals(_DuelAction.usedCard.cardNumber))
                        handPos = nn;
                    nn++;
                }
            }
            if (handPos == -1)
                return;

            int validCardPos = -1;
            int nnn = 0;
            foreach (Record record in validCardToBloom)
            {
                if (record.CardNumber.Equals(_DuelAction.usedCard.cardNumber))
                {
                    validCardPos = nnn;
                }
                nnn++;
            }

            if (validCardPos == -1)
                return;

            void bloomCard(Card cardToBloom, Card cardWithBloomInfo)
            {
                cardToBloom.bloomChild.Add(new Card() { cardNumber = cardToBloom.cardNumber });
                cardToBloom.cardNumber = cardWithBloomInfo.cardNumber;
                cardToBloom.GetCardInfo(cardToBloom.cardNumber);
            }

            switch (_DuelAction.local)
            {
                case "Stage":
                    if (int.Parse(playerRequest.playerID) == cMatchRoom.firstPlayer)
                    {
                        bloomCard(cMatchRoom.playerAStage, _DuelAction.usedCard);

                        cMatchRoom.playerAHand.RemoveAt(handPos);
                    }
                    else
                    {
                        bloomCard(cMatchRoom.playerBStage, _DuelAction.usedCard);
                        cMatchRoom.playerBHand.RemoveAt(handPos);
                    }
                    break;
                case "Collaboration":
                    if (int.Parse(playerRequest.playerID) == cMatchRoom.firstPlayer)
                    {
                        bloomCard(cMatchRoom.playerACollaboration, _DuelAction.usedCard);
                        cMatchRoom.playerAHand.RemoveAt(handPos);
                    }
                    else
                    {
                        bloomCard(cMatchRoom.playerBCollaboration, _DuelAction.usedCard);
                        cMatchRoom.playerBHand.RemoveAt(handPos);
                    }
                    break;
                case "BackStage1":
                case "BackStage2":
                case "BackStage3":
                case "BackStage4":
                case "BackStage5":
                    List<Card> actionCardList = new List<Card>();

                    if (int.Parse(playerRequest.playerID) == cMatchRoom.firstPlayer)
                        actionCardList = cMatchRoom.playerABackPosition;
                    else
                        actionCardList = cMatchRoom.playerBBackPosition;

                    int x = 0;
                    int y = 0;
                    foreach (Card cartinha in actionCardList)
                    {
                        if (cartinha.cardPosition.Equals(_DuelAction.local))
                        {
                            if (!cartinha.playedThisTurn)
                                x = y;
                        }
                        y++;
                    }

                    bloomCard(actionCardList[x], _DuelAction.usedCard);

                    if (int.Parse(playerRequest.playerID) == cMatchRoom.firstPlayer)
                        cMatchRoom.playerAHand.RemoveAt(handPos);
                    else
                        cMatchRoom.playerBHand.RemoveAt(handPos);


                    if (_DuelAction.actionType.Equals("BloomHolomem"))
                        return;

                    checkBloomEffect();
                    break;
            }
            RequestData pReturnData = new RequestData { type = "GamePhase", description = "UsedArt", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
            Lib.SendMessage(playerConnections[cMatchRoom.playerB.PlayerID.ToString()], pReturnData);
            Lib.SendMessage(playerConnections[cMatchRoom.playerA.PlayerID.ToString()], pReturnData);

            cMatchRoom.currentGameHigh++;
        }

        private void checkBloomEffect()
        {
            throw new NotImplementedException();
        }
    }
}