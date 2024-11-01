using hololive_oficial_cardgame_server.SerializableObjects;
using System.Net.WebSockets;
using System.Text.Json;
using static hololive_oficial_cardgame_server.SerializableObjects.MatchRoom;

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

            PlayerRequest pReturnData;
            List<Card> holoPowerList = new();
            List<Card> backPos = new();
            List<string> returnToclient = new();
            Random random = new Random();

            List<Card> playerHand = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerAHand : cMatchRoom.playerBHand;
            List<Card> playerArquive = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerAArquive : cMatchRoom.playerAArquive;
            List<Card> playerDeck = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerADeck : cMatchRoom.playerBDeck;
            List<Card> playerTempHand = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;

            //check if can play limited cards, and also add to limited list
            List<Card> playerLimitedCardsPlayed = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerALimiteCardPlayed : cMatchRoom.playerBLimiteCardPlayed;
            if (_DuelAction.usedCard.cardType.Equals("サポート・スタッフ・LIMITED"))
            {
                if (playerLimitedCardsPlayed.Count() > 0)
                {
                    Lib.WriteConsoleMessage("Already played limited card this turn");
                    return;
                }
                playerLimitedCardsPlayed.Add(new Card() { cardNumber = _DuelAction.usedCard.cardNumber });
            }

            //checking if the player has the card in the hand and getting the pos
            int handPos = Lib.CheckIfCardExistInPlayerHand(cMatchRoom, playerRequest.playerID, _DuelAction.usedCard.cardNumber);
            if (handPos == -1)
            {
                Lib.WriteConsoleMessage("No match found in the player hand");
                return;
            }

            bool energyPaid = false;

            if (string.IsNullOrEmpty(cMatchRoom.currentCardResolving))
                cMatchRoom.currentCardResolving = cMatchRoom.currentCardResolving = _DuelAction.usedCard.cardNumber;

            switch (_DuelAction.usedCard.cardNumber + cMatchRoom.currentCardResolvingStage)
            {
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
                    _DuelAction.usedCard = new Card() { cardNumber = "hSD01-017" };
                    _DuelAction.suffleBackToDeck = true;

                    SendPlayerData(cMatchRoom, false, _DuelAction, "SupportEffectDraw");
                    ResetResolution();
                    break;
                case "hSD01-018":
                    cMatchRoom.currentCardResolving = _DuelAction.usedCard.cardNumber;
                    cMatchRoom.currentCardResolvingStage = "1";
                    cMatchRoom.currentGamePhase = GAMEPHASE.ConditionedDraw;

                    UseCardEffectDrawXAmountAddAnyIfConditionMatchThenReorderToBottom(cMatchRoom, 5, 0, false);
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
                    if (!Lib.HaveSameWords(Lib.CardListToStringList(playerTempHand), _DuelAction.SelectedCards))
                    {
                        Lib.WriteConsoleMessage("select cards didnt match the server info");
                        return;
                    }
                    FromTheListAddFirstToHandThenAddRemainingToBottom(cMatchRoom, possibleDraw, _DuelAction, true, 1, "number");
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
                    playerDeck = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerADeck : cMatchRoom.playerBDeck;
                    foreach (Card c in playerDeck)
                    {
                        c.GetCardInfo(c.cardNumber);
                    }
                    //get valid cards from deck
                    List<Card> queryy = playerDeck.Where(r => r.bloomLevel == "1st" && !(r.cardType == "Buzzホロメン")).ToList();
                    queryy.AddRange(playerDeck.Where(r => r.bloomLevel == "2nd" && !(r.cardType == "Buzzホロメン")).ToList());

                    playerTempHand.AddRange(queryy);

                    //send to player the info
                    UseCardEffectDrawXAddIfMatchCondition(cMatchRoom, queryy, _DuelAction, false);
                    break;
                case "hSD01-0191":
                    Card selected = new Card() { cardNumber = _DuelAction.actionObject };

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
                        Lib.WriteConsoleMessage("invalid selection for hSD01-0191");
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

                    SendPlayerData(cMatchRoom, true, _DuelAction, "SupportEffectDraw");

                    ResetResolution();
                    break;
                case "hSD01-020":
                    //getting selectable energys from the arquive
                    List<Card> tempList = new List<Card>();
                    foreach (Card card in playerArquive)
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
                    if (int.Parse(randomNumber) < 3)
                    {
                        ResetResolution();
                        return;
                    }
                    returnToclient = new List<string>() { randomNumber };

                    //updating current resolving state to wait for the repsonse
                    cMatchRoom.currentCardResolvingStage = "1";

                    //send the info to the currentplayer so he can pick the card
                    _DuelAction.actionObject = JsonSerializer.Serialize(returnToclient, Lib.options);
                    _DuelAction.cardList = tempList;
                    pReturnData = new PlayerRequest { type = "GamePhase", description = "ResolveOnSupportEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };

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

                    UseCardEffectDrawXAmountAddAnyIfConditionMatchThenReorderToBottom(cMatchRoom, 4, 0, true);
                    break;
                case "hSD01-0211":
                    List<string> selectableList = new() { "ときのそら", "AZKi" , "SorAZ" };
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
                default:
                    ResetResolution();
                    break;
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
                    playerID = MatchRoom.GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn),
                    usedCard = new() { cardNumber = cMatchRoom.currentCardResolving },
                };
                pReturnData = new PlayerRequest { type = "GamePhase", description = "DisposeUsedSupport", requestObject = JsonSerializer.Serialize(_DisposeAction, Lib.options) };
                Lib.SendMessage(MessageDispatcher.playerConnections[MatchRoom.GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn).ToString()], pReturnData);

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
                usedCard = new Card() { cardNumber = cMatchRoom.currentCardResolving },
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
                usedCard = new Card() { cardNumber = cUsedNumber },
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
            _ReturnData = new PlayerRequest { type = "GamePhase", description = description, requestObject = JsonSerializer.Serialize(DuelActionResponse, Lib.options) };

            Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn.ToString()], _ReturnData);

            // Handle reveal logic and send data to the other player
            if (reveal == false)
                DuelActionResponse.cardList = cMatchRoom.FillCardListWithEmptyCards(DuelActionResponse.cardList);

            _ReturnData = new PlayerRequest { type = "GamePhase", description = description, requestObject = JsonSerializer.Serialize(DuelActionResponse, Lib.options) };
            Lib.SendMessage(MessageDispatcher.playerConnections[otherPlayer.ToString()], _ReturnData);
        }

        static async Task FromTheListAddFirstToHandThenAddRemainingToBottom(MatchRoom cMatchRoom, List<string> possibleDraw, DuelAction duelaction, bool shouldUseTempHandValidation, int pickedLimit, string shouldUseToCompareWithTempHand = "")
        {
            List<Card> TempHand = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
            List<Card> playerHand = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerAHand : cMatchRoom.playerBTempHand;
            //List<Card> AddToHand = new List<Card>() { new Card() { cardNumber = duelaction.SelectedCards[0] } };
            //AddToHand[0].GetCardInfo(AddToHand[0].cardNumber);

            List<Card> ReturnToDeck = new();
            List<Card> AddToHand = new();

            int pickedCount = 0;

            if (shouldUseTempHandValidation)
            {
                for (int i = 0; i < TempHand.Count(); i++)
                {
                    string name = "";
                    TempHand[i].GetCardInfo(TempHand[i].cardNumber);

                    if (shouldUseToCompareWithTempHand.Equals("name"))
                        name = TempHand[i].name;
                    else if (shouldUseToCompareWithTempHand.Equals("number"))
                        name = TempHand[i].cardNumber;

                    bool addToDeck = false;
                    foreach (string s in possibleDraw)
                    {
                        if (name.Equals(s) && pickedCount < pickedLimit)
                        {
                            AddToHand.Add(TempHand[i]);
                            pickedCount++;
                            addToDeck = true;
                            continue;
                        }
                    }
                    if (!addToDeck)
                        ReturnToDeck.Add(TempHand[i]);
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
    }
}