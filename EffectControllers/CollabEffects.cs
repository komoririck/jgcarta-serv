using hololive_oficial_cardgame_server.SerializableObjects;
using hololive_oficial_cardgame_server.WebSocketDuelFunctions;
using Org.BouncyCastle.Asn1;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;

namespace hololive_oficial_cardgame_server.EffectControllers
{
    class CollabEffects
    {
        internal static async Task OnCollabEffectAsync(DuelAction _DuelAction, MatchRoom cMatchRoom, string playerWhoUsedTheEffect, PlayerRequest playerRequest = null, WebSocket webSocket = null)
        {
            CardEffect _CardEffect;
            PlayerRequest pReturnData = new();

            List<Card> backPos = [];
            List<string> returnToclient = [];

            Card stage = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerAStage : cMatchRoom.playerBStage;
            List<Card> CheerDeck = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerACardCheer : cMatchRoom.playerBCardCheer;
            List<Card> tempHandList = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
            backPos = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerABackPosition : cMatchRoom.playerBBackPosition;
            List<int> diceList = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerADiceRollList : cMatchRoom.playerBDiceRollList;
            List<Card> playerCheer = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerACardCheer : cMatchRoom.playerBCardCheer;
            List<Card> playerDeck = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerADeck : cMatchRoom.playerBDeck;
            List<Card> playerTempHand = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
            Card oshi = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerAOshi : cMatchRoom.playerBOshi;
            List<Card> playerHand = cMatchRoom.currentPlayerTurn == cMatchRoom.firstPlayer ? cMatchRoom.playerAHand : cMatchRoom.playerBHand;
            List<Card> HoloPowerDeck = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerAHoloPower : cMatchRoom.playerBHoloPower;
            List<Card> playerArquive = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerAArquive : cMatchRoom.playerBArquive;

            switch (cMatchRoom.currentCardResolving + cMatchRoom.currentCardResolvingStage)
            {
                case "hBP01-036":
                    if (_DuelAction.targetCard.cardPosition.Equals("Stage"))
                        Lib.RecoveryHPAsync(cMatchRoom, STAGE: true, COLLAB: false, BACKSTAGE: false, RecoveryAmount: 20, targetPlayerID: cMatchRoom.currentPlayerTurn);
                    else if (_DuelAction.targetCard.cardPosition.Equals("Collaboration"))
                        Lib.RecoveryHPAsync(cMatchRoom, STAGE: false, COLLAB: true, BACKSTAGE: false, RecoveryAmount: 20, targetPlayerID: cMatchRoom.currentPlayerTurn);
                    else
                        Lib.RecoveryHPAsync(cMatchRoom, STAGE: false, COLLAB: false, BACKSTAGE: true, RecoveryAmount: 20, targetPlayerID: cMatchRoom.currentPlayerTurn, _DuelAction.targetCard.cardPosition);

                    ResetResolution();
                    break;
                case "hBP01-033":
                    int diceValue = Lib.GetDiceNumber(cMatchRoom, cMatchRoom.currentPlayerTurn);

                    cMatchRoom.currentCardResolvingStage = "1";

                    Lib.SendDiceRollAsync(cMatchRoom, [diceValue], COUNTFORRESONSE: false);

                    pReturnData = new PlayerRequest { type = "DuelUpdate", description = "OnCollabEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                    await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn], pReturnData);
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
                        Lib.RecoveryHPAsync(cMatchRoom, STAGE: true, COLLAB: false, BACKSTAGE: false, RecoveryAmount: 20, targetPlayerID: cMatchRoom.currentPlayerTurn);
                    else if (_DuelAction.targetCard.cardPosition.Equals("Collaboration"))
                        Lib.RecoveryHPAsync(cMatchRoom, STAGE: false, COLLAB: true, BACKSTAGE: false, RecoveryAmount: 20, targetPlayerID: cMatchRoom.currentPlayerTurn);
                    else
                        Lib.RecoveryHPAsync(cMatchRoom, STAGE: false, COLLAB: false, BACKSTAGE: true, RecoveryAmount: 20, targetPlayerID: cMatchRoom.currentPlayerTurn, _DuelAction.targetCard.cardPosition);

                    ResetResolution();
                    break;
                case "hSD01-015":

                    stage.GetCardInfo();

                    if ((stage.cardName.Equals("AZKi") || stage.cardName.Equals("SorAZ")) && CheerDeck.Count > 0)
                    {
                        tempHandList = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
                        //add to temp hand so we can get latter
                        tempHandList.Add(CheerDeck[CheerDeck.Count - 1]);
                        //just for safety, lets change the position to cheer, so the rotine who is gonna use this know where came from to remove
                        tempHandList[0].playedFrom = "CardCheer";
                        //setup list to send to player

                        var handler190 = new AttachCheerEnergyHandler();

                        _DuelAction.actionObject = JsonSerializer.Serialize(new List<string>() { tempHandList[0].cardNumber }, Lib.options);
                        _DuelAction.targetCard = stage;

                        PlayerRequest _playerRequest = new PlayerRequest { type = "DuelUpdate", description = "OnCollabEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                        await handler190.AttachCheerEnergyHandleAsync(_DuelAction, cMatchRoom, stage: true, collab: false, back: false, TOPCHEERDECK: true, FULLCHEERDECK: false, ClientEnergyIndex: 0);
                    }
                    if ((stage.cardName.Equals("ときのそら") || stage.cardName.Equals("SorAZ")) && playerDeck.Count > 0)
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
                    //send the info to the currentplayer so he can pick the card
                    _DuelAction.actionObject = JsonSerializer.Serialize(HoloPowerDeck, Lib.options);
                    pReturnData = new PlayerRequest { type = "DuelUpdate", description = "OnCollabEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };

                    cMatchRoom.currentCardResolvingStage = "1";
                    await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn.ToString()], pReturnData);
                    await Lib.SendMessage(MessageDispatcher.playerConnections[MatchRoom.GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn).ToString()], pReturnData);
                    break;
                case "hSD01-0071":
                    var handler1 = new PickFromListThenGiveBacKFromHandHandler();
                    await handler1.PickFromHoloPowerThenGiveBacKFromHandHandleAsync(playerRequest);
                    ResetResolution();
                    break;
                case "hSD01-009":
                    diceValue = Lib.GetDiceNumber(cMatchRoom, cMatchRoom.currentPlayerTurn);

                    cMatchRoom.currentCardResolvingStage = "1";

                    Lib.SendDiceRollAsync(cMatchRoom, new() { diceValue }, COUNTFORRESONSE: false);


                    if (playerCheer.Count < 1 || backPos.Count < 1)
                    {
                        ResetResolution();
                        return;
                    }

                    pReturnData = new PlayerRequest { type = "DuelUpdate", description = "OnCollabEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                    await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn], pReturnData);
                    break;
                case "hSD01-0091":
                    diceValue = diceList.Last();

                    returnToclient = new List<string>() { diceValue.ToString() };

                    if (diceValue > 4)
                    {
                        ResetResolution();
                        return;
                    }

                    if (diceValue < 5 && CheerDeck.Count > 0)
                    {
                        //add to temp hand so we can get latter
                        tempHandList.Add(CheerDeck[CheerDeck.Count - 1]);
                        //just for safety, lets change the position to cheer, so the rotine who is gonna use this know where came from to remove
                        tempHandList[0].playedFrom = "CardCheer";
                        //setup list to send to player
                        returnToclient = new List<string>() { tempHandList[0].cardNumber };
                    }

                    cMatchRoom.currentCardResolvingStage = "2";

                    //send the info to the currentplayer so he can pick the card
                    _DuelAction.actionObject = JsonSerializer.Serialize(returnToclient, Lib.options);
                    pReturnData = new PlayerRequest { type = "DuelUpdate", description = "OnCollabEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                    await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn], pReturnData);
                    await Lib.SendMessage(MessageDispatcher.playerConnections[MatchRoom.GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn)], pReturnData);
                    break;
                case "hSD01-0092":
                    await new AttachCheerEnergyHandler().AttachCheerEnergyHandleAsync(_DuelAction, cMatchRoom, stage: false, collab: false, back: true, TOPCHEERDECK: true, FULLCHEERDECK: false, ClientEnergyIndex: 0);

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
                    CheerDeck = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerACardCheer : cMatchRoom.playerBCardCheer;

                    if (CheerDeck.Count < 1)
                    {
                        ResetResolution();
                        return;
                    }

                    tempHandList = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;

                    List<Card> listToSend = new();
                    foreach (Card card in CheerDeck)
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
                    await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn.ToString()], pReturnData);
                    break;
                case "hSD01-0121":
                    if (!_DuelAction.actionObject.Equals("Yes"))
                    {
                        ResetResolution();
                        return;
                    }
                    playerTempHand.Add(playerCheer.Last());
                    await new AttachCheerEnergyHandler().AttachCheerEnergyHandleAsync(_DuelAction, cMatchRoom, true, false, false, TOPCHEERDECK: true, FULLCHEERDECK: false);
                    ResetResolution();
                    break;
                case "hBP01-016":
                    if (stage.cardTag.ToLower().Contains("#Promise"))
                    {
                        Lib.UseCardEffectDrawAny(cMatchRoom, 1, "hBP01-016");
                    }
                    ResetResolution();
                    break;
                case "hBP01-023":
                    Lib.UseCardEffectDrawAny(cMatchRoom, 2, "hBP01-023");
                    ResetResolution();
                    break;
                case "hBP01-083":
                    diceValue = Lib.GetDiceNumber(cMatchRoom, cMatchRoom.currentPlayerTurn);
                    cMatchRoom.currentCardResolvingStage = "1";

                    Lib.SendDiceRollAsync(cMatchRoom, new List<int>() { diceValue }, COUNTFORRESONSE: true);
                    break;
                case "hBP01-0831":
                    diceValue = diceList.Last();

                    if (CheerDeck.Count == 0 || !stage.cardTag.Contains("#ID") || diceValue < 3)
                    {
                        ResetResolution();
                        return;
                    }

                    returnToclient = new List<string>() { diceValue.ToString() };

                    //updating current resolving state to wait for the repsonse
                    cMatchRoom.currentCardResolvingStage = "2";

                    //send the info to the currentplayer so he can pick the card
                    _DuelAction.actionObject = JsonSerializer.Serialize(returnToclient, Lib.options);
                    pReturnData = new PlayerRequest { type = "DuelUpdate", description = "OnCollabEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };

                    await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn.ToString()], pReturnData);
                    await Lib.SendMessage(MessageDispatcher.playerConnections[MatchRoom.GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn).ToString()], pReturnData);
                    break;
                case "hBP01-0832":
                    await new AttachCheerEnergyHandler().AttachCheerEnergyHandleAsync(_DuelAction, cMatchRoom, stage: true, collab: true, back: true, TOPCHEERDECK: true);
                    ResetResolution();
                    break;
                case "hBP01-039":
                    diceValue = Lib.GetDiceNumber(cMatchRoom, cMatchRoom.currentPlayerTurn);
                    cMatchRoom.currentCardResolvingStage = "1";

                    Lib.SendDiceRollAsync(cMatchRoom, new List<int>() { diceValue }, COUNTFORRESONSE: true);
                    break;
                case "hBP01-0391":
                    diceValue = diceList.Last();

                    if (CheerDeck.Count == 0 || !oshi.cardName.Equals("兎田ぺこら") || Lib.IsOddNumber(diceValue))
                    {
                        ResetResolution();
                        return;
                    }

                    returnToclient = new List<string>() { diceValue.ToString() };

                    //updating current resolving state to wait for the repsonse
                    cMatchRoom.currentCardResolvingStage = "2";

                    //send the info to the currentplayer so he can pick the card
                    _DuelAction.actionObject = JsonSerializer.Serialize(returnToclient, Lib.options);
                    pReturnData = new PlayerRequest { type = "DuelUpdate", description = "OnCollabEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };

                    await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn.ToString()], pReturnData);
                    await Lib.SendMessage(MessageDispatcher.playerConnections[MatchRoom.GetOtherPlayer(cMatchRoom, cMatchRoom.currentPlayerTurn).ToString()], pReturnData);
                    break;
                case "hBP01-0392":
                    await new AttachCheerEnergyHandler().AttachCheerEnergyHandleAsync(_DuelAction, cMatchRoom, stage: true, collab: true, back: true, TOPCHEERDECK: true);
                    ResetResolution();
                    break;
                case "hBP01-020":
                    DuelAction da = new() { playerID = cMatchRoom.currentPlayerTurn };
                    foreach (Card card in playerDeck)
                    {
                        if (card.cardTag.Contains("#Promise")) {
                            da.cardList.Add(card);
                        }
                    }
                    if (da.cardList.Count == 0)
                    {
                        ResetResolution();
                        return;
                    }

                    cMatchRoom.currentCardResolvingStage = "1";
                    tempHandList.AddRange(da.cardList);

                    pReturnData = new PlayerRequest { type = "DuelUpdate", description = "OnCollabEffect", requestObject = JsonSerializer.Serialize(da, Lib.options) };
                    await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn], pReturnData);
                    break;
                case "hBP01-0201":
                    int n = Lib.CheckIfCardExistAtList(cMatchRoom, cMatchRoom.currentPlayerTurn, _DuelAction.actionObject, "TempHand");

                    if (n == -1)
                    {
                        Lib.WriteConsoleMessage($"invalid selection for {_DuelAction.actionObject}");
                        return;
                    }

                    playerHand.Add(playerDeck[n]);
                    playerDeck.RemoveAt(n);
                    cMatchRoom.ShuffleCards(playerDeck);

                    da = new()
                    {
                        playerID = cMatchRoom.currentPlayerTurn,
                        suffle = true,
                        zone = "Deck",
                        cardList = new() { new Card(_DuelAction.actionObject) }
                    };

                    Lib.SendPlayerData(cMatchRoom, true, _DuelAction, "DrawCollabEffect");
                    ResetResolution();
                    break;
                case "hBP01-031":
                    da = new() { playerID = cMatchRoom.currentPlayerTurn };
                    da.cardList = HoloPowerDeck;

                    if (da.cardList.Count == 0)
                    {
                        ResetResolution();
                        return;
                    }
                    tempHandList.AddRange(da.cardList);
                    cMatchRoom.currentCardResolvingStage = "1";

                    pReturnData = new PlayerRequest { type = "DuelUpdate", description = "OnCollabEffect", requestObject = JsonSerializer.Serialize(da, Lib.options) };
                    await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn], pReturnData);
                    break;
                case "hBP01-0311":
                    n = Lib.CheckIfCardExistAtList(cMatchRoom, cMatchRoom.currentPlayerTurn, _DuelAction.actionObject, "TempHand");

                    if (n == -1)
                    {
                        Lib.WriteConsoleMessage($"invalid selection for {_DuelAction.actionObject}");
                        return;
                    }

                    playerHand.Add(HoloPowerDeck[n]);
                    HoloPowerDeck.RemoveAt(n);
                    cMatchRoom.ShuffleCards(HoloPowerDeck);

                    DuelAction removeDA = new() { playerID = cMatchRoom.currentPlayerTurn, usedCard = new() { playedFrom = "Deck", cardPosition = "HoloPower" } };
                    Lib.SendPlayerData(cMatchRoom, true, removeDA, "MoveCardToZone");

                    da = new()
                    {
                        playerID = cMatchRoom.currentPlayerTurn,
                        suffle = true,
                        zone = "HoloPower",
                        cardList = new() { new Card(_DuelAction.actionObject) }
                    };

                    Lib.SendPlayerData(cMatchRoom, true, da, "DrawCollabEffect");
                    ResetResolution();
                    break;
                case "hBP01-075":
                    int playerAhandCount = cMatchRoom.playerAHand.Count;
                    int playerBhandCount = cMatchRoom.playerBHand.Count;

                    cMatchRoom.playerADeck.InsertRange(0, cMatchRoom.playerAHand);
                    cMatchRoom.playerAHand.Clear();
                    cMatchRoom.playerBDeck.InsertRange(0, cMatchRoom.playerBHand);
                    cMatchRoom.playerBHand.Clear();

                    Lib.getCardFromDeck(cMatchRoom.playerADeck, cMatchRoom.playerAHand, playerAhandCount);
                    DuelAction PADuelAction = new() { 
                        playerID = cMatchRoom.firstPlayer,
                        cardList = cMatchRoom.playerAHand,
                        suffleBackToDeck = true,
                    };
                    Lib.SendPlayerData(cMatchRoom, false, PADuelAction, "DrawCollabEffect");

                    Lib.getCardFromDeck(cMatchRoom.playerBDeck, cMatchRoom.playerBHand, playerBhandCount);
                    DuelAction PBDuelAction = new()
                    {
                        playerID = cMatchRoom.secondPlayer,
                        cardList = cMatchRoom.playerBHand,
                        suffleBackToDeck = true,
                    };
                    Lib.SendPlayerData(cMatchRoom, false, PBDuelAction, "DrawCollabEffect");

                    ResetResolution();
                    break;
                case "hBP01-101":
                    da = new() { playerID = cMatchRoom.currentPlayerTurn };
                    da.cardList = playerArquive;

                    if (da.cardList.Count == 0)
                    {
                        ResetResolution();
                        return;
                    }

                    cMatchRoom.currentCardResolvingStage = "1";

                    tempHandList.AddRange(da.cardList);

                    pReturnData = new PlayerRequest { type = "DuelUpdate", description = "OnCollabEffect", requestObject = JsonSerializer.Serialize(da, Lib.options) };
                    await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn], pReturnData);
                    break;
                case "hBP01-1011":
                    n = Lib.CheckIfCardExistAtList(cMatchRoom, cMatchRoom.currentPlayerTurn, _DuelAction.actionObject, "TempHand");

                    if (n == -1)
                    {
                        Lib.WriteConsoleMessage($"invalid selection for {_DuelAction.actionObject}");
                        return;
                    }

                    playerHand.Add(playerArquive[n]);
                    playerArquive.RemoveAt(n);


                    da = new()
                    {
                        playerID = cMatchRoom.currentPlayerTurn,
                        cardList = new() { new Card(_DuelAction.actionObject) }
                    };

                    Lib.SendPlayerData(cMatchRoom, reveal: true, da, "RemoveCardsFromArquive");
                    Lib.SendPlayerData(cMatchRoom, reveal: true, da, "DrawCollabEffect");

                    ResetResolution();
                    break;
                case "hBP01-077":
                    bool energyPaid = Lib.PayCardEffectCheerOrEquipCost(cMatchRoom, _DuelAction.cheerCostCard.cardPosition, _DuelAction.cheerCostCard.cardNumber);

                    if (!energyPaid || !oshi.cardName.Equals("星街すいせい"))
                    {
                        Lib.WriteConsoleMessage($"invalid selection for cost {_DuelAction.actionObject}");
                        return;
                    }

                    Lib.getCardFromDeck(playerDeck, playerHand, 2);
                    cMatchRoom.currentCardResolvingStage = "1";

                    da = new()
                    {
                        playerID = cMatchRoom.currentPlayerTurn,
                        cardList = playerHand.TakeLast(2).ToList(),
                    };

                    Lib.SendPlayerData(cMatchRoom, reveal: true, da, "DrawCollabEffect");

                    ResetResolution();
                    break;
                case "hBP01-096":
                    diceValue = Lib.GetDiceNumber(cMatchRoom, cMatchRoom.currentPlayerTurn);

                    cMatchRoom.currentCardResolvingStage = "1";

                    Lib.SendDiceRollAsync(cMatchRoom, new() { diceValue }, COUNTFORRESONSE: true);

                    pReturnData = new PlayerRequest { type = "DuelUpdate", description = "OnCollabEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                    await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn], pReturnData);
                    break;
                case "hBP01-0961":
                    if (!Lib.IsOddNumber(diceList.Last()))
                    {
                        Lib.WriteConsoleMessage($"not odd number");
                        ResetResolution();
                        return;
                    }

                    da = new() { playerID = cMatchRoom.currentPlayerTurn };

                    foreach (Card card in playerDeck)
                    {
                        if (card.cardType.Equals("Buzzホロメン"))
                        {
                            da.cardList.Add(card);
                        }
                    }

                    cMatchRoom.currentCardResolvingStage = "2";

                    if (da.cardList.Count == 0)
                    {
                        ResetResolution();
                        return;
                    }
                    tempHandList.AddRange(da.cardList);

                    pReturnData = new PlayerRequest { type = "DuelUpdate", description = "OnCollabEffect", requestObject = JsonSerializer.Serialize(da, Lib.options) };
                    await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn], pReturnData);
                    break;
                case "hBP01-0962":
                    n = Lib.CheckIfCardExistAtList(cMatchRoom, cMatchRoom.currentPlayerTurn, _DuelAction.actionObject, "TempHand");

                    if (n == -1)
                    {
                        Lib.WriteConsoleMessage($"invalid selection for {_DuelAction.actionObject}");
                        return;
                    }

                    playerHand.Add(playerDeck[n]);
                    playerDeck.RemoveAt(n);
                    cMatchRoom.ShuffleCards(playerDeck);

                    da = new()
                    {
                        playerID = cMatchRoom.currentPlayerTurn,
                        suffle = true,
                        zone = "Deck",
                        cardList = new() { new Card(_DuelAction.actionObject) }
                    };

                    Lib.SendPlayerData(cMatchRoom, true, _DuelAction, "DrawCollabEffect");
                    ResetResolution();
                    break;
                case "hBP01-099":
                    diceValue = Lib.GetDiceNumber(cMatchRoom, cMatchRoom.currentPlayerTurn);
                    cMatchRoom.currentCardResolvingStage = "1";
                    Lib.SendDiceRollAsync(cMatchRoom, new() { diceValue }, COUNTFORRESONSE: true);
                    pReturnData = new PlayerRequest { type = "DuelUpdate", description = "OnCollabEffect", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.options) };
                    await Lib.SendMessage(MessageDispatcher.playerConnections[cMatchRoom.currentPlayerTurn], pReturnData);
                    break;
                case "hBP01-0991":
                    if (!Lib.IsOddNumber(diceList.Last()))
                    {
                        Lib.WriteConsoleMessage($"not odd number");
                        ResetResolution();
                        return;
                    }
                    if (Lib.IsSwitchBlocked(cMatchRoom, _DuelAction.targetCard.cardPosition) || Lib.IsSwitchBlocked(cMatchRoom, _DuelAction.usedCard.cardPosition))
                    {
                        Lib.WriteConsoleMessage("Cannot retreat by effect");
                        return;
                    }
                    Lib.SwittchCardYToCardZButKeepPosition(cMatchRoom, MatchRoom.GetOtherPlayer(cMatchRoom, playerRequest.playerID), _DuelAction.targetCard);
                    Lib.SendPlayerData(cMatchRoom, false, _DuelAction, "SwitchStageCard");
                    ResetResolution();
                    break;
                case "hBP01-100":
                    if (_DuelAction.SelectedCards.Count > 3) {
                        Lib.WriteConsoleMessage($"to much cards selected");
                        ResetResolution();
                        return;
                    }
                    int exist = -1;

                    da = new() { playerID = cMatchRoom.currentPlayerTurn };

                    foreach (string cardnumber in _DuelAction.SelectedCards) {
                        exist = Lib.CheckIfCardExistAtList(cMatchRoom, cMatchRoom.currentPlayerTurn, cardnumber, list: "Arquive");
                        if (exist == -1 && playerArquive[exist].GetCardInfo().cardType.Equals("エール"))
                        {
                            Lib.WriteConsoleMessage($"invalid card");
                            ResetResolution();
                            return;
                        }
                        playerArquive[exist].playedFrom = "Arquive";
                        playerArquive[exist].cardPosition = "CheerDeck";
                        da.cardList.Add(playerArquive[exist]);
                        playerArquive.RemoveAt(exist);
                    }
                    Lib.SendPlayerData(cMatchRoom, reveal: true, da, "MoveCardToZone");
                    ResetResolution();
                    break;
                default:
                    ResetResolution();
                    break;
            }
            void ResetResolution()
            {
                cMatchRoom.currentCardResolving = "";
                cMatchRoom.currentCardResolvingStage = "";
                cMatchRoom.ResolvingEffectChain.Clear();

                List<Card> temphand = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;
                temphand.Clear();

                List<int> diceRollList = cMatchRoom.currentPlayerTurn.Equals(cMatchRoom.firstPlayer) ? cMatchRoom.playerADiceRollList : cMatchRoom.playerBDiceRollList;
                diceRollList.Clear();
            }
        }

        internal static async Task OnCollabEffectResolutionAsync(ConcurrentDictionary<string, WebSocket> playerConnections, List<MatchRoom> matchRooms, PlayerRequest playerRequest, WebSocket webSocket)
        {
            DuelAction _DuelAction = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestObject);

            MatchRoom cMatchRoom = MatchRoom.FindPlayerMatchRoom(playerRequest.playerID);

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