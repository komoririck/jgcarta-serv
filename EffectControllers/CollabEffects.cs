using hololive_oficial_cardgame_server.SerializableObjects;
using hololive_oficial_cardgame_server.WebSocketDuelFunctions;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text.Json;

namespace hololive_oficial_cardgame_server.EffectControllers
{
    class CollabEffects
    {
        internal static async Task OnCollabEffectAsync(DuelAction _DuelAction, MatchRoom cMatchRoom, string playerWhoUsedTheEffect, PlayerRequest playerRequest = null, WebSocket webSocket = null)
        {
            CardEffect _CardEffect;
            PlayerRequest pReturnData = new();

            List<Card> tempHandList = new();
            List<Card> EnergyList = new();
            List<Card> backPos = new();
            List<string> returnToclient = new();

            Card stage = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerAStage : cMatchRoom.playerBStage;
            EnergyList = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerACardCheer : cMatchRoom.playerBCardCheer;
            tempHandList = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
            backPos = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerABackPosition : cMatchRoom.playerBBackPosition;
            List<int> diceList = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerADiceRollList : cMatchRoom.playerBDiceRollList;
            List<Card> playerCheer = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerACardCheer : cMatchRoom.playerBCardCheer;
            List<Card> playerDeck = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerADeck : cMatchRoom.playerBDeck;


            switch (cMatchRoom.currentCardResolving + cMatchRoom.currentCardResolvingStage)
            {
                case "hBP01-036":
                    if (_DuelAction.targetCard.cardPosition.Equals("Stage"))
                        Lib.RecoveryHP(cMatchRoom, STAGE: true, COLLAB: false, BACKSTAGE: false, RecoveryAmount: 20, targetPlayerID: cMatchRoom.currentPlayerTurn);
                    else if (_DuelAction.targetCard.cardPosition.Equals("Collaboration"))
                        Lib.RecoveryHP(cMatchRoom, STAGE: false, COLLAB: true, BACKSTAGE: false, RecoveryAmount: 20, targetPlayerID: cMatchRoom.currentPlayerTurn);
                    else
                        Lib.RecoveryHP(cMatchRoom, STAGE: false, COLLAB: false, BACKSTAGE: true, RecoveryAmount: 20, targetPlayerID: cMatchRoom.currentPlayerTurn, _DuelAction.targetCard.cardPosition);

                    ResetResolution();
                    break;
                case "hBP01-033":
                    int diceValue = Lib.GetDiceNumber(cMatchRoom, cMatchRoom.currentPlayerTurn);

                    cMatchRoom.currentCardResolvingStage = "1";

                    Lib.SendDiceRoll(cMatchRoom, new() { diceValue }, COUNTFORRESONSE: false);

                    pReturnData = new PlayerRequest { type = "DuelUpdate", description = "OnCollabEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                    Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn], pReturnData);
                    break;
                case "hBP01-0331":
                    diceValue = diceList.Last();

                    returnToclient = new List<string>() { diceValue.ToString() };

                    if (diceValue == 1 || diceValue == 3 || diceValue == 5)
                    {
                        ResetResolution();
                        return;
                    }


                    if (_DuelAction.targetCard.cardPosition.Equals("Stage"))
                        Lib.RecoveryHP(cMatchRoom, STAGE: true, COLLAB: false, BACKSTAGE: false, RecoveryAmount: 20, targetPlayerID: cMatchRoom.currentPlayerTurn);
                    else if (_DuelAction.targetCard.cardPosition.Equals("Collaboration"))
                        Lib.RecoveryHP(cMatchRoom, STAGE: false, COLLAB: true, BACKSTAGE: false, RecoveryAmount: 20, targetPlayerID: cMatchRoom.currentPlayerTurn);
                    else
                        Lib.RecoveryHP(cMatchRoom, STAGE: false, COLLAB: false, BACKSTAGE: true, RecoveryAmount: 20, targetPlayerID: cMatchRoom.currentPlayerTurn, _DuelAction.targetCard.cardPosition);

                    ResetResolution();
                    break;
                case "hSD01-015":

                    stage.GetCardInfo();

                    if ((stage.name.Equals("AZKi") || stage.name.Equals("SorAZ")) && EnergyList.Count > 0)
                    {
                        tempHandList = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
                        //add to temp hand so we can get latter
                        tempHandList.Add(EnergyList[EnergyList.Count - 1]);
                        //just for safety, lets change the position to cheer, so the rotine who is gonna use this know where came from to remove
                        tempHandList[0].playedFrom = "CardCheer";
                        //setup list to send to player

                        var handler190 = new AttachTopCheerEnergyToBackHandler(MessageDispatcher.playerConnections, MessageDispatcher._MatchRooms);

                        _DuelAction.actionObject = JsonSerializer.Serialize(new List<string>() { tempHandList[0].cardNumber }, Lib.options);
                        _DuelAction.targetCard = stage;

                        PlayerRequest _playerRequest = new PlayerRequest { type = "DuelUpdate", description = "OnCollabEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                        await handler190.AttachCheerEnergyHandleAsync(_DuelAction, cMatchRoom, stage: true, collab: false, back: false, TOPCHEERDECK: true, FULLCHEERDECK: false, energyIndex: 0);
                    }
                    if ((stage.name.Equals("ときのそら") || stage.name.Equals("SorAZ")) && playerDeck.Count > 0)
                    {
                        PlayerRequest ReturnData = new PlayerRequest { type = "DuelUpdate", description = "DrawCollabEffect", requestObject = "" };
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
                    ResetResolution();
                    break;
                case "hBP01-010":
                    _CardEffect = new CardEffect
                    {
                        cardNumber = _DuelAction.usedCard.cardNumber,
                        zoneTarget = "Stage",
                        type = CardEffectType.BuffDamageToCardAtZone,
                        Damage = 10,
                        playerWhoUsedTheEffect = playerWhoUsedTheEffect,
                        playerWhoIsTheTargetOfEffect = playerWhoUsedTheEffect,
                        activatedTurn = cMatchRoom.currentTurn
                    };
                    cMatchRoom.ActiveEffects.Add(_CardEffect);
                    if (stage.cardTag.Contains("#４期生"))
                    {
                        _CardEffect = new CardEffect
                        {
                            cardNumber = _DuelAction.usedCard.cardNumber,
                            zoneTarget = "Stage",
                            type = CardEffectType.BuffDamageToCardAtZone,
                            Damage = 20,
                            playerWhoUsedTheEffect = playerWhoUsedTheEffect,
                            playerWhoIsTheTargetOfEffect = playerWhoUsedTheEffect,
                            activatedTurn = cMatchRoom.currentTurn
                        };
                        cMatchRoom.ActiveEffects.Add(_CardEffect);
                    }
                    ResetResolution();
                    break;
                case "hBP01-015":
                    if (cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer)
                    {
                        if ((cMatchRoom.playerAUsedSupportThisTurn && cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer) || (cMatchRoom.playerBUsedSupportThisTurn && cMatchRoom.currentPlayerTurn != cMatchRoom.firstPlayer))
                        {
                            _CardEffect = new CardEffect
                            {
                                cardNumber = _DuelAction.usedCard.cardNumber,
                                zoneTarget = _DuelAction.usedCard.cardPosition,
                                type = CardEffectType.BuffDamageToCardAtZone,
                                Damage = 20,
                                playerWhoUsedTheEffect = playerWhoUsedTheEffect,
                                playerWhoIsTheTargetOfEffect = playerWhoUsedTheEffect,
                                activatedTurn = cMatchRoom.currentTurn
                            };
                            cMatchRoom.ActiveEffects.Add(_CardEffect);
                        }
                    }
                    ResetResolution();
                    break;
                case "hSD01-004":
                    _CardEffect = new CardEffect
                    {
                        cardNumber = _DuelAction.usedCard.cardNumber,
                        zoneTarget = "Stage",
                        type = CardEffectType.BuffDamageToCardAtZone,
                        Damage = 20,
                        playerWhoUsedTheEffect = playerWhoUsedTheEffect,
                        playerWhoIsTheTargetOfEffect = playerWhoUsedTheEffect,
                        activatedTurn = cMatchRoom.currentTurn
                    };
                    cMatchRoom.ActiveEffects.Add(_CardEffect);
                    ResetResolution();
                    break;
                case "hSD01-007":
                    //get the player holopowerlist
                    EnergyList = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerAHoloPower : cMatchRoom.playerBHoloPower;
                    //send the info to the currentplayer so he can pick the card
                    _DuelAction.actionObject = JsonSerializer.Serialize(EnergyList, Lib.options);
                    pReturnData = new PlayerRequest { type = "DuelUpdate", description = "OnCollabEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };

                    cMatchRoom.currentCardResolvingStage = "1";
                    Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn.ToString()], pReturnData);
                    Lib.SendMessage(MessageDispatcher.playerConnections[MatchRoom.GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn).ToString()], pReturnData);
                    break;
                case "hSD01-0071":
                    var handler1 = new PickFromListThenGiveBacKFromHandHandler(MessageDispatcher.playerConnections, MessageDispatcher._MatchRooms);
                    await handler1.PickFromHoloPowerThenGiveBacKFromHandHandleAsync(playerRequest, webSocket);
                    ResetResolution();
                    break;
                case "hSD01-009":
                    diceValue = Lib.GetDiceNumber(cMatchRoom, cMatchRoom.currentPlayerTurn);

                    cMatchRoom.currentCardResolvingStage = "1";

                    Lib.SendDiceRoll(cMatchRoom, new() {  diceValue }, COUNTFORRESONSE: false);


                    if (playerCheer.Count < 1 || backPos.Count < 1)
                    {
                        ResetResolution();
                        return;
                    }

                    pReturnData = new PlayerRequest { type = "DuelUpdate", description = "OnCollabEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                    Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn], pReturnData);
                    break;
                case "hSD01-0091":
                    diceValue = diceList.Last();

                    returnToclient = new List<string>() { diceValue.ToString() };

                    if (diceValue > 4)
                    {
                        ResetResolution();
                        return;
                    }

                    if (diceValue < 5 && EnergyList.Count > 0)
                    {
                        //add to temp hand so we can get latter
                        tempHandList.Add(EnergyList[EnergyList.Count - 1]);
                        //just for safety, lets change the position to cheer, so the rotine who is gonna use this know where came from to remove
                        tempHandList[0].playedFrom = "CardCheer";
                        //setup list to send to player
                        returnToclient = new List<string>() { tempHandList[0].cardNumber };
                    }

                    cMatchRoom.currentCardResolvingStage = "2";

                    //send the info to the currentplayer so he can pick the card
                    _DuelAction.actionObject = JsonSerializer.Serialize(returnToclient, Lib.options);
                    pReturnData = new PlayerRequest { type = "DuelUpdate", description = "OnCollabEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                    Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn], pReturnData);
                    Lib.SendMessage(MessageDispatcher.playerConnections[MatchRoom.GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn)], pReturnData);
                    break;
                case "hSD01-0092":
                    var handler192 = new AttachTopCheerEnergyToBackHandler(MessageDispatcher.playerConnections, MessageDispatcher._MatchRooms);
                    await handler192.AttachCheerEnergyHandleAsync(_DuelAction, cMatchRoom, stage: false, collab: false, back: true, TOPCHEERDECK: true, FULLCHEERDECK: false, energyIndex: 0);

                    diceValue = diceList.Last();
                    if (diceValue < 2)
                    {
                        cMatchRoom.currentCardResolvingStage = "3";
                    }
                    else
                    {
                        ResetResolution();
                    }
                    break;
                case "hSD01-0093":
                    if (_DuelAction.actionObject.Equals("Yes") && diceList.Last() < 2)
                        if (!new Lib().ReturnCollabToBackStage(cMatchRoom))
                            Lib.WriteConsoleMessage("Fallied to finish ReturnCardToBackStage at AskAttachEnergyAndRetreat");
                    ResetResolution();
                    break;
                case "hSD01-012":
                    EnergyList = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerACardCheer : cMatchRoom.playerBCardCheer;

                    if (EnergyList.Count < 1)
                    {
                        ResetResolution();
                        return;
                    }

                    tempHandList = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;

                    List<Card> listToSend = new();
                    foreach (Card card in EnergyList)
                    {
                        card.GetCardInfo();
                        if (card.color.Equals("白") || card.color.Equals("緑"))
                            listToSend.Add(card);
                    }

                    if (listToSend.Count == 0)
                    {
                        ResetResolution();
                        return;
                    }

                    //hold the card at resolving
                    cMatchRoom.currentCardResolvingStage = "1";
                    tempHandList.AddRange(listToSend);

                    //send the info to the currentplayer so he can pick the card
                    _DuelAction.actionObject = JsonSerializer.Serialize(listToSend, Lib.options);
                    pReturnData = new PlayerRequest { type = "DuelUpdate", description = "OnCollabEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                    Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn.ToString()], pReturnData);
                    break;
                case "hSD01-0121":
                    if (!_DuelAction.actionObject.Equals("Yes"))
                    {
                        ResetResolution();
                        return;
                    }

                    tempHandList = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;

                    var handler191 = new AttachRangeFromCheerEnergyToZoneHandler(MessageDispatcher.playerConnections, MessageDispatcher._MatchRooms);
                    await handler191.AttachRangeFromCheerEnergyToZoneHandleAsync(playerRequest, webSocket, true, false, false);
                    ResetResolution();
                    break;
                case "hBP01-016":
                    if (stage.cardTag.ToLower().Contains("#Promise"))
                    {
                        Lib.UseCardEffectDrawAnyAsync(cMatchRoom, 1, "hBP01-016");
                    }
                    ResetResolution();
                    break;
                case "hBP01-023":
                    Lib.UseCardEffectDrawAnyAsync(cMatchRoom, 2, "hBP01-023");
                    ResetResolution();
                    break;
                default:
                    ResetResolution();
                    break;
            }
            void ResetResolution()
            {
                if (cMatchRoom.extraInfo != null)
                    cMatchRoom.extraInfo.Clear();

                cMatchRoom.currentCardResolving = "";
                cMatchRoom.currentCardResolvingStage = "";
                cMatchRoom.currentDuelActionResolvingRecieved.Clear();

                List<Card> temphand = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
                temphand.Clear();

                List<int> diceRollList = cMatchRoom.currentPlayerTurn.Equals(cMatchRoom.firstPlayer) ? cMatchRoom.playerADiceRollList : cMatchRoom.playerBDiceRollList;
                diceRollList.Clear();
            }
        }

        internal static async Task OnCollabEffectResolutionAsync(ConcurrentDictionary<string, WebSocket> playerConnections, List<MatchRoom> matchRooms, PlayerRequest playerRequest, WebSocket webSocket)
        {
            DuelAction _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestObject);

            int matchnumber = MatchRoom.FindPlayerMatchRoom(matchRooms, playerRequest.playerID);
            MatchRoom cMatchRoom = matchRooms[matchnumber];

            if (playerRequest.playerID != cMatchRoom.currentPlayerTurn)
            {
                Lib.WriteConsoleMessage("Wrong player calling");
                return;
            }

            if (string.IsNullOrEmpty(cMatchRoom.currentCardResolving))
            {
                Lib.WriteConsoleMessage("not card are current resolving");
                return;
            }

            OnCollabEffectAsync(_DuelAction, cMatchRoom, playerRequest.playerID, playerRequest, webSocket);
        }
    }
}