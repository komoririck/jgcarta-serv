using hololive_oficial_cardgame_server.SerializableObjects;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using static hololive_oficial_cardgame_server.SerializableObjects.MatchRoom;
using System.Text.Json;
using Microsoft.VisualBasic;
using hololive_oficial_cardgame_server.WebSocketDuelFunctions;

namespace hololive_oficial_cardgame_server.EffectControllers
{
    class BloomEffects
    {
        internal static async Task OnBloomEffectResolutionAsync(PlayerRequest playerRequest, MatchRoom cMatchRoom)
        {
            DuelAction _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestObject);


            if (string.IsNullOrEmpty(cMatchRoom.currentCardResolving))
                cMatchRoom.currentCardResolving = cMatchRoom.currentCardResolving = _DuelAction.usedCard.cardNumber;

            if (string.IsNullOrEmpty(cMatchRoom.currentCardResolving))
            {
                Lib.WriteConsoleMessage("not card are current resolving");
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
            List<int> diceList = ISFIRSTPLAYER ? cMatchRoom.playerADiceRollList : cMatchRoom.playerBDiceRollList;

            bool energyPaid = false;

            _DuelAction.targetCard?.GetCardInfo();
            _DuelAction.usedCard?.GetCardInfo();
            _DuelAction.cheerCostCard.GetCardInfo();

            //ensure that we have a resolving card, if we have dont need to use
            if (string.IsNullOrEmpty(cMatchRoom.currentCardResolving))
                cMatchRoom.currentCardResolving = _DuelAction.usedCard.cardNumber;

            string playerWhoUsedTheEffect = _DuelAction.playerID;

            try
            {

                switch (_DuelAction.usedCard.cardNumber + cMatchRoom.currentCardResolvingStage)
                {
                    case "hBP01-043":
                        if (_DuelAction.usedCard.cardPosition.Equals("Stage"))
                            Lib.RecoveryHP(cMatchRoom, STAGE: true, COLLAB: false, BACKSTAGE: false, RecoveryAmount: 50, targetPlayerID: cMatchRoom.currentPlayerTurn);
                        else if (_DuelAction.usedCard.cardPosition.Equals("Collaboration"))
                            Lib.RecoveryHP(cMatchRoom, STAGE: false, COLLAB: true, BACKSTAGE: false, RecoveryAmount: 50, targetPlayerID: cMatchRoom.currentPlayerTurn);
                        else
                            Lib.RecoveryHP(cMatchRoom, STAGE: false, COLLAB: false, BACKSTAGE: true, RecoveryAmount: 50, targetPlayerID: cMatchRoom.currentPlayerTurn, _DuelAction.usedCard.cardPosition);

                        ResetResolution(cMatchRoom);
                        break;
                    case "hBP01-030":
                        CardEffect _CardEffect = new CardEffect
                        {
                            cardNumber = _DuelAction.usedCard.cardNumber,
                            zoneTarget = "Collaboration",
                            type = CardEffectType.BuffDamageToCardAtZoneIfHaveTag,
                            cardTag = "#promise",
                            Damage = 30,
                            playerWhoUsedTheEffect = playerWhoUsedTheEffect,
                            playerWhoIsTheTargetOfEffect = playerWhoUsedTheEffect,
                            activatedTurn = cMatchRoom.currentTurn
                        };
                        cMatchRoom.ActiveEffects.Add(_CardEffect);
                        _CardEffect = new CardEffect
                        {
                            cardNumber = _DuelAction.usedCard.cardNumber,
                            zoneTarget = "Stage",
                            type = CardEffectType.BuffDamageToCardAtZoneIfHaveTag,
                            cardTag = "#promise",
                            Damage = 30,
                            playerWhoUsedTheEffect = playerWhoUsedTheEffect,
                            playerWhoIsTheTargetOfEffect = playerWhoUsedTheEffect,
                            activatedTurn = cMatchRoom.currentTurn
                        };
                        cMatchRoom.ActiveEffects.Add(_CardEffect);
                        ResetResolution(cMatchRoom);
                        break;
                    case "hBP01-060":
                        string SelectedCard = _DuelAction.actionObject;
                        int n = Lib.CheckIfCardExistAtList(cMatchRoom, cMatchRoom.currentPlayerTurn, SelectedCard);
                        if (n > -1)
                        {
                            DuelAction duelAction = new()
                            {
                                playerID = cMatchRoom.currentPlayerTurn,
                                local = "Hand",
                                cardList = new() { new Card(SelectedCard) }
                            };

                            playerHand.RemoveAt(n);

                            Lib.SendPlayerData(cMatchRoom, false, duelAction, "RemoveCardsFromHand");

                            PlayerRequest ReturnData = new PlayerRequest { type = "DuelUpdate", description = "DrawBloomEffect", requestObject = "" };
                            if (cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer)
                            {
                                Lib.getCardFromDeck(cMatchRoom.playerADeck, cMatchRoom.playerAHand, 1);
                                Lib.AddTopDeckToDrawObjectAsync(cMatchRoom.firstPlayer, cMatchRoom.playerAHand, true, cMatchRoom, ReturnData);
                            }
                            else
                            {
                                Lib.getCardFromDeck(cMatchRoom.playerBDeck, cMatchRoom.playerBHand, 1);
                                Lib.AddTopDeckToDrawObjectAsync(cMatchRoom.secondPlayer, cMatchRoom.playerBHand, true, cMatchRoom, ReturnData);
                            }
                        }
                        ResetResolution(cMatchRoom);
                        break;
                    case "hBP01-012":
                        if (!_DuelAction.actionObject.Equals("Yes"))
                        {
                            ResetResolution(cMatchRoom);
                            return;
                        }

                        int diceValue = Lib.GetDiceNumber(cMatchRoom, cMatchRoom.currentPlayerTurn);
                        cMatchRoom.currentCardResolvingStage = "1";

                        Lib.SendDiceRoll(cMatchRoom, new List<int>() { diceValue }, COUNTFORRESONSE: true);
                        break;
                    case "hBP01-0121":
                        diceValue = diceList.Last();
                        if (diceValue > 3)
                        {
                            ResetResolution(cMatchRoom);
                            return;
                        }

                        List<Card> listToSend = new();
                        foreach (Card card in playerDeck)
                        {
                            card.GetCardInfo();
                            if (card.cardType.Equals("サポート・マスコット"))
                            {
                                if (!Lib.CanBeAttachedToAnyInTheField(cMatchRoom, playerRequest.playerID, _DuelAction.usedCard))
                                {
                                    listToSend.Add(card);
                                }
                            }
                        }

                        if (listToSend.Count == 0)
                        {
                            ResetResolution(cMatchRoom);
                            return;
                        }

                        //hold the card at resolving
                        cMatchRoom.currentCardResolvingStage = "2";
                        playerTempHand.AddRange(listToSend);

                        //send the info to the currentplayer so he can pick the card
                        _DuelAction.actionObject = JsonSerializer.Serialize(listToSend, Lib.options);
                        pReturnData = new PlayerRequest { type = "DuelUpdate", description = "ResolveOnBloomEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                        Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn.ToString()], pReturnData);
                        break;
                    case "hBP01-0122":
                        var handler151 = new AttachEquipamentToHolomemHandler();
                        await handler151.AttachEquipamentToHolomemHandleAsync(playerRequest, "Deck");
                        ResetResolution(cMatchRoom);
                        break;
                    case "hBP01-013":
                        Card opsStage = !ISFIRSTPLAYER ? cMatchRoom.playerAStage : cMatchRoom.playerBStage;

                        if (opsStage == null) {
                            ResetResolution(cMatchRoom);
                            return;
                        }

                        _DuelAction = new()
                        {
                            playerID = GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn),
                            targetCard = opsStage,
                        };

                        cMatchRoom.currentEffectDamage = 30;

                        _DuelAction.actionObject = cMatchRoom.currentEffectDamage.ToString();

                        _DuelAction.playerID = cMatchRoom.currentPlayerTurn;
                        // Serialize and send data to the current player
                        PlayerRequest _ReturnData = new PlayerRequest { type = "DuelUpdate", description = "InflicDamageToHolomem", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                        Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer], _ReturnData);
                        Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer], _ReturnData);

                        _CardEffect = new CardEffect
                        {
                            cardNumber = _DuelAction.usedCard.cardNumber,
                            zoneTarget = "Stage",
                            type = CardEffectType.ProtectFromOneLifeCostCharge,
                            playerWhoUsedTheEffect = playerWhoUsedTheEffect,
                            playerWhoIsTheTargetOfEffect = GetOtherPlayer(cMatchRoom, playerWhoUsedTheEffect),
                            activatedTurn = cMatchRoom.currentTurn
                        };
                        cMatchRoom.ActiveEffects.Add(_CardEffect);
                        ResetResolution(cMatchRoom);
                        break;
                    case "hBP01-014":
                        opsStage = !ISFIRSTPLAYER ? cMatchRoom.playerAStage : cMatchRoom.playerBStage;

                        if (opsStage == null)
                        {
                            ResetResolution(cMatchRoom);
                            return;
                        }

                        _DuelAction = new()
                        {
                            playerID = GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn),
                            targetCard = opsStage,
                        };


                        cMatchRoom.currentEffectDamage = 50;

                        _DuelAction.actionObject = cMatchRoom.currentEffectDamage.ToString();

                        _DuelAction.playerID = cMatchRoom.currentPlayerTurn;
                        // Serialize and send data to the current player
                        _ReturnData = new PlayerRequest { type = "DuelUpdate", description = "InflicDamageToHolomem", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                        Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer], _ReturnData);
                        Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer], _ReturnData);
                        ResetResolution(cMatchRoom);
                        break;
                    case "hBP01-057":
                        Card opsCollab = !ISFIRSTPLAYER ? cMatchRoom.playerACollaboration : cMatchRoom.playerBCollaboration;

                        if (opsCollab == null)
                        {
                            ResetResolution(cMatchRoom);
                            return;
                        }

                        _DuelAction = new()
                        {
                            playerID = GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn),
                            targetCard = opsCollab,
                        };

                        cMatchRoom.currentEffectDamage = 10;

                        _DuelAction.actionObject = cMatchRoom.currentEffectDamage.ToString();

                        _DuelAction.playerID = cMatchRoom.currentPlayerTurn;
                        // Serialize and send data to the current player
                        _ReturnData = new PlayerRequest { type = "DuelUpdate", description = "InflicDamageToHolomem", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                        Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.firstPlayer], _ReturnData);
                        Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.secondPlayer], _ReturnData);
                        ResetResolution(cMatchRoom);
                        break;
                    case "hBP01-090":
                        List<Card> EnergyList = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerACardCheer : cMatchRoom.playerBCardCheer;

                        if (EnergyList.Count < 1)
                        {
                            ResetResolution(cMatchRoom);
                            return;
                        }

                        playerTempHand = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;

                        listToSend = new();
                        foreach (Card card in EnergyList)
                        {
                            card.GetCardInfo();
                            if (card.color.Equals("青") || card.color.Equals("緑"))
                                listToSend.Add(card);
                        }

                        if (listToSend.Count == 0)
                        {
                            ResetResolution(cMatchRoom);
                            return;
                        }

                        //hold the card at resolving
                        cMatchRoom.currentCardResolvingStage = "1";
                        playerTempHand.AddRange(listToSend);

                        //send the info to the currentplayer so he can pick the card
                        _DuelAction.actionObject = JsonSerializer.Serialize(listToSend, Lib.options);
                        pReturnData = new PlayerRequest { type = "DuelUpdate", description = "ResolveOnBloomEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                        Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn.ToString()], pReturnData);
                        break;
                    case "hBP01-0901":
                        if (!_DuelAction.actionObject.Equals("Yes"))
                        {
                            ResetResolution(cMatchRoom);
                            return;
                        }

                        playerTempHand = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;

                        var handler191 = new AttachRangeFromCheerEnergyToZoneHandler();
                        await handler191.AttachRangeFromCheerEnergyToZoneHandleAsync(playerRequest, true, true, true);
                        ResetResolution(cMatchRoom);
                        break;
                    case "hBP01-037":
                        EnergyList = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerACardCheer : cMatchRoom.playerBCardCheer;
                        if (EnergyList.Count > 0)
                        {
                            //add to temp hand so we can get latter
                            playerTempHand.Add(EnergyList[EnergyList.Count - 1]);
                            //just for safety, lets change the position to cheer, so the rotine who is gonna use this know where came from to remove
                            playerTempHand[0].playedFrom = "CardCheer";
                            //setup list to send to player
                            returnToclient = new List<string>() { playerTempHand[0].cardNumber };
                        }

                        cMatchRoom.currentCardResolvingStage = "1";

                        //send the info to the currentplayer so he can pick the card
                        _DuelAction.actionObject = JsonSerializer.Serialize(returnToclient, Lib.options);
                        pReturnData = new PlayerRequest { type = "DuelUpdate", description = "ResolveOnBloomEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                        Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn], pReturnData);
                        Lib.SendMessage(MessageDispatcher.playerConnections[MatchRoom.GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn)], pReturnData);
                        break;
                    case "hBP01-0371":
                        var handler192 = new AttachTopCheerEnergyToBackHandler();
                        await handler192.AttachCheerEnergyHandleAsync(_DuelAction, cMatchRoom, stage: true, collab: true, back: true, TOPCHEERDECK: true, FULLCHEERDECK: false, ClientEnergyIndex: 0);
                        
                        Card targeted = null;
                        if (_DuelAction.targetCard.cardPosition.Equals("Stage"))
                        {
                            targeted = cMatchRoom.currentPlayerTurn.Equals(cMatchRoom.firstPlayer) ? cMatchRoom.playerAStage : cMatchRoom.playerBStage;
                        }
                        else if (_DuelAction.targetCard.cardPosition.Equals("Collaboration"))
                        {
                            targeted = cMatchRoom.currentPlayerTurn.Equals(cMatchRoom.firstPlayer) ? cMatchRoom.playerACollaboration : cMatchRoom.playerBCollaboration;
                        }
                        else {

                            foreach (Card card in playerBackstage) {
                                if (card.cardPosition.Equals(_DuelAction.targetCard.cardPosition)) {
                                    targeted = card;
                                    break;
                                }
                            }
                        }

                        foreach (Card equip in targeted.attachedEquipe) {
                            if (equip.cardType.Equals("サポート・ツール"))
                            {
                                Lib.RecoveryHP(cMatchRoom, _DuelAction, RecoveryAmount: 40);
                                break;
                            }
                        }
                        ResetResolution(cMatchRoom);
                        break;
                    case "hBP01-081":
                        EnergyList = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerACardCheer : cMatchRoom.playerBCardCheer;

                        if (!_DuelAction.targetCard.GetCardInfo().color.Equals("青"))
                        {
                            ResetResolution(cMatchRoom);
                            return;
                        }

                        if (EnergyList.Count > 0)
                        {
                            //add to temp hand so we can get latter
                            playerTempHand.Add(EnergyList[EnergyList.Count - 1]);
                            //just for safety, lets change the position to cheer, so the rotine who is gonna use this know where came from to remove
                            playerTempHand[0].playedFrom = "CardCheer";
                            //setup list to send to player
                            returnToclient = new List<string>() { playerTempHand[0].cardNumber };
                        }

                        cMatchRoom.currentCardResolvingStage = "1";

                        //send the info to the currentplayer so he can pick the card
                        _DuelAction.actionObject = JsonSerializer.Serialize(returnToclient, Lib.options);
                        pReturnData = new PlayerRequest { type = "DuelUpdate", description = "ResolveOnBloomEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                        Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn], pReturnData);
                        Lib.SendMessage(MessageDispatcher.playerConnections[MatchRoom.GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn)], pReturnData);
                        break;
                    case "hBP01-0811":
                        var handler194 = new AttachTopCheerEnergyToBackHandler();
                        await handler194.AttachCheerEnergyHandleAsync(_DuelAction, cMatchRoom, stage: true, collab: true, back: true, TOPCHEERDECK: true, FULLCHEERDECK: false, ClientEnergyIndex: 0);

                        ResetResolution(cMatchRoom);
                        break;
                    case "hBP01-054":
                        EnergyList = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerACardCheer : cMatchRoom.playerBCardCheer;

                        if (!_DuelAction.targetCard.GetCardInfo().cardTag.Contains("#ID") && !_DuelAction.targetCard.name.Equals("アイラニ・イオフィフティーン"))
                        {
                            ResetResolution(cMatchRoom);
                            return;
                        }

                        if (EnergyList.Count > 0)
                        {
                            //add to temp hand so we can get latter
                            playerTempHand.Add(EnergyList[EnergyList.Count - 1]);
                            //just for safety, lets change the position to cheer, so the rotine who is gonna use this know where came from to remove
                            playerTempHand[0].playedFrom = "CardCheer";
                            //setup list to send to player
                            returnToclient = new List<string>() { playerTempHand[0].cardNumber };
                        }

                        cMatchRoom.currentCardResolvingStage = "1";

                        //send the info to the currentplayer so he can pick the card
                        _DuelAction.actionObject = JsonSerializer.Serialize(returnToclient, Lib.options);
                        pReturnData = new PlayerRequest { type = "DuelUpdate", description = "ResolveOnBloomEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                        Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn], pReturnData);
                        Lib.SendMessage(MessageDispatcher.playerConnections[MatchRoom.GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn)], pReturnData);
                        break;
                    case "hBP01-0541":
                        handler194 = new AttachTopCheerEnergyToBackHandler();
                        await handler194.AttachCheerEnergyHandleAsync(_DuelAction, cMatchRoom, stage: true, collab: true, back: true, TOPCHEERDECK: true, FULLCHEERDECK: false, ClientEnergyIndex: 0);

                        ResetResolution(cMatchRoom);
                        break;
                }
            }
            catch (Exception e)
            {
                Lib.WriteConsoleMessage(e.Message + e.StackTrace + e.InnerException);
            }
        }
        static void ResetResolution(MatchRoom cMatchRoom)
        {

            bool ISFIRSTPLAYER = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer;

            List<Card> playerHand = ISFIRSTPLAYER ? cMatchRoom.playerAHand : cMatchRoom.playerBHand;
            List<Card> playerArquive = ISFIRSTPLAYER ? cMatchRoom.playerAArquive : cMatchRoom.playerBArquive;

            cMatchRoom.currentCardResolving = "";
            cMatchRoom.currentCardResolvingStage = "";

            List<Card> temphand = ISFIRSTPLAYER ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
            cMatchRoom.currentGameHigh++;

            cMatchRoom.currentGamePhase = GAMEPHASE.MainStep;
            temphand.Clear();
        }
    }
}