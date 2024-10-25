﻿using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    internal class PlayHolomemHandler
    {
        private ConcurrentDictionary<string, WebSocket> playerConnections;
        private List<MatchRoom> matchRooms;
        private RequestData _ReturnData;

        public PlayHolomemHandler(ConcurrentDictionary<string, WebSocket> playerConnections, List<MatchRoom> matchRooms)
        {
            this.playerConnections = playerConnections;
            this.matchRooms = matchRooms;
        }

        internal async Task MainDoActionRequestPlayHolomemHandleAsync(PlayerRequest playerRequest, WebSocket webSocket)
        {

            int matchnumber = MatchRoom.FindPlayerMatchRoom(matchRooms, playerRequest.playerID);
            MatchRoom cMatchRoom = matchRooms[matchnumber];

            DuelAction _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestData.extraRequestObject);
            
            if (_DuelAction.targetCard != null)
                _DuelAction.targetCard.GetCardInfo(_DuelAction.targetCard.cardNumber);
            if (_DuelAction.usedCard != null)
                _DuelAction.usedCard.GetCardInfo(_DuelAction.usedCard.cardNumber);
            if (_DuelAction.cheerCostCard != null)
                _DuelAction.cheerCostCard.GetCardInfo(_DuelAction.cheerCostCard.cardNumber);

            List<Record> avaliableCards = FileReader.QueryRecords(null, null, null, _DuelAction.usedCard.cardNumber);

            //if not break
            if (_DuelAction.usedCard.cardPosition.Equals("Collaboration") )
            {
                Lib.WriteConsoleMessage("Cannot play card at collab zone");
                return;
            }

            //if not break
            if (avaliableCards.Count < 1) {
                Lib.WriteConsoleMessage("No avaliable holomem to play");
                return;
            }

            if (!(avaliableCards[0].CardType.Equals("ホロメン") || avaliableCards[0].CardType.Equals("Buzzホロメン")))
            {
                Lib.WriteConsoleMessage("No avaliable ホロメン or Buzzホロメン to play");
                return;
            }

            List<Card> playerHand = int.Parse(playerRequest.playerID) == cMatchRoom.firstPlayer ? cMatchRoom.playerAHand : cMatchRoom.playerBHand;

            //checking if the player has the card in the hand and getting the pos
            int handPos = -1;
            int handPosCounter = 0;
            foreach (Card inHand in playerHand)
            {
                if (inHand.cardNumber.Equals(_DuelAction.usedCard.cardNumber)) 
                { 
                    handPos = handPosCounter;
                    break;
                }
                handPosCounter++;
            }

            if (handPos == -1)
            {
                Lib.PrintPlayerHand(cMatchRoom);
                Lib.WriteConsoleMessage("No match found in the player hand");
                return;
            }

            //getting card info
            playerHand[handPos].GetCardInfo(playerHand[handPos].cardNumber);

            if (!playerHand[handPos].bloomLevel.Equals("Debut"))
            {
                Lib.WriteConsoleMessage("this card cannot be played at this point");
                return;
            }

            //checking of the card can be played at the spot
            switch (_DuelAction.local)
            {
                case "Stage":
                    if (int.Parse(playerRequest.playerID) == cMatchRoom.firstPlayer)
                    {
                        if (cMatchRoom.playerAStage != null)
                        {
                            cMatchRoom.playerAStage = _DuelAction.usedCard;
                            cMatchRoom.playerAStage.cardPosition = _DuelAction.usedCard.cardPosition;
                            cMatchRoom.playerAStage.playedFrom = _DuelAction.usedCard.playedFrom;
                            cMatchRoom.playerAHand.RemoveAt(handPos);
                        }
                    }
                    else
                    {
                        if (cMatchRoom.playerBStage != null)
                        {
                            cMatchRoom.playerBStage = _DuelAction.usedCard;
                            cMatchRoom.playerBStage.cardPosition = _DuelAction.usedCard.cardPosition;
                            cMatchRoom.playerBStage.playedFrom = _DuelAction.usedCard.playedFrom;
                            cMatchRoom.playerBHand.RemoveAt(handPos);
                        }
                    }
                    break;
                case "BackStage1":
                case "BackStage2":
                case "BackStage3":
                case "BackStage4":
                case "BackStage5":

                    if (int.Parse(playerRequest.playerID) == cMatchRoom.firstPlayer)
                    {
                        foreach (Card cartinha in cMatchRoom.playerABackPosition)
                        {
                            if (cartinha.cardPosition.Equals(_DuelAction.local))
                                break;
                        }

                        _DuelAction.usedCard.cardPosition = _DuelAction.local;
                        _DuelAction.usedCard.playedFrom = _DuelAction.playedFrom;

                        cMatchRoom.playerABackPosition.Add(_DuelAction.usedCard);
                        cMatchRoom.playerAHand.RemoveAt(handPos);
                    }
                    else
                    {
                        foreach (Card cartinha in cMatchRoom.playerBBackPosition)
                        {
                            if (cartinha.cardPosition.Equals(_DuelAction.local))
                                break;
                        }
                        _DuelAction.usedCard.cardPosition = _DuelAction.local;
                        _DuelAction.usedCard.playedFrom = _DuelAction.playedFrom;

                        cMatchRoom.playerBBackPosition.Add(_DuelAction.usedCard);
                        cMatchRoom.playerBHand.RemoveAt(handPos);
                    }
                    break;
            }

            _DuelAction.playerID = cMatchRoom.currentPlayerTurn;
            _ReturnData = new RequestData { type = "GamePhase", description = _DuelAction.actionType, requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };

            Lib.SendMessage(playerConnections[cMatchRoom.playerB.PlayerID.ToString()], _ReturnData);
            Lib.SendMessage(playerConnections[cMatchRoom.playerA.PlayerID.ToString()], _ReturnData);

            cMatchRoom.currentGameHigh++;
        }
    }
}