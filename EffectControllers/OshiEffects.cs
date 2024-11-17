
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

                _DuelAction.targetCard.GetCardInfo();
                _DuelAction.usedCard.GetCardInfo();
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
                        case "hSD01-002":
                            cMatchRoom.ActiveEffects.Add(new CardEffect
                            {
                                type = CardEffectType.OneUseFixedDiceRoll,
                                diceRollValue = int.Parse(_DuelAction.actionObject),
                                playerWhoUsedTheEffect = cMatchRoom.currentPlayerTurn,
                                playerWhoIsTheTargetOfEffect = cMatchRoom.currentPlayerTurn,
                                activatedTurn = cMatchRoom.currentTurn
                            });
                            ResetResolution(cMatchRoom, SPSKILL);
                            break;
                        case "hBP01-005":
                            cMatchRoom.ActiveEffects.Add(new CardEffect
                            {
                                type = CardEffectType.BlockRetreat,
                                zoneTarget = "Collaboration",
                                playerWhoUsedTheEffect = cMatchRoom.currentPlayerTurn,
                                playerWhoIsTheTargetOfEffect = ISFIRSTPLAYER ? cMatchRoom.secondPlayer : cMatchRoom.firstPlayer,
                                activatedTurn = cMatchRoom.currentTurn + 1
                            });
                            cMatchRoom.ActiveEffects.Add(new CardEffect
                            {
                                type = CardEffectType.BlockRetreat,
                                zoneTarget = "Stage",
                                playerWhoUsedTheEffect = cMatchRoom.currentPlayerTurn,
                                playerWhoIsTheTargetOfEffect = ISFIRSTPLAYER ? cMatchRoom.secondPlayer : cMatchRoom.firstPlayer,
                                activatedTurn = cMatchRoom.currentTurn + 1
                            });
                            ResetResolution(cMatchRoom, SPSKILL);
                            break;
                        case "hBP01-004":
                            cMatchRoom.ActiveEffects.Add(new CardEffect
                            {
                                type = CardEffectType.FixedDiceRoll,
                                diceRollValue = 6,
                                playerWhoUsedTheEffect = cMatchRoom.currentPlayerTurn,
                                playerWhoIsTheTargetOfEffect = cMatchRoom.currentPlayerTurn,
                                activatedTurn = cMatchRoom.currentTurn
                            });
                            ResetResolution(cMatchRoom, SPSKILL);
                            break;
                        case "hBP01-003":
                            if (playerStage.color.Equals("緑"))
                                Lib.RecoveryHP(cMatchRoom, STAGE: true, COLLAB: false, BACKSTAGE: false, RecoveryAmount: 9999, targetPlayerID: cMatchRoom.currentPlayerTurn);
                            ResetResolution(cMatchRoom, SPSKILL);
                            break;
                        case "hYS01-003":
                            string SelectedCard = _DuelAction.actionObject;
                            int n = Lib.CheckIfCardExistAtList(cMatchRoom, cMatchRoom.currentPlayerTurn, SelectedCard, "Arquive");
                            if (n > -1)
                            {
                                Card card = new Card(SelectedCard).GetCardInfo();
                                if (!(card.cardType.Equals("ホロメン") || card.cardType.Equals("Buzzホロメン")))
                                {
                                    Lib.WriteConsoleMessage("selected Card not holomem");
                                    return;
                                }

                                DuelAction duelAction = new()
                                {
                                    playerID = cMatchRoom.currentPlayerTurn,
                                    cardList = new() { new Card(SelectedCard) }
                                };
                                playerHand.RemoveAt(n);
                                Lib.SendPlayerData(cMatchRoom, false, duelAction, "RemoveCardsFromHand");
                                Lib.SendPlayerData(cMatchRoom, false, duelAction, "DrawOshiEffect");
                            }
                            ResetResolution(cMatchRoom, SPSKILL);
                            break;
                        case "hSD01-001":

                            if (!(_DuelAction.targetCard.cardPosition.Equals("BackStage1") || _DuelAction.targetCard.cardPosition.Equals("BackStage2") || _DuelAction.targetCard.cardPosition.Equals("BackStage3") ||
                                _DuelAction.targetCard.cardPosition.Equals("BackStage4") || _DuelAction.targetCard.cardPosition.Equals("BackStage5")))
                            {
                                Lib.WriteConsoleMessage("Invalid target position");
                                return;
                            }

                            if(Lib.IsSwitchBlocked(cMatchRoom, _DuelAction.targetCard.cardPosition) || Lib.IsSwitchBlocked(cMatchRoom, _DuelAction.usedCard.cardPosition))
                            {
                                Lib.WriteConsoleMessage("Cannot retreat by effect");
                                return;
                            }

                            Lib.SwittchCardYToCardZButKeepPosition(cMatchRoom, MatchRoom.GetOtherPlayer(cMatchRoom, playerRequest.playerID), _DuelAction.targetCard);

                            cMatchRoom.ActiveEffects.Add(new CardEffect
                            {
                                cardNumber = _DuelAction.usedCard.cardNumber,
                                zoneTarget = "Stage",
                                ExistXAtZone_Color = "白",
                                type = CardEffectType.BuffThisCardDamageExistXCOLORAtZone,
                                Damage = 50,
                                playerWhoUsedTheEffect = cMatchRoom.currentPlayerTurn,
                                playerWhoIsTheTargetOfEffect = cMatchRoom.currentPlayerTurn,
                                activatedTurn = cMatchRoom.currentTurn
                            });

                            Lib.SendPlayerData(cMatchRoom, false, _DuelAction, "SwitchOpponentStageCard");
                            ResetResolution(cMatchRoom, SPSKILL);
                            break;
                        case "hYS01-002":
                            cMatchRoom.ActiveEffects.Add(new CardEffect
                            {
                                cardNumber = _DuelAction.usedCard.cardNumber,
                                zoneTarget = "Collaboration",
                                ExistXAtZone_Color = "緑",
                                type = CardEffectType.BuffZoneCardDamageExistXCOLORAtZone,
                                Damage = 20,
                                playerWhoUsedTheEffect = cMatchRoom.currentPlayerTurn,
                                playerWhoIsTheTargetOfEffect = cMatchRoom.currentPlayerTurn,
                                activatedTurn = cMatchRoom.currentTurn
                            });
                            ResetResolution(cMatchRoom, SPSKILL); 
                            break;
                        case "hBP01-001":
                            cMatchRoom.ActiveEffects.Add(new CardEffect
                            {
                                cardNumber = _DuelAction.targetCard.cardNumber,
                                zoneTarget = _DuelAction.targetCard.cardPosition,
                                type = CardEffectType.BuffDamageToCardAtZone,
                                Damage = _DuelAction.usedCard.color.Equals("白") ? 100 : 50,
                                playerWhoUsedTheEffect = cMatchRoom.currentPlayerTurn,
                                playerWhoIsTheTargetOfEffect = cMatchRoom.currentPlayerTurn,
                                activatedTurn = cMatchRoom.currentTurn
                            });
                            ResetResolution(cMatchRoom, SPSKILL);
                            break;
                        case "hYS01-001":
                            cMatchRoom.ActiveEffects.Add(new CardEffect
                            {
                                cardNumber = _DuelAction.usedCard.cardNumber,
                                zoneTarget = "Collaboration",
                                ExistXAtZone_Color = "白",
                                type = CardEffectType.BuffZoneCardDamageExistXCOLORAtZone,
                                Damage = 20,
                                playerWhoUsedTheEffect = cMatchRoom.currentPlayerTurn,
                                playerWhoIsTheTargetOfEffect = cMatchRoom.currentPlayerTurn,
                                activatedTurn = cMatchRoom.currentTurn
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
                        case "hYS01-004":
                            cMatchRoom.ActiveEffects.Add(new CardEffect
                            {
                                cardNumber = _DuelAction.usedCard.cardNumber,
                                zoneTarget = "Collaboration",
                                ExistXAtZone_Color = "青",
                                type = CardEffectType.BuffThisCardDamageExistXCOLORAtZone,
                                Damage = 20,
                                playerWhoUsedTheEffect = cMatchRoom.currentPlayerTurn,
                                playerWhoIsTheTargetOfEffect = cMatchRoom.currentPlayerTurn,
                                activatedTurn = cMatchRoom.currentTurn
                            });
                            ResetResolution(cMatchRoom, SPSKILL);
                            break;
                        case "hYS01-003":

                            cMatchRoom.ActiveEffects.Add(new CardEffect
                            {
                                cardNumber = _DuelAction.usedCard.cardNumber,
                                zoneTarget = "Collaboration",
                                ExistXAtZone_Color = "赤",
                                type = CardEffectType.BuffThisCardDamageExistXCOLORAtZone,
                                Damage = 20,
                                playerWhoUsedTheEffect = cMatchRoom.currentPlayerTurn,
                                playerWhoIsTheTargetOfEffect = cMatchRoom.currentPlayerTurn,
                                activatedTurn = cMatchRoom.currentTurn
                            });
                            ResetResolution(cMatchRoom, SPSKILL);
                            break;
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
                        case "hBP01-006":
                            string SelectedCard = _DuelAction.actionObject;
                            int n = Lib.CheckIfCardExistAtList(cMatchRoom, cMatchRoom.currentPlayerTurn, SelectedCard, "Arquive");
                            if (n > -1)
                            {
                                Card card = new Card(SelectedCard).GetCardInfo();
                                if (!(card.cardType.Equals("ホロメン") || card.cardType.Equals("Buzzホロメン")))
                                {
                                    Lib.WriteConsoleMessage("selected Card not holomem");
                                    return;
                                }

                                duelAction = new()
                                {
                                    playerID = cMatchRoom.currentPlayerTurn,
                                    cardList = new() { card }
                                };
                                playerHand.RemoveAt(n);
                                Lib.SendPlayerData(cMatchRoom, false, duelAction, "RemoveCardsFromArquive");
                                Lib.SendPlayerData(cMatchRoom, false, duelAction, "DrawOshiEffect");
                            }
                            ResetResolution(cMatchRoom, SPSKILL);
                            break;
                        case "hBP01-003":
                            List<Card> listToSend = new();
                            foreach (Card card in playerDeck)
                            {
                                card.GetCardInfo();
                                if (card.name.Equals("石の斧"))
                                    listToSend.Add(card);
                            }

                            if (listToSend.Count == 0)
                            {
                                ResetResolution(cMatchRoom, SPSKILL);
                                return;
                            }
                            //hold the card at resolving
                            cMatchRoom.currentCardResolvingStage = "1";
                            playerTempHand.AddRange(listToSend);

                            //send the info to the currentplayer so he can pick the card
                            _DuelAction.actionObject = JsonSerializer.Serialize(listToSend, Lib.options);
                            pReturnData = new PlayerRequest { type = "DuelUpdate", description = "ResolveOnOshiSPEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                            Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn.ToString()], pReturnData);
                            break;
                        case "hBP01-0031":
                            cMatchRoom.ShuffleCards(playerDeck);
                            Lib.SendPlayerData(cMatchRoom, false, new() { playerID = playerRequest.playerID}, "SuffleDeck");
                            var handler151 = new AttachEquipamentToHolomemHandler(MessageDispatcher.playerConnections, MessageDispatcher._MatchRooms);
                            await handler151.AttachEquipamentToHolomemHandleAsync(playerRequest, webSocket, "Deck");
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



            int indexInHand = Lib.CheckIfCardExistAtList(cMatchRoom, cMatchRoom.currentPlayerTurn, cMatchRoom.currentCardResolving);
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
    }
}