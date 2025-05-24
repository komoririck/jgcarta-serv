using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using hololive_oficial_cardgame_server.SerializableObjects;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    internal class PlayHolomemHandler
    {
        private PlayerRequest _ReturnData;

        bool TESTEMODE = true;

        internal async Task MainDoActionRequestPlayHolomemHandleAsync(PlayerRequest playerRequest, MatchRoom cMatchRoom)
        {
            DuelAction _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestObject);
            
                _DuelAction.targetCard?.GetCardInfo();
                _DuelAction.usedCard?.GetCardInfo();
                _DuelAction.cheerCostCard?.GetCardInfo();

            List<Record> avaliableCards = FileReader.QueryRecords(null, null, null, _DuelAction.usedCard.cardNumber);

            if (TESTEMODE)
                avaliableCards = FileReader.result.AsQueryable().Select(r => r.Value).ToList();

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

            List<Card> playerHand = playerRequest.playerID.Equals(cMatchRoom.firstPlayer) ? cMatchRoom.playerAHand : cMatchRoom.playerBHand;


           
            //checking if the player has the card in the hand and getting the pos
            int handPos = Lib.CheckIfCardExistAtList(cMatchRoom, playerRequest.playerID, _DuelAction.usedCard.cardNumber);
            if (handPos == -1)
            {
                Lib.PrintPlayerHand(cMatchRoom);
                Lib.WriteConsoleMessage("No match found in the player hand");
                return;
            }

            //getting card info
            playerHand[handPos].GetCardInfo();
            bool canContinue = false;

            if (playerHand[handPos].bloomLevel.Equals("Debut") || playerHand[handPos].bloomLevel.Equals("Spot"))
                canContinue = true;


            if (!TESTEMODE)
                if (!canContinue)
                {
                    Lib.WriteConsoleMessage("this card cannot be played at this point");
                    return;
                }

            //checking of the card can be played at the spot
            switch (_DuelAction.local)
            {
                case "Stage":
                    if (playerRequest.playerID.Equals(cMatchRoom.firstPlayer))
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

                    if (playerRequest.playerID.Equals(cMatchRoom.firstPlayer))
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
            _ReturnData = new PlayerRequest { type = "DuelUpdate", description = "PlayHolomem", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.jsonOptions) };

            Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.playerB.PlayerID.ToString()], _ReturnData);
            Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.playerA.PlayerID.ToString()], _ReturnData);

            cMatchRoom.currentGameHigh++;
        }
    }
}