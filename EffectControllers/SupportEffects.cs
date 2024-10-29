using hololive_oficial_cardgame_server.WebSocketDuelFunctions;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Net.WebSockets;
using System.Text.Json;
using static hololive_oficial_cardgame_server.MatchRoom;
using static Mysqlx.Notice.Warning.Types;

namespace hololive_oficial_cardgame_server.EffectControllers
{
    class SupportEffects
    {
        internal static void OnSupportEffects(DuelAction _DuelAction, MatchRoom cMatchRoom, PlayerRequest playerRequest = null, WebSocket webSocket = null)
        {

            if (_DuelAction.targetCard != null)
                _DuelAction.targetCard.GetCardInfo(_DuelAction.targetCard.cardNumber);
            if (_DuelAction.usedCard != null)
                _DuelAction.usedCard.GetCardInfo(_DuelAction.usedCard.cardNumber);
            if (_DuelAction.cheerCostCard != null)
                _DuelAction.cheerCostCard.GetCardInfo(_DuelAction.cheerCostCard.cardNumber);

            RequestData pReturnData;
            List<Card> holoPowerList = new();
            List<Card> backPos = new();
            List<string> returnToclient = new();
            Random random = new Random();

            List<Card> tempRealHandList = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerAHand : cMatchRoom.playerBHand;

            List<Card> tempArquiveList = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerAArquive : cMatchRoom.playerAArquive;

            //check if can play limited cards, and also add to limited list
            List<Card> limitCardList = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerALimiteCardPlayed : cMatchRoom.playerBLimiteCardPlayed;
            if (_DuelAction.usedCard.cardType.Equals("サポート・スタッフ・LIMITED"))
            {
                if (limitCardList.Count() > 0)
                {
                    Lib.WriteConsoleMessage("Already played limited card this turn");
                    return;
                }
                limitCardList.Add(new Card() { cardNumber = _DuelAction.usedCard.cardNumber });
            }

            //checking if the player has the card in the hand and getting the pos
            int handPos = Lib.CheckIfCardExistInPlayerHand(cMatchRoom, int.Parse(playerRequest.playerID), _DuelAction.usedCard.cardNumber);
            if (handPos == -1)
            {
                Lib.WriteConsoleMessage("No match found in the player hand");
                return;
            }

            bool energyPaid = false;

            switch (_DuelAction.usedCard.cardNumber + cMatchRoom.currentCardResolvingStage)
            {
                case "hSD01-016":
                    UseCardEffectDrawAnyAsync(cMatchRoom, 3, "hSD01-016");
                    break;
                case "hSD01-017":

                    List<Card> tempDeck = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerADeck : cMatchRoom.playerBDeck;

                    if (tempRealHandList.Count < 2)
                    {
                        Lib.WriteConsoleMessage("Player dont have enough cards in the hand to activate hSD01-017");
                        return;
                    }

                    cMatchRoom.suffleHandToTheDeck(tempDeck, tempRealHandList);
                    cMatchRoom.ShuffleCards(tempDeck);
                    Lib.getCardFromDeck(tempDeck, tempRealHandList, 5);

                    if (cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID)
                        cMatchRoom.playerALimiteCardPlayed.Add(new Card() { cardNumber = "hSD01-017" });
                    else
                        cMatchRoom.playerBLimiteCardPlayed.Add(new Card() { cardNumber = "hSD01-017" });

                    _DuelAction.playerID = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.startPlayer : cMatchRoom.secondPlayer;
                    _DuelAction.suffle = true;
                    _DuelAction.zone = "Deck";
                    _DuelAction.cardList = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerAHand : cMatchRoom.playerBHand;

                    _DuelAction = new() { usedCard = new Card() { cardNumber = "hSD01-017" }, suffleBackToDeck = true, playerID = cMatchRoom.currentPlayerTurn };

                    pReturnData = new RequestData { type = "GamePhase", description = "DrawSupportEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                    Console.WriteLine(pReturnData);
                    if (cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID)
                    {
                        Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.playerA.PlayerID.ToString()], pReturnData);
                        _DuelAction.cardList = cMatchRoom.FillCardListWithEmptyCards(_DuelAction.cardList);
                        pReturnData = new RequestData { type = "GamePhase", description = "DrawSupportEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                        Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.playerB.PlayerID.ToString()], pReturnData);
                    }
                    else
                    {
                        Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.playerB.PlayerID.ToString()], pReturnData);
                        _DuelAction.cardList = cMatchRoom.FillCardListWithEmptyCards(_DuelAction.cardList);
                        pReturnData = new RequestData { type = "GamePhase", description = "DrawSupportEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                        Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.playerA.PlayerID.ToString()], pReturnData);
                    }
                    break;
                case "hSD01-018":
                    cMatchRoom.currentCardResolving = _DuelAction.usedCard.cardNumber;
                    cMatchRoom.currentCardResolvingStage = "1";
                    cMatchRoom.currentGamePhase = GAMEPHASE.ConditionedDraw;

                    UseCardEffectDrawXAmountAddAnyIfConditionMatchThenReorderToBottom(cMatchRoom, 5, "hSD01-018", 0, false, "", "", true);
                    break;
                case "hSD01-0181":
                    List<Record> limitedSuport = FileReader.QueryRecords(null, "サポート・アイテム・LIMITED", null, null);
                    limitedSuport.AddRange(FileReader.QueryRecords(null, "サポート・イベント・LIMITED", null, null));
                    limitedSuport.AddRange(FileReader.QueryRecords(null, "サポート・スタッフ・LIMITED", null, null));

                    List<string> possibleDraw = new List<string>();
                    foreach (Record r in limitedSuport)
                    {
                        possibleDraw.Add(r.CardNumber);
                    }

                    //check if select match what player have in the server information 
                    if (!Lib.HaveSameWords(Lib.CardListToStringList(tempRealHandList), _DuelAction.SelectedCards)) 
                    {
                        Lib.WriteConsoleMessage("select cards didnt match the server info");
                        return;
                    }
                    UseCardEffectDrawXAmountAddAnyIfConditionMatchThenReorderToBottomResponse(cMatchRoom, possibleDraw, _DuelAction, true, "number");
                    ResetResolution();
                    break;
                case "hSD01-019":

                    energyPaid = Lib.PayCardEffectCheerFieldCost(cMatchRoom, _DuelAction.cheerCostCard.cardPosition, _DuelAction.cheerCostCard.cardNumber);

                    if (!energyPaid)
                        break;

                    cMatchRoom.currentCardResolving = _DuelAction.usedCard.cardNumber;
                    cMatchRoom.currentCardResolvingStage = "1";
                    cMatchRoom.currentGamePhase = GAMEPHASE.ConditionedDraw;

                    //getdeck
                    tempDeck = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerADeck : cMatchRoom.playerBDeck;
                    //get valid cards from deck
                    List<Card> queryy = tempDeck.Where(r => r.bloomLevel == "1st" && !(r.cardType == "Buzzホロメン")).ToList();
                    queryy.AddRange(tempDeck.Where(r => r.bloomLevel == "2nd" && !(r.cardType == "Buzzホロメン")).ToList());
                    //send to player the info
                    UseCardEffectDrawXAddIfMatchCondition(cMatchRoom, queryy, _DuelAction, false);
                    break;
                case "hSD01-0191":
                    limitedSuport = FileReader.QueryRecords(null, null, "1st", null);
                    limitedSuport.AddRange(FileReader.QueryRecords(null, null, "2nd", null));

                    possibleDraw = new List<string>();
                    foreach (Record r in limitedSuport)
                    {
                        possibleDraw.Add(r.CardNumber);
                    }

                    tempDeck = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerADeck : cMatchRoom.playerBDeck;
                    if (tempDeck.Count < 1) 
                    {
                        Lib.WriteConsoleMessage("invalid selection for hSD01-0191");
                        return;
                    }

                    //check if select match what player have in the server information 
                    if (!Lib.HaveSameWords(Lib.CardListToStringList(tempRealHandList), _DuelAction.SelectedCards))
                    {
                        Lib.WriteConsoleMessage("select cards didnt match the server info");
                        return;
                    }

                    UseCardEffectDrawXAmountAddAnyIfConditionMatchThenReorderToBottomResponse(cMatchRoom, possibleDraw, _DuelAction, true, "number");
                    ResetResolution();
                    break;
                case "hSD01-020":
                    //getting selectable energys from the arquive
                    List<Card> tempList = new List<Card>();
                    foreach (Card card in tempArquiveList) 
                    {
                        card.GetCardInfo(card.cardNumber);
                        if (card.cardType.Equals("エール")) 
                        { 
                            tempList.Add(card);
                        }                
                    }
                    if (tempList.Count == 0)
                    {
                        ResetResolution();
                        return;
                    }

                    //getting random number and must be higher than 2 and adding to the player return
                    string randomNumber = random.Next(1, 7).ToString();
                    if (int.Parse(randomNumber) < 3) {
                        ResetResolution();
                        return;
                    }
                    returnToclient = new List<string>() { randomNumber };

                    //updating current resolving state to wait for the repsonse
                    cMatchRoom.currentCardResolvingStage = "1";

                    //send the info to the currentplayer so he can pick the card
                    _DuelAction.actionObject = JsonSerializer.Serialize(returnToclient, Lib.options);
                    _DuelAction.cardList = tempList;
                    pReturnData = new RequestData { type = "GamePhase", description = "ResolveOnSupportEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };

                    Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn.ToString()], pReturnData);
                    Lib.SendMessage(MessageDispatcher.playerConnections[MatchRoom.GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn).ToString()], pReturnData);
                    break;
                case "hSD01-0201":
                    //var handler190 = new AttachTopCheerEnergyToBackHandler(MessageDispatcher.playerConnections, MessageDispatcher._MatchRooms);
                    //await handler190.AttachTopCheerEnergyHandleAsync(playerRequest, webSocket, false, true, false, 1);
                    ResetResolution();
                    break;
                case "hSD01-021":
                    cMatchRoom.currentCardResolving = _DuelAction.usedCard.cardNumber;
                    cMatchRoom.currentCardResolvingStage = "1";
                    cMatchRoom.currentGamePhase = GAMEPHASE.ConditionedDraw;

                    //getdeck
                    tempDeck = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerADeck : cMatchRoom.playerBDeck;
                    tempDeck = tempDeck.Skip(Math.Max(0, tempDeck.Count() - 4)).Take(4).ToList();
                    
                    pReturnData = new RequestData { type = "GamePhase", description = "ResolveOnSupportEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                    _DuelAction.cardList = tempDeck;

                    Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn.ToString()], pReturnData);
                    Lib.SendMessage(MessageDispatcher.playerConnections[MatchRoom.GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn).ToString()], pReturnData);
                    break;
                case "hSD01-0211":
                    possibleDraw = new List<string>();
                    possibleDraw.Add("ときのそら");
                    possibleDraw.Add("AZKi");

                    //check if select match what player have in the server information 
                    if (!Lib.HaveSameWords(Lib.CardListToStringList(tempRealHandList), _DuelAction.SelectedCards))
                    {
                        Lib.WriteConsoleMessage("select cards didnt match the server info");
                        return;
                    }

                    UseCardEffectDrawXAmountAddAnyIfConditionMatchThenReorderToBottomResponse(cMatchRoom, possibleDraw, _DuelAction, true, "name");
                    ResetResolution();
                    break;
                default:
                    ResetResolution();
                    break;
            }
            void ResetResolution()
            {
                int indexInHand = Lib.CheckIfCardExistInPlayerHand(cMatchRoom, cMatchRoom.currentPlayerTurn, cMatchRoom.currentCardResolving);
                if (indexInHand > -1) {
                    tempArquiveList.Add(tempRealHandList[indexInHand]);
                    tempRealHandList.RemoveAt(indexInHand);
                }

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
                card.GetCardInfo(card.cardNumber);

            DuelAction DuelActionResponse = new DuelAction()
            {
                playerID = cMatchRoom.currentPlayerTurn,
                usedCard = new Card() { cardNumber = _DuelAction.usedCard.cardNumber },
                targetCard = _DuelAction.targetCard,
                cheerCostCard = _DuelAction.cheerCostCard,
                suffle = false,
                zone = "Deck",
                cardList = queryy
            };

            SendPlayerData(cMatchRoom, reveal, DuelActionResponse, "SuporteEffectDrawXAddIf", _DuelAction.usedCard.cardNumber);
        }

        static async Task UseCardEffectDrawXAmountAddAnyIfConditionMatchThenReorderToBottom(MatchRoom cMatchRoom, int cNum, string cUsedNumber, int HandMustHave, bool needEnergy = false, string targetCardPosition = "", string costCardnumber = "", bool reveal = false)
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
                usedCard = new Card() { cardNumber = cUsedNumber },
                targetCard = new Card() { cardNumber = costCardnumber, cardPosition = targetCardPosition },
                suffle = false,
                zone = "Deck",
                cardList = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand
            };

            SendPlayerData(cMatchRoom, reveal, DuelActionResponse, "UseCardEffectDrawXAmountAddAnyIfConditionMatchThenReorderToBottom", cUsedNumber);
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
                usedCard = new Card() { cardNumber = cUsedNumber },
                suffle = false,
                zone = "Deck",
                //getting the range of cards from the player hand, then getting the last ones to add to the draw
                cardList = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerAHand.GetRange(cMatchRoom.playerAHand.Count() - cNum, cNum) : cMatchRoom.playerBHand.GetRange(cMatchRoom.playerBHand.Count() - cNum, cNum)
            };

            SendPlayerData(cMatchRoom, false, _Draw, "UseCardEffectDrawAny", cUsedNumber);
        }
        static async Task SendPlayerData(MatchRoom cMatchRoom, bool reveal, DuelAction DuelActionResponse, string description, string cUsedNumber)
        {
            RequestData _ReturnData;
            int otherPlayer = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerB.PlayerID : cMatchRoom.playerA.PlayerID;

            // Serialize and send data to the current player
            _ReturnData = new RequestData { type = "GamePhase", description = description, requestObject = JsonSerializer.Serialize(DuelActionResponse, Lib.options) };

            Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn.ToString()], _ReturnData);

            // Handle reveal logic and send data to the other player
            if (reveal == false)
            {
                DuelActionResponse.cardList = cMatchRoom.FillCardListWithEmptyCards(DuelActionResponse.cardList);
            }

            _ReturnData = new RequestData { type = "GamePhase", description = description, requestObject = JsonSerializer.Serialize(DuelActionResponse, Lib.options) };
            Lib.SendMessage(MessageDispatcher.playerConnections[otherPlayer.ToString()], _ReturnData);
        }

        static async Task UseCardEffectDrawXAmountAddAnyIfConditionMatchThenReorderToBottomResponse(MatchRoom cMatchRoom, List<string> possibleDraw, DuelAction _ConditionedDraw, bool shouldUseTempHandValidation, string shouldUseToCompareWithTempHand = "") 
        {

            List<string> ChosedCardList = _ConditionedDraw.SelectedCards;

            List<Card> TempHand = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerAHand : cMatchRoom.playerBHand;
            List<Card> AddToHand = new List<Card>() { new Card() { cardNumber = ChosedCardList[0] } };
            AddToHand[0].GetCardInfo(AddToHand[0].cardNumber);
            List<Card> ReturnToDeck = new();

            if (shouldUseTempHandValidation)
            {
                AddToHand = new();

                var comparer = StringComparer.Create(new CultureInfo("ja-JP"), true);

                for (int i = 0; i < TempHand.Count(); i++)
                {
                    string name = "";
                    TempHand[i].GetCardInfo(TempHand[i].cardNumber);


                    if (shouldUseToCompareWithTempHand.Equals("name"))
                        name = TempHand[i].name;
                    else if (shouldUseToCompareWithTempHand.Equals("number"))
                        name = TempHand[i].cardNumber;


                    foreach (string s in possibleDraw)
                    {
                        if (comparer.Equals(name, s))
                        {
                            AddToHand.Add(TempHand[i]);
                            continue;
                        }
                    }
                    ReturnToDeck.Add(TempHand[i]);
                }
                Lib.SortOrderToAddDeck(TempHand, _ConditionedDraw.Order);
            }

            DuelAction DrawReturn = new DuelAction()
            {
                playerID = cMatchRoom.currentPlayerTurn,
                suffle = false,
                zone = "Deck",
                cardList = AddToHand
            };

            RequestData _ReturnData = new RequestData { type = "GamePhase", description = "TaskUseCardEffectDrawXAmountAddAnyIfConditionMatchThenReorderToBottom", requestObject = JsonSerializer.Serialize(DrawReturn, Lib.options) };

            if (cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer)
            {
                cMatchRoom.playerAHand.AddRange(AddToHand);
                cMatchRoom.playerADeck.InsertRange(0, ReturnToDeck);
                cMatchRoom.playerATempHand.Clear();

                Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer.ToString()], _ReturnData);
                DrawReturn.cardList = cMatchRoom.FillCardListWithEmptyCards(DrawReturn.cardList);
                Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer.ToString()], _ReturnData);

            }
            else
            {
                cMatchRoom.playerBHand.AddRange(AddToHand);
                cMatchRoom.playerBDeck.InsertRange(0, ReturnToDeck);
                cMatchRoom.playerBTempHand.Clear();

                Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer.ToString()], _ReturnData);
                DrawReturn.cardList = cMatchRoom.FillCardListWithEmptyCards(DrawReturn.cardList);
                Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer.ToString()], _ReturnData);
            }
        }
    }
}