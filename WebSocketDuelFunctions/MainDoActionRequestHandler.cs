using System.Collections.Concurrent;
using System.Net.WebSockets;
using static hololive_oficial_cardgame_server.MatchRoom;
using System.Text;
using System.Text.Json;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    internal class MainDoActionRequestHandler
    {
        private ConcurrentDictionary<string, WebSocket> playerConnections;
        private List<MatchRoom> matchRooms;
        private Draw draw;
        private DuelAction _DuelAction;
        private RequestData pReturnData;
        private RequestData _ReturnData;

        public MainDoActionRequestHandler(ConcurrentDictionary<string, WebSocket> playerConnections, List<MatchRoom> matchRooms)
        {
            this.playerConnections = playerConnections;
            this.matchRooms = matchRooms;
        }

        internal async Task MainDoActionRequestHandleAsync(PlayerRequest playerRequest, WebSocket webSocket)
        {
            int matchnumber = MatchRoom.FindPlayerMatchRoom(matchRooms, playerRequest.playerID);
            MatchRoom cMatchRoom = matchRooms[matchnumber];
            int playerA = cMatchRoom.firstPlayer;
            int playerB = cMatchRoom.secondPlayer;

            if (int.Parse(playerRequest.playerID) != cMatchRoom.currentPlayerTurn || cMatchRoom.currentGamePhase != GAMEPHASE.MainStep)
                return;

            _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestData.extraRequestObject);
            _DuelAction.usedCard.GetCardInfo(_DuelAction.usedCard.cardNumber);
            if (_DuelAction.targetCard != null)
                _DuelAction.targetCard.GetCardInfo(_DuelAction.targetCard.cardNumber);

            int handPos = -1;
            ////////////////////////////
            //NOTHING BETTER THAN A SALAD 
            ////////////////////////////

            bool infoSend = false;

            switch (_DuelAction.actionType)
            {
                case "doArt":
                    infoSend = true;

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

                    Card currentStageOponnentCard = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerBStage : cMatchRoom.playerAStage;
                    Card currentCollabOponnentCard = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerBCollaboration : cMatchRoom.playerACollaboration;

                    Card currentOponnentCard = new();

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

                    List<Card> attachedCards = _DuelAction.usedCard.cardPosition.Equals("Stage") ? cMatchRoom.playerAStage.attachedEnergy : cMatchRoom.playerACollaboration.attachedEnergy;

                    if (attachedCards.Count == 0)
                        return;

                    int damage = ArtCalculator.CalculateTotalDamage(usedArt, attachedCards, _DuelAction.targetCard.color);
                    if (damage < 0)
                        return;

                    currentOponnentCard.currentHp -= damage;
                    currentOponnentCard.normalDamageRecieved += damage;
                    currentOponnentCard.GetCardInfo(currentOponnentCard.cardNumber);

                    if (int.Parse(currentOponnentCard.hp) <= (-1 * currentOponnentCard.currentHp))
                    {
                        List<Card> arquive = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerAArquive : cMatchRoom.playerBArquive;
                        DefeatedHoloMemberAsync(arquive, currentOponnentCard, cMatchRoom, true);
                        return;
                    }

                    _DuelAction.playerID = cMatchRoom.currentPlayerTurn;
                    _DuelAction.actionObject = damage.ToString();
                    _DuelAction.actionType = "UseArt";

                    if (_DuelAction.usedCard.cardPosition.Equals("Stage"))
                        cMatchRoom.centerStageArtUsed = true;
                    if (_DuelAction.usedCard.cardPosition.Equals("Collaboration"))
                        cMatchRoom.collabStageArtUsed = true;

                    pReturnData = new RequestData { type = "GamePhase", description = "UsedArt", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                    Lib.SendMessage(playerConnections[cMatchRoom.playerB.PlayerID.ToString()], pReturnData);
                    Lib.SendMessage(playerConnections[cMatchRoom.playerA.PlayerID.ToString()], pReturnData);

                    cMatchRoom.currentGameHigh++;
                    return;


                    break;
                case "PlayHolomem":
                    //checking if the card can be played

                    List<Record> avaliableCards = FileReader.QueryRecords(null, null, null, _DuelAction.usedCard.cardNumber);//  QueryRecordsByNameAndBloom(new List<Card>() { _DuelAction.usedCard }, "ホロメン");

                    //avaliableCards.AddRange(FileReader.QueryRecordsByNameAndBloom(new List<Card>() { _DuelAction.usedCard }, "Buzzホロメン"));

                    //if not break
                    if (avaliableCards.Count < 1)
                        return;


                    bool canContinuePlayHolomem = false;

                    if (avaliableCards[0].CardType.Equals("ホロメン") || avaliableCards[0].CardType.Equals("Buzzホロメン"))
                        canContinuePlayHolomem = true;

                    if (!canContinuePlayHolomem)
                        return;

                    //checking if the player has the card in the hand and getting the pos
                    handPos = -1;


                    List<Card> playerHand = int.Parse(playerRequest.playerID) == cMatchRoom.firstPlayer ? cMatchRoom.playerAHand : cMatchRoom.playerBHand;

                    int handPosCounter = 0;
                    foreach (Card inHand in playerHand)
                    {
                        if (inHand.cardNumber.Equals(_DuelAction.usedCard.cardNumber))
                            handPos = handPosCounter;
                        handPosCounter++;
                    }
                    playerHand[handPos].GetCardInfo(playerHand[handPos].cardNumber);

                    if (!playerHand[handPos].bloomLevel.Equals("Debut"))
                        return;

                    if (handPos == -1)
                        return;

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
                            List<Card> actionCardList = new List<Card>();
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
                    break;
                case "BloomHolomemWithEffect":
                case "BloomHolomem":
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

                    void bloomCard(Card cardToBloom, Card cardWithBloomInfo) {
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
                                /*
                                _DuelAction.usedCard.bloomChild.Add(cMatchRoom.playerAStage);
                                _DuelAction.usedCard.attachedEnergy = cMatchRoom.playerAStage.attachedEnergy;
                                cMatchRoom.playerAStage.attachedEnergy = null;
                                cMatchRoom.playerAStage = _DuelAction.usedCard;*/

                                cMatchRoom.playerAHand.RemoveAt(handPos);
                            }
                            else
                            {
                                bloomCard(cMatchRoom.playerBStage, _DuelAction.usedCard);
                                /*
                                _DuelAction.usedCard.bloomChild.Add(cMatchRoom.playerBStage);
                                _DuelAction.usedCard.attachedEnergy = cMatchRoom.playerBStage.attachedEnergy;
                                cMatchRoom.playerBStage.attachedEnergy = null;
                                cMatchRoom.playerBStage = _DuelAction.usedCard;
                                */
                                cMatchRoom.playerBHand.RemoveAt(handPos);
                            }
                            break;
                        case "Collaboration":
                            if (int.Parse(playerRequest.playerID) == cMatchRoom.firstPlayer)
                            {
                                bloomCard(cMatchRoom.playerACollaboration, _DuelAction.usedCard);
                                /*
                                _DuelAction.usedCard.bloomChild.Add(cMatchRoom.playerACollaboration);
                                _DuelAction.usedCard.attachedEnergy = cMatchRoom.playerACollaboration.attachedEnergy;
                                cMatchRoom.playerACollaboration.attachedEnergy = null;
                                cMatchRoom.playerACollaboration = _DuelAction.usedCard;
                                */
                                cMatchRoom.playerAHand.RemoveAt(handPos);
                            }
                            else
                            {
                                bloomCard(cMatchRoom.playerBCollaboration, _DuelAction.usedCard);
                                /*
                                _DuelAction.usedCard.bloomChild.Add(cMatchRoom.playerBCollaboration);
                                _DuelAction.usedCard.attachedEnergy = cMatchRoom.playerBCollaboration.attachedEnergy;
                                cMatchRoom.playerBCollaboration.attachedEnergy = null;
                                cMatchRoom.playerBCollaboration = _DuelAction.usedCard;
                                */
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
                            /*
                            _DuelAction.usedCard.bloomChild.Add(actionCardList[x]);
                            _DuelAction.usedCard.attachedEnergy = actionCardList[x].attachedEnergy;
                            actionCardList[x].attachedEnergy = null;
                            actionCardList[x].cardNumber = _DuelAction.usedCard.cardNumber;
                            */
                            if (int.Parse(playerRequest.playerID) == cMatchRoom.firstPlayer)
                            {
                                //cMatchRoom.playerABackPosition.Add(_DuelAction.usedCard);
                                cMatchRoom.playerAHand.RemoveAt(handPos);
                            }
                            else
                            {
                                //cMatchRoom.playerBBackPosition.Add(_DuelAction.usedCard);
                                cMatchRoom.playerBHand.RemoveAt(handPos);
                            }

                            if (_DuelAction.actionType.Equals("BloomHolomem"))
                                return;

                            checkBloomEffect();
                            break;
                    }
                    break;
                case "DoCollab":
                    _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestData.extraRequestObject);
                    if (_DuelAction.usedCard != null)
                        _DuelAction.usedCard.GetCardInfo(_DuelAction.usedCard.cardNumber);
                    if (_DuelAction.targetCard != null)
                        _DuelAction.targetCard.GetCardInfo(_DuelAction.targetCard.cardNumber);

                    if (playerA.Equals(_DuelAction.playerID))
                    {
                        if (cMatchRoom.playerADeck.Count == 0)
                            return;

                        if (string.IsNullOrEmpty(cMatchRoom.playerACollaboration.cardNumber))
                        {
                            int x = 0;
                            foreach (Card c in cMatchRoom.playerABackPosition)
                            {
                                if (c.cardNumber.Equals(_DuelAction.usedCard.cardNumber) && c.suspended == false)
                                {
                                    cMatchRoom.playerAHoloPower.Add(cMatchRoom.playerADeck.Last());
                                    cMatchRoom.playerADeck.RemoveAt(cMatchRoom.playerADeck.Count - 1);
                                    cMatchRoom.playerACollaboration = cMatchRoom.playerABackPosition[x];
                                    break;
                                }
                                x++;
                            }
                            cMatchRoom.playerABackPosition.RemoveAt(x);
                            break;
                        }
                    }
                    else
                    {
                        if (cMatchRoom.playerBDeck.Count == 0)
                            return;

                        if (string.IsNullOrEmpty(cMatchRoom.playerBCollaboration.cardNumber))
                        {
                            int x = 0;
                            foreach (Card c in cMatchRoom.playerBBackPosition)
                            {
                                if (c.cardNumber.Equals(_DuelAction.usedCard.cardNumber) && c.suspended == false)
                                {
                                    cMatchRoom.playerBHoloPower.Add(cMatchRoom.playerBDeck.Last());
                                    cMatchRoom.playerBDeck.RemoveAt(cMatchRoom.playerBDeck.Count - 1);
                                    cMatchRoom.playerBCollaboration = cMatchRoom.playerABackPosition[x];
                                    break;
                                }
                                cMatchRoom.playerBBackPosition.RemoveAt(x);
                                x++;
                            }
                        }
                    }

                    break;
                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
                ///This switch starts the activation of the card effects, i call functions depending of with card it recieve///
                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
                ///
                case "UseSuportStaffMember":
                    _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestData.extraRequestObject);
                    _DuelAction.usedCard.GetCardInfo(_DuelAction.usedCard.cardNumber);
                    _DuelAction.targetCard.GetCardInfo(_DuelAction.targetCard.cardNumber);

                    bool passLimit = false;
                    if (cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer)
                    {
                        foreach (Card cPlayed in cMatchRoom.playerALimiteCardPlayed)
                            if (cPlayed.cardNumber.Equals(_DuelAction.usedCard.cardNumber))
                                passLimit = true;
                    }
                    else
                    {
                        foreach (Card cPlayed in cMatchRoom.playerBLimiteCardPlayed)
                            if (cPlayed.cardNumber.Equals(_DuelAction.usedCard.cardNumber))
                                passLimit = true;
                    }

                    if (passLimit)
                        return;

                    infoSend = true;
                    bool energyPaid = false;
                    switch (_DuelAction.usedCard.cardNumber)
                    {
                        case "hSD01-016":
                            UseCardEffectDrawAsync(cMatchRoom, playerA, playerB, 3, "hSD01-016", true, true);
                            break;
                        case "hSD01-019":

                            energyPaid = PayCardEffectCheerFieldCost(cMatchRoom, playerA, playerB, _DuelAction.cheerCostCard.cardPosition, _DuelAction.cheerCostCard.cardNumber, _DuelAction.usedCard.cardNumber);

                            if (!energyPaid)
                                break;
                            UseCardEffectDrawXAddIfMatchCondition(cMatchRoom, playerA, playerB, 0, "hSD01-019", true, true, 0, false, _DuelAction.targetCard.cardPosition, _DuelAction.targetCard.cardNumber, false);
                            break;
                        case "hBP01-103":

                            energyPaid = PayCardEffectCheerFieldCost(cMatchRoom, playerA, playerB, _DuelAction.cheerCostCard.cardPosition, _DuelAction.cheerCostCard.cardNumber, _DuelAction.usedCard.cardNumber);

                            if (!energyPaid)
                                break;
                            UseCardEffectDrawXAddIfMatchCondition(cMatchRoom, playerA, playerB, 0, "hBP01-103", true, true, 0, true, _DuelAction.targetCard.cardPosition, _DuelAction.targetCard.cardNumber, false);
                            break;
                        case "hBP01-105":

                            energyPaid = PayCardEffectCheerFieldCost(cMatchRoom, playerA, playerB, _DuelAction.cheerCostCard.cardPosition, _DuelAction.cheerCostCard.cardNumber, _DuelAction.usedCard.cardNumber);

                            if (!energyPaid)
                                break;

                            List<Card> tempHand = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
                            tempHand.Add(_DuelAction.usedCard);
                            tempHand.Add(_DuelAction.cheerCostCard);
                            tempHand.Add(_DuelAction.targetCard);

                            UseCardEffectDrawXAddIfMatchCondition(cMatchRoom, playerA, playerB, 0, "hBP01-105", true, true, 0, false, _DuelAction.targetCard.cardPosition, _DuelAction.targetCard.cardNumber, false);
                            break;
                        case "hBP01-109":
                            UseCardEffectDrawXAddIfBetweenReview(cMatchRoom, playerA, playerB, 3, "hBP01-109", true, true, 0);
                            break;
                        case "hSD01-018":
                            UseCardEffectDrawXAddIfBetweenReview(cMatchRoom, playerA, playerB, 5, "hSD01-018", false, true, 0, false, "", "", true);
                            break;
                        case "hBP01-104":
                            UseCardEffectToSummom(cMatchRoom, playerA, playerB, "Deck", "hBP01-104", true, "Debut");
                            break;
                        case "hBP01-102":
                            UseCardEffectDrawXAddIfBetweenReview(cMatchRoom, playerA, playerB, 4, "hBP01-102", true, true, 7);
                            break;
                        case "hSD01-021":
                            UseCardEffectDrawXAddIfBetweenReview(cMatchRoom, playerA, playerB, 4, "hBP01-102", true, true, 7);
                            break;
                        case "hBP01-111":
                            UseCardEffectDrawXAddIfBetweenReview(cMatchRoom, playerA, playerB, 4, "hBP01-111", true, true, 7);
                            break;
                        case "hBP01-113":
                            UseCardEffectDrawXAddIfBetweenReview(cMatchRoom, playerA, playerB, 4, "hBP01-113", true, true, 7);
                            break;
                        case "hSD01-020":
                            Random random = new Random();
                            int randomNumber = random.Next(1, 7);

                            _ReturnData = new RequestData { type = "GamePhase", description = "RollDice", requestObject = randomNumber.ToString() };
                            Lib.SendMessage(playerConnections[cMatchRoom.playerB.PlayerID.ToString()], _ReturnData);
                            Lib.SendMessage(playerConnections[cMatchRoom.playerA.PlayerID.ToString()], _ReturnData);

                            if (randomNumber < 3)
                                return;

                            tempHand = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
                            tempHand.Add(_DuelAction.usedCard);
                            tempHand.Add(_DuelAction.cheerCostCard);
                            tempHand.Add(_DuelAction.targetCard);

                            UseCardEffectDrawXAddIfMatchCondition(cMatchRoom, playerA, playerB, 0, "hSD01-020", true, true, 0, false, _DuelAction.targetCard.cardPosition, _DuelAction.targetCard.cardNumber, false);
                            break;
                        case "hSD01-017":
                            tempHand = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerAHand : cMatchRoom.playerBHand;

                            List<Card> tempDeck = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerADeck : cMatchRoom.playerBDeck;

                            if (tempHand.Count < 2)
                                break;

                            cMatchRoom.suffleHandToTheDeck(tempDeck, tempHand);
                            cMatchRoom.ShuffleCards(tempDeck);
                            Lib.getCardFromDeck(tempDeck, tempHand, 5);

                            if (cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID)
                                cMatchRoom.playerALimiteCardPlayed.Add(new Card() { cardNumber = "hSD01-017" });
                            else
                                cMatchRoom.playerBLimiteCardPlayed.Add(new Card() { cardNumber = "hSD01-017" });

                            draw = new Draw()
                            {
                                playerID = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.startPlayer : cMatchRoom.secondPlayer,
                                suffle = true,
                                zone = "Deck",
                                cardList = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerAHand : cMatchRoom.playerBHand
                            };


                            _DuelAction = new() { actionType = "SuffleAllThenDraw", playerID = cMatchRoom.currentPlayerTurn, actionObject = JsonSerializer.Serialize(draw) };
                            pReturnData = new RequestData { type = "GamePhase", description = "Draw", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                            Console.WriteLine(pReturnData);
                            if (cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID)
                            {
                                Lib.SendMessage(playerConnections[cMatchRoom.playerA.PlayerID.ToString()], pReturnData);
                                draw.cardList = cMatchRoom.FillCardListWithEmptyCards(draw.cardList);
                                _DuelAction.actionObject = JsonSerializer.Serialize(draw);
                                pReturnData = new RequestData { type = "GamePhase", description = "Draw", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                                Lib.SendMessage(playerConnections[cMatchRoom.playerB.PlayerID.ToString()], pReturnData);
                            }
                            else
                            {
                                Lib.SendMessage(playerConnections[cMatchRoom.playerB.PlayerID.ToString()], pReturnData);
                                draw.cardList = cMatchRoom.FillCardListWithEmptyCards(draw.cardList);
                                _DuelAction.actionObject = JsonSerializer.Serialize(draw);
                                pReturnData = new RequestData { type = "GamePhase", description = "Draw", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                                Lib.SendMessage(playerConnections[cMatchRoom.playerA.PlayerID.ToString()], pReturnData);
                            }
                            break;
                    }
                    break;
                case "ReSetCardAtStage":

                    if (cMatchRoom.currentGamePhase != GAMEPHASE.ResetStepReSetStage)
                        return;

                    _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestData.extraRequestObject);

                    currentStageCard = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerAStage : cMatchRoom.playerBStage;
                    List<Card> currentBackStageCard = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerABackPosition : cMatchRoom.playerBBackPosition;

                    // if player backstage is null, proced 
                    if (currentStageCard == null)
                        if (!string.IsNullOrEmpty(currentStageCard.cardNumber))
                            return;

                    //checking if player has the card he send to the server at backstage 
                    int i = -1;
                    for (int n = 0; n < currentBackStageCard.Count; n++) { 
                        if (_DuelAction.usedCard.playedFrom.Equals(currentBackStageCard[n].cardPosition)) {
                            i = n;
                            break;
                        }
                        i++;
                    }

                    if (i == -1)
                        return;

                    // adding card to the stage
                    currentStageCard = currentBackStageCard[i];
                    currentStageCard.cardPosition = "Stage";
                    currentStageCard.suspended = false;

                    // removing from backstage
                    currentBackStageCard.RemoveAt(i);


                    DuelAction duelAction = new DuelAction
                    {
                        playerID = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.firstPlayer : cMatchRoom.secondPlayer,
                        usedCard = currentStageCard,
                        playedFrom = "Collaboration",
                        actionType = "ReSetStage"
                    };

                    cMatchRoom.currentGamePhase = GAMEPHASE.DrawStep;

                    _ReturnData = new RequestData { type = "GamePhase", description = "ReSetStage", requestObject = JsonSerializer.Serialize(duelAction, Lib.options) };

                    Lib.SendMessage(playerConnections[cMatchRoom.firstPlayer.ToString()], _ReturnData);
                    Lib.SendMessage(playerConnections[cMatchRoom.secondPlayer.ToString()], _ReturnData);

                    cMatchRoom.currentGameHigh++;
                    return;
                    break;
                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
                case "next":
                    break;
            }
            if (!infoSend)
            {
                _DuelAction.playerID = cMatchRoom.currentPlayerTurn;
                _ReturnData = new RequestData { type = "GamePhase", description = _DuelAction.actionType, requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };

                Lib.SendMessage(playerConnections[cMatchRoom.playerB.PlayerID.ToString()], _ReturnData);
                Lib.SendMessage(playerConnections[cMatchRoom.playerA.PlayerID.ToString()], _ReturnData);

                cMatchRoom.currentGameHigh++;
            }

            async Task DefeatedHoloMemberAsync(List<Card> arquive, Card currentOponnentCard, MatchRoom cMatchRoom, Boolean result)
            {
                cMatchRoom.cheersAssignedThisChainTotal = GetDownneedCheerAmount(currentOponnentCard.cardNumber);

                if (cMatchRoom.cheersAssignedThisChainTotal > 1) {
                    cMatchRoom.cheersAssignedThisChainAmount = 0;
                }

                arquive.AddRange(currentOponnentCard.attachedEnergy);
                arquive.AddRange(currentOponnentCard.bloomChild);
                arquive.Add(currentOponnentCard);

                DuelAction _duelaction = new();
                _duelaction.actionType = "DefeatedHoloMember";
                _duelaction.playerID = GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn);
                _duelaction.targetCard = currentOponnentCard;

                currentOponnentCard = null;
                RequestData _ReturnData = new RequestData { type = "GamePhase", description = "DefeatedHoloMember", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };

                cMatchRoom.currentGamePhase = GAMEPHASE.HolomemDefeated;

                //assign the values need to check if the user win the duel
                List<Card> attackedPlayerBackStage = _duelaction.playerID == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerABackPosition : cMatchRoom.playerBBackPosition;
                List<Card> attackedPlayerLife = _duelaction.playerID == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerALife : cMatchRoom.playerBLife;

                if (attackedPlayerLife.Count == 1)
                {
                    _ = Lib.EndDuelAsync(result, cMatchRoom);
                    return;
                }

                if (attackedPlayerBackStage.Count == 0)
                {
                    _ = Lib.EndDuelAsync(result, cMatchRoom);
                    return;
                }
                // if the player didnt win the duel, awnser the player to get his new cheer
                Lib.SendMessage(playerConnections[cMatchRoom.playerB.PlayerID.ToString()], _ReturnData);
                Lib.SendMessage(playerConnections[cMatchRoom.playerA.PlayerID.ToString()], _ReturnData);
            }






            bool PayCardEffectCheerFieldCost(MatchRoom cMatchRoom, int playerA, int playerB, string zone, string cardNumber, string resolvingCard)
            {

                Card seletectedCard = new();

                switch (zone)
                {
                    case "Favourite":
                        seletectedCard = (cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer) ? cMatchRoom.playerAFavourite : cMatchRoom.playerBFavourite;
                        break;
                    case "Collaboration":
                        seletectedCard = (cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer) ? cMatchRoom.playerACollaboration : cMatchRoom.playerBCollaboration;
                        break;
                    case "Stage":
                        seletectedCard = (cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer) ? cMatchRoom.playerAStage : cMatchRoom.playerBStage;
                        break;
                    case "BackStage1":
                    case "BackStage2":
                    case "BackStage3":
                    case "BackStage4":
                    case "BackStage5":
                        List<Card> seletectedCardList;
                        seletectedCardList = (cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer) ? cMatchRoom.playerABackPosition : cMatchRoom.playerBBackPosition;
                        foreach (Card card in seletectedCardList)
                            if (card.cardPosition.Equals(zone))
                                seletectedCard = card;
                        break;
                }

                int removePos = -1;
                int n = 0;
                foreach (Card energy in seletectedCard.attachedEnergy)
                {
                    if (energy.cardNumber.Equals(cardNumber))
                    {
                        removePos = n;
                        break;
                    }
                    n++;
                }

                cMatchRoom.currentCardResolving = resolvingCard;

                if (removePos > -1)
                {
                    seletectedCard.attachedEnergy.RemoveAt(removePos);
                    return true;
                }


                return false;
            }
            bool UseCardEffectToSummom(MatchRoom cMatchRoom, int playerA, int playerB, string zone, string cUsedNumber, Boolean result, string bloomLevel)
            {

                List<Card> query = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerADeck : cMatchRoom.playerBDeck;

                foreach (var card in query)
                    card.GetCardInfo(card.cardNumber); // Assuming this method fills card info, including bloomLevel

                query = query.Where(r => r.bloomLevel == bloomLevel).ToList();

                Draw _Draw = new Draw()
                {
                    playerID = cMatchRoom.currentPlayerTurn,
                    suffle = false,
                    zone = "Deck",
                    cardList = query
                };

                DuelAction DuelActionResponse = new DuelAction()
                {
                    playerID = cMatchRoom.currentPlayerTurn,
                    actionType = "conditionedSummom",
                    usedCard = new Card() { cardNumber = cUsedNumber }
                    //,
                    //targetCard = new Card() { cardNumber = costCardnumber, cardPosition = zone }
                };

                cMatchRoom.currentCardResolving = cUsedNumber;
                //cMatchRoom.currentGamePhase = GAMEPHASE.ConditionedSummom;

                SendPlayerData(cMatchRoom, true, _Draw, DuelActionResponse, "SuporteEffectSummomIf", false, cUsedNumber, result);
                return true;
            }
            async Task UseCardEffectDrawXAddIfMatchCondition(MatchRoom cMatchRoom, int playerA, int playerB, int cNum, string cUsedNumber, bool LimiteUseCard, Boolean result, int HandMustHave, bool needEnergy = false, string zone = "", string costCardnumber = "", bool reveal = false)
            {

                List<Card> query = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerADeck : cMatchRoom.playerBDeck;


                foreach (var card in query)
                    card.GetCardInfo(card.cardNumber);

                List<Card> queryy = new();
                switch (cUsedNumber)
                {
                    case "hSD01-019":
                        queryy = query.Where(r => r.bloomLevel == "1st" && !(r.cardType == "Buzzホロメン")).ToList();
                        queryy.AddRange(query.Where(r => r.bloomLevel == "2nd" && !(r.cardType == "Buzzホロメン")).ToList());
                        break;
                    case "hBP01-103":
                        queryy = query.Where(r => r.bloomLevel == "1st" && !(r.cardType == "Buzzホロメン")).ToList();
                        queryy.AddRange(query.Where(r => r.bloomLevel == "Debut" && !(r.cardType == "Buzzホロメン")).ToList());
                        break;
                    case "hSD01-020":
                    case "hBP01-105":
                        queryy = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerACardCheer : cMatchRoom.playerACardCheer;
                        break;
                }

                Draw _Draw = new Draw()
                {
                    playerID = cMatchRoom.currentPlayerTurn,
                    suffle = false,
                    zone = "Deck",
                    cardList = queryy
                };

                DuelAction DuelActionResponse = new DuelAction()
                {
                    playerID = cMatchRoom.currentPlayerTurn,
                    actionType = "conditionedDraw",
                    usedCard = new Card() { cardNumber = cUsedNumber },
                    targetCard = new Card() { cardNumber = costCardnumber, cardPosition = zone }
                };
                cMatchRoom.currentCardResolving = cUsedNumber;
                cMatchRoom.currentGamePhase = GAMEPHASE.ConditionedDraw;

                SendPlayerData(cMatchRoom, reveal, _Draw, DuelActionResponse, "SuporteEffectDrawXAddIf", LimiteUseCard, cUsedNumber, result);
            }

            async Task UseCardEffectDrawXAddIfBetweenReview(MatchRoom cMatchRoom, int playerA, int playerB, int cNum, string cUsedNumber, bool LimiteUseCard, Boolean result, int HandMustHave, bool needEnergy = false, string zone = "", string costCardnumber = "", bool reveal = false)
            {

                if (playerA == cMatchRoom.currentPlayerTurn)
                {
                    if (cMatchRoom.playerADeck.Count < HandMustHave && HandMustHave > 0)
                        return;

                    Lib.getCardFromDeck(cMatchRoom.playerADeck, cMatchRoom.playerATempHand, cNum);
                }
                else
                {
                    if (cMatchRoom.playerBDeck.Count < HandMustHave && HandMustHave > 0)
                        return;

                    Lib.getCardFromDeck(cMatchRoom.playerBDeck, cMatchRoom.playerBTempHand, cNum);
                }


                Draw _Draw = new Draw()
                {
                    playerID = cMatchRoom.currentPlayerTurn,
                    suffle = false,
                    zone = "Deck",
                    cardList = (cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer) ? (cMatchRoom.playerATempHand) : (cMatchRoom.playerBTempHand)
                };

                DuelAction DuelActionResponse = new DuelAction()
                {
                    playerID = cMatchRoom.currentPlayerTurn,
                    actionType = "conditionedDraw",
                    usedCard = new Card() { cardNumber = cUsedNumber },
                    targetCard = new Card() { cardNumber = costCardnumber, cardPosition = zone }
                };
                cMatchRoom.currentCardResolving = cUsedNumber;
                cMatchRoom.currentGamePhase = GAMEPHASE.ConditionedDraw;

                SendPlayerData(cMatchRoom, reveal, _Draw, DuelActionResponse, "SuporteEffectDrawXAddIf", LimiteUseCard, cUsedNumber, result);
            }

            async Task UseCardEffectDrawAsync(MatchRoom cMatchRoom, int playerA, int playerB, int cNum, string cUsedNumber, bool LimiteUseCard, Boolean result)
            {
                if (playerA == cMatchRoom.currentPlayerTurn)
                    Lib.getCardFromDeck(cMatchRoom.playerADeck, cMatchRoom.playerAHand, cNum);
                else
                    Lib.getCardFromDeck(cMatchRoom.playerBDeck, cMatchRoom.playerBHand, cNum);

                Draw _Draw = new Draw()
                {
                    playerID = cMatchRoom.currentPlayerTurn,
                    suffle = false,
                    zone = "Deck",
                    //getting the range of cards from the player hand, then getting the last ones to add to the draw
                    cardList = (cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer) ? cMatchRoom.playerAHand.GetRange(cMatchRoom.playerAHand.Count() - cNum, cNum) : cMatchRoom.playerBHand.GetRange(cMatchRoom.playerBHand.Count() - cNum, cNum)
                };

                DuelAction DuelActionResponse = new DuelAction()
                {
                    playerID = cMatchRoom.currentPlayerTurn,
                    actionType = "doDraw",
                    usedCard = new Card() { cardNumber = cUsedNumber }
                };

                SendPlayerData(cMatchRoom, false, _Draw, DuelActionResponse, "SuporteEffectDraw", LimiteUseCard, cUsedNumber, result);
            }





            async Task SendPlayerData(MatchRoom cMatchRoom, bool reveal, Draw _Draw, DuelAction DuelActionResponse, string description, bool LimiteUseCard, string cUsedNumber, Boolean result)
            {
                RequestData _ReturnData;
                int otherPlayer = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerB.PlayerID : cMatchRoom.playerA.PlayerID;

                // Serialize and send data to the current player
                DuelActionResponse.actionObject = JsonSerializer.Serialize(_Draw);
                _ReturnData = new RequestData { type = "GamePhase", description = description, requestObject = JsonSerializer.Serialize(DuelActionResponse, Lib.options) };

                Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn.ToString()], _ReturnData);

                // Handle reveal logic and send data to the other player
                if (reveal == false)
                    DuelActionResponse.actionObject = JsonSerializer.Serialize(_Draw.cardList = cMatchRoom.FillCardListWithEmptyCards(_Draw.cardList), Lib.options);
                else
                    DuelActionResponse.actionObject = JsonSerializer.Serialize(_Draw.cardList, Lib.options);

                _ReturnData = new RequestData { type = "GamePhase", description = description, requestObject = JsonSerializer.Serialize(DuelActionResponse, Lib.options) };
                Lib.SendMessage(MessageDispatcher.playerConnections[otherPlayer.ToString()], _ReturnData);

                // Update the limit card played for the appropriate player
                if (LimiteUseCard)
                {
                    if (cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID)
                        cMatchRoom.playerALimiteCardPlayed.Add(new Card() { cardNumber = cUsedNumber });
                    else
                        cMatchRoom.playerBLimiteCardPlayed.Add(new Card() { cardNumber = cUsedNumber });
                }
            }





        }

        private int GetDownneedCheerAmount(string cardNumber)
        {
            return 2;
        }

        private void checkBloomEffect()
        {
            throw new NotImplementedException();
        }
    }
}