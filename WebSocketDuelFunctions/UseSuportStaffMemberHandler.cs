using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Security.Policy;
using System.Text.Json;
using static hololive_oficial_cardgame_server.MatchRoom;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    class UseSuportStaffMemberHandler
    {
        private ConcurrentDictionary<string, WebSocket> playerConnections;
        private List<MatchRoom> matchRooms;
        private RequestData _ReturnData;

        public UseSuportStaffMemberHandler(ConcurrentDictionary<string, WebSocket> playerConnections, List<MatchRoom> matchRooms)
        {
            this.playerConnections = playerConnections;
            this.matchRooms = matchRooms;
        }

        internal async Task MainDoActionRequestUseSuportStaffMemberHandleAsync(PlayerRequest playerRequest, WebSocket webSocket)
        {

            int matchnumber = FindPlayerMatchRoom(matchRooms, playerRequest.playerID);
            MatchRoom cMatchRoom = matchRooms[matchnumber];


            DuelAction _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestData.extraRequestObject);

            if (_DuelAction.targetCard != null)
                _DuelAction.targetCard.GetCardInfo(_DuelAction.targetCard.cardNumber);
            if (_DuelAction.usedCard != null)
                _DuelAction.usedCard.GetCardInfo(_DuelAction.usedCard.cardNumber);
            if (_DuelAction.cheerCostCard != null)
                _DuelAction.cheerCostCard.GetCardInfo(_DuelAction.cheerCostCard.cardNumber);

            RequestData pReturnData;
            int playerA = cMatchRoom.firstPlayer;
            int playerB = cMatchRoom.secondPlayer;

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
                    UseCardEffectDrawXAddIfMatchCondition(cMatchRoom, playerA, playerB, 0, "hSD01-019", true, true, 0, false, _DuelAction.targetCard.cardPosition, _DuelAction.targetCard.cardNumber, false, _DuelAction.cheerCostCard);
                    break;
                case "hBP01-103":

                    energyPaid = PayCardEffectCheerFieldCost(cMatchRoom, playerA, playerB, _DuelAction.cheerCostCard.cardPosition, _DuelAction.cheerCostCard.cardNumber, _DuelAction.usedCard.cardNumber);

                    if (!energyPaid)
                        break;
                    UseCardEffectDrawXAddIfMatchCondition(cMatchRoom, playerA, playerB, 0, "hBP01-103", true, true, 0, true, _DuelAction.targetCard.cardPosition, _DuelAction.targetCard.cardNumber, false, _DuelAction.cheerCostCard);
                    break;
                case "hBP01-105":

                    energyPaid = PayCardEffectCheerFieldCost(cMatchRoom, playerA, playerB, _DuelAction.cheerCostCard.cardPosition, _DuelAction.cheerCostCard.cardNumber, _DuelAction.usedCard.cardNumber);

                    if (!energyPaid)
                        break;

                    List<Card> tempHand = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
                    tempHand.Add(_DuelAction.usedCard);
                    tempHand.Add(_DuelAction.cheerCostCard);
                    tempHand.Add(_DuelAction.targetCard);

                    UseCardEffectDrawXAddIfMatchCondition(cMatchRoom, playerA, playerB, 0, "hBP01-105", true, true, 0, false, _DuelAction.targetCard.cardPosition, _DuelAction.targetCard.cardNumber, false, _DuelAction.cheerCostCard);
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

                    UseCardEffectDrawXAddIfMatchCondition(cMatchRoom, playerA, playerB, 0, "hSD01-020", true, true, 0, false, _DuelAction.targetCard.cardPosition, _DuelAction.targetCard.cardNumber, false, _DuelAction.cheerCostCard);
                    break;
                case "hSD01-017":
                    tempHand = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerAHand : cMatchRoom.playerBHand;

                    List<Card> tempDeck = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerADeck : cMatchRoom.playerBDeck;

                    if (tempHand.Count < 2) {
                        Lib.WriteConsoleMessage("Player dont have enough cards in the hand to activatehSD01-017");
                        return;
                    }

                    cMatchRoom.suffleHandToTheDeck(tempDeck, tempHand);
                    cMatchRoom.ShuffleCards(tempDeck);
                    Lib.getCardFromDeck(tempDeck, tempHand, 5);

                    if (cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID)
                        cMatchRoom.playerALimiteCardPlayed.Add(new Card() { cardNumber = "hSD01-017" });
                    else
                        cMatchRoom.playerBLimiteCardPlayed.Add(new Card() { cardNumber = "hSD01-017" });

                    _DuelAction.playerID = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.startPlayer : cMatchRoom.secondPlayer;
                    _DuelAction.suffle = true;
                    _DuelAction.zone = "Deck";
                    _DuelAction.cardList = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerAHand : cMatchRoom.playerBHand;

                    _DuelAction = new() { usedCard = new Card() {cardNumber = "hSD01-017" }, actionType = "SuffleAllThenDraw", playerID = cMatchRoom.currentPlayerTurn};

                    pReturnData = new RequestData { type = "GamePhase", description = "Draw", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                    Console.WriteLine(pReturnData);
                    if (cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID)
                    {
                        Lib.SendMessage(playerConnections[cMatchRoom.playerA.PlayerID.ToString()], pReturnData);
                        _DuelAction.cardList = cMatchRoom.FillCardListWithEmptyCards(_DuelAction.cardList);
                        pReturnData = new RequestData { type = "GamePhase", description = "Draw", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                        Lib.SendMessage(playerConnections[cMatchRoom.playerB.PlayerID.ToString()], pReturnData);
                    }
                    else
                    {
                        Lib.SendMessage(playerConnections[cMatchRoom.playerB.PlayerID.ToString()], pReturnData);
                        _DuelAction.cardList = cMatchRoom.FillCardListWithEmptyCards(_DuelAction.cardList);
                        pReturnData = new RequestData { type = "GamePhase", description = "Draw", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                        Lib.SendMessage(playerConnections[cMatchRoom.playerA.PlayerID.ToString()], pReturnData);
                    }
                    break;
            }




        }






        async Task UseCardEffectDrawXAddIfMatchCondition(MatchRoom cMatchRoom, int playerA, int playerB, int cNum, string cUsedNumber, bool LimiteUseCard, bool result, int HandMustHave, bool needEnergy = false, string zone = "", string costCardnumber = "", bool reveal = false, Card cheercost = null)
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

            DuelAction _Draw = new DuelAction()
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
                targetCard = new Card() { cardNumber = costCardnumber, cardPosition = zone },
                cheerCostCard = cheercost,
            };
            cMatchRoom.currentCardResolving = cUsedNumber;
            cMatchRoom.currentGamePhase = GAMEPHASE.ConditionedDraw;

            SendPlayerData(cMatchRoom, reveal, _Draw, DuelActionResponse, "SuporteEffectDrawXAddIf", LimiteUseCard, cUsedNumber, result);
        }

        async Task UseCardEffectDrawXAddIfBetweenReview(MatchRoom cMatchRoom, int playerA, int playerB, int cNum, string cUsedNumber, bool LimiteUseCard, bool result, int HandMustHave, bool needEnergy = false, string zone = "", string costCardnumber = "", bool reveal = false)
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


            DuelAction _Draw = new DuelAction()
            {
                playerID = cMatchRoom.currentPlayerTurn,
                suffle = false,
                zone = "Deck",
                cardList = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand
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

        async Task UseCardEffectDrawAsync(MatchRoom cMatchRoom, int playerA, int playerB, int cNum, string cUsedNumber, bool LimiteUseCard, bool result)
        {
            if (playerA == cMatchRoom.currentPlayerTurn)
                Lib.getCardFromDeck(cMatchRoom.playerADeck, cMatchRoom.playerAHand, cNum);
            else
                Lib.getCardFromDeck(cMatchRoom.playerBDeck, cMatchRoom.playerBHand, cNum);

            DuelAction _Draw = new DuelAction()
            {
                playerID = cMatchRoom.currentPlayerTurn,
                suffle = false,
                zone = "Deck",
                //getting the range of cards from the player hand, then getting the last ones to add to the draw
                cardList = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerAHand.GetRange(cMatchRoom.playerAHand.Count() - cNum, cNum) : cMatchRoom.playerBHand.GetRange(cMatchRoom.playerBHand.Count() - cNum, cNum)
            };

            DuelAction DuelActionResponse = new DuelAction()
            {
                playerID = cMatchRoom.currentPlayerTurn,
                actionType = "doDraw",
                usedCard = new Card() { cardNumber = cUsedNumber }
            };

            SendPlayerData(cMatchRoom, false, _Draw, DuelActionResponse, "SuporteEffectDraw", LimiteUseCard, cUsedNumber, result);
        }





        async Task SendPlayerData(MatchRoom cMatchRoom, bool reveal, DuelAction _Draw, DuelAction DuelActionResponse, string description, bool LimiteUseCard, string cUsedNumber, bool result)
        {
            RequestData _ReturnData;
            int otherPlayer = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerB.PlayerID : cMatchRoom.playerA.PlayerID;

            // Serialize and send data to the current player
            DuelActionResponse.actionObject = JsonSerializer.Serialize(_Draw);
            _ReturnData = new RequestData { type = "GamePhase", description = description, requestObject = JsonSerializer.Serialize(DuelActionResponse, Lib.options) };

            Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn.ToString()], _ReturnData);

            // Handle reveal logic and send data to the other player
            if (reveal == false)
            {
                _Draw.cardList = cMatchRoom.FillCardListWithEmptyCards(_Draw.cardList);
                DuelActionResponse.actionObject = JsonSerializer.Serialize(_Draw);
            }

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




        bool PayCardEffectCheerFieldCost(MatchRoom cMatchRoom, int playerA, int playerB, string zone, string cardNumber, string resolvingCard)
        {

            Card seletectedCard = new();

            switch (zone)
            {
                case "Favourite":
                    seletectedCard = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerAFavourite : cMatchRoom.playerBFavourite;
                    break;
                case "Collaboration":
                    seletectedCard = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerACollaboration : cMatchRoom.playerBCollaboration;
                    break;
                case "Stage":
                    seletectedCard = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerAStage : cMatchRoom.playerBStage;
                    break;
                case "BackStage1":
                case "BackStage2":
                case "BackStage3":
                case "BackStage4":
                case "BackStage5":
                    List<Card> seletectedCardList;
                    seletectedCardList = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerABackPosition : cMatchRoom.playerBBackPosition;
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
        bool UseCardEffectToSummom(MatchRoom cMatchRoom, int playerA, int playerB, string zone, string cUsedNumber, bool result, string bloomLevel)
        {

            List<Card> query = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerADeck : cMatchRoom.playerBDeck;

            foreach (var card in query)
                card.GetCardInfo(card.cardNumber); // Assuming this method fills card info, including bloomLevel

            query = query.Where(r => r.bloomLevel == bloomLevel).ToList();

            DuelAction _Draw = new DuelAction()
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



    }
}