using hololive_oficial_cardgame_server.SerializableObjects;
using hololive_oficial_cardgame_server.WebSocketDuelFunctions;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.Extensions.Options;
using System.Net.WebSockets;
using System.Text.Json;
using static hololive_oficial_cardgame_server.SerializableObjects.MatchRoom;
using static Mysqlx.Notice.Warning.Types;
using static System.Net.Mime.MediaTypeNames;

namespace hololive_oficial_cardgame_server.EffectControllers
{
    class SupportEffects
    {
        internal static async Task OnSupportEffectsAsync(DuelAction _DuelAction, MatchRoom cMatchRoom, PlayerRequest playerRequest = null, WebSocket webSocket = null)
        {

            if (_DuelAction.targetCard != null)
                _DuelAction.targetCard.GetCardInfo();
            if (_DuelAction.usedCard != null)
                _DuelAction.usedCard.GetCardInfo();
            if (_DuelAction.cheerCostCard != null)
                _DuelAction.cheerCostCard.GetCardInfo();

            PlayerRequest pReturnData;
            List<Card> holoPowerList = new();
            List<Card> backPos = new();
            List<string> returnToclient = new();

            List<Card> playerHand = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerAHand : cMatchRoom.playerBHand;
            List<Card> playerArquive = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerAArquive : cMatchRoom.playerBArquive;
            List<Card> playerDeck = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerADeck : cMatchRoom.playerBDeck;
            List<Card> playerTempHand = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
            List<Card> playerCheer = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerACardCheer : cMatchRoom.playerBCardCheer;
            Card playerStage = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerAStage : cMatchRoom.playerBStage;
            List<Card> playerBackstage = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerABackPosition : cMatchRoom.playerBBackPosition;
            List<Card> tempArquive = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerAArquive : cMatchRoom.playerBArquive;
            List<int> diceList = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerADiceRollList : cMatchRoom.playerBDiceRollList;
            List<Card> tempOpponentBackStage = cMatchRoom.currentPlayerTurn != cMatchRoom.playerA.PlayerID ? cMatchRoom.playerABackPosition : cMatchRoom.playerBBackPosition;
            //check if can play limited cards, and also add to limited list
            List<Card> playerLimitedCardsPlayed = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerALimiteCardPlayed : cMatchRoom.playerBLimiteCardPlayed;
            if (_DuelAction.usedCard.cardType.Equals("サポート・スタッフ・LIMITED"))
            {
                if (playerLimitedCardsPlayed.Count() > 0)
                {
                    Lib.WriteConsoleMessage("Already played limited card this turn");
                    return;
                }
                playerLimitedCardsPlayed.Add(new Card(_DuelAction.usedCard.cardNumber));
            }

            //checking if the player has the card in the hand and getting the pos
            int handPos = Lib.CheckIfCardExistInPlayerHand(cMatchRoom, playerRequest.playerID, _DuelAction.usedCard.cardNumber);
            if (handPos == -1)
            {
                Lib.WriteConsoleMessage("No match found in the player hand");
                return;
            }

            //
            //MAYBE I NEED TO CHECK UPHERE IF THE USED CARD EXIST IN THE PLAYER HAND
            //


            bool energyPaid = false;

            if (string.IsNullOrEmpty(cMatchRoom.currentCardResolving))
                cMatchRoom.currentCardResolving = cMatchRoom.currentCardResolving = _DuelAction.usedCard.cardNumber;

            try
            {

                switch (_DuelAction.usedCard.cardNumber + cMatchRoom.currentCardResolvingStage)
                {
                    case "hBP01-103":

                        energyPaid = Lib.PayCardEffectCheerFieldCost(cMatchRoom, _DuelAction.cheerCostCard.cardPosition, _DuelAction.cheerCostCard.cardNumber);

                        if (!energyPaid)
                            break;

                        cMatchRoom.currentCardResolvingStage = "1";
                        cMatchRoom.currentGamePhase = GAMEPHASE.ConditionedDraw;

                        //getdeck
                        playerDeck = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerADeck : cMatchRoom.playerBDeck;
                        foreach (Card c in playerDeck)
                        {
                            c.GetCardInfo();
                        }
                        //get valid cards from deck
                        List<Card> queryy = playerDeck.Where(r => r.bloomLevel == "1st" && !(r.cardType == "Buzzホロメン")).ToList();
                        queryy.AddRange(playerDeck.Where(r => r.bloomLevel == "Debut" && !(r.cardType == "Buzzホロメン")).ToList());

                        playerTempHand.AddRange(queryy);

                        //send to player the info
                        UseCardEffectDrawXAddIfMatchCondition(cMatchRoom, queryy, _DuelAction, false);
                        break;
                    case "hBP01-1031":
                        Card selected = new Card(_DuelAction.actionObject);

                        bool validSelection = false;
                        foreach (Card card in playerTempHand)
                        {
                            if (card.cardNumber.Equals(selected.cardNumber))
                            {
                                validSelection = true;
                            }
                        }

                        if (!validSelection)
                        {
                            Lib.WriteConsoleMessage($"invalid selection for {_DuelAction.usedCard.cardNumber}");
                            return;
                        }
                        int tempIndex = -1;
                        int tempCounter = 0;
                        foreach (Card card in playerDeck)
                        {
                            if (card.cardNumber.Equals(selected.cardNumber))
                            {
                                tempIndex = tempCounter;
                            }
                            tempCounter++;
                        }
                        playerDeck.RemoveAt(tempIndex);

                        cMatchRoom.ShuffleCards(playerDeck);

                        _DuelAction.playerID = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.firstPlayer : cMatchRoom.secondPlayer;
                        _DuelAction.suffle = true;
                        _DuelAction.zone = "Deck";
                        _DuelAction.cardList.Clear();
                        _DuelAction.cardList.Add(selected);
                        playerHand.Add(selected);

                        SendPlayerData(cMatchRoom, true, _DuelAction, "SupportEffectDraw");

                        ResetResolution();
                        break;
                    case "hBP01-102":
                        if (playerTempHand.Count > 7)
                        {
                            Lib.WriteConsoleMessage("too many cards to activate");
                            return;
                        }

                        cMatchRoom.currentCardResolvingStage = "1";
                        cMatchRoom.currentGamePhase = GAMEPHASE.ConditionedDraw;

                        UseCardEffectDrawXAmountAddAnyIfConditionMatchThenReorderToBottom(cMatchRoom, 4, 0, false);
                        break;
                    case "hBP01-1021":
                        List<Record> limitedSuport = FileReader.QueryRecords(null, null, null, "歌");

                        List<string> possibleDraw = new List<string>();
                        foreach (Record r in limitedSuport)
                        {
                            possibleDraw.Add(r.CardNumber);
                        }

                        //check if select match what player have in the server information 
                        if (!Lib.HaveSameWords(Lib.CardListToStringList(playerTempHand), _DuelAction.SelectedCards))
                        {
                            Lib.WriteConsoleMessage("select cards didnt match the server info");
                            return;
                        }
                        FromTheListAddFirstToHandThenAddRemainingToBottom(cMatchRoom, possibleDraw, _DuelAction, true, possibleDraw.Count, "number");
                        ResetResolution();
                        break;
                    case "hSD01-016":
                        UseCardEffectDrawAnyAsync(cMatchRoom, 3, "hSD01-016");
                        ResetResolution();
                        break;
                    case "hSD01-017":


                        if (playerHand.Count < 2)
                        {
                            Lib.WriteConsoleMessage("Player dont have enough cards in the hand to activate hSD01-017");
                            return;
                        }

                        cMatchRoom.suffleHandToTheDeck(playerDeck, playerHand);
                        cMatchRoom.ShuffleCards(playerDeck);
                        Lib.getCardFromDeck(playerDeck, playerHand, 5);

                        _DuelAction.playerID = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.firstPlayer : cMatchRoom.secondPlayer;
                        _DuelAction.suffle = true;
                        _DuelAction.zone = "Deck";
                        _DuelAction.cardList = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerAHand : cMatchRoom.playerBHand;
                        _DuelAction.usedCard = new Card("hSD01-017");
                        _DuelAction.suffleBackToDeck = true;

                        SendPlayerData(cMatchRoom, false, _DuelAction, "SupportEffectDraw");
                        ResetResolution();
                        break;
                    case "hSD01-018":
                        cMatchRoom.currentCardResolvingStage = "1";
                        cMatchRoom.currentGamePhase = GAMEPHASE.ConditionedDraw;

                        UseCardEffectDrawXAmountAddAnyIfConditionMatchThenReorderToBottom(cMatchRoom, 5, 0, false);
                        break;
                    case "hSD01-0181":
                        limitedSuport = FileReader.QueryRecords(null, "サポート・アイテム・LIMITED", null, null);
                        limitedSuport.AddRange(FileReader.QueryRecords(null, "サポート・イベント・LIMITED", null, null));
                        limitedSuport.AddRange(FileReader.QueryRecords(null, "サポート・スタッフ・LIMITED", null, null));

                        possibleDraw = new List<string>();
                        foreach (Record r in limitedSuport)
                        {
                            possibleDraw.Add(r.CardNumber);
                        }

                        //check if select match what player have in the server information 
                        if (!Lib.HaveSameWords(Lib.CardListToStringList(playerTempHand), _DuelAction.SelectedCards))
                        {
                            Lib.WriteConsoleMessage("select cards didnt match the server info");
                            return;
                        }
                        FromTheListAddFirstToHandThenAddRemainingToBottom(cMatchRoom, possibleDraw, _DuelAction, true, possibleDraw.Count, "number");
                        ResetResolution();
                        break;
                    case "hSD01-019":

                        energyPaid = Lib.PayCardEffectCheerFieldCost(cMatchRoom, _DuelAction.cheerCostCard.cardPosition, _DuelAction.cheerCostCard.cardNumber);

                        if (!energyPaid)
                            break;

                        cMatchRoom.currentCardResolvingStage = "1";
                        cMatchRoom.currentGamePhase = GAMEPHASE.ConditionedDraw;

                        //getdeck
                        playerDeck = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerADeck : cMatchRoom.playerBDeck;
                        foreach (Card c in playerDeck)
                        {
                            c.GetCardInfo();
                        }
                        //get valid cards from deck
                        queryy = playerDeck.Where(r => r.bloomLevel == "1st" && !(r.cardType == "Buzzホロメン")).ToList();
                        queryy.AddRange(playerDeck.Where(r => r.bloomLevel == "2nd" && !(r.cardType == "Buzzホロメン")).ToList());

                        playerTempHand.AddRange(queryy);

                        //send to player the info
                        UseCardEffectDrawXAddIfMatchCondition(cMatchRoom, queryy, _DuelAction, false);
                        break;
                    case "hSD01-0191":
                        selected = new Card(_DuelAction.actionObject);

                        validSelection = false;
                        foreach (Card card in playerTempHand)
                        {
                            if (card.cardNumber.Equals(selected.cardNumber))
                            {
                                validSelection = true;
                            }
                        }

                        if (!validSelection)
                        {
                            Lib.WriteConsoleMessage("invalid selection for hSD01-0191");
                            return;
                        }
                        tempIndex = -1;
                        tempCounter = 0;
                        foreach (Card card in playerDeck)
                        {
                            if (card.cardNumber.Equals(selected.cardNumber))
                            {
                                tempIndex = tempCounter;
                            }
                            tempCounter++;
                        }
                        playerDeck.RemoveAt(tempIndex);

                        cMatchRoom.ShuffleCards(playerDeck);

                        _DuelAction.playerID = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.firstPlayer : cMatchRoom.secondPlayer;
                        _DuelAction.suffle = true;
                        _DuelAction.zone = "Deck";
                        _DuelAction.cardList.Clear();
                        _DuelAction.cardList.Add(selected);
                        playerHand.Add(selected);

                        SendPlayerData(cMatchRoom, true, _DuelAction, "SupportEffectDraw");

                        ResetResolution();
                        break;

                    case "hSD01-020":
                        int diceValue = Lib.GetDiceNumber(cMatchRoom, cMatchRoom.currentPlayerTurn);
                        cMatchRoom.currentCardResolvingStage = "1";

                        Lib.SendDiceRoll(cMatchRoom, diceValue, COUNTFORRESONSE: false);
                        break;
                    case "hSD01-0201":
                        diceValue = diceList.Last();

                        //getting selectable energys from the arquive
                        foreach (Card card in playerArquive)
                        {
                            card.GetCardInfo();
                            if (card.cardType.Equals("エール"))
                            {
                                card.playedFrom = "Arquive";
                                playerTempHand.Add(card);
                            }
                        }
                        if (playerTempHand.Count == 0)
                        {
                            ResetResolution();
                            return;
                        }

                        if (diceValue < 3)
                        {
                            ResetResolution();
                            return;
                        }
                        returnToclient = new List<string>() { diceValue.ToString() };

                        //updating current resolving state to wait for the repsonse
                        cMatchRoom.currentCardResolvingStage = "2";

                        //send the info to the currentplayer so he can pick the card
                        _DuelAction.actionObject = JsonSerializer.Serialize(returnToclient, Lib.options);
                        _DuelAction.cardList = playerTempHand;
                        pReturnData = new PlayerRequest { type = "DuelUpdate", description = "ResolveOnSupportEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };

                        Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn.ToString()], pReturnData);
                        Lib.SendMessage(MessageDispatcher.playerConnections[MatchRoom.GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn).ToString()], pReturnData);
                        break;
                    case "hSD01-0202":
                        var handler190 = new AttachTopCheerEnergyToBackHandler(MessageDispatcher.playerConnections, MessageDispatcher._MatchRooms);
                        await handler190.AttachCheerEnergyHandleAsync(_DuelAction, cMatchRoom, stage: true, collab: true, back: true, TOPCHEERDECK: false, FULLCHEERDECK: false, energyIndex: 1, ARQUIVEFULLDECK: true);
                        ResetResolution();
                        break;
                    case "hSD01-021":
                        cMatchRoom.currentCardResolvingStage = "1";
                        cMatchRoom.currentGamePhase = GAMEPHASE.ConditionedDraw;

                        UseCardEffectDrawXAmountAddAnyIfConditionMatchThenReorderToBottom(cMatchRoom, 4, 0, true);
                        break;
                    case "hSD01-0211":
                        List<string> selectableList = new() { "ときのそら", "AZKi", "SorAZ" };
                        //SORAZ counts as both

                        //check if select match what player have in the server information 
                        if (!Lib.HaveSameWords(Lib.CardListToStringList(playerTempHand), _DuelAction.SelectedCards))
                        {
                            Lib.WriteConsoleMessage("select cards didnt match the server info");
                            return;
                        }

                        FromTheListAddFirstToHandThenAddRemainingToBottom(cMatchRoom, selectableList, _DuelAction, true, 4, "name");
                        ResetResolution();
                        break;
                    case "hBP01-104":
                        cMatchRoom.currentCardResolvingStage = "1";
                        cMatchRoom.currentGamePhase = GAMEPHASE.ConditionedDraw;

                        UseCardEffectToSummom(cMatchRoom, "Deck", _DuelAction.usedCard.cardNumber, "Debut");
                        break;
                    case "hBP01-1041":
                        int n = -1;
                        for (int j = 0; n < playerDeck.Count; j++)
                        {
                            playerDeck[j].GetCardInfo();
                            if (playerDeck[j].cardNumber.Equals(_DuelAction.actionObject) && playerDeck[j].bloomLevel.Equals("Debut"))
                            {
                                n = j;
                                break;
                            }
                        }
                        if (n > 0)
                        {
                            Lib.WriteConsoleMessage("invalid selected card");
                        }

                        playerDeck.RemoveAt(n);
                        cMatchRoom.playerBDeck = cMatchRoom.ShuffleCards(cMatchRoom.playerBDeck);
                        MainConditionedSummomResponseHandleAsync(cMatchRoom, playerRequest.playerID, _DuelAction.actionObject);
                        ResetResolution();
                        break;
                    case "hBP01-105":
                        energyPaid = Lib.PayCardEffectCheerFieldCost(cMatchRoom, _DuelAction.cheerCostCard.cardPosition, _DuelAction.cheerCostCard.cardNumber);

                        if (!energyPaid)
                            break;

                        cMatchRoom.currentCardResolvingStage = "1";
                        cMatchRoom.currentGamePhase = GAMEPHASE.ConditionedDraw;

                        List<Card> cardListToRreturn = new();
                        //getdeck
                        foreach (Card c in playerCheer)
                        {
                            c.GetCardInfo();
                            _DuelAction.usedCard.GetCardInfo();
                            if (MatchCardColors(c, _DuelAction.usedCard))
                            {
                                //we add the played from to match the validation the function already did for other effects
                                c.playedFrom = "CardCheer";
                                cardListToRreturn.Add(c);
                            }
                        }

                        //if theres none, player cannot attach, so reset the effect resolution
                        if (cardListToRreturn.Count == 0)
                        {
                            ResetResolution();
                            return;
                        }

                        playerTempHand.AddRange(cardListToRreturn);

                        //send to player the info
                        UseCardEffectDrawXAddIfMatchCondition(cMatchRoom, cardListToRreturn, _DuelAction, false);
                        break;
                    case "hBP01-1051":
                        var handler1051 = new AttachTopCheerEnergyToBackHandler(MessageDispatcher.playerConnections, MessageDispatcher._MatchRooms);
                        await handler1051.AttachCheerEnergyHandleAsync(_DuelAction, cMatchRoom, stage: true, collab: true, back: true, TOPCHEERDECK: false, FULLCHEERDECK: true, energyIndex: 1);
                        ResetResolution();
                        break;
                    case "hBP01-106":
                        if (!(_DuelAction.targetCard.cardPosition.Equals("BackStage1") || _DuelAction.targetCard.cardPosition.Equals("BackStage2") || _DuelAction.targetCard.cardPosition.Equals("BackStage3") ||
                            _DuelAction.targetCard.cardPosition.Equals("BackStage4") || _DuelAction.targetCard.cardPosition.Equals("BackStage5")))
                        {
                            Lib.WriteConsoleMessage("Invalid target position");
                            return;
                        }

                        Lib.SwittchCardYToCardZButKeepPosition(cMatchRoom, playerRequest.playerID, _DuelAction.targetCard);

                        SendPlayerData(cMatchRoom, false, _DuelAction, "SwitchStageCard");
                        ResetResolution();
                        break;
                    case "hBP01-108":
                        if (!(_DuelAction.targetCard.cardPosition.Equals("BackStage1") || _DuelAction.targetCard.cardPosition.Equals("BackStage2") || _DuelAction.targetCard.cardPosition.Equals("BackStage3") ||
                            _DuelAction.targetCard.cardPosition.Equals("BackStage4") || _DuelAction.targetCard.cardPosition.Equals("BackStage5")))
                        {
                            Lib.WriteConsoleMessage("Invalid target position");
                            return;
                        }

                        Lib.SwittchCardYToCardZButKeepPosition(cMatchRoom, MatchRoom.GetOtherPlayer(cMatchRoom, playerRequest.playerID), _DuelAction.targetCard);

                        SendPlayerData(cMatchRoom, false, _DuelAction, "SwitchOpponentStageCard");
                        ResetResolution();
                        break;
                    case "hBP01-109":
                        if (playerTempHand.Count > 7)
                        {
                            Lib.WriteConsoleMessage("too many cards to activate");
                            return;
                        }

                        cMatchRoom.currentCardResolvingStage = "1";
                        cMatchRoom.currentGamePhase = GAMEPHASE.ConditionedDraw;

                        UseCardEffectDrawXAmountAddAnyIfConditionMatchThenReorderToBottom(cMatchRoom, 4, 0, false);
                        break;
                    case "hBP01-1091":
                        limitedSuport = FileReader.QueryRecords("兎田ぺこら", null, null, null);
                        limitedSuport.AddRange(FileReader.QueryRecords("ムーナ・ホシノヴァ", null, null, null));

                        possibleDraw = new List<string>();
                        foreach (Record r in limitedSuport)
                        {
                            possibleDraw.Add(r.CardNumber);
                        }

                        //check if select match what player have in the server information 
                        if (!Lib.HaveSameWords(Lib.CardListToStringList(playerTempHand), _DuelAction.SelectedCards))
                        {
                            Lib.WriteConsoleMessage("select cards didnt match the server info");
                            return;
                        }
                        FromTheListAddFirstToHandThenAddRemainingToBottom(cMatchRoom, possibleDraw, _DuelAction, true, possibleDraw.Count, "number");
                        ResetResolution();
                        break;
                    case "hBP01-111":
                        if (playerTempHand.Count > 7)
                        {
                            Lib.WriteConsoleMessage("too many cards to activate");
                            return;
                        }

                        cMatchRoom.currentCardResolvingStage = "1";
                        cMatchRoom.currentGamePhase = GAMEPHASE.ConditionedDraw;

                        UseCardEffectDrawXAmountAddAnyIfConditionMatchThenReorderToBottom(cMatchRoom, 4, 0, false);
                        break;
                    case "hBP01-1111":
                        limitedSuport = FileReader.QueryRecords(null, null, null, "#ID３期生");

                        possibleDraw = new List<string>();
                        foreach (Record r in limitedSuport)
                        {
                            possibleDraw.Add(r.CardNumber);
                        }

                        //check if select match what player have in the server information 
                        if (!Lib.HaveSameWords(Lib.CardListToStringList(playerTempHand), _DuelAction.SelectedCards))
                        {
                            Lib.WriteConsoleMessage("select cards didnt match the server info");
                            return;
                        }
                        FromTheListAddFirstToHandThenAddRemainingToBottom(cMatchRoom, possibleDraw, _DuelAction, true, possibleDraw.Count, "number");
                        ResetResolution();
                        break;
                    case "hBP01-113":
                        if (playerTempHand.Count > 7)
                        {
                            Lib.WriteConsoleMessage("too many cards to activate");
                            return;
                        }

                        cMatchRoom.currentCardResolvingStage = "1";
                        cMatchRoom.currentGamePhase = GAMEPHASE.ConditionedDraw;

                        UseCardEffectDrawXAmountAddAnyIfConditionMatchThenReorderToBottom(cMatchRoom, 4, 0, false);
                        break;
                    case "hBP01-1131":
                        limitedSuport = FileReader.QueryRecords(null, null, null, "#Promise");

                        possibleDraw = new List<string>();
                        foreach (Record r in limitedSuport)
                        {
                            possibleDraw.Add(r.CardNumber);
                        }

                        //check if select match what player have in the server information 
                        if (!Lib.HaveSameWords(Lib.CardListToStringList(playerTempHand), _DuelAction.SelectedCards))
                        {
                            Lib.WriteConsoleMessage("select cards didnt match the server info");
                            return;
                        }
                        FromTheListAddFirstToHandThenAddRemainingToBottom(cMatchRoom, possibleDraw, _DuelAction, true, possibleDraw.Count, "number");
                        ResetResolution();
                        break;
                    case "hBP01-107":
                        //SEND A LIST, PICK  TIL 3 FROM, SEND PICKED CARDS BACK, ADD PICKED TO HAND, IGNORE REST
                        if (playerTempHand.Count > 7)
                        {
                            Lib.WriteConsoleMessage("too many cards to activate");
                            return;
                        }

                        cMatchRoom.currentCardResolvingStage = "1";
                        cMatchRoom.currentGamePhase = GAMEPHASE.ConditionedDraw;

                        foreach (Card card in tempArquive)
                        {

                            if (card.cardNumber.Equals("hY04-001") || card.cardNumber.Equals("hY02-001") || card.cardNumber.Equals("hY03-001") || card.cardNumber.Equals("hY01-001"))
                            {
                                playerTempHand.Add(card);
                            }
                        }

                        if (playerTempHand.Count == 0)
                        {
                            ResetResolution();
                            return;
                        }

                        DuelAction DuelActionResponse = new DuelAction()
                        {
                            playerID = cMatchRoom.currentPlayerTurn,
                            usedCard = new Card(cMatchRoom.currentCardResolving),
                            suffle = false,
                            zone = "Deck",
                            cardList = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand
                        };

                        SendPlayerData(cMatchRoom, true, DuelActionResponse, "ResolveOnSupportEffect");
                        break;
                    case "hBP01-1071":
                        n = -1;
                        int m = 0;
                        foreach (string cardSelected in _DuelAction.SelectedCards)
                        {
                            foreach (Card c in tempArquive)
                            {
                                if (cardSelected.Equals(c.cardNumber))
                                {
                                    n = m;
                                    break;
                                }
                                m++;
                            }
                            if (n > 0)
                            {
                                _DuelAction.cardList.Add(tempArquive[n]);
                                playerHand.Add(tempArquive[n]);
                                tempArquive.RemoveAt(n);
                            }
                        }
                        if (n == -1)
                        {
                            Lib.WriteConsoleMessage("invalid selection");
                            return;
                        }
                        _DuelAction.zone = "Arquive";

                        SendPlayerData(cMatchRoom, reveal: true, _DuelAction, "SupportEffectDraw");
                        ResetResolution();
                        break;
                    case "hBP01-112":
                        diceValue = Lib.GetDiceNumber(cMatchRoom, cMatchRoom.currentPlayerTurn);
                        cMatchRoom.currentCardResolvingStage = "1";

                        Lib.SendDiceRoll(cMatchRoom, diceValue, COUNTFORRESONSE: false);
                        break;
                    case "hBP01-1121":
                        diceValue = diceList.Last();

                        if (diceValue < 4)
                        {
                            ResetResolution();
                            return;
                        }

                        n = -1;
                        m = 0;
                        foreach (Card card in tempOpponentBackStage)
                        {
                            if (_DuelAction.targetCard.cardNumber.Equals(card.cardNumber) && _DuelAction.targetCard.cardPosition.Equals(card.cardPosition))
                            {
                                n = m;
                                break;
                            }
                            m++;
                        }
                        if (n == -1)
                        {
                            Lib.WriteConsoleMessage("Invalid target position");
                            ResetResolution();
                            return;
                        }
                        tempOpponentBackStage[n].GetCardInfo();
                        tempOpponentBackStage[n].effectDamageRecieved += 20;
                        tempOpponentBackStage[n].currentHp -= 20;

                        _DuelAction = new()
                        {
                            playerID = GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn),
                            targetCard = tempOpponentBackStage[n],
                        };

                        if (int.Parse(tempOpponentBackStage[n].hp) <= -1 * tempOpponentBackStage[n].currentHp)
                        {
                            List<Card> arquive = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerAArquive : cMatchRoom.playerBArquive;
                            //adding cards from the deafeat holomem to the arquive
                            arquive.AddRange(tempOpponentBackStage[n].attachedEnergy);
                            arquive.AddRange(tempOpponentBackStage[n].bloomChild);
                            arquive.Add(tempOpponentBackStage[n]);
                            SendPlayerData(cMatchRoom, reveal: true, _DuelAction, "DefeatedHoloMemberByEffect");
                        }
                        else
                        {
                            _DuelAction.actionObject = "20";
                            _DuelAction.playerID = cMatchRoom.currentPlayerTurn;
                            // Serialize and send data to the current player
                            PlayerRequest _ReturnData = new PlayerRequest { type = "DuelUpdate", description = "InflicDamageToHolomem", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                            Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer], _ReturnData);
                            Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer], _ReturnData);
                        }

                        ResetResolution();
                        break;
                    case "hBP01-110":// MUMEI SUPORT NOT DONE YET
                        // MUMEI SUPORT NOT DONE YET// MUMEI SUPORT NOT DONE YET// MUMEI SUPORT NOT DONE YET
                        // MUMEI SUPORT NOT DONE YET// MUMEI SUPORT NOT DONE YET// MUMEI SUPORT NOT DONE YET
                        //random dice number
                        diceValue = Lib.GetDiceNumber(cMatchRoom, cMatchRoom.currentPlayerTurn);
                        cMatchRoom.currentCardResolvingStage = "1";

                        Lib.SendDiceRoll(cMatchRoom, diceValue, COUNTFORRESONSE: false);
                        break;
                    case "hBP01-1101":
                        diceValue = diceList.Last();

                        cMatchRoom.currentCardResolvingStage = "1";
                        returnToclient = new List<string>() { diceValue.ToString() };

                        _DuelAction = new()
                        {
                            playerID = GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn),
                            actionObject = JsonSerializer.Serialize(returnToclient, Lib.options)
                        };

                        pReturnData = new PlayerRequest { type = "DuelUpdate", description = "RollDice", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                        Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn.ToString()], pReturnData);

                        if (diceValue > 3)
                        {
                            ResetResolution();
                            return;
                        }
                        break;
                    case "hBP01-1102":


                        break;
                    default:
                        ResetResolution();
                        break;

                }
            }
            catch (Exception e)
            {
                Lib.WriteConsoleMessage(e.Message + e.StackTrace + e.InnerException);
            }
            void ResetResolution()
            {
                int indexInHand = Lib.CheckIfCardExistInPlayerHand(cMatchRoom, cMatchRoom.currentPlayerTurn, cMatchRoom.currentCardResolving);
                if (indexInHand > -1)
                {
                    playerArquive.Add(playerHand[indexInHand]);
                    playerHand.RemoveAt(indexInHand);
                }

                //inform oponnent to add used card to the arquive
                DuelAction _DisposeAction = new()
                {
                    playerID = cMatchRoom.currentPlayerTurn,
                    usedCard = new(cMatchRoom.currentCardResolving),
                };

                pReturnData = new PlayerRequest { type = "DuelUpdate", description = "DisposeUsedSupport", requestObject = JsonSerializer.Serialize(_DisposeAction, Lib.options) };
                Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer], pReturnData);
                Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer], pReturnData);

                if (cMatchRoom.extraInfo != null)
                    cMatchRoom.extraInfo.Clear();
                cMatchRoom.currentCardResolving = "";
                cMatchRoom.currentCardResolvingStage = "";
                List<Card> temphand = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
                cMatchRoom.currentDuelActionResolvingRecieved.Clear();
                cMatchRoom.currentGameHigh++;
                cMatchRoom.currentGamePhase = GAMEPHASE.MainStep;
                temphand.Clear();

            }
        }


        static async Task UseCardEffectDrawXAddIfMatchCondition(MatchRoom cMatchRoom, List<Card> queryy, DuelAction _DuelAction, bool reveal = false)
        {

            List<Card> query = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerADeck : cMatchRoom.playerBDeck;

            foreach (var card in query)
                card.GetCardInfo();

            DuelAction DuelActionResponse = new DuelAction()
            {
                playerID = cMatchRoom.currentPlayerTurn,
                usedCard = new Card(_DuelAction.usedCard.cardNumber),
                targetCard = _DuelAction.targetCard,
                cheerCostCard = _DuelAction.cheerCostCard,
                suffle = false,
                zone = "Deck",
                cardList = queryy
            };

            SendPlayerData(cMatchRoom, reveal, DuelActionResponse, "ResolveOnSupportEffect");
        }

        static async Task UseCardEffectDrawXAmountAddAnyIfConditionMatchThenReorderToBottom(MatchRoom cMatchRoom, int cNum, int HandMustHave, bool reveal = false)
        {

            if (cMatchRoom.firstPlayer == cMatchRoom.currentPlayerTurn)
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

            DuelAction DuelActionResponse = new DuelAction()
            {
                playerID = cMatchRoom.currentPlayerTurn,
                usedCard = new Card(cMatchRoom.currentCardResolving),
                suffle = false,
                zone = "Deck",
                cardList = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand
            };

            SendPlayerData(cMatchRoom, reveal, DuelActionResponse, "ResolveOnSupportEffect");
        }

        static async Task UseCardEffectDrawAnyAsync(MatchRoom cMatchRoom, int cNum, string cUsedNumber)
        {
            if (cMatchRoom.firstPlayer == cMatchRoom.currentPlayerTurn)
                Lib.getCardFromDeck(cMatchRoom.playerADeck, cMatchRoom.playerAHand, cNum);
            else
                Lib.getCardFromDeck(cMatchRoom.playerBDeck, cMatchRoom.playerBHand, cNum);

            DuelAction _Draw = new DuelAction()
            {
                playerID = cMatchRoom.currentPlayerTurn,
                usedCard = new Card(cUsedNumber),
                suffle = false,
                zone = "Deck",
                //getting the range of cards from the player hand, then getting the last ones to add to the draw
                cardList = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerAHand.GetRange(cMatchRoom.playerAHand.Count() - cNum, cNum) : cMatchRoom.playerBHand.GetRange(cMatchRoom.playerBHand.Count() - cNum, cNum)
            };

            SendPlayerData(cMatchRoom, false, _Draw, "SupportEffectDraw");
        }
        static async Task SendPlayerData(MatchRoom cMatchRoom, bool reveal, DuelAction DuelActionResponse, string description)
        {
            PlayerRequest _ReturnData;
            string otherPlayer = cMatchRoom.currentPlayerTurn.Equals(cMatchRoom.playerA.PlayerID) ? cMatchRoom.playerB.PlayerID : cMatchRoom.playerA.PlayerID;

            // Serialize and send data to the current player
            _ReturnData = new PlayerRequest { type = "DuelUpdate", description = description, requestObject = JsonSerializer.Serialize(DuelActionResponse, Lib.options) };

            Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn.ToString()], _ReturnData);

            // Handle reveal logic and send data to the other player
            if (reveal == false)
                DuelActionResponse.cardList = cMatchRoom.FillCardListWithEmptyCards(DuelActionResponse.cardList);

            _ReturnData = new PlayerRequest { type = "DuelUpdate", description = description, requestObject = JsonSerializer.Serialize(DuelActionResponse, Lib.options) };
            Lib.SendMessage(MessageDispatcher.playerConnections[otherPlayer.ToString()], _ReturnData);
        }

        static async Task FromTheListAddFirstToHandThenAddRemainingToBottom(MatchRoom cMatchRoom, List<string> possibleDraw, DuelAction duelaction, bool shouldUseTempHandValidation, int pickedLimit, string shouldUseToCompareWithTempHand = "")
        {
            List<Card> TempHand = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
            List<Card> playerHand = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerAHand : cMatchRoom.playerBTempHand;
            //List<Card> AddToHand = new List<Card>() { new Card() { cardNumber = duelaction.SelectedCards[0] } };
            //AddToHand[0].GetCardInfo(AddToHand[0].cardNumber);

            List<Card> ReturnToDeck = null;
            List<Card> AddToHand = null;

            int pickedCount = 0;

            if (shouldUseTempHandValidation)
            {
                for (int i = 0; i < TempHand.Count(); i++)
                {
                    string comparatingValue = "";
                    TempHand[i].GetCardInfo();

                    if (shouldUseToCompareWithTempHand.Equals("name"))
                        comparatingValue = TempHand[i].name;
                    else if (shouldUseToCompareWithTempHand.Equals("number"))
                        comparatingValue = TempHand[i].cardNumber;

                    bool addToDeck = false;
                    foreach (string s in possibleDraw)
                    {
                        if (AddToHand.Any(item => item.cardNumber == comparatingValue) ||
                            ReturnToDeck.Any(item => item.cardNumber == comparatingValue))
                        {
                            continue;
                        }

                        if (comparatingValue.Equals(s) && pickedCount < pickedLimit)
                        {
                            if (AddToHand == null)
                                AddToHand = new() { TempHand[i] };
                            else
                                AddToHand.Add(TempHand[i]);

                            pickedCount++;
                            addToDeck = true;
                            continue;
                        }
                    }
                    if (!addToDeck)
                    {
                        if (ReturnToDeck == null)
                            ReturnToDeck = new() { TempHand[i] };
                        else
                            ReturnToDeck.Add(TempHand[i]);
                    }
                    addToDeck = false;
                }
                Lib.SortOrderToAddDeck(TempHand, duelaction.Order);
            }

            DuelAction DrawReturn = new DuelAction()
            {
                playerID = cMatchRoom.currentPlayerTurn,
                suffle = false,
                zone = "Deck",
                cardList = AddToHand
            };

            if (cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer)
            {
                cMatchRoom.playerAHand.AddRange(AddToHand);
                cMatchRoom.playerADeck.InsertRange(0, ReturnToDeck);
                cMatchRoom.playerATempHand.Clear();
            }
            else
            {
                cMatchRoom.playerBHand.AddRange(AddToHand);
                cMatchRoom.playerBDeck.InsertRange(0, ReturnToDeck);
                cMatchRoom.playerBTempHand.Clear();
            }

            SendPlayerData(cMatchRoom, false, DrawReturn, "SupportEffectDraw");
        }
        internal static async Task MainConditionedSummomResponseHandleAsync(MatchRoom cMatchRoom, string playerid, string cardToSummom)
        {
            List<Card> backPosition = playerid.Equals(cMatchRoom.firstPlayer) ? cMatchRoom.playerABackPosition : cMatchRoom.playerBBackPosition;

            string local = "BackStage1";
            if (playerid.Equals(cMatchRoom.firstPlayer))
            {
                for (int j = 0; j < backPosition.Count; j++)
                {
                    if (!backPosition[j].cardPosition.Equals("BackStage1"))
                    {
                        local = "BackStage1";
                    }
                    else if (!backPosition[j].cardPosition.Equals("BackStage2"))
                    {
                        local = "BackStage2";
                    }
                    else if (!backPosition[j].cardPosition.Equals("BackStage3"))
                    {
                        local = "BackStage3";
                    }
                    else if (!backPosition[j].cardPosition.Equals("BackStage4"))
                    {
                        local = "BackStage4";
                    }
                    else if (!backPosition[j].cardPosition.Equals("BackStage5"))
                    {
                        local = "BackStage5";
                    }
                }
            }
            DuelAction _DuelActio = new()
            {
                usedCard = new Card(cardToSummom, local),
                playedFrom = "Deck",
                local = local,
                playerID = cMatchRoom.currentPlayerTurn,
                suffle = true
            };
            backPosition.Add(_DuelActio.usedCard);

            Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer.ToString()], new PlayerRequest { type = "DuelUpdate", description = "PlayHolomem", requestObject = JsonSerializer.Serialize(_DuelActio, Lib.options) });
            Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer.ToString()], new PlayerRequest { type = "DuelUpdate", description = "PlayHolomem", requestObject = JsonSerializer.Serialize(_DuelActio, Lib.options) });
            cMatchRoom.currentGameHigh++;
        }
        static async Task UseCardEffectToSummom(MatchRoom cMatchRoom, string zone, string cUsedNumber, string bloomLevel)
        {
            List<Card> query = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerADeck : cMatchRoom.playerBDeck;

            foreach (var card in query)
                card.GetCardInfo();

            query = query.Where(r => r.bloomLevel == bloomLevel).ToList();

            DuelAction _Draw = new DuelAction()
            {
                playerID = cMatchRoom.currentPlayerTurn,
                usedCard = new Card() { cardNumber = cUsedNumber },
                suffle = false,
                zone = "Deck",
                cardList = query
            };

            SendPlayerData(cMatchRoom, false, _Draw, "ResolveOnSupportEffect");
        }
        static bool MatchCardColors(Card card, Card Target)
        {
            if (card.color.Equals(Target.color) || card.color.Equals("白"))
                return true;
            return false;
        }
    }
}