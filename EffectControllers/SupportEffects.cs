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
                _DuelAction.targetCard?.GetCardInfo();
            if (_DuelAction.usedCard != null)
                _DuelAction.usedCard?.GetCardInfo();
            if (_DuelAction.cheerCostCard != null)
                _DuelAction.cheerCostCard.GetCardInfo();

            PlayerRequest pReturnData;
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

            int handPos = cMatchRoom.CheckIfCardExistAtZone(playerRequest.playerID, _DuelAction.usedCard.cardNumber, PlayerZone.Hand);
            if (handPos == -1)
            {
                Lib.WriteConsoleMessage("No match found in the player hand");
                return;
            }

            //
            //MAYBE I NEED TO CHECK UPHERE IF THE USED CARD EXIST IN THE PLAYER HAND
            //


            bool energyPaid = false;

            cMatchRoom.currentCardResolving = string.IsNullOrEmpty(cMatchRoom.currentCardResolvingStage) ? _DuelAction.usedCard.cardNumber : cMatchRoom.currentCardResolving;

            try
            {

                switch (_DuelAction.usedCard.cardNumber + cMatchRoom.currentCardResolvingStage)
                {
                    case "hBP01-103":

                        energyPaid = cMatchRoom.PayCardEffectCheerOrEquipCost(_DuelAction.cheerCostCard.cardPosition, _DuelAction.cheerCostCard.cardNumber);

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
                        cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayersStartWith(cMatchRoom.currentPlayerTurn), hidden: true, duelAction: DuelActionResponse, description: "ResolveOnSupportEffect"));
                        cMatchRoom.PushPlayerAnswer();
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

                        cMatchRoom.ShuffleCards(ref playerDeck);

                        _DuelAction.playerID = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.firstPlayer : cMatchRoom.secondPlayer;
                        _DuelAction.suffle = true;
                        _DuelAction.zone = "Deck";
                        _DuelAction.cardList.Clear();
                        _DuelAction.cardList.Add(selected);
                        playerHand.Add(selected);


                        cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayers(), hidden: false, duelAction: _DuelAction, type: "DuelUpdate", description: "SupportEffectDraw"));
                        cMatchRoom.PushPlayerAnswer();

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

                        int HandMustHave = 0;
                        if (playerDeck.Count < HandMustHave && HandMustHave > 0)
                        {
                            Lib.WriteConsoleMessage("Error 02062025");
                            return;
                        }

                        Lib.MoveTopCardFromXToY(playerDeck, playerTempHand, 4);
                        DuelAction _duelActionResponse = new DuelAction(){playerID = cMatchRoom.currentPlayerTurn,usedCard = new Card(cMatchRoom.currentCardResolving),suffle = false,zone = "Deck",cardList = playerTempHand};
                        cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayersStartWith(cMatchRoom.currentPlayerTurn), hidden: true, duelAction: _duelActionResponse, description: "ResolveOnSupportEffect"));
                        cMatchRoom.PushPlayerAnswer();
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
                        //reorder the recieved cards from the player selection, cliente sends a DuelAction.Order where the lowerest numbers are the ones picked, we reorder the temp hand based on the selection
                        Lib.SortOrderToAddDeck(playerTempHand, _DuelAction.Order);
                        //Check if the selected cards are a fair play, and sort which one shold be add to hand or deck
                        var filteredLists = cMatchRoom.CheckForSelectionInTempHandAndReorder(possibleDraw, possibleDraw.Count, "number");
                        playerHand.AddRange(filteredLists.Item1);
                        playerDeck.InsertRange(0, filteredLists.Item2);
                        playerTempHand.Clear();

                        DuelAction _drawReturn = new DuelAction() { playerID = cMatchRoom.currentPlayerTurn, suffle = false, zone = "Deck", cardList = filteredLists.Item1 };
                        cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayersStartWith(cMatchRoom.currentPlayerTurn), hidden: true, duelAction: _drawReturn, description: "SupportEffectDraw"));
                        cMatchRoom.PushPlayerAnswer();
                        ResetResolution();
                        break;
                    case "hSD01-016":
                        Lib.MoveTopCardFromXToY(playerDeck, playerHand, 3);
                        DuelAction draw_hSD01 = new DuelAction() { playerID = cMatchRoom.currentPlayerTurn, usedCard = new Card("hSD01-016"), suffle = false, zone = "Deck" }.DrawTopCardFromXToY(playerHand, "Deck", 3);
                        cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayersStartWith(cMatchRoom.currentPlayerTurn), hidden: true, duelAction: draw_hSD01, description: "SupportEffectDraw"));
                        cMatchRoom.PushPlayerAnswer();
                        ResetResolution();
                        break;
                    case "hSD01-017":


                        if (playerHand.Count < 2)
                        {
                            Lib.WriteConsoleMessage("Player dont have enough cards in the hand to activate hSD01-017");
                            return;
                        }

                        cMatchRoom.SuffleHandToTheDeck(playerDeck, playerHand);
                        cMatchRoom.ShuffleCards(ref playerDeck);
                        Lib.MoveTopCardFromXToY(playerDeck, playerHand, 5);

                        _DuelAction.playerID = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.firstPlayer : cMatchRoom.secondPlayer;
                        _DuelAction.suffle = true;
                        _DuelAction.zone = "Deck";
                        _DuelAction.cardList = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerAHand : cMatchRoom.playerBHand;
                        _DuelAction.usedCard = new Card("hSD01-017");
                        _DuelAction.suffleBackToDeck = true;

                        cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayersStartWith(cMatchRoom.currentPlayerTurn), hidden: true, duelAction: _DuelAction, type: "DuelUpdate", description: "SupportEffectDraw"));
                        cMatchRoom.PushPlayerAnswer();
                        ResetResolution();
                        break;
                    case "hSD01-018":
                        cMatchRoom.currentCardResolvingStage = "1";
                        cMatchRoom.currentGamePhase = GAMEPHASE.ConditionedDraw;

                        HandMustHave = 0;
                        if (playerDeck.Count < HandMustHave && HandMustHave > 0)
                        {
                            Lib.WriteConsoleMessage("Error 02062025");
                            return;
                        }

                        Lib.MoveTopCardFromXToY(playerDeck, playerTempHand, 5);
                        _duelActionResponse = new DuelAction() { playerID = cMatchRoom.currentPlayerTurn, usedCard = new Card(cMatchRoom.currentCardResolving), suffle = false, zone = "Deck", cardList = playerTempHand };
                        cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayersStartWith(cMatchRoom.currentPlayerTurn), hidden: true, duelAction: _duelActionResponse, description: "ResolveOnSupportEffect"));
                        cMatchRoom.PushPlayerAnswer();
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

                        //reorder the recieved cards from the player selection, cliente sends a DuelAction.Order where the lowerest numbers are the ones picked, we reorder the temp hand based on the selection
                        Lib.SortOrderToAddDeck(playerTempHand, _DuelAction.Order);
                        //Check if the selected cards are a fair play, and sort which one shold be add to hand or deck
                        var _filteredLists = cMatchRoom.CheckForSelectionInTempHandAndReorder(possibleDraw, possibleDraw.Count, "number");
                        playerHand.AddRange(_filteredLists.Item1);
                        playerDeck.InsertRange(0, _filteredLists.Item2);
                        playerTempHand.Clear();

                        DuelAction __drawReturn = new DuelAction() { playerID = cMatchRoom.currentPlayerTurn, suffle = false, zone = "Deck", cardList = _filteredLists.Item1 };
                        cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayersStartWith(cMatchRoom.currentPlayerTurn), hidden: true, duelAction: __drawReturn, description: "SupportEffectDraw"));
                        cMatchRoom.PushPlayerAnswer();
                        ResetResolution();
                        break;
                    case "hSD01-019":

                        energyPaid = cMatchRoom.PayCardEffectCheerOrEquipCost(_DuelAction.cheerCostCard.cardPosition, _DuelAction.cheerCostCard.cardNumber);

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

                        _DuelAction = new DuelAction()
                        {
                            playerID = cMatchRoom.currentPlayerTurn,
                            usedCard = new Card(_DuelAction.usedCard.cardNumber),
                            targetCard = _DuelAction.targetCard,
                            cheerCostCard = _DuelAction.cheerCostCard,
                            suffle = false,
                            zone = "Deck",
                            cardList = queryy
                        };
                        cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayersStartWith(cMatchRoom.currentPlayerTurn), hidden: true, duelAction: _DuelAction, description: "ResolveOnSupportEffect"));
                        cMatchRoom.PushPlayerAnswer();

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

                        cMatchRoom.ShuffleCards(ref playerDeck);

                        _DuelAction.playerID = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.firstPlayer : cMatchRoom.secondPlayer;
                        _DuelAction.suffle = true;
                        _DuelAction.zone = "Deck";
                        _DuelAction.cardList.Clear();
                        _DuelAction.cardList.Add(selected);
                        playerHand.Add(selected);

                        cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayersStartWith(cMatchRoom.currentPlayerTurn), hidden: false, duelAction: _DuelAction, type: "DuelUpdate", description: "SupportEffectDraw"));
                        cMatchRoom.PushPlayerAnswer();

                        ResetResolution();
                        break;

                    case "hSD01-020":
                        int diceValue = cMatchRoom.GetDiceNumber(cMatchRoom.currentPlayerTurn);
                        cMatchRoom.currentCardResolvingStage = "1";

                        cMatchRoom.SendDiceRoll(new List<int>() { diceValue }, COUNTFORRESONSE: true);
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
                                card.cardPosition = "Arquive";
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
                        _DuelAction.actionObject = JsonSerializer.Serialize(returnToclient, Lib.jsonOptions);
                        _DuelAction.cardList = playerTempHand;
                        pReturnData = new PlayerRequest { type = "DuelUpdate", description = "ResolveOnSupportEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.jsonOptions) };

                        cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayers(),pReturnData));
                        cMatchRoom.PushPlayerAnswer();

                        break;
                    case "hSD01-0202":
                        var handler190 = new AttachTopCheerEnergyToBackHandler();
                        await handler190.AttachCheerEnergyHandleAsync(_DuelAction, cMatchRoom, stage: true, collab: true, back: true, TOPCHEERDECK: false, FULLCHEERDECK: false, ClientEnergyIndex: 1, ARQUIVEFULLDECK: true);
                        ResetResolution();
                        break;
                    case "hSD01-021":
                        cMatchRoom.currentCardResolvingStage = "1";
                        cMatchRoom.currentGamePhase = GAMEPHASE.ConditionedDraw;

                        HandMustHave = 0;
                        if (playerDeck.Count < HandMustHave && HandMustHave > 0)
                        {
                            Lib.WriteConsoleMessage("Error 02062025");
                            return;
                        }

                        Lib.MoveTopCardFromXToY(playerDeck, playerTempHand, 4);
                        _duelActionResponse = new DuelAction() { playerID = cMatchRoom.currentPlayerTurn, usedCard = new Card(cMatchRoom.currentCardResolving), suffle = false, zone = "Deck", cardList = playerTempHand };
                        cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayersStartWith(cMatchRoom.currentPlayerTurn), hidden: false, duelAction: _duelActionResponse, description: "ResolveOnSupportEffect"));
                        cMatchRoom.PushPlayerAnswer();
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
                        //reorder the recieved cards from the player selection, cliente sends a DuelAction.Order where the lowerest numbers are the ones picked, we reorder the temp hand based on the selection
                        Lib.SortOrderToAddDeck(playerTempHand, _DuelAction.Order);
                        //Check if the selected cards are a fair play, and sort which one shold be add to hand or deck
                        var ___filteredLists = cMatchRoom.CheckForSelectionInTempHandAndReorder(selectableList, 4, "name");
                        playerHand.AddRange(___filteredLists.Item1);
                        playerDeck.InsertRange(0, ___filteredLists.Item2);
                        playerTempHand.Clear();

                        DuelAction ___drawReturn = new DuelAction() { playerID = cMatchRoom.currentPlayerTurn, suffle = false, zone = "Deck", cardList = ___filteredLists.Item1 };
                        cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayersStartWith(cMatchRoom.currentPlayerTurn), hidden: true, duelAction: ___drawReturn, description: "SupportEffectDraw"));
                        cMatchRoom.PushPlayerAnswer();
                        ResetResolution();
                        break;
                    case "hBP01-104":
                        cMatchRoom.currentCardResolvingStage = "1";
                        cMatchRoom.currentGamePhase = GAMEPHASE.ConditionedDraw;
                        DuelAction __draw = new DuelAction() { playerID = cMatchRoom.currentPlayerTurn, usedCard = new Card() { cardNumber = _DuelAction.usedCard.cardNumber }, suffle = false, zone = "Deck", cardList = cMatchRoom.GetListOfCardWithTag(Player.TurnPlayer, PlayerZone.Deck, "Debut")};
                        cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayersStartWith(cMatchRoom.currentPlayerTurn), hidden: true, duelAction: __draw, description: "ResolveOnSupportEffect"));
                        cMatchRoom.PushPlayerAnswer();
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
                        cMatchRoom.playerBDeck = cMatchRoom.ShuffleCards(ref cMatchRoom.playerBDeck);
                        DuelAction recordedAction = cMatchRoom.PlayHolomemToBackStage(playerRequest.playerID, _DuelAction.actionObject);

                        pReturnData = new PlayerRequest { type = "DuelUpdate", description = "PlayHolomem", requestObject = JsonSerializer.Serialize(recordedAction, Lib.jsonOptions) };
                        cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayers(), playerRequest: pReturnData));
                        cMatchRoom.PushPlayerAnswer();

                        ResetResolution();
                        break;
                    case "hBP01-105":
                        energyPaid = cMatchRoom.PayCardEffectCheerOrEquipCost(_DuelAction.cheerCostCard.cardPosition, _DuelAction.cheerCostCard.cardNumber);

                        if (!energyPaid)
                            break;

                        cMatchRoom.currentCardResolvingStage = "1";
                        cMatchRoom.currentGamePhase = GAMEPHASE.ConditionedDraw;

                        List<Card> cardListToRreturn = new();
                        //getdeck
                        foreach (Card c in playerCheer)
                        {
                            c.GetCardInfo();
                            _DuelAction.usedCard?.GetCardInfo();
                            if (Lib.MatchCardColors(c, _DuelAction.usedCard))
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

                        DuelActionResponse = new DuelAction()
                        {
                            playerID = cMatchRoom.currentPlayerTurn,
                            usedCard = new Card(_DuelAction.usedCard.cardNumber),
                            targetCard = _DuelAction.targetCard,
                            cheerCostCard = _DuelAction.cheerCostCard,
                            suffle = false,
                            zone = "Deck",
                            cardList = cardListToRreturn
                        };
                        cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayersStartWith(cMatchRoom.currentPlayerTurn), hidden: true, duelAction: DuelActionResponse, description: "ResolveOnSupportEffect"));
                        cMatchRoom.PushPlayerAnswer();

                        break;
                    case "hBP01-1051":
                        var handler1051 = new AttachTopCheerEnergyToBackHandler();
                        await handler1051.AttachCheerEnergyHandleAsync(_DuelAction, cMatchRoom, stage: true, collab: true, back: true, TOPCHEERDECK: false, FULLCHEERDECK: true, ClientEnergyIndex: 1);
                        ResetResolution();
                        break;
                    case "hBP01-106":
                        if (!(_DuelAction.targetCard.cardPosition.Equals("BackStage1") || _DuelAction.targetCard.cardPosition.Equals("BackStage2") || _DuelAction.targetCard.cardPosition.Equals("BackStage3") ||
                            _DuelAction.targetCard.cardPosition.Equals("BackStage4") || _DuelAction.targetCard.cardPosition.Equals("BackStage5")))
                        {
                            Lib.WriteConsoleMessage("Invalid target position");
                            return;
                        }

                        if (cMatchRoom.IsSwitchBlocked(_DuelAction.targetCard.cardPosition) || cMatchRoom.IsSwitchBlocked(_DuelAction.usedCard.cardPosition))
                        {
                            Lib.WriteConsoleMessage("Cannot retreat by effect");
                            return;
                        }

                        cMatchRoom.SwittchCardXToCardYButKeepPosition(playerRequest.playerID, _DuelAction.targetCard);

                        cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayersStartWith(cMatchRoom.currentPlayerTurn), hidden: true, duelAction: _DuelAction, type: "DuelUpdate", description: "SwitchStageCard"));
                        cMatchRoom.PushPlayerAnswer();
                        ResetResolution();
                        break;
                    case "hBP01-108":
                        if (!(_DuelAction.targetCard.cardPosition.Equals("BackStage1") || _DuelAction.targetCard.cardPosition.Equals("BackStage2") || _DuelAction.targetCard.cardPosition.Equals("BackStage3") ||
                            _DuelAction.targetCard.cardPosition.Equals("BackStage4") || _DuelAction.targetCard.cardPosition.Equals("BackStage5")))
                        {
                            Lib.WriteConsoleMessage("Invalid target position");
                            return;
                        }

                        if (cMatchRoom.IsSwitchBlocked(_DuelAction.targetCard.cardPosition) || cMatchRoom.IsSwitchBlocked(_DuelAction.usedCard.cardPosition))
                        {
                            Lib.WriteConsoleMessage("Cannot retreat by effect");
                            return;
                        }

                        cMatchRoom.SwittchCardXToCardYButKeepPosition(MatchRoom.GetOtherPlayer(cMatchRoom, playerRequest.playerID), _DuelAction.targetCard);

                        cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayersStartWith(cMatchRoom.currentPlayerTurn), hidden: true, duelAction: _DuelAction, type: "DuelUpdate", description: "SwitchOpponentStageCard"));
                        cMatchRoom.PushPlayerAnswer();
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

                        HandMustHave = 0;
                        if (playerDeck.Count < HandMustHave && HandMustHave > 0)
                        {
                            Lib.WriteConsoleMessage("Error 02062025");
                            return;
                        }

                        Lib.MoveTopCardFromXToY(playerDeck, playerTempHand, 4);
                        _duelActionResponse = new DuelAction() { playerID = cMatchRoom.currentPlayerTurn, usedCard = new Card(cMatchRoom.currentCardResolving), suffle = false, zone = "Deck", cardList = playerTempHand };
                        cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayersStartWith(cMatchRoom.currentPlayerTurn), hidden: true, duelAction: _duelActionResponse, description: "ResolveOnSupportEffect"));
                        cMatchRoom.PushPlayerAnswer();

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
                        //reorder the recieved cards from the player selection, cliente sends a DuelAction.Order where the lowerest numbers are the ones picked, we reorder the temp hand based on the selection
                        Lib.SortOrderToAddDeck(playerTempHand, _DuelAction.Order);
                        //Check if the selected cards are a fair play, and sort which one shold be add to hand or deck
                        var x_filteredLists = cMatchRoom.CheckForSelectionInTempHandAndReorder(possibleDraw, possibleDraw.Count, "number");
                        playerHand.AddRange(x_filteredLists.Item1);
                        playerDeck.InsertRange(0, x_filteredLists.Item2);
                        playerTempHand.Clear();

                        DuelAction x__drawReturn = new DuelAction() { playerID = cMatchRoom.currentPlayerTurn, suffle = false, zone = "Deck", cardList = x_filteredLists.Item1 };
                        cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayersStartWith(cMatchRoom.currentPlayerTurn), hidden: true, duelAction: x__drawReturn, description: "SupportEffectDraw"));
                        cMatchRoom.PushPlayerAnswer();
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

                        HandMustHave = 0;
                        if (playerDeck.Count < HandMustHave && HandMustHave > 0)
                        {
                            Lib.WriteConsoleMessage("Error 02062025");
                            return;
                        }

                        Lib.MoveTopCardFromXToY(playerDeck, playerTempHand, 4);
                        _duelActionResponse = new DuelAction() { playerID = cMatchRoom.currentPlayerTurn, usedCard = new Card(cMatchRoom.currentCardResolving), suffle = false, zone = "Deck", cardList = playerTempHand };
                        cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayersStartWith(cMatchRoom.currentPlayerTurn), hidden: true, duelAction: _duelActionResponse, description: "ResolveOnSupportEffect"));
                        cMatchRoom.PushPlayerAnswer();
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
                        //reorder the recieved cards from the player selection, cliente sends a DuelAction.Order where the lowerest numbers are the ones picked, we reorder the temp hand based on the selection
                        Lib.SortOrderToAddDeck(playerTempHand, _DuelAction.Order);
                        //Check if the selected cards are a fair play, and sort which one shold be add to hand or deck
                        var ____filteredLists = cMatchRoom.CheckForSelectionInTempHandAndReorder(possibleDraw, possibleDraw.Count, "number");
                        playerHand.AddRange(____filteredLists.Item1);
                        playerDeck.InsertRange(0, ____filteredLists.Item2);
                        playerTempHand.Clear();

                        DuelAction ______drawReturn = new DuelAction() { playerID = cMatchRoom.currentPlayerTurn, suffle = false, zone = "Deck", cardList = ____filteredLists.Item1 };
                        cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayersStartWith(cMatchRoom.currentPlayerTurn), hidden: true, duelAction: ______drawReturn, description: "SupportEffectDraw"));
                        cMatchRoom.PushPlayerAnswer();
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

                        HandMustHave = 0;
                        if (playerDeck.Count < HandMustHave && HandMustHave > 0)
                        {
                            Lib.WriteConsoleMessage("Error 02062025");
                            return;
                        }

                        Lib.MoveTopCardFromXToY(playerDeck, playerTempHand, 4);
                        _duelActionResponse = new DuelAction() { playerID = cMatchRoom.currentPlayerTurn, usedCard = new Card(cMatchRoom.currentCardResolving), suffle = false, zone = "Deck", cardList = playerTempHand };
                        cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayersStartWith(cMatchRoom.currentPlayerTurn), hidden: true, duelAction: _duelActionResponse, description: "ResolveOnSupportEffect"));
                        cMatchRoom.PushPlayerAnswer();
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
                        //reorder the recieved cards from the player selection, cliente sends a DuelAction.Order where the lowerest numbers are the ones picked, we reorder the temp hand based on the selection
                        Lib.SortOrderToAddDeck(playerTempHand, _DuelAction.Order);
                        //Check if the selected cards are a fair play, and sort which one shold be add to hand or deck
                        var xx_filteredLists = cMatchRoom.CheckForSelectionInTempHandAndReorder(possibleDraw, possibleDraw.Count, "number");
                        playerHand.AddRange(xx_filteredLists.Item1);
                        playerDeck.InsertRange(0, xx_filteredLists.Item2);
                        playerTempHand.Clear();

                        DuelAction xx__drawReturn = new DuelAction() { playerID = cMatchRoom.currentPlayerTurn, suffle = false, zone = "Deck", cardList = xx_filteredLists.Item1 };
                        cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayersStartWith(cMatchRoom.currentPlayerTurn), hidden: true, duelAction: xx__drawReturn, description: "SupportEffectDraw"));
                        cMatchRoom.PushPlayerAnswer();
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

                        DuelActionResponse = new DuelAction()
                        {
                            playerID = cMatchRoom.currentPlayerTurn,
                            usedCard = new Card(cMatchRoom.currentCardResolving),
                            suffle = false,
                            zone = "Deck",
                            cardList = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand
                        };

                        cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayersStartWith(cMatchRoom.currentPlayerTurn), hidden: false, duelAction: DuelActionResponse, type: "DuelUpdate", description: "ResolveOnSupportEffect"));
                        cMatchRoom.PushPlayerAnswer();
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

                        cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayersStartWith(cMatchRoom.currentPlayerTurn), hidden: false, duelAction: _DuelAction, type: "DuelUpdate", description: "SupportEffectDraw"));
                        cMatchRoom.PushPlayerAnswer();
                        ResetResolution();
                        break;
                    case "hBP01-112":
                        diceValue = cMatchRoom.GetDiceNumber(cMatchRoom.currentPlayerTurn);
                        cMatchRoom.currentCardResolvingStage = "1";

                        cMatchRoom.SendDiceRoll(new List<int>() { diceValue }, COUNTFORRESONSE: false);
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

                        _DuelAction = new()
                        {
                            playerID = GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn),
                            targetCard = tempOpponentBackStage[n],
                        };

                        CardEffect _CardEffect = new CardEffect
                        {
                            cardNumber = _DuelAction.usedCard.cardNumber,
                            zoneTarget = "Center",
                            type = CardEffectType.ProtectFromOneLifeCostCharge,
                            playerWhoUsedTheEffect = cMatchRoom.currentPlayerTurn,
                            playerWhoIsTheTargetOfEffect = GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn),
                            activatedTurn = cMatchRoom.currentTurn
                        };
                        cMatchRoom.ActiveEffects.Add(_CardEffect);


                        cMatchRoom.currentEffectDamage = 20;
                        _DuelAction.actionObject = cMatchRoom.currentEffectDamage.ToString();

                        _DuelAction.playerID = cMatchRoom.currentPlayerTurn;
                        // Serialize and send data to the current player
                        PlayerRequest _ReturnData = new PlayerRequest { type = "DuelUpdate", description = "InflicDamageToHolomem", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.jsonOptions) };


                        cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayers(), _ReturnData));
                        cMatchRoom.PushPlayerAnswer();

                        ResetResolution();
                        break;
                    case "hBP01-110":// MUMEI SUPORT NOT DONE YET
                        // MUMEI SUPORT NOT DONE YET// MUMEI SUPORT NOT DONE YET// MUMEI SUPORT NOT DONE YET
                        // MUMEI SUPORT NOT DONE YET// MUMEI SUPORT NOT DONE YET// MUMEI SUPORT NOT DONE YET
                        //random dice number
                        diceValue = cMatchRoom.GetDiceNumber(cMatchRoom.currentPlayerTurn);
                        cMatchRoom.currentCardResolvingStage = "1";

                        cMatchRoom.SendDiceRoll(new List<int>() { diceValue }, COUNTFORRESONSE: false);
                        break;
                    case "hBP01-1101":
                        diceValue = diceList.Last();

                        cMatchRoom.currentCardResolvingStage = "1";
                        returnToclient = new List<string>() { diceValue.ToString() };

                        _DuelAction = new()
                        {
                            playerID = GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn),
                            actionObject = JsonSerializer.Serialize(returnToclient, Lib.jsonOptions)
                        };

                        pReturnData = new PlayerRequest { type = "DuelUpdate", description = "RollDice", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.jsonOptions) };


                        cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayers(), pReturnData));
                        cMatchRoom.PushPlayerAnswer();

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

                if (cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer)
                    cMatchRoom.playerAUsedSupportThisTurn = true;
                else
                    cMatchRoom.playerBUsedSupportThisTurn = true;
               
            }
            catch (Exception e)
            {
                Lib.WriteConsoleMessage(e.Message + e.StackTrace + e.InnerException);
            }
            void ResetResolution()
            {
                int indexInHand = cMatchRoom.CheckIfCardExistAtZone(cMatchRoom.currentPlayerTurn, cMatchRoom.currentCardResolving, PlayerZone.Hand);
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

                pReturnData = new PlayerRequest { type = "DuelUpdate", description = "DisposeUsedSupport", requestObject = JsonSerializer.Serialize(_DisposeAction, Lib.jsonOptions) };

                cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayers(),pReturnData));
                cMatchRoom.PushPlayerAnswer();

                cMatchRoom.currentCardResolving = "";
                cMatchRoom.currentCardResolvingStage = "";
                List<Card> temphand = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
                cMatchRoom.ResolvingEffectChain.Clear();
                cMatchRoom.currentGameHigh++;
                cMatchRoom.currentGamePhase = GAMEPHASE.MainStep;
                temphand.Clear();

            }
        }
    }
}