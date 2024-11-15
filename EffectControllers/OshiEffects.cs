
using hololive_oficial_cardgame_server.SerializableObjects;
using System.Net.WebSockets;
using System.Text.Json;
using static hololive_oficial_cardgame_server.SerializableObjects.MatchRoom;

namespace hololive_oficial_cardgame_server.EffectControllers
{
    class OshiEffects
    {
        public const bool TESTEMODE = false;

        internal static async Task OnOshiEffectsAsync(DuelAction _DuelAction, MatchRoom cMatchRoom, PlayerRequest playerRequest = null, WebSocket webSocket = null, bool SPSKILL = false)
        {
            //General activation validations
            if (_DuelAction.usedCard.cardNumber.Equals(cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerAOshi : cMatchRoom.playerBOshi))
            {
                Lib.WriteConsoleMessage("No match found in the player hand");
                return;
            }

            if (cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer)
            {
                if (!SPSKILL && cMatchRoom.usedOshiSkillPlayerA)
                    return;

                if (cMatchRoom.usedSPOshiSkillPlayerA)
                    return;
            }
            else
            {
                if (!SPSKILL && cMatchRoom.usedOshiSkillPlayerB)
                    return;

                if (cMatchRoom.usedSPOshiSkillPlayerB)
                    return;
            }

            if (string.IsNullOrEmpty(cMatchRoom.currentCardResolving))
                cMatchRoom.currentCardResolving = _DuelAction.usedCard.cardNumber;

            List<Card> tempHololive = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerAHoloPower : cMatchRoom.playerBHoloPower;
            var holoPowerCostAmount = HoloPowerCost(_DuelAction.usedCard.cardNumber, SPSKILL);

            if (tempHololive.Count() < holoPowerCostAmount)
            {
                Lib.WriteConsoleMessage("Not enough cards to activate");
                return;
            }

            //End - General activation validations

            PlayerRequest pReturnData;
            List<Card> holoPowerList = new();
            List<Card> backPos = new();
            List<string> returnToclient = new();

            bool ISFIRSTPLAYER = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer;

            List<Card> playerHand = ISFIRSTPLAYER ? cMatchRoom.playerAHand : cMatchRoom.playerBHand;
            List<Card> playerArquive = ISFIRSTPLAYER ? cMatchRoom.playerAArquive : cMatchRoom.playerBArquive;
            List<Card> playerDeck = ISFIRSTPLAYER ? cMatchRoom.playerADeck : cMatchRoom.playerBDeck;
            List<Card> playerTempHand = ISFIRSTPLAYER ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
            List<Card> playerCheer = ISFIRSTPLAYER ? cMatchRoom.playerACardCheer : cMatchRoom.playerBCardCheer;
            Card playerStage = ISFIRSTPLAYER ? cMatchRoom.playerAStage : cMatchRoom.playerBStage;
            List<Card> playerBackstage = ISFIRSTPLAYER ? cMatchRoom.playerABackPosition : cMatchRoom.playerBBackPosition;
            //check if can play limited cards, and also add to limited list
            List<Card> playerLimitedCardsPlayed = ISFIRSTPLAYER ? cMatchRoom.playerALimiteCardPlayed : cMatchRoom.playerBLimiteCardPlayed;

            bool energyPaid = false;

            if (_DuelAction.targetCard != null)
                _DuelAction.targetCard.GetCardInfo();
            if (_DuelAction.usedCard != null)
                _DuelAction.usedCard.GetCardInfo();
            if (_DuelAction.cheerCostCard != null)
                _DuelAction.cheerCostCard.GetCardInfo();

            //ensure that we have a resolving card, if we have dont need to use
            if (string.IsNullOrEmpty(cMatchRoom.currentCardResolving))
                cMatchRoom.currentCardResolving = cMatchRoom.currentCardResolving = _DuelAction.usedCard.cardNumber;

            try
            {
                if (SPSKILL)
                {
                    if (ISFIRSTPLAYER)
                    {
                        cMatchRoom.usedSPOshiSkillPlayerA = true;
                    }
                    else
                    {
                        cMatchRoom.usedSPOshiSkillPlayerB = true;
                    }
                    switch (_DuelAction.usedCard.cardNumber + cMatchRoom.currentCardResolvingStage)
                    {
                        case "hSD01-001":

                            if (!(_DuelAction.targetCard.cardPosition.Equals("BackStage1") || _DuelAction.targetCard.cardPosition.Equals("BackStage2") || _DuelAction.targetCard.cardPosition.Equals("BackStage3") ||
                                _DuelAction.targetCard.cardPosition.Equals("BackStage4") || _DuelAction.targetCard.cardPosition.Equals("BackStage5")))
                            {
                                Lib.WriteConsoleMessage("Invalid target position");
                                return;
                            }
                            Lib.SwittchCardYToCardZButKeepPosition(cMatchRoom, MatchRoom.GetOtherPlayer(cMatchRoom, playerRequest.playerID), _DuelAction.targetCard);

                            cMatchRoom.ActiveTurnEffects.Add(new CardEffect
                            {
                                cardNumber = _DuelAction.usedCard.cardNumber,
                                zoneTarget = "Stage",
                                ExistXAtZone_Color = "白",
                                type = CardEffectType.BuffThisCardDamageExistXCOLORAtZone,
                                Damage = 50,
                                playerWhoUsedTheEffect = cMatchRoom.currentPlayerTurn,
                                playerWhoIsTheTargetOfEffect = cMatchRoom.currentPlayerTurn,
                                listIndex = 0
                            });

                            Lib.SendPlayerData(cMatchRoom, false, _DuelAction, "SwitchOpponentStageCard");
                            ResetResolution(cMatchRoom, SPSKILL);
                            break;
                        case "hYS01-002":
                            cMatchRoom.ActiveTurnEffects.Add(new CardEffect
                            {
                                cardNumber = _DuelAction.usedCard.cardNumber,
                                zoneTarget = "Collaboration",
                                ExistXAtZone_Color = "緑",
                                type = CardEffectType.BuffZoneCardDamageExistXCOLORAtZone,
                                Damage = 20,
                                playerWhoUsedTheEffect = cMatchRoom.currentPlayerTurn,
                                playerWhoIsTheTargetOfEffect = cMatchRoom.currentPlayerTurn,
                                listIndex = 0
                            });
                            ResetResolution(cMatchRoom, SPSKILL); 
                            break;
                        case "hBP01-001":
                            cMatchRoom.ActiveTurnEffects.Add(new CardEffect
                            {
                                cardNumber = _DuelAction.targetCard.cardNumber,
                                zoneTarget = _DuelAction.targetCard.cardPosition,
                                type = CardEffectType.BuffDamageToCardAtZone,
                                Damage = _DuelAction.usedCard.color.Equals("白") ? 100 : 50,
                                playerWhoUsedTheEffect = cMatchRoom.currentPlayerTurn,
                                playerWhoIsTheTargetOfEffect = cMatchRoom.currentPlayerTurn,
                                listIndex = 0
                            });
                            ResetResolution(cMatchRoom, SPSKILL);
                            break;
                        default:
                            ResetResolution(cMatchRoom, SPSKILL);
                            break;
                    }
                }
                if (!SPSKILL)
                {
                    if (ISFIRSTPLAYER)
                    {
                        cMatchRoom.usedOshiSkillPlayerA = true;
                    }
                    else
                    {
                        cMatchRoom.usedOshiSkillPlayerB = true;


                    }

                    switch (_DuelAction.usedCard.cardNumber + cMatchRoom.currentCardResolvingStage)
                    {
                        case "hSD01-001":
                            if (!(_DuelAction.targetCard.cardPosition.Equals("BackStage1") || _DuelAction.targetCard.cardPosition.Equals("BackStage2") || _DuelAction.targetCard.cardPosition.Equals("BackStage3") ||
                                _DuelAction.targetCard.cardPosition.Equals("BackStage4") || _DuelAction.targetCard.cardPosition.Equals("BackStage5") || _DuelAction.targetCard.cardPosition.Equals("Collaboration")))
                            {
                                Lib.WriteConsoleMessage("Invalid target position");
                                return;
                            }
                            Card energy = JsonSerializer.Deserialize<Card>(_DuelAction.actionObject);
                            bool energyRemoved = Lib.TransferEnergyFromCardAToTarget(cMatchRoom, CardA: (ISFIRSTPLAYER ? cMatchRoom.playerAStage : cMatchRoom.playerBStage), Energy: energy, _DuelAction);

                            if (!energyRemoved)
                            {
                                Lib.WriteConsoleMessage("Invalid Energy Paid");
                                return;
                            }

                            ResetResolution(cMatchRoom, SPSKILL);
                            break;
                        case "hYS01-002":
                            Lib.RecoveryHP(cMatchRoom, STAGE: true, COLLAB: true, BACKSTAGE: true, RecoveryAmount: 20, targetPlayerID: cMatchRoom.currentPlayerTurn);
                            break;
                        case "hBP01-001":
                            Card opsStage = !ISFIRSTPLAYER ? cMatchRoom.playerAStage : cMatchRoom.playerBStage;
                            if (opsStage == null)
                            {
                                Lib.WriteConsoleMessage("No avaliable target");
                                return;
                            }
                            if (opsStage.currentHp < 50)
                            {
                                Lib.WriteConsoleMessage("Oponnent hp already lower than 50");
                                return;
                            }
                            opsStage.currentHp = 50;

                            DuelAction duelAction = new()
                            {
                                playerID = cMatchRoom.currentPlayerTurn,
                                actionObject = 50.ToString()
                            };
                            Lib.SendPlayerData(cMatchRoom, false, duelAction, "SetHPToFixedValue");
                            ResetResolution(cMatchRoom, SPSKILL);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Lib.WriteConsoleMessage(e.Message + e.StackTrace + e.InnerException);
            }
        }
        static void ResetResolution(MatchRoom cMatchRoom, bool SPSKILL = false)
        {

            bool ISFIRSTPLAYER = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer;

            List<Card> playerHand = ISFIRSTPLAYER ? cMatchRoom.playerAHand : cMatchRoom.playerBHand;
            List<Card> playerArquive = ISFIRSTPLAYER ? cMatchRoom.playerAArquive : cMatchRoom.playerBArquive;

            int HoloPowerAmount = HoloPowerCost(cMatchRoom.currentCardResolving, SPSKILL);

            List<Card> returnToPlayer = new();


            if (ISFIRSTPLAYER)
            {
                Lib.getCardFromDeck(cMatchRoom.playerAHoloPower, cMatchRoom.playerAArquive, HoloPowerAmount);
                for (; HoloPowerAmount > 0; HoloPowerAmount--)
                {
                    returnToPlayer.Add(cMatchRoom.playerAArquive.Last());
                }
            }
            else
            {
                Lib.getCardFromDeck(cMatchRoom.playerBHoloPower, cMatchRoom.playerBArquive, HoloPowerAmount);
                for (; HoloPowerAmount > 0; HoloPowerAmount--)
                {
                    returnToPlayer.Add(cMatchRoom.playerBArquive.Last());
                }
            }

            DuelAction _DuelAction = new DuelAction()
            {
                playerID = cMatchRoom.currentPlayerTurn,
                cardList = returnToPlayer,
            };

            PlayerRequest pReturnData = new PlayerRequest { type = "DuelUpdate", description = "PayHoloPowerCost", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };

            Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer.ToString()], pReturnData);
            Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer.ToString()], pReturnData);



            int indexInHand = Lib.CheckIfCardExistInPlayerHand(cMatchRoom, cMatchRoom.currentPlayerTurn, cMatchRoom.currentCardResolving);
            if (indexInHand > -1)
            {
                playerArquive.Add(playerHand[indexInHand]);
                playerHand.RemoveAt(indexInHand);
            }

            if (cMatchRoom.extraInfo != null)
                cMatchRoom.extraInfo.Clear();

            cMatchRoom.currentCardResolving = "";
            cMatchRoom.currentCardResolvingStage = "";

            List<Card> temphand = ISFIRSTPLAYER ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
            cMatchRoom.currentDuelActionResolvingRecieved.Clear();
            cMatchRoom.currentGameHigh++;

            cMatchRoom.currentGamePhase = GAMEPHASE.MainStep;
            temphand.Clear();
        }
        static private int HoloPowerCost(string cardNumber, bool SP = false)
        {
            if (TESTEMODE)
                return 0;

            if (SP)
                switch (cardNumber)
                {
                    case "hSD01-001": //soda
                        return 2;

                    case "hYS01-002": //upe sama
                        return 1;
                }
            if (!SP)
                switch (cardNumber)
                {
                    case "hSD01-001": //soda
                        return 1;

                    case "hYS01-002": //upe sama
                        return 2;
                }

            return 0;
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

            Lib.SendPlayerData(cMatchRoom, reveal, DuelActionResponse, "ResolveOnSupportEffect");
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

            Lib.SendPlayerData(cMatchRoom, reveal, DuelActionResponse, "ResolveOnSupportEffect");
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

            Lib.SendPlayerData(cMatchRoom, false, _Draw, "SupportEffectDraw");
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
                    string name = "";
                    TempHand[i].GetCardInfo();

                    if (shouldUseToCompareWithTempHand.Equals("name"))
                        name = TempHand[i].name;
                    else if (shouldUseToCompareWithTempHand.Equals("number"))
                        name = TempHand[i].cardNumber;

                    bool addToDeck = false;
                    foreach (string s in possibleDraw)
                    {
                        if (name.Equals(s) && pickedCount < pickedLimit)
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

            Lib.SendPlayerData(cMatchRoom, false, DrawReturn, "SupportEffectDraw");
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

            Lib.SendPlayerData(cMatchRoom, false, _Draw, "ResolveOnSupportEffect");
        }
        static bool MatchCardColors(Card card, Card Target)
        {
            if (card.color.Equals(Target.color) || card.color.Equals("白"))
                return true;
            return false;
        }

    }
}