using hololive_oficial_cardgame_server.SerializableObjects;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using static hololive_oficial_cardgame_server.SerializableObjects.MatchRoom;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    internal class BloomHolomemHandler
    {
        internal async Task MainDoActionRequestBloomHolomemHandleAsync(PlayerRequest playerRequest, MatchRoom cMatchRoom)
        {
            DuelAction _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestObject);

                _DuelAction.targetCard?.GetCardInfo();
                _DuelAction.usedCard?.GetCardInfo();

            bool canContinueBloomHolomem = false;
            if (!(_DuelAction.usedCard.bloomLevel.Equals("Debut") || _DuelAction.usedCard.bloomLevel.Equals("1st")))
            {
                Lib.WriteConsoleMessage("Used card is not valid to bloom");
                return;

            }

            //targetcardValidation
            Card serverTarget = null;
            switch (_DuelAction.targetCard.cardPosition) 
            {
                case "Stage":
                    serverTarget = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerAStage : cMatchRoom.playerBStage;
                    if (!serverTarget.cardPosition.Equals(_DuelAction.targetCard.cardPosition))
                    {
                        Lib.WriteConsoleMessage("invalid target");
                        return;
                    }
                    break;
                case "Collaboration":
                    serverTarget = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerACollaboration : cMatchRoom.playerBCollaboration;
                    if (!serverTarget.cardPosition.Equals(_DuelAction.targetCard.cardPosition))
                    {
                        Lib.WriteConsoleMessage("invalid target");
                        return;
                    }
                    break;
                case "BackStage1":
                case "BackStage2":
                case "BackStage3":
                case "BackStage4":
                case "BackStage5":
                    List<Card> backstage = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerABackPosition : cMatchRoom.playerBBackPosition;
                    foreach (Card card in backstage) 
                    {
                        if (card.cardPosition.Equals(_DuelAction.targetCard.cardPosition)) {
                            serverTarget = card;
                        }
                    }
                    if(serverTarget == null)
                    {
                        Lib.WriteConsoleMessage("invalid target");
                        return;
                    }
                    break;
            }


            //QueryBloomableCard has a special treatment for card with more than one name
            List<Record> validCardToBloom = FileReader.QueryBloomableCard(_DuelAction.targetCard.name, _DuelAction.targetCard.bloomLevel);
            //lets add especial conditions to the bloom, like SorAZ
            if (_DuelAction.usedCard.name.Equals("SorAZ") && _DuelAction.targetCard.bloomLevel.Equals("Debut") && (_DuelAction.targetCard.name.Equals("ときのそら") || _DuelAction.targetCard.name.Equals("AZKi"))) {
                foreach (KeyValuePair<string, Record> r in FileReader.result)
                {
                    if (r.Value.CardNumber.Equals("hSD01-013")) 
                    {
                        validCardToBloom.Add(r.Value);
                    }
                }
            }

            if (_DuelAction.usedCard.cardNumber.Equals("hBP01-045")) {
                List<Card> playerLife = cMatchRoom.currentPlayerTurn.Equals(cMatchRoom.firstPlayer) ? cMatchRoom.playerALife : cMatchRoom.playerBLife;
                if (playerLife.Count < 4) { 
                validCardToBloom = FileReader.QueryBloomableCard(_DuelAction.targetCard.name, "1st");
                validCardToBloom.AddRange(FileReader.QueryBloomableCard(_DuelAction.targetCard.name, "2nd"));
                }
            }

                
            //if not break possible to bloom, break
            if (validCardToBloom.Count < 1)
                return;

            //checking if the player has the card in the hand and getting the pos
            List<Card> playerHand = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerAHand : cMatchRoom.playerBHand;
            int handPos = -1;
            int nn = 0;
            foreach (Card inHand in playerHand)
            {
                if (inHand.cardNumber.Equals(_DuelAction.usedCard.cardNumber))
                    handPos = nn;
                nn++;
            }

            if (handPos == -1) {
                Lib.WriteConsoleMessage("used card doesnt exist in the player hand");
                return;
            }

            //check if the used card exist in the validCardToBloom since we search using name above
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
            {
                Lib.WriteConsoleMessage("used card doesnt exist in the bloom result");
                return;
            }

            switch (_DuelAction.local)
            {
                case "Stage":
                    if (playerRequest.playerID.Equals(cMatchRoom.firstPlayer))
                    {
                        bloomCard(cMatchRoom.playerAStage, _DuelAction.usedCard, cMatchRoom);

                        cMatchRoom.playerAHand.RemoveAt(handPos);
                    }
                    else
                    {
                        bloomCard(cMatchRoom.playerBStage, _DuelAction.usedCard, cMatchRoom);
                        cMatchRoom.playerBHand.RemoveAt(handPos);
                    }
                    break;
                case "Collaboration":
                    if (playerRequest.playerID.Equals(cMatchRoom.firstPlayer))
                    {
                        bloomCard(cMatchRoom.playerACollaboration, _DuelAction.usedCard, cMatchRoom);
                        cMatchRoom.playerAHand.RemoveAt(handPos);
                    }
                    else
                    {
                        bloomCard(cMatchRoom.playerBCollaboration, _DuelAction.usedCard, cMatchRoom);
                        cMatchRoom.playerBHand.RemoveAt(handPos);
                    }
                    break;
                case "BackStage1":
                case "BackStage2":
                case "BackStage3":
                case "BackStage4":
                case "BackStage5":
                    List<Card> actionCardList = new List<Card>();

                    if (playerRequest.playerID.Equals(cMatchRoom.firstPlayer))
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

                    bloomCard(actionCardList[x], _DuelAction.usedCard, cMatchRoom);

                    if (playerRequest.playerID.Equals(cMatchRoom.firstPlayer))
                        cMatchRoom.playerAHand.RemoveAt(handPos);
                    else
                        cMatchRoom.playerBHand.RemoveAt(handPos);

                    break;
            }

            //since we pass all the validation, lets just assign the new card
            void bloomCard(Card cardToBloom, Card cardWithBloomInfo, MatchRoom matchRoom)
            {
                cardToBloom.bloomChild.Add(new Card(cardToBloom.cardNumber));
                cardToBloom.cardNumber = cardWithBloomInfo.cardNumber;
                cardToBloom.GetCardInfo(true);
                cardToBloom.playedThisTurn = true;


                OnBloomIncreaseEffects(cardToBloom, cMatchRoom);
            }

            PlayerRequest pReturnData = new PlayerRequest { type = "DuelUpdate", description = "BloomHolomem", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.jsonOptions) };

            cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayers(), pReturnData));
            cMatchRoom.PushPlayerAnswer();



            cMatchRoom.currentGameHigh++;
        }

        private void OnBloomIncreaseEffects(Card cardToBloom, MatchRoom cMatchRoom)
        {

            string playerId = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.firstPlayer : cMatchRoom.secondPlayer;

            List<Card> playerHand = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerAHand : cMatchRoom.playerBHand;
            List<Card> playerArquive = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerAArquive : cMatchRoom.playerBArquive;
            List<Card> playerDeck = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerADeck : cMatchRoom.playerBDeck;

            foreach (Card card in cardToBloom.attachedEquipe) {
                switch (card.cardNumber) { 
                    case "hBP01-121":

                        if (cardToBloom.name.Equals("小鳥遊キアラ")) { 
                            PlayerRequest ReturnData = new PlayerRequest { type = "DuelUpdate", description = "DrawBloomIncreaseEffect", requestObject = "" };
                            Lib.MoveTopCardFromXToY(playerDeck, playerHand, 1);

                            DuelAction newDraw = new DuelAction().SetID(playerId).DrawTopCardFromXToY(playerHand, "Deck", 1);
                            cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayersStartWith(playerId), hidden: true, playerRequest: ReturnData, duelAction: newDraw));
                            cMatchRoom.PushPlayerAnswer();
                        }
                        break;
                }
            }

        }
    }
}